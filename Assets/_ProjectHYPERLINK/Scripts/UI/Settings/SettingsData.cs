using System;
using UnityEngine;

/// <summary>
/// 게임 설정 데이터 구조
/// 
/// 역할:
/// - 모든 설정 값 저장
/// - PlayerPrefs를 통한 로컬 저장
/// - 설정 적용 및 로드
/// 
/// 저장 방식:
/// - PlayerPrefs 사용 (디바이스 로컬 저장)
/// - JSON 직렬화로 한 번에 저장/로드
/// 
/// 사용 흐름:
/// 1. SettingsManager에서 인스턴스 생성
/// 2. PlayerPrefs에서 로드 또는 기본값 사용
/// 3. UI에서 값 변경
/// 4. Apply 시 실제 설정 적용
/// </summary>
[Serializable]
public class SettingsData
{
    #region Video Settings

    /// <summary>
    /// 해상도 인덱스 (0: 1920x1080, 1: 1280x720)
    /// </summary>
    public int resolutionIndex = 0;

    /// <summary>
    /// 디스플레이 모드 (0: Windowed, 1: Fullscreen Windowed, 2: Fullscreen)
    /// </summary>
    public int displayModeIndex = 0;

    #endregion

    #region Audio Settings

    /// <summary>
    /// 마스터 볼륨 (0.0 ~ 1.0)
    /// </summary>
    public float masterVolume = 1.0f;

    /// <summary>
    /// 효과음 볼륨 (0.0 ~ 1.0)
    /// </summary>
    public float sfxVolume = 1.0f;

    /// <summary>
    /// 배경음악 볼륨 (0.0 ~ 1.0)
    /// </summary>
    public float bgmVolume = 1.0f;

    #endregion

    #region Game Settings

    /// <summary>
    /// 언어 인덱스 (0: English, 1: Korean, 2: Simplified Chinese)
    /// </summary>
    public int languageIndex = 1; // 기본값: Korean

    #endregion

    #region 헬퍼 메서드

    /// <summary>
    /// 기본 설정값 반환
    /// </summary>
    public static SettingsData GetDefault()
    {
        return new SettingsData
        {
            resolutionIndex = 0,
            displayModeIndex = 0,
            masterVolume = 1.0f,
            sfxVolume = 1.0f,
            bgmVolume = 1.0f,
            languageIndex = 1 // Korean
        };
    }

    /// <summary>
    /// 딥 카피
    /// </summary>
    public SettingsData Clone()
    {
        return new SettingsData
        {
            resolutionIndex = this.resolutionIndex,
            displayModeIndex = this.displayModeIndex,
            masterVolume = this.masterVolume,
            sfxVolume = this.sfxVolume,
            bgmVolume = this.bgmVolume,
            languageIndex = this.languageIndex
        };
    }

    #endregion
}

/// <summary>
/// 해상도 정보
/// </summary>
[Serializable]
public struct ResolutionInfo
{
    public int width;
    public int height;
    public string displayName;

    public ResolutionInfo(int width, int height)
    {
        this.width = width;
        this.height = height;
        this.displayName = $"{width}x{height}";
    }
}

/// <summary>
/// 디스플레이 모드
/// </summary>
public enum DisplayMode
{
    Windowed = 0,           // 창 모드
    FullscreenWindowed = 1, // 전체화면 창 모드 (Borderless)
    Fullscreen = 2          // 전체화면
}

/// <summary>
/// 언어 설정
/// </summary>
public enum Language
{
    English = 0,
    Korean = 1,
    SimplifiedChinese = 2
}
