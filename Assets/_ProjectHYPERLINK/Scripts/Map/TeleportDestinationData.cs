using UnityEngine;

/// <summary>
/// 텔레포트 목적지 데이터
/// 
/// 기능:
/// - 씬 이름, 스폰 포인트, 표시 정보
/// - 퀘스트 잠금 조건
/// - 목적지 설명 및 아이콘
/// </summary>
[CreateAssetMenu(fileName = "New Teleport Destination", menuName = "ScriptableObject/Teleport Destination")]
public class TeleportDestinationData : ScriptableObject
{
    [Header("목적지 정보")]
    [Tooltip("목적지가 속한 씬 이름")]
    [SerializeField] private string _sceneName = "";

    [Tooltip("씬 내 스폰 포인트 이름")]
    [SerializeField] private string _spawnPointName = "";

    [Tooltip("UI에 표시될 목적지 이름")]
    [SerializeField] private string _displayName = "";

    [Tooltip("목적지 설명")]
    [TextArea(2, 4)]
    [SerializeField] private string _description = "";

    [Header("잠금 조건")]
    [Tooltip("해제에 필요한 퀘스트 ID (비어있으면 항상 사용 가능)")]
    [SerializeField] private string _requiredQuestID = "";

    [Header("비주얼")]
    [Tooltip("목적지 아이콘 (선택)")]
    [SerializeField] private Sprite _icon;

    // Properties
    public string SceneName => _sceneName;
    public string SpawnPointName => _spawnPointName;
    public string DisplayName => _displayName;
    public string Description => _description;
    public string RequiredQuestID => _requiredQuestID;
    public Sprite Icon => _icon;

    /// <summary>
    /// 유일 식별자 (씬명_스폰포인트명)
    /// </summary>
    public string UniqueID => $"{_sceneName}_{_spawnPointName}";

    /// <summary>
    /// 잠금 조건 충족 여부
    /// </summary>
    public bool IsUnlocked()
    {
        if (string.IsNullOrEmpty(_requiredQuestID))
            return true;

        if (QuestManager.Instance == null)
            return true;

        return QuestManager.Instance.IsQuestCompleted(_requiredQuestID);
    }

    /// <summary>
    /// 유효성 검사
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(_sceneName) && !string.IsNullOrEmpty(_spawnPointName);
    }
}
