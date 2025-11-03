using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 중인 스킬 시각화
/// 
/// 역할:
/// - 마우스 커서를 따라다니는 스킬 아이콘 표시
/// - 배치 가능 여부 시각 피드백 (색상 변경)
/// - 드래그 중 임시 표시용
/// 
/// 동작 과정:
/// 1. OnBeginDrag: 원본 스킬 정보 복사, 활성화
/// 2. OnDrag: 마우스 위치 추적, 색상 변경
/// 3. OnEndDrag: 비활성화
/// 
/// 색상 피드백:
/// - White: 배치 가능
/// - Red: 배치 불가 (슬롯 꽉 참, 중복)
/// - Yellow: 동일 슬롯
/// 
/// 생명주기:
/// - 씬에 항상 존재 (비활성 상태)
/// - 드래그 시에만 활성화
/// - 드래그 종료 시 비활성화
/// 
/// 사용처: SkillDragDropHandler
/// </summary>
public class DraggingVisualizeSkill : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private RectTransform _rect;

    [Header("색상 설정")]
    [SerializeField] private Color _validColor = Color.white;
    [SerializeField] private Color _invalidColor = Color.red;
    [SerializeField] private Color _sameSlotColor = Color.yellow;

    private SkillData _currentSkill;

    /// <summary>
    /// 드래그 중인 스킬
    /// </summary>
    public SkillData CurrentSkill => _currentSkill;

    /// <summary>
    /// 드래그 시작 시 호출
    /// 원본 스킬의 아이콘 복사
    /// 
    /// 호출: SkillDragDropHandler.OnBeginDrag()
    /// </summary>
    public void Spawn(SkillData skillData)
    {
        _currentSkill = skillData;

        if (_currentSkill != null && _currentSkill.SkillIcon != null)
        {
            _image.sprite = _currentSkill.SkillIcon;
            _image.enabled = true;

            // 기본 크기 설정 (스킬 슬롯 크기와 동일하게)
            _rect.sizeDelta = new Vector2(64f, 64f);
        }
        else
        {
            _image.enabled = false;
        }

        // 초기 색상: 유효
        _image.color = _validColor;
    }

    /// <summary>
    /// 마우스 위치로 이동
    /// 
    /// 호출: SkillDragDropHandler.OnDrag()
    /// </summary>
    public void UpdatePosition(Vector2 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// 배치 가능 여부에 따라 색상 변경
    /// 
    /// 호출: SkillDragDropHandler.OnDrag()
    /// 
    /// 색상:
    /// - Valid: White (배치 가능)
    /// - Invalid: Red (배치 불가)
    /// - SameSlot: Yellow (동일 슬롯)
    /// </summary>
    public void SetValidState(bool isValid, bool isSameSlot = false)
    {
        if (isSameSlot)
        {
            _image.color = _sameSlotColor;
        }
        else if (isValid)
        {
            _image.color = _validColor;
        }
        else
        {
            _image.color = _invalidColor;
        }
    }

    /// <summary>
    /// 드래그 종료 시 정리
    /// </summary>
    public void Clear()
    {
        _currentSkill = null;
        _image.sprite = null;
        _image.enabled = false;
    }
}
