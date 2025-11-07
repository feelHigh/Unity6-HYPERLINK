using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// 통합 적 컨트롤러
/// - 일반, 에픽, 보스 몬스터
/// - 골드 보상 지급
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("----- 적 타입 -----")]
    [SerializeField] EnemyType _enemyType;

    [Header("----- 데이터 -----")]
    [SerializeField] EnemyData _enemyData;  //일반/에픽 데이터 (런타임 불러오기)
    [SerializeField] BossData _bossData;    //보스 데이터 (미리 참조)

    [Header("----- 컴포넌트 -----")]
    [SerializeField] Collider _collider;    //콜라이더

    [Header("----- 드랍 설정 -----")]
    ItemDropTableData _dropTable;           //아이템 드랍 테이블
    float _dropChance;                      //드랍 확률

    [Header("----- 현재 속한 그룹 (일반/에픽) -----")]
    [SerializeField] EnemyGroup _group;     //현재 속한 그룹

    [Header("----- 에픽 몬스터 설정 -----")]
    [SerializeField] SpecialAttackBase _specialAttack;  //특수 공격 데이터
    [SerializeField] List<GameObject> _epicOrbs = new List<GameObject>();   //에픽 몬스터 오브 이펙트 리스트

    // 이벤트 //
    public event Action OnInitialized;      //초기화 완료 이벤트
    public event Action OnHit;              //피격 이벤트
    public event Action OnDie;              //죽음 이벤트

    // 현재 상태 스탯 //
    float _maxHp;           //최대 체력
    float _curHp;           //현재 체력
    float _atk;             //공격력
    float _moveSpeed;
    bool _isDead = false;

    // 보상 //
    int _expReward;         //보상 경험치
    int _goldReward;        //보상 골드


    // 프로퍼티 //
    public EnemyData EnemyData => _enemyData;
    public BossData BossData => _bossData;
    public EnemyGroup Group => _group;
    public bool IsEpic => _enemyType == EnemyType.Epic;
    public bool IsBoss => _enemyType == EnemyType.Boss;
    public SpecialAttackBase SpecialAttack => _specialAttack;
    public float Atk => _atk;
    public float MoveSpeed => _moveSpeed;
    public float MaxHp => _maxHp;
    public float CurHp => _curHp;
    public bool IsDead => _isDead;

    private void Start()
    {
        if (_enemyType == EnemyType.Boss)
        {
            InitializeBoss();
        }
    }

    /// <summary>
    /// 일반 적을 초기화하는 함수
    /// </summary>
    public void InitializeNormal(EnemyData data, ItemDropTableData dropTable, float dropChance)
    {
        _enemyData = data;
        _enemyType = _enemyData.EnemyType;
        _group = transform.parent?.GetComponent<EnemyGroup>();

        //드랍 설정
        _dropTable = dropTable;
        _dropChance = dropChance;

        //스탯 초기화
        _maxHp = _enemyData.MaxHp;
        _curHp = _maxHp;
        _atk = _enemyData.Atk;
        _moveSpeed = _enemyData.MoveSpeed;
        _expReward = _enemyData.RewardExp;
        _goldReward = _enemyData.RewardGold;

        _isDead = false;

        //초기화 완료 이벤트 발행
        OnInitialized?.Invoke();
    }

    /// <summary>
    /// 에픽 적을 초기화하는 함수
    /// </summary>
    public void InitializeEpic(EnemyData data, SpecialAttackBase specialAttack, ItemDropTableData dropTable, float dropChance)
    {
        _enemyData = data;
        _enemyType = _enemyData.EnemyType;
        _group = transform.parent?.GetComponent<EnemyGroup>();
        _specialAttack = specialAttack;

        //드랍 설정
        _dropTable = dropTable;
        _dropChance = dropChance;

        //스탯 초기화 (에픽 배수 적용)
        _maxHp = _enemyData.MaxHp * _enemyData.EpicHpMultiplier;
        _curHp = _maxHp;
        _atk = _enemyData.Atk * _enemyData.EpicAtkMultiplier;
        _moveSpeed = _enemyData.MoveSpeed;
        _expReward = _enemyData.RewardExp * _enemyData.EpicExpMultiplier;
        _goldReward = _enemyData.RewardGold * _enemyData.EpicGoldMultiplier;

        _isDead = false;

        //에픽 크기 변경
        transform.localScale *= 1.2f;
        Debug.Log($"{name}이(가) {_specialAttack.Type}타입 에픽 몬스터로 등장!");

        //오브 생성
        CreateEpicOrbs();

        OnInitialized?.Invoke();
    }

    /// <summary>
    /// 보스를 초기화하는 함수
    /// </summary>
    private void InitializeBoss(/*ItemDropTableData dropTable, float dropChance*/)
    {
        if (_bossData == null)
        {
            Debug.LogError($"[Boss] {name}: BossData가 없습니다.");
            return;
        }

        _enemyType = EnemyType.Boss;

        /*
        //드랍 설정
        _dropTable = dropTable;
        _dropChance = dropChance;
        */

        //스탯 초기화
        _maxHp = _bossData.MaxHp;
        _curHp = _maxHp;
        _atk = _bossData.Atk;
        _moveSpeed = _bossData.MoveSpeed;
        _expReward = _bossData.RewardExp;
        _goldReward = _bossData.RewardGold;

        _isDead = false;

        //초기화 완료 이벤트 발행
        OnInitialized?.Invoke();
    }

    /// <summary>
    /// 데미지 받는 함수 (공통)
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(float damage)
    {
        //현재 상태가 죽음 상태면 리턴
        if (_curHp <= 0) return;

        //현재 체력을 데미지 만큼 감소
        _curHp -= damage;
        _curHp = Mathf.Max(_curHp, 0);

        //피격 애니메이션 이벤트 발행
        OnHit?.Invoke();

        //현재 체력이 0보다 작거나 같으면
        if (_curHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 죽음 처리 함수 (공통)
    /// </summary>
    void Die()
    {
        if (_isDead) return;

        _isDead = true;

        //콜라이더 비활성화
        if (_collider != null)
        {
            _collider.enabled = false;
        }
        else
        {
            _collider = GetComponent<Collider>();
            _collider.enabled = false;
        }

        //에픽 몬스터라면 오브 이펙트 파괴
        if (_enemyType == EnemyType.Epic)
        {
            DestroyEpicOrbs();
        }

        //보상 지급
        GiveRewards();

        //아이템 드랍
        TryDropItem();

        //퀘스트 시스템에 적 처치 알림
        NotifyQuestSystem();

        //죽음 이벤트 발행
        OnDie?.Invoke();
    }

    /// <summary>
    /// 퀘스트 시스템에 적 처치 알림
    /// </summary>
    void NotifyQuestSystem()
    {
        if (QuestManager.Instance != null)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            QuestManager.Instance.OnEnemyKilled(_enemyType, currentScene);
        }
    }

    /// <summary>
    /// 에픽 몬스터 오브 이펙트 생성
    /// </summary>
    void CreateEpicOrbs()
    {
        if (_specialAttack == null || _specialAttack.EpicEffect == null) return;

        int orbCount = 2;
        float radius = 1.5f;

        for (int i = 0; i < orbCount; i++)
        {
            float angle = i * Mathf.PI;

            GameObject orbGO = Instantiate(_specialAttack.EpicEffect, transform);
            orbGO.transform.localRotation = Quaternion.identity;
            _epicOrbs.Add(orbGO);

            EffectOrbit orb = orbGO.GetComponent<EffectOrbit>();

            if (orb == null)
            {
                orbGO.AddComponent<EffectOrbit>();
            }

            orb.Initialize(transform, 90f, radius, angle * Mathf.Rad2Deg);
        }
    }

    /// <summary>
    /// 에픽 몬스터 오브 이펙트 파괴
    /// </summary>
    void DestroyEpicOrbs()
    {
        foreach (GameObject orb in _epicOrbs)
        {
            if (orb != null)
            {
                Destroy(orb);
            }
        }
        _epicOrbs.Clear();
    }

    /// <summary>
    /// 보상 지급 함수 (공통)
    /// 골드 보상 활성화
    /// </summary>
    void GiveRewards()
    {
        // 경험치
        ExperienceManager playerExpManager = FindFirstObjectByType<ExperienceManager>();
        if (playerExpManager != null)
        {
            playerExpManager.GainExperience(_expReward);
        }

        // 골드
        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null && _goldReward > 0)
        {
            player.AddGold(_goldReward);
            Debug.Log($"[EnemyController] {name} 처치 - 골드 보상: {_goldReward}");
        }
    }

    /// <summary>
    /// 아이템 드랍 함수 (공통)
    /// </summary>
    void TryDropItem()
    {
        //ItemSpawner와 드랍 테이블이 설정되어 있는지 확인
        if (ItemSpawner.Instance != null && _dropTable != null)
        {
            //확률 체크 (0~1 사이 랜덤 값)
            if (UnityEngine.Random.Range(0f, 1f) < _dropChance)
            {
                //적의 현재 위치에 아이템 드랍
                ItemSpawner.Instance.SpawnItem(transform.position, _dropTable);
                Debug.Log($"아이템 드랍!");
            }
        }
    }

    /// <summary>
    /// Gizmo를 사용한 체력바 시각화
    /// 
    /// Scene 뷰에서 실시간으로 체력 표시:
    /// - 적 머리 위 2.5유닛 위치에 체력바
    /// - 배경: 빨간색 (최대 체력)
    /// - 전경: 초록색 (현재 체력)
    /// - 체력이 줄어들면 초록색 바가 짧아짐
    /// 
    /// 시각화 구조:
    /// - 배경 바: 1유닛 너비 × 0.1유닛 높이 (빨간색)
    /// - 전경 바: (현재체력/최대체력) 비율로 너비 조절 (초록색)
    /// - 전경 바는 약간 더 두껍게 (0.12유닛) 표시
    /// 
    /// 작동 조건:
    /// - Application.isPlaying = true (게임 실행 중에만)
    /// - 에디터 전용 (빌드에는 미포함)
    /// 
    /// 사용 방법:
    /// - Play 모드 진입
    /// - Scene 뷰에서 적 선택
    /// - 머리 위 체력바 확인
    /// - 데미지 받을 때 실시간으로 감소하는 것 확인
    /// </summary>
    private void OnDrawGizmos()
    {
        // 게임 실행 중에만 그리기
        if (Application.isPlaying)
        {
            // 체력바 위치 (적 머리 위 2.5유닛)
            Vector3 healthBarPosition = transform.position + Vector3.up * 2.5f;

            // 체력 비율 계산 (0~1)
            float healthPercentage = _curHp / _maxHp;

            // === 배경 (빨간색 - 최대 체력) ===
            Gizmos.color = Color.red;
            Gizmos.DrawCube(healthBarPosition, new Vector3(1f, 0.1f, 0.1f));

            // === 전경 (초록색 - 현재 체력) ===
            // 체력 비율에 따라 너비 조절
            Gizmos.color = Color.green;
            // X축 오프셋 계산 (왼쪽 정렬)
            // 체력이 50%면 0.25유닛 왼쪽으로 이동
            Gizmos.DrawCube(
                healthBarPosition - Vector3.right * (0.5f - healthPercentage * 0.5f),
                new Vector3(healthPercentage, 0.12f, 0.12f)
            );
        }
    }
}
