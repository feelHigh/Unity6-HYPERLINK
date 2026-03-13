using UnityEngine;

/// <summary>
/// 조건부 디버그 로깅 유틸리티
/// ENABLE_DEBUG_LOG 정의 시 로그 출력, 미정의 시 호출부 포함 완전히 제거됨 (string 할당 포함)
/// 프로파일링 시 Scripting Define Symbols에서 ENABLE_DEBUG_LOG를 제거하면 GC 할당 없이 테스트 가능
/// </summary>
public static class DebugHelper
{
    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    public static void Log(string message)
    {
        Debug.Log(message);
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
}
