using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 스킬 활성화 시스템
/// 
/// 완성된 기능:
/// - ExecuteRangedSkill(): 투사체 발사 및 추적
/// - ExecuteBuffSkill(): 임시 스탯 증가
/// - ApplyTemporaryBuff(): 버프 지속시간 관리
/// </summary>
public class SkillActivationSystem : MonoBehaviour
{
    [Header("캐릭터 참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;

    [Header("스킬 슬롯")]
    [SerializeField] private List<SkillSlotUI> _skillSlots = new List<SkillSlotUI>();

    // 스킬 쿨다운 추적
    private Dictionary<SkillData, float> _skillCooldowns = new Dictionary<SkillData, float>();

    // 현재 활성화된 버프 목록
    private Dictionary<SkillData, Coroutine> _activeBuffs = new Dictionary<SkillData, Coroutine>();

    #region 초기화

    private void Awake()
    {
        if (_playerCharacter == null)
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
        }
    }

    private void Update()
    {
        UpdateCooldowns();
    }

    #endregion

    #region 스킬 활성화

    /// <summary>
    /// 스킬 활성화 메인 메서드
    /// 
    /// 처리 순서:
    /// 1. 유효성 검사 (null, 쿨다운, 마나)
    /// 2. 리소스 소비 (마나)
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
    /// </summary>
    private void ExecuteSkill(SkillData skill)
    {
        switch (skill.SkillType)
        {
            case SkillType.Melee:
                ExecuteMeleeSkill(skill);
                break;
            case SkillType.Ranged:
                ExecuteRangedSkill(skill);
                break;
            case SkillType.AreaOfEffect:
                ExecuteAOESkill(skill);
                break;
            case SkillType.Buff:
                ExecuteBuffSkill(skill);
                break;
            case SkillType.Heal:
                ExecuteHealSkill(skill);
                break;
        }
    }

    #endregion

    #region 스킬 타입별 실행 로직

    /// <summary>
    /// 근거리 스킬 실행
    /// 
    /// 처리:
    /// - 플레이어 주변 범위 내 적 탐색
    /// - 데미지 계산 및 적용
    /// </summary>
    private void ExecuteMeleeSkill(SkillData skill)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, skill.Range);

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                float damage = CalculateSkillDamage(skill);
                enemy.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// 원거리 스킬 실행
    /// 
    /// 처리 과정:
    /// 1. 투사체 프리팹 검증
    /// 2. 발사 위치 계산
    /// 3. 투사체 인스턴스화
    /// 4. Projectile 컴포넌트 초기화
    /// 5. 데미지 및 범위 설정
    /// 
    /// 요구사항:
    /// - SkillData.ProjectilePrefab 설정 필요
    /// - Projectile 스크립트 필요
    /// - 투사체는 Rigidbody + Trigger Collider 필요
    /// 
    /// 사용 예:
    /// - 화살 발사
    /// - 마법 탄환
    /// - 에너지 볼
    /// </summary>
    private void ExecuteRangedSkill(SkillData skill)
    {
        // 투사체 프리팹 확인
        if (skill.ProjectilePrefab == null)
        {
            Debug.LogError($"[{skill.SkillName}] 투사체 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 발사 위치 계산 (플레이어 앞 1m)
        Vector3 spawnPosition = transform.position + transform.forward * 1f + Vector3.up * 1f;
        Quaternion spawnRotation = transform.rotation;

        // 투사체 생성
        GameObject projectileObj = Instantiate(
            skill.ProjectilePrefab,
            spawnPosition,
            spawnRotation
        );

        // Projectile 컴포넌트 확인 및 초기화
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            float damage = CalculateSkillDamage(skill);
            projectile.Initialize(damage, skill.Range, _playerCharacter);
        }
        else
        {
            Debug.LogError($"[{skill.SkillName}] 투사체에 Projectile 스크립트가 없습니다!");
            Destroy(projectileObj);
        }
    }

    /// <summary>
    /// 광역 스킬 실행
    /// 
    /// 처리:
    /// - 플레이어 중심 또는 타겟 위치
    /// - 범위 내 모든 적에게 데미지
    /// </summary>
    private void ExecuteAOESkill(SkillData skill)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, skill.Range);

