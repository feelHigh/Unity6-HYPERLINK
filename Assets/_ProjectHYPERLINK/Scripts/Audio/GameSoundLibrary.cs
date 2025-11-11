using UnityEngine;

/// <summary>
/// 게임 사운드 라이브러리
/// 
/// 역할:
/// - 모든 게임 사운드 에셋을 중앙에서 관리
/// - 플레이어/적 전투 사운드
/// - UI 사운드
/// - 환경 사운드
/// 
/// 사용법:
/// 1. Project 창에서 우클릭 > Create > Audio > Game Sound Library
/// 2. Inspector에서 모든 사운드 에셋 할당
/// 3. AudioManager가 이 ScriptableObject를 참조하여 사운드 재생
/// </summary>
[CreateAssetMenu(fileName = "GameSoundLibrary", menuName = "Audio/Game Sound Library")]
public class GameSoundLibrary : ScriptableObject
{
    #region 플레이어 사운드

    [Header("=== 플레이어 사운드 ===")]

    [Header("플레이어 기본 공격")]
    [Tooltip("플레이어 기본 공격 사운드")]
    [SerializeField] private AudioClip _playerBasicAttack;

    [Header("플레이어 피격")]
    [Tooltip("플레이어가 데미지를 받을 때 사운드")]
    [SerializeField] private AudioClip _playerHit;

    [Tooltip("플레이어가 사망할 때 사운드")]
    [SerializeField] private AudioClip _playerDeath;

    [Header("플레이어 스킬 사운드")]
    [Tooltip("스킬별 사운드는 SkillData에서 개별 설정 가능")]
    [SerializeField] private AudioClip _playerSkillCast; // 기본 스킬 캐스팅 사운드

    #endregion

    #region 적 사운드

    [Header("=== 적 사운드 ===")]

    [Header("일반 적 사운드")]
    [Tooltip("일반 적 기본 공격 (근접)")]
    [SerializeField] private AudioClip _enemyMeleeAttack;

    [Tooltip("일반 적 기본 공격 (원거리)")]
    [SerializeField] private AudioClip _enemyRangedAttack;

    [Tooltip("적이 데미지를 받을 때")]
    [SerializeField] private AudioClip _enemyHit;

    [Tooltip("적이 사망할 때")]
    [SerializeField] private AudioClip _enemyDeath;

    [Header("에픽 몬스터 사운드")]
    [Tooltip("에픽 몬스터 스폰 사운드")]
    [SerializeField] private AudioClip _epicSpawn;

    [Tooltip("에픽 몬스터 특수 공격")]
    [SerializeField] private AudioClip _epicSpecialAttack;

    [Header("보스 사운드")]
    [Tooltip("보스 스폰 사운드")]
    [SerializeField] private AudioClip _bossSpawn;

    [Tooltip("보스 기본 공격")]
    [SerializeField] private AudioClip _bossAttack;

    [Tooltip("보스 특수 공격")]
    [SerializeField] private AudioClip _bossSpecialAttack;

    [Tooltip("보스 사망")]
    [SerializeField] private AudioClip _bossDeath;

    #endregion

    #region UI 사운드

    [Header("=== UI 사운드 ===")]

    [Tooltip("버튼 클릭")]
    [SerializeField] private AudioClip _uiClick;

    [Tooltip("버튼 호버")]
    [SerializeField] private AudioClip _uiHover;

    [Tooltip("아이템 획득")]
    [SerializeField] private AudioClip _itemPickup;

    [Tooltip("아이템 장착")]
    [SerializeField] private AudioClip _itemEquip;

    [Tooltip("레벨업")]
    [SerializeField] private AudioClip _levelUp;

    [Tooltip("스킬 언락")]
    [SerializeField] private AudioClip _skillUnlock;

    #endregion

    #region 환경 사운드

    [Header("=== 환경 사운드 ===")]

    [Tooltip("발소리 (옵션)")]
    [SerializeField] private AudioClip _footstep;

    [Tooltip("문 열기")]
    [SerializeField] private AudioClip _doorOpen;

    [Tooltip("포탈 사용")]
    [SerializeField] private AudioClip _portalUse;

    #endregion

    #region 프로퍼티

    // 플레이어 사운드
    public AudioClip PlayerBasicAttack => _playerBasicAttack;
    public AudioClip PlayerHit => _playerHit;
    public AudioClip PlayerDeath => _playerDeath;
    public AudioClip PlayerSkillCast => _playerSkillCast;

    // 적 사운드
    public AudioClip EnemyMeleeAttack => _enemyMeleeAttack;
    public AudioClip EnemyRangedAttack => _enemyRangedAttack;
    public AudioClip EnemyHit => _enemyHit;
    public AudioClip EnemyDeath => _enemyDeath;
    public AudioClip EpicSpawn => _epicSpawn;
    public AudioClip EpicSpecialAttack => _epicSpecialAttack;
    public AudioClip BossSpawn => _bossSpawn;
    public AudioClip BossAttack => _bossAttack;
    public AudioClip BossSpecialAttack => _bossSpecialAttack;
    public AudioClip BossDeath => _bossDeath;

    // UI 사운드
    public AudioClip UIClick => _uiClick;
    public AudioClip UIHover => _uiHover;
    public AudioClip ItemPickup => _itemPickup;
    public AudioClip ItemEquip => _itemEquip;
    public AudioClip LevelUp => _levelUp;
    public AudioClip SkillUnlock => _skillUnlock;

    // 환경 사운드
    public AudioClip Footstep => _footstep;
    public AudioClip DoorOpen => _doorOpen;
    public AudioClip PortalUse => _portalUse;

    #endregion
}
