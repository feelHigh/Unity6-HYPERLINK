using UnityEngine;

/// <summary>
/// 타겟(플레이어) 감지 및 대미지 처리 클래스
/// 
/// 사용처:
/// 근거리 공격 : 무기/타격 부위에 부착 (Animation Event로 제어)
/// 원거리 공격 : 발사체 프리팹에 부착 (생성 시 자동 활성화)
/// </summary>
public class TargetDetector : MonoBehaviour
{
    [Header("----- 설정 -----")]
    [SerializeField] Collider _detectorColl;            //감지 콜라이더

    float _damage;
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
    /// <param name="damage"></param>
    public void Initialize(float damage)
    {
        _damage = damage;
        _specialAttack = null;
    }

    /// <summary>
    /// 특수 공격 초기화
    /// </summary>
    /// <param name="specialAttack"></param>
    public void InitializeSA(SpecialAttackBase specialAttack)
    {
        _damage = 0;
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
        //플레이어와 충돌했을 때
        if (other.CompareTag("Player"))
        {
            PlayerCharacter player = other.GetComponent<PlayerCharacter>();

            if (player != null)
            {
                //특수 공격 데이터가 있으면 특수 공격
                if (_specialAttack != null)
                {
                    //player.ApplySpecialEffect(_specialAttack);

                    Debug.Log("[TergetDetector] 특수 공격!");
                }
                //없으면 그냥 일반 공격
                else
                {
                    player.TakeDamage(_damage);

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
                Debug.Log(other.gameObject.name);
            }
        }
    }
}
