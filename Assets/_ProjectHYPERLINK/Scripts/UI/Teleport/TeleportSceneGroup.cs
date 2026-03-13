using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

/// <summary>
/// 씬별 목적지 그룹 UI
/// 
/// 구조:
/// - 씬 이름 라벨
/// - 해당 씬의 모든 스폰 포인트 버튼들
/// </summary>
public class TeleportSceneGroup : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI _sceneNameLabel;
    [SerializeField] private Transform _destinationButtonsContainer;

    [Header("프리팹")]
    [SerializeField] private GameObject _destinationButtonPrefab;

    private string _sceneName;
    private List<TeleportDestinationButton> _spawnedButtons = new List<TeleportDestinationButton>();

    /// <summary>
    /// 씬 그룹 초기화
    /// </summary>
    public void Initialize(string sceneName, List<TeleportDestinationData> destinations)
    {
        _sceneName = sceneName;

        // 씬 이름 라벨 설정
        if (_sceneNameLabel != null)
        {
            string sceneNameText = LocalizationSettings.StringDatabase.GetLocalizedString(
                "DATA_PORTAL", "PORTAL_SCENENAME_" + sceneName.ToUpper());

            _sceneNameLabel.text = GetSceneDisplayName(sceneName);
        }

        // 기존 버튼들 제거
        ClearButtons();

        // 목적지 버튼들 생성
        foreach (var destination in destinations)
        {
            CreateDestinationButton(destination);
        }
    }

    /// <summary>
    /// 목적지 버튼 생성
    /// </summary>
    private void CreateDestinationButton(TeleportDestinationData destination)
    {
        if (_destinationButtonPrefab == null)
        {
            DebugHelper.LogError("[TeleportSceneGroup] DestinationButtonPrefab이 설정되지 않았습니다");
            return;
        }

        GameObject buttonObj = Instantiate(_destinationButtonPrefab, _destinationButtonsContainer);
        TeleportDestinationButton button = buttonObj.GetComponent<TeleportDestinationButton>();

        if (button != null)
        {
            button.Initialize(destination);
            _spawnedButtons.Add(button);
        }
        else
        {
            DebugHelper.LogError("[TeleportSceneGroup] DestinationButtonPrefab에 TeleportDestinationButton 컴포넌트가 없습니다");
            Destroy(buttonObj);
        }
    }

    /// <summary>
    /// 모든 버튼 제거
    /// </summary>
    private void ClearButtons()
    {
        foreach (var button in _spawnedButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        _spawnedButtons.Clear();
    }

    /// <summary>
    /// 씬 이름을 한글로 변환 (필요 시 커스터마이즈)
    /// </summary>
    private string GetSceneDisplayName(string sceneName)
    {
        // 씬 이름 매핑 (필요 시 확장)
        switch (sceneName)
        {
            case "Town":
                return "마을";
            case "Dungeon1":
                return "던전 1층";
            case "Dungeon2":
                return "던전 2층";
            case "BossRoom":
                return "보스 방";
            default:
                return sceneName;
        }
    }

    private void OnDestroy()
    {
        ClearButtons();
    }
}
