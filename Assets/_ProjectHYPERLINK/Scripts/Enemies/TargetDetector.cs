using UnityEngine;

/// <summary>
/// 타겟(플레이어) 감지 및 대미지 처리 클래스
/// 
/// 사용처:
/// 근거리 공격 : 무기/타격 부위에 부착 (Animation Event로 제어)
/// 원거리 공격 : 발사체 프리팹에 부착 (생성 시 자동 활성화)
/// 
/// 변경사항:
/// - AttackInfo를 사용한 데미지 전달
/// - 히트 VFX 지원
/// </summary>
public class TargetDetector : MonoBehaviour
{
    [Header("----- 설정 -----")]
    [SerializeField] Collider _detectorColl;            //감지 콜라이더

    float _damage;
    GameObject _hitVfx;
    SpecialAttackBase _specialAttack;

    private void Awake()
    {
        if (_detectorColl == null)
        {
            _detectorColl = GetComponent<Collider>();
        }

        DisableDetector();
    }

    /// <summary>
    /// 일반 공격 초기화
    /// </summary>
    /// <param name="damage">데미지 양</param>
    /// <param name="hitVfx">히트 VFX 프리팹 (선택)</param>
    public void Initialize(float damage, GameObject hitVfx = null)
    {
        _damage = damage;
        _hitVfx = hitVfx;
        _specialAttack = null;
    }

    /// <summary>
    /// 특수 공격 초기화
    /// </summary>
    /// <param name="specialAttack">특수 공격 데이터</param>
    public void InitializeSA(SpecialAttackBase specialAttack)
    {
        _damage = 0;
        _hitVfx = null;
        _specialAttack = specialAttack;
    }

    /// <summary>
    /// 감지 콜라이더 활성화 하는 함수
    /// </summary>
    public void EnableDetector()
    {
        if (_detectorColl != null)
        {
            _detectorColl.enabled = true;
            Debug.Log("[TergetDetector] 콜라이더 활성화");
        }
        else
        {
            Debug.Log("[TergetDetector] 콜라이더가 없습니다.");
        }
    }

    /// <summary>
    /// 감지 콜라이더 비활성화 하는 함수
    /// </summary>
    void DisableDetector()
    {
        if (_detectorColl != null)
        {
            _detectorColl.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name + other.gameObject.transform);

        //플레이어와 충돌했을 때
        if (other.CompareTag("Player"))
        {
            Debug.Log("[TergetDetector] 플레이어 감지");
            PlayerCombat player = other.GetComponent<PlayerCombat>();

            if (player != null)
            {
                //특수 공격 데이터가 있으면 특수 공격
                if (_specialAttack != null)
                {
                    player.ApplySpecialEffect(_specialAttack, transform.position);

                    Debug.Log("[TergetDetector] 특수 공격!");
                }
                //없으면 그냥 일반 공격 (AttackInfo 사용)
                else
                {
                    AttackInfo attackInfo = AttackInfo.CreateEnemyBaseAttack(
                        _damage,
                        other.transform.position,
                        _hitVfx
                    );
                    player.TakeDamage(attackInfo);

                    Debug.Log("[TergetDetector] 기본 공격!");
                }

                //타격 성공 시 콜라이더 비활성화
                DisableDetector();

                //발사체인 경우 파괴
                if (GetComponent<EnemyProjectile>() != null)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.Log("[TergetDetector] PlayerCombat을 찾을 수 없습니다.");
            }
        }
        //적 동료는 무시
        else if (other.CompareTag("Enemy"))
        {
            return;
        }
        //벽이나 다른 오브젝트에 부딪히면
        else
        {
            //발사체인 경우에만 파괴
            if (GetComponent<EnemyProjectile>() != null)
            {
                Destroy(gameObject);
                Debug.Log(other.gameObject.name + other.gameObject.transform);
            }
        }
    }
}
