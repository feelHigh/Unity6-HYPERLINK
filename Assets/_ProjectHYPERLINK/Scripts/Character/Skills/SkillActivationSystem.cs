using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// 스킬 활성화 시스템
/// </summary>
public class SkillActivationSystem : MonoBehaviour
{
    [Header("캐릭터 참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;
    [SerializeField] private PlayerStateController _stateController;
    [SerializeField] private PlayerNavController _navController;

    [Header("스킬 슬롯")]
    [SerializeField] private List<SkillSlotUI> _skillSlots = new List<SkillSlotUI>();

    [Header("키 바인드 설정")]
    [Tooltip("첫 번째 스킬 슬롯 키 (기본: Q)")]
    [SerializeField] private KeyCode _skill1Key = KeyCode.Q;

    [Tooltip("두 번째 스킬 슬롯 키 (기본: W)")]
    [SerializeField] private KeyCode _skill2Key = KeyCode.W;

    [Tooltip("세 번째 스킬 슬롯 키 (기본: E)")]
    [SerializeField] private KeyCode _skill3Key = KeyCode.E;

    [Tooltip("네 번째 스킬 슬롯 키 (기본: R)")]
    [SerializeField] private KeyCode _skill4Key = KeyCode.R;

    [Header("디버그 설정")]
    [SerializeField] private bool _showDebugGizmos = true;

    // 스킬 쿨다운 추적
    private Dictionary<SkillData, float> _skillCooldowns = new Dictionary<SkillData, float>();
    private List<SkillData> _cooldownKeysCache = new List<SkillData>();
    private List<SkillData> _completedCooldowns = new List<SkillData>();

    // 키 바인드 목록
    private KeyCode[] _skillKeys;

    /// <summary>
    /// 스킬 실행 이벤트
    /// </summary>
    public static event System.Action<SkillData> OnSkillExecuted;

    /// <summary>
    /// 스킬 실행 이벤트 (데미지 포함)
    /// </summary>
    public static event System.Action<SkillData, float> OnSkillExecutedWithDamage;

    #region 초기화

    private void Awake()
    {
        if (_playerCharacter == null)
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
            if (_playerCharacter == null)
            {
                DebugHelper.LogError("[SkillActivationSystem] PlayerCharacter를 찾을 수 없습니다!");
            }
        }

        if (_stateController == null)
        {
            _stateController = GetComponent<PlayerStateController>();
            if (_stateController == null)
            {
                DebugHelper.LogError("[SkillActivationSystem] PlayerStateController를 찾을 수 없습니다!");
            }
        }

        if (_navController == null)
        {
            _navController = GetComponent<PlayerNavController>();
            if (_navController == null)
            {
                DebugHelper.LogWarning("[SkillActivationSystem] PlayerNavController를 찾을 수 없습니다!");
            }
        }

        _skillKeys = new KeyCode[] { _skill1Key, _skill2Key, _skill3Key, _skill4Key };
    }

    private void Update()
    {
        UpdateCooldowns();
        HandleSkillInput();
    }

    #endregion

    #region 입력 처리

    /// <summary>
    /// 스킬 키 입력 처리
    /// </summary>
    private void HandleSkillInput()
    {
        // 사망 상태 체크
        if (_playerCharacter == null || !_playerCharacter.IsAlive)
        {
            return;
        }

        // 기본 공격 중 체크 추가
        if (_stateController != null && _stateController.IsPerformingBaseAttack)
        {
            return;
        }

        // 스킬 사용 불가 상태 체크 (침묵, 빙결, 넉다운)
        if (_stateController != null && !_stateController.CanUseSkill)
        {
            return;
        }

        for (int i = 0; i < _skillSlots.Count && i < _skillKeys.Length; i++)
        {
            if (Input.GetKeyDown(_skillKeys[i]))
            {
                SkillSlotUI slot = _skillSlots[i];

                // 슬롯 null 체크
                if (slot == null)
                {
                    DebugHelper.LogWarning($"[SkillActivation] 슬롯 {i}가 null입니다!");
                    continue;
                }

                // 빈 슬롯 체크
                if (slot.IsEmpty)
                {
                    DebugHelper.Log($"[SkillActivation] 슬롯 {i}가 비어있습니다!");
                    continue;
                }

                // SkillData null 체크
                if (slot.SkillData == null)
                {
                    DebugHelper.LogWarning($"[SkillActivation] 슬롯 {i}의 SkillData가 null입니다!");
                    continue;
                }

                // 잠금 상태 체크
                if (slot.IsLocked)
                {
                    DebugHelper.Log($"[SkillActivation] {slot.SkillData.SkillName}이(가) 잠겨있습니다!");
                    continue;
                }

                // 스킬 활성화
                ActivateSkill(slot.SkillData);
            }
        }
    }

    public KeyCode GetSkillKey(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _skillKeys.Length)
            return _skillKeys[slotIndex];
        return KeyCode.None;
    }

