using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 스킬 드래그 앤 드롭 핸들러 (싱글톤)
/// 
/// 기능
/// 1. 스킬 트리에서 스킬 슬롯으로의 드래그 앤 드롭 관리
/// 2. 드래그 비주얼(DraggingVisualizeSkill) 제어
/// 3. 스킬 슬롯 등록 및 관리
/// 4. 배치 가능 여부 검증 (중복 체크)
/// 5. OnDrop과 EndDrag 중복 처리 방지
/// 
/// </summary>
public class SkillDragDropHandler : MonoBehaviour
{
    #region Singleton

    private static SkillDragDropHandler _instance;

    public static SkillDragDropHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SkillDragDropHandler>();
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

    #region Serialized Fields

    [Header("드래그 비주얼")]
    [Tooltip("드래그 중 표시될 스킬 아이콘 비주얼")]
    [SerializeField] private DraggingVisualizeSkill _dragVisual;

    [Header("스킬 슬롯들")]
    [Tooltip("등록된 모든 스킬 슬롯 (자동 등록됨)")]
    [SerializeField] private List<SkillSlotUI> _skillSlots = new List<SkillSlotUI>();

    #endregion

    #region Private Fields

    private SkillActivationSystem _skillActivationSystem;
    private bool _isDragging = false;
    private SkillData _draggedSkill = null;
    private SkillTreeNodeUI _sourceNode = null;
    private SkillSlotUI _hoveredSlot = null;
    private bool _slotDragActive = false;
    private bool _dropHandledBySlot = false;

    #endregion

    #region Properties

    /// <summary>
    /// 현재 드래그 중인지 여부 (읽기 전용)
    /// 
    /// 사용처:
    /// - SkillSlotUI.OnDrop: 드래그 중인지 확인
    /// - SkillTreeNodeUI: 드래그 상태 확인
    /// </summary>
    public bool IsDragging => _isDragging;

    /// <summary>
    /// 드래그 중인 스킬 데이터 (읽기 전용)
    /// 
    /// 사용처:
    /// - SkillSlotUI.OnDrop: 어떤 스킬을 배치할지 확인
    /// - DraggingVisualizeSkill: 어떤 아이콘을 표시할지
    /// </summary>
    public SkillData DraggedSkill => _draggedSkill;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // 싱글톤 패턴: 중복 인스턴스 방지
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // DraggingVisualizeSkill 찾기
        // 1. Inspector에 할당되어 있으면 사용
        // 2. 없으면 자식에서 찾기
        // 3. 그래도 없으면 씬 전체에서 찾기
        if (_dragVisual == null)
        {
            _dragVisual = GetComponentInChildren<DraggingVisualizeSkill>(true);
            if (_dragVisual == null)
            {
                _dragVisual = FindFirstObjectByType<DraggingVisualizeSkill>(FindObjectsInactive.Include);
            }
        }

        // 드래그 비주얼 초기 상태: 비활성화
        if (_dragVisual != null)
        {
            _dragVisual.gameObject.SetActive(false);
        }

