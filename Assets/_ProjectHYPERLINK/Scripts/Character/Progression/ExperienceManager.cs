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
/// - 스킬 언락: 레벨에 맞는 스킬 잠금 해제
/// - 이벤트 발생: UI 및 다른 시스템에 알림
/// 
/// 주의사항:
/// - PlayerCharacter는 같은 GameObject에 필수
/// - LevelUpData ScriptableObject 할당 필수
/// - 연속 레벨업 지원 (한 번에 여러 레벨 상승 가능)

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
        // PlayerCharacter 자동 검색
        if (_playerCharacter == null)
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
            if (_playerCharacter == null)
            {
                Debug.LogError("[ExperienceManager] PlayerCharacter를 찾을 수 없습니다");
            }
        }

        if (_levelUpData == null)
        {
            Debug.LogError("LevelUpData가 할당되지 않았습니다");
        }
    }

    private void Start()
    {
        OnExperienceChanged?.Invoke(_currentExperience,
            GetExperienceRequiredForLevel(_currentLevel + 1), _currentLevel);
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

        OnExperienceChanged?.Invoke(_currentExperience,
            GetExperienceRequiredForLevel(_currentLevel + 1), _currentLevel);
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

    /// 레벨업 처리
    /// 
    /// 처리 순서:
    /// 1. 레벨 증가
    /// 2. LevelUpData에서 스탯 증가량 가져오기
    /// 3. PlayerCharacter에 스탯 적용
    /// 4. 스킬 언락 확인
    /// 5. 이벤트 발생
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

        Debug.Log($"레벨 업 이제 레벨 {_currentLevel}");
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
            Debug.LogError("로드할 데이터가 null입니다");
            return;
        }

        _currentLevel = data.character.level;
        _currentExperience = data.character.experience;

        // UI 업데이트
        OnExperienceChanged?.Invoke(_currentExperience,
            GetExperienceRequiredForLevel(_currentLevel + 1), _currentLevel);

        Debug.Log($"경험치 시스템 로드 완료: 레벨 {_currentLevel}, 경험치 {_currentExperience}");
    }

    /// 현재 상태를 CharacterSaveData에 저장
    /// CharacterDataManager에서 호출
    public void SaveToData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("저장할 데이터가 null입니다");
            return;
        }

        data.character.level = _currentLevel;
        data.character.experience = _currentExperience;
    }

    #endregion
}
