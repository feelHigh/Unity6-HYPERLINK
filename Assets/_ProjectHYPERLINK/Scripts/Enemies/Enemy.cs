using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// 기본 적 클래스
/// 
/// 핵심 기능:
/// - 체력 관리
/// - 데미지 처리
/// - 사망 처리
/// - 경험치 보상
/// - 아이템 드랍
/// - 체력바 시각화 (Gizmo)
/// 
/// 작동 흐름:
/// 1. 플레이어가 적 공격
/// 2. TakeDamage() 호출
/// 3. 체력 감소 및 로그
/// 4. 체력 0 이하 시 Die() 호출
/// 5. 플레이어에게 경험치 지급
/// 6. 확률에 따라 아이템 드랍
/// 7. GameObject 파괴
/// 
/// 연동 시스템:
/// - PlayerNavController/SkillActivationSystem: 데미지 입음
/// - ExperienceManager: 경험치 지급
/// - ItemSpawner: 아이템 드랍
/// 
/// 확장 가능성:
/// - AI 추가 (추적, 공격 패턴)
/// - 엘리트/보스 몬스터 (상속)
/// - 버프/디버프 시스템
/// - 애니메이션 통합
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("적 스탯")]
    [SerializeField] private float _maxHealth = 100f;              // 최대 체력
    [SerializeField] private int _experienceReward = 10;           // 처치 시 경험치 보상

    [Header("드랍 설정")]
    [SerializeField] private ItemDropTableData _dropTable;         // 아이템 드랍 테이블
    [SerializeField][Range(0f, 1f)] private float _dropChance = 0.5f;  // 드랍 확률 (50%)

    private float _currentHealth;  // 현재 체력

    /// <summary>
    /// 적이 살아있는지 여부
    /// AI나 다른 시스템에서 확인용
    /// </summary>
    public bool IsAlive => _currentHealth > 0;

    private void Start()
    {
        // 게임 시작 시 체력을 최대치로 설정
        _currentHealth = _maxHealth;
    }

    /// <summary>
    /// 데미지 받기 (Public - 플레이어/스킬이 호출)
    /// 
    /// 호출 위치:
    /// - PlayerNavController.PerformDashAttack()
    /// - SkillActivationSystem.ExecuteMeleeSkill()
    /// - SkillActivationSystem.ExecuteAOESkill()
    /// 
    /// 처리 과정:
    /// 1. 체력 감소
    /// 2. 디버그 로그 출력
    /// 3. 체력 0 이하 체크
    /// 4. 사망 처리
    /// 
    /// Parameters:
    ///     damage: 받을 데미지량
    ///     
    /// 추후 개선사항:
    /// - 방어력 계산
    /// - 데미지 타입 (물리/마법)
    /// - 피격 애니메이션
    /// - 피격 이펙트
    /// - 넉백 효과
    /// </summary>
    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        Debug.Log($"{gameObject.name}이(가) {damage} 데미지를 받았습니다. 체력: {_currentHealth}");

        // 사망 체크
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 사망 처리
    /// 
    /// 실행 순서:
    /// 1. 사망 로그
    /// 2. 플레이어에게 경험치 지급
    /// 3. 아이템 드랍 (확률 기반)
    /// 4. GameObject 파괴
    /// 
    /// 경험치 시스템:
    /// - ExperienceManager를 씬에서 찾음
    /// - GainExperience() 호출로 경험치 지급
    /// - 레벨업 체크 자동 처리
    /// 
    /// 아이템 드랍 시스템:
    /// - _dropChance 확률로 드랍 여부 결정
    /// - ItemSpawner.SpawnItem() 호출
    /// - ItemDropTable로 아이템 등급 및 스탯 결정
    /// 
    /// 추후 개선사항:
    /// - 사망 애니메이션
    /// - 사망 이펙트/사운드
    /// - 시체 Ragdoll
    /// - 보상 팝업 UI
    /// - 콤보 시스템
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name}이(가) 사망했습니다!");

        // === 경험치 지급 ===
        ExperienceManager playerExperienceManager = FindFirstObjectByType<ExperienceManager>();
        if (playerExperienceManager != null)
        {
            playerExperienceManager.GainExperience(_experienceReward);
        }

        // === 아이템 드랍 ===
        // ItemSpawner와 드랍 테이블이 설정되어 있는지 확인
        if (ItemSpawner.Instance != null && _dropTable != null)
        {
            // 확률 체크 (0~1 사이 랜덤 값)
            if (UnityEngine.Random.Range(0f, 1f) < _dropChance)
            {
                // 적의 현재 위치에 아이템 드랍
                ItemSpawner.Instance.SpawnItem(transform.position, _dropTable);
                Debug.Log($"아이템 드랍!");
            }
        }

        // GameObject 파괴 (적 제거)
        Destroy(gameObject);
    }

    /// <summary>
    /// Gizmo를 사용한 체력바 시각화
    /// 
    /// Scene 뷰에서 실시간으로 체력 표시:
    /// - 적 머리 위 2.5유닛 위치에 체력바
    /// - 배경: 빨간색 (최대 체력)
    /// - 전경: 초록색 (현재 체력)
    /// - 체력이 줄어들면 초록색 바가 짧아짐
    /// 
    /// 시각화 구조:
    /// - 배경 바: 1유닛 너비 × 0.1유닛 높이 (빨간색)
    /// - 전경 바: (현재체력/최대체력) 비율로 너비 조절 (초록색)
    /// - 전경 바는 약간 더 두껍게 (0.12유닛) 표시
    /// 
    /// 작동 조건:
    /// - Application.isPlaying = true (게임 실행 중에만)
    /// - 에디터 전용 (빌드에는 미포함)
    /// 
    /// 사용 방법:
    /// - Play 모드 진입
    /// - Scene 뷰에서 적 선택
    /// - 머리 위 체력바 확인
    /// - 데미지 받을 때 실시간으로 감소하는 것 확인
    /// </summary>
    private void OnDrawGizmos()
    {
        // 게임 실행 중에만 그리기
        if (Application.isPlaying)
        {
            // 체력바 위치 (적 머리 위 2.5유닛)
            Vector3 healthBarPosition = transform.position + Vector3.up * 2.5f;

            // 체력 비율 계산 (0~1)
            float healthPercentage = _currentHealth / _maxHealth;

            // === 배경 (빨간색 - 최대 체력) ===
            Gizmos.color = Color.red;
            Gizmos.DrawCube(healthBarPosition, new Vector3(1f, 0.1f, 0.1f));

            // === 전경 (초록색 - 현재 체력) ===
            // 체력 비율에 따라 너비 조절
            Gizmos.color = Color.green;
            // X축 오프셋 계산 (왼쪽 정렬)
            // 체력이 50%면 0.25유닛 왼쪽으로 이동
            Gizmos.DrawCube(
                healthBarPosition - Vector3.right * (0.5f - healthPercentage * 0.5f),
                new Vector3(healthPercentage, 0.12f, 0.12f)
            );
        }
    }
}