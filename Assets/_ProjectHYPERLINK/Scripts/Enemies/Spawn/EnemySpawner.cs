using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 스포너 클래스
/// - 스폰 데이터를 기반으로 일반 몬스터와 에픽 몬스터 생성
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] EnemySpawnData _data;      //스폰 데이터
    [SerializeField] float _spawnRadius = 10f;  //각 적들 사이의 간격
    [SerializeField] EpicEnemySelector _epicEnemySelector;

    private GameObject _group;
    bool _isActive = false;

    private void Start()
    {
        //SpawnerManager에 자신을 등록
        if (SpawnerManager.Instance != null)
        {
            SpawnerManager.Instance.RegisterSpawner(this);
        }

        //EpicEnemySelector 참조
        _epicEnemySelector = GetComponent<EpicEnemySelector>();

        //몬스터 그룹 생성
        CreateEnemyGroup();
    }

    /// <summary>
    /// 몬스터 그룹과 몬스터들을 미리 생성하고 비활성화하는 함수
    /// </summary>
    void CreateEnemyGroup()
    {
        //그룹 오브젝트를 생성하고 위치 설정
        _group = new GameObject($"{_data.name}_Group");
        _group.transform.SetParent(this.transform);
        _group.transform.localPosition = Vector3.zero;

        //그룹 오브젝트에 Enemygroup 컴포넌트 추가
        _group.AddComponent<EnemyGroup>();

        //스폰할 몬스터 목록 준비
        List<(EnemyData data, GameObject prefab)> enemiesToSpawn = new List<(EnemyData, GameObject)>();
        foreach (var info in _data.spawnList)
        {
            for (int i = 0; i < info.count; i++)
            {
                enemiesToSpawn.Add((info.enemyData, info.prefab));
            }
        }

        // 에픽 후보 선정 //
        int epicCandidateIndex = -1;

        //CanBeEpic인 적들의 인덱스 수집
        List<int> epicIdxs = new List<int>();
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            if (enemiesToSpawn[i].data.CanBeEpic)
            {
                epicIdxs.Add(i);
            }
        }

        //CanBeEpic인 적들 중에서 한 명만 램덤으로 선택
        if (epicIdxs.Count > 0)
        {
            int ran = Random.Range(0, epicIdxs.Count);

            epicCandidateIndex = epicIdxs[ran];
        }

        // 몬스터 실제 생성 //
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            var spawnEntry = enemiesToSpawn[i];
            EnemyData data = spawnEntry.data;
            GameObject prefab = spawnEntry.prefab;

            //위치 설정
            Vector3 ranPos = transform.position + Random.insideUnitSphere * _spawnRadius;
            ranPos.y = 0;

            //프리팹을 생성하고, 그룹 오브젝트의 자식으로 만들기
            GameObject enemyGO = Instantiate(prefab, ranPos, Quaternion.identity);
            enemyGO.transform.SetParent(_group.transform);

            //EnemyController 가져오기
            EnemyController enemy = enemyGO.GetComponent<EnemyController>();
            if (enemy == null)
            {
                Debug.LogError($"[Spawner] {data.Name}에 EnemyController가 없습니다.");
                return;
            }

            //에픽 여부와 특수 공격 데이터를 선택
            bool isCandidate = (i == epicCandidateIndex);
            bool isEpic = isCandidate && _epicEnemySelector.TryBeEpic();
            SpecialAttackBase specialAttack = isEpic ? _epicEnemySelector.GetRandomSpecialAttack() : null;

            //적 초기화
            if (isEpic)
            {
                enemy.InitializeEpic(data, specialAttack, _data.dropTable, _data.dropChance);
            }
            else
            {
                enemy.InitializeNormal(data, _data.dropTable, _data.dropChance);
            }
        }

        //그룹 비활성화
        _group.SetActive(false);
    }

    /// <summary>
    /// SpawnerManager에 의해 호출되어 그룹을 활성화하는 함수
    /// </summary>
    public void ActivateGroup()
    {
        if (!_isActive && _group != null)
        {
            _isActive = true;
            _group.SetActive(true);
        }
    }
}
