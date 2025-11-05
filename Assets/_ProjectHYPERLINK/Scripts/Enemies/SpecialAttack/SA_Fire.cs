using UnityEngine;

[CreateAssetMenu(fileName = "SA_Fire", menuName = "Enemy/Special Attacks/Fire")]
public class SA_Fire : SpecialAttackBase
{
    [Header("----- 근거리 설정 -----")]
    [SerializeField] GameObject _meleeAttackEffect; //근거리 공격 이펙트 프리팹
    [SerializeField] float _meleeRadius = 3f;

    [Header("----- 원거리 설정 -----")]
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

    /// <summary>
    /// 불 속성 근거리 공격 : 화염 방출
    /// </summary>
    public override void ExecuteMelee(Transform attacker, Transform target)
    {
        if (_meleeAttackEffect == null)
        {
            Debug.LogError("근거리 공격 이펙트 프리팹이 연결되지 않았습니다.");
            return;
        }

        //자신 위치에 화염 방출 이펙트 생성
        GameObject effectGO = Instantiate(_meleeAttackEffect, attacker.position, attacker.rotation, attacker);

        //범위 내 플레이어 탐지
        Collider[] colls = Physics.OverlapSphere(attacker.position, _meleeRadius, _playerLayerMask);
        foreach (var coll in colls)
        {
            IMonsterDamageable player = coll.GetComponent<IMonsterDamageable>();
            if (player != null)
            {
                player.ApplySpecialEffect(this, attacker.position);
            }
        }

        Destroy(effectGO, 2f);
    }

    /// <summary>
    /// 불 속성 원거리 공격 : 파이어볼
    /// </summary>
    public override void ExecuteRanged(Transform attacker, Transform target)
    {
        if (_projectilePrefab == null)
        {
            Debug.LogError("발사체 프리팹이 연결되지 않았습니다.");
            return;
        }

        Transform firePos = GetFirePos(attacker);

        //firePos가 있으면 거기서, 없으면 기본 위치에서 발사 (적 위치보다 약간 앞)
        Vector3 spawnPos = firePos != null ? firePos.position : attacker.position + attacker.forward * 1.2f + attacker.up * 1;

        //발사 방향은 타겟(플레이어)를 향해
        Vector3 targetPos = target.position + Vector3.up;
        Vector3 dir = (targetPos - spawnPos).normalized;

        Quaternion spawnRot = Quaternion.LookRotation(dir, Vector3.up);

        //화염구 프리팹 생성
        GameObject projectileGO = Instantiate(_projectilePrefab, spawnPos, spawnRot);

        //생성된 화염구를 초기화
        EnemyProjectile controller = projectileGO.GetComponent<EnemyProjectile>();
        if (controller != null)
        {
            controller.InitializeSA(_projectileSpeed, this);
        }
    }
}
