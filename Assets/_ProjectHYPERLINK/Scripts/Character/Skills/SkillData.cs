using UnityEngine;

/// <summary>
/// 스킬 데이터 ScriptableObject
/// 
/// 역할:
/// - 스킬의 모든 정보 정의
/// - 게임플레이 수치 (데미지, 쿨다운, 마나 등)
/// - UI 정보 (이름, 설명, 아이콘)
/// 
/// 스킬 시스템 구조:
/// 1. SkillData (이 클래스): 데이터 정의
/// 2. SkillActivationSystem: 실행 로직
/// 3. PlayerCharacter: 스킬 언락 관리
/// 4. SkillSlotUI: UI 표시
/// 
/// 생성 방법:
/// - Project 창: Create > Character > Skill Data
/// - 각 스킬마다 하나씩 생성 (Fireball, Heal, Charge 등)
/// 
/// 스킬 타입:
/// - Melee: 근거리 범위 공격
/// - Ranged: 원거리 투사체
/// - AreaOfEffect: 광역 공격
/// - Buff: 자신 강화
/// - Heal: 체력 회복
/// 
/// Diablo 3 스킬 시스템 차이:
/// - Diablo 3: 5개 룬으로 스킬 변형
/// - 이 프로젝트: 단순화된 스킬 (룬 없음)
/// - 추후 룬 시스템 추가 가능
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "Character/Skill Data")]
public class SkillData : ScriptableObject
{
    #region 스킬 기본 정보

    [Header("스킬 정보")]
    [SerializeField] private string _skillName;        // 스킬 이름 (예: "불덩이")
    [SerializeField] private string _description;      // 스킬 설명 (툴팁용)
    [SerializeField] private Sprite _skillIcon;        // 스킬 아이콘 (UI 표시)
    [SerializeField] private int _requiredLevel;       // 언락 필요 레벨 (2, 6, 10 등)

    #endregion

    #region 스킬 속성

    [Header("스킬 속성")]
    [SerializeField] private float _manaCost;          // 마나 소비량
    [SerializeField] private float _cooldown;          // 쿨다운 시간 (초)
    [SerializeField] private float _damage;            // 기본 데미지
    [SerializeField] private float _range;             // 사거리/범위
    [SerializeField] private SkillType _skillType;     // 스킬 타입 (Melee/Ranged/AOE/Buff/Heal)

    [Header("원거리 스킬 설정 (Ranged 타입 전용)")]
    [Tooltip("투사체 프리팹 (Projectile 스크립트 포함 필수)")]
    [SerializeField] private GameObject _projectilePrefab;

    [Header("버프 스킬 설정 (Buff 타입 전용)")]
    [Tooltip("버프 증가량 (주요 스탯에 추가)")]
    [SerializeField] private float _buffAmount;

    [Tooltip("버프 지속시간 (초)")]
    [SerializeField] private float _buffDuration = 10f;

    #endregion

    #region Public 프로퍼티

    /// <summary>
    /// 스킬 이름
    /// UI에 표시되는 이름
    /// </summary>
    public string SkillName => _skillName;

    /// <summary>
    /// 스킬 설명
    /// 툴팁에 표시되는 상세 설명
    /// 
    /// 예시:
    /// "적에게 화염 구체를 발사하여 100%의 화염 피해를 입힙니다."
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// 스킬 아이콘
    /// SkillSlotUI에서 표시
    /// </summary>
    public Sprite SkillIcon => _skillIcon;

    /// <summary>
    /// 언락 필요 레벨
    /// 
    /// 일반적인 레벨 분포:
    /// - 레벨 2: 첫 번째 스킬
    /// - 레벨 6: 두 번째 스킬
    /// - 레벨 10: 세 번째 스킬
    /// 
    /// PlayerCharacter.UnlockSkillsForLevel()에서 확인
    /// </summary>
    public int RequiredLevel => _requiredLevel;

