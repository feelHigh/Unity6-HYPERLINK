using UnityEngine;
using System.Collections;

/// <summary>
/// 마우스 위치 시각적 인디케이터
/// 
/// 기능:
/// - 이동, 기본 공격, 스킬 사용 시 마우스 위치에 시각적 피드백 제공
/// - 오브젝트 풀링으로 성능 최적화
/// - 액션 타입별 다른 인디케이터 표시 가능
/// </summary>
public class MousePositionIndicator : MonoBehaviour
{
    public static MousePositionIndicator Instance { get; private set; }

    [Header("인디케이터 프리팹")]
    [Tooltip("이동 클릭 시 표시할 프리팹")]
    [SerializeField] private GameObject _moveIndicatorPrefab;

    [Tooltip("공격 클릭 시 표시할 프리팹")]
    [SerializeField] private GameObject _attackIndicatorPrefab;

    [Tooltip("스킬 사용 시 표시할 프리팹")]
    [SerializeField] private GameObject _skillIndicatorPrefab;

    [Header("설정")]
    [Tooltip("인디케이터 표시 시간 (초)")]
    [SerializeField] private float _displayDuration = 0.5f;

    [Tooltip("지면에서 약간 떠있는 높이 (Z-fighting 방지)")]
    [SerializeField] private float _heightOffset = 0.05f;

    [Tooltip("풀 크기 (각 타입별)")]
    [SerializeField] private int _poolSize = 5;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = false;

    // 오브젝트 풀
    private GameObject[] _moveIndicatorPool;
    private GameObject[] _attackIndicatorPool;
    private GameObject[] _skillIndicatorPool;

    private int _movePoolIndex = 0;
    private int _attackPoolIndex = 0;
    private int _skillPoolIndex = 0;

    #region 초기화

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializePools();
    }

    private void InitializePools()
    {
        // 이동 인디케이터 풀
        if (_moveIndicatorPrefab != null)
        {
            _moveIndicatorPool = new GameObject[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                _moveIndicatorPool[i] = Instantiate(_moveIndicatorPrefab, transform);
                _moveIndicatorPool[i].SetActive(false);
            }
        }

        // 공격 인디케이터 풀
        if (_attackIndicatorPrefab != null)
        {
            _attackIndicatorPool = new GameObject[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                _attackIndicatorPool[i] = Instantiate(_attackIndicatorPrefab, transform);
                _attackIndicatorPool[i].SetActive(false);
            }
        }

        // 스킬 인디케이터 풀
        if (_skillIndicatorPrefab != null)
        {
            _skillIndicatorPool = new GameObject[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                _skillIndicatorPool[i] = Instantiate(_skillIndicatorPrefab, transform);
                _skillIndicatorPool[i].SetActive(false);
            }
        }

        Log("오브젝트 풀 초기화 완료");
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 이동 인디케이터 표시
    /// </summary>
    public void ShowMoveIndicator(Vector3 worldPosition)
    {
        if (_moveIndicatorPrefab == null || _moveIndicatorPool == null)
        {
            Log("이동 인디케이터 프리팹이 설정되지 않았습니다.");
            return;
        }

        ShowIndicator(_moveIndicatorPool, ref _movePoolIndex, worldPosition, "이동");
    }

    /// <summary>
    /// 공격 인디케이터 표시
    /// </summary>
    public void ShowAttackIndicator(Vector3 worldPosition)
    {
        if (_attackIndicatorPrefab == null || _attackIndicatorPool == null)
        {
            Log("공격 인디케이터 프리팹이 설정되지 않았습니다.");
            return;
        }

        ShowIndicator(_attackIndicatorPool, ref _attackPoolIndex, worldPosition, "공격");
    }

    /// <summary>
    /// 스킬 인디케이터 표시
    /// </summary>
    public void ShowSkillIndicator(Vector3 worldPosition)
    {
        if (_skillIndicatorPrefab == null || _skillIndicatorPool == null)
        {
            Log("스킬 인디케이터 프리팹이 설정되지 않았습니다.");
            return;
        }

        ShowIndicator(_skillIndicatorPool, ref _skillPoolIndex, worldPosition, "스킬");
    }

    #endregion

    #region 내부 메서드

    private void ShowIndicator(GameObject[] pool, ref int poolIndex, Vector3 worldPosition, string actionType)
    {
        if (pool == null || pool.Length == 0) return;

        // 풀에서 다음 인디케이터 가져오기
        GameObject indicator = pool[poolIndex];
        poolIndex = (poolIndex + 1) % pool.Length;

        // 위치 설정 (약간 떠있도록)
        Vector3 spawnPosition = worldPosition;
        spawnPosition.y += _heightOffset;
        indicator.transform.position = spawnPosition;

        // 활성화
        indicator.SetActive(true);

        // 자동 비활성화
        StartCoroutine(DeactivateAfterDelay(indicator, _displayDuration));

        Log($"{actionType} 인디케이터 표시: {worldPosition}");
    }

    private IEnumerator DeactivateAfterDelay(GameObject indicator, float delay)
    {
        yield return WaitForSecondsCache.Get(delay);

        if (indicator != null)
        {
            indicator.SetActive(false);
        }
    }

    #endregion

    #region 디버그

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            DebugHelper.Log($"[MousePositionIndicator] {message}");
        }
    }

    #endregion

    #region 정리

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion
}
