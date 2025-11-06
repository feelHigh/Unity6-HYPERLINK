using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnInfo
{
    [Tooltip("사용할 적 데이터")]
    public EnemyData enemyData;

    [Tooltip("사용할 적 프리팹")]
    public GameObject prefab;

    [Tooltip("스폰할 개수")]
    public int count = 1;
}

[CreateAssetMenu(fileName = "SpawnData", menuName = "Enemy/Spawn Data")]
public class EnemySpawnData : ScriptableObject
{
    [Header("----- 스폰 설정 -----")]
    public List<SpawnInfo> spawnList;

    [Header("----- 드랍 설정 -----")]
    public ItemDropTableData dropTable;
    [Range(0, 1)]
    public float dropChance = 0.5f;
}
