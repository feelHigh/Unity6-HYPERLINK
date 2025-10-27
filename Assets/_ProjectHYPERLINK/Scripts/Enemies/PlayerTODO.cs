/*
1. PlayerNavController.cs에서 해야 할 것
    - 변수 추가
    - Update() 변경
    - HandleMouseInput() 변경

[SerializeField] PlayerStateController _stateCotroller;

private void Update()
{
    if (_isDead) return;

    //상태이상 체크 (Freeze 또는 Knockdown)
    if (_stateCotroller.IsStunned)
    {
        _agent.isStopped = true;
        return;
    }

    HandleMouseInput();
    UpdateAnimator();

    if (_currentTarget != null)
    {
        FollowTarget();
    }
}

private void HandleMouseInput()
{
    if (_isPerformingSkill) return;

    //이동 제한(Root/Freeze) 체크
    if (!_stateCotroller.CanMove)
    {
        //오른쪽 클릭(공격)은 그대로 허용
        if (_stateCotroller.IsRoot == false)
            return;
    }

    if (Input.GetMouseButtonDown(0)) HandleLeftClick();
    if (Input.GetMouseButtonDown(1))
    {
        // ★ 공격 가능 여부 체크
        if (_stateCotroller.CanAttack)
            HandleRightClick();
    }
}
*/

/*
2. SkillActivationSystem.cs에서 해야 할 것
    - ActivateSkill() 수정

public void ActivateSkill(SkillData skill)
{
    //침묵 / 빙결 시 스킬 차단
    if (!_stateCotroller.CanUseSkill)
        return;
    //이하 동일
}
*/