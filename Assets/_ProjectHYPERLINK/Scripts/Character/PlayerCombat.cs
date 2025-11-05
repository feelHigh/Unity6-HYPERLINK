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
            Debug.LogError("[PlayerCombat] PlayerCharacter 참조가 없습니다!");
        }

        if (_stateController == null)
        {
            Debug.LogError("[PlayerCombat] PlayerStateController 참조가 없습니다!");
        }

        if (_navController == null)
        {
            Debug.LogError("[PlayerCombat] PlayerNavController 참조가 없습니다!");
        }
    }

    #region IDamageable Implementation

    public void TakeDamage(float damage)
    {
        if (_playerCharacter != null)
        {
            _playerCharacter.TakeDamage(damage);

            // 일반 피격 시 히트 스턴 적용
            ApplyHitStun();
        }
        else
        {
            Debug.LogError("[PlayerCombat] PlayerCharacter 참조가 없습니다!");
        }
    }

    public void Die()
    {
        Debug.Log("[PlayerCombat] 플레이어 사망!");

        // 모든 상태이상 효과 정리
        CleanupAllEffects();

        // 상태 초기화
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

        // 공격자 위치 저장 (넉백 방향 계산용)
        _lastAttackerPosition = attackerPosition;

        Debug.Log($"[PlayerCombat] 특수 공격 받음: {attack.Type}");

        // 즉시 데미지 적용
        float maxHp = _playerCharacter.MaxHealth;
        float instantDamage = maxHp * attack.InstantDamage;
        TakeDamage(instantDamage);

        // 피격 이펙트
        if (attack.HitEffect != null)
        {
            _currentHitEffect = Instantiate(attack.HitEffect, transform.position, Quaternion.identity);
        }

        // 기존 디버프 정리 (새 디버프 적용 전)
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
                _currentDebuffCoroutine = StartCoroutine(BlindCoroutine(attack));
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

    /// <summary>
    /// 화상 효과
    /// - 즉시 5% 피해
    /// - 5초 동안 1초마다 1% 피해
    /// </summary>
    private IEnumerator BurnCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("[상태이상] 화상 시작");

        // 화상 이펙트 생성
        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        float maxHp = _playerCharacter.MaxHealth;
        float dotDamage = maxHp * attack.DotDamage;
        float tick = attack.DotTickInterval;
        float timer = 0f;

        // 지속 데미지
        while (timer < attack.DotDuration && IsAlive())
        {
            yield return new WaitForSeconds(tick);
            TakeDamage(dotDamage);
            Debug.Log($"[화상] DoT 피해: {dotDamage:F1}");
            timer += tick;
        }

        CleanupDebuffEffect();
        Debug.Log("[상태이상] 화상 종료");
    }

    /// <summary>
    /// 빙결 효과
    /// - 즉시 15% 피해
    /// - 3초 빙결 (이동/공격/스킬 불가)
    /// - 2초 둔화 (이동속도 50% 감소)
    /// </summary>
    private IEnumerator FreezeCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("[상태이상] 빙결 시작");

        // 빙결 이펙트
        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        // 빙결 상태 적용
        if (_stateController != null)
        {
            _stateController.SetFreeze(true);
        }

        // NavMeshAgent 정지
        if (_navController != null)
        {
            _navController.ForceStop();
        }

        // 빙결 대기
        yield return new WaitForSeconds(attack.FreezeDuration);

        // 빙결 해제
        if (_stateController != null)
        {
            _stateController.SetFreeze(false);
        }

        CleanupDebuffEffect();

        // 둔화 효과로 전환
        _currentAdditionalEffect = SpawnDebuffEffect(attack.AdditionalEffect);

        if (_stateController != null)
        {
            _stateController.SetSlow(true, attack.SlowPercent);
        }

        Debug.Log($"[빙결] 둔화 시작 ({attack.SlowPercent}%)");

        // 둔화 대기
        yield return new WaitForSeconds(attack.SlowDuration);

        // 둔화 해제
        if (_stateController != null)
        {
            _stateController.SetSlow(false, 0f);
        }

        CleanupAdditionalEffect();
        Debug.Log("[상태이상] 빙결 종료");
    }

    /// <summary>
    /// 실명 효과
    /// - 즉시 10% 피해
    /// - 4초 침묵 (스킬 사용 불가)
    /// </summary>
    private IEnumerator BlindCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("[상태이상] 실명 시작");

        // 실명 이펙트
        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        // 침묵 상태 적용
        if (_stateController != null)
        {
            _stateController.SetSilence(true);
        }

        Debug.Log($"[실명] 침묵 적용 ({attack.SilenceDuration}초)");

        // TODO: 시야 감소 효과 (카메라 PostProcessing 연동)
        // 예: CameraEffects.SetVignetteIntensity(0.8f);

        // 침묵 대기
        yield return new WaitForSeconds(attack.SilenceDuration);

        // 침묵 해제
        if (_stateController != null)
        {
            _stateController.SetSilence(false);
        }

        CleanupDebuffEffect();
        Debug.Log("[상태이상] 실명 종료");
    }

    /// <summary>
    /// 속박 효과
    /// - 즉시 10% 피해
    /// - 3초 속박 (이동 불가, 공격/스킬 가능)
    /// - 5초 방어력 30% 감소
    /// </summary>
    private IEnumerator RootCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("[상태이상] 속박 시작");

        // 속박 이펙트
        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        // 속박 상태 적용
        if (_stateController != null)
        {
            _stateController.SetRoot(true);
        }

        // NavMeshAgent 정지
        if (_navController != null)
        {
            _navController.ForceStop();
        }

        Debug.Log($"[속박] 이동 불가 ({attack.RootDuration}초)");

        // 속박 대기
        yield return new WaitForSeconds(attack.RootDuration);

        // 속박 해제
        if (_stateController != null)
        {
            _stateController.SetRoot(false);
        }

        CleanupDebuffEffect();

        // 방어력 약화로 전환
        _currentAdditionalEffect = SpawnDebuffEffect(attack.AdditionalEffect);

        if (_stateController != null)
        {
            _stateController.SetWeaken(true, attack.DefenseDebuffPercent);
        }

        Debug.Log($"[속박] 방어력 감소 ({attack.DefenseDebuffPercent}%)");

        // 약화 대기
        yield return new WaitForSeconds(attack.DefenseDebuffDuration);

        // 약화 해제
        if (_stateController != null)
        {
            _stateController.SetWeaken(false, 0f);
        }

        CleanupAdditionalEffect();
        Debug.Log("[상태이상] 속박 종료");
    }

    /// <summary>
    /// 넉백 효과
    /// - 즉시 15% 피해
    /// - 4미터 밀려남
    /// - 1초 넉다운 (이동/공격/스킬 불가)
    /// </summary>
    private IEnumerator KnockbackCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("[상태이상] 넉백 시작");

        // 넉다운 이펙트
        _currentDebuffEffect = SpawnDebuffEffect(attack.DebuffEffect);

        // 넉백 적용 (공격자 방향으로)
        if (_navController != null)
        {
            // 넉백 방향 계산: 공격자 → 플레이어
            Vector3 knockbackDir = (transform.position - _lastAttackerPosition).normalized;
            knockbackDir.y = 0; // 수평 방향만
            _navController.ApplyKnockback(attack.KnockbackPower, knockbackDir);
        }

        // 넉다운 상태 적용
        if (_stateController != null)
        {
            _stateController.SetKnockState(true);
        }

        Debug.Log($"[넉백] 넉다운 ({attack.StunDuration}초)");

        // 넉다운 대기
        yield return new WaitForSeconds(attack.StunDuration);

        // 넉다운 해제
        if (_stateController != null)
        {
            _stateController.SetKnockState(false);
        }

        CleanupDebuffEffect();
        Debug.Log("[상태이상] 넉백 종료");
    }


    /// <summary>
    /// 히트 스턴 적용 (일반 피격)
    /// - 짧은 시간 동안 모든 행동 불가
    /// </summary>
    private void ApplyHitStun()
    {
        // 이미 히트 스턴 중이거나 더 강한 상태이상 중이면 무시
        if (_hitStunCoroutine != null) return;
        if (_stateController != null && (_stateController.IsFrozen || _stateController.IsStunned)) return;

        // 히트 스턴 코루틴 시작
        _hitStunCoroutine = StartCoroutine(HitStunCoroutine());
    }

    /// <summary>
    /// 히트 스턴 코루틴
    /// </summary>
    private IEnumerator HitStunCoroutine()
    {
        Debug.Log($"[히트 스턴] 시작 ({_hitStunDuration}초)");

        // 히트 스턴 상태 적용
        if (_stateController != null)
        {
            _stateController.SetHitStun(true);
        }

        // NavMeshAgent 정지
        if (_navController != null)
        {
            _navController.ForceStop();
        }

        // 히트 스턴 대기
        yield return new WaitForSeconds(_hitStunDuration);

        // 히트 스턴 해제
        if (_stateController != null)
        {
            _stateController.SetHitStun(false);
        }

        _hitStunCoroutine = null;
        Debug.Log("[히트 스턴] 종료");
    }

    #endregion

    #region 이펙트 관리

    /// <summary>
    /// 디버프 이펙트 생성
    /// </summary>
    private GameObject SpawnDebuffEffect(GameObject effectPrefab)
    {
        if (effectPrefab != null)
        {
            return Instantiate(effectPrefab, transform);
        }
        return null;
    }

    /// <summary>
    /// 현재 디버프 효과 정리
    /// </summary>
    private void CleanupCurrentDebuff()
    {
        if (_currentDebuffCoroutine != null)
        {
            StopCoroutine(_currentDebuffCoroutine);
            _currentDebuffCoroutine = null;
        }

        CleanupHitEffect();
        CleanupDebuffEffect();
        CleanupAdditionalEffect();

        // 모든 상태 해제
        if (_stateController != null)
        {
            _stateController.ResetAllStates();
        }
    }

    /// <summary>
    /// 피격 이펙트 오브젝트 제거
    /// </summary>
    private void CleanupHitEffect()
    {
        if (_currentHitEffect != null)
        {
            Destroy(_currentHitEffect);
            _currentHitEffect = null;
        }
    }

    /// <summary>
    /// 디버프 이펙트 오브젝트 제거
    /// </summary>
    private void CleanupDebuffEffect()
    {
        if (_currentDebuffEffect != null)
        {
            Destroy(_currentDebuffEffect);
            _currentDebuffEffect = null;
        }
    }

    /// <summary>
    /// 추가 디버프 이펙트 오브젝트 제거
    /// </summary>
    private void CleanupAdditionalEffect()
    {
        if (_currentAdditionalEffect != null)
        {
            Destroy(_currentAdditionalEffect);
            _currentAdditionalEffect = null;
        }
    }

    /// <summary>
    /// 모든 상태이상 효과 정리 (사망 시 호출)
    /// </summary>
    private void CleanupAllEffects()
    {
        CleanupCurrentDebuff();

        // 히트 스턴 코루틴 정지
        if (_hitStunCoroutine != null)
        {
            StopCoroutine(_hitStunCoroutine);
            _hitStunCoroutine = null;
        }
    }

    #endregion

    #region 디버그

    [ContextMenu("Test: Fire Attack")]
    private void TestFireAttack()
    {
        // 테스트용 Fire 공격 생성 필요
        Debug.Log("Fire Attack 테스트를 위해 SA_Fire ScriptableObject를 연결하세요");
    }

    [ContextMenu("Test: Clear All Debuffs")]
    private void TestClearDebuffs()
    {
        CleanupCurrentDebuff();
        Debug.Log("모든 디버프 정리 완료");
    }

    #endregion
}
