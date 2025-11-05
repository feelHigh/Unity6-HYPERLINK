using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "MapConfig",menuName ="ScriptableObject/Map")]
public class MapConfig : ScriptableObject
{
    [Header("기본 설정")]
    [SerializeField] MapGround _groundPrefab;
    [SerializeField] Material _normalMaterial;
    [SerializeField] Color[] _color;
    [SerializeField] Material[] _tileMaterials;
    [SerializeField] Material[] _wallMaterials;
    [SerializeField] GameObject _doorPrefab;
    [SerializeField] GameObject _potion;
    [SerializeField] GameObject _portal;
    [SerializeField] GameObject _enterAndExit;

    public MapGround Ground => _groundPrefab;
    public Material NormalMaterial => _normalMaterial;
    public Color[] Color => _color;
    public Material[] TileMaterials => _tileMaterials;
    public Material[] WallMaterials => _wallMaterials;
    public GameObject DoorPrefab => _doorPrefab;
    public GameObject Potion => _potion;
    public GameObject Portal => _portal;
    public GameObject EnterAndExit => _enterAndExit;
}

