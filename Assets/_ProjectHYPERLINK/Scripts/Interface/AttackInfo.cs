using UnityEngine;

/// <summary>
/// 공격 정보를 담는 구조체
/// 공격자로부터 피격자에게 전달되는 데이터
/// 
/// 용도:
/// - 데미지 계산
/// - 공격 타입 식별 (기본 공격 vs 스킬 vs 특수 공격)
/// - 적절한 히트 VFX 선택
/// </summary>
public struct AttackInfo
{
    /// <summary>
    /// 공격 타입
    /// </summary>
    public AttackType Type;

    /// <summary>
    /// 데미지 양
    /// </summary>
    public float Damage;

    /// <summary>
    /// 피격 시 생성될 히트 VFX 프리팹
    /// </summary>
    public GameObject HitVfxPrefab;

    /// <summary>
    /// 피격 위치
    /// </summary>
    public Vector3 HitPosition;

    /// <summary>
    /// 플레이어 기본 공격 생성
    /// </summary>
    public static AttackInfo CreatePlayerBaseAttack(float damage, Vector3 hitPosition, GameObject hitVfx = null)
    {
        return new AttackInfo
        {
            Type = AttackType.PlayerBaseAttack,
            Damage = damage,
            HitVfxPrefab = hitVfx,
            HitPosition = hitPosition
        };
    }

    /// <summary>
    /// 플레이어 스킬 공격 생성
    /// </summary>
    public static AttackInfo CreatePlayerSkill(float damage, Vector3 hitPosition, GameObject hitVfx = null)
    {
        return new AttackInfo
        {
            Type = AttackType.PlayerSkill,
            Damage = damage,
            HitVfxPrefab = hitVfx,
            HitPosition = hitPosition
        };
    }

    /// <summary>
    /// 적 기본 공격 생성
    /// </summary>
    public static AttackInfo CreateEnemyBaseAttack(float damage, Vector3 hitPosition, GameObject hitVfx = null)
    {
        return new AttackInfo
        {
            Type = AttackType.EnemyBaseAttack,
            Damage = damage,
            HitVfxPrefab = hitVfx,
            HitPosition = hitPosition
        };
    }

    /// <summary>
    /// 적 특수 공격 생성
    /// </summary>
    public static AttackInfo CreateEnemySpecialAttack(float damage, Vector3 hitPosition, GameObject hitVfx = null)
    {
        return new AttackInfo
        {
            Type = AttackType.EnemySpecialAttack,
            Damage = damage,
            HitVfxPrefab = hitVfx,
            HitPosition = hitPosition
        };
    }
}

/// <summary>
/// 공격 타입 열거형
/// </summary>
public enum AttackType
{
    /// <summary>
    /// 플레이어 기본 공격
    /// </summary>
    PlayerBaseAttack,

    /// <summary>
    /// 플레이어 스킬
    /// </summary>
    PlayerSkill,

    /// <summary>
    /// 적 기본 공격
    /// </summary>
    EnemyBaseAttack,

    /// <summary>
    /// 적 특수 공격
    /// </summary>
    EnemySpecialAttack
}
