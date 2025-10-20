using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에픽 몬스터 생성 관련 로직을 담당하는 클래스
/// </summary>
public class EpicEnemySelector : MonoBehaviour
{
    [Header("----- 에픽 확률 -----")]
    [Range(0, 1)]
    [SerializeField] float _epicChance = 0.15f;

    [Header("----- 특수 공격 리스트 -----")]
    [SerializeField] private List<SpecialAttackBase> _specialAttackList;

    public bool TryBeEpic()
    {
        return Random.value < _epicChance;
    }

    public SpecialAttackBase GetRandomSpecialAttack()
    {
        if (_specialAttackList == null || _specialAttackList.Count == 0)
        {
            return null;
        }

        int ranIdx = Random.Range(0, _specialAttackList.Count);

        return _specialAttackList[ranIdx];
    }
}
