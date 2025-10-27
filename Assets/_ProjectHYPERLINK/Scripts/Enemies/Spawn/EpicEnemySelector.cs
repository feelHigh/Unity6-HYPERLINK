using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에픽 몬스터 생성 관련 로직을 담당하는 클래스
/// </summary>
public class EpicEnemySelector : MonoBehaviour
{
    [Header("----- 에픽 확률 -----")]
    [Range(0, 1)]
    [SerializeField] float _epicChance = 0.15f;     //에픽 확률

    [Header("----- 특수 공격 리스트 -----")]
    [SerializeField] private List<SpecialAttackBase> _specialAttackList;    //특수 공격 리스트

    /// <summary>
    /// 에픽 확률에 따라 에픽인지 아닌지를 반환
    /// </summary>
    /// <returns>true면 에픽, false면 에픽 아님</returns>
    public bool TryBeEpic()
    {
        return Random.value < _epicChance;
    }

    /// <summary>
    /// 5개의 특수 공격 중 랜덤하게 하나를 반환
    /// </summary>
    /// <returns></returns>
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
