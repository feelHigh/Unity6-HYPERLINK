using UnityEngine;

public class WaveAttackController : MonoBehaviour
{
    SpecialAttackBase _specialAttack;

    //이펙트 지속 시간
    [SerializeField] float _lifetime = 3f;

    /// <summary>
    /// 파동형 공격을 초기화하는 함수
    /// </summary>
    public void Initialize(SpecialAttackBase specialAttack)
    {
        _specialAttack = specialAttack;
        Destroy(gameObject, _lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        //이펙트가 지속되는 동안 플레이어가 닿으면
        if (other.CompareTag("Player"))
        {
            IMonsterDamageable player = other.GetComponent<IMonsterDamageable>();
            if (player != null && _specialAttack != null)
            {
                //Player의 ApplySpecialEffect 호출
                player.ApplySpecialEffect(_specialAttack);

                //한 번만 맞도록 이 스크립트와 콜라이더를 비활성화
                GetComponent<Collider>().enabled = false;
                enabled = false;
            }
        }
    }
}