        // SkillActivationSystem 비동기 검색 시작
        StartCoroutine(FindSkillActivationSystem());
    }

    private IEnumerator FindSkillActivationSystem()
    {
        int attempts = 0;
        while (_skillActivationSystem == null && attempts < 20)
        {
            _skillActivationSystem = FindFirstObjectByType<SkillActivationSystem>();
            if (_skillActivationSystem != null)
            {
                DebugHelper.Log("[SkillDragDropHandler] SkillActivationSystem 연결 완료");
                yield break;
            }
            attempts++;
            yield return WaitForSecondsCache.Get(0.5f);
        }

        if (_skillActivationSystem == null)
        {
            DebugHelper.LogWarning("[SkillDragDropHandler] SkillActivationSystem을 찾지 못했습니다. 중복 체크 제한될 수 있음.");
        }
    }

    #endregion

    #region Drag & Drop API

    /// <summary>
    /// 드래그 시작
    /// 
    /// 호출 위치: SkillTreeNodeUI.OnBeginDrag()
    /// 
    /// 처리 순서:
    /// 1. 드래그 상태 플래그 설정
    /// 2. 드래그 데이터 저장
    /// 3. 드롭 처리 플래그 초기화
    /// 4. 드래그 비주얼 활성화 및 스폰
    /// 
    /// 주의:
    /// - _dropHandledBySlot을 false로 초기화 (중요!)
    /// - 이전 드래그의 상태가 남아있을 수 있음
    /// </summary>
    /// <param name="skillData">드래그할 스킬 데이터</param>
    /// <param name="sourceNode">드래그 시작한 스킬 트리 노드</param>
    public void BeginDrag(SkillData skillData, SkillTreeNodeUI sourceNode)
    {
        if (skillData == null)
        {
            DebugHelper.LogWarning("[SkillDragDropHandler] SkillData가 null입니다!");
            return;
        }

        // 드래그 상태 설정
        _isDragging = true;
        _draggedSkill = skillData;
        _sourceNode = sourceNode;

        // [KEY FIX] 이전 드래그의 플래그 초기화
        _dropHandledBySlot = false;

        // 드래그 비주얼 활성화
        if (_dragVisual != null)
        {
            _dragVisual.gameObject.SetActive(true);
            _dragVisual.Spawn(skillData);
        }
    }

    /// <summary>
    /// 드래그 중 (매 프레임 호출)
    /// 
    /// 호출 위치: SkillTreeNodeUI.OnDrag()
    /// 
    /// 처리 순서:
    /// 1. 드래그 비주얼 위치 업데이트
    /// 2. 마우스 아래 슬롯 감지 (GetHoveredSlot)
    /// 3. 배치 가능 여부 확인 (CanPlaceSkillInSlot)
    /// 4. 비주얼 색상 변경 (SetValidState)
    /// 
    /// 최적화:
    /// - 색상 변경은 상태가 바뀔 때만 (SetValidState 내부에서 처리)
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터 (마우스 위치 포함)</param>
    public void Drag(PointerEventData eventData)
    {
        if (!_isDragging || _dragVisual == null)
            return;

        // 1. 드래그 비주얼을 마우스 위치로 이동
        _dragVisual.UpdatePosition(eventData.position);

        // 2. 마우스 아래 슬롯 감지
        _hoveredSlot = GetHoveredSlot(eventData);

        if (_hoveredSlot != null)
        {
            // 3. 배치 가능 여부 확인
            bool canPlace = CanPlaceSkillInSlot(_draggedSkill, _hoveredSlot, out string reason);
            bool isSameSkill = _hoveredSlot.SkillData == _draggedSkill;

            // 4. 비주얼 색상 업데이트
            _dragVisual.SetValidState(canPlace, isSameSkill);
        }
        else
        {
            // 슬롯 밖: 빨간색 (배치 불가)
            _dragVisual.SetValidState(false);
        }
    }

    /// <summary>
    /// 드래그 종료
    /// 
    /// 호출 위치: SkillTreeNodeUI.OnEndDrag()
    /// 
    /// ═══════════════════════════════════════════════════════════════════
    /// 처리 로직 (우선순위 순서)
    /// ═══════════════════════════════════════════════════════════════════
    /// 
    /// [1순위] 슬롯 간 드래그 중?
    ///    → 스킬 트리 드롭 무시
    ///    → ClearDragState() 후 종료
    /// 
    /// [2순위] OnDrop이 이미 처리했나?  ← KEY FIX
    ///    → _dropHandledBySlot == true
    ///    → ClearDragState() 후 종료
    ///    → AssignSkill() 호출하지 않음!
    /// 
    /// [3순위] 정상 EndDrag 처리
    ///    → GetHoveredSlot()으로 대상 확인
    ///    → CanPlaceSkillInSlot()으로 검증
    ///    → AssignSkill() 호출
    /// 
    /// ═══════════════════════════════════════════════════════════════════
    /// 왜 OnDrop과 EndDrag 둘 다 필요한가?
    /// ═══════════════════════════════════════════════════════════════════
    /// 
    /// OnDrop (Unity EventSystem):
    /// - 자동으로 올바른 대상 슬롯을 찾아서 호출
    /// - 정확하지만 호출 보장 안됨 (raycast 실패 가능)
    /// 
    /// EndDrag (수동 처리):
    /// - 항상 호출됨
    /// - OnDrop이 실패했을 때 백업 역할
    /// 
    /// _dropHandledBySlot 플래그:
    /// - OnDrop 성공 시 true 설정
    /// - EndDrag에서 중복 처리 방지
    /// 
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터</param>
    public void EndDrag(PointerEventData eventData)
    {
        // [1순위] 슬롯 간 드래그 중이면 스킬 트리 드롭 무시
        if (_slotDragActive)
        {
            ClearDragState();
            return;
        }

        // 드래그 중이 아니면 처리 안함 (방어 코드)
        if (!_isDragging)
            return;

        // [2순위] [KEY FIX] OnDrop이 이미 처리했으면 중복 방지
        if (_dropHandledBySlot)
        {
            // OnDrop에서 AssignSkill() 호출했으므로
            // 여기서는 정리만 하고 종료
            ClearDragState();
            return;
        }

        // [3순위] OnDrop이 처리 안 했을 때만 여기서 처리
        // (슬롯 밖에 드롭했거나 OnDrop 호출 실패)
        SkillSlotUI dropSlot = GetHoveredSlot(eventData);

        if (dropSlot != null)
        {
            // 배치 가능 여부 재확인
            if (CanPlaceSkillInSlot(_draggedSkill, dropSlot, out string reason))
            {
                dropSlot.AssignSkill(_draggedSkill);
                DebugHelper.Log($"[SkillDragDropHandler] EndDrag를 통해 배치: {_draggedSkill.SkillName} → {dropSlot.name}");
            }
            else
            {
                DebugHelper.LogWarning($"[SkillDragDropHandler] 배치 불가: {reason}");
            }
        }

        ClearDragState();
    }

    public void CancelDrag()
    {
        ClearDragState();
    }

    /// <summary>
    /// OnDrop 처리 완료 알림
    /// 
    /// 호출 위치: SkillSlotUI.OnDrop()
    /// 
    /// 목적:
    /// OnDrop이 스킬 할당을 처리했음을 알려
    /// EndDrag에서 중복 처리하지 않도록 함
    /// 
    /// 효과:
    /// - 이중 AssignSkill() 호출 방지
    /// - "이미 다른 슬롯에 존재" 오류 로그 제거
    /// - 깔끔한 단일 처리 흐름
    /// </summary>
    public void NotifyDropHandled()
    {
        _dropHandledBySlot = true;
    }

    #endregion

    #region Slot Drag Control

    /// <summary>
    /// 슬롯 간 드래그 활성화/비활성화
    /// 
    /// 호출 위치: SkillSlotUI.OnBeginDrag() / OnEndDrag()
    /// 
    /// true: 슬롯끼리 스킬 교환 중
    ///       → 스킬 트리로부터의 드롭 무시
    /// 
    /// false: 스킬 트리에서 슬롯으로 드래그 중
    ///        → 정상 드롭 처리
    /// 
    /// 용도:
    /// 슬롯 간 드래그와 스킬 트리 드래그를 구분하여
    /// 충돌 방지
    /// </summary>
    /// <param name="active">슬롯 간 드래그 활성화 여부</param>
    public void SetSlotDragActive(bool active)
    {
        _slotDragActive = active;
    }

    #endregion

    #region Validation

    /// <summary>
    /// 스킬을 슬롯에 배치 가능한지 확인
    /// 
    /// 검증 항목:
    /// 1. null 체크
    /// 2. 같은 슬롯에 같은 스킬 체크
    /// 3. 다른 슬롯에 같은 스킬 체크 (중복 방지)
    /// 
    /// 중복 체크 로직:
    /// - SkillActivationSystem.HasSkill(skill, excludeIndex)
    /// - excludeIndex: 현재 대상 슬롯 (자기 자신 제외)
    /// - 다른 슬롯에 같은 스킬이 있으면 배치 불가
    /// 
    /// 참고:
    /// - 같은 슬롯에 다른 스킬은 OK (교체)
    /// - 빈 슬롯은 항상 OK
    /// </summary>
    /// <param name="skill">배치하려는 스킬</param>
    /// <param name="slot">대상 슬롯</param>
    /// <param name="reason">실패 이유 (out 파라미터)</param>
    /// <returns>배치 가능 여부</returns>
    private bool CanPlaceSkillInSlot(SkillData skill, SkillSlotUI slot, out string reason)
    {
        reason = "";

        // 1. null 체크
        if (skill == null || slot == null)
        {
            reason = "스킬 또는 슬롯이 null";
            return false;
        }

        // 2. 같은 슬롯에 같은 스킬 체크
        if (slot.SkillData == skill)
        {
            reason = "같은 슬롯에 이미 동일한 스킬 존재";
            return false;
        }

        // 3. 다른 슬롯에 같은 스킬 체크 (중복 방지)
        if (_skillActivationSystem != null && slot.SlotIndex >= 0)
        {
            // 대상 슬롯을 제외하고 다른 슬롯에 있는지 체크
            if (_skillActivationSystem.HasSkill(skill, slot.SlotIndex))
            {
                SkillSlotUI existingSlot = _skillActivationSystem.FindSlotWithSkill(skill);
                if (existingSlot != null && existingSlot != slot)
                {
                    reason = $"다른 슬롯({existingSlot.SlotIndex})에 이미 동일한 스킬 존재";
                    return false;
                }
            }
        }

        // 모든 검증 통과
        return true;
    }

    /// <summary>
    /// 마우스 포인터 아래의 슬롯 찾기
    /// 
    /// 방법:
    /// RectTransformUtility.RectangleContainsScreenPoint()
    /// - RectTransform의 경계 안에 마우스가 있는지 체크
    /// - 스크린 좌표 기준
    /// 
    /// 순회 순서:
    /// _skillSlots 리스트 순서대로 (등록 순서)
    /// 먼저 매치되는 슬롯 반환
    /// 
    /// 참고:
    /// - 겹치는 슬롯이 있으면 먼저 등록된 슬롯 반환
    /// - EventSystem의 OnDrop과 결과가 다를 수 있음
    ///   (이유: EventSystem은 역순 raycast)
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터</param>
    /// <returns>감지된 슬롯 (없으면 null)</returns>
    private SkillSlotUI GetHoveredSlot(PointerEventData eventData)
    {
        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot == null)
                continue;

            RectTransform rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null)
                continue;

            // 스크린 좌표가 슬롯 영역 안에 있는지 체크
            if (RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera))
            {
                return slot;
            }
        }

        return null;
    }

    #endregion

    #region Slot Management

    /// <summary>
    /// 스킬 슬롯 등록
    /// 
    /// 호출 위치: SkillSlotUI.Awake()
    /// 
    /// 자동 등록:
    /// - 슬롯이 생성될 때 자동으로 호출
    /// - Inspector의 리스트에도 추가됨 (디버깅용)
    /// 
    /// 중복 방지:
    /// - Contains() 체크로 중복 등록 방지
    /// </summary>
    /// <param name="slot">등록할 슬롯</param>
    public void RegisterSkillSlot(SkillSlotUI slot)
    {
        if (slot != null && !_skillSlots.Contains(slot))
        {
            _skillSlots.Add(slot);
        }
    }

    /// <summary>
    /// 스킬 슬롯 등록 해제
    /// 
    /// 호출 위치: SkillSlotUI.OnDestroy()
    /// 
    /// 자동 해제:
    /// - 슬롯이 파괴될 때 자동으로 호출
    /// - 메모리 누수 방지
    /// </summary>
    /// <param name="slot">해제할 슬롯</param>
    public void UnregisterSkillSlot(SkillSlotUI slot)
    {
        if (_skillSlots.Contains(slot))
        {
            _skillSlots.Remove(slot);
        }
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 드래그 상태 정리
    /// 
    /// 호출 위치:
    /// - EndDrag()
    /// - CancelDrag()
    /// 
    /// 정리 항목:
    /// - 모든 드래그 플래그 초기화
    /// - 드래그 데이터 제거
    /// - 드래그 비주얼 정리 및 비활성화
    /// </summary>
    private void ClearDragState()
    {
        _isDragging = false;
        _draggedSkill = null;
        _sourceNode = null;
        _hoveredSlot = null;
        _dropHandledBySlot = false;  // [중요] 플래그도 초기화

        if (_dragVisual != null)
        {
            _dragVisual.Clear();
            _dragVisual.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// 디버그: 모든 슬롯 정보 출력
    /// Unity Inspector에서 우클릭 → Debug: Print All Slots
    /// </summary>
    [ContextMenu("Debug: Print All Slots")]
    private void DebugPrintSlots()
    {
        DebugHelper.Log("═══ REGISTERED SKILL SLOTS ═══");
        for (int i = 0; i < _skillSlots.Count; i++)
        {
            if (_skillSlots[i] != null)
            {
                var slot = _skillSlots[i];
                var rect = slot.GetComponent<RectTransform>();
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);

                DebugHelper.Log($"[{i}] {slot.name} (SlotIndex:{slot.SlotIndex})");
                DebugHelper.Log($"    Position: {rect.position}");
                DebugHelper.Log($"    Corners: BL={corners[0]} TR={corners[2]}");
                DebugHelper.Log($"    Skill: {slot.SkillData?.SkillName ?? "Empty"}");
            }
        }
        DebugHelper.Log($"Total: {_skillSlots.Count} slots");
    }

    /// <summary>
    /// 디버그: 현재 드래그 상태 출력
    /// </summary>
    [ContextMenu("Debug: Print Drag State")]
    private void DebugPrintDragState()
    {
        DebugHelper.Log("═══ DRAG STATE ═══");
        DebugHelper.Log($"IsDragging: {_isDragging}");
        DebugHelper.Log($"DraggedSkill: {_draggedSkill?.SkillName ?? "None"}");
        DebugHelper.Log($"SourceNode: {_sourceNode?.name ?? "None"}");
        DebugHelper.Log($"HoveredSlot: {_hoveredSlot?.name ?? "None"}");
        DebugHelper.Log($"SlotDragActive: {_slotDragActive}");
        DebugHelper.Log($"DropHandledBySlot: {_dropHandledBySlot}");
        DebugHelper.Log($"SkillActivationSystem: {(_skillActivationSystem != null ? "Connected" : "Not Found")}");
    }

    #endregion
}
