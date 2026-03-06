using UnityEngine;

/// <summary>
/// 사용자 입력이 없을 때 자동으로 메인 화면으로 복귀합니다.
/// Update에서 입력을 감시하고 타이머를 관리합니다.
/// </summary>
public class IdleWatchdog : MonoBehaviour
{
    [Tooltip("PageNavigator 참조 (Inspector에서 연결)")]
    [SerializeField] private PageNavigator pageNavigator;


    // ── 내부 상태 ──
    private float idleTimer;
    private float idleTimeout;
    private bool isEnabled;

    private void Start()
    {
        // Settings.txt에서 타임아웃 값 로드
        idleTimeout = AppConfig.IdleTimeout;
        idleTimer = 0f;
        isEnabled = true;

        string msg = $"[INFO] IdleWatchdog 초기화 — 타임아웃: {idleTimeout}초";
        Debug.Log(msg);

    }

    private void Update()
    {
        if (!isEnabled) return;

        // 입력 감지: 마우스/키보드/터치
        if (Input.anyKey || Input.touchCount > 0 || HasMouseMoved())
        {
            ResetTimer();
            return;
        }

        // 타이머 증가 (TimeScale 영향 받지 않음)
        idleTimer += Time.unscaledDeltaTime;

        // 타임아웃 도달 시 메인 화면 복귀
        if (idleTimer >= idleTimeout)
        {
            OnIdleTimeout();
        }
    }

    // ── 마우스 이동 감지 (GC 발생 없음) ──
    private Vector3 lastMousePosition;

    private bool HasMouseMoved()
    {
        Vector3 currentPos = Input.mousePosition;
        if (currentPos != lastMousePosition)
        {
            lastMousePosition = currentPos;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Idle 타이머를 초기화합니다.
    /// 외부에서 호출하여 타이머를 수동으로 리셋할 수도 있습니다.
    /// </summary>
    public void ResetTimer()
    {
        idleTimer = 0f;
    }

    /// <summary>
    /// 타임아웃 도달 시 호출됩니다.
    /// </summary>
    private void OnIdleTimeout()
    {
        ResetTimer();

        // 상세 페이지가 열려있을 때만 복귀 실행
        if (pageNavigator != null && pageNavigator.IsDetailOpen)
        {
            string msg = $"[INFO] Idle 타임아웃 ({idleTimeout}초) — 메인 화면으로 자동 복귀";
            Debug.Log(msg);


            pageNavigator.ReturnToMain();
        }
    }

    /// <summary>
    /// IdleWatchdog를 활성화/비활성화합니다.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (enabled) ResetTimer();
    }
}
