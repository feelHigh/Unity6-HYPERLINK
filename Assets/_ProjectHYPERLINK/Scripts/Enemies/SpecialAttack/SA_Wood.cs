using UnityEngine;

[CreateAssetMenu(fileName = "SA_Wood", menuName = "Enemy/Special Attacks/Wood")]
public class SA_Wood : SpecialAttackBase
{
    [Header("----- 장판 설정 -----")]
    [SerializeField] GameObject _aoePrefab;  //장판 프리팹
    [SerializeField] LayerMask _playerLayerMask;

    [Header("----- 이펙트 프리팹 -----")]
    [SerializeField] GameObject _rootEffect;            //속박 이펙트    
    [SerializeField] GameObject _defenseDebuffEffect;   //약화 이펙트

    // 속박 설정 //
    public override float InstantDamage => 0.15f;   //즉시 15%
    public override float RootDuration => 4f;       //4초 동안
    // 약화 설정 //
    public override float DefenseDebuffPercent => 0.15f;    //방어력 15%
    public override float DefenseDebuffDuration => 8f;      //8초 동안

    // 이펙트 //
    public override GameObject HitEffect => null;
    public override GameObject DebuffEffect => _rootEffect;
    public override GameObject AdditionalEffect => _defenseDebuffEffect;

    public override void Execute(Transform attacker, Transform target)
    {
        if (_aoePrefab == null)
        {
            Debug.LogError("장판 프리팹이 연결되지 않았습니다.");
            return;
        }

        // 타겟의 현재 위치에 AoE 프리팹 생성
        Vector3 spawnPos = target.position;
        GameObject aoeGO = Instantiate(_aoePrefab, spawnPos, Quaternion.identity);

        AoEController controller = aoeGO.GetComponent<AoEController>();
        if (controller != null)
        {
            controller.Initialize(this, _playerLayerMask);
        }
    }
}
