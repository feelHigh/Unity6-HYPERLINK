using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 데이터 ScriptableObject
/// 
/// 사운드 오버라이드:
/// - 각 EnemyData마다 고유한 사운드를 설정 가능
/// - 비어있으면 GameSoundLibrary의 기본 사운드 사용
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("----- 기본 정보 -----")]
    [SerializeField] string _name;                  //데이터 이름

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

    [Header("----- 히트 VFX -----")]
    [Tooltip("기본 공격이 플레이어에게 맞았을 때 표시할 VFX")]
    [SerializeField] GameObject _baseAttackHitVfx;

    [Header("----- 에픽 몬스터 스탯 배수 -----")]
    [SerializeField] bool _canBeEpic;
    [SerializeField] float _epicHpMultiplier;   //체력 배수
    [SerializeField] float _epicAtkMultiplier;  //공격력 배수
    [SerializeField] int _epicExpMultiplier;    //보상 경험치 배수
    [SerializeField] int _epicGoldMultiplier;   //보상 골드 배수

    [Header("----- 사운드 오버라이드 (선택사항) -----")]
    [Tooltip("근접 공격 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _meleeAttackSound;

    [Tooltip("원거리 공격 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _rangedAttackSound;

    [Tooltip("피격 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _hitSound;

    [Tooltip("사망 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _deathSound;

    [Tooltip("에픽 스폰 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _epicSpawnSound;

    // ----- 프로퍼티 ------ //
    public string Name => _name;
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
    public GameObject BaseAttackHitVfx => _baseAttackHitVfx;

    // - 에픽 프로퍼티 - //
    public bool CanBeEpic => _canBeEpic;
    public float EpicHpMultiplier => _epicHpMultiplier;
    public float EpicAtkMultiplier => _epicAtkMultiplier;
    public int EpicExpMultiplier => _epicExpMultiplier;
    public int EpicGoldMultiplier => _epicGoldMultiplier;

    // - 사운드 오버라이드 프로퍼티 - //
    public AudioClip MeleeAttackSound => _meleeAttackSound;
    public AudioClip RangedAttackSound => _rangedAttackSound;
    public AudioClip HitSound => _hitSound;
    public AudioClip DeathSound => _deathSound;
    public AudioClip EpicSpawnSound => _epicSpawnSound;
}
