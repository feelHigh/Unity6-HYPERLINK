using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("----- 기본 정보 -----")]
    [SerializeField] string _name;                  //이름
    [SerializeField] EnemyType _enemyType;          //타입

    [Header("----- 프리팹 -----")]
    [SerializeField] List<GameObject> _prefabs = new List<GameObject>();  //프리팹

    [Header("----- 스탯 -----")]
    [SerializeField] float _maxHp;                  //최대 체력
    [SerializeField] float _moveSpeed;              //이동 속도
    [SerializeField] float _atk;                    //공격력

    [Header("----- 행동 패턴 -----")]
    [SerializeField] float _detectionRange;         //탐지 사거리
    [SerializeField] float _chaseDistance;          //추격 유지 사거리

    [Header("----- 전투 -----")]
    [SerializeField] BasicAttackType _basicAttackType;  //일반 공격 타입 (원거리, 근거리)
    [SerializeField] float _attackRange;            //공격 사거리
    [SerializeField] float _attackCoolTime;         //공격 쿨타임
    [SerializeField] int _rewardExp;                //경험치 보상
    [SerializeField] int _rewardGold;               //골드 보상

    [Header("----- 원거리 기본 공격 -----")]
    [SerializeField] GameObject _projectilePrefab;  //발사체 프리팹
    [SerializeField] float _projrctileSpeed = 12f;  //발사체 속도

    [Header("----- 에픽 몬스터 스탯 배수 -----")]
    [SerializeField] bool _canBeEpic;
    [SerializeField] float _epicHpMultiplier;   //체력 배수
    [SerializeField] float _epicAtkMultiplier;  //공격력 배수
    [SerializeField] int _epicExpMultiplier;    //보상 경험치 배수
    [SerializeField] int _epicGoldMultiplier;   //보상 골드 배수

    // ----- 프로퍼티 ------ //
    public string Name => _name;
    public EnemyType EnemyType => _enemyType;
    public float MaxHp => _maxHp;
    public float MoveSpeed => _moveSpeed;
    public float Atk => _atk;
    public float DetectionRange => _detectionRange;
    public float ChaseDistance => _chaseDistance;
    public BasicAttackType BasicAttackType => _basicAttackType;
    public float AttackRange => _attackRange;
    public float AttackCoolTime => _attackCoolTime;
    public int RewardExp => _rewardExp;
    public int RewardGold => _rewardGold;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => _projrctileSpeed;

    // - 에픽 프로퍼티 - //
    public bool CanBeEpic => _canBeEpic;
    public float EpicHpMultiplier => _epicHpMultiplier;
    public float EpicAtkMultiplier => _epicAtkMultiplier;
    public int EpicExpMultiplier => _epicExpMultiplier;
    public int EpicGoldMultiplier => _epicGoldMultiplier;

    /// <summary>
    /// 프리팹 리스트에서 랜덤하게 하나 반환
    /// </summary>
    public GameObject Prefab
    {
        get
        {
            if (_prefabs == null || _prefabs.Count == 0)
            {
                Debug.LogError($"[EnemyData] {_name}: 프리팹이 설정되지 않았습니다.");
                return null;
            }

            //프리팹이 1개면 그냥 반환
            if (_prefabs.Count == 1)
            {
                return _prefabs[0];
            }

            //여러 개면 랜덤 선택
            int ran = Random.Range(0, _prefabs.Count);
            return _prefabs[ran];
        }
    }

    /// <summary>
    /// 프리팹 리스트에서 특정 인덱스의 프리팹 반환
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject GetPrefab(int index)
    {
        if (_prefabs == null || index < 0 || index >= _prefabs.Count)
        {
            Debug.LogError($"[EnemyData] 잘못된 인덱스: {index}");
            return null;
        }

        return _prefabs[index];
    }

    /// <summary>
    /// 프리팹 개수
    /// </summary>
    public int PrefabCount => _prefabs?.Count ?? 0;
}
