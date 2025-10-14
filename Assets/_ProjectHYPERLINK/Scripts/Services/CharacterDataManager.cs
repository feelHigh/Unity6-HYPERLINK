using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// 캐릭터 데이터 중앙 관리자
/// 
/// 역할:
/// - 게임 내 모든 시스템에서 캐릭터 데이터 접근 제공
/// - CloudSaveManager와 게임 시스템 간 브릿지
/// - 데이터 로드/저장 조율
/// - 플레이 시간 추적
/// 
/// 사용 흐름:
/// 1. 게임 시작 → LoadCharacterData() 호출
/// 2. CharacterSaveData를 각 시스템에 분배
/// 3. 주기적으로 CollectAndSaveData() 호출
/// 4. 게임 종료 시 최종 저장
/// 
/// 통합 시스템:
/// - PlayerCharacter: 스탯, 체력/마나, 스킬
/// - ExperienceManager: 레벨, 경험치
/// - EquipmentManager: 장비 정보
/// - InventoryManager: 인벤토리 (추후)
/// </summary>
public class CharacterDataManager : MonoBehaviour
{
    private static CharacterDataManager _instance;
    public static CharacterDataManager Instance => _instance;

    [Header("자동 저장 설정")]
    [SerializeField] private float _autoSaveInterval = 300f; // 5분마다
    private float _autoSaveTimer = 0f;

    [Header("플레이 시간 추적")]
    private float _sessionStartTime;
    private long _totalPlayTimeSeconds;

    // 현재 로드된 캐릭터 데이터
    private CharacterSaveData _currentCharacterData;

    // 시스템 참조
    private PlayerCharacter _playerCharacter;
    private ExperienceManager _experienceManager;
    private EquipmentManager _equipmentManager;

