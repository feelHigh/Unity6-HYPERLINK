using UnityEngine;
using UnityEngine.AI;

public class EnemyAITest : MonoBehaviour
{
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 2f;

    private NavMeshAgent _agent;
    private Transform _player;
    private Enemy _enemy;
    private float _lastAttackTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (_player == null || !_enemy.IsAlive) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= _detectionRange)
        {
            _agent.SetDestination(_player.position);

            if (distance <= _attackRange && Time.time >= _lastAttackTime + _attackCooldown)
            {
                AttackPlayer();
            }
        }
    }

    private void AttackPlayer()
    {
        _lastAttackTime = Time.time;
        // TODO: Implement attack logic
        Debug.Log($"{gameObject.name} attacks player!");
    }
}
