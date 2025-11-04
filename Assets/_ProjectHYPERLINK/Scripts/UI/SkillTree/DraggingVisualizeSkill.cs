using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 중인 스킬 시각화 컴포넌트 - FINAL VERSION
/// 
/// ═══════════════════════════════════════════════════════════════════════
/// 역할 및 책임
/// ═══════════════════════════════════════════════════════════════════════
/// 
/// 1. 마우스 커서를 따라다니는 스킬 아이콘 표시
/// 2. 드래그 중 배치 가능 여부 시각 피드백 (색상 변경)
/// 3. 드래그가 시작/종료될 때 활성화/비활성화
/// 
/// ═══════════════════════════════════════════════════════════════════════
/// Hierarchy 요구사항 (중요!)
/// ═══════════════════════════════════════════════════════════════════════
/// 
/// 이 GameObject는 반드시 Canvas의 직접 자식이어야 합니다:
/// 
/// ✅ 올바른 구조:
/// GameCanvas (Canvas)
/// ├── DraggingVisualizeSkill  ← 여기
/// ├── GameView
/// └── SkillDragDropHandler
/// 
/// ❌ 잘못된 구조:
/// GameCanvas (Canvas)
/// └── SkillDragDropHandler
///     └── DraggingVisualizeSkill  ← 좌표 변환 오류 발생!
/// 
/// ═══════════════════════════════════════════════════════════════════════
/// 해결한 문제
/// ═══════════════════════════════════════════════════════════════════════
/// 
/// [문제 #1] 좌표 변환 오류
/// - 증상: 드래그 비주얼이 마우스와 다른 위치에 표시
/// - 원인: transform.position에 스크린 좌표를 직접 할당
/// - 해결: anchoredPosition 사용 + Anchors 강제 설정
/// 
/// [문제 #2] 로컬 좌표계 불일치
/// - 증상: Local 좌표가 Screen 좌표와 완전히 다름
/// - 원인: 중첩된 부모(SkillDragDropHandler) 아래 배치
/// - 해결: Canvas 직접 자식으로 이동
/// 
/// ═══════════════════════════════════════════════════════════════════════
/// 사용 방법
/// ═══════════════════════════════════════════════════════════════════════
/// 
/// 1. Canvas 아래에 빈 GameObject 생성 (이름: DraggingVisualizeSkill)
/// 2. 이 스크립트 추가
/// 3. 자식으로 Image 오브젝트 생성
/// 4. Inspector에서 Image와 RectTransform 할당
/// 5. 색상 설정 (Valid=White, Invalid=Red, SameSlot=Yellow)
/// 
/// ═══════════════════════════════════════════════════════════════════════
/// 라이프사이클
/// ═══════════════════════════════════════════════════════════════════════
/// 
/// Awake()          → Anchors 강제 설정
/// BeginDrag        → Spawn() 호출, gameObject 활성화
/// Drag (매 프레임)  → UpdatePosition() 호출
/// EndDrag          → Clear() 호출, gameObject 비활성화
/// 
/// </summary>
public class DraggingVisualizeSkill : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI 컴포넌트")]
    [Tooltip("드래그 비주얼로 표시할 Image 컴포넌트")]
    [SerializeField] private Image _image;

    [Tooltip("위치 조정을 위한 RectTransform")]
    [SerializeField] private RectTransform _rect;

    [Header("색상 설정")]
    [Tooltip("배치 가능 시 표시 색상 (기본: White)")]
    [SerializeField] private Color _validColor = Color.white;

    [Tooltip("배치 불가 시 표시 색상 (기본: Red)")]
    [SerializeField] private Color _invalidColor = Color.red;

    [Tooltip("같은 슬롯에 드롭 시 표시 색상 (기본: Yellow)")]
    [SerializeField] private Color _sameSlotColor = Color.yellow;

    #endregion

    #region Private Fields

    /// <summary>
    /// 현재 드래그 중인 스킬 데이터
    /// null이면 드래그 중이 아님
    /// </summary>
    private SkillData _currentSkill;

    #endregion

    #region Properties

    /// <summary>
    /// 현재 드래그 중인 스킬 (읽기 전용)
    /// </summary>
    public SkillData CurrentSkill => _currentSkill;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 초기화
    /// 
    /// CRITICAL: Anchors를 (0,0)으로 강제 설정
    /// 
    /// 이유:
    /// - anchoredPosition은 Anchors 기준 상대 좌표
    /// - Anchors가 (0,0)이면 스크린 픽셀 좌표와 1:1 매핑
    /// - 다른 Anchor 값이면 좌표 변환 복잡해짐
    /// 
    /// 예시:
    /// Anchors (0, 0) + anchoredPosition (100, 50) = 화면 (100, 50)
    /// Anchors (0.5, 0.5) + anchoredPosition (100, 50) = 화면 중심에서 (100, 50) 떨어진 위치
    /// </summary>
    private void Awake()
    {
        // Anchors를 좌측 하단(0, 0)으로 고정
        // 이렇게 하면 anchoredPosition = 스크린 픽셀 좌표
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.zero;

        // Pivot을 중앙(0.5, 0.5)으로 설정
        // 스킬 아이콘이 마우스 커서 중앙에 오도록
        _rect.pivot = new Vector2(0.5f, 0.5f);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 드래그 시작 시 호출
    /// 스킬 아이콘을 설정하고 비주얼을 활성화
    /// 
    /// 호출 위치: SkillDragDropHandler.BeginDrag()
    /// 
    /// 처리 순서:
    /// 1. 스킬 데이터 저장
    /// 2. 아이콘 스프라이트 설정
    /// 3. 크기 설정 (64x64)
    /// 4. 초기 색상을 Valid(White)로 설정
    /// </summary>
    /// <param name="skillData">드래그할 스킬 데이터</param>
    public void Spawn(SkillData skillData)
    {
        _currentSkill = skillData;

        // 스킬 데이터와 아이콘이 유효한 경우
        if (_currentSkill != null && _currentSkill.SkillIcon != null)
        {
            // 아이콘 스프라이트 설정
            _image.sprite = _currentSkill.SkillIcon;
            _image.enabled = true;

            // 비주얼 크기 설정 (스킬 슬롯과 동일한 크기)
            _rect.sizeDelta = new Vector2(64f, 64f);
        }
        else
        {
            // 유효하지 않은 경우 이미지 비활성화
            _image.enabled = false;
        }

        // 초기 색상: 배치 가능 상태 (White)
        _image.color = _validColor;
    }

    /// <summary>
    /// 마우스 위치로 비주얼 이동 - FIXED VERSION
    /// 
    /// 호출 위치: SkillDragDropHandler.Drag() (매 프레임)
    /// 
    /// ═══════════════════════════════════════════════════════════════════
    /// 좌표 시스템 이해하기
    /// ═══════════════════════════════════════════════════════════════════
    /// 
    /// eventData.position (스크린 좌표):
    /// - 픽셀 단위 절대 좌표
    /// - 화면 왼쪽 하단이 (0, 0)
    /// - 1920x1080 화면이면 (0,0) ~ (1920, 1080)
    /// 
    /// anchoredPosition (앵커 기준 좌표):
    /// - Anchors 위치 기준 상대 좌표
    /// - Anchors가 (0,0)이면 스크린 좌표와 동일
    /// - 부모의 위치/크기와 무관
    /// 
    /// ═══════════════════════════════════════════════════════════════════
    /// 왜 이렇게 단순한가?
    /// ═══════════════════════════════════════════════════════════════════
    /// 
    /// 1. DraggingVisualizeSkill이 Canvas 직접 자식
    /// 2. Awake()에서 Anchors를 (0,0)으로 설정
    /// 3. 따라서 anchoredPosition = 스크린 픽셀 좌표
    /// 
    /// ═══════════════════════════════════════════════════════════════════
    /// 이전 버전의 문제
    /// ═══════════════════════════════════════════════════════════════════
    /// 
    /// ❌ transform.position = screenPosition
    ///    → 월드 좌표에 스크린 좌표 직접 할당
    ///    → 좌표계 불일치로 위치 오류
    /// 
    /// ❌ _rect.localPosition = screenPosition
    ///    → 부모 로컬 좌표계에 스크린 좌표 할당
    ///    → 부모의 transform에 영향 받음
    /// 
    /// ✅ _rect.anchoredPosition = screenPosition
    ///    → Anchor 기준 좌표에 스크린 좌표 할당
    ///    → Anchors가 (0,0)이므로 정확히 일치
    /// 
    /// </summary>
    /// <param name="screenPosition">마우스 스크린 좌표</param>
    public void UpdatePosition(Vector2 screenPosition)
    {
        // Anchors가 (0,0)으로 설정되어 있으므로
        // anchoredPosition에 스크린 좌표를 직접 할당 가능
        _rect.anchoredPosition = screenPosition;

        // 디버그: 좌표가 정확히 일치하는지 확인
        // 정상 동작 시: Screen 좌표와 Anchored 좌표가 동일해야 함
        // Debug.Log($"[Visual] Screen:{screenPosition} → Anchored:{_rect.anchoredPosition}");
    }

    /// <summary>
    /// 배치 가능 여부에 따라 색상 변경
    /// 
    /// 호출 위치: SkillDragDropHandler.Drag()
    /// 
    /// 색상 의미:
    /// - White (Valid): 빈 슬롯이거나 교체 가능 → 배치 가능
    /// - Red (Invalid): 중복 스킬이거나 조건 불만족 → 배치 불가
    /// - Yellow (SameSlot): 같은 슬롯에 드롭 → 의미 없음
    /// 
    /// 사용자 피드백:
    /// 드래그 중 실시간으로 색상이 변경되어
    /// 사용자가 배치 가능 여부를 시각적으로 확인 가능
    /// </summary>
    /// <param name="isValid">배치 가능 여부</param>
    /// <param name="isSameSlot">같은 슬롯인지 여부</param>
    public void SetValidState(bool isValid, bool isSameSlot = false)
    {
        if (isSameSlot)
        {
            // 같은 슬롯: Yellow (참고용, 실제로는 배치 안됨)
            _image.color = _sameSlotColor;
        }
        else if (isValid)
        {
            // 배치 가능: White
            _image.color = _validColor;
        }
        else
        {
            // 배치 불가: Red
            _image.color = _invalidColor;
        }
    }

    /// <summary>
    /// 드래그 종료 시 정리
    /// 
    /// 호출 위치: SkillDragDropHandler.EndDrag()
    /// 
    /// 처리:
    /// 1. 스킬 데이터 제거
    /// 2. 아이콘 스프라이트 제거
    /// 3. Image 비활성화
    /// 
    /// 참고: GameObject 자체는 비활성화하지 않음
    ///       (SkillDragDropHandler에서 처리)
    /// </summary>
    public void Clear()
    {
        _currentSkill = null;
        _image.sprite = null;
        _image.enabled = false;
    }

    #endregion

    #region Debug Methods (Optional)

    /// <summary>
    /// 디버그: 현재 상태 출력
    /// Unity Inspector에서 우클릭 → Debug State로 실행 가능
    /// </summary>
    [ContextMenu("Debug: Print State")]
    private void DebugPrintState()
    {
        Debug.Log("═══ DraggingVisualizeSkill State ═══");
        Debug.Log($"Current Skill: {_currentSkill?.SkillName ?? "None"}");
        Debug.Log($"Image Enabled: {_image.enabled}");
        Debug.Log($"Position: {_rect.position}");
        Debug.Log($"Anchored Position: {_rect.anchoredPosition}");
        Debug.Log($"Anchors: Min={_rect.anchorMin} Max={_rect.anchorMax}");
        Debug.Log($"Pivot: {_rect.pivot}");
        Debug.Log($"Size: {_rect.sizeDelta}");
        Debug.Log($"Color: {_image.color}");
    }

    #endregion
}

