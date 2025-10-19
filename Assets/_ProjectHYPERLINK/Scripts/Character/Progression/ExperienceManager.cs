using UnityEngine;
using System;

/// <summary>
/// 경험치 및 레벨링 시스템 (Cloud Save 통합)
/// 
/// 주요 변경사항:
/// - 내부 ExperienceData 제거
/// - LoadFromSaveData() / SaveToData() 추가
/// - CharacterDataManager와 협업
/// 
/// 기능:
/// - 경험치 획득 및 누적
/// - 자동 레벨업 처리
/// - 스탯 증가 적용
/// - 스킬 언락
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    [Header("경험치 설정")]
    [SerializeField] private LevelUpData _levelUpData;
    [SerializeField] private int _currentLevel = 1;
    [SerializeField] private int _currentExperience = 0;

    // 이벤트
    public static event Action<int> OnExperienceGained;
    public static event Action<int, int> OnLevelUp;
    public static event Action<int, int, int> OnExperienceChanged;

    private PlayerCharacter _playerCharacter;

    public int CurrentLevel => _currentLevel;
    public int CurrentExperience => _currentExperience;
    public int ExperienceToNextLevel => GetExperienceRequiredForLevel(_currentLevel + 1) - _currentExperience;
    public bool CanLevelUp => _currentLevel < _levelUpData.MaxLevel;

    private void Awake()
    {
        // 자동 검색 추가
        if (_playerCharacter == null)
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
            if (_playerCharacter == null)
            {
                Debug.LogError("[ExperienceManager] PlayerCharacter를 찾을 수 없습니다!");
            }
        }

        if (_levelUpData == null)
        {
            Debug.LogError("LevelUpData가 할당되지 않았습니다!");
        }
    }

    private void Start()
    {
        OnExperienceChanged?.Invoke(_currentExperience, GetExperienceRequiredForLevel(_currentLevel + 1), _currentLevel);
    }

    /// <summary>
    /// 경험치 획득
    /// Enemy.Die()에서 호출
    /// </summary>
    public void GainExperience(int amount)
    {
        if (!CanLevelUp)
            return;

        _currentExperience += amount;
        OnExperienceGained?.Invoke(amount);

        CheckForLevelUp();

        OnExperienceChanged?.Invoke(_currentExperience, GetExperienceRequiredForLevel(_currentLevel + 1), _currentLevel);
    }

    /// <summary>
    /// 레벨업 체크 (연속 레벨업 지원)
    /// </summary>
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

        // 스킬 언락
        _playerCharacter?.UnlockSkillsForLevel(_currentLevel);

        Debug.Log($"레벨 업! 이제 레벨 {_currentLevel}");
        OnLevelUp?.Invoke(oldLevel, _currentLevel);
    }

    private int GetExperienceRequiredForLevel(int level)
    {
        return _levelUpData.GetExperienceRequiredForLevel(level);
    }

    #region Cloud Save 통합

    /// <summary>
    /// CharacterSaveData에서 데이터 로드
    /// CharacterDataManager에서 호출
    /// 
    /// 복원 항목:
    /// - 레벨
    /// - 누적 경험치
    /// </summary>
    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("로드할 데이터가 null입니다!");
            return;
        }

        _currentLevel = data.character.level;
        _currentExperience = data.character.experience;

        // UI 업데이트
        OnExperienceChanged?.Invoke(_currentExperience, GetExperienceRequiredForLevel(_currentLevel + 1), _currentLevel);

        Debug.Log($"경험치 시스템 로드 완료: 레벨 {_currentLevel}, 경험치 {_currentExperience}");
    }

    /// <summary>
    /// 현재 상태를 CharacterSaveData에 저장
    /// CharacterDataManager에서 호출
    /// 
    /// 저장 항목:
    /// - 레벨
    /// - 누적 경험치
    /// </summary>
    public void SaveToData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("저장할 데이터가 null입니다!");
            return;
        }

        data.character.level = _currentLevel;
        data.character.experience = _currentExperience;
    }

    #endregion
}
