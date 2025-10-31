using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 활성화 시스템
/// 
/// 최근 변경사항:
/// - PlayerStateController 연동 (침묵 상태 체크)
/// - SkillData.cs 실제 구조에 맞춤
/// - UpdateCooldowns() 딕셔너리 수정 버그 수정
/// - 새로운 데미지 공식 적용: ((캐릭터 공격력 × 스킬 배율) + 스킬 기본 데미지) × (1 + (주요 스탯 × 스탯당 데미지 증가%))
/// - [FIX] 사망 상태 체크 추가 (HandleSkillInput, ActivateSkill)
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

    [Header("디버그 설정")]
    [SerializeField] private bool _showDebugGizmos = true;

    // 스킬 쿨다운 추적
    private Dictionary<SkillData, float> _skillCooldowns = new Dictionary<SkillData, float>();

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
                Debug.LogError("[SkillActivationSystem] PlayerCharacter를 찾을 수 없습니다!");
            }
        }

        if (_stateController == null)
        {
            _stateController = GetComponent<PlayerStateController>();
            if (_stateController == null)
            {
                Debug.LogError("[SkillActivationSystem] PlayerStateController를 찾을 수 없습니다!");
            }
        }

        if (_navController == null)
        {
            _navController = GetComponent<PlayerNavController>();
            if (_navController == null)
            {
                Debug.LogWarning("[SkillActivationSystem] PlayerNavController를 찾을 수 없습니다! 기본 공격력 값을 사용합니다.");
            }
        }

        _skillKeys = new KeyCode[] { _skill1Key, _skill2Key, _skill3Key };
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
        // 사망 상태 체크 (최우선)
        if (_playerCharacter == null || !_playerCharacter.IsAlive)
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

                if (slot == null)
                {
                    Debug.LogWarning($"[SkillActivation] 슬롯 {i}가 null입니다!");
                    continue;
                }

                if (slot.SkillData == null)
                {
                    Debug.LogWarning($"[SkillActivation] 슬롯 {i}의 SkillData가 null입니다!");
                    continue;
                }

                if (slot.IsLocked)
                {
                    Debug.Log($"[SkillActivation] {slot.SkillData.SkillName}이(가) 잠겨있습니다!");
                    continue;
                }

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
            Debug.Log($"스킬 슬롯 {slotIndex + 1} 키 변경: {newKey}");
        }
    }

    #endregion

    #region 스킬 활성화

    /// <summary>
    /// 스킬 활성화 메인 메서드
    /// 
    /// 처리 순서:
    /// 1. 사망 상태 체크
    /// 2. 상태이상 체크
    /// 3. 유효성 검사 (쿨다운, 마나)
    /// 4. 마나 소비
    /// 5. 스킬 실행 (타입별 분기)
    /// 6. 쿨다운 시작
    /// </summary>
    public void ActivateSkill(SkillData skill)
    {
        if (skill == null || _playerCharacter == null)
            return;

        // 이중 보안: 사망 상태 최종 확인
        if (!_playerCharacter.IsAlive)
        {
            Debug.Log($"[SkillActivation] 플레이어가 사망 상태입니다!");
            return;
        }

        // 스킬 사용 불가 상태 체크
        if (_stateController != null && !_stateController.CanUseSkill)
        {
            Debug.Log($"[침묵] 스킬 사용 불가 상태입니다!");
            return;
        }

        // 쿨다운 체크
        if (IsSkillOnCooldown(skill))
        {
            Debug.Log($"{skill.SkillName}이 쿨다운 중입니다!");
            return;
        }

        // 마나 체크 및 소비
        if (!_playerCharacter.ConsumeMana(skill.ManaCost))
        {
            Debug.Log($"마나 부족! 필요: {skill.ManaCost}");
            ShowManaCostWarning(skill);
            return;
        }

        // 스킬 실행
        ExecuteSkill(skill);

        // 쿨다운 시작
        StartCooldown(skill);

        Debug.Log($"{skill.SkillName} 사용!");
    }

    /// <summary>
    /// 스킬 타입별 실행 분기
    /// </summary>
    private void ExecuteSkill(SkillData skill)
    {
        // 데미지 계산 (모든 스킬 타입에서 공통으로 사용)
        float damage = CalculateSkillDamage(skill);

        switch (skill.SkillType)
        {
            case SkillType.Melee:
            case SkillType.AreaOfEffect:
                // SkillAnimationController에서 데미지 처리
                OnSkillExecuted?.Invoke(skill);
                OnSkillExecutedWithDamage?.Invoke(skill, damage);
                Debug.Log($"[{skill.SkillName}] 애니메이션 시작, 데미지: {damage:F1}");
                break;

            case SkillType.Ranged:
                ExecuteRangedSkill(skill, damage);
                OnSkillExecuted?.Invoke(skill);
                break;

            case SkillType.Buff:
            case SkillType.Heal:
                // TODO: Buff/Heal 구현을 위해 SkillData.cs에 필요한 프로퍼티 추가 필요
                Debug.LogWarning($"[{skill.SkillName}] {skill.SkillType} 타입은 아직 구현되지 않았습니다!");
                OnSkillExecuted?.Invoke(skill);
                break;
        }
    }

    #endregion

    #region 스킬 타입별 실행 로직

    /// <summary>
    /// 원거리 스킬 실행
    /// </summary>
    private void ExecuteRangedSkill(SkillData skill, float damage)
    {
        if (skill.ProjectilePrefab == null)
        {
            Debug.LogError($"[{skill.SkillName}] 투사체 프리팹이 설정되지 않았습니다!");
            return;
        }

        Vector3 spawnOffset = transform.forward * 1f + Vector3.up * 1.5f;
        Vector3 spawnPosition = transform.position + spawnOffset;

        GameObject projectileObj = Instantiate(
            skill.ProjectilePrefab,
            spawnPosition,
            transform.rotation
        );

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(damage, skill.Range, _playerCharacter);
            Debug.Log($"[{skill.SkillName}] 투사체 발사! 데미지: {damage:F1}");
        }
        else
        {
            Debug.LogError($"[{skill.SkillName}] 투사체에 Projectile 컴포넌트가 없습니다!");
        }
    }

    #endregion

    #region 쿨다운 관리

    /// <summary>
    /// 쿨다운 업데이트
    /// </summary>
    private void UpdateCooldowns()
    {
        // 수정 사항을 임시 저장
        List<SkillData> cooldownsToRemove = new List<SkillData>();
        Dictionary<SkillData, float> cooldownsToUpdate = new Dictionary<SkillData, float>();

        // 읽기 전용으로 순회
        foreach (var kvp in _skillCooldowns)
        {
            SkillData skill = kvp.Key;
            float remainingTime = kvp.Value - Time.deltaTime;

            if (remainingTime <= 0f)
            {
                cooldownsToRemove.Add(skill);
            }
            else
            {
                cooldownsToUpdate[skill] = remainingTime;
            }
        }

        // 순회 완료 후 딕셔너리 수정
        foreach (var kvp in cooldownsToUpdate)
        {
            _skillCooldowns[kvp.Key] = kvp.Value;
            UpdateCooldownUI(kvp.Key, kvp.Value);
        }

        foreach (SkillData skill in cooldownsToRemove)
        {
            _skillCooldowns.Remove(skill);
            UpdateCooldownUI(skill, 0f);
        }
    }

    private void StartCooldown(SkillData skill)
    {
        _skillCooldowns[skill] = skill.Cooldown;
        UpdateCooldownUI(skill, skill.Cooldown);
    }

    private void UpdateCooldownUI(SkillData skill, float remainingTime)
    {
        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot.SkillData == skill)
            {
                slot.UpdateCooldown(remainingTime, skill.Cooldown);
            }
        }
    }

    private bool IsSkillOnCooldown(SkillData skill)
    {
        return _skillCooldowns.ContainsKey(skill) && _skillCooldowns[skill] > 0;
    }

    #endregion

    #region 데미지 계산

    /// <summary>
    /// 스킬 데미지 계산
    /// 
    /// 공식: ((캐릭터 공격력 × 스킬 배율) + 스킬 기본 데미지) × (1 + (주요 스탯 × 스탯당 데미지 증가%))
    /// 
    /// 예시: Sian, Intelligence 10
    /// - 캐릭터 공격력 = 25
    /// - 스킬 배율 = 10
    /// - 스킬 기본 데미지 = 15
    /// - 주요 스탯 (Intelligence) = 10
    /// - 스탯당 데미지 증가% = 0.01
    /// 
    /// 계산: ((25 × 10) + 15) × (1 + (10 × 0.01))
    ///      = (250 + 15) × 1.1
    ///      = 265 × 1.1
    ///      = 291.5
    /// </summary>
    private float CalculateSkillDamage(SkillData skill)
    {
        // 1. 캐릭터 공격력 가져오기
        float characterAttackDamage = GetCharacterAttackDamage();

        // 2. 주요 스탯 가져오기
        int mainStat = _playerCharacter.GetMainStat();

        // 3. 스킬 파라미터 가져오기
        float skillMultiplier = skill.SkillMultiplier;
        float skillBaseDamage = skill.SkillBaseDamage;
        float mainStatDamageIncrease = skill.MainStatDamageIncrease;

        // 4. 최종 데미지 계산
        // ((캐릭터 공격력 × 스킬 배율) + 스킬 기본 데미지) × (1 + (주요 스탯 × 스탯당 데미지 증가%))
        float damage = ((characterAttackDamage * skillMultiplier) + skillBaseDamage)
                       * (1f + (mainStat * mainStatDamageIncrease));

        // 5. 크리티컬 판정
        CharacterStats stats = _playerCharacter.CurrentStats;
        if (Random.Range(0f, 100f) < stats.CriticalChance)
        {
            damage *= (1f + stats.CriticalDamage / 100f);
            Debug.Log($"[{skill.SkillName}] 크리티컬 히트!");
        }

        Debug.Log($"[{skill.SkillName}] 데미지 계산: " +
                  $"(({characterAttackDamage:F1} × {skillMultiplier:F1}) + {skillBaseDamage:F1}) × " +
                  $"(1 + ({mainStat} × {mainStatDamageIncrease:F2})) = {damage:F1}");

        return damage;
    }

    /// <summary>
    /// 캐릭터 공격력 가져오기
    /// 
    /// PlayerNavController의 AttackDamage를 사용합니다.
    /// NavController가 없으면 기본값 25를 사용합니다.
    /// </summary>
    /// <summary>
    /// 캐릭터 공격력 가져오기
    /// 
    /// 새로운 방식: PlayerCharacter의 Physical/Magical Attack 사용
    /// - Laon, Yujin: Physical Attack
    /// - Sian: Magical Attack
    /// 
    /// 스킬 데미지 계산 공식:
    /// ((Physical/Magical Attack × 스킬 배율) + 스킬 기본 데미지) × (1 + (주 스탯 × 증가율))
    /// </summary>
    private float GetCharacterAttackDamage()
    {
        if (_playerCharacter != null)
        {
            return _playerCharacter.GetAttackPower();
        }

        // 폴백: PlayerCharacter가 없을 때
        Debug.LogWarning("[SkillActivation] PlayerCharacter가 없어 기본 공격력(25)을 사용합니다.");
        return 25f;
    }

    #endregion

    #region UI 피드백

    private void ShowManaCostWarning(SkillData skill)
    {
        Debug.Log($"[마나 부족] 현재: {_playerCharacter.CurrentMana:F0} / 필요: {skill.ManaCost}");

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

    #endregion

    #region 디버그

    [ContextMenu("Debug: Print Skill System Status")]
    private void DebugPrintStatus()
    {
        Debug.Log("===== SkillActivationSystem 상태 =====");
        Debug.Log($"등록된 스킬 슬롯: {_skillSlots.Count}개");
        Debug.Log($"쿨다운 추적 중: {_skillCooldowns.Count}개");

        Debug.Log("--- 키 바인드 ---");
        for (int i = 0; i < _skillKeys.Length; i++)
        {
            Debug.Log($"  슬롯 {i + 1}: {_skillKeys[i]}");
        }

        if (_skillCooldowns.Count > 0)
        {
            Debug.Log("--- 쿨다운 목록 ---");
            foreach (var cooldown in _skillCooldowns)
            {
                if (cooldown.Value > 0)
                {
                    Debug.Log($"  - {cooldown.Key.SkillName}: {cooldown.Value:F1}초 남음");
                }
            }
        }

        // 사망 상태 체크 추가
        if (_playerCharacter != null)
        {
            Debug.Log($"플레이어 생존 여부: {_playerCharacter.IsAlive}");
        }

        // 상태이상 체크
        if (_stateController != null)
        {
            Debug.Log($"스킬 사용 가능: {_stateController.CanUseSkill}");
            if (!_stateController.CanUseSkill)
            {
                Debug.Log($"  - 침묵: {_stateController.IsSilenced}");
                Debug.Log($"  - 빙결: {_stateController.IsFrozen}");
                Debug.Log($"  - 넉다운: {_stateController.IsStunned}");
            }
        }

        // 데미지 계산 정보
        if (_playerCharacter != null)
        {
            Debug.Log("--- 데미지 계산 정보 ---");
            Debug.Log($"  캐릭터 공격력: {GetCharacterAttackDamage():F1}");
            Debug.Log($"  주요 스탯: {_playerCharacter.GetMainStat()}");
            Debug.Log($"  클래스: {_playerCharacter.CharacterClass}");
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