    public void SetSkillKey(int slotIndex, KeyCode newKey)
    {
        if (slotIndex >= 0 && slotIndex < _skillKeys.Length)
        {
            _skillKeys[slotIndex] = newKey;
            DebugHelper.Log($"스킬 슬롯 {slotIndex + 1} 키 변경: {newKey}");
        }
    }

    #endregion

    #region 스킬 활성화

    /// <summary>
    /// 스킬 활성화 메인 메서드
    /// </summary>
    public void ActivateSkill(SkillData skill)
    {
        if (skill == null || _playerCharacter == null)
            return;

        // 사망 상태 체크
        if (!_playerCharacter.IsAlive)
        {
            DebugHelper.Log($"[SkillActivation] 플레이어가 사망 상태입니다!");
            return;
        }

        // 기본 공격 중 체크 추가
        if (_stateController != null && _stateController.IsPerformingBaseAttack)
        {
            DebugHelper.Log($"[SkillActivation] 기본 공격 중이므로 스킬을 사용할 수 없습니다!");
            return;
        }

        // 스킬 사용 불가 상태 체크
        if (_stateController != null && !_stateController.CanUseSkill)
        {
            DebugHelper.Log($"[침묵] 스킬 사용 불가 상태입니다!");
            return;
        }

        // 속박 상태에서 대시 스킬(UseRootMotion=false) 차단
        if (_stateController != null && _stateController.IsRoot && !skill.UseRootMotion)
        {
            DebugHelper.Log($"[속박] {skill.SkillName}은(는) 이동을 수반하므로 사용할 수 없습니다!");
            return;
        }

        // 쿨다운 체크
        if (IsSkillOnCooldown(skill))
        {
            DebugHelper.Log($"{skill.SkillName}이 쿨다운 중입니다!");
            return;
        }

        // 마나 체크 및 소비
        if (!_playerCharacter.ConsumeMana(skill.ManaCost))
        {
            DebugHelper.Log($"마나 부족! 필요: {skill.ManaCost}");
            ShowManaCostWarning(skill);
            return;
        }

        // 스킬 실행 상태 설정
        if (_stateController != null)
        {
            _stateController.SetSkillPerforming(true);
        }

        // 스킬 실행
        ExecuteSkill(skill);

        // 스킬 캐스팅 사운드 재생
        var audioMgr = AudioManager.Instance;
        if (audioMgr?.SoundLibrary != null)
        {
            // SkillData에 커스텀 사운드가 있으면 사용, 없으면 기본 사운드
            AudioClip castSound = skill.SkillCastSound != null ? skill.SkillCastSound : audioMgr.SoundLibrary.PlayerSkillCast;

            if (castSound != null)
            {
                audioMgr.PlaySFX(castSound);
            }
        }

        // 쿨다운 시작
        StartCooldown(skill);

        DebugHelper.Log($"{skill.SkillName} 사용!");
    }

    /// <summary>
    /// 스킬 타입별 실행 분기
    /// </summary>
    private void ExecuteSkill(SkillData skill)
    {
        // 데미지 계산
        float damage = CalculateSkillDamage(skill);

        switch (skill.SkillType)
        {
            case SkillType.Melee:
            case SkillType.AreaOfEffect:
                // SkillAnimationController에서 데미지 처리
                OnSkillExecuted?.Invoke(skill);
                OnSkillExecutedWithDamage?.Invoke(skill, damage);
                DebugHelper.Log($"[{skill.SkillName}] 애니메이션 시작, 데미지: {damage:F1}");
                break;

            case SkillType.Ranged:
                ExecuteRangedSkill(skill);
                OnSkillExecuted?.Invoke(skill);
                break;

            case SkillType.Buff:
            case SkillType.Heal:
                // TODO: Buff/Heal 구현
                DebugHelper.LogWarning($"[{skill.SkillName}] {skill.SkillType} 타입은 아직 구현되지 않았습니다!");
                OnSkillExecuted?.Invoke(skill);
                break;

            default:
                DebugHelper.LogWarning($"알 수 없는 스킬 타입: {skill.SkillType}");
                break;
        }
    }

