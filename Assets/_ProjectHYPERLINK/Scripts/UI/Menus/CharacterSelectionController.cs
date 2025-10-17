using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using System;

/// <summary>
/// 캐릭터 선택/생성 화면 컨트롤러
/// 
/// 기능:
/// - 기존 캐릭터 표시 및 계속하기
/// - 새 캐릭터 생성 (직업 선택)
/// - 캐릭터 삭제
/// - Cloud Save 통합
/// </summary>
public class CharacterSelectionController : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject _existingCharacterPanel;
    [SerializeField] private GameObject _classSelectionPanel;
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private GameObject _errorPanel;

    [Header("기존 캐릭터 UI")]
    [SerializeField] private TextMeshProUGUI _characterNameText;
    [SerializeField] private TextMeshProUGUI _classText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _strengthText;
    [SerializeField] private TextMeshProUGUI _dexterityText;
    [SerializeField] private TextMeshProUGUI _intelligenceText;
    [SerializeField] private TextMeshProUGUI _vitalityText;
    [SerializeField] private TextMeshProUGUI _playTimeText;
    [SerializeField] private TextMeshProUGUI _lastPlayedText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _deleteButton;

    [Header("직업 선택 UI")]
    [SerializeField] private Button _warriorButton;
    [SerializeField] private Button _mageButton;
    [SerializeField] private Button _archerButton;
    [SerializeField] private TMP_InputField _characterNameInput;
    [SerializeField] private Button _createCharacterButton;

    [Header("피드백 UI")]
    [SerializeField] private TextMeshProUGUI _loadingText;
    [SerializeField] private TextMeshProUGUI _errorText;

    [Header("씬 설정")]
    [SerializeField] private string _gameScene = "TutorialTestScene";

    private CharacterSaveData _currentCharacterData;
    private CharacterClass _selectedClass;
    private bool _isCreatingCharacter = false;

    private async void Start()
    {
        SetupButtonListeners();
        HideAllPanels();
        await CheckForExistingCharacter();
    }

    private void SetupButtonListeners()
    {
        _continueButton.onClick.AddListener(OnContinueClicked);
        _deleteButton.onClick.AddListener(OnDeleteClicked);

        _warriorButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Warrior));
        _mageButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Mage));
        _archerButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Archer));

        _createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
    }

    /// <summary>
    /// 기존 캐릭터 확인
    /// Cloud Save에서 데이터 로드
    /// </summary>
    private async Task CheckForExistingCharacter()
    {
        ShowLoading("캐릭터 데이터 로드 중...");

        try
        {
            bool hasCharacter = await CloudSaveManager.Instance.HasCharacterAsync();

            if (hasCharacter)
            {
                _currentCharacterData = await CloudSaveManager.Instance.LoadCharacterDataAsync();

                if (_currentCharacterData != null)
                {
                    DisplayExistingCharacter(_currentCharacterData);
                }
                else
                {
                    ShowClassSelection();
                }
            }
            else
            {
                ShowClassSelection();
            }
        }
        catch (Exception e)
        {
            ShowError($"데이터 로드 실패: {e.Message}");
            ShowClassSelection();
        }
        finally
        {
            HideLoading();
        }
    }

    /// <summary>
    /// 기존 캐릭터 정보 표시
    /// </summary>
    private void DisplayExistingCharacter(CharacterSaveData data)
    {
        _characterNameText.text = data.character.characterName;
        _classText.text = data.character.characterClass;
        _levelText.text = $"레벨 {data.character.level}";

        _strengthText.text = $"힘: {data.stats.baseStats.strength}";
        _dexterityText.text = $"민첩: {data.stats.baseStats.dexterity}";
        _intelligenceText.text = $"지능: {data.stats.baseStats.intelligence}";
        _vitalityText.text = $"활력: {data.stats.baseStats.vitality}";

        TimeSpan playTime = TimeSpan.FromSeconds(data.metadata.playTimeSeconds);
        _playTimeText.text = $"플레이: {playTime.Hours}시간 {playTime.Minutes}분";

        DateTime lastPlayed = DateTime.Parse(data.metadata.lastPlayed);
        TimeSpan timeSince = DateTime.UtcNow - lastPlayed;
        _lastPlayedText.text = $"최근 플레이: {FormatTimeSince(timeSince)}";

        _existingCharacterPanel.SetActive(true);
    }

    private void ShowClassSelection()
    {
        _classSelectionPanel.SetActive(true);
        _characterNameInput.gameObject.SetActive(false);
        _createCharacterButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 직업 선택
    /// </summary>
    private void OnClassSelected(CharacterClass characterClass)
    {
        _selectedClass = characterClass;
        _characterNameInput.gameObject.SetActive(true);
        _createCharacterButton.gameObject.SetActive(true);
        _characterNameInput.Select();

        Debug.Log($"직업 선택: {characterClass}");
    }

    /// <summary>
    /// 캐릭터 생성
    /// </summary>
    private async void OnCreateCharacterClicked()
    {
        string characterName = _characterNameInput.text.Trim();

        if (string.IsNullOrEmpty(characterName))
        {
            ShowError("캐릭터 이름을 입력하세요");
            return;
        }

        if (characterName.Length < 3)
        {
            ShowError("캐릭터 이름은 3자 이상이어야 합니다");
            return;
        }

        if (_isCreatingCharacter)
        {
            return;
        }

        _isCreatingCharacter = true;
        ShowLoading("캐릭터 생성 중...");

        try
        {
            CharacterSaveData newCharacter = CharacterSaveData.CreateNew(characterName, _selectedClass);
            bool success = await CloudSaveManager.Instance.SaveCharacterDataAsync(newCharacter);

            if (success)
            {
                _currentCharacterData = newCharacter;
                ShowLoading("게임 로드 중...");
                await Task.Delay(1000);
                LoadGameScene();
            }
            else
            {
                ShowError("캐릭터 생성 실패");
                _isCreatingCharacter = false;
            }
        }
        catch (Exception e)
        {
            ShowError($"오류: {e.Message}");
            _isCreatingCharacter = false;
        }
        finally
        {
            HideLoading();
        }
    }

    private void OnContinueClicked()
    {
        if (_currentCharacterData != null)
        {
            ShowLoading("게임 로드 중...");
            LoadGameScene();
        }
    }

    /// <summary>
    /// 캐릭터 삭제
    /// </summary>
    private async void OnDeleteClicked()
    {
        bool confirm = await ShowConfirmDialog("캐릭터를 영구 삭제하시겠습니까?");

        if (confirm)
        {
            ShowLoading("캐릭터 삭제 중...");

            bool success = await CloudSaveManager.Instance.DeleteCharacterAsync();

            if (success)
            {
                _currentCharacterData = null;
                _existingCharacterPanel.SetActive(false);
                ShowClassSelection();
            }
            else
            {
                ShowError("캐릭터 삭제 실패");
            }

            HideLoading();
        }
    }

    private void LoadGameScene()
    {
        GameSessionManager.Instance.SetCharacterData(_currentCharacterData);
        SceneManager.LoadScene(_gameScene);
    }

    private void HideAllPanels()
    {
        _existingCharacterPanel.SetActive(false);
        _classSelectionPanel.SetActive(false);
        _loadingPanel.SetActive(false);
        _errorPanel.SetActive(false);
    }

    private void ShowLoading(string message)
    {
        _loadingPanel.SetActive(true);
        _loadingText.text = message;
    }

    private void HideLoading()
    {
        _loadingPanel.SetActive(false);
    }

    private void ShowError(string message)
    {
        _errorPanel.SetActive(true);
        _errorText.text = message;
        HideErrorAfterDelay();
    }

    private async void HideErrorAfterDelay()
    {
        await Task.Delay(5000);
        _errorPanel.SetActive(false);
    }

    private async Task<bool> ShowConfirmDialog(string message)
    {
        Debug.Log($"확인: {message}");
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 시간 경과 포맷
    /// </summary>
    private string FormatTimeSince(TimeSpan time)
    {
        if (time.TotalDays >= 1)
            return $"{(int)time.TotalDays}일 전";
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}시간 전";
        if (time.TotalMinutes >= 1)
            return $"{(int)time.TotalMinutes}분 전";
        return "방금";
    }
}
