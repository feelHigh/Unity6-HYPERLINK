using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스킬 슬롯 UI 컴포넌트 (Q/W/E/R)
/// 
/// 기능:
/// - 중복 스킬 방지
/// - 슬롯 간 드래그 앤 드롭 지원
/// - 드래그 앤 드롭으로 스킬 할당
/// - 우클릭으로 스킬 제거
/// - 쿨다운 시각화
/// - 마나 부족 피드백
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

    [Header("자동 검색 설정")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _retryInterval = 0.5f;
    [SerializeField] private int _maxRetries = 20;

    private bool _isInitialized = false;
    private int _retryCount = 0;

    [Header("시스템 참조 (자동 검색)")]
    private SkillActivationSystem _skillActivationSystem;

    #endregion

    // 내부 상태
    private SkillData _skillData;
    private bool _isLocked = false;
    private float _currentCooldown = 0f;
    private int _slotIndex = -1;

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

    private void Start()
    {
        InvokeRepeating(nameof(TryFindSkillActivationSystem), 0.1f, _retryInterval);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(TryFindSkillActivationSystem));

        if (SkillDragDropHandler.Instance != null)
        {
            SkillDragDropHandler.Instance.UnregisterSkillSlot(this);
        }
    }

    private void TryFindSkillActivationSystem()
    {
        _retryCount++;

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _skillActivationSystem = playerObject.GetComponent<SkillActivationSystem>();

            if (_skillActivationSystem != null)
            {
                Debug.Log($"[SkillSlotUI] SkillActivationSystem 찾음 (슬롯 {_slotIndex}, 시도: {_retryCount}회)");
                _isInitialized = true;
                CancelInvoke(nameof(TryFindSkillActivationSystem));
                return;
            }
        }

        if (_retryCount >= _maxRetries)
        {
            Debug.LogError($"[SkillSlotUI] SkillActivationSystem를 {_maxRetries}회 시도 후에도 찾지 못했습니다!");
            CancelInvoke(nameof(TryFindSkillActivationSystem));
        }
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

    private void UpdateKeyBindDisplay()
    {
        if (_keyBindText != null && _skillActivationSystem != null && _slotIndex >= 0)
        {
            KeyCode assignedKey = _skillActivationSystem.GetSkillKey(_slotIndex);
            _keyBindText.text = assignedKey != KeyCode.None ? assignedKey.ToString() : "";
        }
    }

    #endregion

    #region 스킬 할당/제거

    /// <summary>
    /// 스킬 할당 (중복 체크 포함)
    /// </summary>
    public void AssignSkill(SkillData skillData)
    {
        if (skillData == null)
        {
            Debug.LogWarning("[SkillSlotUI] 할당하려는 SkillData가 null입니다!");
            return;
        }

        // 이미 동일한 스킬이 할당되어 있으면 무시
        if (_skillData == skillData)
        {
            Debug.Log($"[SkillSlotUI] 이미 {skillData.SkillName}이(가) 할당되어 있습니다.");
            return;
        }

        // 중복 체크: 다른 슬롯에 이미 있는지 확인
        if (_skillActivationSystem != null && _skillActivationSystem.HasSkill(skillData, _slotIndex))
        {
            SkillSlotUI existingSlot = _skillActivationSystem.FindSlotWithSkill(skillData);
            if (existingSlot != null)
            {
                Debug.LogWarning($"[SkillSlotUI] {skillData.SkillName}은(는) 이미 슬롯 {existingSlot.SlotIndex}에 할당되어 있습니다!");
                FlashRed(); // 시각적 피드백
                return;
            }
        }

        // 기존 스킬 쿨다운 정리
        if (_skillData != null)
        {
            _currentCooldown = 0f;
        }

        // 새 스킬 할당
        _skillData = skillData;
        _isLocked = false;

        // UI 업데이트
        if (_skillIcon != null && _skillData.SkillIcon != null)
        {
            _skillIcon.sprite = _skillData.SkillIcon;
        }

        if (_manaCostText != null)
        {
            _manaCostText.text = _skillData.ManaCost.ToString("F0");
        }

        RefreshDisplay();
        Debug.Log($"[SkillSlotUI] 슬롯 {_slotIndex}에 {skillData.SkillName} 할당!");
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

    /// <summary>
    /// 드래그 시작
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 빈 슬롯이면 드래그 불가
        if (_skillData == null)
        {
            eventData.pointerDrag = null;
            return;
        }

        _isDragging = true;

        // SkillDragDropHandler 간섭 방지
        if (SkillDragDropHandler.Instance != null)
        {
            SkillDragDropHandler.Instance.SetSlotDragActive(true);
        }

        // 드래그 비주얼 생성
        CreateDragVisual();

        Debug.Log($"[SkillSlotUI] 슬롯 {_slotIndex}에서 {_skillData.SkillName} 드래그 시작");
    }

    /// <summary>
    /// 드래그 중
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_dragVisual != null)
        {
            _dragVisual.transform.position = eventData.position;
        }
    }

    /// <summary>
    /// 드래그 종료
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        // SkillDragDropHandler 재활성화
        if (SkillDragDropHandler.Instance != null)
        {
            SkillDragDropHandler.Instance.SetSlotDragActive(false);
        }

        // 드래그 비주얼 제거
        if (_dragVisual != null)
        {
            Destroy(_dragVisual);
            _dragVisual = null;
        }

        // Raycast로 정확한 드롭 대상 찾기
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 거리 순으로 정렬 (가장 가까운 것부터)
        results.Sort((a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastResult result in results)
        {
            // 부모까지 확인
            SkillSlotUI targetSlot = result.gameObject.GetComponentInParent<SkillSlotUI>();

            if (targetSlot != null && targetSlot != this)
            {
                SwapSkills(targetSlot);
                Debug.Log($"[SkillSlotUI] 드롭 성공: 슬롯 {_slotIndex} → 슬롯 {targetSlot.SlotIndex}");
                return;
            }
        }

        Debug.Log($"[SkillSlotUI] 드래그 취소 (유효한 대상 없음)");
    }

    /// <summary>
    /// 드래그 비주얼 생성
    /// </summary>
    private void CreateDragVisual()
    {
        if (_skillData == null || _skillData.SkillIcon == null) return;

        // 캔버스 찾기
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // 드래그 비주얼 생성
        _dragVisual = new GameObject("DragVisual");
        _dragVisual.transform.SetParent(canvas.transform);
        _dragVisual.transform.SetAsLastSibling();

        // 이미지 추가
        Image img = _dragVisual.AddComponent<Image>();
        img.sprite = _skillData.SkillIcon;
        img.raycastTarget = false;

        // 크기 설정
        RectTransform rt = _dragVisual.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50, 50);

        // 반투명
        Color color = img.color;
        color.a = 0.6f;
        img.color = color;
    }

    /// <summary>
    /// 두 슬롯의 스킬 교환
    /// </summary>
    private void SwapSkills(SkillSlotUI targetSlot)
    {
        if (targetSlot == null) return;

        SkillData thisSkill = this._skillData;
        SkillData targetSkill = targetSlot._skillData;

        Debug.Log($"[SkillSlotUI] 교환 시작: 슬롯 {_slotIndex}({thisSkill?.SkillName ?? "Empty"}) ↔ 슬롯 {targetSlot._slotIndex}({targetSkill?.SkillName ?? "Empty"})");

        // 교환
        this.RemoveSkill();
        targetSlot.RemoveSkill();

        if (thisSkill != null)
        {
            targetSlot.AssignSkill(thisSkill);
        }

        if (targetSkill != null)
        {
            this.AssignSkill(targetSkill);
        }

        Debug.Log($"[SkillSlotUI] 교환 완료: 슬롯 {_slotIndex} ↔ 슬롯 {targetSlot._slotIndex}");
    }

    #endregion

    #region 드롭 이벤트 (스킬 트리에서 드롭)

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[SkillSlotUI] 슬롯 {_slotIndex}에 드롭 감지");
        // SkillDragDropHandler가 처리
    }

    #endregion

    #region 우클릭 제거

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RemoveSkill();
        }
    }

    #endregion

    #region 기존 기능

    public void Unlock()
    {
        _isLocked = false;
        if (_lockedOverlay != null)
        {
            _lockedOverlay.gameObject.SetActive(false);
        }
        RefreshDisplay();
    }

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
            if (_currentCooldown >= 1f)
            {
                _cooldownText.text = Mathf.Ceil(_currentCooldown).ToString("F0");
            }
            else if (_currentCooldown > 0f)
            {
                _cooldownText.text = _currentCooldown.ToString("F1");
            }
            else
            {
                _cooldownText.text = "";
            }
        }

        RefreshIconColor();
    }

    public void RefreshDisplay()
    {
        if (_emptyIndicator != null)
        {
            _emptyIndicator.SetActive(_skillData == null);
        }

        if (_skillIcon != null)
        {
            _skillIcon.enabled = (_skillData != null && _skillData.SkillIcon != null);

            if (_skillIcon.enabled)
            {
                _skillIcon.sprite = _skillData.SkillIcon;
            }
            else if (_emptySlotSprite != null)
            {
                _skillIcon.sprite = _emptySlotSprite;
                _skillIcon.enabled = true;
            }
        }

        if (_manaCostText != null)
        {
            _manaCostText.text = _skillData != null ? _skillData.ManaCost.ToString("F0") : "";
        }

        if (_lockedOverlay != null)
        {
            _lockedOverlay.gameObject.SetActive(_isLocked);
        }

        RefreshIconColor();

        if (_keyBindText != null)
        {
            _keyBindText.gameObject.SetActive(true);
        }

        if (_cooldownOverlay != null)
        {
            _cooldownOverlay.fillAmount = 0f;
        }

        if (_cooldownText != null)
        {
            _cooldownText.text = "";
        }
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

    public void FlashRed()
    {
        StartCoroutine(FlashRedCoroutine());
    }

    private IEnumerator FlashRedCoroutine()
    {
        Color originalColor = _skillIcon.color;
        _skillIcon.color = _insufficientManaColor;
        yield return new WaitForSeconds(0.2f);
        _skillIcon.color = originalColor;
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug: Print Slot Info")]
    private void DebugPrintInfo()
    {
        Debug.Log($"===== Skill Slot {_slotIndex} =====");
        Debug.Log($"스킬: {(_skillData != null ? _skillData.SkillName : "비어있음")}");
        Debug.Log($"잠금: {_isLocked}");
        Debug.Log($"쿨다운: {_currentCooldown:F1}초");

        if (_skillActivationSystem != null)
        {
            KeyCode key = _skillActivationSystem.GetSkillKey(_slotIndex);
            Debug.Log($"키 바인드: {key}");
        }
    }

    #endregion
}
