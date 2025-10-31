using UnityEngine;

/// <summary>
/// 플레이어의 디버프 상태를 관리하는 클래스
/// 
/// 최근 변경사항:
/// - SetSlow() 메소드 추가 (이동속도 감소)
/// - SetWeaken() 메소드 추가 (방어력 감소)
/// - ResetAllStates() 메소드 추가
/// - 상태 프로퍼티 추가
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

    // 상태이상 수치
    public float SlowPercent { get; private set; } = 0f;
    public float WeakenPercent { get; private set; } = 0f;

    /// <summary>
    /// 빙결 상태
    /// - 이동/공격/스킬 모두 불가
    /// </summary>
    public void SetFreeze(bool active)
    {
        IsFrozen = active;
        IsStunned = active;
        CanMove = !active;
        CanAttack = !active;
        CanUseSkill = !active;

        Debug.Log($"[상태] 빙결: {active}");
    }

    /// <summary>
    /// 속박 상태
    /// - 이동만 불가
    /// </summary>
    public void SetRoot(bool active)
    {
        IsRoot = active;
        CanMove = !active;

        Debug.Log($"[상태] 속박: {active}");
    }

    /// <summary>
    /// 침묵 상태
    /// - 스킬만 불가
    /// </summary>
    public void SetSilence(bool active)
    {
        IsSilenced = active;
        CanUseSkill = !active;

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
        CanMove = !active;
        CanAttack = !active;
        CanUseSkill = !active;

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

        SlowPercent = 0f;
        WeakenPercent = 0f;

        Debug.Log("[상태] 모든 상태이상 초기화");
    }

    /// <summary>
    /// 현재 이동속도 배율 계산
    /// 1.0 = 100% (정상), 0.5 = 50% (둔화)
    /// </summary>
    public float GetMovementSpeedMultiplier()
    {
        if (!CanMove || IsStunned || IsFrozen || IsRoot)
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
        Debug.Log($"빙결: {IsFrozen}");
        Debug.Log($"속박: {IsRoot}");
        Debug.Log($"침묵: {IsSilenced}");
        Debug.Log($"둔화: {IsSlowed} ({SlowPercent}%)");
        Debug.Log($"약화: {IsWeakened} ({WeakenPercent}%)");
        Debug.Log($"넉다운: {IsStunned}");
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
