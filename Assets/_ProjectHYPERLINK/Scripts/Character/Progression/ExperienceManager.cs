using UnityEngine;
using System;

/// 시스템 분류: 캐릭터 진행 시스템
/// 
/// 의존성: PlayerCharacter, LevelUpData
/// 피의존성: CharacterDataManager, CharacterUIController, Enemy
/// 
/// 핵심 기능: 경험치 획득 및 레벨업 처리
/// 
/// 기능:
/// - 경험치 획득: 적 처치 시 경험치 추가
/// - 레벨업 처리: 필요 경험치 도달 시 자동 레벨업
/// - 스탯 증가: LevelUpData 기반 스탯 상승
/// - 스킬 트리 시스템 사용
/// - 이벤트 발생: UI 및 다른 시스템에 알림
/// 
/// 주의사항:
/// - PlayerCharacter는 같은 GameObject에 필수
/// - LevelUpData ScriptableObject 할당 필수
/// - 연속 레벨업 지원 (한 번에 여러 레벨 상승 가능)
/// 
/// [수정사항]
/// - TotalExpRequiredForNextLevel 추가: UI 바의 최대값
/// - OnExperienceChanged 이벤트 파라미터 명확화

public class ExperienceManager : MonoBehaviour
{
    [Header("경험치 설정")]
    [SerializeField] private LevelUpData _levelUpData;
    [SerializeField] private int _currentLevel = 1;
    [SerializeField] private int _currentExperience = 0;

    // 이벤트
    public static event Action<int> OnExperienceGained;
    public static event Action<int, int> OnLevelUp;

    /// <summary>
    /// 경험치 변경 이벤트
    /// 파라미터: (현재 경험치, 다음 레벨 필요 총 경험치, 현재 레벨)
    /// </summary>
    public static event Action<int, int, int> OnExperienceChanged;

    private PlayerCharacter _playerCharacter;

    public int CurrentLevel => _currentLevel;
    public int CurrentExperience => _currentExperience;

    /// <summary>
    /// 다음 레벨에 필요한 총 경험치 (누적)
    /// UI 바의 fillAmount 계산에 사용: currentExp / totalRequired
    /// </summary>
    public int TotalExpRequiredForNextLevel => GetExperienceRequiredForLevel(_currentLevel + 1);

    /// <summary>
    /// 다음 레벨까지 남은 경험치
    /// </summary>
    public int RemainingExpToNextLevel => TotalExpRequiredForNextLevel - _currentExperience;

    public bool CanLevelUp => _currentLevel < _levelUpData.MaxLevel;

    private void Awake()
    {
        // PlayerCharacter 자동 검색
        if (_playerCharacter == null)
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
            if (_playerCharacter == null)
            {
                DebugHelper.LogError("[ExperienceManager] PlayerCharacter를 찾을 수 없습니다");
            }
        }

        if (_levelUpData == null)
        {
            DebugHelper.LogError("LevelUpData가 할당되지 않았습니다");
        }
    }

    private void Start()
    {
        // 초기 UI 업데이트 - TotalExpRequiredForNextLevel 사용
        OnExperienceChanged?.Invoke(_currentExperience, TotalExpRequiredForNextLevel, _currentLevel);
        DebugHelper.Log($"[ExperienceManager] 초기화: 레벨 {_currentLevel}, 경험치 {_currentExperience}/{TotalExpRequiredForNextLevel}");
    }

    /// 경험치 획득
    /// Enemy.Die에서 호출
    public void GainExperience(int amount)
    {
        if (!CanLevelUp) return;

        _currentExperience += amount;
        OnExperienceGained?.Invoke(amount);

        // 레벨업 체크
        CheckForLevelUp();

        // UI 업데이트 - TotalExpRequiredForNextLevel 사용
        OnExperienceChanged?.Invoke(_currentExperience, TotalExpRequiredForNextLevel, _currentLevel);
    }

    /// 레벨업 체크 (연속 레벨업 지원)
    /// 
    /// 처리 과정:
    /// 1. 현재 경험치가 필요 경험치 이상인지 확인
    /// 2. 조건 충족 시 LevelUp 호출
    /// 3. 다음 레벨 필요 경험치 재확인
    /// 4. 계속 조건 충족 시 반복 (연속 레벨업)
    private void CheckForLevelUp()
    {
        int experienceRequired = GetExperienceRequiredForLevel(_currentLevel + 1);

        while (_currentExperience >= experienceRequired && CanLevelUp)
        {
            LevelUp();
            experienceRequired = GetExperienceRequiredForLevel(_currentLevel + 1);
        }
    }

    /// <summary>
    /// 레벨업 처리
    /// 
    /// 처리 순서:
    /// 1. 레벨 증가
    /// 2. LevelUpData에서 스탯 증가량 가져오기
    /// 3. PlayerCharacter에 스탯 적용
    /// 4. [REMOVED] 스킬 언락 → 스킬 트리에서 수동 언락
    /// 5. OnLevelUp 이벤트 발생 (SkillTreeManager가 SP 획득)
    /// </summary>
    private void LevelUp()
    {
        int oldLevel = _currentLevel;
        _currentLevel++;

        // 스탯 증가
        CharacterStats statGains = _levelUpData.GetStatGainsForLevel(_currentLevel);
        if (statGains != null && _playerCharacter != null)
        {
            _playerCharacter.AddLevelUpStats(statGains);
        }

        // OnLevelUp 이벤트를 통해 SkillTreeManager가 SP를 부여함
        DebugHelper.Log($"레벨 업! 레벨 {oldLevel} → {_currentLevel}");
        DebugHelper.Log($"[ExperienceManager] 스킬은 스킬 트리에서 SP로 언락하세요!");

        OnLevelUp?.Invoke(oldLevel, _currentLevel);
    }

    /// 레벨별 필요 경험치 계산
    private int GetExperienceRequiredForLevel(int level)
    {
        return _levelUpData.GetExperienceRequiredForLevel(level);
    }

    #region Cloud Save 연동

    /// CharacterSaveData에서 데이터 로드
    /// CharacterDataManager에서 호출
    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null)
        {
            DebugHelper.LogError("로드할 데이터가 null입니다");
            return;
        }

        _currentLevel = data.character.level;
        _currentExperience = data.character.experience;

        // UI 업데이트 - TotalExpRequiredForNextLevel 사용
        OnExperienceChanged?.Invoke(_currentExperience, TotalExpRequiredForNextLevel, _currentLevel);

        DebugHelper.Log($"경험치 시스템 로드 완료: 레벨 {_currentLevel}, 경험치 {_currentExperience}/{TotalExpRequiredForNextLevel}");
    }

    /// 현재 상태를 CharacterSaveData에 저장
    /// CharacterDataManager에서 호출
    public void SaveToData(CharacterSaveData data)
    {
        if (data == null)
        {
            DebugHelper.LogError("저장할 데이터가 null입니다");
            return;
        }

        data.character.level = _currentLevel;
        data.character.experience = _currentExperience;
    }

    #endregion
}
