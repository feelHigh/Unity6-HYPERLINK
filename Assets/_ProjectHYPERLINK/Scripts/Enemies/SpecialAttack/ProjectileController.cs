using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    [SerializeField] float _speed;
    [SerializeField] Rigidbody _rb;

    [SerializeField] SpecialAttackBase _specialAttack;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 발사체를 초기화하는 함수
    /// </summary>
    /// <param name="speed"></param>
    /// <param name="totalDamage"></param>
    /// <param name="duration"></param>
    /// <param name="tickInterval"></param>
    public void Initialize(float speed, SpecialAttackBase specialAttack)
    {
        _speed = speed;
        _rb.linearVelocity = transform.forward * _speed;

        _specialAttack = specialAttack;

        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        //플레이어와 부딪혔을 때 화상 효과
        if (other.CompareTag("Player"))
        {
            Debug.Log("발사체가 플레이어에게 명중!");

            //Player 스크립트에서  처리
            IMonsterDamageable player = other.GetComponent<IMonsterDamageable>();
            if (player != null && _specialAttack != null)
            {
                player.ApplySpecialEffect(_specialAttack);
            }

            Destroy(gameObject);
        }

        //적 동료가 아닌 것(벽, 오브젝트 등)에 부딪혔을 때는 파괴
        else if (!other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
