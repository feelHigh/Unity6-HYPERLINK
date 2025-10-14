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
/// 
/// UI 구성:
/// - Skill Icon: 스킬 아이콘 이미지
/// - Cooldown Overlay: 쿨다운 중 어두운 오버레이 (Fill Amount)
/// - Cooldown Text: 남은 쿨다운 시간 (초)
/// - Mana Cost Text: 마나 소비량
/// - Locked Overlay: 잠금 상태 표시
/// 
/// 상태 변화:
/// 1. 잠금 (Locked): 회색, 사용 불가
/// 2. 사용 가능 (Available): 밝은 색, 클릭 가능
/// 3. 쿨다운 (Cooldown): 어두운 색 + 타이머
/// 4. 마나 부족 (Insufficient Mana): 빨간색
/// 
/// 작동 흐름:
/// 1. Initialize(): 스킬 데이터 할당
/// 2. Unlock(): 레벨업 시 언락
/// 3. StartCooldown(): 스킬 사용 시 호출
/// 4. Update(): 매 프레임 쿨다운 감소
/// 5. SetInsufficientMana(): 마나 체크
/// 
/// SkillActivationSystem 연동:
/// - RegisterSkillSlot()로 등록
/// - 쿨다운 시작 시 자동 알림
/// - UI와 로직 동기화
/// 
/// Diablo 3 스타일:
/// - 쿨다운 시계 방향 애니메이션
/// - 남은 시간 숫자 표시
/// - 마나 부족 시 빨간색 표시
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    #region UI 참조

    [Header("UI 참조")]
    [SerializeField] private Image _skillIcon;                 // 스킬 아이콘
    [SerializeField] private Image _cooldownOverlay;           // 쿨다운 오버레이 (Fill Amount)
    [SerializeField] private TextMeshProUGUI _cooldownText;    // 쿨다운 남은 시간
    [SerializeField] private TextMeshProUGUI _manaCostText;    // 마나 소비량
    [SerializeField] private Image _lockedOverlay;             // 잠금 상태 오버레이

    #endregion

    #region 시각 설정

    [Header("시각 설정")]
    [SerializeField] private Color _availableColor = Color.white;                      // 사용 가능 색상
    [SerializeField] private Color _onCooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 쿨다운 중 색상 (회색)
    [SerializeField] private Color _insufficientManaColor = new Color(1f, 0.3f, 0.3f, 1f); // 마나 부족 색상 (빨간색)

    #endregion

    // 내부 상태
    private SkillData _skillData;          // 할당된 스킬 데이터
    private bool _isLocked = true;         // 잠금 상태 (언락 전)
    private float _currentCooldown = 0f;   // 현재 남은 쿨다운 시간

    // Public 프로퍼티
    public SkillData SkillData => _skillData;
    public bool IsOnCooldown => _currentCooldown > 0f;
    public bool IsLocked => _isLocked;

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
    /// 4. 잠금 상태 UI 업데이트
    /// 
    /// Parameters:
    ///     skillData: 할당할 스킬 데이터
    ///     
    /// 호출 시점:
    /// - 게임 시작 시 (이미 언락된 스킬)
    /// - 레벨업 시 (새로 언락된 스킬)
    /// </summary>
    public void Initialize(SkillData skillData)
    {
        _skillData = skillData;

        // 스킬 아이콘 설정
        if (_skillIcon != null && skillData != null)
        {
            _skillIcon.sprite = skillData.SkillIcon;
        }

        // 마나 소비량 표시
        if (_manaCostText != null && skillData != null)
        {
            _manaCostText.text = skillData.ManaCost.ToString("F0"); // 정수로 표시
        }

        // 잠금 상태 UI 업데이트
        UpdateLockedState();
    }

    /// <summary>
    /// 스킬 슬롯 언락
    /// 
    /// PlayerCharacter.UnlockSkillsForLevel() 후 호출됨
    /// 
    /// 효과:
    /// - 잠금 오버레이 제거
    /// - 아이콘 색상 밝게 변경
    /// - 사용 가능 상태로 전환
    /// </summary>
    public void Unlock()
    {
        _isLocked = false;
        UpdateLockedState();
    }

    /// <summary>
    /// 쿨다운 시작
    /// 
    /// SkillActivationSystem.StartCooldown()에서 호출됨
    /// 
    /// 처리 과정:
    /// 1. SkillData에서 쿨다운 시간 가져오기
    /// 2. _currentCooldown에 설정
    /// 3. Update()에서 매 프레임 감소
    /// 4. 0 도달 시 쿨다운 완료
    /// </summary>
    public void StartCooldown()
    {
        if (_skillData != null)
        {
            _currentCooldown = _skillData.Cooldown;
        }
    }

    /// <summary>
    /// 마나 부족 상태 설정
    /// 
    /// CharacterUIController.UpdateSkillManaStatus()에서 매 프레임 호출
    /// 
    /// Parameters:
    ///     insufficient: true면 마나 부족 (빨간색), false면 정상 (흰색)
    ///     
    /// 시각적 피드백:
    /// - 마나 부족: 아이콘 빨간색
    /// - 마나 충분: 아이콘 흰색
    /// - 플레이어에게 즉각적인 피드백
    /// </summary>
    public void SetInsufficientMana(bool insufficient)
    {
        if (_skillIcon != null && !_isLocked)
        {
            _skillIcon.color = insufficient ? _insufficientManaColor : _availableColor;
        }
    }

    /// <summary>
    /// 쿨다운 업데이트 (매 프레임)
    /// 
    /// 처리 과정:
    /// 1. 남은 쿨다운 시간 감소
    /// 2. Cooldown Overlay 업데이트 (Fill Amount)
    /// 3. Cooldown Text 업데이트 (초 단위)
    /// 4. 아이콘 색상 변경 (쿨다운 중 = 어두움)
    /// 5. 쿨다운 완료 시 UI 정리
    /// 
    /// Fill Amount 계산:
    /// - fillAmount = 남은 시간 / 총 시간
    /// - 예: 3초 남음 / 10초 = 0.3 (30%)
    /// - 시간이 지나면 Fill Amount 감소 (시계 방향 애니메이션)
    /// </summary>
    private void UpdateCooldown()
    {
        // === 쿨다운 진행 중 ===
        if (_currentCooldown > 0f)
        {
            // 시간 감소 (프레임 독립적)
            _currentCooldown -= Time.deltaTime;

            // Cooldown Overlay 업데이트
            if (_cooldownOverlay != null)
            {
                // Fill Amount 계산 (남은 비율)
                _cooldownOverlay.fillAmount = _skillData != null ? _currentCooldown / _skillData.Cooldown : 0f;
                _cooldownOverlay.gameObject.SetActive(true);  // 보이기
            }

            // Cooldown Text 업데이트
            if (_cooldownText != null)
            {
                // 정수로 올림하여 표시 (예: 3.7초 → 4초)
                _cooldownText.text = Mathf.Ceil(_currentCooldown).ToString("F0");
                _cooldownText.gameObject.SetActive(true);  // 보이기
            }

            // 아이콘 어둡게
            if (_skillIcon != null)
            {
                _skillIcon.color = _onCooldownColor;
            }
        }
        // === 쿨다운 완료 ===
        else
        {
            // Cooldown Overlay 숨기기
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.gameObject.SetActive(false);
            }

            // Cooldown Text 숨기기
            if (_cooldownText != null)
            {
                _cooldownText.gameObject.SetActive(false);
            }

            // 아이콘 밝게 (잠금 상태가 아니면)
            if (_skillIcon != null && !_isLocked)
            {
                _skillIcon.color = _availableColor;
            }
        }
    }

    /// <summary>
    /// 잠금 상태 UI 업데이트
    /// 
    /// 잠금 상태 (스킬 언락 전):
    /// - Locked Overlay 활성화
    /// - 아이콘 어둡게
    /// 
    /// 언락 상태 (스킬 사용 가능):
    /// - Locked Overlay 비활성화
    /// - 아이콘 밝게
    /// </summary>
    private void UpdateLockedState()
    {
        // Locked Overlay 표시/숨김
        if (_lockedOverlay != null)
        {
            _lockedOverlay.gameObject.SetActive(_isLocked);
        }

        // 아이콘 색상
        if (_skillIcon != null)
        {
            _skillIcon.color = _isLocked ? _onCooldownColor : _availableColor;
        }
    }
}