    /// <summary>
    /// 원거리 스킬 실행
    /// </summary>
    private void ExecuteRangedSkill(SkillData skill)
    {
        if (skill.ProjectilePrefab == null)
        {
            DebugHelper.LogError($"[{skill.SkillName}] 투사체 프리팹이 없습니다!");
            return;
        }

        // 발사 위치
        Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;

        // 발사 방향
        Vector3 direction = transform.forward;

        // 투사체 생성 (풀 사용)
        GameObject projectileObj = GameObjectPool.Instance.Get(skill.ProjectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        // 데미지 계산
        float damage = CalculateSkillDamage(skill);

        // 히트 VFX 전달
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(damage, skill.Range, _playerCharacter, skill.EnemyHitVfxConfig);
        }

        DebugHelper.Log($"[{skill.SkillName}] 투사체 발사 완료");
    }

    #endregion

    #region 쿨다운 관리

    private void StartCooldown(SkillData skill)
    {
        if (!_skillCooldowns.ContainsKey(skill))
        {
            _skillCooldowns.Add(skill, skill.Cooldown);
        }
        else
        {
            _skillCooldowns[skill] = skill.Cooldown;
        }

        SkillSlotUI slot = FindSlotWithSkill(skill);
        if (slot != null)
        {
            slot.StartCooldown(skill.Cooldown);
        }

        DebugHelper.Log($"[{skill.SkillName}] 쿨다운 시작: {skill.Cooldown}초");
    }

    private void UpdateCooldowns()
    {
        if (_skillCooldowns.Count == 0) return;

        _cooldownKeysCache.Clear();
        foreach (var kvp in _skillCooldowns)
            _cooldownKeysCache.Add(kvp.Key);
        _completedCooldowns.Clear();

        foreach (SkillData skill in _cooldownKeysCache)
        {
            if (_skillCooldowns[skill] > 0)
            {
                _skillCooldowns[skill] -= Time.deltaTime;

                if (_skillCooldowns[skill] <= 0)
                {
                    _completedCooldowns.Add(skill);
                }
            }
        }

        foreach (SkillData skill in _completedCooldowns)
        {
            _skillCooldowns[skill] = 0;
            DebugHelper.Log($"[{skill.SkillName}] 쿨다운 완료");
        }
    }

    private bool IsSkillOnCooldown(SkillData skill)
    {
        if (_skillCooldowns.ContainsKey(skill))
        {
            return _skillCooldowns[skill] > 0;
        }
        return false;
    }

    public float GetRemainingCooldown(SkillData skill)
    {
        if (_skillCooldowns.ContainsKey(skill))
        {
            return Mathf.Max(0, _skillCooldowns[skill]);
        }
        return 0f;
    }

    #endregion

    #region 데미지 계산

    /// <summary>
    /// 스킬 데미지 계산
    /// 
    /// 공식: ((캐릭터 공격력 × 스킬 배율) + 스킬 기본 데미지) × (1 + (주요 스탯 × 스탯당 데미지 증가%))
    /// </summary>
    private float CalculateSkillDamage(SkillData skill)
    {
        float characterAttackDamage = GetCharacterAttackDamage();
        int mainStat = _playerCharacter.GetMainStat();

        float skillMultiplier = skill.SkillMultiplier;
        float skillBaseDamage = skill.SkillBaseDamage;
        float mainStatDamageIncrease = skill.MainStatDamageIncrease;

        float damage = ((characterAttackDamage * skillMultiplier) + skillBaseDamage)
                       * (1f + (mainStat * mainStatDamageIncrease));

        // 크리티컬 판정
        CharacterStats stats = _playerCharacter.CurrentStats;
        if (Random.Range(0f, 100f) < stats.CriticalChance)
        {
            damage *= (1f + stats.CriticalDamage / 100f);
            DebugHelper.Log($"[{skill.SkillName}] 크리티컬 히트!");
        }

        DebugHelper.Log($"[{skill.SkillName}] 데미지 계산: " +
                  $"(({characterAttackDamage:F1} × {skillMultiplier:F1}) + {skillBaseDamage:F1}) × " +
                  $"(1 + ({mainStat} × {mainStatDamageIncrease:F2})) = {damage:F1}");

        return damage;
    }

