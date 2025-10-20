using UnityEngine;
using System.Collections.Generic;

public class EnemyGroup : MonoBehaviour
{
   [SerializeField] List<EnemyController> _enemies = new List<EnemyController>();
    bool _hasAggro = false;

    private void Start()
    {
        //자식으로 있는 모든 EnemyController를 찾아 리스트에 등록
        GetComponentsInChildren<EnemyController>(_enemies);
    }

    private void Update()
    {
        if (!_hasAggro) return;

        bool allMembersPatrolling = true;

        //모든 멤버들이 순찰 상태인지,아닌지 확인
        foreach (var enemy in _enemies)
        {
            if (enemy != null && enemy.CurState != EnemyController.EnemyState.Dead)
            {
                if (enemy.CurState != EnemyController.EnemyState.Patrol)
                {
                    allMembersPatrolling = false;
                    break;
                }
            }
        }

        //만약 모든 멤버가 순찰 상태라면 그룹의 어그로를 해제
        if (allMembersPatrolling)
        {
            _hasAggro = false;
        }
    }

    /// <summary>
    /// 멤버로부터 보고를 받아 그룹 전체에 어그로를 공유하는 함수
    /// </summary>
    /// <param name="target"></param>
    public void ShareAggro(Transform target)
    {
        //이미 어그로 상태이면 리턴
        if (_hasAggro) return;

        _hasAggro = true;

        //그룹 네 모든 멤버에게 타겟을 알리고 추격 명령을 내림
        foreach (var enemy in _enemies)
        {
            if (enemy != null && enemy.CurState != EnemyController.EnemyState.Dead)
            {
                enemy.ActivateChase(target);
            }
        }
    }
}
