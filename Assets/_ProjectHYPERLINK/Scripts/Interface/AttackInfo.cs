using UnityEngine;

/// <summary>
/// 공격 정보를 담는 구조체
/// </summary>
public struct AttackInfo
{
    public AttackType Type;
    public float Damage;
    public HitVfxConfig HitVfxConfig;
    public Vector3 HitPosition;

    /// <summary>
    /// 플레이어 기본 공격 생성
    /// </summary>
    public static AttackInfo CreatePlayerBaseAttack(float damage, Vector3 hitPosition, HitVfxConfig hitVfxConfig = null)
    {
        return new AttackInfo
        {
            Type = AttackType.PlayerBaseAttack,
            Damage = damage,
            HitVfxConfig = hitVfxConfig,
            HitPosition = hitPosition
        };
    }

    /// <summary>
    /// 플레이어 스킬 공격 생성
    /// </summary>
    public static AttackInfo CreatePlayerSkill(float damage, Vector3 hitPosition, HitVfxConfig hitVfxConfig = null)
    {
        return new AttackInfo
        {
            Type = AttackType.PlayerSkill,
            Damage = damage,
            HitVfxConfig = hitVfxConfig,
            HitPosition = hitPosition
        };
    }

    /// <summary>
    /// 적 기본 공격 생성
    /// </summary>
    public static AttackInfo CreateEnemyBaseAttack(float damage, Vector3 hitPosition, HitVfxConfig hitVfxConfig = null)
    {
        return new AttackInfo
        {
            Type = AttackType.EnemyBaseAttack,
            Damage = damage,
            HitVfxConfig = hitVfxConfig,
            HitPosition = hitPosition
        };
    }

    /// <summary>
    /// 적 특수 공격 생성
    /// </summary>
    public static AttackInfo CreateEnemySpecialAttack(float damage, Vector3 hitPosition, HitVfxConfig hitVfxConfig = null)
    {
        return new AttackInfo
        {
            Type = AttackType.EnemySpecialAttack,
            Damage = damage,
            HitVfxConfig = hitVfxConfig,
            HitPosition = hitPosition
        };
    }
}

public enum AttackType
{
    PlayerBaseAttack,
    PlayerSkill,
    EnemyBaseAttack,
    EnemySpecialAttack
}
