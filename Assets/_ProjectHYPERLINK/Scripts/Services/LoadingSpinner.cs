using UnityEngine;

/// <summary>
/// 로딩 스피너 애니메이션
/// 
/// 간단한 회전 애니메이션:
/// - Z축 회전 (시계 반대 방향)
/// - 일정 속도 회전
/// 
/// 사용 위치:
/// - LoginController 로딩 패널
/// - CharacterSelectionController 로딩 패널
/// - 기타 로딩 화면
/// </summary>
public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 180f;  // 초당 회전 각도

    private void Update()
    {
        // Z축 회전 (시계 반대 방향)
        transform.Rotate(0f, 0f, -_rotationSpeed * Time.deltaTime);
    }
}