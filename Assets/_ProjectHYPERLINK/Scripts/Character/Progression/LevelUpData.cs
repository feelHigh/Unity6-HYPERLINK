using UnityEngine;

/// <summary>
/// 레벨업 데이터 ScriptableObject
/// 
/// 역할:
/// - 레벨별 필요 경험치 정의
/// - 레벨업 시 스탯 증가량 정의
/// - 게임 전체의 성장 곡선 결정
/// 
/// 구조:
/// - 배열 형태로 레벨 1~15 데이터 저장
/// - 각 레벨마다:
///   1. 필요 누적 경험치
///   2. 스탯 증가량 (CharacterStats)
/// 
/// 사용 위치:
/// - ExperienceManager: 경험치 및 레벨업 체크
/// - 게임 밸런싱: 디자이너가 에디터에서 수치 조절
/// 
/// 설정 방법:
/// 1. Project 창에서 Create > Character > Level Up Data
/// 2. Inspector에서 레벨별 데이터 입력
/// 3. ExperienceManager에 할당
/// 
/// 밸런싱 고려사항:
/// - 초반 레벨: 빠른 성장 (낮은 경험치 요구)
/// - 후반 레벨: 느린 성장 (높은 경험치 요구)
/// - 스탯 증가: 직업별 차별화 가능
/// 
/// Diablo 3 성장 곡선:
/// - 레벨 1-10: 빠른 성장 (튜토리얼)
/// - 레벨 10-70: 선형/지수 혼합
/// - 레벨 70+: Paragon 시스템 (무한 성장)
/// </summary>
[CreateAssetMenu(fileName = "LevelUpData", menuName = "Character/Level Up Data")]
public class LevelUpData : ScriptableObject
{
    /// <summary>
    /// 레벨별 요구사항 데이터 구조체
    /// 
    /// 각 레벨마다 하나씩 생성:
    /// - 레벨 2 요구사항
    /// - 레벨 3 요구사항
    /// - ...
    /// - 레벨 15 요구사항
    /// </summary>
    [System.Serializable]
    public class LevelRequirements
    {
        [SerializeField] private int _level;                    // 목표 레벨 (예: 2, 3, 4, ...)
        [SerializeField] private int _experienceRequired;       // 필요한 누적 경험치
        [SerializeField] private CharacterStats _statGains;     // 레벨업 시 증가하는 스탯

        public int Level => _level;
        public int ExperienceRequired => _experienceRequired;
        public CharacterStats StatGains => _statGains;
    }

    [SerializeField] private LevelRequirements[] _levelData;  // 모든 레벨의 데이터 배열
    [SerializeField] private int _maxLevel = 15;              // 최대 레벨 (현재 15)

    public int MaxLevel => _maxLevel;
    public LevelRequirements[] LevelData => _levelData;

    /// <summary>
    /// 특정 레벨까지 필요한 누적 경험치 반환
    /// 
    /// ExperienceManager.GetExperienceRequiredForLevel()에서 호출
    /// 
    /// 예시:
    /// - 레벨 2: 100 XP
    /// - 레벨 3: 250 XP (누적)
    /// - 레벨 4: 450 XP (누적)
    /// 
    /// Parameters:
    ///     level: 조회할 목표 레벨 (2~15)
    ///     
    /// Returns:
    ///     int: 해당 레벨까지 필요한 누적 경험치
    ///          레벨 1 이하 또는 범위 초과 시 0 반환
    ///          
    /// 주의사항:
    /// - 배열 인덱스는 0부터 시작
    /// - 레벨 2 데이터 = _levelData[0]
    /// - 레벨 3 데이터 = _levelData[1]
    /// - 따라서 level - 2가 인덱스
    /// </summary>
    public int GetExperienceRequiredForLevel(int level)
    {
        // 레벨 1 이하 또는 범위 초과
        if (level <= 1 || level > _levelData.Length + 1)
        {
            return 0;
        }

        // 배열 인덱스 계산: 레벨 - 2
        // (레벨 2 = 인덱스 0, 레벨 3 = 인덱스 1, ...)
        return _levelData[level - 2].ExperienceRequired;
    }

    /// <summary>
    /// 특정 레벨의 스탯 증가량 반환
    /// 
    /// ExperienceManager.LevelUp()에서 호출
    /// 
    /// 사용 흐름:
    /// 1. 플레이어가 레벨 5 달성
    /// 2. GetStatGainsForLevel(5) 호출
    /// 3. 레벨 5의 스탯 증가량 (예: 힘+2, 체력+10) 반환
    /// 4. PlayerCharacter.AddLevelUpStats()로 적용
    /// 
    /// Parameters:
    ///     level: 조회할 목표 레벨 (2~15)
    ///     
    /// Returns:
    ///     CharacterStats: 해당 레벨의 스탯 증가량
    ///                     레벨 1 이하 또는 범위 초과 시 null
    ///                     
    /// 밸런싱 팁:
    /// - 초반: 작은 증가량 (힘+1, 체력+5)
    /// - 후반: 큰 증가량 (힘+3, 체력+20)
    /// - 직업별로 다른 증가량 설정 가능
    /// </summary>
    public CharacterStats GetStatGainsForLevel(int level)
    {
        // 레벨 1 이하 또는 범위 초과
        if (level <= 1 || level > _levelData.Length + 1)
        {
            return null;
        }

        // 배열 인덱스 계산
        return _levelData[level - 2].StatGains;
    }
}