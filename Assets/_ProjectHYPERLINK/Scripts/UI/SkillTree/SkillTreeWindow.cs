using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 스킬 트리 윈도우
/// 
/// 역할:
/// - 전체 스킬 트리 UI 관리
/// - 노드 UI 생성 및 배치
/// - 툴팁 표시
/// - SP 표시
/// - 연결선 생성
/// 
/// UI 구성:
/// - Grid Layout: 노드 배치
/// - Tooltip Panel: 노드 정보 표시
/// - SP Counter: 현재 SP 표시
/// - Connection Lines: 노드 간 연결선
/// 
/// </summary>
public class SkillTreeWindow : MonoBehaviour
{
    #region UI 참조
    [Header("자동 검색 설정")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _retryInterval = 0.5f;
    [SerializeField] private int _maxRetries = 20;

    private bool _isInitialized = false;
    private int _retryCount = 0;
    
    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject _windowRoot;
    [SerializeField] private Transform _nodeContainer;
    [SerializeField] private GameObject _nodeUIPrefab;
    [SerializeField] private Transform _connectionLineContainer;
    [SerializeField] private GameObject _connectionLinePrefab;

    [Header("툴팁")]
    [SerializeField] private GameObject _tooltipPanel;
    [SerializeField] private TextMeshProUGUI _tooltipTitle;
    [SerializeField] private TextMeshProUGUI _tooltipDescription;
    [SerializeField] private TextMeshProUGUI _tooltipStats;
    [SerializeField] private TextMeshProUGUI _tooltipRequirements;

    [Header("SP 표시")]
    [SerializeField] private TextMeshProUGUI _skillPointsText;

    #endregion

    #region 그리드 설정

    [Header("그리드 레이아웃")]
    [Tooltip("노드 간격 (X축)")]
    [SerializeField] private float _nodeSpacingX = 200f;

    [Tooltip("노드 간격 (Y축)")]
    [SerializeField] private float _nodeSpacingY = 150f;

    [Tooltip("그리드 시작 위치 (왼쪽 상단)")]
    [SerializeField] private Vector2 _gridStartPosition = new Vector2(-300f, 200f);

    #endregion

    #region 참조

    [Header("시스템 참조")]
    [SerializeField] private SkillTreeManager _skillTreeManager;

    #endregion

    #region 내부 상태

    private Dictionary<string, SkillTreeNodeUI> _nodeUIMap = new Dictionary<string, SkillTreeNodeUI>();
    private List<LineRenderer> _connectionLines = new List<LineRenderer>();

    #endregion

    #region 초기화

    private void Awake()
    {
        // 검증만
        if (_nodeUIPrefab == null)
            Debug.LogError("[SkillTreeWindow] NodeUI Prefab이 할당되지 않았습니다!");

        if (_tooltipPanel != null)
            _tooltipPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // 이벤트 구독
        SkillTreeManager.OnSkillPointsChanged += UpdateSkillPointsDisplay;
        SkillTreeManager.OnNodeUnlocked += OnNodeUnlockedHandler;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        SkillTreeManager.OnSkillPointsChanged -= UpdateSkillPointsDisplay;
        SkillTreeManager.OnNodeUnlocked -= OnNodeUnlockedHandler;
    }

    private void Start()
    {
        InvokeRepeating(nameof(TryFindSkillTreeManager), 0.1f, _retryInterval);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(TryFindSkillTreeManager));
        UnsubscribeFromEvents();
    }

    private void TryFindSkillTreeManager()
    {
        _retryCount++;

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _skillTreeManager = playerObject.GetComponent<SkillTreeManager>();

            if (_skillTreeManager != null)
            {
                Debug.Log($"[SkillTreeWindow] SkillTreeManager 찾음 (시도: {_retryCount}회)");

                SubscribeToEvents();
                InitializeSkillTree();
                _isInitialized = true;

                CancelInvoke(nameof(TryFindSkillTreeManager));
                return;
            }
        }

