using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스킬 슬롯 상태 관리자 (DontDestroyOnLoad)
/// 
/// 목적:
/// - 씬 전환 시 스킬 슬롯에 배치된 스킬 상태 저장/복원
/// - CharacterDataManager에서 로드된 데이터 복원
/// - SkillSlotUI가 비워지는 문제 해결
/// 
/// 동작:
/// 1. CharacterDataManager.LoadSkillSlots(): 저장 데이터 전달
/// 2. PlayerInitializationManager.OnAllSystemsReady: UI 복원 시작
/// 3. RestoreSlotStates(): 실제 UI에 스킬 배치
/// 
/// 사용 위치:
/// - DontDestroyOnLoad 씬에 배치 (Canvas와 함께)
/// - CharacterDataManager가 데이터 전달
/// - PlayerInitializationManager가 복원 트리거
/// </summary>
public class SkillSlotStateManager : MonoBehaviour
{
    #region Singleton

    private static SkillSlotStateManager _instance;
    public static SkillSlotStateManager Instance => _instance;

    #endregion

    [Header("디버그 설정")]
    [SerializeField] private bool _enableDebugLogs = true;

    [Header("복원 재시도 설정")]
    [SerializeField] private int _maxRetries = 5;
    [SerializeField] private float _retryDelay = 0.5f;

    // 스킬 슬롯 상태 저장 (슬롯 인덱스 → SkillData 이름)
    private Dictionary<int, string> _savedSlotStates = new Dictionary<int, string>();
    private int _currentRetryCount = 0;

