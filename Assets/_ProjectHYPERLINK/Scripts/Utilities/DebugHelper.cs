using UnityEngine;

/// <summary>
/// 조건부 디버그 로깅 유틸리티
/// 에디터에서만 로그 출력, 빌드에서는 완전히 제거됨 (string 할당 포함)
/// </summary>
public static class DebugHelper
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(string message)
    {
        Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
}
