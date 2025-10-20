using UnityEngine;

[CreateAssetMenu(fileName = "SA_Metal", menuName = "Enemy/Special Attacks/Metal")]
public class SA_Metal : SpecialAttackBase
{
    [Header("----- 공격 설정 -----")]
    [SerializeField] GameObject _attackEffect;      //공격 이펙트

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

    public override void Execute(Transform attacker, Transform target)
    {
        if (_attackEffect == null)
        {
            Debug.LogError("공격 이펙트 프리팹이 연결되지 않았습니다.");
            return;
        }
        else
        {
            //공격 이펙트 생성
            Instantiate(_attackEffect, attacker.position + attacker.forward, attacker.rotation);
        }

        IMonsterDamageable player = target.GetComponent<IMonsterDamageable>();
        if (player != null)
        {
            player.ApplySpecialEffect(this);
        }
    }
}