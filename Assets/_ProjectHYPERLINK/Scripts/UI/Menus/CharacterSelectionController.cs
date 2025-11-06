using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Threading.Tasks;

/// <summary>
/// 캐릭터 선택 및 생성 화면 컨트롤러
/// 
/// 변경사항:
/// - CharacterStats SO 참조 3개 추가 (각 직업별)
/// - OnCreateCharacterClicked() 수정 - baseStats 전달
/// - GetBaseStatsForClass() 헬퍼 메서드 추가
/// </summary>
public class CharacterSelectionController : MonoBehaviour
{
    [Header("UI 패널")]
    [SerializeField] private GameObject _existingCharacterPanel;
    [SerializeField] private GameObject _classSelectionPanel;

    [Header("기존 캐릭터 정보")]
    [SerializeField] private TextMeshProUGUI _characterNameText;
    [SerializeField] private TextMeshProUGUI _classText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _strengthText;
    [SerializeField] private TextMeshProUGUI _dexterityText;
    [SerializeField] private TextMeshProUGUI _intelligenceText;
    [SerializeField] private TextMeshProUGUI _vitalityText;
    [SerializeField] private TextMeshProUGUI _playTimeText;
    [SerializeField] private TextMeshProUGUI _lastPlayedText;

    [Header("버튼")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private Button _laonButton;
    [SerializeField] private Button _sianButton;
    [SerializeField] private Button _yujinButton;
    [SerializeField] private Button _createCharacterButton;

    [Header("캐릭터 생성")]
    [SerializeField] private TMP_InputField _characterNameInput;

    [Header("로딩 UI")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _loadingText;

    [Header("에러 UI")]
    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private TextMeshProUGUI _errorText;
    [SerializeField] private Button _errorOkButton;

    [Header("씬 설정")]
    [SerializeField] private string _gameScene = "TutorialTestScene";

    [Header("캐릭터 기본 스탯 (Unity 에디터 설정)")]
    [SerializeField] private CharacterStats _laonBaseStats;   // Laon(Warrior) 기본 스탯
    [SerializeField] private CharacterStats _sianBaseStats;   // Sian(Mage) 기본 스탯
    [SerializeField] private CharacterStats _yujinBaseStats;  // Yujin(Archer) 기본 스탯

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

        _laonButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Laon));
        _sianButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Sian));
        _yujinButton.onClick.AddListener(() => OnClassSelected(CharacterClass.Yujin));

        _createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);

        if (_errorOkButton != null)
        {
            _errorOkButton.onClick.AddListener(HideError);
        }
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

        // 마지막 씬 정보 추가
        string lastScene = !string.IsNullOrEmpty(data.position?.scene) ? data.position.scene : _gameScene;
        _lastPlayedText.text = $"최근 플레이: {FormatTimeSince(timeSince)} ({lastScene})";

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
    /// 
    /// 변경사항:
    /// - GetBaseStatsForClass()로 선택한 직업의 CharacterStats SO 가져오기
    /// - CharacterSaveData.CreateNew()에 baseStats 전달
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
            // 선택한 직업에 맞는 CharacterStats SO 가져오기
            CharacterStats baseStats = GetBaseStatsForClass(_selectedClass);

            if (baseStats == null)
            {
                ShowError($"{_selectedClass} 직업의 기본 스탯이 설정되지 않았습니다!\nInspector에서 CharacterStats를 할당하세요.");
                _isCreatingCharacter = false;
                HideLoading();
                return;
            }

            // CharacterStats를 포함하여 새 캐릭터 생성
            CharacterSaveData newCharacter = CharacterSaveData.CreateNew(characterName, _selectedClass, baseStats);
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

        if (!confirm)
        {
            return;
        }

        ShowLoading("캐릭터 삭제 중...");

        try
        {
            bool success = await CloudSaveManager.Instance.DeleteCharacterAsync();

            if (success)
            {
                _currentCharacterData = null;
                HideAllPanels();
                ShowClassSelection();
                Debug.Log("캐릭터 삭제 완료");
            }
            else
            {
                ShowError("캐릭터 삭제 실패");
            }
        }
        catch (Exception e)
        {
            ShowError($"오류: {e.Message}");
        }
        finally
        {
            HideLoading();
        }
    }

    /// <summary>
    /// 선택한 직업에 맞는 CharacterStats ScriptableObject 반환
    /// 
    /// 핵심 메서드:
    /// - Inspector에서 할당한 CharacterStats SO 반환
    /// - null 체크로 설정 누락 방지
    /// </summary>
    /// <param name="characterClass">선택한 캐릭터 직업</param>
    /// <returns>해당 직업의 기본 스탯 ScriptableObject</returns>
    private CharacterStats GetBaseStatsForClass(CharacterClass characterClass)
    {
        switch (characterClass)
        {
            case CharacterClass.Laon:
                if (_laonBaseStats == null)
                {
                    Debug.LogError("[CharacterSelection] Laon Base Stats가 할당되지 않았습니다!");
                }
                return _laonBaseStats;

            case CharacterClass.Sian:
                if (_sianBaseStats == null)
                {
                    Debug.LogError("[CharacterSelection] Sian Base Stats가 할당되지 않았습니다!");
                }
                return _sianBaseStats;

            case CharacterClass.Yujin:
                if (_yujinBaseStats == null)
                {
                    Debug.LogError("[CharacterSelection] Yujin Base Stats가 할당되지 않았습니다!");
                }
                return _yujinBaseStats;

            default:
                Debug.LogError($"[CharacterSelection] 알 수 없는 직업: {characterClass}");
                return null;
        }
    }

    /// <summary>
    /// 게임 씬 로드
    /// 
    /// 로직:
    /// 1. 마지막 저장된 씬 정보 확인 (position.scene)
    /// 2. 씬 정보가 없거나 빈 값이면 기본 씬 사용
    /// 3. 씬 정보가 있으면 해당 씬 로드
    /// </summary>
    private void LoadGameScene()
    {
        GameSessionManager.Instance.SetCharacterData(_currentCharacterData);

        // 마지막 저장된 씬 정보 확인
        string lastScene = _currentCharacterData?.position?.scene;

        // 씬 정보가 없거나 빈 값이면 기본 씬 사용
        if (string.IsNullOrEmpty(lastScene))
        {
            Debug.Log($"[CharacterSelection] 씬 정보 없음 - 기본 씬 로드: {_gameScene}");
            SceneManager.LoadScene(_gameScene);
        }
        else
        {
            Debug.Log($"[CharacterSelection] 마지막 씬 로드: {lastScene}");
            SceneManager.LoadScene(lastScene);
        }
    }

    private void HideAllPanels()
    {
        _existingCharacterPanel.SetActive(false);
        _classSelectionPanel.SetActive(false);
        HideLoading();
        HideError();
    }

    private void ShowLoading(string message)
    {
        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(true);
            if (_loadingText != null)
            {
                _loadingText.text = message;
            }
        }
    }

    private void HideLoading()
    {
        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(false);
        }
    }

    private void ShowError(string message)
    {
        if (_errorPanel != null)
        {
            _errorPanel.SetActive(true);
            if (_errorText != null)
            {
                _errorText.text = message;
            }
        }
        Debug.LogError($"[CharacterSelection] {message}");
    }

    private void HideError()
    {
        if (_errorPanel != null)
        {
            _errorPanel.SetActive(false);
        }
    }

    private async Task<bool> ShowConfirmDialog(string message)
    {
        // 간단한 확인 창 구현 (실제로는 별도 UI 패널 사용 권장)
        Debug.LogWarning($"확인 다이얼로그: {message}");
        await Task.Delay(100);
        return true;  // 테스트용: 항상 true 반환
    }

    private string FormatTimeSince(TimeSpan timeSince)
    {
        if (timeSince.TotalMinutes < 1)
        {
            return "방금 전";
        }
        else if (timeSince.TotalHours < 1)
        {
            return $"{(int)timeSince.TotalMinutes}분 전";
        }
        else if (timeSince.TotalDays < 1)
        {
            return $"{(int)timeSince.TotalHours}시간 전";
        }
        else if (timeSince.TotalDays < 7)
        {
            return $"{(int)timeSince.TotalDays}일 전";
        }
        else
        {
            return $"{(int)(timeSince.TotalDays / 7)}주 전";
        }
    }
}
