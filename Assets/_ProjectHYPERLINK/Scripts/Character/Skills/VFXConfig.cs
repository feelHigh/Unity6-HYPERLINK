using System;
using UnityEngine;

/// <summary>
/// VFX 설정 데이터 클래스
/// 
/// 용도: 각 VFX 프리팹의 생성 타이밍, 위치, 회전, 수명 설정
/// - 여러 VFX를 독립적으로 제어 가능
/// - Unity Inspector에서 직관적으로 설정 가능
/// </summary>
[Serializable]
public class VfxConfig
{
    [Header("VFX 프리팹")]
    [Tooltip("생성할 VFX 프리팹")]
    [SerializeField] private GameObject _vfxPrefab;

    [Header("타이밍 설정")]
    [Tooltip("VFX 생성 타이밍 (0.0 ~ 1.0, 애니메이션 진행률)")]
    [Range(0f, 1f)]
    [SerializeField] private float _spawnTiming = 0.5f;

    [Header("위치 및 회전")]
    [Tooltip("VFX 위치 오프셋 (캐릭터 로컬 좌표)")]
    [SerializeField] private Vector3 _positionOffset = new Vector3(0f, 1f, 1f);

    [Tooltip("VFX 회전 오프셋 (오일러 각도)")]
    [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

    [Header("수명 설정")]
    [Tooltip("VFX 자동 제거 시간 (초, 0이면 파티클 시스템 Duration 사용)")]
    [SerializeField] private float _lifetime = 0f;

    [Header("부모 설정")]
    [Tooltip("true면 캐릭터의 자식으로 생성 (캐릭터를 따라다님)")]
    [SerializeField] private bool _attachToCharacter = false;

    // 프로퍼티
    public GameObject VfxPrefab => _vfxPrefab;
    public float SpawnTiming => _spawnTiming;
    public Vector3 PositionOffset => _positionOffset;
    public Vector3 RotationOffset => _rotationOffset;
    public float Lifetime => _lifetime;
    public bool AttachToCharacter => _attachToCharacter;

    /// <summary>
    /// 유효한 VFX 설정인지 확인
    /// </summary>
    public bool IsValid()
    {
        return _vfxPrefab != null;
    }
}
