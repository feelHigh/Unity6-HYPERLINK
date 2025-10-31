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
    bool _isActive = false;     //활성화 여부
    bool _hasHitPlayer = false; //플레이어를 공격했는지 여부

    /// <summary>
    /// 파동형 공격을 초기화하는 함수
    /// </summary>
    public void Initialize(SpecialAttackBase specialAttack)
    {
        _specialAttack = specialAttack;

        _curLength = 0f;
        _isActive = true;
        _hasHitPlayer = false;

        Destroy(gameObject, 2f);
    }

    private void Update()
    {
        if (!_isActive) return;

        //길이 증가
        _curLength += _expandSpeed * Time.deltaTime;
        _curLength = Mathf.Min(_curLength, _maxDistance);

        //플레이어가 공격에 맞지 않았을 때에만
        if (!_hasHitPlayer)
        {
            //RayCast로 전방 체크
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _curLength, _playerLayerMask))
            {
                Debug.Log("플레이어 레이어 감지");
            }

            //OverlabBox로 범위 내 플레이어 감지
            Vector3 boxCenter = transform.position + transform.forward * (_curLength * 0.5f);
            Collider[] colls = Physics.OverlapBox(boxCenter,
                new Vector3(_boxHalfExtents.x, _boxHalfExtents.y, _curLength * 0.5f),
                transform.rotation, _playerLayerMask);

            foreach (Collider coll in colls)
            {
                Debug.Log($"[WaveAttack] 얼음 공격 적중 {coll.name}");

                PlayerCombat player = coll.GetComponent<PlayerCombat>();
                if (player != null)
                {
                    player.ApplySpecialEffect(_specialAttack);

                    _hasHitPlayer = true;
                    break;
                }
            }
        }

        //최대 거리 도달 시 비활성화
        if (_curLength >= _maxDistance)
        {
            _isActive = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (!_isActive) return;

        Gizmos.color = new Color(0, 0.6f, 1f, 0.25f);
        Vector3 boxCenter = transform.position + transform.forward * (_curLength * 0.5f);
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, new Vector3(_boxHalfExtents.x * 2, _boxHalfExtents.y * 2, _curLength));
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
