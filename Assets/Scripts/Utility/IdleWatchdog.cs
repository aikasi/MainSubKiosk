using UnityEngine;

/// <summary>
/// 사용자 입력이 없을 때 자동으로 메인 화면으로 복귀합니다.
/// 상세 페이지 또는 0000 페이지가 열려있을 때만 타이머가 동작합니다.
/// </summary>
public class IdleWatchdog : MonoBehaviour
{
    [Tooltip("PageNavigator 참조 (Inspector에서 연결 필수!)")]
    [SerializeField] private PageNavigator pageNavigator;

    // ── 내부 상태 ──
    private float idleTimer;
    private float idleTimeout;
    private bool isEnabled;

    private void Start()
    {
        idleTimeout = AppConfig.IdleTimeout;
        idleTimer = 0f;
        isEnabled = true;
        
        lastMousePosition = Input.mousePosition;

        if (pageNavigator == null)
        {
            Debug.LogError("[ERROR] IdleWatchdog: Inspector에 PageNavigator가 연결되지 않았습니다! (드래그 앤 드롭 필요)");
        }
        else
        {
            Debug.Log($"[INFO] IdleWatchdog 초기화 — 타임아웃: {idleTimeout}초");
        }
    }

    private void Update()
    {
        // 1. 컴포넌트 비활성화, 참조 누락, 혹은 상세 페이지가 안 열렸으면 작동 정지
        if (!isEnabled || pageNavigator == null || !pageNavigator.IsDetailOpen)
        {
            if (idleTimer > 0f) ResetTimer(); // 메인 화면 대기 시엔 0으로 유지
            return;
        }

        // 2. 입력 감지: 키보드/터치/유의미한 마우스 이동
        if (Input.anyKey || Input.touchCount > 0 || HasMouseMovedSignificant())
        {
            ResetTimer();
            return;
        }

        // 3. 타이머 증가
        idleTimer += Time.unscaledDeltaTime;

        // 4. 타임아웃 도달 시 구출 로직
        if (idleTimer >= idleTimeout)
        {
            OnIdleTimeout();
        }
    }

    // ── 마우스 유의미한 이동 감지 (키오스크 터치스크린 미세 떨림 무시) ──
    private Vector3 lastMousePosition;
    private const float MOUSE_MOVE_THRESHOLD_SQ = 4.0f; // 2픽셀 제곱 기준

    private bool HasMouseMovedSignificant()
    {
        Vector3 currentPos = Input.mousePosition;
        float sqrDist = (currentPos - lastMousePosition).sqrMagnitude;
        
        if (sqrDist > MOUSE_MOVE_THRESHOLD_SQ)
        {
            lastMousePosition = currentPos;
            return true;
        }
        
        lastMousePosition = currentPos; // 떨림 누적 방지(현재 위치 갱신)
        return false;
    }

    /// <summary>Idle 타이머를 초기화합니다.</summary>
    public void ResetTimer()
    {
        idleTimer = 0f;
    }

    /// <summary>타임아웃 도달 시 호출됩니다.</summary>
    private void OnIdleTimeout()
    {
        ResetTimer();

        if (pageNavigator != null && pageNavigator.IsDetailOpen)
        {
            Debug.Log($"[INFO] Idle 타임아웃 ({idleTimeout}초) — 메인 화면으로 자동 복귀");
            pageNavigator.ReturnToMain();
        }
    }

    /// <summary>IdleWatchdog를 활성화/비활성화합니다.</summary>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (enabled) ResetTimer();
    }
}
