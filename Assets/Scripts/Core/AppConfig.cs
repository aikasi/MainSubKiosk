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
#elif MEDIA_TABLE
    public const string AppID = "MediaTable";
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

    // ── 미디어 테이블 전용 폴더 설정 ──
    /// <summary>
    /// 미디어 테이블용 메인 썸네일 버튼(-btn)들이 들어있는 기준 폴더 경로 
    /// (Settings.txt 설정값, 기본: Content_MediaTable)
    /// </summary>
    public static string MediaTableBtnPath => CSVReader.GetStringValue("MediaTable_Btn_Path", "Content_MediaTable");

    /// <summary>
    /// 미디어 테이블용 책 이미지(-page_x)들이 들어있는 기준 폴더 경로 
    /// (Settings.txt 설정값, 기본: Content_MediaTable)
    /// </summary>
    public static string MediaTablePagePath => CSVReader.GetStringValue("MediaTable_Page_Path", "Content_MediaTable");

    /// <summary>
    /// 미디어 테이블용 메인 썸네일 버튼(-btn) 폴더의 절대 경로를 반환합니다.
    /// 에디터: Application.dataPath 기준, 빌드: streamingAssetsPath 기준
    /// </summary>
    public static string MediaTableBtnBasePath
    {
        get
        {
#if UNITY_EDITOR
            return System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "StreamingAssets",
                MediaTableBtnPath);
#else
            return System.IO.Path.Combine(
                UnityEngine.Application.streamingAssetsPath,
                MediaTableBtnPath);
#endif
        }
    }

    /// <summary>
    /// 미디어 테이블용 책 속지(-page_x) 폴더의 절대 경로를 반환합니다.
    /// 에디터: Application.dataPath 기준, 빌드: streamingAssetsPath 기준
    /// </summary>
    public static string MediaTablePageBasePath
    {
        get
        {
#if UNITY_EDITOR
            return System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "StreamingAssets",
                MediaTablePagePath);
#else
            return System.IO.Path.Combine(
                UnityEngine.Application.streamingAssetsPath,
                MediaTablePagePath);
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

    /// <summary>중간에 비어있는 책 속지(-page_x)가 발견될 경우 경고 팝업을 띄울지 여부</summary>
    public static bool ShowMissingPageError => CSVReader.GetStringValue("Show_Missing_Page_Error", "true").ToLowerInvariant() == "true";

    /// <summary>목표 프레임 제한 (기본 60)</summary>
    public static int TargetFrameRate => CSVReader.GetIntValue("Target_Frame_Rate", 60);

    /// <summary>인식할 이미지 확장자 (세미콜론 구분)</summary>
    public static string ImageExtensions => CSVReader.GetStringValue("Image_Extensions", ".png;.jpg;.jpeg");

    /// <summary>그리드 생성 제외 외부 버튼 (세미콜론 구분)</summary>
    public static string ExcludeGridButtons => CSVReader.GetStringValue("Exclude_Grid_Buttons", "");

    /// <summary>필수 지정 버튼 명단 (세미콜론 구분)</summary>
    public static string RequiredButtons => CSVReader.GetStringValue("Required_Buttons", "");

    /// <summary>
    /// 미디어 테이블 책장 뒤로 넘길 때, 과거 페이지 이미지를 메모리에 몇 장이나 남겨둘 것인지 설정합니다.
    /// (Settings.txt 설정값, 기본: 2)
    /// 0으로 설정 시 지나간 페이지는 즉시 메모리에서 강제 삭제됩니다.
    /// </summary>
    public static int KeepPreviousPages => CSVReader.GetIntValue("Keep_Previous_Pages", 2);
}
