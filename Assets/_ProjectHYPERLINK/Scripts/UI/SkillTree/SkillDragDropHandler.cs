using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 스킬 드래그 앤 드롭 핸들러 (싱글톤)
/// 
/// 역할:
/// - 스킬 트리 노드에서 스킬 슬롯으로의 드래그 앤 드롭 중재
/// - 드래그 상태 관리
/// - 배치 유효성 검사
/// - DraggingVisualizeSkill 제어
/// - 슬롯 간 드래그 간섭 방지
/// 
/// 이벤트 흐름:
/// 1. SkillTreeNodeUI: 유저가 드래그 시작
/// 2. SkillDragDropHandler: 드래그 상태 관리 (이 클래스)
/// 3. SkillSlotUI: 드롭 받기
/// 
/// 디자인 패턴:
/// - 싱글톤 패턴
/// - 중재자 패턴 (Mediator)
/// </summary>
public class SkillDragDropHandler : MonoBehaviour
{
    #region 싱글톤

    private static SkillDragDropHandler _instance;
    public static SkillDragDropHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SkillDragDropHandler>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SkillDragDropHandler");
                    _instance = go.AddComponent<SkillDragDropHandler>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region 참조

    [Header("드래그 비주얼")]
    [SerializeField] private DraggingVisualizeSkill _dragVisual;

    [Header("스킬 슬롯들")]
    [SerializeField] private List<SkillSlotUI> _skillSlots = new List<SkillSlotUI>();

    #endregion

    #region 내부 상태

    private bool _isDragging = false;
    private SkillData _draggedSkill = null;
    private SkillTreeNodeUI _sourceNode = null;
    private SkillSlotUI _hoveredSlot = null;

    // [NEW] 슬롯 간 드래그 활성 플래그
    private bool _slotDragActive = false;

    #endregion

    #region 프로퍼티

    public bool IsDragging => _isDragging;
    public SkillData DraggedSkill => _draggedSkill;

    #endregion

    #region Unity 생명주기

