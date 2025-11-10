using UnityEngine;

/// <summary>
/// 플레이어의 디버프 상태 및 공격 상태를 관리하는 클래스
/// </summary>
public class PlayerStateController : MonoBehaviour
{
    // 행동 제어 플래그
    public bool CanMove { get; private set; } = true;
    public bool CanAttack { get; private set; } = true;
    public bool CanUseSkill { get; private set; } = true;

    // 상태이상 플래그
    public bool IsStunned { get; private set; } = false;
    public bool IsRoot { get; private set; } = false;
    public bool IsFrozen { get; private set; } = false;
    public bool IsSilenced { get; private set; } = false;
    public bool IsSlowed { get; private set; } = false;
    public bool IsWeakened { get; private set; } = false;
    public bool IsHitStunned { get; private set; } = false;

    // 공격 상태 구분
    public bool IsPerformingBaseAttack { get; private set; } = false;  // 기본 공격 중
    public bool IsPerformingSkill { get; private set; } = false;       // 스킬 실행 중

    // 레거시 호환성 유지
    public bool IsAttacking => IsPerformingBaseAttack || IsPerformingSkill;

    // 상태이상 수치
    public float SlowPercent { get; private set; } = 0f;
    public float WeakenPercent { get; private set; } = 0f;

    /// <summary>
    /// 기본 공격 상태 설정
    /// </summary>
    public void SetBaseAttacking(bool active)
    {
        // 활성화 시도 시 스킬 실행 중이면 차단
        if (active && IsPerformingSkill)
        {
            Debug.LogWarning("[상태] 스킬 실행 중이므로 기본 공격을 시작할 수 없습니다.");
            return;
        }

        IsPerformingBaseAttack = active;

        // 공격 상태에 따른 행동 제어
        UpdateActionFlags();

        Debug.Log($"[상태] 기본 공격 중: {active}");
    }

    /// <summary>
    /// 스킬 실행 상태 설정
    /// 
    /// 기능:
    /// - 기본 공격 중일 때 스킬 차단
    /// - 스킬 실행 중일 때 기본 공격 및 이동 차단
    /// </summary>
    public void SetSkillPerforming(bool active)
    {
        // 활성화 시도 시 기본 공격 중이면 차단
        if (active && IsPerformingBaseAttack)
        {
            Debug.LogWarning("[상태] 기본 공격 중이므로 스킬을 시작할 수 없습니다.");
            return;
        }

        IsPerformingSkill = active;

        // 공격 상태에 따른 행동 제어
        UpdateActionFlags();

        Debug.Log($"[상태] 스킬 실행 중: {active}");
    }

    /// <summary>
    /// 행동 제어 플래그 업데이트
    /// 
    /// 모든 공격 및 상태이상을 고려하여 CanMove/CanAttack/CanUseSkill 갱신
    /// </summary>
    private void UpdateActionFlags()
    {
        // 기본 이동 가능 여부
        bool canMoveBase = !IsStunned && !IsFrozen && !IsRoot && !IsHitStunned;

        // 공격 중에는 이동 불가
        if (IsPerformingBaseAttack || IsPerformingSkill)
        {
            CanMove = false;
        }
        else
        {
            CanMove = canMoveBase;
        }

        // 기본 공격 가능 여부
        // - 상태이상 없음
        // - 스킬 실행 중이 아님
        CanAttack = !IsStunned && !IsFrozen && !IsHitStunned && !IsPerformingSkill;

        // 스킬 사용 가능 여부
        // - 침묵/빙결/스턴 없음
        // - 기본 공격 중이 아님
        CanUseSkill = !IsSilenced && !IsStunned && !IsFrozen && !IsHitStunned && !IsPerformingBaseAttack;
    }

    /// <summary>
    /// 히트 스턴 상태
    /// - 피격 시 짧은 시간 동안 모든 행동 불가
    /// - 일반 몬스터 공격 시 적용
    /// </summary>
    public void SetHitStun(bool active)
    {
        IsHitStunned = active;
        IsStunned = active;
        UpdateActionFlags();

        Debug.Log($"[상태] 히트 스턴: {active}");
    }

    /// <summary>
    /// 빙결 상태
    /// - 이동/공격/스킬 모두 불가
    /// </summary>
    public void SetFreeze(bool active)
    {
        IsFrozen = active;
        IsStunned = active;
        UpdateActionFlags();

        Debug.Log($"[상태] 빙결: {active}");
    }

