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

    [Header("----- 데미지 텍스트 -----")]
    [SerializeField] GameObject _damageTextPrefab;      //데미지 텍스트 프리팹
    [SerializeField] Vector3 _spawnOffset = new Vector3(0f, 3f, 0f);    //데미지 텍스트 생성 위치 오프셋

    bool _isActive = false;

    private void Awake()
    {
        //Panel 비활성화
        if (_bossHealthPanel != null)
        {
            _bossHealthPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (_bossController == null)
        {
            _bossController = GetComponentInParent<EnemyController>();
            if (_bossController == null)
            {
                DebugHelper.LogError("[BossUI] 보스 컨트롤러를 찾을 수 없습니다");
                return;
            }
        }

        _bossController.OnInitialized += InitializeBossUI;
    }

    private void Update()
    {
        if (!_isActive) return;

        if (_bossHealthBar != null)
        {
            //부드러운 체력바 애니메이션
            _bossHealthBar.fillAmount = Mathf.Lerp(
                _bossHealthBar.fillAmount,
                _targetFillAmount,
                Time.deltaTime * _fillSpeed);
        }
    }

    private void OnDestroy()
    {
        if (_bossController != null)
        {
            _bossController.OnInitialized -= InitializeBossUI;
            _bossController.OnHit -= UpdateHealthBar;
            _bossController.OnShowDamage -= SpawnDamageText;
            _bossController.OnDie -= OnBossDeath;
        }
    }

    /// <summary>
    /// 보스 UI 초기화
    /// </summary>
    /// <param name="boss">보스 컨트롤러</param>
    void InitializeBossUI()
    {
        if (_bossController == null)
        {
            DebugHelper.LogError("[BossUI] 보스 컨트롤러를 찾을 수 없습니다");
            return;
        }

        if (!_bossController.IsBoss)
        {
            DebugHelper.LogError("[BossUI] 보스가 아닌 적에게 Boss UI 존재");
            return;
        }

        //피격,죽음 이벤트 구독
        _bossController.OnHit += UpdateHealthBar;
        _bossController.OnShowDamage += SpawnDamageText;
        _bossController.OnDie += OnBossDeath;

        //보스 이름 설정
        if (_bossNameText != null)
        {
            string bossName = _bossController.EnemyName ?? "Boss";
            _bossNameText.text = bossName;
        }

        //초기 체력 설정
        UpdateHealthBar();

        _isActive = true;

        //Panel 활성화
        if (_bossHealthPanel != null)
        {
            _bossHealthPanel.SetActive(true);
        }

        DebugHelper.Log($"[BossUI] 보스 '{_bossController.name}' UI 초기화 완료.");
    }

    /// <summary>
    /// 보스 체력바 업데이트
    /// EnemyController OnHit 이벤트에 연결해서
    /// 보스 피격 시 자동 호출
    /// </summary>
    void UpdateHealthBar()
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

    /// <summary>
    /// 데미지 텍스트 생성
    /// </summary>
    /// <param name="damage">데미지 수치</param>
    /// <param name="isCritical">크리티컬 여부</param>
    void SpawnDamageText(float damage, bool isCritical)
    {
        if (_damageTextPrefab == null)
        {
            DebugHelper.LogWarning("[BossUI] DamageText 프리팹이 존재하지 않습니다.");
            return;
        }

        if (_bossController == null) return;

        //생성 위치 계산
        Vector3 spawnPos = _bossController.transform.position + _spawnOffset;

        //풀에서 가져오기
        GameObject damageTextGO = GameObjectPool.Instance.Get(_damageTextPrefab, spawnPos, Quaternion.identity);

        //초기화
        DamageText damageText = damageTextGO.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Initialize(damage, isCritical);
            damageText.AddRandomOffset();
        }
    }

    /// <summary>
    /// 보스 사망 시 호출
    /// </summary>
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
            _bossController.OnHit -= UpdateHealthBar;
            _bossController.OnShowDamage -= SpawnDamageText;
            _bossController.OnDie -= OnBossDeath;
        }

        _isActive = false;

        DebugHelper.Log("[BossUI] 보스 사망! 체력바 숨김");
    }
}
