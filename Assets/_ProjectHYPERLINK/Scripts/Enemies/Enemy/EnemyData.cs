using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("----- 기본 정보 -----")]
    [SerializeField] string _name;              //이름
    [SerializeField] GameObject _prefab;        //프리팹

    [Header("----- 스탯 -----")]
    [SerializeField] float _maxHp;              //최대 체력
    [SerializeField] float _moveSpeed;          //이동 속도
    [SerializeField] float _atk;                //공격력

    [Header("----- 행동 패턴 -----")]
    [SerializeField] float _detectionRange;     //탐지 사거리
    [SerializeField] float _chaseDistance;      //추격 유지 사거리
    [SerializeField] float _patrolRadius;       //배회 사거리
    [SerializeField] float _patrolWaitTime;     //배회 쿨타임

    [Header("----- 전투 -----")]
    [SerializeField] float _attackRange;        //공격 사거리
    [SerializeField] float _attackCoolTime;     //공격 쿨타임
    [SerializeField] int _rewardExp;          //경험치 보상
    [SerializeField] int _rewardGold;           //골드 보상

    [Header("----- 에픽 몬스터 스탯 배수 -----")]
    [SerializeField] float _epicHpMultiplier;   //체력 배수
    [SerializeField] float _epicAtkMultiplier;  //공격력 배수
    [SerializeField] int _epicExpMultirlier;  //보상 경험치 배수
    [SerializeField] int _epicGoldMultirlier;   //보상 골드 배수

    // ----- 프로퍼티 ------ //
    public string Name => _name;
    public GameObject Prefab => _prefab;
    public float MaxHp => _maxHp;
    public float MoveSpeed => _moveSpeed;
    public float Atk => _atk;
    public float DetectionRange => _detectionRange;
    public float ChaseDistance => _chaseDistance;
    public float PatrolRadius => _patrolRadius;
    public float PatrolWaitTime => _patrolWaitTime;
    public float AttackRange => _attackRange;
    public float AttackCoolTime => _attackCoolTime;
    public int RewardExp => _rewardExp;
    public int RewardGold => _rewardGold;
    
    // - 에픽 프로퍼티 - //
    public float EpicHpMultiplier => _epicHpMultiplier;
    public float EpicAtkMultiplier => _epicAtkMultiplier;
    public int EpicExpMultiplier => _epicExpMultirlier;
    public int EpicGoldMultiplier => _epicGoldMultirlier;
}
