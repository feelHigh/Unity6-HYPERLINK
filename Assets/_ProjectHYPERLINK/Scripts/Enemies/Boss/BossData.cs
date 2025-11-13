using UnityEngine;

/// <summary>
/// 보스 데이터 ScriptableObject
/// 
/// 사운드 오버라이드:
/// - 각 보스마다 고유한 사운드를 설정 가능
/// - 비어있으면 GameSoundLibrary의 기본 사운드 사용
/// </summary>
[CreateAssetMenu(fileName = "BossData", menuName = "Enemy/BossData")]
public class BossData : ScriptableObject
{
    [Header("----- 기본 정보 -----")]
    [SerializeField] string _bossName;                  //보스 이름

    [Header("----- 기본 스탯 -----")]
    [SerializeField] float _maxHp = 5000f;              //최대 체력
    [SerializeField] float _atk = 50f;                  //공격력
    [SerializeField] float _moveSpeed = 4f;             //이동 속도
    [SerializeField] float _detectionRange = 30f;       //감지 범위

    [Header("----- 일반 공격 -----")]
    [SerializeField] float _basicAttackRange = 5f;      //일반 공격 범위
    [SerializeField] float _basicAttackCooldown = 2f;   //일반 공격 쿨타임
    [SerializeField] float _basicAttackDamageMultiplier = 1f;  //공격력 배수

    [Header("----- 패턴 전역 설정 -----")]
    [Tooltip("패턴 실행 후 다음 패턴까지의 대기 시간")]
    [SerializeField] float _globalPatternCooldown = 3f;

    [Header("----- 패턴 1: 내려찍기 -----")]
    [Tooltip("준비 시간 (초)")]
    [SerializeField] float _slamPrepareTime = 1.5f;
    [Tooltip("공격 범위 (반지름)")]
    [SerializeField] float _slamRadius = 8f;
    [Tooltip("공격력 배수")]
    [SerializeField] float _slamDamageMultiplier = 2f;
    [Tooltip("쿨타임 (초)")]
    [SerializeField] float _slamCooldown = 12f;

    [Header("----- 패턴 2: 돌진 -----")]
    [Tooltip("돌진 속도")]
    [SerializeField] float _chargeSpeed = 20f;
    [Tooltip("돌진 피해 배수")]
    [SerializeField] float _chargeDamageMultiplier = 1.5f;
    [Tooltip("보스 기절 시간")]
    [SerializeField] float _chargeStunDuration = 1f;
    [Tooltip("플레이어 밀려남 거리")]
    [SerializeField] float _chargeKnockbackPower = 5f;
    [Tooltip("플레이어 기절 시간")]
    [SerializeField] float _chargePlayerStunDuration = 1.5f;
    [Tooltip("쿨타임 (초)")]
    [SerializeField] float _chargeCooldown = 15f;

    [Header("----- 패턴 3: 3연속 베기 -----")]
    [Tooltip("포효 시간 (초)")]
    [SerializeField] float _comboRoarDuration = 1f;
    [Tooltip("공격 간격 (초)")]
    [SerializeField] float _comboAttackInterval = 0.8f;
    [Tooltip("전진 거리 (공격당)")]
    [SerializeField] float _comboMoveDistance = 2f;
    [Tooltip("공격 범위")]
    [SerializeField] float _comboAttackRange = 6f;
    [Tooltip("공격력 배수")]
    [SerializeField] float _comboDamageMultiplier = 1.2f;
    [Tooltip("쿨타임 (초)")]
    [SerializeField] float _comboCooldown = 18f;

    [Header("----- 패턴 4: 화염 브레스 -----")]
    [Tooltip("포효 시간 (초)")]
    [SerializeField] float _breathRoarDuration = 1f;
    [Tooltip("브레스 사거리")]
    [SerializeField] float _breathRange = 30f;
    [Tooltip("브레스 폭")]
    [SerializeField] float _breathWidth = 3f;
    [Tooltip("브레스 지속시간")]
    [SerializeField] float _breathDuration = 2f;
    [Tooltip("초당 틱 횟수")]
    [SerializeField] int _breathTicksPerSecond = 4;
    [Tooltip("틱당 공격력 배수")]
    [SerializeField] float _breathDamageMultiplier = 0.3f;
    [Tooltip("쿨타임 (초)")]
    [SerializeField] float _breathCooldown = 20f;

    [Header("----- 보상 -----")]
    [SerializeField] int _rewardExp = 1000;
    [SerializeField] int _rewardGold = 500;

