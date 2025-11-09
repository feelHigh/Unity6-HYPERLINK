using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 UI 컨트롤러 (통합 키 바인딩)
/// 
/// CanvasGroup으로 패널 가시성 제어
/// 
/// 키 바인딩:
/// - K: 스킬 트리 윈도우 토글
/// - I: 인벤토리 윈도우 토글
/// - U: 퀘스트 윈도우 토글
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
    [Tooltip("SkillTree Panel의 CanvasGroup")]
    [SerializeField] private CanvasGroup _skillTreeCanvasGroup;
    [Tooltip("Quest UI Window의 CanvasGroup")]
    [SerializeField] private CanvasGroup _questUICanvasGroup;

    [Header("UI 패널 (GameObject)")]
    [SerializeField] private HealthManaBar _healthManaBar;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _skillTreePanel;
    [SerializeField] private GameObject _questUIPanel;

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

    [Header("소비 아이템 표시")]
    [Tooltip("레드 소다 개수 표시 텍스트")]
    [SerializeField] private TextMeshProUGUI _redSodaText;

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
    private int _previousRedSoda;

    private bool _isInitialized = false;
    private int _retryCount = 0;

    #region 초기화

    private void Awake()
    {
        // CanvasGroup 방식: GameObject 활성, 가시성만 제어
        SetPanelVisible(_inventoryCanvasGroup, false);
        SetPanelVisible(_skillTreeCanvasGroup, false);
        SetPanelVisible(_questUICanvasGroup, false);

        // Fallback: CanvasGroup 없으면 기존 방식
        if (_inventoryCanvasGroup == null && _inventoryPanel != null)
            _inventoryPanel.SetActive(false);

        if (_skillTreeCanvasGroup == null && _skillTreePanel != null)
            _skillTreePanel.SetActive(false);

        if (_questUICanvasGroup == null && _questUIPanel != null)
            _questUIPanel.SetActive(false);
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

    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        PlayerCharacter.OnHealthChanged += OnHealthChanged;
        PlayerCharacter.OnManaChanged += OnManaChanged;
        PlayerCharacter.OnStatsChanged += UpdateStatsDisplay;
        PlayerCharacter.OnSkillUnlocked += OnSkillUnlocked;
        PlayerCharacter.OnRedSodaChanged += OnRedSodaChanged;

        ExperienceManager.OnExperienceChanged += OnExperienceChanged;
        ExperienceManager.OnLevelUp += OnLevelUp;
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        PlayerCharacter.OnHealthChanged -= OnHealthChanged;
        PlayerCharacter.OnManaChanged -= OnManaChanged;
        PlayerCharacter.OnStatsChanged -= UpdateStatsDisplay;
        PlayerCharacter.OnSkillUnlocked -= OnSkillUnlocked;
        PlayerCharacter.OnRedSodaChanged -= OnRedSodaChanged;

        ExperienceManager.OnExperienceChanged -= OnExperienceChanged;
        ExperienceManager.OnLevelUp -= OnLevelUp;
    }

    private void InitializeUI()
    {
        ForceUpdateAll();
    }

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
                _skillSlots[i].Initialize(i, skillData: null);

                if (_skillActivationSystem != null)
                {
                    _skillActivationSystem.RegisterSkillSlot(_skillSlots[i]);
                }
            }
        }
    }

    #endregion

    #region CanvasGroup 헬퍼

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

    #region 이벤트 핸들러

    private void OnHealthChanged(float current, float max)
    {
        if (Mathf.Approximately(current, _previousHealth) && Mathf.Approximately(max, _previousMaxHealth))
            return;

        _previousHealth = current;
        _previousMaxHealth = max;
    }

    private void OnManaChanged(float current, float max)
    {
        if (Mathf.Approximately(current, _previousMana) && Mathf.Approximately(max, _previousMaxMana))
            return;

        _previousMana = current;
        _previousMaxMana = max;
    }

    private void UpdateStatsDisplay(CharacterStats stats)
    {
        if (AreStatsEqual(stats, _previousStats))
            return;

        _previousStats = stats;

        if (_strengthText != null)
            _strengthText.text = $"{stats.Strength}";

        if (_dexterityText != null)
            _dexterityText.text = $"{stats.Dexterity}";

        if (_intelligenceText != null)
            _intelligenceText.text = $"{stats.Intelligence}";

        if (_vitalityText != null)
            _vitalityText.text = $"{stats.Vitality}";

        if (_critChanceText != null)
            _critChanceText.text = $"{stats.CriticalChance:F1}%";

        if (_critDamageText != null)
            _critDamageText.text = $"{stats.CriticalDamage:F1}%";
    }

    private bool AreStatsEqual(CharacterStats a, CharacterStats b)
    {
        if (a == null || b == null) return false;

        return a.Strength == b.Strength &&
               a.Dexterity == b.Dexterity &&
               a.Intelligence == b.Intelligence &&
               a.Vitality == b.Vitality &&
               Mathf.Approximately(a.CriticalChance, b.CriticalChance) &&
               Mathf.Approximately(a.CriticalDamage, b.CriticalDamage);
    }

    /// <summary>
    /// 강제 업데이트
    /// </summary>
    private void ForceUpdateAll()
    {
        if (_playerCharacter == null || _experienceManager == null)
            return;

        UpdateStatsDisplay(_playerCharacter.CurrentStats);
        OnHealthChanged(_playerCharacter.CurrentHealth, _playerCharacter.MaxHealth);
        OnManaChanged(_playerCharacter.CurrentMana, _playerCharacter.MaxMana);
        OnRedSodaChanged(_playerCharacter.RedSoda); // ⭐ 추가

        int current = _experienceManager.CurrentExperience;
        int required = _experienceManager.ExperienceToNextLevel;
        int level = _experienceManager.CurrentLevel;
        OnExperienceChanged(current, required, level);
    }

    private void OnExperienceChanged(int current, int required, int level)
    {
        if (current == _previousExperience &&
            required == _previousExperienceRequired &&
            level == _previousLevel)
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

    private void OnSkillUnlocked(SkillData skill)
    {
        Log($"스킬 언락: {skill.SkillName}");
        ShowSkillUnlockedNotification(skill);
        RefreshSkillSlots();
    }

    private void ShowSkillUnlockedNotification(SkillData skill)
    {
        Log($"[알림] {skill.SkillName}을(를) 언락했습니다! 스킬 슬롯으로 드래그하여 배치하세요.");
    }

    /// <summary>
    /// 레드 소다 개수 변경 핸들러
    /// 
    /// 호출 시점:
    /// - 플레이어가 레드 소다 사용 (Number 1 키)
    /// - 레드 소다 획득
    /// - 초기화 시
    /// </summary>
    private void OnRedSodaChanged(int count)
    {
        // 중복 업데이트 방지
        if (count == _previousRedSoda)
            return;

        _previousRedSoda = count;

        // UI 업데이트
        if (_redSodaText != null)
        {
            _redSodaText.text = $"x{count}";
            Log($"레드 소다 UI 업데이트: {count}개");
        }
        else
        {
            LogWarning("레드 소다 텍스트가 할당되지 않았습니다!");
        }
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

        if (Input.GetKeyDown(KeyCode.U))
            ToggleQuestUIPanel();

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

    public void ToggleQuestUIPanel()
    {
        if (_questUICanvasGroup != null)
        {
            bool isVisible = IsPanelVisible(_questUICanvasGroup);
            SetPanelVisible(_questUICanvasGroup, !isVisible);
            Log($"퀘스트 UI {(!isVisible ? "열림" : "닫힘")}");
        }
        else if (_questUIPanel != null)
        {
            bool newState = !_questUIPanel.activeSelf;
            _questUIPanel.SetActive(newState);
            Log($"퀘스트 UI {(newState ? "열림" : "닫힘")}");
        }
        else
        {
            LogWarning("퀘스트 UI 패널이 할당되지 않았습니다!");
        }
    }

    private void HandleEscape()
    {
        bool anyPanelOpen = false;

        if (IsPanelVisible(_inventoryCanvasGroup)) anyPanelOpen = true;
        if (IsPanelVisible(_skillTreeCanvasGroup)) anyPanelOpen = true;
        if (IsPanelVisible(_questUICanvasGroup)) anyPanelOpen = true;

        if (!anyPanelOpen)
        {
            if (_inventoryPanel != null && _inventoryPanel.activeSelf) anyPanelOpen = true;
            if (_skillTreePanel != null && _skillTreePanel.activeSelf) anyPanelOpen = true;
            if (_questUIPanel != null && _questUIPanel.activeSelf) anyPanelOpen = true;
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
        SetPanelVisible(_inventoryCanvasGroup, false);
        SetPanelVisible(_skillTreeCanvasGroup, false);
        SetPanelVisible(_questUICanvasGroup, false);

        if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
        if (_skillTreePanel != null) _skillTreePanel.SetActive(false);
        if (_questUIPanel != null) _questUIPanel.SetActive(false);

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
