using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기본 적 클래스
/// 
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] EnemyData _data;       //적 데이터

    [Header("----- 드랍 설정 -----")]
    [SerializeField] private ItemDropTableData _dropTable;              // 아이템 드랍 테이블
    [SerializeField][Range(0f, 1f)] private float _dropChance = 0.5f;   // 드랍 확률 (50%)

    [Header("----- 현재 속한 그룹 -----")]
    [SerializeField] EnemyGroup _group;     //현재 속한 그룹

    [Header("----- 에픽 몬스터 -----")]
    [SerializeField] SpecialAttackBase _specialAttack;  //특수 공격 데이터
    [SerializeField] List<GameObject> _epicOrbs = new List<GameObject>();   //에픽 몬스터 오브 이펙트 리스트

    // 이벤트 //
    public event Action OnInitialized;      //초기화 완료 이벤트
    public event Action OnHit;              //피격 이벤트
    public event Action OnDie;              //죽음 이벤트

    // 현재 상태 스탯 //
    bool _isEpic = false;   //에픽 몬스터 여부
    float _maxHp;           //최대 체력
    float _curHp;           //현재 체력
    float _atk;             //공격력

    int _expReward;         //보상 경험치
    int _goldReward;        //보상 골드


    // 프로퍼티 //
    public EnemyData Data => _data;
    public EnemyGroup Group => _group;
    public bool IsEpic => _isEpic;
    public SpecialAttackBase SpecialAttack => _specialAttack;
    public float Atk => _atk;

    /// <summary>
    /// Enemy를 초기화하는 함수
    /// </summary>
    /// <param name="canBeEpic"></param>
    public void Initialize(bool isEpic, SpecialAttackBase specialAttack)
    {
        _group = transform.parent.GetComponent<EnemyGroup>();

        //에픽 여부
        _isEpic = isEpic;
        _specialAttack = specialAttack;

        //스탯 초기화
        _maxHp = _data.MaxHp * (_isEpic ? _data.EpicHpMultiplier : 1);
        _curHp = _maxHp;

        _atk = _data.Atk * (_isEpic ? _data.EpicAtkMultiplier : 1);

        _expReward = _data.RewardExp * (_isEpic ? _data.EpicExpMultiplier : 1);
        _goldReward = _data.RewardGold * (_isEpic ? _data.EpicGoldMultiplier : 1);

        //에픽 몬스터 크기 변경 및 이펙트 (오브) 생성
        if (_isEpic)
        {
            transform.localScale *= 1.2f;
            Debug.Log($"{name}이(가) {_specialAttack.Type}타입 에픽 몬스터로 등장!");

            if (_specialAttack != null && _specialAttack.EpicEffect != null)
            {
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
        }

        //초기화 완료 이벤트 발행
        OnInitialized?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        //현재 상태가 죽음 상태면 리턴
        if (_curHp <= 0) return;

        //현재 체력을 데미지 만큼 감소
        _curHp -= damage;

        //애니메이션 이벤트 발행
        OnHit?.Invoke();

        //현재 체력이 0보다 작거나 같으면
        if (_curHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        //죽음 이벤트 발행
        OnDie?.Invoke();

        //콜라이더 비활성화
        GetComponent<Collider>().enabled = false;

        //에픽 몬스터라면 오브 이펙트 파괴
        if (_isEpic)
        {
            foreach (GameObject orb in _epicOrbs)
            {
                Destroy(orb);
            }

            _epicOrbs.Clear();
        }

        //보상 지급
        //경험치
        ExperienceManager playerExperienceManager = FindFirstObjectByType<ExperienceManager>();
        if (playerExperienceManager != null)
        {
            playerExperienceManager.GainExperience(_expReward);
        }
        //골드
        //PlayerCombat player = _target.GetComponent<PlayerCombat>();
        //if (player != null)
        //{
            //player.AddGold(_goldReward);
        //}

        //아이템 드랍
        // ItemSpawner와 드랍 테이블이 설정되어 있는지 확인
        if (ItemSpawner.Instance != null && _dropTable != null)
        {
            // 확률 체크 (0~1 사이 랜덤 값)
            if (UnityEngine.Random.Range(0f, 1f) < _dropChance)
            {
                // 적의 현재 위치에 아이템 드랍
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
