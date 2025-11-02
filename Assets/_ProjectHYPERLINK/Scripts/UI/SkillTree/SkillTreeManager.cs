using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 스킬 트리 매니저
/// 
/// 역할:
/// - 스킬 포인트 (SP) 관리
/// - 노드 언락/잠금 상태 관리
/// - 선행 조건 검증
/// - 패시브 스킬 효과 적용
/// 
/// 의존성:
/// - ExperienceManager: 레벨업 시 SP 획득
/// - PlayerCharacter: 패시브 스탯 적용
/// 
/// 이벤트:
/// - OnSkillPointsChanged: SP 변경 시
/// - OnNodeUnlocked: 노드 언락 시
/// - OnNodeStateChanged: 노드 상태 변경 시
/// </summary>
public class SkillTreeManager : MonoBehaviour
{
    #region 설정

    [Header("스킬 트리 설정")]
    [Tooltip("전체 스킬 트리 노드 목록")]
    [SerializeField] private List<SkillTreeNodeData> _allNodes = new List<SkillTreeNodeData>();

    [Tooltip("시작 스킬 포인트")]
    [SerializeField] private int _startingSkillPoints = 0;

    [Tooltip("레벨당 획득 스킬 포인트")]
    [SerializeField] private int _skillPointsPerLevel = 1;

    #endregion

    #region 참조

