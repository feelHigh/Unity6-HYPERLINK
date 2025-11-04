using UnityEngine;
using System;

/// <summary>
/// 보스 컨트롤러
///
/// 주요 기능:
/// - 보스 등장/사망 이벤트
/// </summary>
public class BossController : MonoBehaviour, IDamageable
{
    [Header("----- 보스 데이터 -----")]
    [SerializeField] BossData _data;

    [Header("----- 컴포넌트 참조 -----")]
    [SerializeField] Collider _collider;

    //이벤트
    public event Action OnInitialized;      //초기화 완료
    public event Action OnHit;              //피격
    public event Action OnDie;              //사망

    //현재 스탯
    float _maxHp;
    float _curHp;
    float _atk;
    float _moveSpeed;

    bool _isDead = false;

    //프로퍼티
    public BossData Data => _data;
    public float Atk => _atk;
    public float MoveSpeed => _moveSpeed;
    public float MaxHp => _maxHp;
    public float CurHp => _curHp;
    public bool IsDead => _isDead;

    private void Start()
    {
        InitializeBoss();
    }

    /// <summary>
    /// 보스 초기화
    /// </summary>
    public void InitializeBoss()
    {
        //스탯 초기화
        _maxHp = _data.MaxHp;
        _curHp = _maxHp;
        _atk = _data.Atk;
        _moveSpeed = _data.MoveSpeed;

        _isDead = false;

        Debug.Log($"[Boss] {_data.BossName} 초기화 완료");

        //초기화 이벤트 발행
        OnInitialized?.Invoke();
    }

    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        _curHp -= damage;
        _curHp = Mathf.Max(_curHp, 0);

        Debug.Log($"[Boss] 데미지 받음: {damage} (현재 체력: {_curHp}/{_maxHp})");

        //피격 이벤트
        OnHit?.Invoke();

        //사망 체크
        if (_curHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    public void Die()
    {
        if (_isDead) return;

        _isDead = true;
        Debug.Log($"[Boss] {_data.BossName} 사망!");

        //콜라이더 비활성화
        if (_collider != null)
        {
            _collider.enabled = false;
        }

        //사망 이벤트
        OnDie?.Invoke();

        //TODO: 보스 보상 처리 (경험치, 아이템, 골드?)
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        //보스 체력바 (일반 적보다 크고 눈에 띄게)
        Vector3 healthBarPos = transform.position + Vector3.up * 4f;
        float healthPercent = _curHp / _maxHp;

        //배경 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawCube(healthBarPos, new Vector3(2f, 0.2f, 0.1f));

        //전경 (노란색 - 보스는 노란색으로 표시)
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(
            healthBarPos - Vector3.right * (1f - healthPercent),
            new Vector3(healthPercent * 2f, 0.25f, 0.12f)
        );
    }
}