    [Header("----- 이펙트 -----")]
    [Tooltip("기본 공격 이펙트")]
    [SerializeField] private GameObject _basicAattackEffect;
    [Tooltip("포효 이펙트")]
    [SerializeField] private GameObject _roarEffect;
    [Tooltip("패턴 1: 내려찍기 이펙트")]
    [SerializeField] private GameObject _slamEffect;
    [Tooltip("패턴 2: 돌진 이펙트")]
    [SerializeField] private GameObject _chargeEffect;
    [Tooltip("패턴 3: 3연타 이펙트")]
    [SerializeField] private GameObject _comboEffect;
    [Tooltip("패턴 4: 브레스 이펙트")]
    [SerializeField] private GameObject _breathEffect;

    [Header("----- 사운드 오버라이드 (선택사항) -----")]
    [Tooltip("보스 스폰 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _spawnSound;

    [Tooltip("보스 기본 공격 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _basicAttackSound;

    [Tooltip("보스 특수 공격 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _specialAttackSound;

    [Tooltip("보스 사망 사운드 (비어있으면 GameSoundLibrary 기본값 사용)")]
    [SerializeField] private AudioClip _deathSound;

    [Tooltip("패턴 1: 내려찍기 사운드 (선택사항)")]
    [SerializeField] private AudioClip _slamSound;

    [Tooltip("패턴 2: 돌진 사운드 (선택사항)")]
    [SerializeField] private AudioClip _chargeSound;

    [Tooltip("패턴 3: 3연타 사운드 (선택사항)")]
    [SerializeField] private AudioClip _comboSound;

    [Tooltip("패턴 4: 브레스 사운드 (선택사항)")]
    [SerializeField] private AudioClip _breathSound;

    //프로퍼티
    public string BossName => _bossName;
    public float MaxHp => _maxHp;
    public float Atk => _atk;
    public float MoveSpeed => _moveSpeed;
    public float DetectionRange => _detectionRange;

    //일반 공격
    public float BasicAttackRange => _basicAttackRange;
    public float BasicAttackCooldown => _basicAttackCooldown;
    public float BasicAttackDamageMultiplier => _basicAttackDamageMultiplier;

    //패턴 전역 설정
    public float GlobalPatternCooldown => _globalPatternCooldown;

    //패턴 1: 내려찍기
    public float SlamPrepareTime => _slamPrepareTime;
    public float SlamRadius => _slamRadius;
    public float SlamDamageMultiplier => _slamDamageMultiplier;
    public float SlamCooldown => _slamCooldown;

    //패턴 2: 돌진
    public float ChargeSpeed => _chargeSpeed;
    public float ChargeDamageMultiplier => _chargeDamageMultiplier;
    public float ChargeStunDuration => _chargeStunDuration;
    public float ChargeKnockbackPower => _chargeKnockbackPower;
    public float ChargePlayerStunDuration => _chargePlayerStunDuration;
    public float ChargeCooldown => _chargeCooldown;

    //패턴 3: 3연타
    public float ComboRoarDuration => _comboRoarDuration;
    public float ComboAttackInterval => _comboAttackInterval;
    public float ComboMoveDistance => _comboMoveDistance;
    public float ComboAttackRange => _comboAttackRange;
    public float ComboDamageMultiplier => _comboDamageMultiplier;
    public float ComboCooldown => _comboCooldown;

    //패턴 4: 화염 브레스
    public float BreathRoarDuration => _breathRoarDuration;
    public float BreathRange => _breathRange;
    public float BreathWidth => _breathWidth;
    public float BreathDuration => _breathDuration;
    public int BreathTicksPerSecond => _breathTicksPerSecond;
    public float BreathDamageMultiplier => _breathDamageMultiplier;
    public float BreathCooldown => _breathCooldown;

    //보상
    public int RewardExp => _rewardExp;
    public int RewardGold => _rewardGold;

    //이펙트
    public GameObject BasicAttackEffect => _basicAattackEffect;
    public GameObject RoarEffect => _roarEffect;
    public GameObject SlamEffect => _slamEffect;
    public GameObject ChargeEffect => _chargeEffect;
    public GameObject ComboEffect => _comboEffect;
    public GameObject BreathEffect => _breathEffect;

    //사운드 오버라이드 프로퍼티
    public AudioClip SpawnSound => _spawnSound;
    public AudioClip BasicAttackSound => _basicAttackSound;
    public AudioClip SpecialAttackSound => _specialAttackSound;
    public AudioClip DeathSound => _deathSound;
    public AudioClip SlamSound => _slamSound;
    public AudioClip ChargeSound => _chargeSound;
    public AudioClip ComboSound => _comboSound;
    public AudioClip BreathSound => _breathSound;
}
