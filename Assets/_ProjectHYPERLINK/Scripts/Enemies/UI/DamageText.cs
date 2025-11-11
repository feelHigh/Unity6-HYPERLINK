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
/// - 일정 시간 후 자동 제거
/// </summary>
public class DamageText : MonoBehaviour
{
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

    private void Awake()
    {
        _mainCam = Camera.main;
        _startPos = transform.position;
    }

    /// <summary>
    /// 데미지 텍스트 초기화
    /// </summary>
    /// <param name="damage">데미지 수치</param>
    /// <param name="isCritical">크리티컬 여부</param>
    public void Initialize(float damage, bool isCritical)
    {
        //TMP 참조
        if (_text == null)
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        //캔버스 그룹 참조
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        //데미지 텍스트 설정
        _text.text = Mathf.RoundToInt(damage).ToString();

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

        //애니메이션 시작
        StartCoroutine(AnimateText());
    }

    private void Update()
    {
        //카메라를 향해 회전
        if (_mainCam != null)
        {
            transform.rotation = _mainCam.transform.rotation;
        }
    }

    /// <summary>
    /// 텍스트 애니메이션 코루틴
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

            yield return null;
        }

        //생명 시간 종료 후 파괴
        Destroy(gameObject);
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