        if (_retryCount >= _maxRetries)
        {
            Debug.LogError($"[SkillTreeWindow] SkillTreeManager를 {_maxRetries}회 시도 후에도 찾지 못했습니다!");
            CancelInvoke(nameof(TryFindSkillTreeManager));
        }
    }

    private void SubscribeToEvents()
    {
        SkillTreeManager.OnSkillPointsChanged += UpdateSkillPointsDisplay;
        SkillTreeManager.OnNodeUnlocked += OnNodeUnlockedHandler;
    }

    private void UnsubscribeFromEvents()
    {
        SkillTreeManager.OnSkillPointsChanged -= UpdateSkillPointsDisplay;
        SkillTreeManager.OnNodeUnlocked -= OnNodeUnlockedHandler;
    }

    #endregion

    #region 스킬 트리 생성

    /// <summary>
    /// 스킬 트리 초기화
    /// </summary>
    public void InitializeSkillTree()
    {
        if (_skillTreeManager == null)
        {
            Debug.LogError("[SkillTreeWindow] SkillTreeManager가 없습니다!");
            return;
        }

        ClearAllNodes();
        CreateAllNodes();
        CreateConnectionLines();
        UpdateSkillPointsDisplay(_skillTreeManager.CurrentSkillPoints);

        Debug.Log($"[SkillTreeWindow] 스킬 트리 UI 생성 완료: {_nodeUIMap.Count}개 노드");
    }

    /// <summary>
    /// 모든 노드 생성
    /// </summary>
    private void CreateAllNodes()
    {
        List<SkillTreeNodeData> allNodes = _skillTreeManager.AllNodes;

        foreach (SkillTreeNodeData nodeData in allNodes)
        {
            CreateNodeUI(nodeData);
        }
    }

    /// <summary>
    /// 개별 노드 UI 생성
    /// </summary>
    private void CreateNodeUI(SkillTreeNodeData nodeData)
    {
        if (nodeData == null || _nodeUIPrefab == null || _nodeContainer == null)
            return;

        // 프리팹 인스턴스화
        GameObject nodeObj = Instantiate(_nodeUIPrefab, _nodeContainer);
        SkillTreeNodeUI nodeUI = nodeObj.GetComponent<SkillTreeNodeUI>();

        if (nodeUI == null)
        {
            Debug.LogError($"[SkillTreeWindow] {nodeData.NodeName}의 SkillTreeNodeUI 컴포넌트를 찾을 수 없습니다!");
            Destroy(nodeObj);
            return;
        }

        // 위치 설정 (그리드 기반)
        RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector2 position = CalculateNodePosition(nodeData.GridRow, nodeData.GridColumn);
            rectTransform.anchoredPosition = position;
        }

        // 초기화
        nodeUI.Initialize(nodeData, _skillTreeManager, this);

        // 맵에 추가
        _nodeUIMap[nodeData.NodeID] = nodeUI;
    }

    /// <summary>
    /// 노드 위치 계산 (그리드 기반)
    /// </summary>
    private Vector2 CalculateNodePosition(int row, int column)
    {
        float x = _gridStartPosition.x + (column * _nodeSpacingX);
        float y = _gridStartPosition.y - (row * _nodeSpacingY);
        return new Vector2(x, y);
    }

    /// <summary>
    /// 연결선 생성
    /// </summary>
    private void CreateConnectionLines()
    {
        if (_connectionLinePrefab == null || _connectionLineContainer == null)
            return;

        // 기존 연결선 제거
        ClearConnectionLines();

        // 각 노드에 대해 선행 노드와 연결선 생성
        foreach (SkillTreeNodeData nodeData in _skillTreeManager.AllNodes)
        {
            if (nodeData.RequiredNodeData != null)
            {
                CreateConnectionLine(nodeData, nodeData.RequiredNodeData);
            }
        }
    }

    /// <summary>
    /// 개별 연결선 생성
    /// </summary>
    private void CreateConnectionLine(SkillTreeNodeData fromNode, SkillTreeNodeData toNode)
    {
        if (!_nodeUIMap.ContainsKey(fromNode.NodeID) || !_nodeUIMap.ContainsKey(toNode.NodeID))
            return;

        GameObject lineObj = Instantiate(_connectionLinePrefab, _connectionLineContainer);
        LineRenderer lineRenderer = lineObj.GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            Debug.LogError("[SkillTreeWindow] LineRenderer 컴포넌트가 없습니다!");
            Destroy(lineObj);
            return;
        }

        // 연결선 그리기
        SkillTreeNodeUI fromNodeUI = _nodeUIMap[fromNode.NodeID];
        SkillTreeNodeUI toNodeUI = _nodeUIMap[toNode.NodeID];
        fromNodeUI.DrawConnectionToParent(toNodeUI, lineRenderer);

        _connectionLines.Add(lineRenderer);
    }

    /// <summary>
    /// 모든 노드 제거
    /// </summary>
    private void ClearAllNodes()
    {
        foreach (SkillTreeNodeUI nodeUI in _nodeUIMap.Values)
        {
            if (nodeUI != null)
                Destroy(nodeUI.gameObject);
        }
        _nodeUIMap.Clear();
    }

    /// <summary>
    /// 모든 연결선 제거
    /// </summary>
    private void ClearConnectionLines()
    {
        foreach (LineRenderer line in _connectionLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        _connectionLines.Clear();
    }

    #endregion

    #region 툴팁

    /// <summary>
    /// 툴팁 표시
    /// </summary>
    public void ShowTooltip(SkillTreeNodeData nodeData, SkillTreeNodeUI nodeUI)
    {
        if (_tooltipPanel == null || nodeData == null)
            return;

        _tooltipPanel.SetActive(true);

        // 제목
        if (_tooltipTitle != null)
        {
            string nodeType = nodeData.IsPassive ? "[패시브]" : "[액티브]";
            _tooltipTitle.text = $"{nodeType} {nodeData.NodeName}";
        }

        // 설명
        if (_tooltipDescription != null)
        {
            _tooltipDescription.text = nodeData.Description;
        }

        // 스탯 정보
        if (_tooltipStats != null)
        {
            _tooltipStats.text = GetStatsText(nodeData);
        }

        // 필요 조건
        if (_tooltipRequirements != null)
        {
            _tooltipRequirements.text = GetRequirementsText(nodeData);
        }

        // 툴팁 위치 설정 (노드 옆)
        PositionTooltip(nodeUI);
    }

    /// <summary>
    /// 툴팁 숨기기
    /// </summary>
    public void HideTooltip()
    {
        if (_tooltipPanel != null)
            _tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// 스탯 텍스트 생성
    /// </summary>
    private string GetStatsText(SkillTreeNodeData nodeData)
    {
        if (nodeData.IsPassive)
        {
            if (nodeData.PassiveStatBonuses == null || nodeData.PassiveStatBonuses.Count == 0)
                return "스탯 보너스 없음";

            string statsText = "<b>스탯 보너스:</b>\n";
            foreach (PassiveStatBonus bonus in nodeData.PassiveStatBonuses)
            {
                statsText += $"  +{bonus.Value} {GetStatTypeName(bonus.StatType)}\n";
            }
            return statsText;
        }
        else if (nodeData.SkillData != null)
        {
            SkillData skill = nodeData.SkillData;
            return $"<b>스킬 정보:</b>\n" +
                   $"  마나: {skill.ManaCost}\n" +
                   $"  쿨다운: {skill.Cooldown}초\n" +
                   $"  사거리: {skill.Range}m";
        }

        return "";
    }

    /// <summary>
    /// 필요 조건 텍스트 생성
    /// </summary>
    private string GetRequirementsText(SkillTreeNodeData nodeData)
    {
        string reqText = "<b>필요 조건:</b>\n";
        reqText += $"  스킬 포인트: {nodeData.RequiredSkillPoints}\n";
        reqText += $"  레벨: {nodeData.RequiredLevel}\n";

        if (nodeData.RequiredNodeData != null)
        {
            reqText += $"  선행 스킬: {nodeData.RequiredNodeData.NodeName}\n";
        }

        // 언락 가능 여부 확인
        if (_skillTreeManager.CanUnlockNode(nodeData, out string failReason))
        {
            reqText += "\n<color=green>언락 가능!</color>";
        }
        else if (!_skillTreeManager.IsNodeUnlocked(nodeData.NodeID))
        {
            reqText += $"\n<color=red>{failReason}</color>";
        }
        else
        {
            reqText += "\n<color=green>언락됨</color>";
        }

        return reqText;
    }

    /// <summary>
    /// 스탯 타입 이름 반환
    /// </summary>
    private string GetStatTypeName(PassiveStatType statType)
    {
        switch (statType)
        {
            case PassiveStatType.Strength: return "힘";
            case PassiveStatType.Dexterity: return "민첩";
            case PassiveStatType.Intelligence: return "지능";
            case PassiveStatType.Vitality: return "활력";
            case PassiveStatType.PhysicalAttack: return "물리 공격력";
            case PassiveStatType.MagicalAttack: return "마법 공격력";
            case PassiveStatType.Armor: return "방어력";
            case PassiveStatType.AllResistance: return "모든 저항";
            case PassiveStatType.CriticalChance: return "치명타 확률";
            case PassiveStatType.CriticalDamage: return "치명타 데미지";
            case PassiveStatType.AttackSpeed: return "공격 속도";
            case PassiveStatType.MovementSpeed: return "이동 속도";
            case PassiveStatType.MaxHealth: return "최대 체력";
            case PassiveStatType.MaxMana: return "최대 마나";
            case PassiveStatType.HealthRegeneration: return "체력 재생";
            case PassiveStatType.ManaRegeneration: return "마나 재생";
            default: return statType.ToString();
        }
    }

    /// <summary>
    /// 툴팁 위치 설정
    /// </summary>
    private void PositionTooltip(SkillTreeNodeUI nodeUI)
    {
        if (_tooltipPanel == null || nodeUI == null)
            return;

        RectTransform tooltipRect = _tooltipPanel.GetComponent<RectTransform>();
        RectTransform nodeRect = nodeUI.GetComponent<RectTransform>();

        if (tooltipRect != null && nodeRect != null)
        {
            // 노드 오른쪽에 툴팁 표시
            Vector2 position = nodeRect.anchoredPosition;
            position.x += 700f; // 노드 너비 + 여백
            position.y -= 350f; // 노드 너비 + 여백
            tooltipRect.anchoredPosition = position;
        }
    }

    #endregion

    #region SP 표시

    /// <summary>
    /// SP 표시 업데이트
    /// </summary>
    private void UpdateSkillPointsDisplay(int currentSP)
    {
        if (_skillPointsText != null)
        {
            _skillPointsText.text = $"스킬 포인트: {currentSP}";
        }
    }

    #endregion

    #region 노드 상태 갱신

    /// <summary>
    /// 노드 언락 이벤트 핸들러
    /// </summary>
    private void OnNodeUnlockedHandler(SkillTreeNodeData nodeData)
    {
        // 모든 노드 UI 상태 갱신 (언락 가능 여부 변경될 수 있음)
        RefreshAllNodes();

        // 연결선 색상 업데이트
        UpdateConnectionLines();
    }

    /// <summary>
    /// 모든 노드 UI 갱신
    /// </summary>
    public void RefreshAllNodes()
    {
        foreach (SkillTreeNodeUI nodeUI in _nodeUIMap.Values)
        {
            if (nodeUI != null)
                nodeUI.RefreshState();
        }
    }

    /// <summary>
    /// 연결선 색상 업데이트
    /// </summary>
    private void UpdateConnectionLines()
    {
        // 연결선 재생성 (색상 반영)
        CreateConnectionLines();
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug: Refresh All Nodes")]
    private void DebugRefreshAll()
    {
        RefreshAllNodes();
        Debug.Log("[DEBUG] 모든 노드 갱신 완료");
    }

    #endregion
}
