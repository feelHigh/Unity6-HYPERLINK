using UnityEngine;

/// <summary>
/// 금속(Metal) 속성 특수 공격
/// 
/// 최근 변경사항:
/// - ExecuteMelee에서 attacker.position 전달하도록 수정
/// - 넉백 방향이 공격자 기준으로 올바르게 계산되도록 개선
/// </summary>
[CreateAssetMenu(fileName = "SA_Metal", menuName = "Enemy/Special Attacks/Metal")]
public class SA_Metal : SpecialAttackBase
{
    [Header("----- 근거리 설정 -----")]
    [SerializeField] GameObject _meleeAttackEffect; //근거리 공격 이펙트 프리팹
    [SerializeField] float _meleeRadius = 3f;       // 근거리 공격 범위

    [Header("----- 원거리 설정 -----")]
    [SerializeField] GameObject _rangedAttackEffect;//원거리 공격 이펙트 프리팹
    [SerializeField] GameObject _hammerPrefab;      //망치 프리팹

    [Header("----- 이펙트 프리팹 -----")]
    [SerializeField] GameObject _hitEffect;         //피격 이펙트    
    [SerializeField] GameObject _stunEffect;        //넉다운 이펙트

    // 넉백 설정 //
    public override float InstantDamage => 0.15f;   //즉시 15%
    public override float KnockbackPower => 4f;     //4미터
    // 넉다운 설정 //
    public override float StunDuration => 1f;       //1초 동안

    // 이펙트 //
    public override GameObject HitEffect => _hitEffect;
    public override GameObject DebuffEffect => _stunEffect;

    /// <summary>
    /// 금 속성 근거리 공격 : 강하게 휘두르기
    /// 
    /// attacker.position을 ApplySpecialEffect에 전달
    /// - 넉백 방향을 올바르게 계산하기 위해 공격자 위치 전달
    /// </summary>
    public override void ExecuteMelee(Transform attacker, Transform target)
    {
        if (_meleeAttackEffect == null)
        {
            Debug.LogError("근거리 공격 이펙트 프리팹이 연결되지 않았습니다.");
            return;
        }

        // 공격 이펙트 생성
        GameObject effectGO = Instantiate(_meleeAttackEffect, attacker.position + attacker.forward, attacker.rotation);

        // 범위 내 플레이어 탐지
        Collider[] colls = Physics.OverlapSphere(attacker.position, _meleeRadius, _playerLayerMask);
        foreach (var coll in colls)
        {
            IMonsterDamageable player = coll.GetComponent<IMonsterDamageable>();
            if (player != null)
            {
                // 공격자 위치 전달
                player.ApplySpecialEffect(this, attacker.position);
            }
        }

        Destroy(effectGO, 2f);
    }

    /// <summary>
    /// 금 속성 원거리 공격 : 망치 떨구기
    /// </summary>
    public override void ExecuteRanged(Transform attacker, Transform target)
    {
        if (_hammerPrefab == null)
        {
            Debug.LogError("망치 프리팹이 연결되지 않았습니다.");
            return;
        }

        //원거리 공격 이펙트 생성 및 컨트롤러 초기화
        GameObject hammerGO = Instantiate(_rangedAttackEffect, target.position, target.rotation);
        HammerController hammer = hammerGO.GetComponent<HammerController>();
        if (hammer != null)
        {
            hammer.Initialize(target, _hammerPrefab, this);
        }
    }
}
