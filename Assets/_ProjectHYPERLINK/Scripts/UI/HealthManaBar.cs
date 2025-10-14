using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 체력/마나 바 UI 컴포넌트
/// 
/// 핵심 기능:
/// - 실시간 체력/마나 표시
/// - 부드러운 바 애니메이션 (Lerp)
/// - 텍스트 표시 (현재/최대)
/// - 이벤트 기반 업데이트
/// 
/// UI 구성:
/// - Fill Image: 바의 채워진 부분 (Fill Amount 0~1)
/// - Background Image: 바의 배경
/// - Text: "150 / 200" 형식
/// 
/// 작동 방식:
/// 1. PlayerCharacter가 체력/마나 변경 이벤트 발생
/// 2. UpdateHealthBar/UpdateManaBar 메서드 호출
/// 3. 목표 Fill Amount 설정
/// 4. Update()에서 매 프레임 Lerp로 부드럽게 전환
/// 5. 텍스트도 함께 업데이트
/// 
/// Diablo 3 스타일:
/// - Globe 형태 (구체) 대신 바 형태 사용
/// - 부드러운 애니메이션
/// - 명확한 수치 표시
/// 
/// 설정 방법:
/// 1. Canvas에 Image 컴포넌트 추가 (체력 바)
/// 2. Image Type: Filled, Fill Method: Horizontal
/// 3. 이 스크립트 컴포넌트 추가
/// 4. Inspector에서 참조 할당
/// </summary>
public class HealthManaBar : MonoBehaviour
{
    #region 체력 바

    [Header("체력 바")]
    [SerializeField] private Image _healthFillImage;           // 체력 바 Fill 이미지
    [SerializeField] private Image _healthBackgroundImage;     // 체력 바 배경
    [SerializeField] private TextMeshProUGUI _healthText;      // 체력 텍스트 (예: "150 / 200")

    #endregion

    #region 마나 바

    [Header("마나 바")]
    [SerializeField] private Image _manaFillImage;             // 마나 바 Fill 이미지
    [SerializeField] private Image _manaBackgroundImage;       // 마나 바 배경
    [SerializeField] private TextMeshProUGUI _manaText;        // 마나 텍스트

    #endregion

    #region 애니메이션 설정

    [Header("애니메이션 설정")]
    [SerializeField] private float _fillSpeed = 5f;            // 바 채움 속도 (높을수록 빠름)
    [SerializeField] private bool _useTextDisplay = true;      // 텍스트 표시 여부

    #endregion

    // 목표 Fill Amount (0~1)
    private float _targetHealthFill = 1f;  // 체력 100%로 시작
    private float _targetManaFill = 1f;    // 마나 100%로 시작

    /// <summary>
    /// 이벤트 구독
    /// 
    /// PlayerCharacter의 이벤트 구독:
    /// - OnHealthChanged: 체력 변경 시
    /// - OnManaChanged: 마나 변경 시
    /// 
    /// OnEnable에서 구독하는 이유:
    /// - 오브젝트 활성화 시 자동 구독
    /// - OnDisable에서 해제하여 메모리 누수 방지
    /// </summary>
    private void OnEnable()
    {
        PlayerCharacter.OnHealthChanged += UpdateHealthBar;
        PlayerCharacter.OnManaChanged += UpdateManaBar;
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// 
    /// 중요: 반드시 OnDisable에서 해제해야 함
    /// 이유: 메모리 누수 방지
    /// </summary>
    private void OnDisable()
    {
        PlayerCharacter.OnHealthChanged -= UpdateHealthBar;
        PlayerCharacter.OnManaChanged -= UpdateManaBar;
    }

    /// <summary>
    /// 매 프레임 바 애니메이션 업데이트
    /// 
    /// Lerp를 사용한 부드러운 전환:
    /// - 현재 값 → 목표 값으로 서서히 이동
    /// - _fillSpeed로 속도 조절
    /// - 급격한 변화 방지 (시각적으로 부드러움)
    /// </summary>
    private void Update()
    {
        SmoothFillBars();
    }

    /// <summary>
    /// 체력 바 업데이트 (이벤트 핸들러)
    /// 
    /// PlayerCharacter.OnHealthChanged 이벤트에서 호출됨
    /// 
    /// Parameters:
    ///     current: 현재 체력 (예: 75)
    ///     max: 최대 체력 (예: 100)
    ///     
    /// 처리 과정:
    /// 1. 체력 비율 계산 (current / max)
    /// 2. 목표 Fill Amount 설정
    /// 3. Update()에서 Lerp로 부드럽게 전환
    /// 4. 텍스트 업데이트 (옵션)
    /// 
    /// 예시:
    /// - 현재: 75, 최대: 100
    /// - Fill Amount: 0.75 (75%)
    /// - 텍스트: "75 / 100"
    /// </summary>
    private void UpdateHealthBar(float current, float max)
    {
        // 체력 비율 계산 (0~1)
        // max가 0이면 0 반환 (0으로 나누기 방지)
        _targetHealthFill = max > 0 ? current / max : 0f;

        // 텍스트 업데이트 (옵션)
        if (_useTextDisplay && _healthText != null)
        {
            // 정수로 반올림하여 표시
            _healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    /// <summary>
    /// 마나 바 업데이트 (이벤트 핸들러)
    /// 
    /// PlayerCharacter.OnManaChanged 이벤트에서 호출됨
    /// 
    /// 체력 바와 동일한 로직
    /// 
    /// Parameters:
    ///     current: 현재 마나
    ///     max: 최대 마나
    /// </summary>
    private void UpdateManaBar(float current, float max)
    {
        // 마나 비율 계산
        _targetManaFill = max > 0 ? current / max : 0f;

        // 텍스트 업데이트
        if (_useTextDisplay && _manaText != null)
        {
            _manaText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    /// <summary>
    /// 부드러운 바 채우기 애니메이션
    /// 
    /// Lerp (Linear Interpolation):
    /// - 현재 값에서 목표 값으로 선형 보간
    /// - 매 프레임 일정 비율씩 이동
    /// - Time.deltaTime으로 프레임 독립적 속도
    /// 
    /// 공식:
    /// newValue = Lerp(currentValue, targetValue, Time.deltaTime * speed)
    /// 
    /// 효과:
    /// - 급격한 변화 방지
    /// - 부드러운 시각적 피드백
    /// - 플레이어 경험 향상
    /// 
    /// 속도 조절:
    /// - _fillSpeed = 1: 느림
    /// - _fillSpeed = 5: 보통 (권장)
    /// - _fillSpeed = 10: 빠름
    /// - _fillSpeed = 100: 거의 즉시
    /// </summary>
    private void SmoothFillBars()
    {
        // === 체력 바 애니메이션 ===
        if (_healthFillImage != null)
        {
            _healthFillImage.fillAmount = Mathf.Lerp(
                _healthFillImage.fillAmount,  // 현재 값
                _targetHealthFill,            // 목표 값
                Time.deltaTime * _fillSpeed   // 보간 비율 (프레임 독립적)
            );
        }

        // === 마나 바 애니메이션 ===
        if (_manaFillImage != null)
        {
            _manaFillImage.fillAmount = Mathf.Lerp(
                _manaFillImage.fillAmount,
                _targetManaFill,
                Time.deltaTime * _fillSpeed
            );
        }
    }
}