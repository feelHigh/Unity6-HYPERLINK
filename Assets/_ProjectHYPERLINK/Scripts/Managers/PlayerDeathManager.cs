using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어 사망 및 리스폰 관리자
/// 
/// 역할:
/// - 사망 처리 통합 관리
/// - 리스폰 로직 실행
/// - Animator Dead 트리거
/// - 입력 차단
/// - 상태 초기화
/// 
/// 싱글톤 패턴 사용
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

        // 플레이어 자동 검색
        if (_playerObject == null)
        {
            _playerObject = GameObject.FindGameObjectWithTag("Player");
            if (_playerObject == null)
            {
                LogWarning("Player GameObject를 찾을 수 없습니다");
            }
        }

        // 컴포넌트 캐시
        CachePlayerComponents();
    }

    private void OnEnable()
    {
        // 사망 이벤트 구독
        PlayerCharacter.OnPlayerDead += OnPlayerDiedHandler;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerCharacter.OnPlayerDead -= OnPlayerDiedHandler;
    }

    /// <summary>
    /// 플레이어 컴포넌트 캐싱
    /// </summary>
    private void CachePlayerComponents()
    {
        if (_playerObject == null)
            return;

        _playerCharacter = _playerObject.GetComponent<PlayerCharacter>();
        _playerCombat = _playerObject.GetComponent<PlayerCombat>();
        _animator = _playerObject.GetComponent<Animator>();
        _stateController = _playerObject.GetComponent<PlayerStateController>();
        _inputController = _playerObject.GetComponent<PlayerInputController>();

        // 검증
        if (_playerCharacter == null)
            LogError("PlayerCharacter를 찾을 수 없습니다!");

        if (_animator == null)
            LogError("Animator를 찾을 수 없습니다!");
    }

    #endregion

    #region 사망 처리

    /// <summary>
    /// 플레이어 사망 이벤트 핸들러
    /// </summary>
    private void OnPlayerDiedHandler()
    {
        if (_isDead)
        {
            LogWarning("이미 사망 상태입니다");
            return;
        }

        Log("플레이어 사망 처리 시작");
        _isDead = true;

        // 사망 처리 실행
        HandleDeath();
    }

    /// <summary>
    /// 사망 처리 로직
    /// </summary>
    private void HandleDeath()
    {
        // 1. Animator Dead 트리거
        TriggerDeathAnimation();

        // 2. 입력 차단
        DisablePlayerInput();

        // 3. 상태 초기화 (디버프 제거)
        ClearPlayerStates();

        // 4. PlayerCombat.Die() 호출 (사운드 재생)
        if (_playerCombat != null)
        {
            _playerCombat.Die();
        }

        Log("사망 처리 완료 - UI 대기 중");
    }

    /// <summary>
    /// 사망 애니메이션 트리거
    /// </summary>
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

    /// <summary>
    /// 플레이어 입력 차단
    /// </summary>
    private void DisablePlayerInput()
    {
        if (_inputController != null)
        {
            _inputController.enabled = false;
            Log("입력 차단 완료");
        }

        // StateController도 비활성화
        if (_stateController != null)
        {
            _stateController.ResetAllStates();
        }
    }

    /// <summary>
    /// 플레이어 상태 초기화
    /// </summary>
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

    /// <summary>
    /// 플레이어 리스폰 (DeathUIPanel에서 호출)
    /// </summary>
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

    /// <summary>
    /// 리스폰 코루틴
    /// </summary>
    private IEnumerator RespawnCoroutine()
    {
        // 1. 페이드 효과 (옵션)
        if (_useRespawnFadeEffect)
        {
            // TODO: 화면 페이드 효과 추가
            yield return new WaitForSeconds(0.3f);
        }

        // 2. 체력/마나 완전 회복
        RestorePlayerResources();

        // 3. 스폰 포인트로 이동
        TeleportToSpawnPoint();

        // 4. Animator 상태 복구
        ResetAnimatorState();

        // 5. 입력 재활성화
        EnablePlayerInput();

        // 6. 사망 상태 해제
        _isDead = false;

        Log("플레이어 리스폰 완료");

        // 7. 무적 시간 부여 (옵션)
        if (_respawnInvincibilityDuration > 0f)
        {
            yield return StartCoroutine(GrantRespawnInvincibility());
        }
    }

    /// <summary>
    /// 체력/마나 완전 회복
    /// </summary>
    /// <summary>
    /// 플레이어 부활 (체력/마나 회복 + 사망 상태 해제)
    /// </summary>
    private void RestorePlayerResources()
    {
        if (_playerCharacter == null)
            return;
        
        _playerCharacter.Revive();

        Log($"플레이어 부활 완료 - HP: {_playerCharacter.CurrentHealth}/{_playerCharacter.MaxHealth}");
    }

    /// <summary>
    /// 스폰 포인트로 이동
    /// </summary>
    private void TeleportToSpawnPoint()
    {
        if (PlayerSpawner.Instance != null)
        {
            // PlayerSpawner의 TeleportToSpawnPoint 사용 (기존 플레이어 유지)
            PlayerSpawner.Instance.TeleportToSpawnPoint();
            Log("플레이어를 기본 스폰 포인트로 이동");
        }
        else
        {
            LogError("PlayerSpawner를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// Animator 상태 복구
    /// </summary>
    private void ResetAnimatorState()
    {
        if (_animator != null)
        {
            // Revive 트리거 (있다면)
            if (HasParameter(_animator, HASH_REVIVE))
            {
                _animator.SetTrigger(HASH_REVIVE);
            }
            else
            {
                // Idle 상태로 강제 전환
                _animator.Play("Idle", 0, 0f);
            }

            Log("Animator 상태 복구 완료");
        }
    }

    /// <summary>
    /// 입력 재활성화
    /// </summary>
    private void EnablePlayerInput()
    {
        if (_inputController != null)
        {
            _inputController.enabled = true;
            Log("입력 재활성화 완료");
        }
    }

    /// <summary>
    /// 리스폰 후 무적 시간 부여
    /// </summary>
    private IEnumerator GrantRespawnInvincibility()
    {
        Log($"무적 시간 시작 ({_respawnInvincibilityDuration}초)");

        // TODO: 무적 상태 플래그 추가 및 IDamageable 체크 로직 수정 필요
        // 임시로 딜레이만 부여
        yield return new WaitForSeconds(_respawnInvincibilityDuration);

        Log("무적 시간 종료");
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// Animator에 특정 파라미터가 있는지 확인
    /// </summary>
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

    /// <summary>
    /// 현재 사망 상태 확인
    /// </summary>
    public bool IsDead => _isDead;

    #endregion

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[PlayerDeathManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[PlayerDeathManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[PlayerDeathManager] {message}");
    }

    #endregion
}
