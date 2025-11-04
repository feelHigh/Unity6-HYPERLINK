using UnityEngine;

/// <summary>
/// 몬스터한테 데미지를 받을 수 있는 인터페이스
/// (아마 플레이어만 상속)
/// 
/// 최근 변경사항:
/// - ApplySpecialEffect에 attackerPosition 파라미터 추가
/// - 넉백 방향을 올바르게 계산하기 위해 공격자 위치 전달
/// </summary>
public interface IMonsterDamageable
{
    void TakeDamage(float damage);

    /// <summary>
    /// 에픽 몬스터의 특수 공격 효과를 적용받는 함수
    /// </summary>
    /// <param name="attack">특수 공격 데이터</param>
    /// <param name="attackerPosition">공격자 위치 (넉백 방향 계산용)</param>
    void ApplySpecialEffect(SpecialAttackBase attack, Vector3 attackerPosition);
}
