using UnityEngine;
using System.Collections;

public class WaveAttackController : MonoBehaviour
{
    [SerializeField] float _maxDistance = 7.5f;     //최종 길이
    [SerializeField] float _expandSpeed = 3.25f;    //초당 늘어나는 속도
    [SerializeField] Vector3 _boxHalfExtents = new Vector3(0.5f, 1f, 0.5f);     //공격 면적
    [SerializeField] LayerMask _playerLayerMask;    //감지할 플레이어 레이어 마스크

    SpecialAttackBase _specialAttack;
    float _curLength = 0f;      //현재 공격 길이
    bool _isActive = false;     //공격 실행 여부
    bool _isHitEnvironment = false;

    /// <summary>
    /// 파동형 공격을 초기화하는 함수
    /// </summary>
    public void Initialize(SpecialAttackBase specialAttack)
    {
        _specialAttack = specialAttack;
        _isActive = true;

        Destroy(gameObject, 2f);
    }

    private void Update()
    {
        if (!_isActive) return;

        //길이 증가
        _curLength += _expandSpeed * Time.deltaTime;
        _curLength = Mathf.Min(_curLength, _maxDistance);

        //RayCast로 전방 체크
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, _curLength, _playerLayerMask))
        {
            Debug.Log("플레이어 레이어 감지");
        }

        
    }

    private void OnTriggerEnter(Collider other)
    {
        //이펙트가 지속되는 동안 플레이어가 닿으면
        if (other.CompareTag("Player"))
        {
            PlayerCombat player = other.GetComponent<PlayerCombat>();
            if (player != null && _specialAttack != null)
            {
                //Player의 ApplySpecialEffect 호출
                player.ApplySpecialEffect(_specialAttack);

                //한 번만 맞도록 이 스크립트와 콜라이더를 비활성화
                GetComponent<Collider>().enabled = false;
                enabled = false;
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            return;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        
    }
}
