using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 데이터 중앙 관리 시스템
/// 
/// [수정] GameSessionManager와 동기화 추가
/// - LoadCharacterData() 후 GameSessionManager 업데이트
/// - CollectAndSaveData() 전 GameSessionManager 동기화
/// 
/// 기능:
/// - Cloud Save 연동
/// - Experience, Character, Equipment, Inventory 데이터 관리
/// - 골드 관리
/// - 자동 저장 (5분마다)
/// - 플레이 시간 추적
/// - 스킬 트리 저장/로드
/// - 스킬 슬롯 (Q/W/E/R) 저장/로드
/// - 장비 UI 동기화
/// </summary>
public class CharacterDataManager : MonoBehaviour
{
    private static CharacterDataManager _instance;
    public static CharacterDataManager Instance => _instance;

    [Header("자동 저장 설정")]
    [SerializeField] private float _autoSaveInterval = 300f;

    [Header("디버그 설정")]
    [SerializeField] private bool _enableDebugLogs = true;

    private float _autoSaveTimer = 0f;
    private float _sessionStartTime;
    private long _totalPlayTimeSeconds;

    private CharacterSaveData _currentCharacterData;

    private PlayerCharacter _playerCharacter;
    private ExperienceManager _experienceManager;
    private EquipmentManager _equipmentManager;
    private EquipInventory _equipInventory;
    private SkillTreeManager _skillTreeManager;
    private SkillActivationSystem _skillActivationSystem;

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
    /// 시스템 참조 초기화
    /// </summary>
    public void InitializeSystemReferences()
    {
        _playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        _experienceManager = FindFirstObjectByType<ExperienceManager>();
        _equipmentManager = FindFirstObjectByType<EquipmentManager>();
        _equipInventory = FindFirstObjectByType<EquipInventory>();
        _skillTreeManager = FindFirstObjectByType<SkillTreeManager>();
        _skillActivationSystem = FindFirstObjectByType<SkillActivationSystem>();

        if (_playerCharacter == null)
            LogError("PlayerCharacter를 찾을 수 없습니다");

        if (_experienceManager == null)
            LogError("ExperienceManager를 찾을 수 없습니다");

        if (_equipmentManager == null)
            LogError("EquipmentManager를 찾을 수 없습니다");

        if (_equipInventory == null)
            LogWarning("EquipInventory를 찾을 수 없습니다 (장비 UI 로드 불가)");

        if (_skillTreeManager == null)
            LogWarning("SkillTreeManager를 찾을 수 없습니다 (스킬 트리 비활성화)");

        if (_skillActivationSystem == null)
            LogWarning("SkillActivationSystem를 찾을 수 없습니다 (스킬 슬롯 저장 비활성화)");
    }

    /// <summary>
    /// 캐릭터 데이터 로드
    /// GameSessionManager 동기화
    /// </summary>
    public async Task<bool> LoadCharacterData()
    {
        Log("캐릭터 데이터 로드 시작");

        _currentCharacterData = await CloudSaveManager.Instance.LoadCharacterDataAsync();

        if (_currentCharacterData == null)
        {
            LogError("캐릭터 데이터 로드 실패");
            return false;
        }

        // GameSessionManager 동기화
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.UpdateCharacterData(_currentCharacterData);
            Log("GameSessionManager 동기화 완료");
        }
        else
        {
            LogWarning("GameSessionManager를 찾을 수 없습니다");
        }

        if (_playerCharacter == null || _experienceManager == null)
        {
            InitializeSystemReferences();
        }

        ApplyDataToSystems(_currentCharacterData);

        _totalPlayTimeSeconds = _currentCharacterData.metadata.playTimeSeconds;
        _sessionStartTime = Time.time;

