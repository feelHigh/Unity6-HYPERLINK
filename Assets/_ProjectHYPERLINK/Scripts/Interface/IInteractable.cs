using UnityEngine;

/// <summary>
/// 상호작용 가능한 오브젝트 인터페이스
/// 
/// 사용 목적:
/// - 레벨 디자이너가 플레이어 스크립트를 수정하지 않고 상호작용 오브젝트 생성
/// - 문, 상자, NPC, 레버 등 모든 상호작용 가능 오브젝트에 사용
/// - PlayerInteractionController가 이 인터페이스로 통일된 방식으로 처리
/// 
/// 구현 예시:
/// - Door: 문 열기/닫기
/// - Chest: 아이템 드랍
/// - NPC: 대화 시작
/// - Lever: 스위치 활성화
/// - TeleportPortal: 텔레포트
/// 
/// 사용 흐름:
/// 1. 레벨 디자이너가 Door.cs 같은 클래스 생성
/// 2. IInteractable 인터페이스 구현
/// 3. GameObject에 추가 및 Collider 설정
/// 4. PlayerInteractionController가 자동으로 감지
/// 5. 플레이어가 E키 누르면 Interact() 호출
/// 
/// Unity 인터페이스 장점:
/// - 다형성: 다양한 오브젝트를 같은 방식으로 처리
/// - 확장성: 새로운 상호작용 추가가 쉬움
/// - 분리: 플레이어 코드와 레벨 오브젝트 분리
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 상호작용 실행
    /// 
    /// 호출 시점:
    /// - 플레이어가 범위 내에서 E키(상호작용 키) 누를 때
    /// - PlayerInteractionController.Update()에서 호출
    /// 
    /// Parameters:
    ///     player: 상호작용을 시작한 플레이어 (필요 시 사용)
    ///     
    /// 구현 예시:
    /// - Door: 문 열기 애니메이션 재생
    /// - Chest: 아이템 드랍 및 열린 상태로 변경
    /// - NPC: 대화 UI 표시
    /// 
    /// 주의사항:
    /// - 이 메서드는 반드시 구현해야 함
    /// - 무거운 작업(로딩 등)은 비동기로 처리
    /// - 상태 변경 시 시각/사운드 피드백 제공 권장
    /// </summary>
    void Interact(PlayerCharacter player);

    /// <summary>
    /// 상호작용 프롬프트 텍스트 반환
    /// 
    /// 용도:
    /// - UI에 "Press E to Open Door" 같은 메시지 표시
    /// - InteractionPromptUI가 이 메서드 호출
    /// 
    /// Returns:
    ///     string: 표시할 텍스트 (예: "문 열기", "상자 열기", "대화하기")
    ///     
    /// 구현 예시:
    /// - Door (닫힘): "문 열기"
    /// - Door (열림): "문 닫기"
    /// - Chest (닫힘): "상자 열기"
    /// - Chest (열림): "" (이미 열림)
    /// - NPC: "대화하기"
    /// 
    /// 다국어 지원:
    /// - 현재는 한국어 하드코딩
    /// - 추후 LocalizationManager 통합 가능
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// 현재 상호작용 가능한지 확인
    /// 
    /// 용도:
    /// - 잠긴 문, 이미 열린 상자 등 상호작용 불가 상태 체크
    /// - UI 프롬프트 표시 여부 결정
    /// - 시각적 피드백 (회색 처리, 잠금 아이콘 등)
    /// 
    /// Parameters:
    ///     player: 상호작용 시도하는 플레이어
    ///     
    /// Returns:
    ///     true: 상호작용 가능 (E키 입력 시 Interact() 호출)
    ///     false: 상호작용 불가 (프롬프트 비활성화 또는 "잠김" 표시)
    ///     
    /// 구현 예시:
    /// - Door (잠김): false 반환, GetInteractionPrompt()는 "문이 잠겨있습니다"
    /// - Door (열림): true 반환 (닫을 수 있으면)
    /// - Chest (이미 열림): false 반환
    /// - NPC (퀘스트 조건 미충족): false 반환
    /// 
    /// 고급 사용:
    /// - 플레이어 레벨 체크
    /// - 인벤토리에 특정 아이템 있는지 체크
    /// - 퀘스트 진행도 체크
    /// </summary>
    bool CanInteract(PlayerCharacter player);

    /// <summary>
    /// 상호작용 가능 범위 반환 (단위: 미터)
    /// 
    /// 용도:
    /// - PlayerInteractionController가 거리 체크
    /// - 너무 멀면 프롬프트 숨김
    /// 
    /// Returns:
    ///     float: 상호작용 가능 거리 (Unity 유닛)
    ///     
    /// 권장 값:
    /// - 작은 오브젝트 (레버, 버튼): 1.5~2.0
    /// - 중간 오브젝트 (문, 상자): 2.5~3.0  
    /// - 큰 오브젝트 (NPC, 포탈): 3.5~4.0
    /// 
    /// 성능 최적화:
    /// - const 값 반환 권장 (계산 없이)
    /// - PlayerInteractionController가 매 프레임 호출
    /// 
    /// 구현 예시:
    /// public float GetInteractionRange() => 2.5f; // 간단한 방법
    /// public float GetInteractionRange() { return _interactionRange; } // 필드 사용
    /// </summary>
    float GetInteractionRange();
}

/// <summary>
/// 상호작용 타입 열거형 (선택 사항)
/// 
/// 용도:
/// - 상호작용 종류별로 다른 UI/사운드/이펙트 적용
/// - 통계 수집 (어떤 상호작용을 가장 많이 했는지)
/// - 튜토리얼 시스템 (특정 타입 상호작용 가이드)
/// 
/// 사용 예시:
/// public InteractionType GetInteractionType() => InteractionType.Door;
/// </summary>
public enum InteractionType
{
    None,           // 기본값
    Door,           // 문
    Chest,          // 상자
    NPC,            // 대화
    Lever,          // 레버/스위치
    Portal,         // 텔레포트
    Pickup,         // 아이템 줍기
    QuestObject,    // 퀘스트 오브젝트
    Crafting,       // 제작대
    Merchant        // 상인
}
