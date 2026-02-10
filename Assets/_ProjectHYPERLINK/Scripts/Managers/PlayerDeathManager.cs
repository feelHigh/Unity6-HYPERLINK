using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어 사망 및 리스폰 관리자 (완전 수정 버전)
/// 
/// 수정 사항:
/// - NavController 회전 초기화 추가
/// - SkillAnimationController 초기화 추가
/// - 플레이어 동적 검색 개선
/// </summary>
public class PlayerDeathManager : MonoBehaviour
{
    public static PlayerDeathManager Instance { get; private set; }

    [Header("참조")]
    [Tooltip("플레이어 GameObject (자동 검색 가능)")]
    [SerializeField] private GameObject _playerObject;

    [Header("리스폰 설정")]
    [Tooltip("리스폰 시 무적 시간 (초)")]
    [SerializeField] private float _respawnInvincibilityDuration = 2f;

    [Tooltip("리스폰 후 페이드 인 효과 사용")]
    [SerializeField] private bool _useRespawnFadeEffect = false;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    // 컴포넌트 캐시
    private PlayerCharacter _playerCharacter;
    private PlayerCombat _playerCombat;
    private Animator _animator;
    private PlayerStateController _stateController;
    private PlayerInputController _inputController;
    private PlayerNavController _navController; // ⭐ 추가
    private SkillAnimationController _skillAnimController; // ⭐ 추가

    // 상태 플래그
    private bool _isDead = false;

    // Animator 해시
    private static readonly int HASH_DEAD = Animator.StringToHash("Dead");
    private static readonly int HASH_SPEED = Animator.StringToHash("Speed");
    private static readonly int HASH_REVIVE = Animator.StringToHash("Revive");

