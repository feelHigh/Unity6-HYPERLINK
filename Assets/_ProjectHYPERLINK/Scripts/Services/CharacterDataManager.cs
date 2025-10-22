using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 데이터 중앙 관리 시스템
/// 
/// 기능:
/// - Cloud Save 연동
/// - Experience, Character, Equipment, Inventory 데이터 관리
/// - 자동 저장 (5분마다)
/// - 플레이 시간 추적
/// </summary>
public class CharacterDataManager : MonoBehaviour
{
    private static CharacterDataManager _instance;
    public static CharacterDataManager Instance => _instance;

    [Header("자동 저장 설정")]
    [SerializeField] private float _autoSaveInterval = 300f;

    private float _autoSaveTimer = 0f;
    private float _sessionStartTime;
    private long _totalPlayTimeSeconds;

    private CharacterSaveData _currentCharacterData;

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

    public void InitializeSystemReferences()
    {
        _playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        _experienceManager = FindFirstObjectByType<ExperienceManager>();
        _equipmentManager = FindFirstObjectByType<EquipmentManager>();

        if (_playerCharacter == null)
            Debug.LogError("[CharacterDataManager] PlayerCharacter를 찾을 수 없습니다");

        if (_experienceManager == null)
            Debug.LogError("[CharacterDataManager] ExperienceManager를 찾을 수 없습니다");

        if (_equipmentManager == null)
            Debug.LogError("[CharacterDataManager] EquipmentManager를 찾을 수 없습니다");
    }

    /// <summary>
    /// 캐릭터 데이터 로드
    /// </summary>
    public async Task<bool> LoadCharacterData()
    {
        Debug.Log("[CharacterDataManager] 캐릭터 데이터 로드 시작");

        _currentCharacterData = await CloudSaveManager.Instance.LoadCharacterDataAsync();

        if (_currentCharacterData == null)
        {
            Debug.LogError("[CharacterDataManager] 캐릭터 데이터 로드 실패");
            return false;
        }

        if (_playerCharacter == null || _experienceManager == null)
        {
            InitializeSystemReferences();
        }

        ApplyDataToSystems(_currentCharacterData);

        _totalPlayTimeSeconds = _currentCharacterData.metadata.playTimeSeconds;
        _sessionStartTime = Time.time;

        Debug.Log($"[CharacterDataManager] 캐릭터 로드 완료: {_currentCharacterData.character.characterName}, 레벨 {_currentCharacterData.character.level}");
        return true;
    }

    /// <summary>
    /// 로드된 데이터를 각 시스템에 적용
    /// 순서: Experience → Character → Equipment → Inventory
    /// </summary>
    private void ApplyDataToSystems(CharacterSaveData data)
    {
        if (_experienceManager != null)
        {
            _experienceManager.LoadFromSaveData(data);
        }

        if (_playerCharacter != null)
        {
            _playerCharacter.LoadFromSaveData(data);
        }

        if (_equipmentManager != null)
        {
            _equipmentManager.LoadFromSaveData(data);
        }

        // 인벤토리 로드
        LoadInventoryData(data);
    }

    /// <summary>
    /// 인벤토리 데이터 로드
    /// </summary>
    private void LoadInventoryData(CharacterSaveData data)
    {
        if (ItemInventory.Instance == null)
        {
            Debug.LogWarning("[CharacterDataManager] ItemInventory 인스턴스를 찾을 수 없습니다");
            return;
        }

        if (data.inventory == null || data.inventory.items == null)
        {
            Debug.Log("[CharacterDataManager] 저장된 인벤토리 데이터 없음");
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
                Debug.LogWarning($"[CharacterDataManager] 아이템을 찾을 수 없음: {item.itemId}");
                failCount++;
            }
        }

        Debug.Log($"[CharacterDataManager] 인벤토리 로드 완료: 성공 {successCount}개, 실패 {failCount}개");
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

        Debug.LogWarning($"[CharacterDataManager] EquipmentManager를 찾을 수 없음");
        return null;
    }

    /// <summary>
    /// 캐릭터 데이터 수집 및 저장
    /// </summary>
    public async Task<bool> CollectAndSaveData()
    {
        if (!IsDataLoaded)
        {
            Debug.LogWarning("[CharacterDataManager] 저장할 데이터가 없습니다");
            return false;
        }

        Debug.Log("[CharacterDataManager] 캐릭터 데이터 수집 및 저장 시작");

        UpdateMetadata();
        CollectDataFromSystems();

        bool success = await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);

        if (success)
        {
            Debug.Log("[CharacterDataManager] 캐릭터 데이터 저장 완료");
        }
        else
        {
            Debug.LogError("[CharacterDataManager] 캐릭터 데이터 저장 실패");
        }

        return success;
    }

    /// <summary>
    /// 각 시스템에서 현재 상태 수집
    /// Experience, Character, Equipment, Inventory 모두 수집
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

        // 위치 정보
        if (_playerCharacter != null)
        {
            _currentCharacterData.position.scene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            Transform playerTransform = _playerCharacter.transform;
            _currentCharacterData.position.x = playerTransform.position.x;
            _currentCharacterData.position.y = playerTransform.position.y;
            _currentCharacterData.position.z = playerTransform.position.z;

            Debug.Log($"[CharacterDataManager] 위치 저장: 씬={_currentCharacterData.position.scene}, 좌표=({_currentCharacterData.position.x:F2}, {_currentCharacterData.position.y:F2}, {_currentCharacterData.position.z:F2})");
        }
    }

    /// <summary>
    /// 인벤토리 데이터 저장
    /// </summary>
    private void SaveInventoryData(CharacterSaveData saveData)
    {
        if (ItemInventory.Instance == null)
        {
            Debug.LogWarning("[CharacterDataManager] ItemInventory 인스턴스를 찾을 수 없습니다");
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

        Debug.Log($"[CharacterDataManager] 인벤토리 저장: {items.Count}개 아이템");
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
        Debug.Log("[CharacterDataManager] 자동 저장 실행");
        await CollectAndSaveData();
    }

    private async void OnApplicationQuit()
    {
        if (IsDataLoaded)
        {
            Debug.Log("[CharacterDataManager] 게임 종료 - 최종 저장 실행");
            await CollectAndSaveData();
        }
    }

    public string GetCharacterName()
    {
        return _currentCharacterData?.character.characterName ?? "Unknown";
    }
}
