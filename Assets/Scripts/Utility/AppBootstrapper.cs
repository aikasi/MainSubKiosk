using UnityEngine;

/// <summary>
/// 앱 시작 시 전체 초기화 시퀀스를 오케스트레이션합니다.
/// CSVReader는 [DefaultExecutionOrder(-1000)]으로 이미 먼저 실행됩니다.
/// </summary>
public class AppBootstrapper : MonoBehaviour
{

    [Tooltip("ResourcePathCache (Inspector에서 연결)")]
    [SerializeField] private ResourcePathCache resourcePathCache;

    [Tooltip("IntegrityChecker (Inspector에서 연결)")]
    [SerializeField] private IntegrityChecker integrityChecker;

    [Header("Page 참조")]
    [Tooltip("MainScreenManager (Inspector에서 연결)")]
    [SerializeField] private MainScreenManager mainScreenManager;

    private System.Collections.IEnumerator Start()
    {
        // 다른 싱글톤 인스턴스(ErrorPopup 등)들이 Awake를 완료할 때까지 딱 1프레임 대기
        yield return null;
        InitializeApp();
    }

    /// <summary>
    /// 전체 초기화 시퀀스를 실행합니다.
    /// </summary>
    private void InitializeApp()
    {
        // ⓪ 프레임 제한 및 마우스 커서 표시 여부 제어
        Application.targetFrameRate = AppConfig.TargetFrameRate;
        Cursor.visible = !AppConfig.HideMouseCursor;

        // ① CSVReader는 DefaultExecutionOrder(-1000)으로 이미 Awake에서 로드 완료
        Debug.Log($"[INFO] ===== {AppConfig.AppID} 앱 초기화 시작 =====");

        // ② Logger는 autoInitialize로 이미 Awake에서 파일 생성 완료


        // ③ 리소스 경로 캐싱
        if (resourcePathCache != null)
        {
            resourcePathCache.Initialize();

        }
        else
        {
            Debug.LogError("[ERROR] 시스템 초기화 오류: 이미지와 파일 경로 설정(ResourcePathCache)이 연결되어 있지 않습니다. 관리자에게 문의하세요.");

        }

        // ④ 데이터 무결성 검사
        if (integrityChecker != null)
        {
            bool isValid = integrityChecker.Validate();
            if (!isValid)
            {
                Debug.LogWarning("[WARN] 일부 리소스(이미지 등)가 누락되거나 손상되었습니다. 화면에 일부 내용이 보이지 않을 수 있습니다.");
            }
        }

        // ⑤ 해상도 적용
        ApplyResolution();

        // ⑥ 메인 화면 초기화 및 버튼 바인딩
        if (mainScreenManager != null)
        {
            mainScreenManager.InitializeAsync();
        }
        else
        {
            Debug.LogError("[ERROR] 시스템 초기화 오류: 메인 화면 설정(MainScreenManager)이 누락되었습니다. 관리자에게 문의하세요.");

        }

        Debug.Log($"[INFO] ===== {AppConfig.AppID} 앱 초기화 완료 =====");

    }

    /// <summary>
    /// Settings.txt의 Resolution 값을 적용합니다.
    /// 형식: "1920x1080"
    /// </summary>
    private void ApplyResolution()
    {
        // 3-4. 해상도 적용
        string resolutionStr = AppConfig.Resolution;

        string[] parts = resolutionStr.ToLowerInvariant().Split('x');
        if (parts.Length == 2 &&
            int.TryParse(parts[0].Trim(), out int width) &&
            int.TryParse(parts[1].Trim(), out int height))
        {
            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
            string msg = $"[INFO] 해상도 적용: {width}x{height}";
            Debug.Log(msg);

        }
        else
        {
            // 기본값 적용
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
            string warnMsg = $"[WARN] 화면 크기 설정('{resolutionStr}')의 형식이 잘못되어 기본 해상도(1920x1080)로 안전하게 실행합니다.";
            Debug.LogWarning(warnMsg);

        }
    }
}
