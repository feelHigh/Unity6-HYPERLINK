using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 UI 컨트롤러 (통합 키 바인딩)
/// 
/// 기능:
/// - 플레이어 자동 검색
/// - 모든 UI 키 바인딩 통합 관리
/// - 캐릭터 스탯 실시간 업데이트
/// - 경험치 바 표시
/// - 스킬 슬롯 관리
/// 
/// 키 바인딩:
/// - C: 캐릭터 패널 토글
/// - K: 스킬 패널 토글
/// - I: 인벤토리 패널 토글
/// - Tab: 미니맵 토글 (TODO)
/// - M: 맵 & 퀘스트 패널 토글 (TODO)
/// - ESC: 모든 패널 닫기 / LoginScene 이동 옵션
/// </summary>
public class CharacterUIController : MonoBehaviour
{
    [Header("자동 검색 설정")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _retryInterval = 0.5f;
    [SerializeField] private int _maxRetries = 20;
    [SerializeField] private bool _enableDebugLogs = true;

    [Header("참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;
    [SerializeField] private SkillActivationSystem _skillActivationSystem;
    [SerializeField] private ExperienceManager _experienceManager;

    [Header("UI 패널")]
    [SerializeField] private HealthManaBar _healthManaBar;
    [SerializeField] private GameObject _characterPanel;
    [SerializeField] private GameObject _skillPanel;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _minimapPanel;
    [SerializeField] private GameObject _mapQuestPanel;

    [Header("ESC 동작 설정")]
    [Tooltip("ESC 키로 LoginScene 이동 활성화")]
    [SerializeField] private bool _enableEscapeToLogin = true;
    [SerializeField] private string _loginSceneName = "LoginScene";

    [Header("캐릭터 스탯 표시")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _strengthText;
    [SerializeField] private TextMeshProUGUI _dexterityText;
    [SerializeField] private TextMeshProUGUI _intelligenceText;
    [SerializeField] private TextMeshProUGUI _vitalityText;
    [SerializeField] private TextMeshProUGUI _critChanceText;
    [SerializeField] private TextMeshProUGUI _critDamageText;

    [Header("경험치 바")]
    [SerializeField] private UnityEngine.UI.Image _experienceBar;
    [SerializeField] private TextMeshProUGUI _experienceText;

    [Header("스킬 슬롯")]
    [SerializeField] private List<SkillSlotUI> _skillSlots = new List<SkillSlotUI>();

    private CharacterStats _previousStats;
    private float _previousHealth;
    private float _previousMaxHealth;
    private float _previousMana;
    private float _previousMaxMana;
    private int _previousLevel;
    private int _previousExperience;
    private int _previousExperienceRequired;

    private bool _isInitialized = false;
    private int _retryCount = 0;

    #region 초기화

    private void Awake()
    {
        // 모든 패널 초기 상태 (닫힘)
        if (_characterPanel != null)
            _characterPanel.SetActive(false);

        if (_skillPanel != null)
            _skillPanel.SetActive(true);

        if (_inventoryPanel != null)
            _inventoryPanel.SetActive(false);

        if (_minimapPanel != null)
            _minimapPanel.SetActive(false);

        if (_mapQuestPanel != null)
            _mapQuestPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // 이미 초기화된 경우 이벤트 재구독
        if (_isInitialized)
        {
            SubscribeToEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void Start()
    {
        InvokeRepeating(nameof(TryFindPlayerAndSystems), 0.1f, _retryInterval);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(TryFindPlayerAndSystems));
        UnsubscribeFromEvents();

        ClearSkillSlots();
    }

    private void ClearSkillSlots()
    {
        if (_skillActivationSystem != null)
        {
            foreach (var slot in _skillSlots)
            {
                if (slot != null)
                {
                    _skillActivationSystem.UnregisterSkillSlot(slot);
                }
            }
        }
    }

    private void TryFindPlayerAndSystems()
    {
        _retryCount++;

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _playerCharacter = playerObject.GetComponent<PlayerCharacter>();

            if (_playerCharacter != null)
            {
                Log($"플레이어 찾음: {playerObject.name} (시도: {_retryCount}회)");

                FindRelatedSystems();
                SubscribeToEvents();
                InitializeUI();
                InitializeSkillSlots();
                _isInitialized = true;

                ForceUpdateAll();

                CancelInvoke(nameof(TryFindPlayerAndSystems));

                Log($"CharacterUIController 초기화 완료!");
                return;
            }
        }

        if (_retryCount >= _maxRetries)
        {
            LogError($"플레이어를 {_maxRetries}회 시도 후에도 찾지 못했습니다!");
            CancelInvoke(nameof(TryFindPlayerAndSystems));
        }
    }

    private void FindRelatedSystems()
    {
        if (_skillActivationSystem == null)
        {
            _skillActivationSystem = _playerCharacter.GetComponent<SkillActivationSystem>();
        }

        if (_experienceManager == null)
        {
            _experienceManager = _playerCharacter.GetComponent<ExperienceManager>();
        }
    }

    private void SubscribeToEvents()
    {
        // Static 이벤트 구독
        PlayerCharacter.OnHealthChanged += UpdateHealthBar;
        PlayerCharacter.OnManaChanged += UpdateManaBar;
        PlayerCharacter.OnStatsChanged += UpdateStatsDisplay;
        PlayerCharacter.OnSkillUnlocked += OnSkillUnlocked;

        ExperienceManager.OnExperienceChanged += UpdateExperience;
        ExperienceManager.OnLevelUp += OnLevelUp;
    }

    private void UnsubscribeFromEvents()
    {
        // Static 이벤트 구독 해제
        PlayerCharacter.OnHealthChanged -= UpdateHealthBar;
        PlayerCharacter.OnManaChanged -= UpdateManaBar;
        PlayerCharacter.OnStatsChanged -= UpdateStatsDisplay;
        PlayerCharacter.OnSkillUnlocked -= OnSkillUnlocked;

        ExperienceManager.OnExperienceChanged -= UpdateExperience;
        ExperienceManager.OnLevelUp -= OnLevelUp;
    }

    private void InitializeUI()
    {
        // HealthManaBar는 OnEnable에서 자동 구독하므로 Initialize 불필요
        ForceUpdateAll();
    }

    private void InitializeSkillSlots()
    {
        if (_playerCharacter == null) return;

        // UnlockedSkills에서 스킬 데이터 가져와서 슬롯 초기화
        List<SkillData> unlockedSkills = _playerCharacter.UnlockedSkills;

        Log($"스킬 슬롯 초기화 시작: {unlockedSkills.Count}개 언락됨");

        for (int i = 0; i < _skillSlots.Count && i < unlockedSkills.Count; i++)
        {
            if (_skillSlots[i] != null && unlockedSkills[i] != null)
            {
                _skillSlots[i].Initialize(unlockedSkills[i], i);

                // 이미 언락된 스킬은 즉시 Unlock 호출 (잠금 해제)
                _skillSlots[i].Unlock();

                // SkillActivationSystem에 등록
                if (_skillActivationSystem != null)
                {
                    _skillActivationSystem.RegisterSkillSlot(_skillSlots[i]);
                    Log($"  슬롯 {i}: {unlockedSkills[i].SkillName} → SkillActivationSystem 등록 완료");
                }
            }
        }
    }

    #endregion

    #region 강제 업데이트

    private void ForceUpdateAll()
    {
        if (!_isInitialized || _playerCharacter == null)
            return;

        CharacterStats stats = _playerCharacter.CurrentStats;
        UpdateStatsDisplay(stats);

        UpdateHealthBar(_playerCharacter.CurrentHealth, _playerCharacter.MaxHealth);
        UpdateManaBar(_playerCharacter.CurrentMana, _playerCharacter.MaxMana);

        if (_experienceManager != null)
        {
            UpdateExperience(
                _experienceManager.CurrentExperience,
                _experienceManager.ExperienceToNextLevel,
                _experienceManager.CurrentLevel
            );
        }
    }

    #endregion

    #region UI 업데이트

    private void UpdateHealthBar(float current, float max)
    {
        if (!_isInitialized ||
            _previousHealth != current ||
            _previousMaxHealth != max)
        {
            _previousHealth = current;
            _previousMaxHealth = max;
        }
    }

    private void UpdateManaBar(float current, float max)
    {
        if (!_isInitialized ||
            _previousMana != current ||
            _previousMaxMana != max)
        {
            _previousMana = current;
            _previousMaxMana = max;
        }
    }

    private void UpdateExperience(int current, int required, int level)
    {
        if (!_isInitialized ||
            _previousExperience != current ||
            _previousExperienceRequired != required ||
            _previousLevel != level)
        {
            _previousExperience = current;
            _previousExperienceRequired = required;
            _previousLevel = level;

            if (_experienceBar != null)
            {
                _experienceBar.fillAmount = (float)current / required;
            }

            if (_experienceText != null)
            {
                _experienceText.text = $"{current} / {required}";
            }
        }
    }

    private void UpdateStatsDisplay(CharacterStats stats)
    {
        if (stats == null)
            return;

        if (_previousStats == null)
        {
            UpdateAllStats(stats);
            _previousStats = stats;
            return;
        }

        if (_levelText != null && _experienceManager != null)
            _levelText.text = $"레벨 {_experienceManager.CurrentLevel}";

        if (_strengthText != null && _previousStats.Strength != stats.Strength)
            _strengthText.text = stats.Strength.ToString();

        if (_dexterityText != null && _previousStats.Dexterity != stats.Dexterity)
            _dexterityText.text = stats.Dexterity.ToString();

        if (_intelligenceText != null && _previousStats.Intelligence != stats.Intelligence)
            _intelligenceText.text = stats.Intelligence.ToString();

        if (_vitalityText != null && _previousStats.Vitality != stats.Vitality)
            _vitalityText.text = stats.Vitality.ToString();

        if (_critChanceText != null && !Mathf.Approximately(_previousStats.CriticalChance, stats.CriticalChance))
            _critChanceText.text = $"{stats.CriticalChance:F1}%";

        if (_critDamageText != null && !Mathf.Approximately(_previousStats.CriticalDamage, stats.CriticalDamage))
            _critDamageText.text = $"{stats.CriticalDamage:F1}%";

        _previousStats = stats;
    }

    private void UpdateAllStats(CharacterStats stats)
    {
        if (_levelText != null && _experienceManager != null)
            _levelText.text = $"레벨 {_experienceManager.CurrentLevel}";

        if (_strengthText != null)
            _strengthText.text = stats.Strength.ToString();

        if (_dexterityText != null)
            _dexterityText.text = stats.Dexterity.ToString();

        if (_intelligenceText != null)
            _intelligenceText.text = stats.Intelligence.ToString();

        if (_vitalityText != null)
            _vitalityText.text = stats.Vitality.ToString();

        if (_critChanceText != null)
            _critChanceText.text = $"{stats.CriticalChance:F1}%";

        if (_critDamageText != null)
            _critDamageText.text = $"{stats.CriticalDamage:F1}%";
    }

    #endregion

    #region 이벤트 핸들러

    private void OnLevelUp(int oldLevel, int newLevel)
    {
        Log($"레벨업: {oldLevel} → {newLevel}");
    }

    private void OnSkillUnlocked(SkillData skill)
    {
        Log($"스킬 언락: {skill.SkillName}");

        // 해당 스킬을 가진 슬롯 찾아서 Unlock 호출
        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot != null && slot.SkillData == skill)
            {
                slot.Unlock();
                Log($"  슬롯 언락 완료: {skill.SkillName}");
                break;
            }
        }

        RefreshSkillSlots();
    }

    private void RefreshSkillSlots()
    {
        foreach (SkillSlotUI slot in _skillSlots)
        {
            if (slot != null)
            {
                slot.RefreshDisplay();
            }
        }
    }

    #endregion

    #region 키보드 입력 및 패널 토글

    private void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// 모든 UI 키 입력 처리
    /// </summary>
    private void HandleInput()
    {
        // C: 캐릭터 패널
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCharacterPanel();
        }

        // K: 스킬 패널
        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleSkillPanel();
        }

        // I: 인벤토리 패널
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventoryPanel();
        }

