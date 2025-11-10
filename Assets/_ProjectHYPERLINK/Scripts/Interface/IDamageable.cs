using UnityEngine;

/// <summary>
/// 플레이어한테 데미지를 받을 수 있는 인터페이스
/// (적, 파괴 가능한 오브젝트가 상속)
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 기본 데미지 처리 (하위 호환성 유지)
    /// </summary>
    /// <param name="damage">데미지 양</param>
    void TakeDamage(float damage);

    /// <summary>
    /// 공격 정보를 포함한 데미지 처리 (히트 VFX 지원)
    /// </summary>
    /// <param name="attackInfo">공격 정보 (타입, 데미지, VFX 포함)</param>
    void TakeDamage(AttackInfo attackInfo);
}
