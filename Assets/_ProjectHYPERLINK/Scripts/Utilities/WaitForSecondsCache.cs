using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WaitForSeconds 캐시 유틸리티
/// 동일한 대기 시간에 대해 WaitForSeconds 인스턴스를 재사용하여 GC 압박 감소
///
/// 사용법: yield return WaitForSecondsCache.Get(0.5f);
/// </summary>
public static class WaitForSecondsCache
{
    private static readonly Dictionary<float, WaitForSeconds> _cache = new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds Get(float seconds)
    {
        if (!_cache.TryGetValue(seconds, out WaitForSeconds wait))
        {
            wait = new WaitForSeconds(seconds);
            _cache[seconds] = wait;
        }
        return wait;
    }
}
