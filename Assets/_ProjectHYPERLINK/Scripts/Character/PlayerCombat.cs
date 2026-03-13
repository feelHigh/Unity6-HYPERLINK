using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 전투 시스템
/// 
/// 역할:
/// - 데미지 받기
/// - 특수 공격 효과 처리 (IMonsterDamageable 인터페이스 구현)
/// - 사망 처리
/// - 골드 관리
/// </summary>
public class PlayerCombat : MonoBehaviour, IDamageable, IMonsterDamageable
{
    [Header("참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;
    [SerializeField] private PlayerStateController _stateController;
    [SerializeField] private PlayerNavController _navController;

    // 상태이상 디버프 추적
    private Coroutine _currentDebuffCoroutine;
    private GameObject _currentHitEffect;
    private GameObject _currentDebuffEffect;
    private GameObject _currentAdditionalEffect;
    private Vector3 _lastAttackerPosition;
    // 히트 스턴 관리
    private Coroutine _hitStunCoroutine;

    [Header("히트 스턴 설정")]
    [SerializeField, Tooltip("일반 피격 시 스턴 지속 시간 (초)")]
    private float _hitStunDuration = 0.2f;

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacter>();
        _stateController = GetComponent<PlayerStateController>();
        _navController = GetComponent<PlayerNavController>();

        if (_playerCharacter == null)
        {
            DebugHelper.LogError("[PlayerCombat] PlayerCharacter 참조가 없습니다!");
        }

        if (_stateController == null)
        {
            DebugHelper.LogError("[PlayerCombat] PlayerStateController 참조가 없습니다!");
        }

        if (_navController == null)
        {
            DebugHelper.LogError("[PlayerCombat] PlayerNavController 참조가 없습니다!");
        }
    }

    #region IDamageable Implementation

    /// <summary>
    /// 단순 데미지만 받기 (하위 호환성)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_playerCharacter != null)
        {
            _playerCharacter.TakeDamage(damage);
            ApplyHitStun();
        }
        else
        {
            DebugHelper.LogError("[PlayerCombat] PlayerCharacter 참조가 없습니다!");
        }
    }

    /// <summary>
    /// AttackInfo와 함께 데미지 받기 (히트 VFX 포함)
    /// </summary>
    public void TakeDamage(AttackInfo attackInfo)
    {
        if (_playerCharacter != null && IsAlive())
        {
            // 데미지 적용
            _playerCharacter.TakeDamage(attackInfo.Damage);

            // 피격 사운드 재생
            var audio = AudioManager.Instance;
            if (audio?.SoundLibrary != null)
            {
                audio.PlaySFX(audio.SoundLibrary.PlayerHit);
            }

            DebugHelper.Log($"[PlayerCombat] {attackInfo.Type} 공격 받음: {attackInfo.Damage:F1} 데미지");

            // 히트 VFX 생성
            if (attackInfo.HitVfxConfig != null && attackInfo.HitVfxConfig.IsValid())
            {
                SpawnHitVFX(attackInfo.HitVfxConfig, attackInfo.HitPosition);
            }

            ApplyHitStun();
        }
    }

    /// <summary>
    /// 히트 VFX 생성 (Offset, Rotation, Size 적용)
    /// </summary>
    private void SpawnHitVFX(HitVfxConfig config, Vector3 basePosition)
    {
        if (config == null || !config.IsValid())
        {
            DebugHelper.LogWarning("[PlayerCombat] HitVfxConfig가 유효하지 않습니다.");
            return;
        }

        Vector3 spawnPosition = basePosition + config.PositionOffset;
        Quaternion spawnRotation = Quaternion.Euler(config.RotationOffset);

        GameObject vfx = GameObjectPool.Instance.Get(config.VfxPrefab, spawnPosition, spawnRotation);
        vfx.transform.localScale = config.VfxPrefab.transform.localScale * config.Scale;

        float lifetime = config.Lifetime;
        if (lifetime <= 0)
        {
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            }
            else
            {
                lifetime = 3f;
            }
        }