    /// <summary>
    /// 마나 소비량
    /// 
    /// SkillActivationSystem.ActivateSkill()에서 체크:
    /// - 마나 부족: 스킬 사용 불가
    /// - 마나 충분: PlayerCharacter.TryConsumeMana() 호출
    /// 
    /// 밸런싱:
    /// - 강력한 스킬: 높은 마나 (30~50)
    /// - 약한 스킬: 낮은 마나 (10~20)
    /// </summary>
    public float ManaCost => _manaCost;

    /// <summary>
    /// 쿨다운 시간 (초)
    /// 
    /// 스킬 사용 후 다시 사용 가능할 때까지 대기 시간
    /// 
    /// SkillActivationSystem에서 관리:
    /// - 스킬 사용 시 쿨다운 시작
    /// - 매 프레임 시간 감소
    /// - 0 도달 시 재사용 가능
    /// 
    /// 밸런싱:
    /// - 강력한 스킬: 긴 쿨다운 (10~30초)
    /// - 약한 스킬: 짧은 쿨다운 (3~8초)
    /// - 기본 공격: 쿨다운 없음 (0초)
    /// </summary>
    public float Cooldown => _cooldown;

    /// <summary>
    /// 기본 데미지
    /// 
    /// 최종 데미지 계산:
    /// 1. 기본 데미지 (_damage)
    /// 2. 주요 스탯 보너스 (힘/지능/민첩)
    /// 3. 크리티컬 체크
    /// 
    /// SkillActivationSystem.CalculateSkillDamage()에서 계산
    /// 
    /// 예시:
    /// - 기본 데미지: 50
    /// - 주요 스탯: 100 (2배)
    /// - 크리티컬: 150% (2.5배)
    /// - 최종: 50 × 2 × 2.5 = 250
    /// 
    /// 힐 스킬의 경우:
    /// - _damage 값이 회복량을 의미
    /// </summary>
    public float Damage => _damage;

    /// <summary>
    /// 사거리/범위 (유닛)
    /// 
    /// 스킬 타입별 의미:
    /// - Melee/AOE: 플레이어 중심 반경
    /// - Ranged: 투사체 최대 거리
    /// - Buff/Heal: 영향 범위
    /// 
    /// Physics.OverlapSphere()에서 사용
    /// 
    /// 밸런싱:
    /// - 근거리: 2~5 유닛
    /// - 원거리: 10~20 유닛
    /// - 광역: 5~10 유닛
    /// </summary>
    public float Range => _range;

    /// <summary>
    /// 스킬 타입
    /// 
    /// SkillActivationSystem.ExecuteSkill()에서 분기:
    /// - Melee → ExecuteMeleeSkill()
    /// - Ranged → ExecuteRangedSkill()
    /// - AreaOfEffect → ExecuteAOESkill()
    /// - Buff → ExecuteBuffSkill()
    /// - Heal → ExecuteHealSkill()
    /// 
    /// 각 타입마다 다른 실행 로직
    /// </summary>
    public SkillType SkillType => _skillType;

    /// <summary>
    /// 투사체 프리팹
    /// 
    /// 원거리 스킬(Ranged) 전용
    /// 
    /// 요구사항:
    /// - GameObject에 Projectile 스크립트 필수
    /// - Rigidbody (Is Kinematic = true)
    /// - Collider (Is Trigger = true)
    /// - Trail Renderer (선택)
    /// 
    /// SkillActivationSystem.ExecuteRangedSkill()에서 사용
    /// 
    /// 설정 방법:
    /// 1. Projectile 프리팹 생성
    /// 2. Inspector에서 이 필드에 할당
    /// 3. SkillType을 Ranged로 설정
    /// </summary>
    public GameObject ProjectilePrefab => _projectilePrefab;

