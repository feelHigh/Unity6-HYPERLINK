using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스킬 슬롯 UI 컴포넌트 (Q/W/E/R)
/// 
/// 주의사항:
/// - OnDrop에서 NotifyDropHandled() 호출 추가 (이중 처리 방지)
/// </summary>
public class SkillSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler,
                            IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region UI 참조

    [Header("UI 참조")]
    [SerializeField] private Image _skillIcon;
    [SerializeField] private Image _cooldownOverlay;
    [SerializeField] private TextMeshProUGUI _cooldownText;
    [SerializeField] private TextMeshProUGUI _manaCostText;
    [SerializeField] private TextMeshProUGUI _keyBindText;
    [SerializeField] private Image _lockedOverlay;
    [SerializeField] private GameObject _emptyIndicator;

    #endregion

    #region 시각 설정

    [Header("시각 설정")]
    [SerializeField] private Color _availableColor = Color.white;
    [SerializeField] private Color _onCooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color _insufficientManaColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Sprite _emptySlotSprite;

    #endregion

    #region 참조

    private bool _isInitialized = false;

    [Header("시스템 참조")]
    private SkillActivationSystem _skillActivationSystem;

    #endregion

    // 내부 상태
    private SkillData _skillData;
    private bool _isLocked = false;
    private float _currentCooldown = 0f;
    private int _slotIndex = -1;

    // 쿨다운 텍스트 캐싱 (GC 최적화: 매 프레임 ToString 호출 방지)
    private int _lastDisplayedCooldownInt = -1;

    // 드래그 상태
    private bool _isDragging = false;
    private GameObject _dragVisual;

    // Public 프로퍼티
    public SkillData SkillData => _skillData;
    public bool IsOnCooldown => _currentCooldown > 0f;
    public bool IsLocked => _isLocked;
    public bool IsEmpty => _skillData == null;
    public int SlotIndex => _slotIndex;

    #region Unity 생명주기

    private void Awake()
    {
        if (SkillDragDropHandler.Instance != null)
        {
            SkillDragDropHandler.Instance.RegisterSkillSlot(this);
        }
    }

    private void OnEnable()
    {
        PlayerInitializationManager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerInitializationManager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnDestroy()
    {
        if (SkillDragDropHandler.Instance != null)
        {
            SkillDragDropHandler.Instance.UnregisterSkillSlot(this);
        }
    }

    private void OnPlayerSpawned(GameObject playerObject)
    {
        if (_isInitialized || playerObject == null) return;

        _skillActivationSystem = playerObject.GetComponent<SkillActivationSystem>();

        if (_skillActivationSystem != null)
        {
            Debug.Log($"[SkillSlotUI] SkillActivationSystem 찾음 (이벤트)");

            if (_slotIndex < 0)
            {
                _slotIndex = transform.GetSiblingIndex();
                Debug.Log($"[SkillSlotUI] {name}의 슬롯 인덱스 자동 설정: {_slotIndex}");
            }

            _isInitialized = true;
            InitializeDisplay();
        }
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        UpdateCooldownInternal();
    }

    #endregion

    #region 초기화

    public void Initialize(int slotIndex, SkillData skillData = null)
    {
        _slotIndex = slotIndex;
        _skillData = skillData;

        UpdateKeyBindDisplay();
        RefreshDisplay();

        Debug.Log($"[SkillSlotUI] 슬롯 {slotIndex} 초기화: {(_skillData != null ? _skillData.SkillName : "비어있음")}");
    }

    private void InitializeDisplay()
    {
        UpdateKeyBindDisplay();
        RefreshDisplay();
    }

    private void UpdateKeyBindDisplay()
    {
        if (_keyBindText != null && _skillActivationSystem != null && _slotIndex >= 0)
        {
            KeyCode assignedKey = _skillActivationSystem.GetSkillKey(_slotIndex);
            _keyBindText.text = assignedKey != KeyCode.None ? assignedKey.ToString() : "";
        }
    }

    #endregion

    #region 쿨다운 업데이트

    public void UpdateCooldown(float remainingTime, float maxCooldown)
    {
        _currentCooldown = remainingTime;

        if (maxCooldown <= 0f)
        {
            maxCooldown = 1f;
        }

        float cooldownPercent = Mathf.Clamp01(_currentCooldown / maxCooldown);

        if (_cooldownOverlay != null)
        {
            _cooldownOverlay.fillAmount = cooldownPercent;
        }

        if (_cooldownText != null)
        {
            int displayValue = Mathf.CeilToInt(_currentCooldown);
            if (displayValue != _lastDisplayedCooldownInt)
            {
                _lastDisplayedCooldownInt = displayValue;
                _cooldownText.text = displayValue > 0 ? displayValue.ToString() : "";
            }
        }

        RefreshIconColor();
    }

    private void UpdateCooldownInternal()
    {
        if (_currentCooldown > 0f)
        {
            _currentCooldown -= Time.deltaTime;

            if (_currentCooldown <= 0f)
            {
                _currentCooldown = 0f;
                _lastDisplayedCooldownInt = -1;
            }

            UpdateCooldownDisplay();
        }
    }

    private void UpdateCooldownDisplay()
    {
        if (_cooldownOverlay != null)
        {
            float fillAmount = _skillData != null && _skillData.Cooldown > 0
                ? _currentCooldown / _skillData.Cooldown
                : 0f;
            _cooldownOverlay.fillAmount = fillAmount;
        }

        if (_cooldownText != null)
        {
            int displayValue = Mathf.CeilToInt(_currentCooldown);
            if (displayValue != _lastDisplayedCooldownInt)
            {
                _lastDisplayedCooldownInt = displayValue;
                _cooldownText.text = displayValue > 0 ? displayValue.ToString() : "";
            }
        }
    }

    public void StartCooldown(float cooldownTime)
    {
        _currentCooldown = cooldownTime;
        _lastDisplayedCooldownInt = -1;
        UpdateCooldownDisplay();
    }

    #endregion

    #region 디스플레이

    public void RefreshDisplay()
    {
        bool hasSkill = _skillData != null;

        if (_skillIcon != null)
        {
            _skillIcon.enabled = hasSkill && _skillData.SkillIcon != null;

            if (_skillIcon.enabled)
            {
                _skillIcon.sprite = _skillData.SkillIcon;
            }
        }

        if (_lockedOverlay != null)
        {
            _lockedOverlay.gameObject.SetActive(_isLocked);
        }

        if (_emptyIndicator != null)
        {
            _emptyIndicator.SetActive(!hasSkill);
        }

        if (_manaCostText != null)
        {
            _manaCostText.text = hasSkill && _skillData.ManaCost > 0
                ? _skillData.ManaCost.ToString("F0") : "";
        }

        UpdateCooldownDisplay();
        RefreshIconColor();
    }

    private void RefreshIconColor()
    {
        if (_skillIcon == null || _isLocked || _skillData == null)
            return;

        if (IsOnCooldown)
        {
            _skillIcon.color = _onCooldownColor;
        }
        else
        {
            _skillIcon.color = _availableColor;
        }
    }

    public void SetInsufficientMana(bool insufficient)
    {
        if (_skillIcon == null || _isLocked || _skillData == null)
            return;

        if (insufficient)
        {
            _skillIcon.color = _insufficientManaColor;
        }
        else
        {
            RefreshIconColor();
        }
    }

    public void Lock()
    {
        _isLocked = true;
        RefreshDisplay();
    }

    public void Unlock()
    {
        _isLocked = false;
        RefreshDisplay();
    }

    public void FlashRed()
    {
        if (_skillIcon != null)
        {
            StartCoroutine(FlashRedCoroutine());
        }
    }

    private IEnumerator FlashRedCoroutine()
    {
        if (_skillIcon == null) yield break;

        Color original = _skillIcon.color;
        _skillIcon.color = Color.red;
        yield return WaitForSecondsCache.Get(0.2f);
        _skillIcon.color = original;
    }

    #endregion
    
    #region 드롭 이벤트 (스킬 트리에서)

    /// <summary>
    /// 스킬 드롭 이벤트 핸들러
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// 호출 시점
    /// ═══════════════════════════════════════════════════════════════════════
    /// 
    /// Unity EventSystem이 자동으로 호출:
    /// 1. IDropHandler 인터페이스 구현
    /// 2. 드래그 중인 오브젝트가 이 슬롯 위에서 드롭됨
    /// 3. Unity가 raycast로 hit 감지
    /// 4. 가장 위쪽(앞쪽) 오브젝트의 OnDrop 호출
    /// 
    /// 문제:
    /// Unity의 raycast 순서와 정확도 문제로
    /// 때때로 잘못된 슬롯에서 OnDrop이 호출됨
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// 처리 순서
    /// ═══════════════════════════════════════════════════════════════════════
    /// 
    /// [1단계] 잠금 체크
    ///    → 슬롯이 잠겨있으면 즉시 리턴
    /// 
    /// [2단계] 위치 검증 (중요)
    ///    → 마우스가 정말 이 슬롯 위에 있는가?
    ///    → RectTransformUtility.RectangleContainsScreenPoint()
    ///    → 없으면 OnDrop 무시 (잘못된 호출)
    /// 
    /// [3단계] 드래그 상태 확인
    ///    → SkillDragDropHandler가 드래그 중인가?
    ///    → 드래그 중인 스킬 데이터 가져오기
    /// 
    /// [4단계] 스킬 할당
    ///    → AssignSkill() 호출
    /// 
    /// [5단계] 핸들러에게 알림
    ///    → NotifyDropHandled() 호출
    ///    → EndDrag에서 중복 처리 방지
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// 위치 검증이 필요한 이유
    /// ═══════════════════════════════════════════════════════════════════════
    /// 
    /// Unity EventSystem 동작:
    /// - Hierarchy 역순으로 raycast (뒤쪽 오브젝트부터)
    /// - 겹치는 UI가 있으면 예측 불가능
    /// - 경계가 딱 맞물리면 오작동 가능
    /// 
    /// 우리 상황:
    /// SkillSlot_1 TR: (853.20, 76.00)  ← 오른쪽 경계
    /// SkillSlot_2 BL: (853.20, 12.00)  ← 왼쪽 경계
    /// → 1픽셀 겹침!
    /// 
    /// 마우스 (814, 65):
    /// - 명백히 Slot_1 영역 내부
    /// - 그러나 Slot_2.OnDrop() 호출됨
    /// 
    /// 재검증:
    /// RectTransformUtility.RectangleContainsScreenPoint()
    /// - 각 슬롯이 스스로 체크
    /// - "내 영역 내부에 마우스 있나?"
    /// - Slot_2: false → OnDrop 무시
    /// - Slot_1: true → 정상 처리
    /// 
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터 (마우스 위치 포함)</param>
    public void OnDrop(PointerEventData eventData)
    {
        if (_isLocked)
        {
            Debug.Log("[SkillSlotUI] 슬롯이 잠겨 있습니다.");
            return;
        }
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        bool isUnderMouse = RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera
        );

        if (!isUnderMouse)
        {
            Debug.LogWarning($"[SkillSlotUI] {name}에 OnDrop 호출되었지만 마우스는 슬롯 밖 - 무시");
            return;
        }
        
        if (SkillDragDropHandler.Instance != null && SkillDragDropHandler.Instance.IsDragging)
        {
            SkillData draggedSkill = SkillDragDropHandler.Instance.DraggedSkill;

            if (draggedSkill != null)
            {
                AssignSkill(draggedSkill);
                SkillDragDropHandler.Instance.NotifyDropHandled();
            }
        }
    }

    #endregion
    
    #region 클릭 이벤트

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RemoveSkill();
        }
    }

    #endregion

    #region 스킬 할당/제거

    public void AssignSkill(SkillData skillData)
    {
        if (skillData == null)
        {
            Debug.LogWarning("[SkillSlotUI] 할당하려는 SkillData가 null입니다!");
            return;
        }

        if (_skillData == skillData)
        {
            Debug.Log($"[SkillSlotUI] {name}에 이미 {skillData.SkillName}이(가) 할당되어 있습니다.");
            return;
        }

        if (_skillActivationSystem != null && _slotIndex >= 0)
        {
            if (_skillActivationSystem.HasSkill(skillData, _slotIndex))
            {
                SkillSlotUI existingSlot = _skillActivationSystem.FindSlotWithSkill(skillData);
                if (existingSlot != null && existingSlot != this)
                {
                    Debug.LogWarning($"[SkillSlotUI] {skillData.SkillName}은(는) 이미 슬롯 {existingSlot.SlotIndex}에 할당되어 있습니다!");
                    FlashRed();
                    return;
                }
            }
        }

        if (_skillData != null)
        {
            _currentCooldown = 0f;
            _lastDisplayedCooldownInt = -1;
        }

        _skillData = skillData;
        _isLocked = false;

        if (_skillIcon != null && _skillData.SkillIcon != null)
        {
            _skillIcon.sprite = _skillData.SkillIcon;
        }

        if (_manaCostText != null)
        {
            _manaCostText.text = _skillData.ManaCost.ToString("F0");
        }

        RefreshDisplay();
        Debug.Log($"[SkillSlotUI] 슬롯 {_slotIndex} ({name})에 {skillData.SkillName} 할당!");
    }

    public void RemoveSkill()
    {
        if (_skillData == null)
        {
            Debug.Log("[SkillSlotUI] 제거할 스킬이 없습니다.");
            return;
        }

        string removedSkillName = _skillData.SkillName;
        _skillData = null;
        _currentCooldown = 0f;
        _lastDisplayedCooldownInt = -1;

        if (_skillIcon != null)
        {
            _skillIcon.sprite = _emptySlotSprite;
        }

        if (_manaCostText != null)
        {
            _manaCostText.text = "";
        }

        RefreshDisplay();
        Debug.Log($"[SkillSlotUI] 슬롯 {_slotIndex}에서 {removedSkillName} 제거!");
    }

    #endregion

    #region 드래그 이벤트 (슬롯 간 드래그)

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_skillData == null)
        {
            eventData.pointerDrag = null;
            return;
        }

        _isDragging = true;

        if (SkillDragDropHandler.Instance != null)
        {
            SkillDragDropHandler.Instance.SetSlotDragActive(true);
        }

        CreateDragVisual();

        Debug.Log($"[SkillSlotUI] 슬롯 {_slotIndex}에서 {_skillData.SkillName} 드래그 시작");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragVisual != null)
        {
            _dragVisual.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        if (SkillDragDropHandler.Instance != null)
        {
            SkillDragDropHandler.Instance.SetSlotDragActive(false);
        }

        if (_dragVisual != null)
        {
            Destroy(_dragVisual);
            _dragVisual = null;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        SkillSlotUI targetSlot = null;

        foreach (RaycastResult result in results)
        {
            SkillSlotUI slot = result.gameObject.GetComponentInParent<SkillSlotUI>();

            if (slot != null && slot != this)
            {
                targetSlot = slot;
                break;
            }
        }

        if (targetSlot != null)
        {
            SwapSkills(targetSlot);
            Debug.Log($"[SkillSlotUI] 드롭 성공: 슬롯 {_slotIndex} ↔ 슬롯 {targetSlot.SlotIndex}");
        }
        else
        {
            Debug.Log($"[SkillSlotUI] 드래그 취소 (유효한 대상 없음)");
        }
    }

    private void CreateDragVisual()
    {
        if (_skillData == null || _skillData.SkillIcon == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        _dragVisual = new GameObject("DragVisual");
        _dragVisual.transform.SetParent(canvas.transform);
        _dragVisual.transform.SetAsLastSibling();

        Image img = _dragVisual.AddComponent<Image>();
        img.sprite = _skillData.SkillIcon;
        img.raycastTarget = false;

        RectTransform rt = _dragVisual.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50, 50);

        Color color = img.color;
        color.a = 0.6f;
        img.color = color;
    }

    private void SwapSkills(SkillSlotUI targetSlot)
    {
        if (targetSlot == null) return;

        SkillData thisSkill = this._skillData;
        SkillData targetSkill = targetSlot._skillData;

        Debug.Log($"[SkillSlotUI] 교환 시작: 슬롯 {_slotIndex}({thisSkill?.SkillName ?? "Empty"}) ↔ 슬롯 {targetSlot._slotIndex}({targetSkill?.SkillName ?? "Empty"})");

        this._skillData = targetSkill;
        targetSlot._skillData = thisSkill;

        this._currentCooldown = 0f;
        targetSlot._currentCooldown = 0f;

        this.RefreshDisplay();
        targetSlot.RefreshDisplay();

        Debug.Log($"[SkillSlotUI] 교환 완료!");
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug: Print Slot Info")]
    private void DebugPrintInfo()
    {
        Debug.Log("===== SkillSlotUI 정보 =====");
        Debug.Log($"GameObject: {name}");
        Debug.Log($"슬롯 인덱스: {_slotIndex}");
        Debug.Log($"할당된 스킬: {_skillData?.SkillName ?? "없음"}");
        Debug.Log($"잠금 상태: {_isLocked}");
        Debug.Log($"쿨다운: {_currentCooldown:F2}초");
        Debug.Log($"드래그 중: {_isDragging}");
        Debug.Log($"초기화 완료: {_isInitialized}");
    }

    #endregion
}
