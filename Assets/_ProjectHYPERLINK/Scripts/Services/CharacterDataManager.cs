using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 데이터 중앙 관리 시스템
/// 
/// 기능:
/// - Cloud Save 연동
/// - Experience, Character, Equipment, Inventory 데이터 관리
/// - 자동 저장 (5분마다)
/// - 플레이 시간 추적
/// - 스킬 트리 저장/로드
/// - 스킬 슬롯 (Q/W/E/R) 저장/로드
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
    private SkillTreeManager _skillTreeManager;
    private SkillActivationSystem _skillActivationSystem;  // [NEW: SKILL SLOTS]

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
        _skillTreeManager = FindFirstObjectByType<SkillTreeManager>();
        _skillActivationSystem = FindFirstObjectByType<SkillActivationSystem>();

        if (_playerCharacter == null)
            LogError("PlayerCharacter를 찾을 수 없습니다");

        if (_experienceManager == null)
            LogError("ExperienceManager를 찾을 수 없습니다");

        if (_equipmentManager == null)
            LogError("EquipmentManager를 찾을 수 없습니다");

        if (_skillTreeManager == null)
            LogWarning("SkillTreeManager를 찾을 수 없습니다 (스킬 트리 비활성화)");

        if (_skillActivationSystem == null)
            LogWarning("SkillActivationSystem를 찾을 수 없습니다 (스킬 슬롯 저장 비활성화)");
    }

    /// <summary>
    /// 캐릭터 데이터 로드
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

        if (_playerCharacter == null || _experienceManager == null)
        {
            InitializeSystemReferences();
        }

        ApplyDataToSystems(_currentCharacterData);

        _totalPlayTimeSeconds = _currentCharacterData.metadata.playTimeSeconds;
        _sessionStartTime = Time.time;

        Log($"캐릭터 로드 완료: {_currentCharacterData.character.characterName}, 레벨 {_currentCharacterData.character.level}");
        return true;
    }

    /// <summary>
    /// 로드된 데이터를 각 시스템에 적용
    /// 
    /// 순서: Experience → Character → Equipment → Inventory → SkillTree → SkillSlots
    /// 
    /// 중요: 
    /// - 스킬 트리는 마지막에서 두 번째 (패시브 스탯 적용)
    /// - 스킬 슬롯은 가장 마지막 (스킬이 언락된 후 할당)
    /// </summary>
    private void ApplyDataToSystems(CharacterSaveData data)
    {
        // Phase 1: 경험치 및 레벨
        if (_experienceManager != null)
        {
            _experienceManager.LoadFromSaveData(data);
        }

        // Phase 2: 캐릭터 스탯
        if (_playerCharacter != null)
        {
            _playerCharacter.LoadFromSaveData(data);
        }

        // Phase 3: 장비
        if (_equipmentManager != null)
        {
            _equipmentManager.LoadFromSaveData(data);
        }

        // Phase 4: 인벤토리
        LoadInventoryData(data);

        // Phase 5: 스킬 트리 (마지막에서 두 번째)
        if (_skillTreeManager != null && data.progression?.skillTree != null)
        {
            _skillTreeManager.LoadSkillTree(data.progression.skillTree);
            Log("스킬 트리 로드 완료");

            // 스킬 트리 로드 후 UI 갱신 대기
            // SkillTreeManager가 OnSkillTreeLoaded 이벤트를 발생시키면
            // SkillTreeWindow가 자동으로 UI를 갱신함
        }
        else if (_skillTreeManager != null)
        {
            LogWarning("스킬 트리 저장 데이터 없음 (신규 캐릭터)");
        }

        // Phase 6: 스킬 슬롯 (가장 마지막)
        // 스킬이 언락된 후에 슬롯에 할당해야 함
        if (_skillActivationSystem != null && data.progression?.skillSlots != null)
        {
            LoadSkillSlots(data.progression.skillSlots);
        }
    }

    /// <summary>
    /// 인벤토리 데이터 로드
    /// </summary>
    private void LoadInventoryData(CharacterSaveData data)
    {
        if (ItemInventory.Instance == null)
        {
            LogWarning("ItemInventory 인스턴스를 찾을 수 없습니다");
            return;
        }

        if (data.inventory == null || data.inventory.items == null)
        {
            Log("저장된 인벤토리 데이터 없음");
            return;
        }

        // 인벤토리 초기화
        ItemInventory.Instance.ClearInventory();

        // 각 아이템을 슬롯에 로드
        int successCount = 0;
        int failCount = 0;

        foreach (var item in data.inventory.items)
        {
            // ItemNumber로 ItemData 찾기
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

    /// <summary>
    /// ItemNumber로 ItemData 찾기
    /// </summary>
    private ItemData FindItemDataByNumber(string itemNumber)
    {
        if (_equipmentManager != null)
        {
            return _equipmentManager.FindItemByNumber(itemNumber);
        }

        LogWarning($"EquipmentManager를 찾을 수 없음");
        return null;
    }

    /// <summary>
    /// 스킬 슬롯 로드
    /// 
    /// 처리 과정:
    /// 1. SkillActivationSystem에서 모든 슬롯 가져오기
    /// 2. 저장된 스킬 ID로 SkillData 찾기
    /// 3. 각 슬롯에 스킬 할당
    /// 
    /// 주의사항:
    /// - 스킬 트리 로드 후에 호출되어야 함 (스킬이 언락된 상태)
    /// - SkillData를 찾지 못하면 해당 슬롯은 빈 상태로 유지
    /// </summary>
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
            // 유효한 슬롯 인덱스 확인
            if (slotData.slotIndex < 0 || slotData.slotIndex >= slots.Count)
            {
                LogWarning($"잘못된 슬롯 인덱스: {slotData.slotIndex}");
                failedCount++;
                continue;
            }

            SkillSlotUI slot = slots[slotData.slotIndex];

            // 빈 슬롯인 경우
            if (string.IsNullOrEmpty(slotData.assignedSkillID))
            {
                slot.RemoveSkill();
                continue;
            }

            // 스킬 이름으로 SkillData 찾기
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

    /// <summary>
    /// 스킬 이름으로 SkillData 찾기
    /// 
    /// 검색 순서:
    /// 1. PlayerCharacter의 UnlockedSkills에서 검색
    /// 2. SkillTreeManager의 AllNodes에서 검색
    /// 
    /// 반환값:
    /// - 찾으면: SkillData
    /// - 못 찾으면: null
    /// </summary>
    private SkillData FindSkillByName(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
            return null;

        // 1. PlayerCharacter의 UnlockedSkills에서 검색 (빠름)
        if (_playerCharacter != null)
        {
            SkillData found = _playerCharacter.UnlockedSkills
                .FirstOrDefault(skill => skill.SkillName == skillName);

            if (found != null)
                return found;
        }

        // 2. SkillTreeManager의 AllNodes에서 검색 (폴백)
        if (_skillTreeManager != null)
        {
            foreach (SkillTreeNodeData node in _skillTreeManager.AllNodes)
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
    /// 캐릭터 데이터 수집 및 저장
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

        bool success = await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);

        if (success)
        {
            Log("캐릭터 데이터 저장 완료");
        }
        else
        {
            LogError("캐릭터 데이터 저장 실패");
        }

        return success;
    }

    /// <summary>
    /// 각 시스템에서 현재 상태 수집
    /// 
    /// Experience, Character, Equipment, Inventory, SkillTree, SkillSlots 모두 수집
    /// </summary>
    private void CollectDataFromSystems()
    {
        if (_experienceManager != null)
        {
            _experienceManager.SaveToData(_currentCharacterData);
        }

        if (_playerCharacter != null)
        {
            _playerCharacter.SaveToData(_currentCharacterData);
        }

        if (_equipmentManager != null)
        {
            _equipmentManager.SaveToData(_currentCharacterData);
        }

        // 인벤토리 저장
        SaveInventoryData(_currentCharacterData);

        // 스킬 트리 저장
        if (_skillTreeManager != null)
        {
            if (_currentCharacterData.progression == null)
            {
                _currentCharacterData.progression = new CharacterSaveData.ProgressionData();
            }

            _currentCharacterData.progression.skillTree = _skillTreeManager.SaveSkillTree();
            Log("스킬 트리 저장 완료");
        }

        // 스킬 슬롯 저장
        if (_skillActivationSystem != null)
        {
            if (_currentCharacterData.progression == null)
            {
                _currentCharacterData.progression = new CharacterSaveData.ProgressionData();
            }

            SaveSkillSlots(_currentCharacterData);
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

    /// <summary>
    /// 인벤토리 데이터 저장
    /// </summary>
    private void SaveInventoryData(CharacterSaveData saveData)
    {
        if (ItemInventory.Instance == null)
        {
            LogWarning("ItemInventory 인스턴스를 찾을 수 없습니다");
            return;
        }

        // 기존 인벤토리 데이터 초기화
        if (saveData.inventory == null)
        {
            saveData.inventory = new CharacterSaveData.InventoryData();
        }

        saveData.inventory.items.Clear();

        // 모든 아이템 수집
        List<(ItemData data, int slotIndex)> items = ItemInventory.Instance.GetAllItems();

        foreach (var (data, slotIndex) in items)
        {
            saveData.inventory.items.Add(new CharacterSaveData.InventoryData.InventoryItem
            {
                itemId = data.ItemNumber.ToString(),
                quantity = 1, // 현재는 수량 개념 없음
                slot = slotIndex
            });
        }

        Log($"인벤토리 저장: {items.Count}개 아이템");
    }

    /// <summary>
    /// 스킬 슬롯 저장
    /// 
    /// 처리 과정:
    /// 1. SkillActivationSystem에서 모든 슬롯 가져오기
    /// 2. 각 슬롯의 할당된 스킬 확인
    /// 3. SkillSlotData로 변환하여 저장
    /// 
    /// 빈 슬롯 처리:
    /// - 빈 슬롯도 저장 (assignedSkillID = "")
    /// - 로드 시 빈 슬롯으로 복원
    /// </summary>
    private void SaveSkillSlots(CharacterSaveData saveData)
    {
        if (_skillActivationSystem == null)
        {
            LogWarning("SkillActivationSystem이 없어 스킬 슬롯을 저장할 수 없습니다");
            return;
        }

        List<SkillSlotUI> slots = _skillActivationSystem.GetAllSkillSlots();

        if (slots == null || slots.Count == 0)
        {
            LogWarning("스킬 슬롯 UI를 찾을 수 없습니다");
            return;
        }

        // 기존 슬롯 데이터 초기화
        saveData.progression.skillSlots.Clear();

        // 각 슬롯 저장
        for (int i = 0; i < slots.Count; i++)
        {
            SkillSlotUI slot = slots[i];

            string skillID = "";
            if (slot != null && slot.SkillData != null)
            {
                skillID = slot.SkillData.SkillName;
            }

            SkillSlotData slotData = new SkillSlotData(i, skillID);
            saveData.progression.skillSlots.Add(slotData);

            Log($"슬롯 {i} 저장: {(string.IsNullOrEmpty(skillID) ? "Empty" : skillID)}");
        }

        Log($"스킬 슬롯 저장 완료: {slots.Count}개");
    }

    /// <summary>
    /// 메타데이터 업데이트 (플레이 시간, 최종 플레이 시각)
    /// </summary>
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
