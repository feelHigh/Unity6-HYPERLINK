using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 프리팹에 붙일 데미지 텍스트 클래스
///
/// 기능 :
/// - 위로 떠오르면서 데미지 수치 표시
/// - 페이드 아웃 효과
/// - 크리티컬/일반 공격 구분 (색상, 크기)
/// - 일정 시간 후 풀에 자동 반환
/// </summary>
public class DamageText : MonoBehaviour
{
    // 정수→문자열 캐시 (GC 최적화: 0~9999 범위의 ToString() 할당 제거)
    private static readonly string[] _cachedIntStrings = new string[10000];

    static DamageText()
    {
        for (int i = 0; i < _cachedIntStrings.Length; i++)
            _cachedIntStrings[i] = i.ToString();
    }

    [Header("----- 참조 -----")]
    [SerializeField] TextMeshProUGUI _text;     //데미치 수치 텍스트
    [SerializeField] CanvasGroup _canvasGroup;  //투명도 제어용 캔버스 그룹

    [Header("----- 애니메이션 설정 -----")]
    [SerializeField] float _moveSpeed = 2f;     //위로 이동하는 속도
    [SerializeField] float _lifeTime = 1.5f;    //생명 시간
    [SerializeField] AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0, 1.2f, 1, 1f);     //크기 애니메이션
    [SerializeField] AnimationCurve _alphaCurve = AnimationCurve.EaseInOut(0, 1f, 1, 0f);       //투명도 애니메이션

    [Header("----- 색상 설정 -----")]
    [SerializeField] Color _nomalColor = Color.white;   //일반 데미지 색상 (하양)
    [SerializeField] Color _criticalColor = Color.red;  //크리티컬 데미지 색상 (빨강)

    Camera _mainCam;            //메인 카메라
    Vector3 _startPos;          //시작 위치
    float _elapsedTime = 0f;    //생성 후 경과 시간
    Coroutine _animCoroutine;   //현재 실행 중인 애니메이션 코루틴

    private void Awake()
    {
        _mainCam = Camera.main;

        // GetComponent를 Awake에서 한 번만 수행 (풀 재사용 시에도 유지)
        if (_text == null)
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// 데미지 텍스트 초기화
    /// </summary>
    /// <param name="damage">데미지 수치</param>
    /// <param name="isCritical">크리티컬 여부</param>
    public void Initialize(float damage, bool isCritical)
    {
        // 풀에서 재사용 시 시작 위치 갱신
        _startPos = transform.position;
        _elapsedTime = 0f;

        // 카메라 캐시 갱신 (씬 전환 후 null 가능)
        if (_mainCam == null)
        {
            _mainCam = Camera.main;
        }

        //데미지 텍스트 설정
        int dmg = Mathf.RoundToInt(damage);
        _text.text = (dmg >= 0 && dmg < _cachedIntStrings.Length)
            ? _cachedIntStrings[dmg]
            : dmg.ToString();

        //크리티컬 여부에 따라 색상 및 크기 설정
        if (isCritical)
        {
            _text.color = _criticalColor;
            _text.fontSize = 48f;
        }
        else
        {
            _text.color = _nomalColor;
            _text.fontSize = 36f;
        }

        // 초기 상태 복원 (풀 재사용 시 필요)
        transform.localScale = Vector3.one;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        // 이전 애니메이션 정리 후 새로 시작
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
        }
        _animCoroutine = StartCoroutine(AnimateText());
    }

    /// <summary>
    /// 텍스트 애니메이션 코루틴
    /// 빌보드 회전도 여기서 처리 (별도 Update 불필요)
    /// </summary>
    /// <returns></returns>
    IEnumerator AnimateText()
    {
        _elapsedTime = 0f;

        while (_elapsedTime < _lifeTime)
        {
            _elapsedTime += Time.deltaTime;
            float t = _elapsedTime / _lifeTime;

            //위로 이동
            transform.position = _startPos + Vector3.up * (_moveSpeed * _elapsedTime);

            //크기 애니메이션
            float scale = _scaleCurve.Evaluate(t);
            transform.localScale = Vector3.one * scale;

            //투명도 애니메이션
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _alphaCurve.Evaluate(t);
            }

            // 빌보드 회전 (Phase 5: Update에서 코루틴으로 이동)
            if (_mainCam != null)
            {
                transform.rotation = _mainCam.transform.rotation;
            }

            yield return null;
        }

        _animCoroutine = null;

        // 풀에 반환
        GameObjectPool.Instance.Release(gameObject);
    }

    /// <summary>
    /// 약간의 랜덤 오프셋을 추가하는 함수
    /// </summary>
    public void AddRandomOffset()
    {
        float ranX = Random.Range(-0.3f, 0.3f);
        float ranY = Random.Range(-0.1f, 0.1f);

        Vector3 ranOffset = new Vector3(ranX, ranY, 0f);

        _startPos += ranOffset;
        transform.position = _startPos;
    }
}
