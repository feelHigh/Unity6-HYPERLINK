using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnInfo
{
    public EnemyData enemyData;
    public int count;
}

[CreateAssetMenu(fileName = "SpawnData", menuName = "Enemy/Spawn Data")]
public class EnemySpawnData : ScriptableObject
{
    public List<SpawnInfo> spawnList;

    [Header("----- 드랍 설정 -----")]
    public ItemDropTableData dropTable;
    [Range(0, 1)]
    public float dropChance = 0.5f;
}
