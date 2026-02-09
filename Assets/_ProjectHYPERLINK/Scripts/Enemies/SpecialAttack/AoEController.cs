using UnityEngine;
using System.Collections;

public class AoEController : MonoBehaviour
{
    [SerializeField] float _delay = 1.5f;           //폭발 지연 시간
    [SerializeField] float _radius = 3f;            //공격 범위
    [SerializeField] LayerMask _playerLayerMask;    //플레이어 레이어 마스크

    SpecialAttackBase _specialAttack;

    /// <summary>
    /// AoE를 초기화하는 함수
    /// </summary>
    public void Initialize(SpecialAttackBase specialAttack, LayerMask playerLayerMask)
    {
        _specialAttack = specialAttack;
        _playerLayerMask = playerLayerMask;
        StartCoroutine(AttackCoroutine());

        //4초 후 오브젝트 파괴
        Destroy(gameObject, 4f);
    }

    IEnumerator AttackCoroutine()
    {
        //1.5초 대기 (이펙트 재생 중)
        yield return WaitForSecondsCache.Get(_delay);

        //범위 내 플레이어 탐지
        Collider[] colliders = Physics.OverlapSphere(transform.position, _radius, _playerLayerMask);
        foreach (var col in colliders)
        {
            PlayerCombat player = col.GetComponent<PlayerCombat>();
            if (player != null)
            {
                //플레이어에게 효과 적용 (AoE 중심점을 공격자 위치로 전달)
                player.ApplySpecialEffect(_specialAttack, transform.position);
            }
            else
            {
                Debug.Log("[AoEController] 플레이어를 찾을 수 없습니다.");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
