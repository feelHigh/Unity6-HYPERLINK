using UnityEngine;

/// <summary>
/// 플레이어 키보드 입력 통합 컨트롤러
/// 
/// 키 바인딩:
/// - Number 1: 레드 소다 사용
/// - C: 캐릭터 패널
/// - Tab: 미니맵 (TODO)
/// - I: 인벤토리 (TODO)
/// - M: 맵 & 퀘스트 (TODO)
/// - Esc: 옵션 패널 (LoginScene 이동)
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject _characterPanel;

    private PlayerCharacter _playerCharacter;

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacter>();

        if (_playerCharacter == null)
        {
            Debug.LogError("[PlayerInputController] PlayerCharacter 컴포넌트가 필요합니다!");
            enabled = false;
        }
    }

    private void Update()
    {
        HandleConsumableInput();
        HandleUIInput();
    }

    private void HandleConsumableInput()
    {
        // Number 1: 레드 소다 사용
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            _playerCharacter.UseRedSoda();
        }
    }

    private void HandleUIInput()
    {
        // C: 캐릭터 패널 토글
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (_characterPanel != null)
            {
                _characterPanel.SetActive(!_characterPanel.activeSelf);
            }
        }

        // Tab: 미니맵 (TODO)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("TODO: 미니맵 표시");
        }

        // I: 인벤토리 (TODO)
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("TODO: 인벤토리 표시");
        }

        // M: 맵 & 퀘스트 (TODO)
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("TODO: 맵/퀘스트 표시");
        }

        // Esc: LoginScene으로 이동
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadLoginScene();
        }
    }

    private void LoadLoginScene()
    {
        Debug.Log("LoginScene으로 이동");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }
}
