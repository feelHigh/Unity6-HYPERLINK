using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 모든 스포너의 위치를 파악하고, 
/// 주기적으로 플레이어와의 거리를 체크해서 
/// 가까운 스포너에 활성화 명령을 내리는 클래스.
/// 씬에 단 하나만 존재.
/// </summary>
public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance;

    [SerializeField] private Transform _player;
    [SerializeField] float _activationDistance = 50f;  //스포너가 활성화될 범위
    private float _activationDistanceSqr;                //sqrMagnitude 비교용 캐시

    List<EnemySpawner> _allSpawners = new List<EnemySpawner>();     //모든 스포너 리스트
    float _checkInterval = 1f;  //체크 시간 간격

    private void OnEnable()
    {
        PlayerInitializationManager.FindPlayer += FindPlayer;
    }
    private void OnDisable()
    {
        PlayerInitializationManager.FindPlayer -= FindPlayer;
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _activationDistanceSqr = _activationDistance * _activationDistance;
    }

    /// <summary>
    /// 플레이어를 찾은 후에 스포너 거리 체크 코루틴을 실행하는 코루틴
    /// </summary>
    /// <returns></returns>
    private void FindPlayer()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        

        StartCoroutine(CheckSpawnersRoutine());
    }

    /// <summary>
    /// 스포너가 생성될 때 자신을 매니저에게 등록하는 함수
    /// </summary>
    /// <param name="spawner"></param>
    public void RegisterSpawner(EnemySpawner spawner)
    {
        _allSpawners.Add(spawner);
    }

    /// <summary>
    /// 주기적으로 플레이어와 스포너들의 거리를 체크하는 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator CheckSpawnersRoutine()
    {
        while (true)
        {
            foreach (var spawner in _allSpawners)
            {
                if (spawner == null) continue;

                //플레이어와 스포너 사이의 거리를 체크 (sqrMagnitude로 sqrt 비용 절감)
                float sqrDistance = (_player.position - spawner.transform.position).sqrMagnitude;

                //만약 거리가 활성화 거리만큼 가까워지면
                if (sqrDistance <= _activationDistanceSqr)
                {
                    //스포너에게 그룹을 활성화하라고 명령
                    spawner.ActivateGroup();
                }
            }

            yield return WaitForSecondsCache.Get(_checkInterval);
        }
    }
}