    [Header("시스템 참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;
    [SerializeField] private ExperienceManager _experienceManager;

    #endregion

    #region 내부 상태

    // 현재 스킬 포인트
    private int _currentSkillPoints;

    // 언락된 노드 목록 (NodeID를 키로 사용)
    private HashSet<string> _unlockedNodes = new HashSet<string>();

    // 활성화된 패시브 효과들
    private CharacterStats _totalPassiveStats;

    #endregion

    #region 이벤트

    public static event Action<int> OnSkillPointsChanged;
    public static event Action<SkillTreeNodeData> OnNodeUnlocked;
    public static event Action<SkillTreeNodeData> OnNodeStateChanged;

    #endregion

    #region 프로퍼티

    public int CurrentSkillPoints => _currentSkillPoints;
    public List<SkillTreeNodeData> AllNodes => _allNodes;
    public bool IsNodeUnlocked(string nodeID) => _unlockedNodes.Contains(nodeID);

    #endregion

    #region 초기화

    private void Awake()
    {
        // 자동 참조
        if (_playerCharacter == null)
            _playerCharacter = GetComponent<PlayerCharacter>();

        if (_experienceManager == null)
            _experienceManager = GetComponent<ExperienceManager>();

        // 검증
        if (_playerCharacter == null)
            Debug.LogError("[SkillTreeManager] PlayerCharacter를 찾을 수 없습니다!");

        if (_experienceManager == null)
            Debug.LogError("[SkillTreeManager] ExperienceManager를 찾을 수 없습니다!");

        // 패시브 스탯 초기화
        _totalPassiveStats = ScriptableObject.CreateInstance<CharacterStats>();
    }

    private void OnEnable()
    {
        // 레벨업 이벤트 구독
        ExperienceManager.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        ExperienceManager.OnLevelUp -= HandleLevelUp;
    }

    private void Start()
    {
        InitializeSkillTree();
    }

    /// <summary>
    /// 스킬 트리 초기화
    /// </summary>
    private void InitializeSkillTree()
    {
        _currentSkillPoints = _startingSkillPoints;
        _unlockedNodes.Clear();

        Debug.Log($"[SkillTreeManager] 초기화 완료 - SP: {_currentSkillPoints}, 노드: {_allNodes.Count}개");

        OnSkillPointsChanged?.Invoke(_currentSkillPoints);
    }

    #endregion

    #region 스킬 포인트 관리

    /// <summary>
    /// 레벨업 시 SP 획득
    /// </summary>
    private void HandleLevelUp(int oldLevel, int newLevel)
    {
        int levelsGained = newLevel - oldLevel;
        int spGained = levelsGained * _skillPointsPerLevel;

        AddSkillPoints(spGained);

        Debug.Log($"[SkillTreeManager] 레벨업 ({oldLevel} → {newLevel}): +{spGained} SP");
    }

    /// <summary>
    /// SP 추가
    /// </summary>
    public void AddSkillPoints(int amount)
    {
        _currentSkillPoints += amount;
        OnSkillPointsChanged?.Invoke(_currentSkillPoints);
    }

    /// <summary>
    /// SP 소비
    /// </summary>
    private bool SpendSkillPoints(int amount)
    {
        if (_currentSkillPoints >= amount)
        {
            _currentSkillPoints -= amount;
            OnSkillPointsChanged?.Invoke(_currentSkillPoints);
            return true;
        }

        return false;
    }

    #endregion

    #region 노드 언락

    /// <summary>
    /// 노드 언락 시도
    /// 
    /// 반환값:
    /// - true: 언락 성공
    /// - false: 언락 실패 (조건 미충족)
    /// </summary>
    public bool TryUnlockNode(SkillTreeNodeData nodeData)
    {
        if (nodeData == null)
        {
            Debug.LogWarning("[SkillTreeManager] nodeData가 null입니다!");
            return false;
        }

        // 이미 언락됨
        if (IsNodeUnlocked(nodeData.NodeID))
        {
            Debug.Log($"[SkillTreeManager] {nodeData.NodeName}은(는) 이미 언락되었습니다.");
            return false;
        }

        // 조건 검증
        if (!CanUnlockNode(nodeData, out string failReason))
        {
            Debug.Log($"[SkillTreeManager] {nodeData.NodeName} 언락 실패: {failReason}");
            return false;
        }

        // SP 소비
        if (!SpendSkillPoints(nodeData.RequiredSkillPoints))
        {
            Debug.LogWarning("[SkillTreeManager] SP 부족!");
            return false;
        }

        // 노드 언락
        UnlockNode(nodeData);
        return true;
    }

    /// <summary>
    /// 노드 언락 가능 여부 확인
    /// </summary>
    public bool CanUnlockNode(SkillTreeNodeData nodeData, out string failReason)
    {
        failReason = "";

        if (nodeData == null)
        {
            failReason = "노드 데이터가 없습니다.";
            return false;
        }

        // 이미 언락됨
        if (IsNodeUnlocked(nodeData.NodeID))
        {
            failReason = "이미 언락된 노드입니다.";
            return false;
        }

        // SP 확인
        if (_currentSkillPoints < nodeData.RequiredSkillPoints)
        {
            failReason = $"스킬 포인트 부족 ({_currentSkillPoints}/{nodeData.RequiredSkillPoints})";
            return false;
        }

        // 레벨 확인
        if (_experienceManager != null && _experienceManager.CurrentLevel < nodeData.RequiredLevel)
        {
            failReason = $"필요 레벨 {nodeData.RequiredLevel} (현재: {_experienceManager.CurrentLevel})";
            return false;
        }

        // 선행 노드 확인
        if (nodeData.RequiredNodeData != null)
        {
            if (!IsNodeUnlocked(nodeData.RequiredNodeData.NodeID))
            {
                failReason = $"선행 스킬 필요: {nodeData.RequiredNodeData.NodeName}";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 노드 언락 실행
    /// </summary>
    private void UnlockNode(SkillTreeNodeData nodeData)
    {
        _unlockedNodes.Add(nodeData.NodeID);

        // 패시브 스킬: 스탯 적용
        if (nodeData.IsPassive)
        {
            ApplyPassiveStats(nodeData);
        }
        // 액티브 스킬: PlayerCharacter에 스킬 추가
        else if (nodeData.SkillData != null)
        {
            _playerCharacter?.UnlockSkill(nodeData.SkillData);
        }

        Debug.Log($"[SkillTreeManager] {nodeData.NodeName} 언락 완료!");

        OnNodeUnlocked?.Invoke(nodeData);
        OnNodeStateChanged?.Invoke(nodeData);
    }

    #endregion

    #region 패시브 스킬 관리

    /// <summary>
    /// 패시브 스탯 적용
    /// </summary>
    private void ApplyPassiveStats(SkillTreeNodeData nodeData)
    {
        if (nodeData.PassiveStatBonuses == null || nodeData.PassiveStatBonuses.Count == 0)
        {
            Debug.LogWarning($"[SkillTreeManager] {nodeData.NodeName}에 패시브 보너스가 없습니다!");
            return;
        }

        // Builder로 패시브 스탯 생성
        CharacterStatsBuilder builder = new CharacterStatsBuilder();

        foreach (PassiveStatBonus bonus in nodeData.PassiveStatBonuses)
        {
            ApplyPassiveBonus(builder, bonus);
        }

        CharacterStats passiveStats = builder.Build();

        // 전체 패시브 스탯에 누적
        _totalPassiveStats = _totalPassiveStats.AddStats(passiveStats);

        // PlayerCharacter에 적용
        if (_playerCharacter != null)
        {
            // 기존 패시브 제거 후 새로 적용 (중복 방지)
            _playerCharacter.RemoveAllPassiveStats();
            _playerCharacter.AddTemporaryStats(_totalPassiveStats);
        }

        Debug.Log($"[SkillTreeManager] 패시브 스탯 적용: {nodeData.NodeName}");
    }

    /// <summary>
    /// 개별 패시브 보너스를 Builder에 추가
    /// </summary>
    private void ApplyPassiveBonus(CharacterStatsBuilder builder, PassiveStatBonus bonus)
    {
        switch (bonus.StatType)
        {
            case PassiveStatType.Strength:
                builder.AddStrength((int)bonus.Value);
                break;
            case PassiveStatType.Dexterity:
                builder.AddDexterity((int)bonus.Value);
                break;
            case PassiveStatType.Intelligence:
                builder.AddIntelligence((int)bonus.Value);
                break;
            case PassiveStatType.Vitality:
                builder.AddVitality((int)bonus.Value);
                break;
            case PassiveStatType.PhysicalAttack:
                builder.AddPhysicalAttack(bonus.Value);
                break;
            case PassiveStatType.MagicalAttack:
                builder.AddMagicalAttack(bonus.Value);
                break;
            case PassiveStatType.Armor:
                builder.AddArmor(bonus.Value);
                break;
            case PassiveStatType.AllResistance:
                builder.AddAllResistance(bonus.Value);
                break;
            case PassiveStatType.CriticalChance:
                builder.AddCriticalChance(bonus.Value);
                break;
            case PassiveStatType.CriticalDamage:
                builder.AddCriticalDamage(bonus.Value);
                break;
            case PassiveStatType.AttackSpeed:
                builder.AddAttackSpeed(bonus.Value);
                break;
            case PassiveStatType.MovementSpeed:
                builder.AddMovementSpeed(bonus.Value);
                break;
            case PassiveStatType.MaxHealth:
                builder.AddMaxHealth(bonus.Value);
                break;
            case PassiveStatType.MaxMana:
                builder.AddMaxMana(bonus.Value);
                break;
            case PassiveStatType.HealthRegeneration:
                builder.AddHealthRegeneration(bonus.Value);
                break;
            case PassiveStatType.ManaRegeneration:
                builder.AddManaRegeneration(bonus.Value);
                break;
        }
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 특정 티어의 모든 노드 가져오기
    /// </summary>
    public List<SkillTreeNodeData> GetNodesByTier(int tier)
    {
        return _allNodes.Where(node => node.Tier == tier).ToList();
    }

    /// <summary>
    /// 노드 ID로 노드 찾기
    /// </summary>
    public SkillTreeNodeData GetNodeByID(string nodeID)
    {
        return _allNodes.FirstOrDefault(node => node.NodeID == nodeID);
    }

    /// <summary>
    /// 언락된 노드 수
    /// </summary>
    public int GetUnlockedNodeCount()
    {
        return _unlockedNodes.Count;
    }

    #endregion

    #region 세이브/로드

    /// <summary>
    /// 스킬 트리 상태 저장
    /// </summary>
    public SkillTreeSaveData SaveSkillTree()
    {
        return new SkillTreeSaveData
        {
            currentSkillPoints = _currentSkillPoints,
            unlockedNodeIDs = new List<string>(_unlockedNodes)
        };
    }

    /// <summary>
    /// 스킬 트리 상태 로드
    /// </summary>
    public void LoadSkillTree(SkillTreeSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("[SkillTreeManager] 로드할 데이터가 없습니다!");
            return;
        }

        _currentSkillPoints = saveData.currentSkillPoints;
        _unlockedNodes = new HashSet<string>(saveData.unlockedNodeIDs);

        // 언락된 노드들의 효과 재적용
        RecalculateAllPassiveStats();

        Debug.Log($"[SkillTreeManager] 스킬 트리 로드 완료 - SP: {_currentSkillPoints}, 언락: {_unlockedNodes.Count}개");

        OnSkillPointsChanged?.Invoke(_currentSkillPoints);
    }

    /// <summary>
    /// 모든 패시브 스탯 재계산
    /// </summary>
    private void RecalculateAllPassiveStats()
    {
        _totalPassiveStats = ScriptableObject.CreateInstance<CharacterStats>();

        foreach (string nodeID in _unlockedNodes)
        {
            SkillTreeNodeData nodeData = GetNodeByID(nodeID);
            if (nodeData != null && nodeData.IsPassive)
            {
                CharacterStatsBuilder builder = new CharacterStatsBuilder();
                foreach (PassiveStatBonus bonus in nodeData.PassiveStatBonuses)
                {
                    ApplyPassiveBonus(builder, bonus);
                }
                _totalPassiveStats = _totalPassiveStats.AddStats(builder.Build());
            }
        }

        // PlayerCharacter에 재적용
        if (_playerCharacter != null)
        {
            _playerCharacter.RemoveAllPassiveStats();
            _playerCharacter.AddTemporaryStats(_totalPassiveStats);
        }
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug: Print Skill Tree State")]
    private void DebugPrintState()
    {
        Debug.Log("===== 스킬 트리 상태 =====");
        Debug.Log($"현재 SP: {_currentSkillPoints}");
        Debug.Log($"총 노드: {_allNodes.Count}개");
        Debug.Log($"언락 노드: {_unlockedNodes.Count}개");

        Debug.Log("--- 언락된 노드 ---");
        foreach (string nodeID in _unlockedNodes)
        {
            SkillTreeNodeData node = GetNodeByID(nodeID);
            if (node != null)
            {
                Debug.Log($"  - {node.NodeName} (Tier {node.Tier}, {(node.IsPassive ? "패시브" : "액티브")})");
            }
        }
    }

    [ContextMenu("Debug: Add 5 Skill Points")]
    private void DebugAddSkillPoints()
    {
        AddSkillPoints(5);
        Debug.Log($"[DEBUG] +5 SP 추가 (현재: {_currentSkillPoints})");
    }

    #endregion
}
