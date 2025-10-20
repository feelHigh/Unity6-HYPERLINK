using UnityEngine;

[CreateAssetMenu(fileName = "SA_Water", menuName = "Enemy/Special Attacks/Water")]
public class SA_Water : SpecialAttackBase
{
    [Header("----- 발사체 설정 -----")]
    [SerializeField] GameObject _attackEffect;  //공격 이펙트

    [Header("----- 이펙트 프리팹 -----")]
    [SerializeField] GameObject _hitEffect;         //피격 이펙트    
    [SerializeField] GameObject _freezeEffect;      //빙결 이펙트
    [SerializeField] GameObject _slowEffect;        //둔화 이펙트

    // 빙결 설정 //
    public override float InstantDamage => 0.1f;    //즉시 10%
    public override float FreezeDuration => 1.5f;   //1.5초 동안
    // 둔화 설정 //
    public override float SlowPercent => 0.3f;      //둔화 30%
    public override float SlowDuration => 4f;       //4초 동안

    // 이펙트 //
    public override GameObject HitEffect => _hitEffect;
    public override GameObject DebuffEffect => _freezeEffect;
    public override GameObject AdditionalEffect => _slowEffect;

    public override void Execute(Transform attacker, Transform target)
    {
        if (_attackEffect == null)
        {
            Debug.LogError("공격 이펙트 프리팹이 연결되지 않았습니다.");
            return;
        }

        //적의 위치에서 소환
        Vector3 spawnPos = attacker.position;
        //플레이어를 바라보도록 회전
        Quaternion spawnRot = Quaternion.LookRotation(target.position - attacker.position);

        GameObject waveGO = Instantiate(_attackEffect, spawnPos, spawnRot);

        WaveAttackController controller = waveGO.GetComponent<WaveAttackController>();
        if (controller != null)
        {
            controller.Initialize(this);
        }
    }
}