        // Tab: 미니맵 (TODO)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMinimapPanel();
        }

        // M: 맵 & 퀘스트 (TODO)
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMapQuestPanel();
        }

        // Esc: 모든 패널 닫기 또는 LoginScene 이동
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
        }
    }

    public void ToggleCharacterPanel()
    {
        if (_characterPanel != null)
        {
            bool newState = !_characterPanel.activeSelf;
            _characterPanel.SetActive(newState);

            if (newState)
            {
                ForceUpdateAll();
            }

            Log($"캐릭터 패널 {(newState ? "열림" : "닫힘")}");
        }
    }

    public void ToggleSkillPanel()
    {
        if (_skillPanel != null)
        {
            bool newState = !_skillPanel.activeSelf;
            _skillPanel.SetActive(newState);

            if (newState)
            {
                RefreshSkillSlots();
            }

            Log($"스킬 패널 {(newState ? "열림" : "닫힘")}");
        }
    }

    public void ToggleInventoryPanel()
    {
        if (_inventoryPanel != null)
        {
            bool newState = !_inventoryPanel.activeSelf;
            _inventoryPanel.SetActive(newState);

            Log($"인벤토리 패널 {(newState ? "열림" : "닫힘")}");
        }
        else
        {
            LogWarning("인벤토리 패널이 할당되지 않았습니다!");
        }
    }

    public void ToggleMinimapPanel()
    {
        if (_minimapPanel != null)
        {
            bool newState = !_minimapPanel.activeSelf;
            _minimapPanel.SetActive(newState);

            Log($"미니맵 패널 {(newState ? "열림" : "닫힘")}");
        }
        else
        {
            Log("TODO: 미니맵 시스템 구현 예정");
        }
    }

    public void ToggleMapQuestPanel()
    {
        if (_mapQuestPanel != null)
        {
            bool newState = !_mapQuestPanel.activeSelf;
            _mapQuestPanel.SetActive(newState);

            Log($"맵 & 퀘스트 패널 {(newState ? "열림" : "닫힘")}");
        }
        else
        {
            Log("TODO: 맵 & 퀘스트 시스템 구현 예정");
        }
    }

    /// <summary>
    /// ESC 키 처리
    /// 1순위: 열린 패널이 있으면 모두 닫기
    /// 2순위: 패널이 없으면 LoginScene 이동 (옵션)
    /// </summary>
    private void HandleEscape()
    {
        bool anyPanelOpen = false;

        if (_characterPanel != null && _characterPanel.activeSelf)
            anyPanelOpen = true;

        if (_skillPanel != null && _skillPanel.activeSelf)
            anyPanelOpen = true;

        if (_inventoryPanel != null && _inventoryPanel.activeSelf)
            anyPanelOpen = true;

        if (_minimapPanel != null && _minimapPanel.activeSelf)
            anyPanelOpen = true;

        if (_mapQuestPanel != null && _mapQuestPanel.activeSelf)
            anyPanelOpen = true;

        if (anyPanelOpen)
        {
            CloseAllPanels();
        }
        else if (_enableEscapeToLogin)
        {
            LoadLoginScene();
        }
    }

    public void CloseAllPanels()
    {
        if (_characterPanel != null)
            _characterPanel.SetActive(false);

        if (_skillPanel != null)
            _skillPanel.SetActive(false);

        if (_inventoryPanel != null)
            _inventoryPanel.SetActive(false);

        if (_minimapPanel != null)
            _minimapPanel.SetActive(false);

        if (_mapQuestPanel != null)
            _mapQuestPanel.SetActive(false);

        Log("모든 패널 닫기");
    }

    private void LoadLoginScene()
    {
        Log($"LoginScene 이동: {_loginSceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(_loginSceneName);
    }

    #endregion

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[CharacterUIController] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[CharacterUIController] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[CharacterUIController] {message}");
    }

    #endregion
}