    public CharacterSaveData CurrentCharacterData => _currentCharacterData;
    public bool IsDataLoaded => _currentCharacterData != null;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _sessionStartTime = Time.time;
    }

    private void Update()
    {
        // 자동 저장 타이머
        if (IsDataLoaded)
        {
            _autoSaveTimer += Time.deltaTime;

            if (_autoSaveTimer >= _autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                _ = AutoSave();
            }
        }
    }

    /// <summary>
    /// 게임 시스템 참조 설정
    /// 
    /// 게임 씬 로드 후 호출해야 함
    /// MainLevel 씬의 GameManager에서 호출
    /// </summary>
    public void InitializeSystemReferences()
    {
        _playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        _experienceManager = FindFirstObjectByType<ExperienceManager>();
        _equipmentManager = FindFirstObjectByType<EquipmentManager>();

        if (_playerCharacter == null)
            Debug.LogError("PlayerCharacter를 찾을 수 없습니다!");

        if (_experienceManager == null)
            Debug.LogError("ExperienceManager를 찾을 수 없습니다!");

        if (_equipmentManager == null)
            Debug.LogError("EquipmentManager를 찾을 수 없습니다!");
    }

    /// <summary>
    /// 클라우드에서 캐릭터 데이터 로드 및 게임 시스템에 적용
    /// 
    /// 호출 시점:
    /// - 게임 씬 진입 시 (캐릭터 선택 후)
    /// - 씬 전환 후 데이터 복원
    /// 
    /// 처리 과정:
    /// 1. CloudSaveManager에서 데이터 로드
    /// 2. 각 시스템에 데이터 분배
    /// 3. 플레이 시간 초기화
    /// </summary>
    public async Task<bool> LoadCharacterData()
    {
        Debug.Log("캐릭터 데이터 로드 시작...");

        // 1. 클라우드에서 로드
        _currentCharacterData = await CloudSaveManager.Instance.LoadCharacterDataAsync();

        if (_currentCharacterData == null)
        {
            Debug.LogError("캐릭터 데이터 로드 실패!");
            return false;
        }

        // 2. 게임 시스템 참조 확보
        if (_playerCharacter == null || _experienceManager == null)
        {
            InitializeSystemReferences();
        }

        // 3. 각 시스템에 데이터 적용
        ApplyDataToSystems(_currentCharacterData);

        // 4. 플레이 시간 초기화
        _totalPlayTimeSeconds = _currentCharacterData.metadata.playTimeSeconds;
        _sessionStartTime = Time.time;

        Debug.Log($"캐릭터 로드 완료: {_currentCharacterData.character.characterName}, 레벨 {_currentCharacterData.character.level}");
        return true;
    }

    /// <summary>
    /// 로드된 데이터를 각 시스템에 적용
    /// 
    /// 적용 순서가 중요:
    /// 1. ExperienceManager (레벨, 경험치)
    /// 2. PlayerCharacter (스탯, 체력/마나, 스킬)
    /// 3. EquipmentManager (장비)
    /// 4. InventoryManager (인벤토리) - 추후
    /// </summary>
    private void ApplyDataToSystems(CharacterSaveData data)
    {
        // 경험치 시스템
        if (_experienceManager != null)
        {
            _experienceManager.LoadFromSaveData(data);
        }

        // 캐릭터 시스템
        if (_playerCharacter != null)
        {
            _playerCharacter.LoadFromSaveData(data);
        }

        // 장비 시스템
        if (_equipmentManager != null)
        {
            _equipmentManager.LoadFromSaveData(data);
        }

        // TODO: InventoryManager 통합
    }

    /// <summary>
    /// 현재 게임 상태를 수집하여 클라우드에 저장
    /// 
    /// 호출 시점:
    /// - 자동 저장 (5분마다)
    /// - 수동 저장 (게임 종료, 씬 전환)
    /// - 중요 이벤트 (레벨업, 장비 변경 등)
    /// 
    /// 처리 과정:
    /// 1. 각 시스템에서 현재 상태 수집
    /// 2. CharacterSaveData 업데이트
    /// 3. CloudSaveManager로 저장
    /// </summary>
    public async Task<bool> CollectAndSaveData()
    {
        if (!IsDataLoaded)
        {
            Debug.LogWarning("저장할 데이터가 없습니다.");
            return false;
        }

        Debug.Log("캐릭터 데이터 수집 및 저장 시작...");

        // 1. 메타데이터 업데이트
        UpdateMetadata();

        // 2. 각 시스템에서 데이터 수집
        CollectDataFromSystems();

        // 3. 클라우드에 저장
        bool success = await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);

        if (success)
        {
            Debug.Log("캐릭터 데이터 저장 완료!");
        }
        else
        {
            Debug.LogError("캐릭터 데이터 저장 실패!");
        }

        return success;
    }

    /// <summary>
    /// 각 시스템에서 현재 상태 수집
    /// </summary>
    private void CollectDataFromSystems()
    {
        // 경험치/레벨
        if (_experienceManager != null)
        {
            _experienceManager.SaveToData(ref _currentCharacterData);
        }

        // 캐릭터 상태 (스탯, 체력/마나, 스킬)
        if (_playerCharacter != null)
        {
            _playerCharacter.SaveToData(ref _currentCharacterData);
        }

        // 장비
        if (_equipmentManager != null)
        {
            _equipmentManager.SaveToData(ref _currentCharacterData);
        }

        // 위치 정보
        if (_playerCharacter != null)
        {
            Transform playerTransform = _playerCharacter.transform;
            _currentCharacterData.position.x = playerTransform.position.x;
            _currentCharacterData.position.y = playerTransform.position.y;
            _currentCharacterData.position.z = playerTransform.position.z;
        }

        // TODO: InventoryManager에서 인벤토리 수집
    }

    /// <summary>
    /// 메타데이터 업데이트 (플레이 시간, 최종 플레이 시각)
    /// </summary>
    private void UpdateMetadata()
    {
        // 현재 세션 플레이 시간 계산
        float sessionTime = Time.time - _sessionStartTime;
        _totalPlayTimeSeconds += (long)sessionTime;
        _sessionStartTime = Time.time;

        // 메타데이터 업데이트
        _currentCharacterData.metadata.lastPlayed = System.DateTime.UtcNow.ToString("o");
        _currentCharacterData.metadata.playTimeSeconds = _totalPlayTimeSeconds;
    }

    /// <summary>
    /// 자동 저장
    /// </summary>
    private async Task AutoSave()
    {
        Debug.Log("자동 저장 실행...");
        await CollectAndSaveData();
    }

    /// <summary>
    /// 게임 종료 시 최종 저장
    /// </summary>
    private async void OnApplicationQuit()
    {
        if (IsDataLoaded)
        {
            Debug.Log("게임 종료 - 최종 저장 실행...");
            await CollectAndSaveData();
        }
    }

    /// <summary>
    /// 현재 캐릭터 이름 반환
    /// </summary>
    public string GetCharacterName()
    {
        return _currentCharacterData?.character.characterName ?? "Unknown";
    }

    /// <summary>
    /// 현재 레벨 반환
    /// </summary>
    public int GetCurrentLevel()
    {
        return _currentCharacterData?.character.level ?? 1;
    }

    /// <summary>
    /// 플레이 시간 반환 (초)
    /// </summary>
    public long GetPlayTime()
    {
        if (_currentCharacterData == null)
            return 0;

        float sessionTime = Time.time - _sessionStartTime;
        return _totalPlayTimeSeconds + (long)sessionTime;
    }
}