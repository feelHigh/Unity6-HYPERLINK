using UnityEngine;

/// <summary>
/// 엠블렘 기반 아이템 드랍 테이블
/// </summary>
[CreateAssetMenu(fileName = "ItemDropTable", menuName = "Items/Drop Table")]
public class ItemDropTableData : ScriptableObject
{
    [Header("엠블렘 드랍 확률 (총합 100%)")]
    [SerializeField][Range(0f, 100f)] private float _standardEmblemChance = 60f;
    [SerializeField][Range(0f, 100f)] private float _silverEmblemChance = 30f;
    [SerializeField][Range(0f, 100f)] private float _goldEmblemChance = 9f;
    [SerializeField][Range(0f, 100f)] private float _diamondEmblemChance = 1f;

    [Header("드랍 타입")]
    [SerializeField] private DropType _dropType = DropType.Universal;

    public DropType DropType => _dropType;

    /// <summary>
    /// 확률에 따라 아이템 등급 결정
    /// </summary>
    public ItemQuality RollItemQuality()
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        cumulative += _standardEmblemChance;
        if (roll < cumulative)
            return ItemQuality.StandardEmblem;

        cumulative += _silverEmblemChance;
        if (roll < cumulative)
            return ItemQuality.SilverEmblem;

        cumulative += _goldEmblemChance;
        if (roll < cumulative)
            return ItemQuality.GoldEmblem;

        return ItemQuality.DiamondEmblem;
    }

    /// <summary>
    /// Inspector 유효성 검증
    /// </summary>
    private void OnValidate()
    {
        float total = _standardEmblemChance + _silverEmblemChance + _goldEmblemChance + _diamondEmblemChance;

        if (Mathf.Abs(total - 100f) > 0.01f)
        {
            Debug.LogWarning($"[{name}] 드랍 확률 합계가 100%가 아닙니다! 현재: {total}%", this);
        }
    }
}
