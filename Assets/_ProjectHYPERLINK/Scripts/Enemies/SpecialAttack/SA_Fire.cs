using UnityEngine;

[CreateAssetMenu(fileName = "SA_Fire", menuName = "Enemy/Special Attacks/Fire")]
public class SA_Fire : SpecialAttackBase
{
    [Header("----- 발사체 설정 -----")]
    [SerializeField] GameObject _projectilePrefab;  //발사체 프리팹
    [SerializeField] float _projectileSpeed = 15f;  //발사체 속도

    [Header("----- 이펙트 프리팹 -----")]
    [SerializeField] GameObject _hitEffect;         //피격 이펙트    
    [SerializeField] GameObject _dotEffect;         //화상 이펙트

    // 화상 설정 //
    public override float InstantDamage => 0.05f;   //즉시 5%
    public override float DotDamage => 0.01f;       //매틱 1%
    public override float DotDuration => 5f;        //5초 동안

    // 이펙트 //
    public override GameObject HitEffect => _hitEffect;
    public override GameObject DebuffEffect => _dotEffect;

    public override void Execute(Transform attacker, Transform target)
    {
        if (_projectilePrefab == null)
        {
            Debug.LogError("발사체 프리팹이 연결되지 않았습니다.");
            return;
        }

        Transform firePos = GetFirePos(attacker);

        //firePos가 있으면 거기서, 없으면 기본 위치에서 발사 (적 위치보다 약간 앞)
        Vector3 spawnPos = firePos != null ? firePos.position : attacker.position + attacker.forward * 1.2f;
        //발사 방향은 타겟(플레이어)를 향해
        Quaternion spawnRot = Quaternion.LookRotation(target.position - spawnPos);

        //화염구 프리팹 생성
        GameObject projectileGO = Instantiate(_projectilePrefab, spawnPos, spawnRot);

        //생성된 화염구를 초기화
        ProjectileController controller = projectileGO.GetComponent<ProjectileController>();
        if (controller != null)
        {
            controller.Initialize(_projectileSpeed, this);
        }
    }
}