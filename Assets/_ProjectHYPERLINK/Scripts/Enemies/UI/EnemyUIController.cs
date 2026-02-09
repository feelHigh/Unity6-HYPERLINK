using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 UI 컨트롤러
///
/// 기능 :
/// - 머리 위 체력바 관리
/// - 데미지 텍스트 생성
/// - 카메라를 향해 항상 회전
/// </summary>
public class EnemyUIController : MonoBehaviour
{
    [Header("----- 참조 -----")]
    [SerializeField] EnemyController _enemy;        //적 컨트롤러

    [Header("----- 체력바 -----")]
    [SerializeField] Image _healthBar;              //체력바 Fill 이미지
    [SerializeField] CanvasGroup _canvasGroup;      //캔버스 투명도 제어용
    [SerializeField] Canvas _canvas;                //캔버스

    [Header("----- 체력바 설정 -----")]
    [SerializeField] float _hideHealthBarDelay = 3f; //체력이 꽉 찼을 때 숨기는 지연 시간

    [Header("----- 데미지 텍스트 -----")]
    [SerializeField] GameObject _damageTextPrefab;  //데미지 텍스트 프리팹
    [SerializeField] Vector3 _spawnOffset = new Vector3(0f, 2f, 0f);    //데미지 텍스트 생성 위치 오프셋

    Camera _mainCam;                //메인 카메라
    float _lastDamageTime;          //마지막 피격 시간
    bool _isFullHealth = true;  //체력이 풀인지 여부
    bool _healthDirty = false;  //체력바 갱신 필요 여부

    private void Awake()
    {
        //컨트롤러 참조
        if (_enemy == null)
        {
            _enemy = GetComponentInParent<EnemyController>();
        }

        //메인 카메라 참조
        _mainCam = Camera.main;

        //캔버스 그룹 참조
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        //초기 상태 숨김
        _canvasGroup.alpha = 0f;
        _canvas.enabled = false;
    }

    private void OnEnable()
    {
        //적 이벤트 구독
        if (_enemy != null)
        {
            _enemy.OnHit += OnEnemyHit;
            _enemy.OnShowDamage += SpawnDamageText;
            _enemy.OnDie += OnEnemyDie;
        }
    }

    private void OnDisable()
    {
        //이벤트 구독 해제
        if (_enemy != null)
        {
            _enemy.OnHit -= OnEnemyHit;
            _enemy.OnShowDamage -= SpawnDamageText;
            _enemy.OnDie -= OnEnemyDie;
        }
    }

    private void LateUpdate()
    {
        if (_enemy == null || _enemy.IsDead) return;

        //캔버스가 비활성화면 회전/페이드 처리 불필요
        if (!_canvas.enabled) return;

        //체력바 업데이트 (피격 시에만 dirty 플래그 설정됨)
        if (_healthDirty)
        {
            UpdateHealthBar();
            _healthDirty = false;
        }

        //카메라를 향해 회전
        if (_mainCam != null)
        {
            transform.rotation = _mainCam.transform.rotation;
        }

        //체력이 꽉 차면 딜레이 후 숨김
        if (_isFullHealth && _canvasGroup != null)
        {
            if (Time.time - _lastDamageTime > _hideHealthBarDelay)
            {
                _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 0f, Time.deltaTime * 3f);
            }
        }

        //완전히 투명하면 캔버스 비활성화
        if (_canvasGroup.alpha < 0.01f)
        {
            _canvas.enabled = false;
        }
    }

    /// <summary>
    /// 체력바 업데이트 함수
    /// </summary>
    void UpdateHealthBar()
    {
        //체력 비율 계산
        float percantage = _enemy.CurHp / _enemy.MaxHp;

        //체력 바 업데이트
        if (_healthBar != null)
        {
            _healthBar.fillAmount = percantage;
        }

        //체력이 풀인지 체크
        _isFullHealth = percantage >= 0.99f;
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
            Debug.LogWarning("[EnemyUI] DamageText 프리팹이 존재하지 않습니다.");
            return;
        }

        //생성 위치 계산
        Vector3 spawnPos = _enemy.transform.position + _spawnOffset;

        //생성
        GameObject damageTextGO = Instantiate(_damageTextPrefab, spawnPos, Quaternion.identity);

        //초기화
        DamageText damageText = damageTextGO.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Initialize(damage, isCritical);
            damageText.AddRandomOffset();
        }
    }

    /// <summary>
    /// 적 피격 시 호출
    /// </summary>
    void OnEnemyHit()
    {
        //마지막 피격 시간 갱신
        _lastDamageTime = Time.time;

        //체력바 갱신 플래그 설정
        _healthDirty = true;

        //캔버스 활성화
        if (_canvasGroup != null && _canvas != null)
        {
            _canvasGroup.alpha = 1f;
            _canvas.enabled = true;
        }
    }

    /// <summary>
    /// 적 사망 시 호출
    /// </summary>
    void OnEnemyDie()
    {
        //캔버스 비활성화
        if (_canvasGroup != null && _canvas != null)
        {
            _canvasGroup.alpha = 0f;
            _canvas.enabled = false;
        }
    }
}
