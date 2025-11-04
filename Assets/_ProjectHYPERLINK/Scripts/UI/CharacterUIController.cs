using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 UI 컨트롤러 (통합 키 바인딩)
/// 
/// CanvasGroup으로 패널 가시성 제어
/// 
/// 키 바인딩:
/// - C: 캐릭터 패널 토글
/// - K: 스킬 패널 토글
/// - I: 인벤토리 패널 토글
/// - T: 스킬 트리 패널 토글
/// - Tab: 미니맵 토글
/// - M: 맵 & 퀘스트 패널 토글
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

    [Header("UI 패널 (CanvasGroup)")]
    [Tooltip("Inventory GameObject의 CanvasGroup")]
    [SerializeField] private CanvasGroup _inventoryCanvasGroup;
    [Tooltip("Character Panel의 CanvasGroup")]
    [SerializeField] private CanvasGroup _characterCanvasGroup;
    [Tooltip("Skill Panel의 CanvasGroup")]
    [SerializeField] private CanvasGroup _skillCanvasGroup;
    [Tooltip("SkillTree Panel의 CanvasGroup")]
    [SerializeField] private CanvasGroup _skillTreeCanvasGroup;
    [Tooltip("Minimap Panel의 CanvasGroup")]
    [SerializeField] private CanvasGroup _minimapCanvasGroup;
    [Tooltip("Map Quest Panel의 CanvasGroup")]
    [SerializeField] private CanvasGroup _mapQuestCanvasGroup;

    [Header("UI 패널 (GameObject)")]
    [SerializeField] private HealthManaBar _healthManaBar;
    [SerializeField] private GameObject _characterPanel;
    [SerializeField] private GameObject _skillPanel;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _skillTreePanel;
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
        // CanvasGroup 방식: GameObject 활성, 가시성만 제어
        SetPanelVisible(_characterCanvasGroup, false);
        SetPanelVisible(_skillCanvasGroup, true);
        SetPanelVisible(_inventoryCanvasGroup, false);
        SetPanelVisible(_skillTreeCanvasGroup, false);
        SetPanelVisible(_minimapCanvasGroup, false);
        SetPanelVisible(_mapQuestCanvasGroup, false);

        // Fallback: CanvasGroup 없으면 기존 방식
        if (_characterCanvasGroup == null && _characterPanel != null)
            _characterPanel.SetActive(false);

        if (_skillCanvasGroup == null && _skillPanel != null)
            _skillPanel.SetActive(true);

        if (_inventoryCanvasGroup == null && _inventoryPanel != null)
            _inventoryPanel.SetActive(false);

        if (_skillTreeCanvasGroup == null && _skillTreePanel != null)
            _skillTreePanel.SetActive(false);

        if (_minimapCanvasGroup == null && _minimapPanel != null)
            _minimapPanel.SetActive(false);

        if (_mapQuestCanvasGroup == null && _mapQuestPanel != null)
            _mapQuestPanel.SetActive(false);
    }

    private void OnEnable()
    {
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
        PlayerCharacter.OnHealthChanged += OnHealthChanged;
        PlayerCharacter.OnManaChanged += OnManaChanged;
        PlayerCharacter.OnStatsChanged += UpdateStatsDisplay;
        PlayerCharacter.OnSkillUnlocked += OnSkillUnlocked;

        ExperienceManager.OnExperienceChanged += OnExperienceChanged;
        ExperienceManager.OnLevelUp += OnLevelUp;
    }

    private void UnsubscribeFromEvents()
    {
        PlayerCharacter.OnHealthChanged -= OnHealthChanged;
        PlayerCharacter.OnManaChanged -= OnManaChanged;
        PlayerCharacter.OnStatsChanged -= UpdateStatsDisplay;
        PlayerCharacter.OnSkillUnlocked -= OnSkillUnlocked;

        ExperienceManager.OnExperienceChanged -= OnExperienceChanged;
        ExperienceManager.OnLevelUp -= OnLevelUp;
    }

    private void InitializeUI()
    {
        ForceUpdateAll();
    }

    /// <summary>
    /// 스킬 슬롯 초기화
    /// - 모든 슬롯을 빈 상태로 초기화
    /// - 플레이어가 스킬 트리에서 스킬을 언락하고 드래그 앤 드롭으로 배치
    /// - 더 많은 플레이어 제어와 전략적 선택 제공
    /// </summary>
    private void InitializeSkillSlots()
    {
        if (_playerCharacter == null)
        {
            LogError("PlayerCharacter가 없어 스킬 슬롯을 초기화할 수 없습니다.");
            return;
        }

        Log($"스킬 슬롯 초기화 시작 (빈 슬롯으로 시작)");

        for (int i = 0; i < _skillSlots.Count; i++)
        {
            if (_skillSlots[i] != null)
            {
                // 빈 슬롯으로 초기화 (skillData = null)
                _skillSlots[i].Initialize(i, skillData: null);

                // SkillActivationSystem에 슬롯 등록
                if (_skillActivationSystem != null)
                {
                    _skillActivationSystem.RegisterSkillSlot(_skillSlots[i]);
                    Log($"  슬롯 {i}: 빈 슬롯 → SkillActivationSystem 등록 완료");
                }
            }
            else
            {
                LogError($"  슬롯 {i}가 null입니다!");
            }
        }

        Log($"스킬 슬롯 초기화 완료: {_skillSlots.Count}개 슬롯 (모두 비어있음)");
    }

    #endregion

    #region CanvasGroup 유틸리티

    private void SetPanelVisible(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private bool IsPanelVisible(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return false;
        return canvasGroup.alpha > 0f;
    }

    #endregion

    #region UI 업데이트

    private void ForceUpdateAll()
    {
        if (_playerCharacter == null)
        {
            LogWarning("ForceUpdateAll: PlayerCharacter가 null입니다");
            return;
        }

        CharacterStats currentStats = _playerCharacter.GetTotalStats();
        UpdateStatsDisplay(currentStats);

        float currentHealth = _playerCharacter.CurrentHealth;
        float maxHealth = _playerCharacter.MaxHealth;
        OnHealthChanged(currentHealth, maxHealth);

        float currentMana = _playerCharacter.CurrentMana;
        float maxMana = _playerCharacter.MaxMana;
        OnManaChanged(currentMana, maxMana);

        if (_experienceManager != null)
        {
            int currentExp = _experienceManager.CurrentExperience;
            int requiredExp = _experienceManager.ExperienceToNextLevel;
            int level = _experienceManager.CurrentLevel;
            OnExperienceChanged(currentExp, requiredExp, level);
        }

        Log("UI 강제 업데이트 완료");
    }

    private void OnHealthChanged(float current, float max)
    {
        if (_previousHealth == current && _previousMaxHealth == max)
            return;

        _previousHealth = current;
        _previousMaxHealth = max;
    }

    private void OnManaChanged(float current, float max)
    {
        if (_previousMana == current && _previousMaxMana == max)
            return;

        _previousMana = current;
        _previousMaxMana = max;
    }

    private void UpdateStatsDisplay(CharacterStats stats)
    {
        if (_previousStats != null && stats.Equals(_previousStats))
            return;

        _previousStats = stats;

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

    private void OnExperienceChanged(int current, int required, int level)
    {
        if (_previousExperience == current && _previousExperienceRequired == required && _previousLevel == level)
            return;

        _previousExperience = current;
        _previousExperienceRequired = required;
        _previousLevel = level;

        if (_experienceBar != null)
        {
            float fillAmount = required > 0 ? (float)current / required : 0f;
            _experienceBar.fillAmount = fillAmount;
        }

        if (_experienceText != null)
        {
            _experienceText.text = $"{current} / {required}";
        }

        if (_levelText != null)
        {
            _levelText.text = $"Lv. {level}";
        }
    }

    private void OnLevelUp(int oldLevel, int newLevel)
    {
        Log($"레벨업: {oldLevel} → {newLevel}");
    }

    /// <summary>
    /// 스킬 언락 이벤트 핸들러
    /// - 스킬 언락 알림만 표시
    /// - 플레이어가 직접 드래그 앤 드롭으로 슬롯에 배치
    /// </summary>
    private void OnSkillUnlocked(SkillData skill)
    {
        Log($"스킬 언락: {skill.SkillName}");
        
        ShowSkillUnlockedNotification(skill);

        RefreshSkillSlots();
    }

    /// <summary>
    /// 스킬 언락 알림 표시
    /// </summary>
    private void ShowSkillUnlockedNotification(SkillData skill)
    {
        // TODO: UI 알림 팝업 구현
        // 예: "새 스킬 언락! [스킬 이름]을(를) 스킬 슬롯에 드래그하세요."
        Log($"[알림] {skill.SkillName}을(를) 언락했습니다! 스킬 슬롯으로 드래그하여 배치하세요.");
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

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventoryPanel();

        if (Input.GetKeyDown(KeyCode.K))
            ToggleSkillTreePanel();

        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleMinimapPanel();

        if (Input.GetKeyDown(KeyCode.M))
            ToggleMapQuestPanel();

        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscape();
    }

    public void ToggleInventoryPanel()
    {
        if (_inventoryCanvasGroup != null)
        {
            bool isVisible = IsPanelVisible(_inventoryCanvasGroup);

            if (isVisible && ItemInventory.Instance != null)
                ItemInventory.Instance.Close();

            SetPanelVisible(_inventoryCanvasGroup, !isVisible);

            Log($"인벤토리 패널 {(!isVisible ? "열림" : "닫힘")}");
        }
        else if (_inventoryPanel != null)
        {
            bool newState = !_inventoryPanel.activeSelf;

            if (_inventoryPanel.activeSelf && ItemInventory.Instance != null)
                ItemInventory.Instance.Close();

            _inventoryPanel.SetActive(newState);

            Log($"인벤토리 패널 {(newState ? "열림" : "닫힘")}");
        }
        else
        {
            LogWarning("인벤토리 패널이 할당되지 않았습니다!");
        }
    }

    public void ToggleSkillTreePanel()
    {
        if (_skillTreeCanvasGroup != null)
        {
            bool isVisible = IsPanelVisible(_skillTreeCanvasGroup);
            SetPanelVisible(_skillTreeCanvasGroup, !isVisible);
            Log($"스킬 트리 패널 {(!isVisible ? "열림" : "닫힘")}");
        }
        else if (_skillTreePanel != null)
        {
            bool newState = !_skillTreePanel.activeSelf;
            _skillTreePanel.SetActive(newState);
            Log($"스킬 트리 패널 {(newState ? "열림" : "닫힘")}");
        }
    }

    public void ToggleMinimapPanel()
    {
        if (_minimapCanvasGroup != null)
        {
            bool isVisible = IsPanelVisible(_minimapCanvasGroup);
            SetPanelVisible(_minimapCanvasGroup, !isVisible);
            Log($"미니맵 패널 {(!isVisible ? "열림" : "닫힘")}");
        }
        else if (_minimapPanel != null)
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
        if (_mapQuestCanvasGroup != null)
        {
            bool isVisible = IsPanelVisible(_mapQuestCanvasGroup);
            SetPanelVisible(_mapQuestCanvasGroup, !isVisible);
            Log($"맵 & 퀘스트 패널 {(!isVisible ? "열림" : "닫힘")}");
        }
        else if (_mapQuestPanel != null)
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

    private void HandleEscape()
    {
        bool anyPanelOpen = false;

        if (IsPanelVisible(_characterCanvasGroup)) anyPanelOpen = true;
        if (IsPanelVisible(_skillCanvasGroup)) anyPanelOpen = true;
        if (IsPanelVisible(_inventoryCanvasGroup)) anyPanelOpen = true;
        if (IsPanelVisible(_skillTreeCanvasGroup)) anyPanelOpen = true;
        if (IsPanelVisible(_minimapCanvasGroup)) anyPanelOpen = true;
        if (IsPanelVisible(_mapQuestCanvasGroup)) anyPanelOpen = true;

        if (!anyPanelOpen)
        {
            if (_characterPanel != null && _characterPanel.activeSelf) anyPanelOpen = true;
            if (_skillPanel != null && _skillPanel.activeSelf) anyPanelOpen = true;
            if (_inventoryPanel != null && _inventoryPanel.activeSelf) anyPanelOpen = true;
            if (_skillTreePanel != null && _skillTreePanel.activeSelf) anyPanelOpen = true;
            if (_minimapPanel != null && _minimapPanel.activeSelf) anyPanelOpen = true;
            if (_mapQuestPanel != null && _mapQuestPanel.activeSelf) anyPanelOpen = true;
        }

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
        SetPanelVisible(_characterCanvasGroup, false);
        SetPanelVisible(_skillCanvasGroup, false);
        SetPanelVisible(_inventoryCanvasGroup, false);
        SetPanelVisible(_skillTreeCanvasGroup, false);
        SetPanelVisible(_minimapCanvasGroup, false);
        SetPanelVisible(_mapQuestCanvasGroup, false);

        if (_characterPanel != null) _characterPanel.SetActive(false);
        if (_skillPanel != null) _skillPanel.SetActive(false);
        if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
        if (_skillTreePanel != null) _skillTreePanel.SetActive(false);
        if (_minimapPanel != null) _minimapPanel.SetActive(false);
        if (_mapQuestPanel != null) _mapQuestPanel.SetActive(false);

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
