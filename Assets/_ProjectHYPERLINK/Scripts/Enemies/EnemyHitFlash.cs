using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적이 피격 당했을 때 시각적 효과를 구현하는 클래스
/// </summary>
public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] EnemyController _controller;

    [Header("----- 플래시 설정 -----")]
    [SerializeField] float _flashDuration = 0.1f;       //플래시 지속 시간
    [SerializeField] Color _flashColor = Color.red;     //플래시 색상
    [SerializeField] float _emissionIntensity = 3f;     //이미션 강도

    //자동으로 찾을 모든 렌더러들
    List<Material> _materials = new List<Material>();
    List<Color> _originalColors = new List<Color>();
    List<string> _colorPropertyNames = new List<string>();

    // 캐시된 HasProperty 결과 (Phase 2B)
    List<int> _hitStrengthPropIndex = new List<int>();   // 각 머티리얼의 _HitStrength/_FlashAmount 프로퍼티 인덱스 (-1 = 없음)
    List<bool> _hasEmissionColor = new List<bool>();     // 각 머티리얼의 _EmissionColor 보유 여부

    //알고 있는 Color 프로퍼티
    static readonly string[] _knownColorProps = new string[]
    {
        "_Color",           //Standard Shader
        "_BaseColor",       //URP Lit
        "_TintColor",       //Particle
        "_EmissionColor",   //Emission
        "_MainColor",       //커스텀
        "_Tint",            //커스텀
        "_Albedo",          //커스텀
        "_SpecColor",       //Specular Color
        "_WhiteColor",      //커스텀
        "_MidColor",        //커스텀
        "_LastColor",       //커스텀
        "_FresnelColor"     //프레넬 색상
    };

    //쉐이더 그래프용 프로퍼티
    static readonly string[] _hitStrengthProps = new string[]
    {
        "_HitStrength", "_FlashAmount"
    };

    Coroutine _flashCoroutine;
    bool _isInitialized = false;

    private void Awake()
    {
        //모든 렌더러 찾기
        FindAllMaterials();

        //EnemyController 참조
        _controller = GetComponent<EnemyController>();
        if (_controller == null)
        {
            DebugHelper.LogWarning($"[HitFlash] {gameObject.name}에서 EnemyController를 찾을 수 없습니다");
        }
    }

    private void OnEnable()
    {
        //이벤트 구독
        if (_controller != null)
        {
            _controller.OnHit += HandleHit;
            DebugHelper.Log($"[HitFlash] OnHit 이벤트 구독 완료");
        }
    }

    private void OnDisable()
    {
        //이벤트 구독 해제
        if (_controller != null)
        {
            _controller.OnHit -= HandleHit;
        }
    }

    /// <summary>
    /// Onhit 이벤트에 구독할 이벤트 핸들러
    /// </summary>
    void HandleHit()
    {
        DebugHelper.Log($"[HitFlash] HandleHit 호출됨!");
        Flash();
    }

    /// <summary>
    /// 모든 렌더러와 매터리얼 초기화
    /// </summary>
    void FindAllMaterials()
    {
        //초기화 전 전체 클리어
        _materials.Clear();
        _originalColors.Clear();
        _colorPropertyNames.Clear();
        _hitStrengthPropIndex.Clear();
        _hasEmissionColor.Clear();

        var renderers = GetComponentsInChildren<Renderer>(true);

        DebugHelper.Log($"[HitFlash] 찾은 렌더러 수: {renderers.Length}");

        foreach (var renderer in renderers)
        {
            //매터리얼 인스턴스 생성 (원본 보호)
            Material[] mats = renderer.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = new Material(mats[i]);
                _materials.Add(mats[i]);

                //컬러 프로퍼티 찾기
                string colorProp = FindColorProperty(mats[i]);
                if (colorProp != null)
                {
                    _colorPropertyNames.Add(colorProp);
                    _originalColors.Add(mats[i].GetColor(colorProp));
                }
                else
                {
                    _colorPropertyNames.Add(null);
                    _originalColors.Add(Color.white);
                }

                // HasProperty 결과 캐싱 (Phase 2B)
                int hitPropIdx = -1;
                for (int j = 0; j < _hitStrengthProps.Length; j++)
                {
                    if (mats[i].HasProperty(_hitStrengthProps[j]))
                    {
                        hitPropIdx = j;
                        break;
                    }
                }
                _hitStrengthPropIndex.Add(hitPropIdx);
                _hasEmissionColor.Add(mats[i].HasProperty("_EmissionColor"));
            }

            //인스턴스 적용
            renderer.materials = mats;
        }

        _isInitialized = true;
        DebugHelper.Log($"[HitFlash] 총 머티리얼 수: {_materials.Count}");
    }

    /// <summary>
    /// 머티리얼에서 사용 가능한 색상 프로퍼티 찾기
    /// </summary>
    string FindColorProperty(Material mat)
    {
        //우선순위대로 체크
        foreach (string propName in _knownColorProps)
        {
            if (mat.HasProperty(propName))
            {
                return propName;
            }
        }

        //리스트에 없는 프로퍼티라면 쉐이더에서 첫번째 Color 프로퍼티 사용
        Shader shader = mat.shader;
        int propertyCount = shader.GetPropertyCount();

        for (int i = 0; i < propertyCount; i++)
        {
            if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color)
            {
                return shader.GetPropertyName(i);
            }
        }

        return null;
    }

    /// <summary>
    /// 피격 시 호출할 메서드
    /// </summary>
    void Flash()
    {
        //초기화 전이면 초기화부터
        if (!_isInitialized)
        {
            FindAllMaterials();
        }

        //이미 실행 중이면 중지 후 다시 시작
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// 플래시 효과 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator FlashRoutine()
    {
        //모든 매터리얼을 플래시 색상으로 변경
        for (int i = 0; i < _materials.Count; i++)
        {
            var mat = _materials[i];
            if (mat == null) continue;

            //쉐이더 그래프 전용 (_HitStrength) - 캐시된 인덱스 사용
            int hitIdx = _hitStrengthPropIndex[i];
            if (hitIdx >= 0)
            {
                mat.SetFloat(_hitStrengthProps[hitIdx], 1f);
            }

            //기본 색상 변경
            string prop = _colorPropertyNames[i];
            if (!string.IsNullOrEmpty(prop))
                mat.SetColor(prop, _flashColor);

            //Emission 강화 - 캐시된 결과 사용
            if (_hasEmissionColor[i])
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", _flashColor * _emissionIntensity);
            }
        }

        //지속 시간 만큼 대기
        yield return WaitForSecondsCache.Get(_flashDuration);

        //원래 색상으로 복구
        for (int i = 0; i < _materials.Count; i++)
        {
            if (_materials[i] == null) continue;

            try
            {
                _materials[i].SetColor(_colorPropertyNames[i], _originalColors[i]);

                // 캐시된 결과 사용
                if (_hasEmissionColor[i])
                {
                    _materials[i].SetColor("_EmissionColor", Color.black);
                }

                // 히트 강도 리셋
                int hitIdx = _hitStrengthPropIndex[i];
                if (hitIdx >= 0)
                {
                    _materials[i].SetFloat(_hitStrengthProps[hitIdx], 0f);
                }
            }
            catch (System.Exception e)
            {
                DebugHelper.LogWarning($"[HitFlash] 색상 복구 실패: {_materials[i].name} - {e.Message}");
            }
        }

        _flashCoroutine = null;
    }

    //오브젝트 파괴 시 생성된 매터리얼 인스턴스 정리
    private void OnDestroy()
    {
        foreach (Material mat in _materials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
    }
}
