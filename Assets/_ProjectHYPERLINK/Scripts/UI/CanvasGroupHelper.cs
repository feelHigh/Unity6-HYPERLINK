using UnityEngine;

/// <summary>
/// CanvasGroup 가시성/상호작용 관리 헬퍼 유틸리티
/// 
/// 모든 UI 윈도우/패널에서 일관된 CanvasGroup 제어를 위한 공통 메서드 제공
/// </summary>
public static class CanvasGroupHelper
{
    /// <summary>
    /// CanvasGroup의 가시성과 상호작용 설정
    /// </summary>
    /// <param name="canvasGroup">대상 CanvasGroup</param>
    /// <param name="visible">표시 여부</param>
    public static void SetVisible(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("[CanvasGroupHelper] CanvasGroup이 null입니다.");
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    /// <summary>
    /// CanvasGroup의 현재 가시성 확인
    /// </summary>
    /// <param name="canvasGroup">확인할 CanvasGroup</param>
    /// <returns>표시 중이면 true, 아니면 false</returns>
    public static bool IsVisible(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return false;

        return canvasGroup.alpha > 0f;
    }

    /// <summary>
    /// CanvasGroup의 가시성 토글
    /// </summary>
    /// <param name="canvasGroup">대상 CanvasGroup</param>
    /// <returns>토글 후 상태 (true = 표시됨)</returns>
    public static bool Toggle(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("[CanvasGroupHelper] CanvasGroup이 null입니다.");
            return false;
        }

        bool newState = !IsVisible(canvasGroup);
        SetVisible(canvasGroup, newState);
        return newState;
    }

    /// <summary>
    /// CanvasGroup이 올바르게 설정되었는지 검증
    /// </summary>
    /// <param name="canvasGroup">검증할 CanvasGroup</param>
    /// <param name="contextName">로깅용 컨텍스트 이름</param>
    /// <returns>유효하면 true</returns>
    public static bool Validate(CanvasGroup canvasGroup, string contextName = "")
    {
        if (canvasGroup == null)
        {
            Debug.LogError($"[CanvasGroupHelper] {contextName} CanvasGroup이 할당되지 않았습니다!");
            return false;
        }
        return true;
    }
}