        Log($"캐릭터 로드 완료: {_currentCharacterData.character.characterName}, 레벨 {_currentCharacterData.character.level}, 골드 {_currentCharacterData.inventory.gold}");
        return true;
    }

    /// <summary>
    /// 로드된 데이터를 각 시스템에 적용
    /// </summary>
    private void ApplyDataToSystems(CharacterSaveData data)
    {
        // Phase 1: 경험치 및 레벨
        if (_experienceManager != null)
        {
            _experienceManager.LoadFromSaveData(data);
        }

        // Phase 2: 캐릭터 스탯 및 골드
        if (_playerCharacter != null)
        {
            _playerCharacter.LoadFromSaveData(data);
            Log($"캐릭터 로드 완료: HP {data.stats.currentHealth}/{data.stats.maxHealth}, 골드 {data.inventory.gold}");
        }

        // Phase 3: 장비
        if (_equipmentManager != null)
        {
            _equipmentManager.LoadFromSaveData(data);
        }

        // Phase 4: 장비 UI 동기화
        if (_equipInventory != null)
        {
            _equipInventory.LoadEquipmentUI();
            Log("장비 UI 동기화 완료");
        }

        // Phase 5: 인벤토리
        LoadInventoryData(data);

        // Phase 6: 스킬 트리
        if (_skillTreeManager != null && data.progression != null && data.progression.skillTree != null)
        {
            _skillTreeManager.LoadSkillTree(data.progression.skillTree);
            Log("스킬 트리 로드 완료");
        }

        // Phase 7: 스킬 슬롯 (마지막)
        if (_skillActivationSystem != null && data.progression != null && data.progression.skillSlots != null)
        {
            LoadSkillSlots(data.progression.skillSlots);
        }
    }

    private void LoadInventoryData(CharacterSaveData data)
    {
        if (ItemInventory.Instance == null)
        {
            LogWarning("ItemInventory를 찾을 수 없습니다");
            return;
        }

        if (data.inventory == null || data.inventory.items == null)
        {
            Log("저장된 인벤토리 데이터 없음");
            return;
        }

        ItemInventory.Instance.ClearInventory();

        int successCount = 0;
        int failCount = 0;

        foreach (var item in data.inventory.items)
        {
            ItemData itemData = FindItemDataByNumber(item.itemId);

            if (itemData != null)
            {
                bool loaded = ItemInventory.Instance.LoadItemToSlot(itemData, item.slot);
                if (loaded)
                    successCount++;
                else
                    failCount++;
            }
            else
            {
                LogWarning($"아이템을 찾을 수 없음: {item.itemId}");
                failCount++;
            }
        }

        Log($"인벤토리 로드 완료: 성공 {successCount}개, 실패 {failCount}개");
    }

    private ItemData FindItemDataByNumber(string itemNumber)
    {
        if (_equipmentManager != null)
        {
            return _equipmentManager.FindItemByNumber(itemNumber);
        }

        LogWarning($"EquipmentManager를 찾을 수 없음");
        return null;
    }

    private void LoadSkillSlots(List<SkillSlotData> skillSlots)
    {
        if (_skillActivationSystem == null)
        {
            LogWarning("SkillActivationSystem이 없어 스킬 슬롯을 로드할 수 없습니다");
            return;
        }

        List<SkillSlotUI> slots = _skillActivationSystem.GetAllSkillSlots();

        if (slots == null || slots.Count == 0)
        {
            LogWarning("스킬 슬롯 UI를 찾을 수 없습니다");
            return;
        }

        int loadedCount = 0;
        int failedCount = 0;

        foreach (SkillSlotData slotData in skillSlots)
        {
            if (slotData.slotIndex < 0 || slotData.slotIndex >= slots.Count)
            {
                LogWarning($"잘못된 슬롯 인덱스: {slotData.slotIndex}");
                failedCount++;
                continue;
            }

            SkillSlotUI slot = slots[slotData.slotIndex];

            if (string.IsNullOrEmpty(slotData.assignedSkillID))
            {
                slot.RemoveSkill();
                continue;
            }

            SkillData skillData = FindSkillByName(slotData.assignedSkillID);

            if (skillData != null)
            {
                slot.AssignSkill(skillData);
                loadedCount++;
                Log($"슬롯 {slotData.slotIndex} 로드: {skillData.SkillName}");
            }
            else
            {
                LogWarning($"스킬을 찾을 수 없음: {slotData.assignedSkillID}");
                slot.RemoveSkill();
                failedCount++;
            }
        }

        Log($"스킬 슬롯 로드 완료: 성공 {loadedCount}개, 실패 {failedCount}개");
    }

    private SkillData FindSkillByName(string skillName)
    {
        if (_playerCharacter != null)
        {
            foreach (var skill in _playerCharacter.UnlockedSkills)
            {
                if (skill.SkillName == skillName)
                {
                    return skill;
                }
            }
        }

        if (_skillTreeManager != null)
        {
            var allNodes = _skillTreeManager.AllNodes;
            foreach (var node in allNodes)
            {
                if (node.SkillData != null && node.SkillData.SkillName == skillName)
                {
                    return node.SkillData;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 현재 상태 수집 및 저장
    /// 
    /// 저장 전 GameSessionManager 동기화
    /// </summary>
    public async Task<bool> CollectAndSaveData()
    {
        if (!IsDataLoaded)
        {
            LogWarning("저장할 데이터가 없습니다");
            return false;
        }

        Log("캐릭터 데이터 수집 및 저장 시작");

        UpdateMetadata();
        CollectDataFromSystems();

        // 저장 전 GameSessionManager 동기화
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.UpdateCharacterData(_currentCharacterData);
            Log("GameSessionManager 동기화 완료");
        }

        bool success = await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);

        if (success)
        {
            Log($"캐릭터 데이터 저장 완료 (골드: {_currentCharacterData.inventory.gold})");
        }
        else
        {
            LogError("캐릭터 데이터 저장 실패");
        }

        return success;
    }

    private void CollectDataFromSystems()
    {
        if (_experienceManager != null)
        {
            _experienceManager.SaveToData(_currentCharacterData);
        }

        if (_playerCharacter != null)
        {
            _playerCharacter.SaveToData(_currentCharacterData);
            Log($"PlayerCharacter 저장 완료 (골드: {_playerCharacter.CurrentGold})");
        }

        if (_equipmentManager != null)
        {
            _equipmentManager.SaveToData(_currentCharacterData);
        }

        SaveInventoryData(_currentCharacterData);

        if (_skillTreeManager != null)
        {
            if (_currentCharacterData.progression == null)
            {
                _currentCharacterData.progression = new CharacterSaveData.ProgressionData();
            }

            _currentCharacterData.progression.skillTree = _skillTreeManager.SaveSkillTree();
            Log("스킬 트리 저장 완료");
        }

        if (_skillActivationSystem != null)
        {
            if (_currentCharacterData.progression == null)
            {
                _currentCharacterData.progression = new CharacterSaveData.ProgressionData();
            }

            SaveSkillSlots(_currentCharacterData);
        }

        // 퀘스트 데이터 수집
        if (QuestManager.Instance != null)
        {
            CollectQuestData(_currentCharacterData);
            Log("퀘스트 데이터 수집 완료");
        }

        // 위치 정보
        if (_playerCharacter != null)
        {
            _currentCharacterData.position.scene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            Transform playerTransform = _playerCharacter.transform;
            _currentCharacterData.position.x = playerTransform.position.x;
            _currentCharacterData.position.y = playerTransform.position.y;
            _currentCharacterData.position.z = playerTransform.position.z;

            Log($"위치 저장: 씬={_currentCharacterData.position.scene}, 좌표=({_currentCharacterData.position.x:F2}, {_currentCharacterData.position.y:F2}, {_currentCharacterData.position.z:F2})");
        }
    }

    private void SaveInventoryData(CharacterSaveData data)
    {
        if (ItemInventory.Instance == null)
        {
            LogWarning("ItemInventory를 찾을 수 없습니다");
            return;
        }

        data.inventory.items.Clear();

        var allItems = ItemInventory.Instance.GetAllItems();
        foreach (var (itemData, slotIndex) in allItems)
        {
            if (itemData != null)
            {
                data.inventory.items.Add(new CharacterSaveData.InventoryData.InventoryItem
                {
                    itemId = itemData.ItemNumber.ToString(),
                    quantity = 1, // 아이템은 개별로 저장됨
                    slot = slotIndex
                });
            }
        }

        Log($"인벤토리 저장 완료: {data.inventory.items.Count}개 아이템");
    }

    private void SaveSkillSlots(CharacterSaveData data)
    {
        List<SkillSlotUI> slots = _skillActivationSystem.GetAllSkillSlots();

        if (slots == null || slots.Count == 0)
        {
            LogWarning("스킬 슬롯 UI를 찾을 수 없습니다");
            return;
        }

        data.progression.skillSlots.Clear();

        for (int i = 0; i < slots.Count; i++)
        {
            SkillSlotUI slot = slots[i];
            string skillID = slot.SkillData != null ? slot.SkillData.SkillName : "";

            data.progression.skillSlots.Add(new SkillSlotData(i, skillID));
            Log($"슬롯 {i} 저장: {(string.IsNullOrEmpty(skillID) ? "Empty" : skillID)}");
        }

        Log($"스킬 슬롯 저장 완료: {slots.Count}개");
    }

    private void CollectQuestData(CharacterSaveData data)
    {
        if (data.gameplay == null)
        {
            data.gameplay = new CharacterSaveData.GameplayData();
        }

        data.gameplay.questsCompleted = QuestManager.Instance.GetCompletedQuestIDs();
        data.gameplay.questProgress = QuestManager.Instance.GetActiveQuestProgress();

        Log($"퀘스트 데이터 수집: 완료 {data.gameplay.questsCompleted.Count}개, 진행중 {data.gameplay.questProgress.Count}개");
    }

    private void UpdateMetadata()
    {
        float sessionTime = Time.time - _sessionStartTime;
        _totalPlayTimeSeconds += (long)sessionTime;
        _sessionStartTime = Time.time;

        _currentCharacterData.metadata.lastPlayed = System.DateTime.UtcNow.ToString("o");
        _currentCharacterData.metadata.playTimeSeconds = _totalPlayTimeSeconds;
    }

    private async Task AutoSave()
    {
        Log("자동 저장 실행");
        await CollectAndSaveData();
    }

    private async void OnApplicationQuit()
    {
        if (IsDataLoaded)
        {
            Log("게임 종료 - 최종 저장 실행");
            await CollectAndSaveData();
        }
    }

    public string GetCharacterName()
    {
        return _currentCharacterData?.character.characterName ?? "Unknown";
    }

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[CharacterDataManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[CharacterDataManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[CharacterDataManager] {message}");
    }

    #endregion
}
