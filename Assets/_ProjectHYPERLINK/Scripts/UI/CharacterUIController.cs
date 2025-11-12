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
/// - O: 설정 윈도우 토글
/// - ESC: 모든 패널 닫기 / LoginScene 이동 옵션
/// 
/// [수정사항]
/// - OnExperienceChanged: TotalExpRequiredForNextLevel 사용으로 바 계산 수정
/// - InitializeSkillSlots: 빈 슬롯 초기화 (스킬 데이터는 SkillActivationSystem이 관리)
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

    [Header("UI 패널 (CanvasGroup 필수)")]
    [Tooltip("Inventory GameObject의 CanvasGroup")]
    [SerializeField] private CanvasGroup _inventoryCanvasGroup;
    [Tooltip("SkillTree Panel의 CanvasGroup")]
    [SerializeField] private CanvasGroup _skillTreeCanvasGroup;
    [Tooltip("Quest UI Window의 CanvasGroup")]
    [SerializeField] private CanvasGroup _questUICanvasGroup;
    [Tooltip("Settings Window의 CanvasGroup")]
    [SerializeField] private CanvasGroup _settingsCanvasGroup;

    [Header("추가 컴포넌트")]
    [SerializeField] private HealthManaBar _healthManaBar;
    [SerializeField] private SettingsWindow _settingsWindow;

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
        // 모든 패널 초기화 (비활성화)
        CanvasGroupHelper.SetVisible(_skillTreeCanvasGroup, false);
        CanvasGroupHelper.SetVisible(_inventoryCanvasGroup, false);
        CanvasGroupHelper.SetVisible(_questUICanvasGroup, false);
        CanvasGroupHelper.SetVisible(_settingsCanvasGroup, false);

        // CanvasGroup 검증
        ValidateCanvasGroups();
    }

    private void ValidateCanvasGroups()
    {
        CanvasGroupHelper.Validate(_inventoryCanvasGroup, "Inventory");
        CanvasGroupHelper.Validate(_skillTreeCanvasGroup, "SkillTree");
        CanvasGroupHelper.Validate(_questUICanvasGroup, "QuestUI");
        CanvasGroupHelper.Validate(_settingsCanvasGroup, "Settings");
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
            if (_skillActivationSystem != null)
                Log("SkillActivationSystem 찾음");
        }

        if (_experienceManager == null)
        {
            _experienceManager = _playerCharacter.GetComponent<ExperienceManager>();
            if (_experienceManager != null)
                Log("ExperienceManager 찾음");
        }
    }

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

    /// <summary>
    /// 스킬 슬롯 초기화
    /// 
    /// 변경사항:
    /// - 빈 슬롯으로 시작
    /// - SkillActivationSystem에 등록만 수행
    /// - 실제 스킬 배치는 플레이어가 드래그 앤 드롭으로 수행
    /// </summary>
    private void InitializeSkillSlots()
    {
        if (_playerCharacter == null)
        {
            LogError("PlayerCharacter가 없어 스킬 슬롯을 초기화할 수 없습니다.");
            return;
        }

        Log($"스킬 슬롯 초기화 시작 (빈 슬롯으로 시작 - SkillActivationSystem 등록)");

        for (int i = 0; i < _skillSlots.Count; i++)
        {
            if (_skillSlots[i] != null)
            {
                // 슬롯 인덱스만 설정 (스킬 데이터는 null)
                _skillSlots[i].Initialize(i, skillData: null);

                // SkillActivationSystem에 등록
                if (_skillActivationSystem != null)
                {
                    _skillActivationSystem.RegisterSkillSlot(_skillSlots[i]);
                    Log($"  슬롯 {i} → SkillActivationSystem 등록 완료");
                }
            }
        }
    }

    #endregion

    #region UI 업데이트

    private void ForceUpdateAll()
    {
        if (_playerCharacter == null || _experienceManager == null)
            return;

        UpdateStatsDisplay(_playerCharacter.CurrentStats);
        OnHealthChanged(_playerCharacter.CurrentHealth, _playerCharacter.MaxHealth);
        OnManaChanged(_playerCharacter.CurrentMana, _playerCharacter.MaxMana);
        OnRedSodaChanged(_playerCharacter.RedSoda);

        // [수정] TotalExpRequiredForNextLevel 사용
        int current = _experienceManager.CurrentExperience;
        int totalRequired = _experienceManager.TotalExpRequiredForNextLevel;
        int level = _experienceManager.CurrentLevel;
        OnExperienceChanged(current, totalRequired, level);
    }

    private void OnHealthChanged(float current, float max)
    {
        if (Mathf.Approximately(current, _previousHealth) && Mathf.Approximately(max, _previousMaxHealth))
            return;

        _previousHealth = current;
        _previousMaxHealth = max;

        // HealthManaBar는 자체적으로 PlayerCharacter.OnHealthChanged 이벤트 구독
    }

    private void OnManaChanged(float current, float max)
    {
        if (Mathf.Approximately(current, _previousMana) && Mathf.Approximately(max, _previousMaxMana))
            return;

        _previousMana = current;
        _previousMaxMana = max;

        // HealthManaBar는 자체적으로 PlayerCharacter.OnManaChanged 이벤트 구독
    }

    private void UpdateStatsDisplay(CharacterStats stats)
    {
        if (stats == null)
            return;

        if (_previousStats != null && AreStatsEqual(stats, _previousStats))
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
    /// 경험치 변경 이벤트 핸들러
    /// 
    /// [수정사항]
    /// - required 파라미터: 다음 레벨까지 필요한 총 경험치 (TotalExpRequiredForNextLevel)
    /// - fillAmount 계산: current / required (정확한 비율)
    /// 
    /// ExperienceManager.OnExperienceChanged: Action<int, int, int> (current, totalRequired, level)
    /// </summary>
    private void OnExperienceChanged(int current, int totalRequired, int level)
    {
        if (current == _previousExperience &&
            totalRequired == _previousExperienceRequired &&
            level == _previousLevel)
            return;

        _previousExperience = current;
        _previousExperienceRequired = totalRequired;
        _previousLevel = level;

        // [수정] 경험치 바 계산 수정: current / totalRequired
        if (_experienceBar != null)
        {
            float fillAmount = totalRequired > 0 ? (float)current / totalRequired : 0f;
            _experienceBar.fillAmount = fillAmount;

            Log($"경험치 바 업데이트: {current}/{totalRequired} = {fillAmount:F2} ({fillAmount * 100:F1}%)");
        }

        // 경험치 텍스트 표시
        if (_experienceText != null)
        {
            _experienceText.text = $"{current} / {totalRequired}";
        }

        // 레벨 표시
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
        if (count == _previousRedSoda)
            return;

        _previousRedSoda = count;

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
        if (Input.GetKeyDown(KeyCode.K))
            ToggleSkillTreePanel();

        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventoryPanel();

        if (Input.GetKeyDown(KeyCode.U))
            ToggleQuestPanel();

        if (Input.GetKeyDown(KeyCode.O))
            ToggleSettingsPanel();

        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscape();
    }

    public void ToggleSkillTreePanel()
    {
        bool isVisible = CanvasGroupHelper.IsVisible(_skillTreeCanvasGroup);
        CanvasGroupHelper.SetVisible(_skillTreeCanvasGroup, !isVisible);

        if (!isVisible)
            RefreshSkillSlots();

        Log($"스킬 트리 패널 {(!isVisible ? "열림" : "닫힘")}");
    }

    public void ToggleInventoryPanel()
    {
        bool isVisible = CanvasGroupHelper.IsVisible(_inventoryCanvasGroup);
        CanvasGroupHelper.SetVisible(_inventoryCanvasGroup, !isVisible);
        Log($"인벤토리 패널 {(!isVisible ? "열림" : "닫힘")}");
    }

    public void ToggleQuestPanel()
    {
        bool isVisible = CanvasGroupHelper.IsVisible(_questUICanvasGroup);
        CanvasGroupHelper.SetVisible(_questUICanvasGroup, !isVisible);
        Log($"퀘스트 패널 {(!isVisible ? "열림" : "닫힘")}");
    }

    public void ToggleSettingsPanel()
    {
        bool isVisible = CanvasGroupHelper.IsVisible(_settingsCanvasGroup);
        CanvasGroupHelper.SetVisible(_settingsCanvasGroup, !isVisible);

        Log($"설정 패널 {(!isVisible ? "열림" : "닫힘")}");
    }

    private void HandleEscape()
    {
        // 열려 있는 패널이 있으면 모두 닫기
        bool anyClosed = false;

        if (CanvasGroupHelper.IsVisible(_skillTreeCanvasGroup))
        {
            CanvasGroupHelper.SetVisible(_skillTreeCanvasGroup, false);
            anyClosed = true;
        }

        if (CanvasGroupHelper.IsVisible(_inventoryCanvasGroup))
        {
            CanvasGroupHelper.SetVisible(_inventoryCanvasGroup, false);
            anyClosed = true;
        }

        if (CanvasGroupHelper.IsVisible(_questUICanvasGroup))
        {
            CanvasGroupHelper.SetVisible(_questUICanvasGroup, false);
            anyClosed = true;
        }

        if (CanvasGroupHelper.IsVisible(_settingsCanvasGroup))
        {
            CanvasGroupHelper.SetVisible(_settingsCanvasGroup, false);
            anyClosed = true;
        }

        if (anyClosed)
        {
            Log("ESC - 모든 패널 닫힘");
            return;
        }

        // 모든 패널이 닫혀 있으면 LoginScene 이동
        if (_enableEscapeToLogin)
        {
            Log($"ESC - {_loginSceneName}으로 이동");
            UnityEngine.SceneManagement.SceneManager.LoadScene(_loginSceneName);
        }
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
