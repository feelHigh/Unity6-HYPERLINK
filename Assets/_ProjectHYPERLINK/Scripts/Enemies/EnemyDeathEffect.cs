using System.Collections;
using UnityEngine;

/// <summary>
/// 적 사망 시 연출을 담당하는 클래스
/// 인스펙터에서 원하는 연출 활성화 및 설정
/// </summary>
public class EnemyDeathEffect : MonoBehaviour
{
    [Header("----- EnemyController -----")]
    [SerializeField] EnemyController _controller;

    [Header("----- 기본 설정 -----")]
    [SerializeField] float _startDelay = 0f;                //연출 시작 딜레이
    [SerializeField] bool _disableAfterEffect = false;      //연출 완료 후 오브젝트 비활성화 여부
    [SerializeField] float _disableDuration = 0f;           //연출 완료 후 비활성화되기까지의 시간

    [Header("----- 오브젝트 제어 -----")]
    [SerializeField] bool _enableObjectControl = false;
    [SerializeField] float _objectDisableDelay = 0f;        //오브젝트 비활성화 시작 딜레이
    [SerializeField] GameObject[] _objectsToDisable;        //비활성화할 오브젝트들
    [SerializeField] float _objectSpawnDelay = 0f;          //오브젝트 생성 시작 딜레이
    [SerializeField] GameObject[] _objectsToSpawn;          //생성할 오브젝트들
    [SerializeField] Vector3 _spawnOffset = Vector3.zero;   //생성 위치 오프셋
    [SerializeField] float _destroyAfterDuration = 0f;      //생성한 오브젝트 파괴 시간

