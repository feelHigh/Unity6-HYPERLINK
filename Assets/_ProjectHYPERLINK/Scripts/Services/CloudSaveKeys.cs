/// <summary>
/// Cloud Save 키 상수 정의
/// 
/// Cloud Save에서 사용하는 키 문자열을 중앙 관리
/// 오타 방지 및 유지보수성 향상
/// 
/// 사용 위치:
/// - CloudSaveManager.LoadCharacterDataAsync()
/// - CloudSaveManager.SaveCharacterDataAsync()
/// - CloudSaveManager.HasCharacterAsync()
/// - CloudSaveManager.DeleteCharacterAsync()
/// </summary>
public static class CloudSaveKeys
{
    // 메인 캐릭터 데이터 키
    public const string CHARACTER_DATA = "character_data";

    // 선택적 메타데이터 키들
    public const string LAST_PLAYED = "last_played";
    public const string PLAY_TIME = "play_time";

    // 캐릭터 존재 확인용
    public const string HAS_CHARACTER = "has_character";
}