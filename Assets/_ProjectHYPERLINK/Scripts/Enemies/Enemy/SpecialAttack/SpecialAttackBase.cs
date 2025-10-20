using UnityEngine;

/// <summary>
/// 모든 특수 공격 데이터의 기반이 될 추상 클래스
/// </summary>
public abstract class SpecialAttackBase : ScriptableObject
{
    [Header("----- 에픽 몬스터 이펙트 -----")]
    [SerializeField] GameObject _epicEffect;

    [Header("----- 특수 공격 공통 -----")]
    [SerializeField] SpecialAttackType _type;       //특수 공격 타입
    [SerializeField] float _coolTime = 10f;         //특수 공격 쿨타임

    [Header("----- 발사체 발사 지점 -----")]
    [SerializeField] protected Transform cachedFirePos;

    // 프로퍼티 //
    public GameObject EpicEffect => _epicEffect;
    public SpecialAttackType Type => _type;
    public float CoolTime => _coolTime;
    public int SpecialAttackAnim => (int)_type;     //특수 공격 애니메이션 트리거

    // 특수 효과 값 (각 자식 클래스에서 재정의) //
    //공통 (즉시 피해)
    [Range(0f, 1f)]
    public virtual float InstantDamage => 1f;       //즉시 대미지 (Player 최대 체력 비례 퍼센트)

    //이펙트 프리팹 (Player가 사용)
    public virtual GameObject HitEffect => null;    //피격 이펙트
    public virtual GameObject DebuffEffect => null; //디버프 이펙트 (화상, 빙결, 속박 등)
    public virtual GameObject AdditionalEffect => null;  //추가 디버프 이펙트 (이속 감소, 방어력 감소 등)

    //Fire
    public virtual float DotDamage => 0f;           //도트 대미지
    public virtual float DotDuration => 0f;         //도트 지속 시간
    public virtual float DotTickInterval => 1f;     //도트 틱 간격

    //Water
    public virtual float BeamDuration => 0f;        //공격(빔) 지속 시간
    public virtual float FreezeDuration => 0f;      //빙결 지속 시간
    public virtual float SlowPercent => 0f;         //둔화 퍼센트
    public virtual float SlowDuration => 0f;        //둔화 지속 시간

    //Earth
    public virtual float BlindDuration => 0f;       //시야 방해 지속 시간
    public virtual float SilenceDuration => 0f;     //침묵 지속 시간

    //Wood
    public virtual float RootDuration => 0f;        //속박 지속 시간
    public virtual float DefenseDebuffPercent => 0f;    //약화 퍼센트
    public virtual float DefenseDebuffDuration => 0f;   //약화 지속 시간

    //Metal
    public virtual float KnockbackPower => 0f;      //넉백 파워 (거리)
    public virtual float StunDuration => 0f;        //넉다운 지속 시간

    /// <summary>
    /// firePos(발사체 발사 지점)를 찾는 함수
    /// </summary>
    /// <param name="attacker"></param>
    /// <returns></returns>
    protected Transform GetFirePos(Transform attacker)
    {
        if (cachedFirePos != null)
        {
            return cachedFirePos;
        }

        Transform firePos = attacker.Find("firePos");

        if (firePos == null)
        {
            Debug.LogWarning($"[{attacker.name}] firePos를 찾을 수 없습니다. 기본 위치에서 발사합니다.");
            return null;
        }

        cachedFirePos = firePos;

        return cachedFirePos;
    }

    /// <summary>
    /// 특수 공격을 실행하는 함수
    /// </summary>
    /// <param name="attacker">공격을 시전하는 자신</param>
    /// <param name="target">공격을 당하는 대상</param>
    public abstract void Execute(Transform attacker, Transform target);
}
