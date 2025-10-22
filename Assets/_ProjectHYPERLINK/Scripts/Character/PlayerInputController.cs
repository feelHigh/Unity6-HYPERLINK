using UnityEngine;

/// <summary>
/// 플레이어 키보드 입력 컨트롤러 (소비 아이템 전용)
/// 
/// 역할:
/// - 소비 아이템 키 바인딩만 처리
/// - UI 입력은 CharacterUIController에서 처리
/// 
/// 키 바인딩:
/// - Number 1: 레드 소다 사용
/// </summary>
public class PlayerInputController : MonoBehaviour
{
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
    }

    /// <summary>
    /// 소비 아이템 입력 처리
    /// </summary>
    private void HandleConsumableInput()
    {
        // Number 1: 레드 소다 사용
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            _playerCharacter.UseRedSoda();
        }
    }
}
