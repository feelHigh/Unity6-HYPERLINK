using UnityEngine;

/// <summary>
/// 입구/출구 포탈 (맵 생성 시스템용)
/// 
/// 기능:
/// - MapGenerator와 연동
/// - Enter: 이전 지역으로 이동
/// - Exit: 다음 지역으로 이동
/// - Portal 시스템과 통합
/// 
/// 사용법:
/// - MapGenerator가 자동으로 생성
/// - Direction 필드로 "Enter" 또는 "Exit" 지정
/// </summary>
public class EnterAndExit : MonoBehaviour, IInteractable
{
    [Header("방향 설정")]
    [SerializeField] private string _direction = "Exit"; // "Enter" 또는 "Exit"
    [SerializeField] private string _targetSceneName = "";
    [SerializeField] private string _targetSpawnPoint = "";

    [Header("상호작용 설정")]
    [SerializeField] private float _interactionRange = 3.0f;
    [SerializeField] private bool _autoActivate = true;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private bool _isActivating = false;

    public string Direction => _direction;

    #region Unity Lifecycle

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_autoActivate || !other.CompareTag("Player")) return;

        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            Log($"자동 작동: {_direction}");
            Interact(player);
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

        if (string.IsNullOrEmpty(_targetSceneName))
        {
            LogError("대상 씬이 설정되지 않았습니다");
            return;
        }

        _isActivating = true;
        ActivateTransition();
    }

    public bool CanInteract(PlayerCharacter player)
    {
        return !_isActivating && !string.IsNullOrEmpty(_targetSceneName);
    }

    public string GetInteractionPrompt()
    {
        // 호환성 유지용
        return _direction == "Enter" ? "이전 지역" : "다음 지역";
    }

    public float GetInteractionRange()
    {
        return _interactionRange;
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Portal;
    }

    public string GetInteractionName()
    {
        return _direction == "Enter" ? "입구" : "출구";
    }

    #endregion

    #region 전환 로직

    private void ActivateTransition()
    {
        Log($"{_direction} -> {_targetSceneName}");

        // 플레이어 위치 저장
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.SavePlayerPosition();
        }

        // 스폰 포인트 정보 저장
        if (!string.IsNullOrEmpty(_targetSpawnPoint))
        {
            PlayerPrefs.SetString("TargetSpawnPoint", _targetSpawnPoint);
            PlayerPrefs.Save();
        }
        else
        {
            PlayerPrefs.DeleteKey("TargetSpawnPoint");
        }

        // 씬 로드
        UnityEngine.SceneManagement.SceneManager.LoadScene(_targetSceneName);
    }

    #endregion

    #region 공개 메서드 (MapGenerator용)

    /// <summary>
    /// 대상 씬 설정 (MapGenerator가 호출)
    /// </summary>
    public void SetupTransition(string direction, string targetScene, string spawnPoint = "")
    {
        _direction = direction;
        _targetSceneName = targetScene;
        _targetSpawnPoint = spawnPoint;

        Log($"설정 완료: {direction} -> {targetScene}" +
            (string.IsNullOrEmpty(spawnPoint) ? "" : $" ({spawnPoint})"));
    }

    #endregion

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[EnterAndExit '{name}'] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[EnterAndExit '{name}'] {message}");
    }

    private void OnDrawGizmos()
    {
        // 입구/출구 시각화
        Gizmos.color = _direction == "Enter" ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, _interactionRange);

        // 방향 표시
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

    #endregion
}
