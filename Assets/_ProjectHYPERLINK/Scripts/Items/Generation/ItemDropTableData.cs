using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이템 드랍 테이블 ScriptableObject
/// 
/// 역할:
/// - 아이템 등급별 드랍 확률 정의
/// - 적 종류별 드랍 테이블 관리
/// 
/// 사용 흐름:
/// 1. Enemy에 드랍 테이블 할당
/// 2. 적 사망 시 RollItemQuality() 호출
/// 3. 확률에 따라 등급 결정
/// 4. ItemSpawner가 해당 등급으로 아이템 생성
/// 
/// 확률 시스템:
/// - 가중치 기반 랜덤
/// - 합계에서 비율로 계산
/// 
/// 예시:
/// - Normal: 60 → 60%
/// - Magic: 30 → 30%
/// - Rare: 10 → 10%
/// </summary>
[CreateAssetMenu(fileName = "ItemDropTable", menuName = "ScriptableObject/ItemDropTable")]
public class ItemDropTableData : ScriptableObject
{
    [Header("드랍 설정")]
    [SerializeField] private DropType _type;  // 드랍 타입 (Class1/2/3/Universal)

    [Header("등급별 드랍 확률 (순서: Normal, Magic, Rare, Epic, Legendary, Set)")]
    [Tooltip("각 등급의 드랍 확률 (가중치)")]
    [SerializeField] private List<int> _drops;  // 6개 등급의 확률

    public List<int> Drop => _drops;
    public DropType Type => _type;

    /// <summary>
    /// 아이템 등급 결정 (확률 기반)
    /// 
    /// 알고리즘:
    /// 1. 모든 가중치 합산
    /// 2. 0~합계 범위에서 랜덤 숫자
    /// 3. 누적 가중치로 등급 결정
    /// 
    /// Returns:
    ///     ItemQuality: 결정된 아이템 등급
    ///     
    /// 예시:
    /// - _drops = [60, 30, 10, 0, 0, 0]
    /// - 합계 = 100
    /// - 랜덤(0~100) = 75
    /// - 60 ≤ 75 < 90 → Magic 등급
    /// </summary>
    public ItemQuality RollItemQuality()
    {
        int totalWeight = 0;
        foreach (int weight in _drops)
        {
            totalWeight += weight;
        }

        int roll = Random.Range(0, totalWeight + 1);
        int currentWeight = 0;

        for (int i = 0; i < _drops.Count; i++)
        {
            currentWeight += _drops[i];
            if (roll <= currentWeight)
            {
                // 인덱스를 ItemQuality enum으로 변환
                return (ItemQuality)Mathf.Min(i, (int)ItemQuality.Set);
            }
        }

        return ItemQuality.Normal;  // 폴백
    }

    /// <summary>
    /// Inspector 유효성 검사
    /// 
    /// 자동 실행:
    /// - Inspector에서 값 변경 시
    /// - ScriptableObject 저장 시
    /// 
    /// 규칙:
    /// - 정확히 6개 등급 (Normal~Set)
    /// - 부족하면 0으로 채움
    /// - 초과하면 제거
    /// </summary>
    private void OnValidate()
    {
        // 6개 등급 강제
        while (_drops.Count < 6)
        {
            _drops.Add(0);
        }

        if (_drops.Count > 6)
        {
            _drops.RemoveRange(6, _drops.Count - 6);
        }
    }
}