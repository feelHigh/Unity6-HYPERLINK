using UnityEngine;

/// <summary>
/// 플레이어의 디버프 상태를 관리하는 클래스
/// </summary>
public class PlayerStateController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;
    public bool CanAttack { get; private set; } = true;
    public bool CanUseSkill { get; private set; } = true;
    public bool IsStunned { get; private set; } = false;
    public bool IsRoot { get; private set; } = false;

    /// <summary>
    /// 빙결 상태
    /// </summary>
    public void SetFreeze(bool active)
    {
        IsStunned = active;
        CanMove = !active;
        CanAttack = !active;
        CanUseSkill = !active;
    }

    /// <summary>
    /// 속박 상태
    /// </summary>
    public void SetRoot(bool active)
    {
        IsRoot = active;
        CanMove = !active;
    }

    /// <summary>
    /// 침묵 상태
    /// </summary>
    public void SetSilence(bool active)
    {
        CanUseSkill = !active;
    }

    public void SetWeaken(bool active)
    {
        // 약화는 PlayerCharacter에서 방어력만 제어할 예정
    }

    /// <summary>
    /// 넉다운 상태
    /// </summary>
    /// <param name="active"></param>
    public void SetKnockState(bool active)
    {
        IsStunned = active;
        CanMove = !active;
        CanAttack = !active;
        CanUseSkill = !active;
    }
}
