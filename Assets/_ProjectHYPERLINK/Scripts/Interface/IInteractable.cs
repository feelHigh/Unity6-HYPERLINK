using UnityEngine;

/// <summary>
/// 상호작용 가능한 오브젝트 인터페이스
/// 
/// ⭐ Task 3 업데이트:
/// - GetInteractionType() 추가
/// - GetInteractionName() 추가
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 상호작용 실행
    /// </summary>
    void Interact(PlayerCharacter player);

    /// <summary>
    /// 상호작용 프롬프트 텍스트 반환
    /// 더 이상 사용되지 않음 (호환성 유지용)
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// 상호작용 가능 여부
    /// </summary>
    bool CanInteract(PlayerCharacter player);

    /// <summary>
    /// 상호작용 가능 범위
    /// </summary>
    float GetInteractionRange();

    /// <summary>
    /// ⭐ 새 메서드: 상호작용 타입 반환
    /// 
    /// 반환값:
    /// - Door, Chest, NPC, Merchant 등
    /// 
    /// 사용처:
    /// - WorldSpaceInteractionUI가 타입별 아이콘 표시
    /// - 통계 수집
    /// </summary>
    InteractionType GetInteractionType();

    /// <summary>
    /// ⭐ 새 메서드: 상호작용 오브젝트 이름 반환
    /// 
    /// 반환값:
    /// - "문", "상자", "상인" 등의 한글 이름
    /// 
    /// 사용처:
    /// - WorldSpaceInteractionUI가 오브젝트 위에 표시
    /// 
    /// 예시:
    /// - Door: "문"
    /// - Chest: "보물상자"
    /// - NPC: "상인"
    /// - Portal: "포탈"
    /// </summary>
    string GetInteractionName();
}

/// <summary>
/// 상호작용 타입 열거형
/// </summary>
public enum InteractionType
{
    None,           // 기본값
    Door,           // 문
    Chest,          // 상자
    NPC,            // NPC 대화
    Lever,          // 레버/스위치
    Portal,         // 텔레포트
    Pickup,         // 아이템 줍기
    QuestObject,    // 퀘스트 오브젝트
    Crafting,       // 제작대
    Merchant        // 상인
}
