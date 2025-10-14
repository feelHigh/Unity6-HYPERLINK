using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// 스킬 활성화 및 쿨다운 관리 시스템 (PlayerCharacter 리팩토링 반영)
/// 
/// 주요 변경사항:
/// - PlayerCharacter.ConsumeMana() → bool 반환값 처리
/// - PlayerCharacter.RestoreHealth() → Heal() 변경
/// - PlayerCharacter.GetMainStat() 사용
/// 
/// 핵심 기능:
/// - 키보드 입력으로 스킬 활성화 (Q, W, E)
/// - 스킬 쿨다운 관리
/// - 마나 소비 체크
/// - 스킬 타입별 실행 로직
/// - 데미지 계산 (크리티컬 포함)
/// - UI 쿨다운 동기화
/// </summary>
public class SkillActivationSystem : MonoBehaviour
{
    [Header("스킬 설정")]
    [SerializeField]
    private KeyCode[] _skillKeys = new KeyCode[]
    {
        KeyCode.Q,  // 첫 번째 스킬
        KeyCode.W,  // 두 번째 스킬
        KeyCode.E   // 세 번째 스킬
    };

    [Header("비주얼 이펙트")]
    [SerializeField] private GameObject _skillCastEffect;
    [SerializeField] private Transform _castPoint;

    private PlayerCharacter _playerCharacter;
    private Dictionary<SkillData, float> _skillCooldowns = new Dictionary<SkillData, float>();
    private List<SkillSlotUI> _skillSlots = new List<SkillSlotUI>();

    public static event Action<SkillData> OnSkillActivated;
    public static event Action<SkillData> OnSkillFailed;

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacter>();
    }

    private void Update()
    {
        HandleSkillInput();
        UpdateCooldowns();
    }

    /// <summary>
    /// 스킬 슬롯 UI 등록
    /// CharacterUIController에서 초기화 시 호출
    /// </summary>
    public void RegisterSkillSlot(SkillSlotUI skillSlot)
    {
        if (!_skillSlots.Contains(skillSlot))
        {
            _skillSlots.Add(skillSlot);
        }
    }

    public void UnregisterSkillSlot(SkillSlotUI skillSlot)
    {
        _skillSlots.Remove(skillSlot);
    }

    /// <summary>
    /// 키보드 입력 감지 및 스킬 활성화
    /// </summary>
    private void HandleSkillInput()
    {
        if (_playerCharacter == null || !_playerCharacter.IsAlive)
            return;

        for (int i = 0; i < _skillKeys.Length; i++)
        {
            if (Input.GetKeyDown(_skillKeys[i]))
            {
                TryActivateSkillAtIndex(i);
            }
        }
    }

    /// <summary>
    /// 특정 인덱스의 스킬 활성화 시도
    /// </summary>
    private void TryActivateSkillAtIndex(int index)
    {
        if (index >= _playerCharacter.UnlockedSkills.Count)
            return;

        SkillData skill = _playerCharacter.UnlockedSkills[index];
        ActivateSkill(skill);
    }

    /// <summary>
    /// 스킬 활성화 메인 로직
    /// 
    /// 실행 조건 체크:
    /// 1. 스킬이 언락되어 있는가?
    /// 2. 스킬이 쿨다운 중인가?
    /// 3. 마나가 충분한가?
    /// </summary>
    public bool ActivateSkill(SkillData skill)
    {
        if (skill == null)
            return false;

        // 1. 언락 체크
        if (!_playerCharacter.UnlockedSkills.Contains(skill))
        {
            Debug.Log($"스킬 {skill.SkillName}이(가) 아직 언락되지 않았습니다");
            OnSkillFailed?.Invoke(skill);
            return false;
        }

        // 2. 쿨다운 체크
        if (IsSkillOnCooldown(skill))
        {
            Debug.Log($"스킬 {skill.SkillName}이(가) 쿨다운 중입니다");
            OnSkillFailed?.Invoke(skill);
            return false;
        }

        // 3. 마나 체크 및 소비
        if (!_playerCharacter.ConsumeMana(skill.ManaCost))
        {
            Debug.Log($"스킬 {skill.SkillName}을(를) 사용할 마나가 부족합니다");
            OnSkillFailed?.Invoke(skill);
            return false;
        }

        // 모든 조건 통과 - 스킬 실행
        ExecuteSkill(skill);
        StartCooldown(skill);

        OnSkillActivated?.Invoke(skill);
        return true;
    }

    /// <summary>
    /// 스킬 타입별 실행 로직 분기
    /// </summary>
    private void ExecuteSkill(SkillData skill)
    {
        Debug.Log($"스킬 활성화: {skill.SkillName}");

        // 시전 이펙트
        if (_skillCastEffect != null && _castPoint != null)
        {
            GameObject effect = Instantiate(_skillCastEffect, _castPoint.position, _castPoint.rotation);
            Destroy(effect, 2f);
        }

        // 스킬 타입별 실행
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

    /// <summary>
    /// 근거리 스킬 실행
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
    /// TODO: 투사체 시스템 구현
    /// </summary>
    private void ExecuteRangedSkill(SkillData skill)
    {
        Debug.Log($"원거리 스킬 실행: {skill.SkillName}");
        // TODO: 투사체 스폰
    }

    /// <summary>
    /// 광역 스킬 실행
    /// </summary>
    private void ExecuteAOESkill(SkillData skill)
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
    /// 버프 스킬 실행
    /// TODO: 버프 시스템 구현
    /// </summary>
    private void ExecuteBuffSkill(SkillData skill)
    {
        Debug.Log($"버프 스킬 실행: {skill.SkillName}");
        // TODO: 버프 적용
    }

    /// <summary>
    /// 힐 스킬 실행
    /// 
    /// 변경사항: RestoreHealth() → Heal()
    /// </summary>
    private void ExecuteHealSkill(SkillData skill)
    {
        _playerCharacter.Heal(skill.Damage);
    }

    /// <summary>
    /// 스킬 데미지 계산 (크리티컬 포함)
    /// 
    /// 계산 공식:
    /// 1. 기본 데미지 = skill.Damage
    /// 2. 스탯 보너스 = 기본 데미지 × (1 + 주요스탯/100)
    /// 3. 크리티컬 체크
    /// 4. 크리티컬이면 데미지 × (1 + 크리티컬 데미지/100)
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
            Debug.Log("크리티컬 히트!");
        }

        return damageWithStat;
    }

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
}