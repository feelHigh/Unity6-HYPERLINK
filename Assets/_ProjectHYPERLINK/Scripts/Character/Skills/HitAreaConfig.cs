using UnityEngine;

/// <summary>
/// Hit Area 설정 데이터 클래스
/// 
/// 용도: 각 히트 영역의 형태, 크기, 위치, 타이밍 설정
/// - 여러 hit area를 독립적으로 제어 가능
/// - Sphere/Box 형태 지원
/// - Unity Inspector에서 직관적으로 설정
/// </summary>
[System.Serializable]
public class HitAreaConfig
{
    [Header("기본 설정")]
    [Tooltip("히트 영역 형태 (Sphere 또는 Box)")]
    [SerializeField] private HitAreaShape _shape = HitAreaShape.Sphere;

    [Tooltip("데미지 적용 타이밍 (0.0 ~ 1.0, 애니메이션 진행률)")]
    [Range(0f, 1f)]
    [SerializeField] private float _damagePointTiming = 0.7f;

    [Header("위치 설정")]
    [Tooltip("히트 영역 위치 오프셋 (플레이어 로컬 좌표)")]
    [SerializeField] private Vector3 _positionOffset = Vector3.zero;

    [Tooltip("회전 오프셋 (오일러 각도) - Box 형태에서 유용")]
    [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

    [Header("크기 설정")]
    [Tooltip("Sphere 형태일 때 반지름")]
    [SerializeField] private float _sphereRadius = 5f;

    [Tooltip("Box 형태일 때 박스 크기")]
    [SerializeField] private Vector3 _boxSize = new Vector3(3f, 2f, 4f);

    [Header("고급 설정")]
    [Tooltip("이 hit area를 무시할 적 태그 (선택사항)")]
    [SerializeField] private string[] _ignoreTags = new string[0];

    [Tooltip("데미지 배율 (기본 데미지에 곱해짐, 1.0 = 100%)")]
    [Range(0f, 5f)]
    [SerializeField] private float _damageMultiplier = 1.0f;

    // 프로퍼티
    public HitAreaShape Shape => _shape;
    public float DamagePointTiming => _damagePointTiming;
    public Vector3 PositionOffset => _positionOffset;
    public Vector3 RotationOffset => _rotationOffset;
    public float SphereRadius => _sphereRadius;
    public Vector3 BoxSize => _boxSize;
    public string[] IgnoreTags => _ignoreTags;
    public float DamageMultiplier => _damageMultiplier;

    /// <summary>
    /// 유효한 hit area 설정인지 확인
    /// </summary>
    public bool IsValid()
    {
        if (_shape == HitAreaShape.Sphere)
        {
            return _sphereRadius > 0f;
        }
        else // Box
        {
            return _boxSize.x > 0f && _boxSize.y > 0f && _boxSize.z > 0f;
        }
    }

    /// <summary>
    /// 특정 태그를 무시해야 하는지 확인
    /// </summary>
    public bool ShouldIgnoreTag(string tag)
    {
        if (_ignoreTags == null || _ignoreTags.Length == 0)
            return false;

        foreach (string ignoreTag in _ignoreTags)
        {
            if (ignoreTag == tag)
                return true;
        }
        return false;
    }
}

/// <summary>
/// Hit Area 형태
/// </summary>
public enum HitAreaShape
{
    Sphere,  // 구형 범위
    Box      // 박스 범위
}