        GameObjectPool.Instance.Release(vfx, lifetime);
    }

    public void Die()
    {
        DebugHelper.Log("[PlayerCombat] 플레이어 사망!");

        // 사망 사운드 재생
        var deathAudio = AudioManager.Instance;
        if (deathAudio?.SoundLibrary != null)
        {
            deathAudio.PlaySFX(deathAudio.SoundLibrary.PlayerDeath);
        }

        CleanupAllEffects();

        if (_stateController != null)
        {
            _stateController.ResetAllStates();
        }
    }

    public float GetCurrentHealth() => _playerCharacter?.CurrentHealth ?? 0f;
    public float GetMaxHealth() => _playerCharacter?.MaxHealth ?? 0f;
    public bool IsAlive() => _playerCharacter?.IsAlive ?? false;

    #endregion

    #region IMonsterDamageable 구현

    /// <summary>
    /// 에픽 몬스터의 특수 공격 효과 적용
    /// </summary>
    public void ApplySpecialEffect(SpecialAttackBase attack, Vector3 attackerPosition)
    {
        if (_playerCharacter == null || !IsAlive()) return;

        _lastAttackerPosition = attackerPosition;

        DebugHelper.Log($"[PlayerCombat] 특수 공격 받음: {attack.Type}");

        // 즉시 데미지 적용
        float maxHp = _playerCharacter.MaxHealth;
        float instantDamage = maxHp * attack.InstantDamage;
        TakeDamage(instantDamage);

        // 피격 이펙트
        if (attack.HitEffect != null)
        {
            _currentHitEffect = Instantiate(attack.HitEffect, transform.position, Quaternion.identity);
        }

        CleanupCurrentDebuff();

        // 속성별 상태이상 적용
        switch (attack.Type)
        {
            case SpecialAttackType.Fire:
                _currentDebuffCoroutine = StartCoroutine(BurnCoroutine(attack));
                break;
            case SpecialAttackType.Water:
                _currentDebuffCoroutine = StartCoroutine(FreezeCoroutine(attack));
                break;
            case SpecialAttackType.Earth:
                _currentDebuffCoroutine = StartCoroutine(SilenceCroutine(attack));
                break;
            case SpecialAttackType.Wood:
                _currentDebuffCoroutine = StartCoroutine(RootCoroutine(attack));
                break;
            case SpecialAttackType.Metal:
                _currentDebuffCoroutine = StartCoroutine(KnockbackCoroutine(attack));
                break;
        }
    }

    #endregion

    #region 상태이상 코루틴

    private IEnumerator BurnCoroutine(SpecialAttackBase attack)
    {
        DebugHelper.Log("[상태이상] 화상 시작");

        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        float maxHp = _playerCharacter.MaxHealth;
        float dotDamage = maxHp * attack.DotDamage;
        float tick = attack.DotTickInterval;
        float timer = 0f;

        while (timer < attack.DotDuration)
        {
            yield return WaitForSecondsCache.Get(tick);
            timer += tick;

            if (!IsAlive()) break;

            _playerCharacter.TakeDamage(dotDamage);
            DebugHelper.Log($"[화상] DoT 데미지: {dotDamage:F1}");
        }

        CleanupDebuffEffect();
        DebugHelper.Log("[상태이상] 화상 종료");
    }

    private IEnumerator FreezeCoroutine(SpecialAttackBase attack)
    {
        DebugHelper.Log("[상태이상] 빙결 시작");

        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        if (_stateController != null)
        {
            _stateController.SetFreeze(true);
        }

        yield return WaitForSecondsCache.Get(attack.FreezeDuration);

        if (_stateController != null)
        {
            _stateController.SetFreeze(false);
        }

        CleanupDebuffEffect();

        float slowPercent = attack.SlowPercent;
        float slowDuration = attack.SlowDuration;

        if (slowDuration > 0 && slowPercent > 0)
        {
            _currentAdditionalEffect = SpawnDebuffEffect(attack.AdditionalEffect);

            if (_stateController != null)
            {
                _stateController.SetSlow(true, slowPercent);
            }

            DebugHelper.Log($"[둔화] {slowPercent:F0}% 감소, {slowDuration}초");

            yield return WaitForSecondsCache.Get(slowDuration);

            if (_stateController != null)
            {
                _stateController.SetSlow(false, 0f);
            }

            CleanupAdditionalEffect();
            DebugHelper.Log("[둔화] 종료");
        }

        DebugHelper.Log("[상태이상] 빙결 종료");
    }

    private IEnumerator SilenceCroutine(SpecialAttackBase attack)
    {
        DebugHelper.Log("[상태이상] 침묵 시작");
        /*
        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        if (_stateController != null)
        {
            _stateController.SetFreeze(true);
        }

        yield return WaitForSecondsCache.Get(attack.BlindDuration);

        if (_stateController != null)
        {
            _stateController.SetFreeze(false);
        }

        CleanupDebuffEffect();
        */

        // 침묵 효과 (수정: SetSilence 사용)
        if (attack.SilenceDuration > 0)
        {
            _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

            if (_stateController != null)
            {
                _stateController.SetSilence(true);
            }

            DebugHelper.Log($"[침묵] {attack.SilenceDuration}초");

            yield return WaitForSecondsCache.Get(attack.SilenceDuration);

            if (_stateController != null)
            {
                _stateController.SetSilence(false);
            }

            CleanupDebuffEffect();
            DebugHelper.Log("[침묵] 종료");
        }

        //DebugHelper.Log("[상태이상] 시야 방해 종료");
    }

    private IEnumerator RootCoroutine(SpecialAttackBase attack)
    {
        DebugHelper.Log("[상태이상] 속박 시작");

        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        if (_stateController != null)
        {
            _stateController.SetRoot(true);
        }

        if (_navController != null)
        {
            _navController.ForceStop();
        }

        yield return WaitForSecondsCache.Get(attack.RootDuration);

        if (_stateController != null)
        {
            _stateController.SetRoot(false);
        }

        CleanupDebuffEffect();

        // 약화 효과
        float debuffPercent = attack.DefenseDebuffPercent;
        float debuffDuration = attack.DefenseDebuffDuration;

        if (debuffDuration > 0 && debuffPercent > 0)
        {
            _currentAdditionalEffect = SpawnDebuffEffect(attack.AdditionalEffect);

            if (_stateController != null)
            {
                _stateController.SetWeaken(true, debuffPercent);
            }

            DebugHelper.Log($"[약화] 방어력 {debuffPercent:F0}% 감소, {debuffDuration}초");

            yield return WaitForSecondsCache.Get(debuffDuration);

            if (_stateController != null)
            {
                _stateController.SetWeaken(false, 0f);
            }

            CleanupAdditionalEffect();
            DebugHelper.Log("[약화] 종료");
        }

        DebugHelper.Log("[상태이상] 속박 종료");
    }

    private IEnumerator KnockbackCoroutine(SpecialAttackBase attack)
    {
        DebugHelper.Log("[상태이상] 넉백 시작");

        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        if (_stateController != null)
        {
            _stateController.SetKnockState(true);
        }

        Vector3 knockbackDir = (transform.position - _lastAttackerPosition).normalized;
        knockbackDir.y = 0;

        if (_navController != null)
        {
            _navController.ApplyKnockback(attack.KnockbackPower, knockbackDir);
        }

        yield return WaitForSecondsCache.Get(attack.StunDuration);

        if (_stateController != null)
        {
            _stateController.SetKnockState(false);
        }

        CleanupDebuffEffect();
        DebugHelper.Log("[상태이상] 넉백 종료");
    }

    #endregion

    #region 히트 스턴

    private void ApplyHitStun()
    {
        if (_stateController == null) return;

        if (_hitStunCoroutine != null)
        {
            StopCoroutine(_hitStunCoroutine);
        }

        _hitStunCoroutine = StartCoroutine(HitStunCoroutine());
    }

    private IEnumerator HitStunCoroutine()
    {
        _stateController.SetHitStun(true);

        yield return WaitForSecondsCache.Get(_hitStunDuration);

        _stateController.SetHitStun(false);
        _hitStunCoroutine = null;
    }

    #endregion

    #region 이펙트 정리

    private GameObject SpawnDebuffEffect(GameObject effectPrefab)
    {
        if (effectPrefab == null) return null;

        return Instantiate(effectPrefab, transform.position, Quaternion.identity, transform);
    }

    private void CleanupDebuffEffect()
    {
        if (_currentDebuffEffect != null)
        {
            Destroy(_currentDebuffEffect);
            _currentDebuffEffect = null;
        }
    }

    private void CleanupAdditionalEffect()
    {
        if (_currentAdditionalEffect != null)
        {
            Destroy(_currentAdditionalEffect);
            _currentAdditionalEffect = null;
        }
    }

    private void CleanupHitEffect()
    {
        if (_currentHitEffect != null)
        {
            Destroy(_currentHitEffect);
            _currentHitEffect = null;
        }
    }

    private void CleanupCurrentDebuff()
    {
        if (_currentDebuffCoroutine != null)
        {
            StopCoroutine(_currentDebuffCoroutine);
            _currentDebuffCoroutine = null;
        }

        CleanupDebuffEffect();
        CleanupAdditionalEffect();
        CleanupHitEffect();
    }

    private void CleanupAllEffects()
    {
        CleanupCurrentDebuff();

        if (_hitStunCoroutine != null)
        {
            StopCoroutine(_hitStunCoroutine);
            _hitStunCoroutine = null;
        }
    }

    #endregion

    #region 골드

    private int _currentGold = 0;
    public int CurrentGold => _currentGold;

    public void AddGold(int amount)
    {
        _currentGold += amount;
        DebugHelper.Log($"[PlayerCombat] 골드 획득: +{amount} (총: {_currentGold})");
    }

    public bool SpendGold(int amount)
    {
        if (_currentGold >= amount)
        {
            _currentGold -= amount;
            DebugHelper.Log($"[PlayerCombat] 골드 사용: -{amount} (남은 골드: {_currentGold})");
            return true;
        }

        DebugHelper.Log($"[PlayerCombat] 골드 부족: 필요={amount}, 현재={_currentGold}");
        return false;
    }

    #endregion
}
