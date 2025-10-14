using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 관련 모든 UI 요소의 메인 컨트롤러
/// 
/// 관리하는 UI 요소:
/// - 체력/마나 바 (HealthManaBar)
/// - 캐릭터 스탯 패널
/// - 스킬 슬롯 UI
/// - 경험치 바
/// - 레벨 표시
/// 
/// 핵심 기능:
/// - 이벤트 기반 UI 업데이트
/// - 스킬 슬롯 초기화 및 관리
/// - 마나 부족 시각적 피드백
/// - 키보드 단축키 (C: 캐릭터 창, K: 스킬 창)
/// 
/// 이벤트 구독:
/// - PlayerCharacter: 스탯 변경, 스킬 언락
/// - ExperienceManager: 경험치 변경, 레벨업
/// 
/// 연동 시스템:
/// - PlayerCharacter: 데이터 소스
/// - SkillActivationSystem: 스킬 슬롯 등록
/// - ExperienceManager: 진행도 표시
/// 
/// UI 업데이트 흐름:
/// 1. 게임 내 이벤트 발생 (레벨업, 스탯 변경 등)
/// 2. 관련 시스템이 이벤트 발생
/// 3. CharacterUIController가 이벤트 수신
/// 4. UI 요소 업데이트
/// 5. 플레이어에게 시각적 피드백
/// 
/// 초기화 과정:
/// 1. Awake: 컴포넌트 참조 찾기
/// 2. OnEnable: 이벤트 구독
/// 3. Start: 초기 UI 설정
/// 4. 이후 이벤트 기반 업데이트
/// </summary>
public class CharacterUIController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;
    [SerializeField] private SkillActivationSystem _skillActivationSystem;

    [Header("UI 패널")]
    [SerializeField] private HealthManaBar _healthManaBar;     // 체력/마나 바 컴포넌트
    [SerializeField] private GameObject _characterPanel;       // 캐릭터 스탯 창
    [SerializeField] private GameObject _skillPanel;           // 스킬 창

    [Header("캐릭터 스탯 표시")]
    [SerializeField] private TextMeshProUGUI _levelText;          // 레벨
    [SerializeField] private TextMeshProUGUI _strengthText;       // 힘
    [SerializeField] private TextMeshProUGUI _dexterityText;      // 민첩
    [SerializeField] private TextMeshProUGUI _intelligenceText;   // 지능
    [SerializeField] private TextMeshProUGUI _vitalityText;       // 활력
    [SerializeField] private TextMeshProUGUI _critChanceText;     // 크리티컬 확률
    [SerializeField] private TextMeshProUGUI _critDamageText;     // 크리티컬 데미지

    [Header("경험치 바")]
    [SerializeField] private UnityEngine.UI.Image _experienceFillBar;   // 경험치 바 (Fill Amount)
    [SerializeField] private TextMeshProUGUI _experienceText;           // 경험치 텍스트 (현재/필요)

    [Header("스킬 슬롯")]
    [SerializeField] private List<SkillSlotUI> _skillSlots = new List<SkillSlotUI>();  // Q/W/E 스킬 슬롯

    private ExperienceManager _experienceManager;

    private void Awake()
    {
        // 컴포넌트 자동 찾기 (Inspector에서 할당 안 된 경우)
        if (_playerCharacter == null)
        {
            _playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        }

        if (_skillActivationSystem == null)
        {
            _skillActivationSystem = FindFirstObjectByType<SkillActivationSystem>();
        }

        _experienceManager = FindFirstObjectByType<ExperienceManager>();

        // 스킬 슬롯을 SkillActivationSystem에 등록
        RegisterSkillSlots();
    }

    /// <summary>
    /// 이벤트 구독
    /// 
    /// OnEnable에서 구독, OnDisable에서 해제하는 패턴:
    /// - 메모리 누수 방지
    /// - 오브젝트 비활성화 시 이벤트 수신 중단
    /// - 재활성화 시 자동 구독
    /// 
    /// 구독하는 이벤트:
    /// - 스탯 변경: UpdateStatsDisplay()
    /// - 스킬 언락: HandleSkillUnlocked()
    /// - 경험치 변경: UpdateExperienceBar()
    /// - 레벨업: UpdateLevelDisplay()
    /// </summary>
    private void OnEnable()
    {
        PlayerCharacter.OnStatsChanged += UpdateStatsDisplay;
        PlayerCharacter.OnSkillUnlocked += HandleSkillUnlocked;
        ExperienceManager.OnExperienceChanged += UpdateExperienceBar;
        ExperienceManager.OnLevelUp += UpdateLevelDisplay;
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// 
    /// 중요: OnDisable에서 반드시 해제해야 함
    /// 이유: 메모리 누수 방지
    /// </summary>
    private void OnDisable()
    {
        PlayerCharacter.OnStatsChanged -= UpdateStatsDisplay;
        PlayerCharacter.OnSkillUnlocked -= HandleSkillUnlocked;
        ExperienceManager.OnExperienceChanged -= UpdateExperienceBar;
        ExperienceManager.OnLevelUp -= UpdateLevelDisplay;
    }

    private void Start()
    {
        // 게임 시작 시 초기 UI 설정
        InitializeUI();
    }

    private void Update()
    {
        HandleUIInput();           // 키보드 단축키 처리
        UpdateSkillManaStatus();   // 스킬 마나 상태 업데이트
    }

    /// <summary>
    /// UI 초기화
    /// 
    /// 게임 시작 시 한 번 실행:
    /// 1. 현재 스탯 표시
    /// 2. 현재 레벨 및 경험치 표시
    /// 3. 스킬 슬롯 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 스탯 표시
        if (_playerCharacter != null)
        {
            UpdateStatsDisplay(_playerCharacter.CurrentStats);
        }

        // 경험치/레벨 표시
        if (_experienceManager != null)
        {
            UpdateLevelDisplay(_experienceManager.CurrentLevel, _experienceManager.CurrentLevel);
            UpdateExperienceBar(
                _experienceManager.CurrentExperience,
                _experienceManager.ExperienceToNextLevel + _experienceManager.CurrentExperience,
                _experienceManager.CurrentLevel
            );
        }

        // 스킬 슬롯 초기화
        InitializeSkillSlots();
    }

    /// <summary>
    /// 스킬 슬롯 초기화
    /// 
    /// 처리 과정:
    /// 1. 플레이어의 언락된 스킬 목록 가져오기
    /// 2. 각 슬롯에 스킬 할당
    /// 3. 슬롯 언락 (활성화)
    /// 
    /// 슬롯 매핑:
    /// - _skillSlots[0] = Q키 스킬
    /// - _skillSlots[1] = W키 스킬
    /// - _skillSlots[2] = E키 스킬
    /// 
    /// 주의: 언락된 스킬보다 슬롯이 많을 수 있음
    ///       (빈 슬롯은 잠금 상태 유지)
    /// </summary>
    private void InitializeSkillSlots()
    {
        if (_playerCharacter == null)
        {
            return;
        }

        for (int i = 0; i < _skillSlots.Count; i++)
        {
            // 해당 인덱스에 언락된 스킬이 있는지 확인
            if (i < _playerCharacter.UnlockedSkills.Count)
            {
                _skillSlots[i].Initialize(_playerCharacter.UnlockedSkills[i]);
                _skillSlots[i].Unlock();  // 슬롯 활성화
            }
        }
    }

    /// <summary>
    /// 스킬 슬롯을 SkillActivationSystem에 등록
    /// 
    /// 목적:
    /// - SkillActivationSystem이 쿨다운 시작 시 UI에 알림
    /// - UI와 로직의 동기화
    /// 
    /// 양방향 통신:
    /// - SkillActivationSystem → SkillSlotUI: 쿨다운 시작 알림
    /// - SkillSlotUI → SkillActivationSystem: (없음, 일방향)
    /// </summary>
    private void RegisterSkillSlots()
    {
        if (_skillActivationSystem == null)
        {
            return;
        }

        foreach (SkillSlotUI slot in _skillSlots)
        {
            _skillActivationSystem.RegisterSkillSlot(slot);
        }
    }

    /// <summary>
    /// 스탯 표시 업데이트
    /// 
    /// 호출 시점:
    /// - 게임 시작 (InitializeUI)
    /// - 레벨업 시
    /// - 장비 변경 시
    /// - 버프/디버프 적용 시
    /// 
    /// Parameters:
    ///     stats: 표시할 스탯 (기본 + 장비 합산)
    ///     
    /// 표시 형식:
    /// - 주요 스탯: "STR: 25" (정수)
    /// - 크리티컬: "Crit Chance: 15.5%" (소수점 1자리)
    /// </summary>
    private void UpdateStatsDisplay(CharacterStats stats)
    {
        if (stats == null)
        {
            return;
        }

        if (_strengthText != null)
        {
            _strengthText.text = $"STR: {stats.Strength}";
        }

        if (_dexterityText != null)
        {
            _dexterityText.text = $"DEX: {stats.Dexterity}";
        }

        if (_intelligenceText != null)
        {
            _intelligenceText.text = $"INT: {stats.Intelligence}";
        }

        if (_vitalityText != null)
        {
            _vitalityText.text = $"VIT: {stats.Vitality}";
        }

        if (_critChanceText != null)
        {
            _critChanceText.text = $"Crit Chance: {stats.CriticalChance:F1}%";
        }

        if (_critDamageText != null)
        {
            _critDamageText.text = $"Crit Damage: {stats.CriticalDamage:F0}%";
        }
    }

    /// <summary>
    /// 경험치 바 업데이트
    /// 
    /// 호출 시점:
    /// - 경험치 획득 시
    /// - 레벨업 시
    /// 
    /// Parameters:
    ///     current: 현재 누적 경험치
    ///     required: 다음 레벨까지 필요한 누적 경험치
    ///     level: 현재 레벨 (사용 안 함)
    ///     
    /// UI 업데이트:
    /// - Fill Amount: 경험치 비율 (0~1)
    /// - Text: "150 / 250" 형식
    /// 
    /// 예시:
    /// - current: 150
    /// - required: 250
    /// - fillAmount: 150/250 = 0.6 (60%)
    /// </summary>
    private void UpdateExperienceBar(int current, int required, int level)
    {
        if (_experienceFillBar != null)
        {
            float fillAmount = required > 0 ? (float)current / required : 0f;
            _experienceFillBar.fillAmount = fillAmount;
        }

        if (_experienceText != null)
        {
            _experienceText.text = $"{current} / {required}";
        }
    }

    /// <summary>
    /// 레벨 표시 업데이트
    /// 
    /// 호출 시점:
    /// - 레벨업 시
    /// 
    /// Parameters:
    ///     oldLevel: 이전 레벨 (사용 안 함)
    ///     newLevel: 새 레벨
    ///     
    /// 표시 형식: "Level 5"
    /// </summary>
    private void UpdateLevelDisplay(int oldLevel, int newLevel)
    {
        if (_levelText != null)
        {
            _levelText.text = $"Level {newLevel}";
        }
    }

    /// <summary>
    /// 새 스킬 언락 처리
    /// 
    /// 호출 시점:
    /// - PlayerCharacter.UnlockSkillsForLevel() 실행 후
    /// 
    /// 처리 과정:
    /// 1. 플레이어의 언락된 스킬 목록에서 인덱스 찾기
    /// 2. 해당 인덱스의 스킬 슬롯에 할당
    /// 3. 슬롯 언락 (활성화)
    /// 
    /// Parameters:
    ///     skill: 언락된 스킬 데이터
    ///     
    /// 주의: 스킬 슬롯이 부족하면 무시
    ///       (최대 3개 슬롯만 지원)
    /// </summary>
    private void HandleSkillUnlocked(SkillData skill)
    {
        int skillIndex = _playerCharacter.UnlockedSkills.IndexOf(skill);

        if (skillIndex >= 0 && skillIndex < _skillSlots.Count)
        {
            _skillSlots[skillIndex].Initialize(skill);
            _skillSlots[skillIndex].Unlock();
        }
    }

    /// <summary>
    /// 스킬 마나 상태 업데이트 (매 프레임)
    /// 
    /// 목적:
    /// - 마나 부족 시 스킬 슬롯을 빨간색으로 표시
    /// - 플레이어에게 시각적 피드백
    /// 
    /// 처리 과정:
    /// 1. 각 언락된 스킬 순회
    /// 2. 현재 마나 >= 스킬 마나 코스트?
    /// 3. SkillSlotUI에 상태 전달
    /// 4. UI가 색상 변경
    /// 
    /// 색상:
    /// - 충분: 흰색
    /// - 부족: 빨간색
    /// </summary>
    private void UpdateSkillManaStatus()
    {
        if (_playerCharacter == null)
        {
            return;
        }

        for (int i = 0; i < _skillSlots.Count && i < _playerCharacter.UnlockedSkills.Count; i++)
        {
            SkillData skill = _playerCharacter.UnlockedSkills[i];
            bool hasEnoughMana = _playerCharacter.CurrentMana >= skill.ManaCost;

            _skillSlots[i].SetInsufficientMana(!hasEnoughMana);
        }
    }

    /// <summary>
    /// 키보드 단축키 처리
    /// 
    /// 단축키:
    /// - C키: 캐릭터 패널 토글
    /// - K키: 스킬 패널 토글
    /// 
    /// 토글: 열려있으면 닫고, 닫혀있으면 열기
    /// </summary>
    private void HandleUIInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCharacterPanel();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleSkillPanel();
        }
    }

    /// <summary>
    /// 캐릭터 패널 토글
    /// 현재 활성 상태의 반대로 설정
    /// </summary>
    private void ToggleCharacterPanel()
    {
        if (_characterPanel != null)
        {
            _characterPanel.SetActive(!_characterPanel.activeSelf);
        }
    }

    /// <summary>
    /// 스킬 패널 토글
    /// 현재 활성 상태의 반대로 설정
    /// </summary>
    private void ToggleSkillPanel()
    {
        if (_skillPanel != null)
        {
            _skillPanel.SetActive(!_skillPanel.activeSelf);
        }
    }
}