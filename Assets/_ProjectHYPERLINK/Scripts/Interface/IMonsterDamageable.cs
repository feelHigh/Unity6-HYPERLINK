using UnityEngine;

/// <summary>
/// 몬스터한테 데미지를 받을 수 있는 인터페이스
/// (아마 플레이어만 상속)
/// </summary>
public interface IMonsterDamageable
{
    void TakeDamage(float damage);

    /// <summary>
    /// 에픽 몬스터의 특수 공격 효과를 적용받는 함수
    /// </summary>
    /// <param name="attack"></param>
    void ApplySpecialEffect(SpecialAttackBase attack);

    void Die();
}