    private float GetCharacterAttackDamage()
    {
        if (_playerCharacter != null)
        {
            return _playerCharacter.GetAttackPower();
        }

        DebugHelper.LogWarning("[SkillActivation] PlayerCharacter가 없어 기본 공격력(25)을 사용합니다.");
        return 25f;
    }

    #endregion

    #region UI 피드백

    private void ShowManaCostWarning(SkillData skill)
    {
        DebugHelper.Log($"[마나 부족] 현재: {_playerCharacter.CurrentMana:F0} / 필요: {skill.ManaCost}");

        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot.SkillData == skill)
            {
                // TODO: UI 플래시 효과
            }
        }
    }

    #endregion

    #region 스킬 슬롯 관리

    public void RegisterSkillSlot(SkillSlotUI slot)
    {
        if (!_skillSlots.Contains(slot))
        {
            _skillSlots.Add(slot);
        }
    }

    public void UnregisterSkillSlot(SkillSlotUI slot)
    {
        _skillSlots.Remove(slot);
    }

    public List<SkillSlotUI> GetAllSkillSlots()
    {
        return _skillSlots;
    }

    #endregion

    #region 스킬 슬롯 유틸리티

    public bool HasSkill(SkillData skillData, int excludeSlotIndex = -1)
    {
        if (skillData == null) return false;

        for (int i = 0; i < _skillSlots.Count; i++)
        {
            if (i == excludeSlotIndex) continue;

            if (_skillSlots[i] != null && _skillSlots[i].SkillData == skillData)
            {
                return true;
            }
        }

        return false;
    }

    public SkillSlotUI FindSlotWithSkill(SkillData skillData)
    {
        if (skillData == null) return null;

        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot != null && slot.SkillData == skillData)
            {
                return slot;
            }
        }

        return null;
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug: Print Skill System Status")]
    private void DebugPrintStatus()
    {
        DebugHelper.Log("===== SkillActivationSystem 상태 =====");
        DebugHelper.Log($"등록된 스킬 슬롯: {_skillSlots.Count}개");
        DebugHelper.Log($"쿨다운 추적 중: {_skillCooldowns.Count}개");

        DebugHelper.Log("--- 키 바인드 ---");
        for (int i = 0; i < _skillKeys.Length; i++)
        {
            DebugHelper.Log($"  슬롯 {i + 1}: {_skillKeys[i]}");
        }

        if (_skillCooldowns.Count > 0)
        {
            DebugHelper.Log("--- 쿨다운 목록 ---");
            foreach (var cooldown in _skillCooldowns)
            {
                if (cooldown.Value > 0)
                {
                    DebugHelper.Log($"  - {cooldown.Key.SkillName}: {cooldown.Value:F1}초 남음");
                }
            }
        }

        if (_playerCharacter != null)
        {
            DebugHelper.Log($"플레이어 생존 여부: {_playerCharacter.IsAlive}");
        }

        if (_stateController != null)
        {
            DebugHelper.Log($"스킬 사용 가능: {_stateController.CanUseSkill}");
            DebugHelper.Log($"기본 공격 중: {_stateController.IsPerformingBaseAttack}");
            DebugHelper.Log($"스킬 실행 중: {_stateController.IsPerformingSkill}");
        }

        if (_playerCharacter != null)
        {
            DebugHelper.Log("--- 데미지 계산 정보 ---");
            DebugHelper.Log($"  캐릭터 공격력: {GetCharacterAttackDamage():F1}");
            DebugHelper.Log($"  주요 스탯: {_playerCharacter.GetMainStat()}");
            DebugHelper.Log($"  클래스: {_playerCharacter.CharacterClass}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_showDebugGizmos)
            return;

        Color[] colors = { Color.red, Color.blue, Color.green };

        for (int i = 0; i < _skillSlots.Count; i++)
        {
            if (_skillSlots[i] != null && _skillSlots[i].SkillData != null)
            {
                SkillData skill = _skillSlots[i].SkillData;
                Gizmos.color = colors[i % colors.Length];
                Gizmos.DrawWireSphere(transform.position, skill.Range);
            }
        }
    }

    #endregion
}
