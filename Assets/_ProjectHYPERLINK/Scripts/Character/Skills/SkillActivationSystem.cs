using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 스킬 활성화 시스템
/// 
/// 핵심 기능:
/// - 키 바인드 기반 스킬 활성화 (Q/W/E 설정 가능)
/// - ExecuteRangedSkill(): 투사체 발사 및 추적
/// - ExecuteAOESkill(): 광역 공격 실행
/// - ExecuteBuffSkill(): 임시 스탯 증가
/// - ApplyTemporaryBuff(): 버프 지속시간 관리
/// 
/// 최근 변경사항:
/// - 키 바인드 시스템 추가 (설정 가능)
/// - AOE 스킬 실행 로직 구현
/// - 디버그 기능 강화
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

    [Header("AOE 스킬 설정")]
    [Tooltip("AOE 스킬 이펙트 프리팹 (선택 사항)")]
    [SerializeField] private GameObject _aoeEffectPrefab;

    [Tooltip("AOE 이펙트 지속 시간")]
    [SerializeField] private float _aoeEffectDuration = 2f;

    [Header("디버그 설정")]
    [SerializeField] private bool _showDebugGizmos = true;

    // 스킬 쿨다운 추적
    private Dictionary<SkillData, float> _skillCooldowns = new Dictionary<SkillData, float>();

    // 현재 활성화된 버프 목록
    private Dictionary<SkillData, Coroutine> _activeBuffs = new Dictionary<SkillData, Coroutine>();

    // 키 바인드 목록 (인덱스로 접근)
    private KeyCode[] _skillKeys;

    #region 초기화

    private void Awake()
    {
        // 자동 검색 추가
        if (_playerCharacter == null)
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
            if (_playerCharacter == null)
            {
                Debug.LogError("[SkillActivationSystem] PlayerCharacter를 찾을 수 없습니다!");
            }
        }

        // 키 바인드 배열 초기화
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
    /// 
    /// 각 스킬 슬롯에 할당된 키를 확인하여
    /// 해당 스킬을 활성화합니다.
    /// 
    /// 기본 키:
    /// - Q: 첫 번째 스킬
    /// - W: 두 번째 스킬
    /// - E: 세 번째 스킬
    /// </summary>
    private void HandleSkillInput()
    {
        // 각 스킬 슬롯에 대해 키 입력 확인
        for (int i = 0; i < _skillSlots.Count && i < _skillKeys.Length; i++)
        {
            if (Input.GetKeyDown(_skillKeys[i]))
            {
                SkillSlotUI slot = _skillSlots[i];

                // 슬롯이 유효하고 스킬이 할당되어 있는지 확인
                if (slot != null && slot.SkillData != null && !slot.IsLocked)
                {
                    ActivateSkill(slot.SkillData);
                }
            }
        }
    }

    /// <summary>
    /// 특정 슬롯의 키 바인드 가져오기
    /// SkillSlotUI에서 키 표시용으로 사용
    /// </summary>
    public KeyCode GetSkillKey(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _skillKeys.Length)
        {
            return _skillKeys[slotIndex];
        }
        return KeyCode.None;
    }

    /// <summary>
    /// 특정 슬롯의 키 바인드 변경 (런타임)
    /// 향후 키 설정 UI에서 사용 가능
    /// </summary>
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
        int enemyCount = 0;

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                float damage = CalculateSkillDamage(skill);
                enemy.TakeDamage(damage);
                enemyCount++;
            }
        }

        Debug.Log($"[{skill.SkillName}] {enemyCount}명의 적에게 타격!");
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
    /// </summary>
    private void ExecuteRangedSkill(SkillData skill)
    {
        // 투사체 프리팹 확인
        if (skill.ProjectilePrefab == null)
        {
            Debug.LogError($"[{skill.SkillName}] 투사체 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 발사 위치 계산 (플레이어 앞 1유닛, 높이 1.5유닛)
        Vector3 spawnOffset = transform.forward * 1f + Vector3.up * 1.5f;
        Vector3 spawnPosition = transform.position + spawnOffset;

        // 투사체 생성
        GameObject projectileObj = Instantiate(
            skill.ProjectilePrefab,
            spawnPosition,
            transform.rotation
        );

        // Projectile 컴포넌트 초기화
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
    /// 광역 스킬 실행 (신규 구현)
    /// 
    /// 처리 과정:
    /// 1. 플레이어 중심 범위 내 모든 적 탐색
    /// 2. 각 적에게 계산된 데미지 적용
    /// 3. 선택적으로 AOE 이펙트 생성
    /// 
    /// Melee와의 차이점:
    /// - AOE는 더 넓은 범위 (일반적으로 Range가 더 큼)
    /// - 시각적 이펙트 생성
    /// - 더 높은 마나 소비/쿨다운
    /// 
    /// 사용 예:
    /// - 폭발 마법
    /// - 지진
    /// - 회오리바람
    /// </summary>
    private void ExecuteAOESkill(SkillData skill)
    {
        Vector3 centerPosition = transform.position;
        Collider[] hits = Physics.OverlapSphere(centerPosition, skill.Range);
        int enemyCount = 0;

        // 범위 내 모든 적에게 데미지 적용
        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                float damage = CalculateSkillDamage(skill);
                enemy.TakeDamage(damage);
                enemyCount++;
            }
        }

        // AOE 이펙트 생성 (프리팹이 설정된 경우)
        if (_aoeEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(_aoeEffectPrefab, centerPosition, Quaternion.identity);

            // 범위에 맞게 스케일 조정
            effectObj.transform.localScale = Vector3.one * (skill.Range * 2f);

            // 일정 시간 후 자동 삭제
            Destroy(effectObj, _aoeEffectDuration);
        }

        Debug.Log($"[{skill.SkillName}] 광역 공격! {enemyCount}명의 적 타격, 범위: {skill.Range}m");
    }

    /// <summary>
    /// 버프 스킬 실행
    /// 
    /// 처리:
    /// - 이미 활성화된 버프가 있으면 중복 방지
    /// - Coroutine으로 버프 지속시간 관리
    /// </summary>
    private void ExecuteBuffSkill(SkillData skill)
    {
        // 중복 버프 방지
        if (_activeBuffs.ContainsKey(skill))
        {
            Debug.Log($"[{skill.SkillName}] 이미 활성화된 버프입니다!");
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
        float healAmount = skill.Damage; // 힐 스킬은 Damage 값을 회복량으로 사용
        _playerCharacter.Heal(healAmount);
        Debug.Log($"[{skill.SkillName}] 체력 회복: {healAmount}");
    }

    /// <summary>
    /// 임시 버프 적용 (Coroutine)
    /// 
    /// 처리 순서:
    /// 1. 버프용 CharacterStats 생성
    /// 2. PlayerCharacter에 스탯 추가
    /// 3. 지속시간 대기
    /// 4. 스탯 제거
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
    /// 직업별 스탯 증가:
    /// - Laon: Strength
    /// - Sian: Intelligence
    /// - Yujin: Dexterity
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
        Debug.Log($"마나 부족! 현재: {_playerCharacter.CurrentMana:F0} / 필요: {skill.ManaCost}");

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

        // 키 바인드 정보
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

    /// <summary>
    /// Scene 뷰에서 스킬 범위 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!_showDebugGizmos)
            return;

        // 각 스킬의 범위를 다른 색상으로 표시
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
