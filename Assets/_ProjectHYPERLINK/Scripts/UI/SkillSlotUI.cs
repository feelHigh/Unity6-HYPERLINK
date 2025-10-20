using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스킬 슬롯 UI 컴포넌트 (Q/W/E 키)
/// 
/// 핵심 기능:
/// - 스킬 아이콘 표시
/// - 쿨다운 시각화 (Fill Amount + 텍스트)
/// - 마나 부족 시각적 피드백 (빨간색)
/// - 잠금/언락 상태 표시
/// - 키 바인드 표시 (신규)
/// 
/// UI 구성:
/// - Skill Icon: 스킬 아이콘 이미지
/// - Cooldown Overlay: 쿨다운 중 어두운 오버레이 (Fill Amount)
/// - Cooldown Text: 남은 쿨다운 시간 (초)
/// - Mana Cost Text: 마나 소비량
/// - Key Bind Text: 할당된 키 (Q/W/E)
/// - Locked Overlay: 잠금 상태 표시
/// 
/// 최근 변경사항:
/// - 키 바인드 표시 기능 추가
/// - SkillActivationSystem과 연동
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    #region UI 참조

    [Header("UI 참조")]
    [SerializeField] private Image _skillIcon;                 // 스킬 아이콘
    [SerializeField] private Image _cooldownOverlay;           // 쿨다운 오버레이 (Fill Amount)
    [SerializeField] private TextMeshProUGUI _cooldownText;    // 쿨다운 남은 시간
    [SerializeField] private TextMeshProUGUI _manaCostText;    // 마나 소비량
    [SerializeField] private TextMeshProUGUI _keyBindText;     // 키 바인드 표시 (신규)
    [SerializeField] private Image _lockedOverlay;             // 잠금 상태 오버레이

    #endregion

    #region 시각 설정

    [Header("시각 설정")]
    [SerializeField] private Color _availableColor = Color.white;                      // 사용 가능 색상
    [SerializeField] private Color _onCooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 쿨다운 중 색상 (회색)
    [SerializeField] private Color _insufficientManaColor = new Color(1f, 0.3f, 0.3f, 1f); // 마나 부족 색상 (빨간색)

    #endregion

    #region 참조

    [Header("시스템 참조")]
    [SerializeField] private SkillActivationSystem _skillActivationSystem;

    #endregion

    // 내부 상태
    private SkillData _skillData;          // 할당된 스킬 데이터
    private bool _isLocked = true;         // 잠금 상태 (언락 전)
    private float _currentCooldown = 0f;   // 현재 남은 쿨다운 시간
    private int _slotIndex = -1;           // 슬롯 인덱스 (0, 1, 2)

    // Public 프로퍼티
    public SkillData SkillData => _skillData;
    public bool IsOnCooldown => _currentCooldown > 0f;
    public bool IsLocked => _isLocked;

    private void Awake()
    {
        // SkillActivationSystem 자동 검색
        if (_skillActivationSystem == null)
        {
            _skillActivationSystem = FindObjectOfType<SkillActivationSystem>();
        }
    }

    private void Update()
    {
        // 매 프레임 쿨다운 업데이트
        UpdateCooldown();
    }

    /// <summary>
    /// 스킬 슬롯 초기화
    /// 
    /// CharacterUIController.InitializeSkillSlots()에서 호출
    /// 
    /// 처리 과정:
    /// 1. SkillData 할당
    /// 2. 스킬 아이콘 설정
    /// 3. 마나 소비량 텍스트 설정
    /// 4. 키 바인드 텍스트 설정 (신규)
    /// 5. 초기 상태 UI 업데이트
    /// </summary>
    public void Initialize(SkillData skillData, int slotIndex)
    {
        _skillData = skillData;
        _slotIndex = slotIndex;

        if (_skillData != null)
        {
            // 스킬 아이콘 설정
            if (_skillIcon != null && _skillData.SkillIcon != null)
            {
                _skillIcon.sprite = _skillData.SkillIcon;
            }

            // 마나 소비량 표시
            if (_manaCostText != null)
            {
                _manaCostText.text = _skillData.ManaCost.ToString("F0");
            }

            // 키 바인드 표시 (신규)
            UpdateKeyBindDisplay();
        }

        // 초기 상태 설정
        RefreshDisplay();
    }

    /// <summary>
    /// 키 바인드 표시 업데이트 (신규)
    /// 
    /// SkillActivationSystem에서 할당된 키를 가져와 표시
    /// </summary>
    private void UpdateKeyBindDisplay()
    {
        if (_keyBindText != null && _skillActivationSystem != null && _slotIndex >= 0)
        {
            KeyCode assignedKey = _skillActivationSystem.GetSkillKey(_slotIndex);

            if (assignedKey != KeyCode.None)
            {
                _keyBindText.text = assignedKey.ToString();
            }
            else
            {
                _keyBindText.text = "";
            }
        }
    }

    /// <summary>
    /// 스킬 언락
    /// 
    /// PlayerCharacter.UnlockSkillsForLevel()에서 호출
    /// </summary>
    public void Unlock()
    {
        _isLocked = false;

        if (_lockedOverlay != null)
        {
            _lockedOverlay.gameObject.SetActive(false);
        }

        RefreshDisplay();
        Debug.Log($"[SkillSlotUI] {_skillData?.SkillName} 언락!");
    }

    /// <summary>
    /// 쿨다운 시작
    /// 
    /// SkillActivationSystem.StartCooldown()에서 호출
    /// </summary>
    public void StartCooldown()
    {
        if (_skillData != null)
        {
            _currentCooldown = _skillData.Cooldown;
        }
    }

    /// <summary>
    /// 쿨다운 업데이트 (매 프레임)
    /// 
    /// 처리:
    /// - 쿨다운 시간 감소
    /// - Fill Amount 업데이트
    /// - 쿨다운 텍스트 업데이트
    /// </summary>
    private void UpdateCooldown()
    {
        if (_currentCooldown > 0f)
        {
            _currentCooldown -= Time.deltaTime;

            if (_currentCooldown < 0f)
            {
                _currentCooldown = 0f;
            }

            // 쿨다운 UI 업데이트
            UpdateCooldownDisplay();
        }
        else if (_cooldownOverlay != null && _cooldownOverlay.fillAmount > 0f)
        {
            // 쿨다운 종료 시 오버레이 제거
            _cooldownOverlay.fillAmount = 0f;
            if (_cooldownText != null)
            {
                _cooldownText.text = "";
            }
            RefreshIconColor();
        }
    }

    /// <summary>
    /// 쿨다운 시각 업데이트
    /// 
    /// Fill Amount:
    /// - 1.0 = 쿨다운 시작 (완전히 가려짐)
    /// - 0.0 = 쿨다운 종료 (완전히 보임)
    /// </summary>
    private void UpdateCooldownDisplay()
    {
        if (_skillData == null)
            return;

        float cooldownPercent = _currentCooldown / _skillData.Cooldown;

        // Fill Amount 업데이트
        if (_cooldownOverlay != null)
        {
            _cooldownOverlay.fillAmount = cooldownPercent;
        }

        // 텍스트 업데이트 (1초 이상만 표시)
        if (_cooldownText != null)
        {
            if (_currentCooldown >= 1f)
            {
                _cooldownText.text = Mathf.Ceil(_currentCooldown).ToString("F0");
            }
            else
            {
                _cooldownText.text = "";
            }
        }
    }

    /// <summary>
    /// UI 표시 새로고침
    /// 
    /// 호출 시점:
    /// - 초기화 시
    /// - 언락 시
    /// - 레벨업 시
    /// </summary>
    public void RefreshDisplay()
    {
        if (_skillData == null)
            return;

        // 잠금 상태 UI
        if (_lockedOverlay != null)
        {
            _lockedOverlay.gameObject.SetActive(_isLocked);
        }

        // 아이콘 색상
        RefreshIconColor();

        // 키 바인드 (언락 시에만 표시)
        if (_keyBindText != null)
        {
            _keyBindText.gameObject.SetActive(!_isLocked);
        }
    }

    /// <summary>
    /// 아이콘 색상 업데이트
    /// 
    /// 상태별 색상:
    /// - 사용 가능: 흰색
    /// - 쿨다운: 회색
    /// - 마나 부족: 빨간색
    /// </summary>
    private void RefreshIconColor()
    {
        if (_skillIcon == null || _isLocked)
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

    /// <summary>
    /// 마나 부족 상태 설정
    /// 
    /// SkillActivationSystem에서 마나 체크 실패 시 호출 가능
    /// </summary>
    public void SetInsufficientMana(bool insufficient)
    {
        if (_skillIcon == null || _isLocked)
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

    /// <summary>
    /// 빨간색 플래시 효과 (마나 부족 피드백)
    /// 
    /// SkillActivationSystem.ShowManaCostWarning()에서 호출 가능
    /// </summary>
    public void FlashRed()
    {
        StartCoroutine(FlashRedCoroutine());
    }

    private System.Collections.IEnumerator FlashRedCoroutine()
    {
        Color originalColor = _skillIcon.color;
        _skillIcon.color = _insufficientManaColor;

        yield return new UnityEngine.WaitForSeconds(0.2f);

        _skillIcon.color = originalColor;
    }

    #region 디버그

    /// <summary>
    /// 디버그: 슬롯 정보 출력
    /// </summary>
    [ContextMenu("Debug: Print Slot Info")]
    private void DebugPrintInfo()
    {
        if (_skillData != null)
        {
            Debug.Log($"===== Skill Slot {_slotIndex} =====");
            Debug.Log($"스킬: {_skillData.SkillName}");
            Debug.Log($"잠금: {_isLocked}");
            Debug.Log($"쿨다운: {_currentCooldown:F1}초");

            if (_skillActivationSystem != null)
            {
                KeyCode key = _skillActivationSystem.GetSkillKey(_slotIndex);
                Debug.Log($"키 바인드: {key}");
            }
        }
        else
        {
            Debug.Log($"Skill Slot {_slotIndex}: 스킬 미할당");
        }
    }

    #endregion
}
