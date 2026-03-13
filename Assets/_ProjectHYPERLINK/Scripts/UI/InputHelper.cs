using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 입력 헬퍼 유틸리티
/// 
/// 역할:
/// - UI 클릭 감지
/// - 월드 입력과 UI 입력 구분
/// - EventSystem 통합
/// 
/// 사용법:
/// if (!InputHelper.IsPointerOverUI())
/// {
///     // 월드 입력 처리
/// }
/// </summary>
public static class InputHelper
{
    /// <summary>
    /// 마우스 포인터가 UI 위에 있는지 확인
    /// 
    /// 설명:
    /// - PC: EventSystem.IsPointerOverGameObject() 사용
    /// - 모바일: 터치 ID를 고려한 체크
    /// - Canvas의 GraphicRaycaster가 있는 모든 UI 감지
    /// 
    /// 주의사항:
    /// - Canvas에 GraphicRaycaster가 필요함
    /// - EventSystem이 씬에 있어야 함
    /// </summary>
    public static bool IsPointerOverUI()
    {
        // EventSystem이 없으면 false 반환 (UI 없음)
        if (EventSystem.current == null)
        {
            return false;
        }

        // PC: 마우스 포인터 체크
        if (Input.touchCount == 0)
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        // 모바일: 첫 번째 터치 체크
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }

        return false;
    }

    /// <summary>
    /// 특정 터치 ID가 UI 위에 있는지 확인 (모바일용)
    /// </summary>
    public static bool IsPointerOverUI(int touchId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject(touchId);
    }

    /// <summary>
    /// 현재 포인터가 가리키는 UI 오브젝트 반환
    /// 
    /// 사용 예:
    /// GameObject uiObject = InputHelper.GetUIObjectUnderPointer();
    /// if (uiObject != null)
    /// {
    ///     DebugHelper.Log($"UI 클릭: {uiObject.name}");
    /// }
    /// </summary>
    public static GameObject GetUIObjectUnderPointer()
    {
        if (EventSystem.current == null)
        {
            return null;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count > 0 ? results[0].gameObject : null;
    }

    /// <summary>
    /// 마우스 클릭이 월드에서 발생했는지 확인
    /// (UI가 아닌 곳을 클릭한 경우)
    /// </summary>
    public static bool IsWorldClick()
    {
        return Input.GetMouseButtonDown(0) && !IsPointerOverUI();
    }

    /// <summary>
    /// 디버그용: 현재 마우스 위치의 UI 정보 출력
    /// </summary>
    public static void DebugPrintUIUnderPointer()
    {
        if (EventSystem.current == null)
        {
            DebugHelper.Log("[InputHelper] EventSystem이 없습니다!");
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0)
        {
            DebugHelper.Log("[InputHelper] UI 없음");
            return;
        }

        DebugHelper.Log($"[InputHelper] 감지된 UI: {results.Count}개");
        for (int i = 0; i < results.Count; i++)
        {
            DebugHelper.Log($"  [{i}] {results[i].gameObject.name} (Depth: {results[i].depth})");
        }
    }
}
