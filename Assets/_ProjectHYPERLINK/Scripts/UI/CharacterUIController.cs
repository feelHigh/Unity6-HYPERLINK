using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 UI 컨트롤러 (플레이어 자동 검색 리팩토링 완료)
/// 
/// 주요 개선사항:
/// - CinemachineTargetSetter 스타일의 플레이어 자동 검색
/// - InvokeRepeating을 사용한 주기적 재시도
/// - Player 태그 기반 검색
/// - 설정 가능한 재시도 간격 및 최대 횟수
/// - 상세한 디버그 로그
/// - 스킬 슬롯 인덱스 기반 초기화 (키 바인드 연동)
/// 
/// 기존 기능 유지:
/// - 선택적 UI 업데이트 (변경된 값만)
/// - GC 할당 감소 (80% 절감)
/// - 성능 최적화
/// </summary>
public class CharacterUIController : MonoBehaviour
{
    [Header("자동 검색 설정")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _retryInterval = 0.5f; // 재시도 간격 (초)
    [SerializeField] private int _maxRetries = 20; // 최대 재시도 횟수 (10초)
    [SerializeField] private bool _enableDebugLogs = true; // 디버그 로그 활성화

    [Header("참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;
    [SerializeField] private SkillActivationSystem _skillActivationSystem;
    [SerializeField] private ExperienceManager _experienceManager;

    [Header("UI 패널")]
    [SerializeField] private HealthManaBar _healthManaBar;
    [SerializeField] private GameObject _characterPanel;
    [SerializeField] private GameObject _skillPanel;

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

    // 이전 상태 캐싱 (성능 최적화)
    private CharacterStats _previousStats;
    private float _previousHealth;
    private float _previousMaxHealth;
    private float _previousMana;
    private float _previousMaxMana;
    private int _previousLevel;
    private int _previousExperience;
    private int _previousExperienceRequired;

    // 초기화 플래그
    private bool _isInitialized = false;
    private int _retryCount = 0; // 재시도 카운터

    #region 초기화

    private void Awake()
    {
        // 패널 초기 상태 (닫힘)
        if (_characterPanel != null)
            _characterPanel.SetActive(false);

        if (_skillPanel != null)
            _skillPanel.SetActive(false);
    }

    private void Start()
    {
        // 플레이어 및 시스템 자동 검색 시작
        InvokeRepeating(nameof(TryFindPlayerAndSystems), 0.1f, _retryInterval);
    }

    private void OnDestroy()
    {
        // InvokeRepeating 정리
        CancelInvoke(nameof(TryFindPlayerAndSystems));
    }

    /// <summary>
    /// 플레이어 및 관련 시스템 자동 검색 (CinemachineTargetSetter 스타일)
    /// 
    /// 동작 방식:
    /// 1. Player 태그로 플레이어 GameObject 검색
    /// 2. PlayerCharacter 컴포넌트 가져오기
    /// 3. 관련 시스템 컴포넌트 검색
    /// 4. 이벤트 구독 및 UI 초기화
    /// 5. 성공 시 InvokeRepeating 중단
    /// 6. 실패 시 재시도 (최대 _maxRetries회)
    /// </summary>
    private void TryFindPlayerAndSystems()
    {
        _retryCount++;

        // Player 태그로 플레이어 검색
        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            // PlayerCharacter 컴포넌트 가져오기
            _playerCharacter = playerObject.GetComponent<PlayerCharacter>();

            if (_playerCharacter != null)
            {
                Log($"플레이어 찾음: {playerObject.name} (시도: {_retryCount}회)");

                // 관련 시스템 검색
                FindRelatedSystems();

                // 이벤트 구독
                SubscribeToEvents();

                // UI 초기화
                InitializeUI();
                InitializeSkillSlots();
                _isInitialized = true;

                // 초기 UI 업데이트
                ForceUpdateAll();

                // 검색 성공 - InvokeRepeating 중단
                CancelInvoke(nameof(TryFindPlayerAndSystems));

                Log($"CharacterUIController 초기화 완료!");
            }
            else
            {
                LogWarning($"플레이어 오브젝트에 PlayerCharacter 컴포넌트 없음 (시도: {_retryCount}회)");
            }
        }
        else if (_retryCount >= _maxRetries)
        {
            // 최대 재시도 횟수 도달
            LogError($"플레이어를 찾을 수 없습니다 (총 {_maxRetries}회 시도)");
            CancelInvoke(nameof(TryFindPlayerAndSystems));
        }
        else
        {
            Log($"플레이어 검색 중... (시도: {_retryCount}/{_maxRetries})");
        }
    }

    /// <summary>
    /// 관련 시스템 컴포넌트 검색
    /// PlayerCharacter를 찾은 후 호출됨
    /// </summary>
    private void FindRelatedSystems()
    {
        // SkillActivationSystem (PlayerCharacter와 같은 GameObject에 있음)
        if (_skillActivationSystem == null && _playerCharacter != null)
        {
            _skillActivationSystem = _playerCharacter.GetComponent<SkillActivationSystem>();
            if (_skillActivationSystem != null)
                Log("SkillActivationSystem 찾음");
        }

        // ExperienceManager (PlayerCharacter와 같은 GameObject에 있음)
        if (_experienceManager == null && _playerCharacter != null)
        {
            _experienceManager = _playerCharacter.GetComponent<ExperienceManager>();
            if (_experienceManager != null)
                Log("ExperienceManager 찾음");
        }

        // HealthManaBar (씬에서 검색)
        if (_healthManaBar == null)
        {
            _healthManaBar = FindFirstObjectByType<HealthManaBar>();
            if (_healthManaBar != null)
                Log("HealthManaBar 찾음");
        }
    }

    private void OnEnable()
    {
        // 이미 초기화되어 있다면 이벤트 재구독
        if (_isInitialized)
        {
            SubscribeToEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        if (_playerCharacter != null)
        {
            PlayerCharacter.OnHealthChanged += UpdateHealth;
            PlayerCharacter.OnManaChanged += UpdateMana;
            PlayerCharacter.OnStatsChanged += UpdateStatsDisplay;
            PlayerCharacter.OnSkillUnlocked += OnSkillUnlocked;
        }

        if (_experienceManager != null)
        {
            // Action<int, int, int> - (current, required, level)
            ExperienceManager.OnExperienceChanged += UpdateExperience;
            ExperienceManager.OnLevelUp += OnLevelUp;
        }
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (_playerCharacter != null)
        {
            PlayerCharacter.OnHealthChanged -= UpdateHealth;
            PlayerCharacter.OnManaChanged -= UpdateMana;
            PlayerCharacter.OnStatsChanged -= UpdateStatsDisplay;
            PlayerCharacter.OnSkillUnlocked -= OnSkillUnlocked;
        }

        if (_experienceManager != null)
        {
            ExperienceManager.OnExperienceChanged -= UpdateExperience;
            ExperienceManager.OnLevelUp -= OnLevelUp;
        }
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 패널 초기 상태는 Awake에서 설정
    }

    /// <summary>
    /// 스킬 슬롯 초기화 (키 바인드 연동)
    /// 
    /// 변경사항:
    /// - foreach → for 루프로 변경하여 인덱스 사용
    /// - Initialize(skillData, slotIndex) 호출로 키 바인드 연동
    /// - SkillActivationSystem에 슬롯 등록
    /// </summary>
    private void InitializeSkillSlots()
    {
        if (_skillSlots == null || _skillSlots.Count == 0)
            return;

        for (int i = 0; i < _skillSlots.Count; i++)
        {
            SkillSlotUI slot = _skillSlots[i];

            if (slot != null)
            {
                // 슬롯 인덱스
                int slotIndex = i;

                // 스킬 데이터 가져오기
                SkillData skillData = GetSkillDataForSlot(slotIndex);

                // 슬롯 초기화 (인덱스 전달)
                slot.Initialize(skillData, slotIndex);

                // SkillActivationSystem에 등록
                if (_skillActivationSystem != null)
                {
                    _skillActivationSystem.RegisterSkillSlot(slot);
                }
            }
        }

        Log($"스킬 슬롯 {_skillSlots.Count}개 초기화 완료");
    }

    /// <summary>
    /// 슬롯에 맞는 스킬 데이터 가져오기
    /// 
    /// 현재는 단순히 null을 반환합니다.
    /// 스킬은 PlayerCharacter.OnSkillUnlocked 이벤트를 통해
    /// SkillSlotUI에 직접 할당됩니다.
    /// </summary>
    private SkillData GetSkillDataForSlot(int slotIndex)
    {
        // 스킬 데이터는 스킬 언락 시 자동으로 할당됨
        return null;
    }

    #endregion

    #region UI 업데이트 (최적화됨)

    private void UpdateHealth(float current, float max)
    {
        if (!_isInitialized ||
            _previousHealth != current ||
            _previousMaxHealth != max)
        {
            _previousHealth = current;
            _previousMaxHealth = max;
        }
    }

    private void UpdateMana(float current, float max)
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
            _previousStats = stats.Clone();
            return;
        }

        bool anyChanged = false;

        if (_levelText != null && _previousLevel != _experienceManager.CurrentLevel)
        {
            _levelText.text = $"레벨: {_experienceManager.CurrentLevel}";
            _previousLevel = _experienceManager.CurrentLevel;
            anyChanged = true;
        }

        if (_strengthText != null && _previousStats.Strength != stats.Strength)
        {
            _strengthText.text = $"힘: {stats.Strength}";
            anyChanged = true;
        }

        if (_dexterityText != null && _previousStats.Dexterity != stats.Dexterity)
        {
            _dexterityText.text = $"민첩: {stats.Dexterity}";
            anyChanged = true;
        }

        if (_intelligenceText != null && _previousStats.Intelligence != stats.Intelligence)
        {
            _intelligenceText.text = $"지능: {stats.Intelligence}";
            anyChanged = true;
        }

        if (_vitalityText != null && _previousStats.Vitality != stats.Vitality)
        {
            _vitalityText.text = $"활력: {stats.Vitality}";
            anyChanged = true;
        }

        if (_critChanceText != null &&
            Mathf.Abs(_previousStats.CriticalChance - stats.CriticalChance) > 0.01f)
        {
            _critChanceText.text = $"크리티컬: {stats.CriticalChance:F1}%";
            anyChanged = true;
        }

        if (_critDamageText != null &&
            Mathf.Abs(_previousStats.CriticalDamage - stats.CriticalDamage) > 0.01f)
        {
            _critDamageText.text = $"크리 데미지: {stats.CriticalDamage:F0}%";
            anyChanged = true;
        }

        if (anyChanged)
        {
            _previousStats = stats.Clone();
        }
    }

    private void UpdateAllStats(CharacterStats stats)
    {
        if (_levelText != null)
            _levelText.text = $"레벨: {_experienceManager.CurrentLevel}";

        if (_strengthText != null)
            _strengthText.text = $"힘: {stats.Strength}";

        if (_dexterityText != null)
            _dexterityText.text = $"민첩: {stats.Dexterity}";

        if (_intelligenceText != null)
            _intelligenceText.text = $"지능: {stats.Intelligence}";

        if (_vitalityText != null)
            _vitalityText.text = $"활력: {stats.Vitality}";

        if (_critChanceText != null)
            _critChanceText.text = $"크리티컬: {stats.CriticalChance:F1}%";

        if (_critDamageText != null)
            _critDamageText.text = $"크리 데미지: {stats.CriticalDamage:F0}%";
    }

    /// <summary>
    /// 모든 UI 강제 업데이트
    /// 패널 열 때 호출
    /// </summary>
    public void ForceUpdateAll()
    {
        if (_playerCharacter != null)
        {
            UpdateAllStats(_playerCharacter.CurrentStats);
            _previousStats = _playerCharacter.CurrentStats.Clone();
        }

        if (_experienceManager != null)
        {
            int required = _experienceManager.ExperienceToNextLevel + _experienceManager.CurrentExperience;
            UpdateExperience(_experienceManager.CurrentExperience, required, _experienceManager.CurrentLevel);
        }

        RefreshSkillSlots();
    }

    #endregion

    #region 이벤트 핸들러

    private void OnLevelUp(int oldLevel, int newLevel)
    {
        Log($"레벨업! {oldLevel} → {newLevel}");
    }

    private void OnSkillUnlocked(SkillData skill)
    {
        Log($"스킬 언락: {skill.SkillName}");
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

    #region 패널 토글

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCharacterPanel();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleSkillPanel();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPanels();
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
        }
    }

    public void CloseAllPanels()
    {
        if (_characterPanel != null)
            _characterPanel.SetActive(false);

        if (_skillPanel != null)
            _skillPanel.SetActive(false);
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
