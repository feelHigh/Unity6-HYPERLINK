using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 스킬 활성화 시스템 (리팩토링 버전)
/// 
/// 주요 변경사항:
/// - [CHANGED] AOE/Melee 스킬 데미지 로직 제거 → SkillAnimationController로 이관
/// - [REASON] 애니메이션 타이밍에 맞춘 데미지 적용 위해
/// - 기존 기능 모두 유지 (Ranged, Buff, Heal)
/// 
/// 역할 분담:
/// - SkillActivationSystem: 유효성 검사, 마나 소비, 쿨다운 관리, 이벤트 발생
/// - SkillAnimationController: 애니메이션 재생, 타이밍 제어, 데미지 적용
/// 
/// 핵심 기능:
/// - 키 바인드 기반 스킬 활성화 (Q/W/E)
/// - 스킬 타입별 실행 분기
/// - 쿨다운 및 마나 관리
/// </summary>
public class SkillActivationSystem : MonoBehaviour
{
    [Header("캐릭터 참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;

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

    // 현재 활성화된 버프 목록
    private Dictionary<SkillData, Coroutine> _activeBuffs = new Dictionary<SkillData, Coroutine>();

    // 키 바인드 목록
    private KeyCode[] _skillKeys;

    /// <summary>
    /// 스킬 실행 이벤트
    /// SkillAnimationController에서 구독하여 애니메이션 재생
    /// </summary>
    public static event System.Action<SkillData> OnSkillExecuted;

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
    /// 1. 유효성 검사 (쿨다운, 마나)
    /// 2. 마나 소비
    /// 3. 스킬 실행 (타입별 분기)
    /// 4. 쿨다운 시작
    /// </summary>
    public void ActivateSkill(SkillData skill)
    {
        if (skill == null || _playerCharacter == null)
            return;

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
    /// 
    /// 주의:
    /// - Melee/AOE: 이벤트만 발생 (데미지는 SkillAnimationController에서 처리)
    /// - Ranged/Buff/Heal: 직접 처리
    /// </summary>
    private void ExecuteSkill(SkillData skill)
    {
        switch (skill.SkillType)
        {
            case SkillType.Melee:
            case SkillType.AreaOfEffect:
                // 애니메이션 컨트롤러에 알림만 전송
                // 실제 데미지는 SkillAnimationController에서 타이밍에 맞춰 적용
                OnSkillExecuted?.Invoke(skill);
                Debug.Log($"[{skill.SkillName}] 애니메이션 시작 → 데미지는 타이밍에 적용됨");
                break;

            case SkillType.Ranged:
                ExecuteRangedSkill(skill);
                OnSkillExecuted?.Invoke(skill);
                break;

            case SkillType.Buff:
                ExecuteBuffSkill(skill);
                OnSkillExecuted?.Invoke(skill);
                break;

            case SkillType.Heal:
                ExecuteHealSkill(skill);
                OnSkillExecuted?.Invoke(skill);
                break;
        }
    }

    #endregion

    #region 스킬 타입별 실행 로직

    /// <summary>
    /// 원거리 스킬 실행
    /// 
    /// 투사체 생성 및 발사
    /// </summary>
    private void ExecuteRangedSkill(SkillData skill)
    {
        if (skill.ProjectilePrefab == null)
        {
            Debug.LogError($"[{skill.SkillName}] 투사체 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 발사 위치 계산
        Vector3 spawnOffset = transform.forward * 1f + Vector3.up * 1.5f;
        Vector3 spawnPosition = transform.position + spawnOffset;

        // 투사체 생성
        GameObject projectileObj = Instantiate(
            skill.ProjectilePrefab,
            spawnPosition,
            transform.rotation
        );

        // Projectile 초기화
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            float damage = CalculateSkillDamage(skill);
            projectile.Initialize(damage, skill.Range, _playerCharacter);
            Debug.Log($"[{skill.SkillName}] 투사체 발사! 데미지: {damage:F0}");
        }
        else
        {
            Debug.LogError($"[{skill.SkillName}] 투사체 프리팹에 Projectile 스크립트가 없습니다!");
            Destroy(projectileObj);
        }
    }

    /// <summary>
    /// 버프 스킬 실행
    /// 
    /// 임시 스탯 증가
    /// </summary>
    private void ExecuteBuffSkill(SkillData skill)
    {
        // 이미 같은 버프가 활성화되어 있으면 중복 적용 방지
        if (_activeBuffs.ContainsKey(skill))
        {
            Debug.LogWarning($"[{skill.SkillName}] 이미 버프가 활성화되어 있습니다!");
            return;
        }

        // 버프 적용
        Coroutine buffCoroutine = StartCoroutine(ApplyTemporaryBuff(skill));
        _activeBuffs.Add(skill, buffCoroutine);
    }

    /// <summary>
    /// 회복 스킬 실행
    /// </summary>
    private void ExecuteHealSkill(SkillData skill)
    {
        float healAmount = skill.Damage;
        _playerCharacter.Heal(healAmount);
        Debug.Log($"[{skill.SkillName}] 체력 회복: {healAmount}");
    }

    /// <summary>
    /// 임시 버프 적용 코루틴
    /// </summary>
    private IEnumerator ApplyTemporaryBuff(SkillData skill)
    {
        // 1. 버프 스탯 생성
        CharacterStats buffStats = CreateBuffStats(skill.BuffAmount);

        // 2. PlayerCharacter에 스탯 추가
        _playerCharacter.AddTemporaryStats(buffStats);
        Debug.Log($"[{skill.SkillName}] 버프 적용: +{skill.BuffAmount} 스탯, {skill.BuffDuration}초");

        // 3. 지속시간 대기
        yield return new WaitForSeconds(skill.BuffDuration);

        // 4. 스탯 제거
        _playerCharacter.RemoveTemporaryStats(buffStats);
        Debug.Log($"[{skill.SkillName}] 버프 종료");

        // 5. 활성 버프 목록에서 제거
        if (_activeBuffs.ContainsKey(skill))
        {
            _activeBuffs.Remove(skill);
        }
    }

    /// <summary>
    /// 버프용 CharacterStats 생성
    /// 
    /// 직업별 주요 스탯 증가
    /// </summary>
    private CharacterStats CreateBuffStats(float buffAmount)
    {
        CharacterClass playerClass = _playerCharacter.CharacterClass;

        switch (playerClass)
        {
            case CharacterClass.Laon:
                return new CharacterStatsBuilder()
                    .SetStrength((int)buffAmount)
                    .Build();

            case CharacterClass.Sian:
                return new CharacterStatsBuilder()
                    .SetIntelligence((int)buffAmount)
                    .Build();

            case CharacterClass.Yujin:
                return new CharacterStatsBuilder()
                    .SetDexterity((int)buffAmount)
                    .Build();

            default:
                return new CharacterStatsBuilder()
                    .SetStrength((int)buffAmount)
                    .Build();
        }
    }

    /// <summary>
    /// 모든 버프 강제 종료
    /// </summary>
    public void ClearAllBuffs()
    {
        foreach (var buff in _activeBuffs)
        {
            if (buff.Value != null)
            {
                StopCoroutine(buff.Value);
            }
        }
        _activeBuffs.Clear();
        Debug.Log("모든 버프 제거 완료");
    }

    #endregion

    #region 데미지 계산

    /// <summary>
    /// 스킬 데미지 계산 (크리티컬 포함)
    /// 
    /// 공식: 기본 데미지 × (1 + 주요스탯/100) × 크리티컬배율
    /// </summary>
    private float CalculateSkillDamage(SkillData skill)
    {
        // 기본 데미지 + 주요 스탯 보너스
        int mainStat = _playerCharacter.GetMainStat();
        float baseDamage = skill.Damage;
        float damageWithStat = baseDamage * (1f + mainStat / 100f);

        // 크리티컬 체크
        CharacterStats stats = _playerCharacter.CurrentStats;
        float critChance = stats.CriticalChance;
        float critDamage = stats.CriticalDamage;

        if (Random.Range(0f, 100f) < critChance)
        {
            damageWithStat *= (1f + critDamage / 100f);
            Debug.Log($"크리티컬 히트! 데미지: {damageWithStat:F0}");
        }

        return damageWithStat;
    }

    #endregion

    #region 쿨다운 관리

    private void StartCooldown(SkillData skill)
    {
        _skillCooldowns[skill] = skill.Cooldown;

        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot.SkillData == skill)
            {
                slot.StartCooldown();
            }
        }
    }

    private bool IsSkillOnCooldown(SkillData skill)
    {
        if (_skillCooldowns.ContainsKey(skill))
        {
            return _skillCooldowns[skill] > 0f;
        }
        return false;
    }

    private void UpdateCooldowns()
    {
        List<SkillData> skillsToUpdate = new List<SkillData>(_skillCooldowns.Keys);

        foreach (SkillData skill in skillsToUpdate)
        {
            if (_skillCooldowns[skill] > 0f)
            {
                _skillCooldowns[skill] -= Time.deltaTime;

                if (_skillCooldowns[skill] <= 0f)
                {
                    _skillCooldowns[skill] = 0f;
                }
            }
        }
    }

    #endregion

    #region UI 피드백

    private void ShowManaCostWarning(SkillData skill)
    {
        Debug.Log($"마나 부족! 현재: {_playerCharacter.CurrentMana:F0} / 필요: {skill.ManaCost}");

        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot.SkillData == skill)
            {
                // slot.FlashRed(); // 향후 구현
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
        Debug.Log($"활성 버프: {_activeBuffs.Count}개");

        Debug.Log("--- 키 바인드 ---");
        for (int i = 0; i < _skillKeys.Length; i++)
        {
            Debug.Log($"  슬롯 {i + 1}: {_skillKeys[i]}");
        }

        if (_activeBuffs.Count > 0)
        {
            Debug.Log("--- 활성 버프 목록 ---");
            foreach (var buff in _activeBuffs)
            {
                Debug.Log($"  - {buff.Key.SkillName}");
            }
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