    #region 초기화

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Awake에서는 플레이어를 검색하지 않음 (런타임 생성)
    }

    private IEnumerator Start()
    {
        // 플레이어 생성 대기
        float timeout = 5f;
        float elapsed = 0f;

        while (_playerObject == null && elapsed < timeout)
        {
            _playerObject = GameObject.FindGameObjectWithTag("Player");

            if (_playerObject != null)
            {
                CachePlayerComponents();
                Log("플레이어 참조 및 컴포넌트 캐싱 완료");
                break;
            }

            elapsed += 0.1f;
            yield return WaitForSecondsCache.Get(0.1f);
        }

        if (_playerObject == null)
        {
            LogWarning("플레이어를 찾을 수 없습니다 (타임아웃)");
        }
    }

    private void OnEnable()
    {
        PlayerCharacter.OnPlayerDead += OnPlayerDiedHandler;
    }

    private void OnDisable()
    {
        PlayerCharacter.OnPlayerDead -= OnPlayerDiedHandler;
    }

    /// <summary>
    /// 플레이어 컴포넌트 캐싱
    /// </summary>
    private void CachePlayerComponents()
    {
        if (_playerObject == null)
        {
            LogWarning("플레이어 오브젝트가 null입니다");
            return;
        }

        _playerCharacter = _playerObject.GetComponent<PlayerCharacter>();
        _playerCombat = _playerObject.GetComponent<PlayerCombat>();
        _animator = _playerObject.GetComponent<Animator>();
        _stateController = _playerObject.GetComponent<PlayerStateController>();
        _inputController = _playerObject.GetComponent<PlayerInputController>();
        _navController = _playerObject.GetComponent<PlayerNavController>(); // ⭐ 추가
        _skillAnimController = _playerObject.GetComponent<SkillAnimationController>(); // ⭐ 추가

        // 검증
        if (_playerCharacter == null)
            LogError("PlayerCharacter를 찾을 수 없습니다!");

        if (_animator == null)
            LogError("Animator를 찾을 수 없습니다!");

        if (_navController == null)
            LogWarning("PlayerNavController를 찾을 수 없습니다!");

        if (_skillAnimController == null)
            LogWarning("SkillAnimationController를 찾을 수 없습니다!");
    }

    #endregion

    #region 사망 처리

    private void OnPlayerDiedHandler()
    {
        if (_isDead)
        {
            LogWarning("이미 사망 상태입니다");
            return;
        }

        // 플레이어 참조가 없으면 지금 찾기
        if (_playerObject == null)
        {
            _playerObject = GameObject.FindGameObjectWithTag("Player");
            CachePlayerComponents();
        }

        if (_playerObject == null)
        {
            LogError("플레이어를 찾을 수 없습니다!");
            return;
        }

        Log("플레이어 사망 처리 시작");
        _isDead = true;
        HandleDeath();
    }

    private void HandleDeath()
    {
        TriggerDeathAnimation();
        DisablePlayerInput();
        ClearPlayerStates();

        if (_playerCombat != null)
        {
            _playerCombat.Die();
        }

        Log("사망 처리 완료 - UI 대기 중");
    }

    private void TriggerDeathAnimation()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(HASH_DEAD);
            _animator.SetFloat(HASH_SPEED, 0f);
            Log("Animator Dead 트리거 실행");
        }
        else
        {
            LogWarning("Animator가 없어 사망 애니메이션을 재생할 수 없습니다");
        }
    }

    private void DisablePlayerInput()
    {
        if (_inputController != null)
        {
            _inputController.enabled = false;
            Log("입력 차단 완료");
        }

        if (_stateController != null)
        {
            _stateController.ResetAllStates();
        }
    }

    private void ClearPlayerStates()
    {
        if (_stateController != null)
        {
            _stateController.ResetAllStates();
        }

        if (_playerCharacter != null)
        {
            _playerCharacter.ClearAllTemporaryBuffs();
        }

        Log("플레이어 상태 초기화 완료");
    }

    #endregion

    #region 리스폰 처리

    public void RespawnPlayer()
    {
        if (!_isDead)
        {
            LogWarning("사망 상태가 아닙니다");
            return;
        }

        Log("플레이어 리스폰 시작");
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        if (_useRespawnFadeEffect)
        {
            yield return WaitForSecondsCache.Get(0.3f);
        }

        // 1. 체력/마나 회복 + 사망 상태 해제
        RestorePlayerResources();

        // 2. 스폰 포인트로 이동
        TeleportToSpawnPoint();

        // 3. Animator 상태 복구
        ResetAnimatorState();

        // 4. 입력 재활성화
        EnablePlayerInput();

        // ⭐ 5. NavController 상태 초기화 (회전 포함)
        if (_navController != null)
        {
            _navController.ResetAfterRespawn();
        }
        else
        {
            LogWarning("NavController가 null입니다");
        }

        // ⭐ 6. SkillAnimationController 상태 초기화
        if (_skillAnimController != null)
        {
            _skillAnimController.ResetAfterRespawn();
        }

        // 7. 사망 상태 해제
        _isDead = false;

        Log("플레이어 리스폰 완료");

        if (_respawnInvincibilityDuration > 0f)
        {
            yield return StartCoroutine(GrantRespawnInvincibility());
        }
    }

    private void RestorePlayerResources()
    {
        if (_playerCharacter == null)
            return;

        // Revive() 메서드 사용 (사망 플래그 + 리소스 회복)
        _playerCharacter.Revive();

        Log($"체력/마나 회복 완료 - HP: {_playerCharacter.CurrentHealth}/{_playerCharacter.MaxHealth}");
    }

    private void TeleportToSpawnPoint()
    {
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.TeleportToSpawnPoint();
            Log("플레이어를 기본 스폰 포인트로 이동");
        }
        else
        {
            LogError("PlayerSpawner를 찾을 수 없습니다!");
        }
    }

    private void ResetAnimatorState()
    {
        if (_animator != null)
        {
            // Revive 트리거가 있으면 사용, 없으면 강제 전환
            if (HasParameter(_animator, HASH_REVIVE))
            {
                _animator.SetTrigger(HASH_REVIVE);
                Log("Animator Revive 트리거 실행");
            }
            else
            {
                _animator.Play("Idle", 0, 0f);
                Log("Animator 강제 Idle 전환");
            }
        }
    }

    private void EnablePlayerInput()
    {
        if (_inputController != null)
        {
            _inputController.enabled = true;
            Log("입력 재활성화 완료");
        }
    }

    private IEnumerator GrantRespawnInvincibility()
    {
        Log($"무적 시간 시작 ({_respawnInvincibilityDuration}초)");
        yield return WaitForSecondsCache.Get(_respawnInvincibilityDuration);
        Log("무적 시간 종료");
    }

    #endregion

    #region 유틸리티

    private bool HasParameter(Animator animator, int paramHash)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == paramHash)
                return true;
        }

        return false;
    }

    public bool IsDead => _isDead;

    #endregion

    #region 로깅

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[PlayerDeathManager] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[PlayerDeathManager] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogError(string message)
    {
        Debug.LogError($"[PlayerDeathManager] {message}");
    }

    #endregion
}
