using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 스킬 트리 노드 UI
/// 
/// 역할:
/// - 개별 노드 시각화 (아이콘, 상태)
/// - 노드 클릭 처리
/// - 툴팁 표시
/// - 언락 가능/불가능 상태 표시
/// 
/// UI 구성:
/// - Icon: 스킬 아이콘
/// - LockedOverlay: 잠금 상태 오버레이
/// - UnlockableHighlight: 언락 가능할 때 하이라이트
/// - ConnectionLine: 부모 노드와의 연결선
/// </summary>
public class SkillTreeNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    #region UI 참조

    [Header("UI 컴포넌트")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _lockedOverlay;
    [SerializeField] private GameObject _unlockableHighlight;
    [SerializeField] private TextMeshProUGUI _costText;

    #endregion

    #region 색상 설정

    [Header("색상 설정")]
    [SerializeField] private Color _lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color _unlockedColor = Color.white;
    [SerializeField] private Color _unlockableColor = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private Color _activeNodeColor = new Color(0.5f, 1f, 0.5f, 1f);
    [SerializeField] private Color _passiveNodeColor = new Color(0.5f, 0.7f, 1f, 1f);

    #endregion

    #region 참조

    [Header("시스템 참조")]
    [SerializeField] private SkillTreeManager _skillTreeManager;
    [SerializeField] private SkillTreeWindow _skillTreeWindow;

    #endregion

    #region 내부 상태

    private SkillTreeNodeData _nodeData;
    private bool _isUnlocked;
    private bool _canUnlock;

    #endregion

    #region 프로퍼티

    public SkillTreeNodeData NodeData => _nodeData;
    public bool IsUnlocked => _isUnlocked;

    #endregion

    #region 초기화

    /// <summary>
    /// 노드 UI 초기화
    /// </summary>
    public void Initialize(SkillTreeNodeData nodeData, SkillTreeManager manager, SkillTreeWindow window)
    {
        _nodeData = nodeData;
        _skillTreeManager = manager;
        _skillTreeWindow = window;

        if (_nodeData == null)
        {
            Debug.LogError("[SkillTreeNodeUI] NodeData가 null입니다!");
            return;
        }

        SetupVisuals();
        RefreshState();
    }

    /// <summary>
    /// 비주얼 설정
    /// </summary>
    private void SetupVisuals()
    {
        // 아이콘 설정
        if (_iconImage != null && _nodeData.Icon != null)
        {
            _iconImage.sprite = _nodeData.Icon;
        }

        // 배경 색상 (액티브/패시브 구분)
        if (_backgroundImage != null)
        {
            _backgroundImage.color = _nodeData.IsPassive ? _passiveNodeColor : _activeNodeColor;
        }

        // SP 코스트 표시
        if (_costText != null)
        {
            _costText.text = _nodeData.RequiredSkillPoints.ToString();
        }
    }

    #endregion

    #region 상태 업데이트

    /// <summary>
    /// 노드 상태 갱신
    /// </summary>
    public void RefreshState()
    {
        if (_nodeData == null || _skillTreeManager == null)
            return;

        // 언락 상태 확인
        _isUnlocked = _skillTreeManager.IsNodeUnlocked(_nodeData.NodeID);

        // 언락 가능 여부 확인
        _canUnlock = !_isUnlocked && _skillTreeManager.CanUnlockNode(_nodeData, out _);

        UpdateVisuals();
    }

    /// <summary>
    /// 비주얼 업데이트
    /// </summary>
    private void UpdateVisuals()
    {
        if (_isUnlocked)
        {
            // 언락됨: 밝게 표시
            SetLocked(false);
            SetHighlight(false);
        }
        else if (_canUnlock)
        {
            // 언락 가능: 하이라이트
            SetLocked(false);
            SetHighlight(true);
        }
        else
        {
            // 잠김: 어둡게 표시
            SetLocked(true);
            SetHighlight(false);
        }
    }

    /// <summary>
    /// 잠금 상태 설정
    /// </summary>
    private void SetLocked(bool locked)
    {
        if (_lockedOverlay != null)
        {
            _lockedOverlay.gameObject.SetActive(locked);
        }

        if (_iconImage != null)
        {
            _iconImage.color = locked ? _lockedColor : _unlockedColor;
        }
    }

    /// <summary>
    /// 하이라이트 설정
    /// </summary>
    private void SetHighlight(bool highlight)
    {
        if (_unlockableHighlight != null)
        {
            _unlockableHighlight.SetActive(highlight);
        }
    }

    #endregion

    #region 입력 처리

    /// <summary>
    /// 클릭 이벤트
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_nodeData == null || _skillTreeManager == null)
            return;

        // 이미 언락됨
        if (_isUnlocked)
        {
            Debug.Log($"[SkillTreeNodeUI] {_nodeData.NodeName}은(는) 이미 언락되었습니다.");
            return;
        }

        // 언락 시도
        if (_skillTreeManager.TryUnlockNode(_nodeData))
        {
            Debug.Log($"[SkillTreeNodeUI] {_nodeData.NodeName} 언락 성공!");
            RefreshState();
        }
        else
        {
            // 실패 이유 확인
            _skillTreeManager.CanUnlockNode(_nodeData, out string failReason);
            Debug.Log($"[SkillTreeNodeUI] {_nodeData.NodeName} 언락 실패: {failReason}");
        }
    }

    /// <summary>
    /// 마우스 호버 시작
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_skillTreeWindow != null && _nodeData != null)
        {
            _skillTreeWindow.ShowTooltip(_nodeData, this);
        }
    }

    /// <summary>
    /// 마우스 호버 종료
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_skillTreeWindow != null)
        {
            _skillTreeWindow.HideTooltip();
        }
    }

    #endregion

    #region 연결선 그리기

    /// <summary>
    /// 부모 노드와의 연결선 그리기
    /// (SkillTreeWindow에서 호출)
    /// </summary>
    public void DrawConnectionToParent(SkillTreeNodeUI parentNodeUI, LineRenderer lineRenderer)
    {
        if (parentNodeUI == null || lineRenderer == null)
            return;

        Vector3 startPos = parentNodeUI.transform.position;
        Vector3 endPos = transform.position;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // 연결선 색상 (부모 노드가 언락되었는지에 따라)
        Color lineColor = parentNodeUI.IsUnlocked ? Color.green : Color.gray;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug: Print Node Info")]
    private void DebugPrintInfo()
    {
        if (_nodeData == null)
        {
            Debug.Log("[SkillTreeNodeUI] NodeData가 없습니다!");
            return;
        }

        Debug.Log("===== 노드 정보 =====");
        Debug.Log($"이름: {_nodeData.NodeName}");
        Debug.Log($"타입: {(_nodeData.IsPassive ? "패시브" : "액티브")}");
        Debug.Log($"티어: {_nodeData.Tier}");
        Debug.Log($"필요 SP: {_nodeData.RequiredSkillPoints}");
        Debug.Log($"필요 레벨: {_nodeData.RequiredLevel}");
        Debug.Log($"언락 상태: {_isUnlocked}");
        Debug.Log($"언락 가능: {_canUnlock}");
    }

    #endregion
}
