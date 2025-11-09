using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 UI 컨트롤러
///
/// 보스 이름 및 체력 표시
/// </summary>
public class BossUIController : MonoBehaviour
{
    [Header("----- UI 요소 -----")]
    [SerializeField] GameObject _bossHealthPanel;       //체력바 패널
    [SerializeField] TextMeshProUGUI _bossNameText;     //보스 이름 텍스트
    [SerializeField] Image _bossHealthBar;              //체력바 Fill 이미지

    [Header("----- 참조 -----")]
    [SerializeField] EnemyController _bossController;   //보스 컨트롤러

    [Header("----- 애니메이션 설정 -----")]
    [SerializeField] float _targetFillAmount;           //목표 체력 비율
    [SerializeField] float _fillSpeed = 5f;             //체력바 감소 속도

    private void Awake()
    {
        //Panel 비활성화
        if (_bossHealthPanel != null)
        {
            _bossHealthPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (_bossHealthBar != null)
        {
            //부드러운 체력바 애니메이션
            _bossHealthBar.fillAmount = Mathf.Lerp(
                _bossHealthBar.fillAmount,
                _targetFillAmount,
                Time.deltaTime * _fillSpeed);
        }
    }

    private void OnEnable()
    {
        EnemyController.OnBossSpawned += InitializeBossUI;
    }

    private void OnDisable()
    {
        EnemyController.OnBossSpawned -= InitializeBossUI;
    }

    /// <summary>
    /// 보스 UI 초기화
    /// </summary>
    /// <param name="boss">보스 컨트롤러</param>
    void InitializeBossUI(EnemyController boss)
    {
        if (boss == null)
        {
            Debug.LogError("[BossHealthBarUI] 보스 컨트롤러가 null입니다.");
            return;
        }

        _bossController = boss;

        //피격,죽음 이벤트 구독
        _bossController.OnHit += UpdateHeathBar;
        _bossController.OnDie += OnBossDeath;

        //보스 이름 설정
        if (_bossNameText != null)
        {
            string bossName = _bossController.EnemyName ?? "Boss";
            _bossNameText.text = bossName;
        }

        //초기 체력 설정
        UpdateHeathBar();

        //Panel 활성화
        if (_bossHealthPanel != null)
        {
            _bossHealthPanel.SetActive(true);
        }

        Debug.Log($"[BossHealthBarUI] 보스 '{_bossController.name}' 체력바 초기화 완료.");
    }

    /// <summary>
    /// 보스 체력바 업데이트
    /// EnemyController OnHit 이벤트에 연결해서
    /// 보스 피격 시 자동 호출
    /// </summary>
    void UpdateHeathBar()
    {
        if (_bossController == null) return;

        //체력 비율 계산 (0~1)
        float healthPercentage = _bossController.CurHp / _bossController.MaxHp;

        //체력바 업데이트
        if (_bossHealthBar != null)
        {
            _targetFillAmount = healthPercentage;
        }
    }

    void OnBossDeath()
    {
        //Panel 비활성화
        if (_bossHealthPanel != null)
        {
            _bossHealthPanel.SetActive(false);
        }

        //이벤트 구독 해제
        if (_bossController != null)
        {
            _bossController.OnHit -= UpdateHeathBar;
            _bossController.OnDie -= OnBossDeath;
        }

        _bossController = null;

        Debug.Log("[BossHealthBarUI] 보스 사망! 체력바 숨김");
    }
}