        int hitCount = 0;
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                float damage = CalculateSkillDamage(skill);
                enemy.TakeDamage(damage);
                hitCount++;
            }
        }

        Debug.Log($"광역 스킬: {hitCount}명의 적 타격");
    }

    /// <summary>
    /// 버프 스킬 실행 (완전 구현)
    /// 
    /// 처리 과정:
    /// 1. 버프 데이터 검증
    /// 2. 코루틴으로 버프 적용
    /// 3. 지속시간 관리
    /// 4. 버프 종료 후 제거
    /// 
    /// 요구사항:
    /// - SkillData.BuffAmount > 0
    /// - SkillData.BuffDuration > 0
    /// - PlayerCharacter에 AddTemporaryStats/RemoveTemporaryStats 필요
    /// 
    /// 버프 중첩:
    /// - 같은 버프 재사용 시 기존 버프 중단하고 새로 시작
    /// - 다른 버프는 동시 적용 가능
    /// </summary>
    private void ExecuteBuffSkill(SkillData skill)
    {
        // 버프 데이터 검증
        if (skill.BuffAmount <= 0 || skill.BuffDuration <= 0)
        {
            Debug.LogError($"[{skill.SkillName}] 버프 데이터가 올바르지 않습니다! " +
                          $"Amount: {skill.BuffAmount}, Duration: {skill.BuffDuration}");
            return;
        }

        // 기존 버프가 있으면 중단
        if (_activeBuffs.ContainsKey(skill))
        {
            StopCoroutine(_activeBuffs[skill]);
            _activeBuffs.Remove(skill);
            Debug.Log($"[{skill.SkillName}] 기존 버프 중단");
        }

        // 새 버프 시작
        Coroutine buffCoroutine = StartCoroutine(ApplyTemporaryBuff(skill));
        _activeBuffs[skill] = buffCoroutine;
    }

    /// <summary>
    /// 힐 스킬 실행
    /// </summary>
    private void ExecuteHealSkill(SkillData skill)
    {
        _playerCharacter.Heal(skill.Damage);
        Debug.Log($"체력 회복: {skill.Damage}");
    }

    #endregion

    #region 버프 시스템 (신규 구현)

    /// <summary>
    /// 임시 버프 적용 코루틴 (신규)
    /// 
    /// 처리 과정:
    /// 1. 버프 스탯 생성
    /// 2. PlayerCharacter에 스탯 추가
    /// 3. 지속시간 대기
    /// 4. 스탯 제거
    /// 5. 활성 버프 목록에서 제거
    /// 
    /// 버프 스탯 계산:
    /// - BuffAmount가 주요 스탯에 추가
    /// - 직업별 스탯 적용:
    ///   * Warrior: Strength + BuffAmount
    ///   * Mage: Intelligence + BuffAmount
    ///   * Archer: Dexterity + BuffAmount
    /// 
    /// 사용 예:
    /// - 스킬: "전투 분노" (힘 +50, 10초)
    /// - BuffAmount = 50
    /// - BuffDuration = 10
    /// - 결과: 10초간 힘 +50
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
    /// 버프용 CharacterStats 생성 (신규)
    /// 
    /// 직업별 스탯 증가:
    /// - Warrior: Strength
    /// - Mage: Intelligence
    /// - Archer: Dexterity
    /// 
    /// CharacterStats Builder 패턴 사용
    /// </summary>
    private CharacterStats CreateBuffStats(float buffAmount)
    {
        CharacterClass playerClass = _playerCharacter.CharacterClass;

        switch (playerClass)
        {
            case CharacterClass.Warrior:
                return new CharacterStatsBuilder()
                    .SetStrength((int)buffAmount)
                    .Build();

            case CharacterClass.Mage:
                return new CharacterStatsBuilder()
                    .SetIntelligence((int)buffAmount)
                    .Build();

            case CharacterClass.Archer:
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
    /// 모든 버프 강제 종료 (디버그/리셋용)
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
    /// 계산 공식:
    /// 1. 기본 데미지 = skill.Damage
    /// 2. 주요 스탯 보너스 = 기본 × (1 + 주요스탯/100)
    /// 3. 크리티컬 체크
    /// 4. 크리티컬 시 = 데미지 × (1 + 크리데미지/100)
    /// 
    /// 예시:
    /// - 기본 데미지: 50
    /// - 주요 스탯: 100 (2배)
    /// - 크리티컬: 150% (2.5배)
    /// - 최종: 50 × 2 × 2.5 = 250
    /// </summary>
    private float CalculateSkillDamage(SkillData skill)
    {
        // 1. 기본 데미지 + 주요 스탯 보너스
        int mainStat = _playerCharacter.GetMainStat();
        float baseDamage = skill.Damage;
        float damageWithStat = baseDamage * (1f + mainStat / 100f);

        // 2. 크리티컬 체크
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

    /// <summary>
    /// 스킬 쿨다운 시작
    /// </summary>
    private void StartCooldown(SkillData skill)
    {
        _skillCooldowns[skill] = skill.Cooldown;

        // UI 알림
        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot.SkillData == skill)
            {
                slot.StartCooldown();
            }
        }
    }

    /// <summary>
    /// 스킬 쿨다운 중인지 확인
    /// </summary>
    private bool IsSkillOnCooldown(SkillData skill)
    {
        if (_skillCooldowns.ContainsKey(skill))
        {
            return _skillCooldowns[skill] > 0f;
        }
        return false;
    }

    /// <summary>
    /// 모든 스킬의 쿨다운 시간 업데이트
    /// </summary>
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

    /// <summary>
    /// 마나 부족 경고 표시
    /// </summary>
    private void ShowManaCostWarning(SkillData skill)
    {
        // UI 피드백 (UI 시스템이 있으면 활성화)
        // UIManager.Instance.ShowWarning($"마나 부족! 필요: {skill.ManaCost}");

        // 스킬 슬롯에 붉은색 플래시 효과
        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot.SkillData == skill)
            {
                // slot.FlashRed();  // 구현 필요
            }
        }
    }

    #endregion

    #region 스킬 슬롯 관리

    /// <summary>
    /// 스킬 슬롯에 스킬 등록
    /// CharacterUIController에서 호출
    /// </summary>
    public void RegisterSkillSlot(SkillSlotUI slot)
    {
        if (!_skillSlots.Contains(slot))
        {
            _skillSlots.Add(slot);
        }
    }

    /// <summary>
    /// 스킬 슬롯 해제
    /// </summary>
    public void UnregisterSkillSlot(SkillSlotUI slot)
    {
        _skillSlots.Remove(slot);
    }

    #endregion

    #region 디버그

    /// <summary>
    /// 현재 상태 출력 (디버그용)
    /// </summary>
    [ContextMenu("Debug: Print Skill System Status")]
    private void DebugPrintStatus()
    {
        Debug.Log("===== SkillActivationSystem 상태 =====");
        Debug.Log($"등록된 스킬 슬롯: {_skillSlots.Count}개");
        Debug.Log($"쿨다운 추적 중: {_skillCooldowns.Count}개");
        Debug.Log($"활성 버프: {_activeBuffs.Count}개");

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

    #endregion
}