    [Header("----- Material 페이드 아웃 -----")]
    [SerializeField] bool _enableFadeOut = false;
    [SerializeField] float _fadeOutDelay = 0f;              //페이드 시작 딜레이
    [SerializeField] bool _disableShadows = false;          //페이드할 때 그림자 비활성화 여부
    [SerializeField] float _shadowDisableDelay = 0f;        //그림자 비활성화 시작 딜레이
    //일반 렌더러 매더리얼
    [SerializeField] Renderer[] _targetRenderers;           //페이드할 렌더러들
    [SerializeField] float _fadeDuration = 1f;              //페이드 시간
    [SerializeField] AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);  //페이드 곡선
    //쉐이더 매터리얼
    [SerializeField] string _shaderGraphPropertyName = "_Alpha";   //쉐이더 그래프의 알파 변수 이름

    [Header("----- 위치 변경 -----")]
    [SerializeField] bool _enablePositionChange = false;
    [SerializeField] bool _isUsingLocalPosition = false;    //로컬 좌표 사용 여부
    [SerializeField] float _positionChangeDelay = 0f;       //위치 이동 시작 딜레이
    [SerializeField] Transform _targetModel;                //이동시킬 모델
    [SerializeField] Vector3 _positionOffset = new Vector3(0, -2, 0);   //목표 위치
    [SerializeField] float _moveDuration = 1f;              //이동 시간
    [SerializeField] AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  //이동 곡선

    Vector3 _originalPos;

    // GC 최적화: 인스턴스화된 Material 캐싱 (ApplyFade에서 .material 반복 접근 방지)
    private Material[] _cachedMaterials;

    private void Awake()
    {
        if (_controller == null)
        {
            _controller = GetComponent<EnemyController>();
        }

        //죽음 이벤트 구독
        _controller.OnDie += PlayEffect;

        //모델 없으면 자동 설정
        if (_enablePositionChange && _targetModel == null)
        {
            _targetModel = transform;
        }

        //매터리얼 인스턴스 생성 (원본 보호) + 캐싱
        if (_enableFadeOut && _targetRenderers != null)
        {
            _cachedMaterials = new Material[_targetRenderers.Length];
            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                if (_targetRenderers[i] != null)
                {
                    _targetRenderers[i].material = new Material(_targetRenderers[i].material);
                    _cachedMaterials[i] = _targetRenderers[i].material;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.OnDie -= PlayEffect;
        }
    }

    /// <summary>
    /// 사망 연출 실행
    /// </summary>
    void PlayEffect()
    {
        //초기 위치 저장
        if (_enablePositionChange && _targetModel != null)
        {
            _originalPos = _isUsingLocalPosition ? _targetModel.localPosition : _targetModel.position;
        }

        StartCoroutine(DeathEffectCoroutine());
    }

    /// <summary>
    /// 사망 연출 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator DeathEffectCoroutine()
    {
        //시작 딜레이 대기
        if (_startDelay > 0)
        {
            yield return WaitForSecondsCache.Get(_startDelay);
        }

        //각 연출 별 코루틴 실행
        if (_enableObjectControl)
        {
            StartCoroutine(ObjectControlCoroutine());
        }

        if (_enableFadeOut)
        {
            StartCoroutine(FadeOutCoroutine());
        }

        if (_enablePositionChange)
        {
            StartCoroutine(PositionChangeCoroutine());
        }

        //모든 연출 끝날 때까지 대기
        float maxDuration = CaculateMaxDuration();
        if (maxDuration > 0)
        {
            yield return WaitForSecondsCache.Get(maxDuration);
        }

        //연출 완료 후 오브젝트 비활성화
        if (_disableAfterEffect)
        {
            if (_disableDuration > 0)
            {
                yield return WaitForSecondsCache.Get(_disableDuration);
            }

            gameObject.SetActive(false);
        }

        StopAllCoroutines();
    }

    #region 연출 별 코루틴
    /// <summary>
    /// 오브젝트 제어 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator ObjectControlCoroutine()
    {
        //비활성화 딜레이 대기
        if (_objectDisableDelay > 0)
        {
            yield return WaitForSecondsCache.Get(_objectDisableDelay);
        }

        //비활성화
        if (_objectsToDisable != null)
        {
            foreach (var obj in _objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        //생성 딜레이 대기
        if (_objectSpawnDelay > 0)
        {
            yield return WaitForSecondsCache.Get(_objectSpawnDelay);
        }

        //생성
        if (_objectsToSpawn != null)
        {
            foreach (var obj in _objectsToSpawn)
            {
                if (obj != null)
                {
                    Vector3 spawnPos = transform.position + _spawnOffset;
                    GameObject spawned = Instantiate(obj, spawnPos, transform.rotation);

                    //파괴 시간 설정되어 있으면 시간에 맞게 파괴
                    if (_destroyAfterDuration > 0)
                    {
                        Destroy(spawned, _destroyAfterDuration);
                    }
                    //파괴 시간 미설정 시 3초 후 자동 파괴
                    else
                    {
                        Destroy(spawned, 3f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 페이드 아웃 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator FadeOutCoroutine()
    {
        //딜레이 대기
        if (_fadeOutDelay > 0)
        {
            yield return WaitForSecondsCache.Get(_fadeOutDelay);
        }

        //그림자 비활성화
        if (_disableShadows)
        {
            StartCoroutine(DisableShadowCoroutine());
        }

        //페이드 아웃 실행
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float fadeTime = elapsed / _fadeDuration;
            float alpha = _fadeCurve.Evaluate(fadeTime);

            ApplyFade(alpha);
            yield return null;
        }
    }

    /// <summary>
    /// 그림자 비활성화 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator DisableShadowCoroutine()
    {
        //딜레이 대기
        if (_shadowDisableDelay > 0)
        {
            yield return WaitForSecondsCache.Get(_shadowDisableDelay);
        }

        //그림자 비활성화
        if (_targetRenderers != null)
        {
            foreach (var renderer in _targetRenderers)
            {
                if (renderer != null)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }
    }

    /// <summary>
    /// 페이드 적용 함수
    /// </summary>
    /// <param name="alpha"></param>
    void ApplyFade(float alpha)
    {
        if (_cachedMaterials == null) return;

        for (int i = 0; i < _cachedMaterials.Length; i++)
        {
            Material mat = _cachedMaterials[i];
            if (mat == null) continue;

            //쉐이더 그래프
            if (mat.HasProperty(_shaderGraphPropertyName))
            {
                mat.SetFloat(_shaderGraphPropertyName, alpha);
            }
            //URP Lit 쉐이더
            else if (mat.HasProperty("_BaseColor"))
            {
                Color color = mat.GetColor("_BaseColor");
                color.a = alpha;
                mat.SetColor("_BaseColor", color);
            }
            //스탠다드 쉐이더 또는 _Mode 프로퍼티가 있는 경우
            else if (mat.HasProperty("_Mode"))
            {
                Color color = mat.color;
                color.a = alpha;
                mat.color = color;

                //Transparent 렌더링 모드로 변경
                mat.SetFloat("_Mode", 2);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            //일반 쉐이더
            else if (mat.HasProperty("_Color"))
            {
                Color color = mat.GetColor("_Color");
                color.a = alpha;
                mat.SetColor("_Color", color);
            }
            //위의 모든 경우가 아닐 때
            else if (_targetRenderers[i] != null)
            {
                DebugHelper.LogWarning($"{_targetRenderers[i].gameObject.name}의 Material 알파 조정 불가.");
            }
        }
    }

    /// <summary>
    /// 위치 변경 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator PositionChangeCoroutine()
    {
        //딜레이 대기
        if (_positionChangeDelay > 0)
        {
            yield return WaitForSecondsCache.Get(_positionChangeDelay);
        }

        if (_targetModel == null) yield break;

        //위치 이동 실행
        float elapsed = 0f;
        while (elapsed < _moveDuration)
        {
            elapsed += Time.deltaTime;
            float moveTime = elapsed / _moveDuration;
            float curveValue = _moveCurve.Evaluate(moveTime);

            if (_isUsingLocalPosition)
            {
                _targetModel.localPosition = _originalPos + (_positionOffset * curveValue);
            }
            else
            {
                _targetModel.position = _originalPos + (_positionOffset * curveValue);
            }

            yield return null;
        }
    }
    #endregion

    /// <summary>
    /// 가장 긴 연출 시간 계산
    /// </summary>
    /// <returns></returns>
    float CaculateMaxDuration()
    {
        float maxDuration = 0f;

        if (_enableObjectControl)
        {
            maxDuration = Mathf.Max(maxDuration, _objectDisableDelay + _objectSpawnDelay);
        }

        if (_enableFadeOut)
        {
            maxDuration = Mathf.Max(maxDuration, _fadeOutDelay + _fadeDuration);
        }

        if (_enablePositionChange)
        {
            maxDuration = Mathf.Max(maxDuration, _positionChangeDelay + _moveDuration);
        }

        return maxDuration;
    }
}
