using UnityEngine;

/// <summary>
/// 페이지 간 이동을 중앙에서 관리합니다 (메인 ↔ 상세).
/// 전환 중 중복 클릭을 차단합니다.
/// </summary>
public class PageNavigator : MonoBehaviour
{
    [Tooltip("상세 페이지 매니저 (Inspector에서 연결)")]
    [SerializeField] private DetailPageManager detailPageManager;


    // ── 전환 중 중복 입력 차단 ──
    private bool isTransitioning;

    /// <summary>
    /// 현재 상세 페이지가 열려있는지 여부
    /// </summary>
    public bool IsDetailOpen => detailPageManager != null && detailPageManager.IsShowing;

    /// <summary>
    /// 상세 페이지를 엽니다.
    /// 전환 중이면 무시합니다.
    /// </summary>
    /// <param name="itemId">아이템 ID (예: "0001")</param>
    public async void OpenDetail(string itemId)
    {
        if (isTransitioning)
        {
            Debug.Log("[INFO] PageNavigator: 전환 중 — 입력 무시");
            return;
        }

        if (detailPageManager == null)
        {
            LogError("단말기 설정 오류: 상세 페이지 화면 시스템(DetailPageManager) 연결이 누락되었습니다.");
            return;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            LogError("콘텐츠 오류: 열어볼 콘텐츠의 정보(ID)가 비어 있거나 손상되었습니다.");
            return;
        }

        isTransitioning = true;

        string msg = $"[INFO] 상세 페이지 열기: {itemId}";
        Debug.Log(msg);


        detailPageManager.Show(itemId);

        // 전환 효과 완료를 위한 짧은 대기
        // DetailPageManager.Show는 async void이므로 직접 await 불가
        // 전환 시간 + 여유를 두고 플래그 해제
        float transitionSpeed = AppConfig.TransitionSpeed;
        await System.Threading.Tasks.Task.Delay(
            (int)((transitionSpeed + 0.1f) * 1000));

        isTransitioning = false;
    }

    /// <summary>
    /// 메인 화면으로 복귀합니다.
    /// 전환 중이면 무시합니다.
    /// </summary>
    public async void ReturnToMain()
    {
        if (isTransitioning) return;

        if (detailPageManager == null || !detailPageManager.IsShowing) return;

        isTransitioning = true;

        string msg = "[INFO] 메인 화면으로 복귀";
        Debug.Log(msg);


        detailPageManager.Hide();

        // 전환 완료 대기
        float transitionSpeed = AppConfig.TransitionSpeed;
        await System.Threading.Tasks.Task.Delay(
            (int)((transitionSpeed + 0.1f) * 1000));

        isTransitioning = false;
    }

    /// <summary>
    /// 에러 로그를 기록합니다.
    /// </summary>
    private void LogError(string message)
    {
        string fullMsg = $"[ERROR] PageNavigator: {message}";
        Debug.LogError(fullMsg);

    }
}