    private void Awake()
    {
        // 싱글톤 체크
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // DraggingVisualizeSkill 자동 검색
        if (_dragVisual == null)
        {
            _dragVisual = GetComponentInChildren<DraggingVisualizeSkill>(true);
            if (_dragVisual == null)
            {
                Debug.LogError("[SkillDragDropHandler] DraggingVisualizeSkill을 찾을 수 없습니다!");
            }
        }

        // 드래그 비주얼 초기화 (비활성화)
        if (_dragVisual != null)
        {
            _dragVisual.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 드래그 앤 드롭 API

    /// <summary>
    /// 드래그 시작
    /// 
    /// 호출: SkillTreeNodeUI.OnBeginDrag()
    /// </summary>
    public void BeginDrag(SkillData skillData, SkillTreeNodeUI sourceNode)
    {
        if (skillData == null)
        {
            Debug.LogWarning("[SkillDragDropHandler] SkillData가 null입니다!");
            return;
        }

        _isDragging = true;
        _draggedSkill = skillData;
        _sourceNode = sourceNode;

        // 드래그 비주얼 활성화
        if (_dragVisual != null)
        {
            _dragVisual.gameObject.SetActive(true);
            _dragVisual.Spawn(skillData);
        }

        Debug.Log($"[SkillDragDrop] 드래그 시작: {skillData.SkillName}");
    }

    /// <summary>
    /// 드래그 중
    /// 
    /// 호출: SkillTreeNodeUI.OnDrag()
    /// 매 프레임 호출
    /// </summary>
    public void Drag(PointerEventData eventData)
    {
        if (!_isDragging || _dragVisual == null)
            return;

        // 드래그 비주얼 위치 업데이트
        _dragVisual.UpdatePosition(eventData.position);

        // 현재 호버 중인 슬롯 찾기
        _hoveredSlot = GetHoveredSlot(eventData);

        // 배치 가능 여부에 따라 색상 변경
        if (_hoveredSlot != null)
        {
            bool canPlace = CanPlaceSkillInSlot(_draggedSkill, _hoveredSlot);
            bool isSameSkill = _hoveredSlot.SkillData == _draggedSkill;

            _dragVisual.SetValidState(canPlace, isSameSkill);
        }
        else
        {
            // 슬롯 밖: 배치 불가
            _dragVisual.SetValidState(false);
        }
    }

    /// <summary>
    /// 드래그 종료
    /// 
    /// 호출: SkillTreeNodeUI.OnEndDrag()
    /// </summary>
    public void EndDrag(PointerEventData eventData)
    {
        // [NEW] 슬롯 간 드래그 중이면 무시
        if (_slotDragActive)
        {
            _isDragging = false;
            _draggedSkill = null;
            _sourceNode = null;

            if (_dragVisual != null)
            {
                _dragVisual.Clear();
                _dragVisual.gameObject.SetActive(false);
            }

            Debug.Log($"[SkillDragDrop] 슬롯 간 드래그 중 - 스킬 트리 드롭 무시");
            return;
        }

        if (!_isDragging)
            return;

        // 드롭할 슬롯 찾기
        SkillSlotUI dropSlot = GetHoveredSlot(eventData);

        if (dropSlot != null)
        {
            // 슬롯에 스킬 배치 시도
            if (CanPlaceSkillInSlot(_draggedSkill, dropSlot))
            {
                dropSlot.AssignSkill(_draggedSkill);
                Debug.Log($"[SkillDragDrop] {_draggedSkill.SkillName}을(를) {dropSlot.name}에 배치했습니다.");
            }
            else
            {
                Debug.Log($"[SkillDragDrop] {dropSlot.name}에 스킬을 배치할 수 없습니다.");
            }
        }

        // 드래그 상태 정리
        ClearDragState();

        Debug.Log($"[SkillDragDrop] 드래그 종료");
    }

    /// <summary>
    /// 드래그 취소
    /// </summary>
    public void CancelDrag()
    {
        ClearDragState();
    }

    #endregion

    #region 슬롯 간 드래그 제어 (NEW)

    /// <summary>
    /// 슬롯 간 드래그 활성화/비활성화 설정
    /// 
    /// 호출: SkillSlotUI.OnBeginDrag() / OnEndDrag()
    /// 
    /// 목적: 슬롯끼리 드래그할 때 SkillDragDropHandler가 간섭하지 않도록 함
    /// </summary>
    public void SetSlotDragActive(bool active)
    {
        _slotDragActive = active;
        Debug.Log($"[SkillDragDrop] 슬롯 간 드래그: {(active ? "활성" : "비활성")}");
    }

    #endregion

    #region 유효성 검사

    /// <summary>
    /// 스킬을 슬롯에 배치 가능한지 확인
    /// </summary>
    private bool CanPlaceSkillInSlot(SkillData skill, SkillSlotUI slot)
    {
        if (skill == null || slot == null)
            return false;

        // 이미 동일한 스킬이 있으면 배치 불가
        if (slot.SkillData == skill)
            return false;

        // 슬롯이 비어있거나 다른 스킬이 있으면 배치 가능 (교체)
        return true;
    }

    /// <summary>
    /// 현재 마우스 위치에서 호버 중인 스킬 슬롯 찾기
    /// </summary>
    private SkillSlotUI GetHoveredSlot(PointerEventData eventData)
    {
        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot == null)
                continue;

            RectTransform rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null)
                continue;

            // RectTransform 영역 내에 마우스가 있는지 확인
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, eventData.pressEventCamera))
            {
                return slot;
            }
        }

        return null;
    }

    #endregion

    #region 슬롯 관리

    /// <summary>
    /// 스킬 슬롯 등록
    /// 
    /// 호출: SkillSlotUI.Awake() 또는 CharacterUIController
    /// </summary>
    public void RegisterSkillSlot(SkillSlotUI slot)
    {
        if (slot != null && !_skillSlots.Contains(slot))
        {
            _skillSlots.Add(slot);
            Debug.Log($"[SkillDragDropHandler] 슬롯 등록: {slot.name}");
        }
    }

    /// <summary>
    /// 스킬 슬롯 해제
    /// </summary>
    public void UnregisterSkillSlot(SkillSlotUI slot)
    {
        if (_skillSlots.Contains(slot))
        {
            _skillSlots.Remove(slot);
            Debug.Log($"[SkillDragDropHandler] 슬롯 해제: {slot.name}");
        }
    }

    #endregion

    #region 내부 헬퍼

    /// <summary>
    /// 드래그 상태 정리
    /// </summary>
    private void ClearDragState()
    {
        _isDragging = false;
        _draggedSkill = null;
        _sourceNode = null;
        _hoveredSlot = null;

        if (_dragVisual != null)
        {
            _dragVisual.Clear();
            _dragVisual.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug: Print State")]
    private void DebugPrintState()
    {
        Debug.Log("===== SkillDragDropHandler 상태 =====");
        Debug.Log($"드래그 중: {_isDragging}");
        Debug.Log($"슬롯 간 드래그 활성: {_slotDragActive}");
        Debug.Log($"드래그된 스킬: {_draggedSkill?.SkillName ?? "없음"}");
        Debug.Log($"등록된 슬롯: {_skillSlots.Count}개");

        if (_skillSlots.Count > 0)
        {
            Debug.Log("--- 슬롯 목록 ---");
            for (int i = 0; i < _skillSlots.Count; i++)
            {
                if (_skillSlots[i] != null)
                {
                    string skillName = _skillSlots[i].SkillData?.SkillName ?? "비어있음";
                    Debug.Log($"  슬롯 {i}: {_skillSlots[i].name} - {skillName}");
                }
            }
        }
    }

    #endregion
}
