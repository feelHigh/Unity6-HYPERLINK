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

    List<EnemySpawner> _allSpawners = new List<EnemySpawner>();
    float _checkInterval = 1f;  //체크 시간 간격

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
    }

    private void Start()
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

                //플레이어와 스포너 사이의 거리를 체크
                float distance = Vector3.Distance(_player.position, spawner.transform.position);

                //만약 거리가 활성화 거리만큼 가까워지면
                if (distance <= _activationDistance)
                {
                    //스포너에게 그룹을 활성화하라고 명령
                    spawner.ActivateGroup();
                }
            }

            yield return new WaitForSeconds(_checkInterval);
        }
    }
}