    #region 초기화

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Log("SkillSlotStateManager 초기화 (DontDestroyOnLoad)");
    }

    private void OnEnable()
    {
        // PlayerInitializationManager의 모든 시스템 준비 완료 이벤트 구독
        PlayerInitializationManager.OnAllSystemsReady += OnAllSystemsReady;
    }

    private void OnDisable()
    {
        PlayerInitializationManager.OnAllSystemsReady -= OnAllSystemsReady;
    }

    #endregion

    #region CharacterDataManager 연동

    /// <summary>
    /// CharacterDataManager에서 저장 데이터 로드
    /// 
    /// 호출 시점:
    /// - CharacterDataManager.LoadSkillSlots() 내
    /// - 게임 시작 또는 씬 전환 후 데이터 로드 시
    /// 
    /// 파라미터:
    /// - slotStates: 슬롯 인덱스 → 스킬 이름 매핑
    /// 
    /// 동작:
    /// - _savedSlotStates에 데이터 저장
    /// - OnAllSystemsReady 이벤트에서 UI 복원
    /// </summary>
    public void LoadFromSaveData(Dictionary<int, string> slotStates)
    {
        if (slotStates == null || slotStates.Count == 0)
        {
            Log("CharacterDataManager: 로드할 스킬 슬롯 데이터가 없습니다.");
            return;
        }

        _savedSlotStates = new Dictionary<int, string>(slotStates);
        Log($"CharacterDataManager에서 스킬 슬롯 데이터 로드 완료 ({_savedSlotStates.Count}개 슬롯)");

        foreach (var kvp in _savedSlotStates)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                Log($"  슬롯 {kvp.Key}: {kvp.Value}");
            }
        }
    }

    #endregion

    #region 스킬 슬롯 상태 저장 (씬 전환용)

    /// <summary>
    /// 현재 스킬 슬롯 상태 저장
    /// 
    /// 호출 시점:
    /// - 씬 전환 전 (GameInitializer.OnSceneUnloaded)
    /// - 수동 저장 요청 시
    /// 
    /// 저장 정보:
    /// - 각 슬롯에 배치된 스킬의 이름 (SkillData.SkillName)
    /// - 빈 슬롯은 null 또는 빈 문자열로 저장
    /// </summary>
    public void SaveCurrentSlotStates(List<SkillSlotUI> skillSlots)
    {
        if (skillSlots == null || skillSlots.Count == 0)
        {
            LogWarning("저장할 스킬 슬롯이 없습니다.");
            return;
        }

        _savedSlotStates.Clear();

        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i] != null)
            {
                SkillData skillData = skillSlots[i].SkillData;
                string skillName = skillData != null ? skillData.SkillName : null;

                _savedSlotStates[i] = skillName;

                if (skillName != null)
                {
                    Log($"슬롯 {i} 상태 저장: {skillName}");
                }
            }
        }

        Log($"스킬 슬롯 상태 저장 완료 ({_savedSlotStates.Count}개 슬롯)");
    }

    #endregion

    #region 스킬 슬롯 상태 복원

    /// <summary>
    /// 모든 시스템 준비 완료 시 호출
    /// PlayerInitializationManager.OnAllSystemsReady 이벤트 핸들러
    /// </summary>
    private void OnAllSystemsReady()
    {
        Log("OnAllSystemsReady 이벤트 수신 - 스킬 슬롯 복원 시작");

        _currentRetryCount = 0;
        Invoke(nameof(RestoreSlotStates), 0.5f);
    }

    /// <summary>
    /// 저장된 스킬 슬롯 상태 복원
    /// 
    /// 복원 과정:
    /// 1. Player GameObject 및 PlayerCharacter 찾기
    /// 2. SkillActivationSystem에서 SkillSlotUI 참조 가져오기
    /// 3. PlayerCharacter에서 언락된 스킬 목록 가져오기
    /// 4. 저장된 슬롯 상태에 따라 스킬 재배치
    /// 
    /// 호출 시점:
    /// - OnAllSystemsReady에서 0.5초 지연 후
    /// - 실패 시 재시도 (최대 5회)
    /// </summary>
    private void RestoreSlotStates()
    {
        if (_savedSlotStates.Count == 0)
        {
            Log("복원할 스킬 슬롯 상태가 없습니다 (신규 게임 시작 또는 초기화 전)");
            return;
        }

        Log($"스킬 슬롯 복원 시작 (시도: {_currentRetryCount + 1}/{_maxRetries})");

        // Player 찾기
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            LogWarning("Player를 찾을 수 없습니다 - 재시도 예약");
            RetryRestoration();
            return;
        }

        PlayerCharacter playerCharacter = playerObject.GetComponent<PlayerCharacter>();
        if (playerCharacter == null)
        {
            LogWarning("PlayerCharacter 컴포넌트를 찾을 수 없습니다 - 재시도 예약");
            RetryRestoration();
            return;
        }

        // SkillActivationSystem 찾기
        SkillActivationSystem skillActivationSystem = playerObject.GetComponent<SkillActivationSystem>();
        if (skillActivationSystem == null)
        {
            LogWarning("SkillActivationSystem을 찾을 수 없습니다 - 재시도 예약");
            RetryRestoration();
            return;
        }

        // SkillSlotUI 목록 가져오기
        List<SkillSlotUI> skillSlots = skillActivationSystem.GetAllSkillSlots();
        if (skillSlots == null || skillSlots.Count == 0)
        {
            LogWarning($"스킬 슬롯 목록이 비어있습니다 ({skillSlots?.Count ?? 0}개) - 재시도 예약");
            RetryRestoration();
            return;
        }

        // 언락된 스킬 목록 가져오기
        List<SkillData> unlockedSkills = playerCharacter.UnlockedSkills;
        Log($"PlayerCharacter 언락된 스킬 {unlockedSkills.Count}개 확인");

        // 스킬 이름 → SkillData 매핑 생성
        Dictionary<string, SkillData> skillMap = new Dictionary<string, SkillData>();

        // 1. PlayerCharacter에서 언락된 스킬 추가
        foreach (SkillData skill in unlockedSkills)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.SkillName))
            {
                skillMap[skill.SkillName] = skill;
            }
        }

        // 2. SkillTreeManager에서도 스킬 검색 (중복 방지)
        SkillTreeManager skillTreeManager = FindFirstObjectByType<SkillTreeManager>();
        if (skillTreeManager != null)
        {
            var allNodes = skillTreeManager.AllNodes;
            foreach (var node in allNodes)
            {
                if (node != null && node.SkillData != null && !string.IsNullOrEmpty(node.SkillData.SkillName))
                {
                    if (!skillMap.ContainsKey(node.SkillData.SkillName))
                    {
                        skillMap[node.SkillData.SkillName] = node.SkillData;
                    }
                }
            }
            Log($"SkillTreeManager에서 추가 스킬 검색 완료 (총 {skillMap.Count}개)");
        }

        // 저장된 상태에 따라 스킬 슬롯 복원
        int restoredCount = 0;
        int emptyCount = 0;
        int failedCount = 0;

        foreach (var kvp in _savedSlotStates)
        {
            int slotIndex = kvp.Key;
            string skillName = kvp.Value;

            // 슬롯 인덱스 유효성 검사
            if (slotIndex < 0 || slotIndex >= skillSlots.Count)
            {
                LogWarning($"잘못된 슬롯 인덱스: {slotIndex}");
                failedCount++;
                continue;
            }

            SkillSlotUI slot = skillSlots[slotIndex];
            if (slot == null)
            {
                LogWarning($"슬롯 {slotIndex}이(가) null입니다.");
                failedCount++;
                continue;
            }

            // 빈 슬롯인 경우
            if (string.IsNullOrEmpty(skillName))
            {
                slot.RemoveSkill();
                emptyCount++;
                continue;
            }

            // 스킬 찾기 및 배치
            if (skillMap.TryGetValue(skillName, out SkillData skillData))
            {
                slot.AssignSkill(skillData);
                restoredCount++;
                Log($"슬롯 {slotIndex}: {skillName} 복원 완료");
            }
            else
            {
                LogWarning($"슬롯 {slotIndex}: 스킬 '{skillName}'을(를) 찾을 수 없습니다 (언락 안 됨?)");
                slot.RemoveSkill();
                failedCount++;
            }
        }

        Log($"스킬 슬롯 복원 완료 - 성공: {restoredCount}, 빈 슬롯: {emptyCount}, 실패: {failedCount}");
        _currentRetryCount = 0; // 성공 시 재시도 카운터 리셋
    }

    /// <summary>
    /// 복원 재시도
    /// </summary>
    private void RetryRestoration()
    {
        _currentRetryCount++;

        if (_currentRetryCount >= _maxRetries)
        {
            LogError($"스킬 슬롯 복원 실패: {_maxRetries}회 시도 후 포기");
            _currentRetryCount = 0;
            return;
        }

        Log($"스킬 슬롯 복원 재시도 예약 ({_retryDelay}초 후)");
        Invoke(nameof(RestoreSlotStates), _retryDelay);
    }

    #endregion

    #region Public API

    /// <summary>
    /// 저장된 상태 초기화
    /// 
    /// 호출 시점:
    /// - 새 게임 시작
    /// - 캐릭터 선택 씬으로 돌아갈 때
    /// </summary>
    public void ClearSavedStates()
    {
        _savedSlotStates.Clear();
        Log("저장된 스킬 슬롯 상태 초기화");
    }

    /// <summary>
    /// 저장된 상태 개수 확인
    /// </summary>
    public int GetSavedStateCount()
    {
        return _savedSlotStates.Count;
    }

    /// <summary>
    /// 특정 슬롯의 저장된 스킬 이름 가져오기
    /// </summary>
    public string GetSavedSkillName(int slotIndex)
    {
        if (_savedSlotStates.TryGetValue(slotIndex, out string skillName))
        {
            return skillName;
        }
        return null;
    }

    #endregion

    #region 로깅

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[SkillSlotStateManager] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[SkillSlotStateManager] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogError(string message)
    {
        Debug.LogError($"[SkillSlotStateManager] {message}");
    }

    #endregion
}
