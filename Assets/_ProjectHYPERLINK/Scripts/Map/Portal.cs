using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 포탈 시스템
/// 
/// 기능:
/// - 씬 간 전환 (Scene Transition)
/// - 위치 간 텔레포트 (Portal-to-Portal)
/// - IInteractable 구현으로 상호작용 시스템 통합
/// - 퀘스트 완료 체크로 포탈 잠금/해제
/// - 자동 작동 또는 수동 상호작용
/// 
/// 사용법:
/// 1. GameObject에 Portal 컴포넌트 추가
/// 2. PortalData ScriptableObject 생성 및 할당
/// 3. [선택] RequiredQuestID 설정 (퀘스트 완료 시에만 사용 가능)
/// 4. Collider 컴포넌트 필수 (자동 Trigger 설정)
/// 
/// 커스텀 에디터:
/// - 레벨 디자이너가 PortalData로 포탈 설정 관리
/// </summary>
[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour, IInteractable
{
    [Header("포탈 설정")]
    [Tooltip("포탈 데이터 (ScriptableObject)")]
    [SerializeField] private PortalData _portalData;

    [Header("퀘스트 잠금 설정")]
    [Tooltip("이 포탈을 사용하기 위해 완료해야 하는 퀘스트 ID (비어있으면 항상 사용 가능)")]
    [SerializeField] private string _requiredQuestID = "";

    [Header("비주얼 (Optional)")]
    [Tooltip("포탈 이펙트 오브젝트")]
    [SerializeField] private GameObject _portalEffect;

    [Tooltip("플레이어 감지 시 회전")]
    [SerializeField] private bool _rotateTowardsPlayer = true;

    [SerializeField] private float _rotationSpeed = 5f;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    [Header("포탈 위치")]
    [SerializeField] string _portalLocation;

    // 내부 상태
    private Collider _collider;
    private bool _isActivating = false;
    private Transform _playerTransform;

    public string PortalLocation => _portalLocation;

    #region Unity Lifecycle

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;

        ValidateConfiguration();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        if (_rotateTowardsPlayer && _playerTransform != null && _portalEffect != null)
        {
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            direction.y = 0; // 수평 회전만

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _portalEffect.transform.rotation = Quaternion.Lerp(
                    _portalEffect.transform.rotation,
                    targetRotation,
                    Time.deltaTime * _rotationSpeed
                );
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerTransform = other.transform;

        // 자동 작동 모드
        if (_portalData != null && _portalData.AutoActivate && _portalData.IsActive)
        {
            PlayerCharacter player = other.GetComponent<PlayerCharacter>();
            if (player != null)
            {
                Log("자동 작동: 플레이어 감지");
                ActivatePortal(player);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerTransform = null;
        }
    }

    #endregion

    #region IInteractable 구현

    public void Interact(PlayerCharacter player)
    {
        if (_isActivating)
        {
            Log("이미 작동 중입니다");
            return;
        }

        ActivatePortal(player);
    }

    public string GetInteractionPrompt()
    {
        // 호환성 유지용 (더 이상 사용 안 함)
        if (_portalData == null) return "포탈";

        switch (_portalData.Type)
        {
            case PortalType.SceneTransition:
                return $"{_portalData.TargetSceneName} 이동";
            case PortalType.Teleport:
                return $"{_portalData.TeleportLocationName} 텔레포트";
            default:
                return "포탈";
        }
    }

    public bool CanInteract(PlayerCharacter player)
    {
        if (_portalData == null)
        {
            LogWarning("PortalData가 설정되지 않았습니다");
            return false;
        }

        if (!_portalData.IsActive)
        {
            Log("포탈이 비활성화되어 있습니다");
            return false;
        }

        if (_isActivating)
        {
            return false;
        }

        if (!_portalData.IsValid())
        {
            return false;
        }

        // 퀘스트 완료 체크
        if (!IsQuestRequirementMet())
        {
            Log($"퀘스트 미완료: {_requiredQuestID}");
            return false;
        }

        return true;
    }

    public float GetInteractionRange()
    {
        return _portalData != null ? _portalData.InteractionRange : 3.0f;
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Portal;
    }

    public string GetInteractionName()
    {
        // 퀘스트 미완료 시 잠김 표시
        if (!string.IsNullOrEmpty(_requiredQuestID) && !IsQuestRequirementMet())
        {
            return $"{_portalData?.PortalName ?? "포탈"} (잠김)";
        }

        return _portalData != null ? _portalData.PortalName : "포탈";
    }

    #endregion

    #region 퀘스트 체크

    /// <summary>
    /// 퀘스트 요구사항 충족 여부 확인
    /// </summary>
    private bool IsQuestRequirementMet()
    {
        // 퀘스트 요구사항이 없으면 항상 사용 가능
        if (string.IsNullOrEmpty(_requiredQuestID))
        {
            return true;
        }

        // QuestManager가 없으면 퀘스트 시스템이 비활성화된 것으로 간주
        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager가 없습니다. 퀘스트 체크 건너뜀");
            return true;
        }

        // 퀘스트 완료 여부 확인
        bool isCompleted = QuestManager.Instance.IsQuestCompleted(_requiredQuestID);

        if (!isCompleted)
        {
            Log($"필요 퀘스트 미완료: {_requiredQuestID}");
        }

        return isCompleted;
    }

    #endregion

    #region 포탈 작동 로직

    /// <summary>
    /// 포탈 활성화 (씬 전환 또는 텔레포트)
    /// </summary>
    private void ActivatePortal(PlayerCharacter player)
    {
        if (_portalData == null || !_portalData.IsValid())
        {
            LogError("유효하지 않은 포탈 설정입니다");
            return;
        }

        // 퀘스트 체크
        if (!IsQuestRequirementMet())
        {
            LogWarning("퀘스트 요구사항 미충족");
            return;
        }

        _isActivating = true;

        switch (_portalData.Type)
        {
            case PortalType.SceneTransition:
                ActivateSceneTransition();
                break;

            case PortalType.Teleport:
                ActivateTeleport();
                break;
        }

        // VFX 재생 (선택 사항)
        PlayPortalEffect();
    }

    /// <summary>
    /// 씬 전환 모드
    /// </summary>
    private void ActivateSceneTransition()
    {
        string targetScene = _portalData.TargetSceneName;
        string spawnPoint = _portalData.TargetSpawnPointName;

        Log($"씬 전환: {targetScene}" +
            (string.IsNullOrEmpty(spawnPoint) ? "" : $" -> {spawnPoint}"));

        // PlayerSpawner에 위치 저장
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.SavePlayerPosition();
        }

        // 스폰 포인트 정보를 PlayerPrefs에 저장 (다음 씬에서 사용)
        if (!string.IsNullOrEmpty(spawnPoint))
        {
            PlayerPrefs.SetString("TargetSpawnPoint", spawnPoint);
            PlayerPrefs.Save();
        }
        else
        {
            PlayerPrefs.DeleteKey("TargetSpawnPoint");
        }

        // 씬 로드
        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// 텔레포트 모드
    /// </summary>
    private void ActivateTeleport()
    {
        string locationName = _portalData.TeleportLocationName;

        Log($"텔레포트: {locationName}");

        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.TeleportToLocation(locationName);
        }
        else
        {
            LogError("PlayerSpawner를 찾을 수 없습니다!");
        }

        _isActivating = false;
    }

    #endregion

    #region 비주얼 & 이펙트

    /// <summary>
    /// 포탈 비주얼 업데이트
    /// </summary>
    private void UpdateVisuals()
    {
        if (_portalEffect != null && _portalData != null)
        {
            // 퀘스트 미완료 시 비활성화 표시
            bool isUsable = _portalData.IsActive && IsQuestRequirementMet();
            _portalEffect.SetActive(isUsable);

            // 색상 적용 (Material이 있는 경우)
            Renderer renderer = _portalEffect.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                // 퀘스트 미완료 시 회색으로 표시
                Color targetColor = isUsable ? _portalData.PortalColor : Color.gray;
                renderer.material.color = targetColor;
            }
        }
    }

    /// <summary>
    /// 포탈 이펙트 재생
    /// </summary>
    private void PlayPortalEffect()
    {
        // TODO: VFX 재생, 사운드 재생 등
        Log("포탈 이펙트 재생");
    }

    #endregion

    #region 유효성 검사

    /// <summary>
    /// 설정 유효성 검사
    /// </summary>
    private void ValidateConfiguration()
    {
        if (_portalData == null)
        {
            LogWarning("PortalData가 할당되지 않았습니다. 인스펙터에서 설정하세요.");
            return;
        }

        if (!_portalData.IsValid())
        {
            LogError($"유효하지 않은 PortalData: {_portalData.GetDebugInfo()}");
        }
    }

    #endregion

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[Portal '{name}'] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[Portal '{name}'] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[Portal '{name}'] {message}");
    }

    private void OnDrawGizmos()
    {
        if (_portalData == null) return;

        // 퀘스트 미완료 시 빨간색으로 표시
        bool isUsable = _portalData.IsActive && IsQuestRequirementMet();
        Gizmos.color = isUsable ? Color.cyan : Color.red;
        Gizmos.DrawWireSphere(transform.position, _portalData.InteractionRange);

        // 포탈 방향 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

    private void OnDrawGizmosSelected()
    {
        if (_portalData == null) return;

        // 선택 시 추가 정보 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _portalData.InteractionRange * 0.5f);

#if UNITY_EDITOR
        // 디버그 정보 표시
        string questInfo = string.IsNullOrEmpty(_requiredQuestID) ? "퀘스트 제한 없음" : $"필요 퀘스트: {_requiredQuestID}";

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Portal: {_portalData.PortalName}\n{_portalData.GetDebugInfo()}\n{questInfo}"
        );
#endif
    }

    #endregion
}
