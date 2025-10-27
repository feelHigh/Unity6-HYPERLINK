using System.Collections;
using UnityEngine;

public class HammerController : MonoBehaviour
{
    [SerializeField] Transform _target;
    [SerializeField] GameObject _hammerPrefab;      //망치 프리팹
    [SerializeField] float _fallDelay = 1f;       //망치 낙하까지의 지연 시간
    [SerializeField] float _fallSpeed = 40f;        //망치 낙하 속도

    SpecialAttackBase _specialAttack;

    public void Initialize(Transform target, GameObject hammer, SpecialAttackBase specialAttack)
    {
        _target = target;
        _hammerPrefab = hammer;
        _specialAttack = specialAttack;

        StartCoroutine(AttackCoroutine());

        Destroy(gameObject, 5f);
    }

    IEnumerator AttackCoroutine()
    {
        yield return new WaitForSeconds(_fallDelay);

        //이펙트의 상공에 생성
        Vector3 spawnPos = transform.position;
        spawnPos.y = 20f;

        //망치 프리팹 생성
        GameObject projectileGO = Instantiate(_hammerPrefab, spawnPos, Quaternion.identity);

        //생성된 망치를 초기화
        EnemyProjectile controller = projectileGO.GetComponent<EnemyProjectile>();
        if (controller != null)
        {
            controller.InitializeHammer(_fallSpeed, _specialAttack);
        }
    }
}
