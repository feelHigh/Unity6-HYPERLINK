using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

/// <summary>
/// 포탈 시스템 (리팩토링됨 - TeleportUIWindow 참조 개선)
/// 
/// 기능:
/// - 씬 간 전환 (Scene Transition) - 데이터 저장 포함
/// - 위치 간 텔레포트 (Portal-to-Portal)
/// - 텔레포트 UI 열기 (플레이어가 목적지 선택) - 개선됨
/// - IInteractable 구현으로 상호작용 시스템 통합
/// - 퀘스트 완료 체크로 포탈 잠금/해제
/// - 발견 시스템 통합
/// - 자동 작동 또는 수동 상호작용
/// 
/// 사용법:
/// 1. GameObject에 Portal 컴포넌트 추가
/// 2. PortalData ScriptableObject 생성 및 할당
/// 3. [선택] RequiredQuestID 설정 (퀘스트 완료 시에만 사용 가능)
/// 4. [선택] TeleportUIWindow 직접 참조 (OpenTeleportUI 타입일 때)
/// 5. Collider 컴포넌트 필수 (자동 Trigger 설정)
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

    [Header("텔레포트 UI 참조 (OpenTeleportUI 타입일 때만)")]
    [Tooltip("직접 참조 (선택 사항). 비어있으면 자동으로 찾습니다.")]
    [SerializeField] private TeleportUIWindow _teleportUIWindow;

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

    // TeleportUIWindow 캐싱
    private TeleportUIWindow _cachedTeleportUI;
    private bool _hasSearchedForUI = false;

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

        // OpenTeleportUI 타입이면 미리 TeleportUIWindow 찾기
        if (_portalData != null && _portalData.Type == PortalType.OpenTeleportUI)
        {
            FindTeleportUIWindow();
        }
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
            case PortalType.OpenTeleportUI:
                return "텔레포트";
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
    /// 포탈 활성화 (씬 전환, 텔레포트, UI 열기)
    /// </summary>
    private async void ActivatePortal(PlayerCharacter player)
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
                await ActivateSceneTransition();
                break;

            case PortalType.Teleport:
                ActivateTeleport();
                break;

            case PortalType.OpenTeleportUI:
                OpenTeleportUI();
                break;
        }

        // VFX 재생 (선택 사항)
        PlayPortalEffect();
    }

    /// <summary>
    /// 씬 전환 모드 (비동기 처리)
    /// 
    /// 변경사항:
    /// - 씬 전환 전 CharacterDataManager를 통한 전체 데이터 저장
    /// - 저장 완료 대기 후 씬 전환
    /// - 퀘스트 데이터 저장 포함
    /// - 위치 정보 자동 업데이트
    /// - 오류 처리 개선 (저장 실패 시 씬 전환 중단)
    /// - 발견 시스템 통합 (목적지 자동 발견)
    /// 
    /// 데이터 저장 순서:
    /// 1. 스폰 포인트 정보를 PlayerPrefs에 저장 (다음 씬에서 사용)
    /// 2. CharacterDataManager를 통한 전체 게임 데이터 저장
    ///    - 캐릭터 정보 (레벨, 경험치)
    ///    - 스탯 (HP, 마나, 모든 스탯)
    ///    - 장비
    ///    - 인벤토리 (아이템, 골드)
    ///    - 스킬 트리 및 스킬 슬롯
    ///    - 위치 (현재 씬, 좌표)
    /// 3. QuestManager를 통한 퀘스트 진행 상황 저장
    /// 4. 저장 완료 후 씬 전환
    /// </summary>
    private async Task ActivateSceneTransition()
    {
        string targetScene = _portalData.TargetSceneName;
        string spawnPoint = _portalData.TargetSpawnPointName;

        Log($"씬 전환 준비: {targetScene}" +
            (string.IsNullOrEmpty(spawnPoint) ? "" : $" -> {spawnPoint}"));

        // === 발견 시스템: 목적지 자동 발견 ===
        if (TeleportManager.Instance != null && !string.IsNullOrEmpty(spawnPoint))
        {
            TeleportManager.Instance.DiscoverDestination(targetScene, spawnPoint);
        }

        // === 1단계: 씬 전환 정보 저장 ===

        // 목표 씬과 스폰 포인트를 PlayerPrefs에 저장 (다음 씬에서 사용)
        if (!string.IsNullOrEmpty(spawnPoint))
        {
            PlayerPrefs.SetString("TargetSpawnPoint", spawnPoint);
        }
        else
        {
            PlayerPrefs.DeleteKey("TargetSpawnPoint");
        }

        PlayerPrefs.SetString("TargetScene", targetScene);
        PlayerPrefs.Save();

        Log("씬 전환 정보 저장 완료");

        // === 2단계: 전체 게임 데이터 저장 ===

        if (CharacterDataManager.Instance != null)
        {
            Log("캐릭터 데이터 저장 중...");

            // 비동기 저장 시작 및 완료 대기
            bool saveSuccess = await CharacterDataManager.Instance.CollectAndSaveData();

            if (!saveSuccess)
            {
                LogError("데이터 저장 실패! 씬 전환을 중단합니다.");
                _isActivating = false;
                return;
            }

            Log("캐릭터 데이터 저장 완료");
        }
        else
        {
            LogError("CharacterDataManager를 찾을 수 없습니다! 데이터가 손실될 수 있습니다.");
            // 사용자에게 경고 후 계속 진행할지 결정
            // 현재는 경고만 출력하고 진행
        }

        // === 3단계: 퀘스트 데이터 저장 ===

        if (QuestManager.Instance != null)
        {
            Log("퀘스트 데이터 저장 중...");
            QuestManager.Instance.SaveQuestProgress();
            Log("퀘스트 데이터 저장 완료");
        }

        // === 4단계: 씬 전환 실행 ===

        Log($"모든 데이터 저장 완료. 씬 전환 시작: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// 텔레포트 모드
    /// 발견 시스템 통합
    /// </summary>
    private void ActivateTeleport()
    {
        string locationName = _portalData.TeleportLocationName;

        Log($"텔레포트: {locationName}");

        // 발견 시스템: 현재 씬의 텔레포트 위치 발견
        if (TeleportManager.Instance != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            TeleportManager.Instance.DiscoverDestination(currentScene, locationName);
        }

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

    /// <summary>
    /// 텔레포트 UI 열기 모드 (개선됨)
    /// 
    /// 개선 사항:
    /// - 캐싱 메커니즘 추가
    /// - 직접 참조 지원
    /// - 더 나은 에러 메시지
    /// - 디버그 로그 강화
    /// </summary>
    private void OpenTeleportUI()
    {
        Log("텔레포트 UI 열기 시도");

        // 1단계: TeleportUIWindow 찾기 또는 캐시 사용
        TeleportUIWindow teleportUI = FindTeleportUIWindow();

        if (teleportUI == null)
        {
            LogError("TeleportUIWindow를 찾을 수 없습니다!");
            LogError("해결 방법:");
            LogError("1. GameCanvas에 TeleportUIWindow GameObject가 있는지 확인");
            LogError("2. TeleportUIWindow에 TeleportUIWindow.cs 컴포넌트가 있는지 확인");
            LogError("3. 또는 Portal Inspector에서 Teleport UI Window 필드에 직접 할당");
            _isActivating = false;
            return;
        }

        // 2단계: UI 열기
        Log($"TeleportUIWindow 찾음: {teleportUI.name}");

        // UI가 이미 열려있는지 확인
        if (teleportUI.IsOpen())
        {
            Log("TeleportUIWindow가 이미 열려 있습니다");
        }
        else
        {
            Log("TeleportUIWindow.Open() 호출");
            teleportUI.Open();
            Log("TeleportUIWindow 열기 완료");
        }

        _isActivating = false;
    }

    /// <summary>
    /// TeleportUIWindow 찾기 (캐싱 지원)
    /// 
    /// 우선순위:
    /// 1. Inspector에서 직접 할당된 참조 (_teleportUIWindow)
    /// 2. 이전에 찾은 캐시 (_cachedTeleportUI)
    /// 3. FindObjectOfType으로 새로 검색
    /// </summary>
    private TeleportUIWindow FindTeleportUIWindow()
    {
        // 1. 직접 할당된 참조 확인
        if (_teleportUIWindow != null)
        {
            Log("직접 할당된 TeleportUIWindow 사용");
            _cachedTeleportUI = _teleportUIWindow;
            return _teleportUIWindow;
        }

        // 2. 캐시된 참조 확인
        if (_cachedTeleportUI != null)
        {
            Log("캐시된 TeleportUIWindow 사용");
            return _cachedTeleportUI;
        }

        // 3. 아직 검색하지 않았거나 캐시가 없으면 검색
        if (!_hasSearchedForUI)
        {
            Log("TeleportUIWindow 검색 중...");
            _cachedTeleportUI = FindObjectOfType<TeleportUIWindow>(true); // includeInactive = true
            _hasSearchedForUI = true;

            if (_cachedTeleportUI != null)
            {
                Log($"TeleportUIWindow 찾음: {_cachedTeleportUI.name}");
            }
            else
            {
                LogWarning("TeleportUIWindow를 찾을 수 없습니다. 씬에 있는지 확인하세요.");
            }
        }

        return _cachedTeleportUI;
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

        // OpenTeleportUI 타입이면 TeleportUIWindow 확인
        if (_portalData.Type == PortalType.OpenTeleportUI)
        {
            if (_teleportUIWindow == null)
            {
                LogWarning("TeleportUIWindow가 직접 할당되지 않았습니다. 자동 검색을 시도합니다.");
            }
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
        string uiInfo = "";
        if (_portalData.Type == PortalType.OpenTeleportUI)
        {
            uiInfo = _teleportUIWindow != null ? $"\nUI 참조: {_teleportUIWindow.name}" : "\nUI 참조: 자동 검색";
        }

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Portal: {_portalData.PortalName}\n{_portalData.GetDebugInfo()}\n{questInfo}{uiInfo}"
        );
#endif
    }

    #endregion
}