    /// <summary>
    /// 버프 증가량
    /// 
    /// 버프 스킬(Buff) 전용
    /// 
    /// 적용 방식:
    /// - Laon: Strength + BuffAmount
    /// - Sian: Intelligence + BuffAmount
    /// - Yujin: Dexterity + BuffAmount
    /// 
    /// 예시:
    /// - BuffAmount = 50: 주요 스탯 +50
    /// - BuffAmount = 100: 주요 스탯 +100
    /// 
    /// SkillActivationSystem.CreateBuffStats()에서 사용
    /// </summary>
    public float BuffAmount => _buffAmount;

    /// <summary>
    /// 버프 지속시간 (초)
    /// 
    /// 버프 스킬(Buff) 전용
    /// 
    /// 지속시간 동안:
    /// - BuffAmount만큼 스탯 증가
    /// - UI에 버프 아이콘 표시 (추후 구현)
    /// - 지속시간 종료 시 자동 제거
    /// 
    /// 권장값:
    /// - 약한 버프: 5-10초
    /// - 강한 버프: 15-30초
    /// - 궁극기 버프: 30-60초
    /// 
    /// SkillActivationSystem.ApplyTemporaryBuff()에서 사용
    /// </summary>
    public float BuffDuration => _buffDuration;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// Inspector 값 변경 시 유효성 검증 (Unity Editor 전용)
    /// 
    /// 검증 사항:
    /// - 원거리 스킬은 투사체 프리팹 필수
    /// - 버프 스킬은 BuffAmount와 BuffDuration > 0 필수
    /// </summary>
    private void OnValidate()
    {
        // 기존 검증 로직 유지
        _manaCost = Mathf.Max(0f, _manaCost);
        _cooldown = Mathf.Max(0f, _cooldown);
        _damage = Mathf.Max(0f, _damage);
        _range = Mathf.Max(0f, _range);

        // 신규 검증 추가
        _buffAmount = Mathf.Max(0f, _buffAmount);
        _buffDuration = Mathf.Max(0f, _buffDuration);

        // 타입별 경고
        if (_skillType == SkillType.Ranged && _projectilePrefab == null)
        {
            Debug.LogWarning($"[{_skillName}] 원거리 스킬은 Projectile 프리팹이 필요합니다!", this);
        }

        if (_skillType == SkillType.Buff)
        {
            if (_buffAmount <= 0)
            {
                Debug.LogWarning($"[{_skillName}] 버프 스킬은 BuffAmount > 0이어야 합니다!", this);
            }
            if (_buffDuration <= 0)
            {
                Debug.LogWarning($"[{_skillName}] 버프 스킬은 BuffDuration > 0이어야 합니다!", this);
            }
        }

        // 투사체 프리팹 검증
        if (_projectilePrefab != null && _skillType == SkillType.Ranged)
        {
            Projectile projectile = _projectilePrefab.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogError($"[{_skillName}] 투사체 프리팹에 Projectile 스크립트가 없습니다!", this);
            }
        }
    }
#endif
}

/// <summary>
/// 스킬 타입 열거형
/// 
/// 각 타입은 다른 실행 메커니즘을 가짐:
/// 
/// Melee (근거리):
/// - 플레이어 주변 범위 내 적 감지
/// - 즉시 데미지 적용
/// - 예: 검 베기, 회전 공격
/// 
/// Ranged (원거리):
/// - 투사체 생성 및 발사
/// - 투사체가 적에게 도달 시 데미지
/// - 예: 화살, 마법 탄환
/// 
/// AreaOfEffect (광역):
/// - 넓은 범위의 모든 적 타격
/// - 플레이어 중심 또는 타겟 위치
/// - 예: 폭발, 지진
/// 
/// Buff (버프):
/// - 플레이어 능력 강화
/// - 일정 시간 효과 유지
/// - 예: 공격력 증가, 이동속도 증가
/// 
/// Heal (회복):
/// - 체력 회복
/// - _damage 값이 회복량
/// - 예: 치유, 생명력 흡수
/// </summary>
public enum SkillType
{
    Melee,           // 근거리 공격
    Ranged,          // 원거리 공격
    AreaOfEffect,    // 광역 공격
    Buff,            // 버프
    Heal             // 회복
}