    /// <summary>
    /// 속박 상태 (Wood 속성 특수 공격)
    /// - 이동만 불가
    /// - 공격/스킬 가능
    /// </summary>
    public void SetRoot(bool active)
    {
        IsRoot = active;
        UpdateActionFlags();

        Debug.Log($"[상태] 속박: {active}");
    }

    /// <summary>
    /// 침묵 상태
    /// - 스킬만 불가
    /// </summary>
    public void SetSilence(bool active)
    {
        IsSilenced = active;
        UpdateActionFlags();

        Debug.Log($"[상태] 침묵: {active}");
    }

    /// <summary>
    /// 둔화 상태
    /// - 이동속도 감소
    /// </summary>
    public void SetSlow(bool active, float slowPercent)
    {
        IsSlowed = active;
        SlowPercent = active ? slowPercent : 0f;

        Debug.Log($"[상태] 둔화: {active} ({slowPercent}%)");
    }

    /// <summary>
    /// 약화 상태
    /// - 방어력 감소
    /// </summary>
    public void SetWeaken(bool active, float weakenPercent)
    {
        IsWeakened = active;
        WeakenPercent = active ? weakenPercent : 0f;

        Debug.Log($"[상태] 약화: {active} ({weakenPercent}%)");
    }

    /// <summary>
    /// 넉다운 상태
    /// - 이동/공격/스킬 모두 불가
    /// </summary>
    public void SetKnockState(bool active)
    {
        IsStunned = active;
        UpdateActionFlags();

        Debug.Log($"[상태] 넉다운: {active}");
    }

    /// <summary>
    /// 모든 상태이상 초기화
    /// </summary>
    public void ResetAllStates()
    {
        CanMove = true;
        CanAttack = true;
        CanUseSkill = true;

        IsStunned = false;
        IsRoot = false;
        IsFrozen = false;
        IsSilenced = false;
        IsSlowed = false;
        IsWeakened = false;
        IsPerformingBaseAttack = false;
        IsPerformingSkill = false;
        IsHitStunned = false;

        SlowPercent = 0f;
        WeakenPercent = 0f;

        Debug.Log("[상태] 모든 상태이상 초기화");
    }

    /// <summary>
    /// 현재 이동속도 배율 계산
    /// 1.0 = 100% (정상), 0.5 = 50% (둔화), 0.0 = 이동 불가
    /// </summary>
    public float GetMovementSpeedMultiplier()
    {
        if (!CanMove || IsStunned || IsFrozen || IsRoot || IsHitStunned)
            return 0f;

        if (IsSlowed)
            return 1f - (SlowPercent / 100f);

        return 1f;
    }

    /// <summary>
    /// 방어력 배율 계산
    /// 1.0 = 100% (정상), 0.7 = 70% (약화 30%)
    /// </summary>
    public float GetDefenseMultiplier()
    {
        if (IsWeakened)
            return 1f - (WeakenPercent / 100f);

        return 1f;
    }

    #region 디버그

    [ContextMenu("Debug: Print Current States")]
    private void DebugPrintStates()
    {
        Debug.Log("===== PlayerStateController 상태 =====");
        Debug.Log($"이동 가능: {CanMove}");
        Debug.Log($"공격 가능: {CanAttack}");
        Debug.Log($"스킬 사용 가능: {CanUseSkill}");
        Debug.Log("--- 공격 상태 ---");
        Debug.Log($"기본 공격 중: {IsPerformingBaseAttack}");
        Debug.Log($"스킬 실행 중: {IsPerformingSkill}");
        Debug.Log("--- 상태이상 ---");
        Debug.Log($"빙결: {IsFrozen}");
        Debug.Log($"속박: {IsRoot}");
        Debug.Log($"침묵: {IsSilenced}");
        Debug.Log($"둔화: {IsSlowed} ({SlowPercent}%)");
        Debug.Log($"약화: {IsWeakened} ({WeakenPercent}%)");
        Debug.Log($"넉다운: {IsStunned}");
        Debug.Log($"히트 스턴: {IsHitStunned}");
        Debug.Log($"이동속도 배율: {GetMovementSpeedMultiplier():P0}");
        Debug.Log($"방어력 배율: {GetDefenseMultiplier():P0}");
    }

    [ContextMenu("Test: Reset All States")]
    private void TestResetStates()
    {
        ResetAllStates();
    }

    #endregion
}
