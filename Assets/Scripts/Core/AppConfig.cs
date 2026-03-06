/// <summary>
/// 컴파일 타임에 앱 ID와 콘텐츠 폴더 경로를 확정하는 정적 설정 클래스.
/// Scripting Define Symbols(MAIN_SUB_KIOSK, APP_B, APP_C)로 분기합니다.
/// </summary>
public static class AppConfig
{
    // ── 앱 식별자 ──
#if MAIN_SUB_KIOSK
    public const string AppID = "MainSubKiosk";
#elif APP_B
    public const string AppID = "APP_B";       // 가칭 — 추후 변경 예정
#elif APP_C
    public const string AppID = "APP_C";       // 가칭 — 추후 변경 예정
#else
    public const string AppID = "MainSubKiosk"; // 기본값: 심볼 미지정 시 MainSubKiosk
#endif

    // ── 콘텐츠 폴더명 ──
    public const string ContentFolderName = "Content_" + AppID;

    /// <summary>
    /// 콘텐츠 폴더의 절대 경로를 반환합니다.
    /// 에디터: Application.dataPath 상위 폴더 기준
    /// 빌드: exe 옆 디렉토리 기준
    /// </summary>
    public static string ContentBasePath
    {
        get
        {
#if UNITY_EDITOR
            // 에디터: Assets/ 폴더 내 StreamingAssets
            return System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "StreamingAssets",
                ContentFolderName);
#else
            return System.IO.Path.Combine(
                UnityEngine.Application.streamingAssetsPath,
                ContentFolderName);
#endif
        }
    }

    // ── 런타임 설정값 (Settings.txt 연동) ──

    /// <summary>터치 없을 시 메인 화면 복귀 시간</summary>
    public static float IdleTimeout => CSVReader.GetFloatValue("Idle_Timeout", 60f);

    /// <summary>화면 전환 애니메이션 속도</summary>
    public static float TransitionSpeed => CSVReader.GetFloatValue("Transition_Speed", 0.5f);

    /// <summary>화면 해상도 문자열 (ex: 1920x1080)</summary>
    public static string Resolution => CSVReader.GetStringValue("Resolution", "1920x1080");

    /// <summary>시작 시 에러 경고 팝업 표시 여부</summary>
    public static bool ShowErrorPopup => CSVReader.GetStringValue("Show_Error_Popup", "true").ToLowerInvariant() == "true";

    /// <summary>사용 중 런타임 에러 경고 팝업 표시 여부</summary>
    public static bool ShowRuntimeErrorPopup => CSVReader.GetStringValue("Show_Runtime_Error_Popup", "true").ToLowerInvariant() == "true";

    /// <summary>마우스 커서 화면 숨김 여부</summary>
    public static bool HideMouseCursor => CSVReader.GetStringValue("Hide_Mouse_Cursor", "true").ToLowerInvariant() == "true";

    /// <summary>목표 프레임 제한 (기본 60)</summary>
    public static int TargetFrameRate => CSVReader.GetIntValue("Target_Frame_Rate", 60);

    /// <summary>인식할 이미지 확장자 (세미콜론 구분)</summary>
    public static string ImageExtensions => CSVReader.GetStringValue("Image_Extensions", ".png;.jpg;.jpeg");

    /// <summary>그리드 생성 제외 외부 버튼 (세미콜론 구분)</summary>
    public static string ExcludeGridButtons => CSVReader.GetStringValue("Exclude_Grid_Buttons", "");

    /// <summary>필수 지정 버튼 명단 (세미콜론 구분)</summary>
    public static string RequiredButtons => CSVReader.GetStringValue("Required_Buttons", "");
}
