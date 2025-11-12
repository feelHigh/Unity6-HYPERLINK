using System;
using UnityEngine;

/// <summary>
/// 히트 VFX 설정 데이터 클래스
/// 
/// 목적: 플레이어와 적이 공격받을 때 표시되는 VFX의 위치, 회전, 크기를 세밀하게 제어
/// 
/// 주요 기능:
/// - VFX 프리팹 참조
/// - 위치 오프셋 (피격 위치 기준)
/// - 회전 오프셋 (파티클 방향 조정)
/// - 크기 배율 (이펙트 크기 조절)
/// - 수명 설정 (자동 제거 시간)
/// 
/// 사용 예시:
/// - 작은 적: 작은 히트 이펙트 (Size = 0.5)
/// - 큰 보스: 큰 히트 이펙트 (Size = 2.0)
/// - 헤드샷: 위쪽 오프셋 (Offset.y = 2.0)
/// </summary>
[Serializable]
public class HitVfxConfig
{
    [Header("VFX 프리팹")]
    [Tooltip("생성할 히트 VFX 프리팹")]
    [SerializeField] private GameObject _vfxPrefab;

    [Header("위치 및 회전")]
    [Tooltip("VFX 위치 오프셋 (피격 위치 기준 월드 좌표)")]
    [SerializeField] private Vector3 _positionOffset = Vector3.zero;

    [Tooltip("VFX 회전 오프셋 (오일러 각도)")]
    [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

    [Header("크기 설정")]
    [Tooltip("VFX 크기 배율 (1.0 = 원본 크기, 2.0 = 2배 크기)")]
    [Range(0.1f, 5f)]
    [SerializeField] private float _scale = 1f;

    [Header("수명 설정")]
    [Tooltip("VFX 자동 제거 시간 (초, 0이면 파티클 시스템 Duration 사용)")]
    [SerializeField] private float _lifetime = 0f;

    // 프로퍼티
    public GameObject VfxPrefab => _vfxPrefab;
    public Vector3 PositionOffset => _positionOffset;
    public Vector3 RotationOffset => _rotationOffset;
    public float Scale => _scale;
    public float Lifetime => _lifetime;

    /// <summary>
    /// 유효한 VFX 설정인지 확인
    /// </summary>
    public bool IsValid()
    {
        return _vfxPrefab != null;
    }

    /// <summary>
    /// 모든 설정을 포함하여 HitVfxConfig 생성
    /// </summary>
    public static HitVfxConfig Create(GameObject vfxPrefab, Vector3 positionOffset, Vector3 rotationOffset, float scale = 1f, float lifetime = 0f)
    {
        return new HitVfxConfig
        {
            _vfxPrefab = vfxPrefab,
            _positionOffset = positionOffset,
            _rotationOffset = rotationOffset,
            _scale = scale,
            _lifetime = lifetime
        };
    }
}
