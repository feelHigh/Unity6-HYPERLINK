using UnityEngine;

/// <summary>
/// 문 상호작용 구현 예시
/// 
/// 기능:
/// - 열기/닫기 토글
/// - 애니메이션 재생
/// - 사운드 효과
/// - 잠금 시스템
/// - 키 아이템 요구 (선택)
/// 
/// 레벨 디자이너 사용법:
/// 1. 문 GameObject 생성
/// 2. Door 컴포넌트 추가
/// 3. Collider 추가 (Trigger 체크)
/// 4. Animator 설정 (선택)
/// 5. Inspector에서 설정 조정
/// 
/// Hierarchy 예시:
/// Door (GameObject)
/// ├─ Door (Script)
/// ├─ Box Collider (Is Trigger = true)
/// ├─ Animator (선택)
/// └─ DoorModel (3D 모델)
/// 
/// 확장 가능:
/// - 슬라이딩 도어
/// - 회전 도어
/// - 비밀 문
/// - 보스룸 입구
/// </summary>
[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour, IInteractable
{
    [Header("문 설정")]
    [SerializeField] private bool _isLocked = false;
    [Tooltip("잠금 해제에 필요한 키 아이템 (선택)")]
    [SerializeField] private string _requiredKeyItemId = "";

    [Header("상호작용 설정")]
    [SerializeField] private float _interactionRange = 2.5f;
    [SerializeField] private bool _canClose = true; // 한 번 열면 닫을 수 없는 문인가?

    [Header("애니메이션 (선택)")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _openTrigger = "Open";
    [SerializeField] private string _closeTrigger = "Close";

    [Header("사운드 (선택)")]
    [SerializeField] private AudioClip _openSound;
    [SerializeField] private AudioClip _closeSound;
    [SerializeField] private AudioClip _lockedSound;
    private AudioSource _audioSource;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    // 문 상태
    private bool _isOpen = false;

    private void Awake()
    {
        // Collider를 Trigger로 설정 (자동)
        Collider collider = GetComponent<Collider>();
        if (collider != null && !collider.isTrigger)
        {
            collider.isTrigger = true;
            Log("Collider를 Trigger로 자동 설정했습니다");
        }

        // AudioSource 추가 (필요 시)
        if (_openSound != null || _closeSound != null || _lockedSound != null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    #region IInteractable 구현

    /// <summary>
    /// 문 열기/닫기
    /// </summary>
    public void Interact(PlayerCharacter player)
    {
        // 잠금 확인
        if (_isLocked)
        {
            if (!string.IsNullOrEmpty(_requiredKeyItemId))
            {
                // TODO: 플레이어 인벤토리에서 키 확인
                // if (!player.Inventory.HasItem(_requiredKeyItemId))
                // {
                //     PlaySound(_lockedSound);
                //     Log("문이 잠겨있습니다. 키가 필요합니다.");
                //     return;
                // }

                // 키가 있으면 잠금 해제
                _isLocked = false;
                Log($"키 아이템 '{_requiredKeyItemId}'로 문을 열었습니다!");
            }
            else
            {
                // 키 없이 잠김
                PlaySound(_lockedSound);
                Log("문이 잠겨있습니다");
                return;
            }
        }

        // 문 토글
        if (_isOpen)
        {
            // 닫기
            if (_canClose)
            {
                CloseDoor();
            }
        }
        else
        {
            // 열기
            OpenDoor();
        }
    }

    /// <summary>
    /// 상호작용 프롬프트 텍스트
    /// </summary>
    public string GetInteractionPrompt()
    {
        if (_isLocked)
        {
            if (!string.IsNullOrEmpty(_requiredKeyItemId))
            {
                return $"잠긴 문 (키 필요: {_requiredKeyItemId})";
            }
            return "문이 잠겨있습니다";
        }

        if (_isOpen)
        {
            return _canClose ? "문 닫기" : "";
        }

        return "문 열기";
    }

    /// <summary>
    /// 상호작용 가능 여부
    /// </summary>
    public bool CanInteract(PlayerCharacter player)
    {
        // 잠긴 문도 시도는 가능 (잠금 메시지 표시용)
        if (_isLocked)
            return true;

        // 이미 열린 문은 닫을 수 있을 때만 상호작용 가능
        if (_isOpen)
            return _canClose;

        // 닫힌 문은 항상 상호작용 가능
        return true;
    }

    /// <summary>
    /// 상호작용 범위
    /// </summary>
    public float GetInteractionRange()
    {
        return _interactionRange;
    }

    #endregion

    #region 문 동작

    /// <summary>
    /// 문 열기
    /// </summary>
    private void OpenDoor()
    {
        _isOpen = true;

        // 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger(_openTrigger);
        }

        // 사운드
        PlaySound(_openSound);

        Log("문을 열었습니다");
    }

    /// <summary>
    /// 문 닫기
    /// </summary>
    private void CloseDoor()
    {
        _isOpen = false;

        // 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger(_closeTrigger);
        }

        // 사운드
        PlaySound(_closeSound);

        Log("문을 닫았습니다");
    }

    #endregion

    #region Public 메서드 (외부 제어용)

    /// <summary>
    /// 외부에서 문 잠금 설정
    /// 퀘스트, 트리거 등에서 사용
    /// </summary>
    public void SetLocked(bool locked)
    {
        _isLocked = locked;
        Log($"문 잠금 상태 변경: {locked}");
    }

    /// <summary>
    /// 외부에서 문 강제로 열기
    /// 컷신, 이벤트 등에서 사용
    /// </summary>
    public void ForceOpen()
    {
        if (!_isOpen)
        {
            _isLocked = false;
            OpenDoor();
        }
    }

    /// <summary>
    /// 외부에서 문 강제로 닫기
    /// </summary>
    public void ForceClose()
    {
        if (_isOpen)
        {
            CloseDoor();
        }
    }

    #endregion

    #region 헬퍼 메서드

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[Door - {gameObject.name}] {message}");
        }
    }

    #endregion

    #region 시각화 (에디터용)

    private void OnDrawGizmos()
    {
        // 상호작용 범위 시각화 (노란색 와이어 구)
        Gizmos.color = _isLocked ? Color.red : (_isOpen ? Color.green : Color.yellow);
        Gizmos.DrawWireSphere(transform.position, _interactionRange);
    }

    #endregion
}
