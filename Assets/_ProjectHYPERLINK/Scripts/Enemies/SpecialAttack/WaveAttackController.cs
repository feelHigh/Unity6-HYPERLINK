using UnityEngine;
using System.Collections;

public class WaveAttackController : MonoBehaviour
{
    [SerializeField] float _maxDistance = 16f;      //최종 길이
    [SerializeField] float _expandSpeed = 8f;       //초당 늘어나는 속도
    [SerializeField] Vector3 _boxHalfExtents = new Vector3(0.5f, 1f, 0.5f);     //공격 면적

    SpecialAttackBase _specialAttack;
    BoxCollider _collider;

    /// <summary>
    /// 파동형 공격을 초기화하는 함수
    /// </summary>
    public void Initialize(SpecialAttackBase specialAttack)
    {
        _specialAttack = specialAttack;
        // _collider.enabled = false;

        //StartCoroutine(AttackCoroutine());

        Destroy(gameObject, 2f);
    }

    //IEnumerator AttackCoroutine()
    //{
    //    float timer = 0f;
    //     while (timer >= )
    //}

    private void OnTriggerEnter(Collider other)
    {
        //이펙트가 지속되는 동안 플레이어가 닿으면
        if (other.CompareTag("Player"))
        {
            PlayerCombat player = other.GetComponent<PlayerCombat>();
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
