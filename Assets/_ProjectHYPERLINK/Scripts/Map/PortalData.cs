using UnityEngine;

/// <summary>
/// 포탈 설정 데이터 (ScriptableObject)
/// 
/// 기능:
/// - 포탈 타입 정의 (씬 전환 / 텔레포트 / UI 열기)
/// - 대상 씬 또는 위치 지정
/// - 에디터 워크플로우 지원
/// 
/// 사용법:
/// 1. Assets > Create > ScriptableObject > Portal Data
/// 2. 포탈 타입 선택
/// 3. 대상 씬/위치 설정
/// 4. Portal 컴포넌트에 할당
/// </summary>
[CreateAssetMenu(fileName = "New Portal Data", menuName = "ScriptableObject/Portal Data")]
public class PortalData : ScriptableObject
{
    [Header("포탈 정보")]
    [Tooltip("포탈 이름 (UI 표시용)")]
    [SerializeField] private string _portalName = "포탈";

    [Tooltip("포탈 작동 타입")]
    [SerializeField] private PortalType _portalType = PortalType.SceneTransition;

    [Header("씬 전환 설정")]
    [Tooltip("이동할 씬 이름 (PortalType이 SceneTransition일 때)")]
    [SerializeField] private string _targetSceneName = "";

    [Tooltip("목표 씬의 스폰 위치 이름 (비어있으면 기본 스폰)")]
    [SerializeField] private string _targetSpawnPointName = "";

    [Header("텔레포트 설정")]
    [Tooltip("텔레포트 위치 이름 (PortalType이 Teleport일 때)")]
    [SerializeField] private string _teleportLocationName = "";

    [Header("상호작용 설정")]
    [Tooltip("상호작용 범위")]
    [SerializeField] private float _interactionRange = 3.0f;

    [Tooltip("자동 작동 (플레이어가 닿으면 바로 작동)")]
    [SerializeField] private bool _autoActivate = false;

    [Header("비주얼 설정")]
    [Tooltip("포탈 활성화 여부")]
    [SerializeField] private bool _isActive = true;

    [Tooltip("포탈 이펙트 색상")]
    [SerializeField] private Color _portalColor = Color.cyan;

    // Properties
    public string PortalName => _portalName;
    public PortalType Type => _portalType;
    public string TargetSceneName => _targetSceneName;
    public string TargetSpawnPointName => _targetSpawnPointName;
    public string TeleportLocationName => _teleportLocationName;
    public float InteractionRange => _interactionRange;
    public bool AutoActivate => _autoActivate;
    public bool IsActive => _isActive;
    public Color PortalColor => _portalColor;

    /// <summary>
    /// 설정 유효성 검사
    /// </summary>
    public bool IsValid()
    {
        switch (_portalType)
        {
            case PortalType.SceneTransition:
                return !string.IsNullOrEmpty(_targetSceneName);

            case PortalType.Teleport:
                return !string.IsNullOrEmpty(_teleportLocationName);

            case PortalType.OpenTeleportUI:
                return true; // UI 타입은 항상 유효

            default:
                return false;
        }
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public string GetDebugInfo()
    {
        switch (_portalType)
        {
            case PortalType.SceneTransition:
                return $"Scene: {_targetSceneName}" +
                       (string.IsNullOrEmpty(_targetSpawnPointName) ? "" : $" -> {_targetSpawnPointName}");

            case PortalType.Teleport:
                return $"Location: {_teleportLocationName}";

            case PortalType.OpenTeleportUI:
                return "Open Teleport UI";

            default:
                return "Invalid";
        }
    }
}

/// <summary>
/// 포탈 타입 열거형
/// </summary>
public enum PortalType
{
    /// <summary>
    /// 다른 씬으로 전환
    /// </summary>
    SceneTransition,

    /// <summary>
    /// 같은 씬 또는 다른 씬의 특정 위치로 텔레포트
    /// </summary>
    Teleport,

    /// <summary>
    /// 텔레포트 UI 열기 (플레이어가 목적지 선택)
    /// </summary>
    OpenTeleportUI
}
