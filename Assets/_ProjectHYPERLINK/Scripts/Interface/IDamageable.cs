using UnityEngine;

/// <summary>
/// 플레이어한테 데미지를 받을 수 있는 인터페이스
/// (적, 파괴 가능한 오브젝트가 상속)
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}
