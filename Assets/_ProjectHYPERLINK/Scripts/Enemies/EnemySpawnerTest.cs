using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerTest : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject[] _enemyPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private int _maxEnemies = 10;
    [SerializeField] private float _spawnInterval = 5f;
    [SerializeField] private bool _autoSpawn = true;

    [Header("Wave Settings")]
    [SerializeField] private bool _useWaveSystem = false;
    [SerializeField] private int _enemiesPerWave = 5;
    [SerializeField] private float _waveCooldown = 10f;

    private List<GameObject> _activeEnemies = new List<GameObject>();
    private int _currentWave = 0;
    private bool _isSpawning = false;

    private void Start()
    {
        if (_autoSpawn)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (_isSpawning) return;

        _isSpawning = true;

        if (_useWaveSystem)
        {
            StartCoroutine(WaveSpawnRoutine());
        }
        else
        {
            StartCoroutine(ContinuousSpawnRoutine());
        }
    }

    public void StopSpawning()
    {
        _isSpawning = false;
        StopAllCoroutines();
    }

    private IEnumerator ContinuousSpawnRoutine()
    {
        while (_isSpawning)
        {
            // Clean up destroyed enemies
            _activeEnemies.RemoveAll(e => e == null);

            // Spawn if below max
            if (_activeEnemies.Count < _maxEnemies)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    private IEnumerator WaveSpawnRoutine()
    {
        while (_isSpawning)
        {
            _currentWave++;
            Debug.Log($"Wave {_currentWave} starting!");

            for (int i = 0; i < _enemiesPerWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitUntil(() => _activeEnemies.TrueForAll(e => e == null));

            Debug.Log($"Wave {_currentWave} completed!");

            yield return new WaitForSeconds(_waveCooldown);
        }
    }

    private void SpawnEnemy()
    {
        if (_enemyPrefabs.Length == 0 || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("No enemy prefabs or spawn points!");
            return;
        }

        GameObject enemyPrefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Length)];

        Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        GameObject enemy = Instantiate(enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation);

        _activeEnemies.Add(enemy);

        Debug.Log($"Spawned {enemy.name} at {spawnPoint.name}");
    }

    public void SpawnEnemyAt(int enemyIndex, int spawnIndex)
    {
        if (enemyIndex >= _enemyPrefabs.Length || spawnIndex >= _spawnPoints.Length)
        {
            Debug.LogError("Invalid index!");
            return;
        }

        GameObject enemy = Instantiate(_enemyPrefabs[enemyIndex],
            _spawnPoints[spawnIndex].position,
            _spawnPoints[spawnIndex].rotation);

        _activeEnemies.Add(enemy);
    }

    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in _activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        _activeEnemies.Clear();
    }
    
    public int GetActiveEnemyCount()
    {
        _activeEnemies.RemoveAll(e => e == null);
        return _activeEnemies.Count;
    }

    private void OnDrawGizmos()
    {
        if (_spawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (Transform point in _spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 1f);
                Gizmos.DrawLine(point.position, point.position + Vector3.up * 2f);
            }
        }
    }
}