/*
 * ═══════════════════════════════════════════════════════════════════════
 * 사용 예시
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * // SkillDragDropHandler.cs
 * 
 * public void BeginDrag(SkillData skillData)
 * {
 *     _dragVisual.gameObject.SetActive(true);
 *     _dragVisual.Spawn(skillData);
 * }
 * 
 * public void Drag(PointerEventData eventData)
 * {
 *     _dragVisual.UpdatePosition(eventData.position);
 *     
 *     SkillSlotUI slot = GetHoveredSlot(eventData);
 *     bool canPlace = CanPlaceSkill(slot);
 *     _dragVisual.SetValidState(canPlace);
 * }
 * 
 * public void EndDrag()
 * {
 *     _dragVisual.Clear();
 *     _dragVisual.gameObject.SetActive(false);
 * }
 * 
 * ═══════════════════════════════════════════════════════════════════════
 * 문제 해결 체크리스트
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * [✓] DraggingVisualizeSkill이 Canvas 직접 자식인가?
 * [✓] RectTransform Anchors가 (0, 0)인가?
 * [✓] Pivot이 (0.5, 0.5)인가?
 * [✓] UpdatePosition에서 anchoredPosition 사용하는가?
 * [✓] Image의 Raycast Target이 꺼져있는가?
 * 
 * ═══════════════════════════════════════════════════════════════════════
 * 버전 정보
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * Version: 1.0 Final
 * Date: 2025-11-03
 * Status: ✅ Production Ready
 * 
 */
