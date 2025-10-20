using UnityEngine;

public class EffectOrbit : MonoBehaviour
{
    //공전
    Transform _center;
    float _orbitSpeed = 90f;
    float _orbitRadius = 1.5f;
    float _angle;

    //부유
    float _floatAmplitude;  //부유 높이
    float _floatSpeed;      //부유 속도
    float _floatOffset;     //랜덤 위상 오프셋

    Vector3 _baseOffset;    //공전 기준 오프셋

    public void Initialize(Transform center, float speed, float radius, float startAngle = 0f)
    {
        _center = center;
        _orbitSpeed = speed;
        _orbitRadius = radius;
        _angle = startAngle;

        _floatAmplitude = Random.Range(0.15f, 0.35f);
        _floatSpeed = Random.Range(1.0f, 1.8f);
        _floatOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (_center == null) return;

        //공전 각도 업데이트
        _angle += _orbitSpeed * Time.deltaTime;
        float rad = _angle * Mathf.Deg2Rad;

        //공전 위치 계산
        _baseOffset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * _orbitRadius;

        //부유 위치 계산
        float y = Mathf.Sin(Time.time * _floatSpeed + _floatOffset) * _floatAmplitude;

        //최종 위치
        transform.position = _center.position + _baseOffset + new Vector3(0, y, 0);
    }
}
