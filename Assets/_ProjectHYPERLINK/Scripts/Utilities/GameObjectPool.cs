using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범용 게임오브젝트 풀링 시스템
///
/// 기능:
/// - 프리팹 기반 오브젝트 풀 (Dictionary 키 = prefab InstanceID)
/// - Get(prefab, position, rotation) / Release(instance) API
/// - 자동 반환 타이머 (Destroy(obj, lifetime) 패턴 대체)
/// - 자동 확장 (고정 크기 제한 없음)
/// </summary>
public class GameObjectPool : MonoBehaviour
{
    static GameObjectPool _instance;
    public static GameObjectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("[GameObjectPool]");
                _instance = go.AddComponent<GameObjectPool>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 프리팹 InstanceID -> 풀 큐
    readonly Dictionary<int, Queue<GameObject>> _pools = new Dictionary<int, Queue<GameObject>>();

    // 활성 인스턴스 -> 원본 프리팹 InstanceID (Release 시 어느 풀로 돌려보낼지)
    readonly Dictionary<int, int> _activeInstances = new Dictionary<int, int>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 풀에서 오브젝트를 가져온다. 풀이 비어있으면 새로 생성한다.
    /// </summary>
    /// <param name="prefab">원본 프리팹</param>
    /// <param name="position">생성 위치</param>
    /// <param name="rotation">생성 회전</param>
    /// <returns>활성화된 게임 오브젝트</returns>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int prefabId = prefab.GetInstanceID();
        GameObject instance = null;

        if (_pools.TryGetValue(prefabId, out Queue<GameObject> pool))
        {
            // 파괴된 오브젝트 건너뛰기
            while (pool.Count > 0)
            {
                instance = pool.Dequeue();
                if (instance != null) break;
                instance = null;
            }
        }

        if (instance == null)
        {
            instance = Instantiate(prefab, position, rotation);
        }
        else
        {
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = prefab.transform.localScale;
            instance.SetActive(true);
        }

        _activeInstances[instance.GetInstanceID()] = prefabId;
        return instance;
    }

    /// <summary>
    /// 부모 트랜스폼을 지정하여 풀에서 오브젝트를 가져온다.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject instance = Get(prefab, position, rotation);
        instance.transform.SetParent(parent);
        return instance;
    }

    /// <summary>
    /// 오브젝트를 풀로 반환한다.
    /// </summary>
    /// <param name="instance">반환할 게임 오브젝트</param>
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        int instanceId = instance.GetInstanceID();
        if (!_activeInstances.TryGetValue(instanceId, out int prefabId))
        {
            // 풀에서 관리하지 않는 오브젝트면 그냥 파괴
            Destroy(instance);
            return;
        }

        _activeInstances.Remove(instanceId);
        instance.SetActive(false);
        instance.transform.SetParent(transform);

        if (!_pools.TryGetValue(prefabId, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _pools[prefabId] = pool;
        }

        pool.Enqueue(instance);
    }

    /// <summary>
    /// 지연 후 자동으로 풀에 반환한다.
    /// Destroy(obj, lifetime) 패턴을 대체한다.
    /// </summary>
    /// <param name="instance">반환할 게임 오브젝트</param>
    /// <param name="delay">반환까지의 지연 시간 (초)</param>
    public void Release(GameObject instance, float delay)
    {
        if (instance == null) return;
        StartCoroutine(DelayedRelease(instance, delay));
    }

    IEnumerator DelayedRelease(GameObject instance, float delay)
    {
        yield return WaitForSecondsCache.Get(delay);
        Release(instance);
    }

    /// <summary>
    /// 씬 전환 시 비활성 풀 오브젝트 정리 (DontDestroyOnLoad 외)
    /// </summary>
    public void ClearPool(GameObject prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (_pools.TryGetValue(prefabId, out Queue<GameObject> pool))
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
    }
}
