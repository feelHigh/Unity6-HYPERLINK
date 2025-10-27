using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 모든 스포너의 위치를 파악하고 플레이어와의 거리를 체크하는 시스템 (리팩토링 v2)
/// 
/// 변경사항:
/// - Start()의 FindGameObjectWithTag 제거
/// - PlayerInitializationManager 이벤트 구독 방식으로 변경
/// - OnPlayerSpawned 이벤트 사용
/// 
/// 역할:
/// - 모든 EnemySpawner 관리
/// - 플레이어와 스포너 거리 체크
/// - 가까운 스포너 활성화
/// 
/// 사용:
/// - 씬에 단 하나만 존재 (싱글톤)
/// </summary>
public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance;

    [Header("플레이어 거리 설정")]
    [SerializeField] private float _activationDistance = 50f;  // 스포너가 활성화될 범위
    [SerializeField] private float _checkInterval = 1f;        // 체크 시간 간격

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private Transform _player;
    private List<EnemySpawner> _allSpawners = new List<EnemySpawner>();
    private Coroutine _checkCoroutine;
    private bool _isPlayerReady = false;

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

    private void OnEnable()
    {
        // PlayerInitializationManager 이벤트 구독
        PlayerInitializationManager.OnPlayerSpawned += HandlePlayerSpawned;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerInitializationManager.OnPlayerSpawned -= HandlePlayerSpawned;
    }

    /// <summary>
    /// Player 스폰 완료 이벤트 핸들러
    /// PlayerInitializationManager의 Phase 1에서 호출됨
    /// </summary>
    private void HandlePlayerSpawned(GameObject player)
    {
        if (player == null)
        {
            LogError("HandlePlayerSpawned: player가 null입니다!");
            return;
        }

        _player = player.transform;
        _isPlayerReady = true;
        Log($"플레이어 발견: {player.name}");

        // 거리 체크 코루틴 시작
        StartCheckingSpawners();
    }

    /// <summary>
    /// 거리 체크 코루틴 시작
    /// </summary>
    private void StartCheckingSpawners()
    {
        if (_checkCoroutine != null)
        {
            StopCoroutine(_checkCoroutine);
        }

        _checkCoroutine = StartCoroutine(CheckSpawnersRoutine());
        Log("스포너 거리 체크 시작");
    }

    /// <summary>
    /// 스포너가 생성될 때 자신을 매니저에게 등록하는 함수
    /// </summary>
    public void RegisterSpawner(EnemySpawner spawner)
    {
        if (!_allSpawners.Contains(spawner))
        {
            _allSpawners.Add(spawner);
            Log($"스포너 등록: {spawner.name}");
        }
    }

    /// <summary>
    /// 스포너 등록 해제
    /// </summary>
    public void UnregisterSpawner(EnemySpawner spawner)
    {
        if (_allSpawners.Contains(spawner))
        {
            _allSpawners.Remove(spawner);
            Log($"스포너 등록 해제: {spawner.name}");
        }
    }

    /// <summary>
    /// 주기적으로 플레이어와 스포너들의 거리를 체크하는 코루틴
    /// </summary>
    private IEnumerator CheckSpawnersRoutine()
    {
        while (true)
        {
            // Player가 준비될 때까지 대기
            if (!_isPlayerReady || _player == null)
            {
                yield return new WaitForSeconds(_checkInterval);
                continue;
            }

            foreach (var spawner in _allSpawners)
            {
                if (spawner == null) continue;

                // 플레이어와 스포너 사이의 거리를 체크
                float distance = Vector3.Distance(_player.position, spawner.transform.position);

                // 만약 거리가 활성화 거리만큼 가까워지면
                if (distance <= _activationDistance)
                {
                    // 스포너에게 그룹을 활성화하라고 명령
                    spawner.ActivateGroup();
                }
            }

            yield return new WaitForSeconds(_checkInterval);
        }
    }

    /// <summary>
    /// Player가 준비되었는지 확인
    /// </summary>
    public bool IsPlayerReady() => _isPlayerReady;

    /// <summary>
    /// 등록된 스포너 개수 가져오기
    /// </summary>
    public int GetSpawnerCount() => _allSpawners.Count;

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[SpawnerManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SpawnerManager] {message}");
    }

    #endregion
}
