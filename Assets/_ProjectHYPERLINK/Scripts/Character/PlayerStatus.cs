using System.Collections;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IMonsterDamageable
{
    [SerializeField] Rigidbody _rb;

    [SerializeField] float _maxHp;
    [SerializeField] float _curHp;
    [SerializeField] int _gold;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _curHp = _maxHp;
    }

    void Update()
    {
        
    }

    public void AddGold(int amount)
    {
        _gold += amount;
    }

    public void TakeDamage(float damage)
    {
        _curHp = Mathf.Max(0, _curHp - damage);

        if (_curHp <= 0)
        {
            Die();
        }
    }

    public void ApplySpecialEffect(SpecialAttackBase attack)
    {
        //즉시 대미지 적용
        float instantDamage = _maxHp * attack.InstantDamage;
        TakeDamage(instantDamage);

        //피격 이펙트 생성
        if (attack.HitEffect != null)
        {
            Instantiate(attack.HitEffect, transform.position, Quaternion.identity);
        }

        //속성 별 효과 코루틴 실행
        switch (attack.Type)
        {
            case SpecialAttackType.Fire:
                StartCoroutine(BurnCoroutine(attack));
                break;
            case SpecialAttackType.Water:
                StartCoroutine(FreezeCoroutine(attack));
                break;
            case SpecialAttackType.Earth:
                StartCoroutine(BlindCoroutine(attack));
                break;
            case SpecialAttackType.Wood:
                StartCoroutine(RootCoroutine(attack));
                break;
            case SpecialAttackType.Metal:
                StartCoroutine(KnockbackCoroutine(attack));
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 불 속성 특수 공격 효과(화상)를 적용하는 코루틴
    /// </summary>
    /// <param name="attack"></param>
    /// <returns></returns>
    IEnumerator BurnCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("화 효과 : 화상 발동");

        //화상 지속 이펙트 생성
        GameObject debuffEffect = null;
        if (attack.DebuffEffect != null)
        {
            debuffEffect = Instantiate(attack.DebuffEffect, transform);
        }

        //5초간 1초마다 화상 대미지
        float dotDamage = _maxHp * attack.DotDamage;
        float tick = attack.DotTickInterval;
        float timer = 0f;

        while (timer < attack.DotDuration)
        {
            yield return new WaitForSeconds(tick);
            TakeDamage(dotDamage);

            Debug.Log($"화상 도트 피해 {dotDamage} 입음");
            
            timer += tick;
        }

        //이펙트 삭제
        Destroy(debuffEffect);
    }

    /// <summary>
    /// 물 속성 특수 공격 효과를 적용하는 코루틴
    /// </summary>
    /// <param name="attack"></param>
    /// <returns></returns>
    IEnumerator FreezeCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("물 효과 : 빙결 및 둔화 발동");

        // 1. 빙결
        //빙결 이펙트 생성
        GameObject debuffEffect = null;
        if (attack.DebuffEffect != null)
        {
            debuffEffect = Instantiate (attack.DebuffEffect, transform);
        }

        //행동 불가 함수 ex. SetControllable(false);
        Debug.Log("빙결 시작");
        yield return new WaitForSeconds(attack.FreezeDuration);
        Debug.Log("빙결 해제");
        //SetControllable(true);

        //이펙트 삭제
        Destroy(debuffEffect);

        // 2. 둔화
        //둔화 이펙트 생성
        GameObject additionalEffect = null;
        if (attack.AdditionalEffect != null)
        {
            additionalEffect = Instantiate(attack.AdditionalEffect, transform);
        }

        //둔화 로직
        Debug.Log("둔화 시작");
        yield return new WaitForSeconds(attack.SlowDuration);
        Debug.Log("둔화 해제");
        //둔화 해제

        Destroy(additionalEffect);
    }

    /// <summary>
    /// 땅 속성 특수 공격 효과를 적용하는 코루틴
    /// </summary>
    /// <param name="attack"></param>
    /// <returns></returns>
    IEnumerator BlindCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("땅 효과 : 시야 방해 및 침묵 발동");

        // 1. 시야 방해 (UI?)
        Debug.Log("시야 방해 시작");

        // 2. 침묵
        //침묵 이펙트 생성
        GameObject debuffEffect = null;
        if (attack.DebuffEffect != null)
        {
            debuffEffect = Instantiate(attack.DebuffEffect, transform);
        }

        //침묵 함수 ex. SetSilenced(true);
        Debug.Log("침묵 시작");
        yield return new WaitForSeconds(attack.SilenceDuration);
        Debug.Log("침묵 해제");
        //SetSilenced(false);

        //이펙트 삭제
        Destroy(debuffEffect);

        yield return new WaitForSeconds(attack.BlindDuration - attack.SilenceDuration);
        Debug.Log("시야 방해 해제");
    }

    /// <summary>
    /// 목 속성 특수 공격 효과를 적용하는 코루틴
    /// </summary>
    /// <param name="attack"></param>
    /// <returns></returns>
    IEnumerator RootCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("목 효과 : 속박 및 방어력 감소 발동");

        // 1. 속박
        //속박 이펙트 생성
        GameObject debuffEffect = null;
        if (attack.DebuffEffect != null)
        {
            debuffEffect = Instantiate(attack.DebuffEffect, transform);
        }

        //이동 불가 함수 ex. SetRooted(true);
        Debug.Log("속박 시작");
        yield return new WaitForSeconds(attack.RootDuration);
        Debug.Log("속박 해제");
        //SetRooted(false);

        Destroy(debuffEffect);

        // 2. 방어력 감소
        //방어력 감소 이펙트 생성
        GameObject additionalEffect = null;
        if (attack.AdditionalEffect != null)
        {
            additionalEffect = Instantiate(attack.AdditionalEffect, transform);
        }

        //방어력 감소 로직
        Debug.Log("방어력 감소 시작");
        yield return new WaitForSeconds(attack.DefenseDebuffDuration);
        Debug.Log("방어력 감소 해제");
        //방어력 회복

        Destroy(additionalEffect);
    }

    /// <summary>
    /// 금 속성 특수 공격 효과를 적용하는 코루틴
    /// </summary>
    /// <param name="attack"></param>
    /// <returns></returns>
    IEnumerator KnockbackCoroutine(SpecialAttackBase attack)
    {
        Debug.Log("금 효과 : 넉백 및 넉다운 발동");

        //넉백
        if (_rb != null)
        {
            //공격자 위치 알아야 정확한 넉백 방향 알 수 있음
            //여기서는 임시로 뒤로 밀려난다고 가정.
            Vector3 knockbackDir = -transform.forward;
            _rb.AddForce(knockbackDir * attack.KnockbackPower, ForceMode.Impulse);
        }

        //넉다운
        //넉다운 이펙트 생성
        GameObject debuffEffect = null;
        if (attack.DebuffEffect != null)
        {
            debuffEffect = Instantiate(attack.DebuffEffect, transform);
        }

        //행동 불가 함수 ex. SetControllable(false);
        Debug.Log("넉다운 시작");
        yield return new WaitForSeconds(attack.StunDuration);
        Debug.Log("넉다운 해제");
        //SetControllable(true);

        //이펙트 삭제
        Destroy(debuffEffect);
    }

    public void Die()
    {
        Debug.Log("플레이어 사망");
    }
}
