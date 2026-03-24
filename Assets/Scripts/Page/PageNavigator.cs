using UnityEngine;

/// <summary>
/// 페이지 간 이동을 중앙에서 관리합니다 (메인 ↔ 상세 ↔ 0000).
/// 0000 아이템은 SpecialPageManager로, 그 외엔 DetailPageManager로 라우팅합니다.
/// 전환 중 중복 클릭을 차단합니다.
/// </summary>
public class PageNavigator : MonoBehaviour
{
    [Tooltip("상세 페이지 매니저 (0001~0006)")]
    [SerializeField] private DetailPageManager detailPageManager;

    [Tooltip("0000 전용 페이지 매니저")]
    [SerializeField] private SpecialPageManager specialPageManager;

    [Tooltip("메인 화면 매니저 (버튼 Off 복원용)")]
    [SerializeField] private MainScreenManager mainScreenManager;

    // ── 전환 중 중복 입력 차단 ──
    private bool isTransitioning;

    /// <summary>현재 상세 페이지 또는 0000 페이지가 열려있는지 여부</summary>
    public bool IsDetailOpen =>
        (detailPageManager != null && detailPageManager.IsShowing) ||
        (specialPageManager != null && specialPageManager.IsShowing);

    private void Awake()
    {
        // 상세 페이지 로딩 완료 콜백 구독
        if (detailPageManager != null)
        {
            detailPageManager.OnLoadComplete += OnDetailLoadComplete;
        }
    }

    /// <summary>
    /// 상세 페이지 또는 0000 페이지를 엽니다.
    /// 0000이면 SpecialPageManager, 그 외엔 DetailPageManager로 라우팅합니다.
    /// </summary>
    public bool OpenDetail(string itemId)
    {
        if (isTransitioning)
        {
            Debug.Log("[INFO] PageNavigator: 전환 중 — 입력 무시");
            return false;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError("[ERROR] PageNavigator: 콘텐츠 ID가 비어 있습니다.");
            return false;
        }

        isTransitioning = true;

        Debug.Log($"[INFO] 페이지 열기: {itemId}");

        // 0000이면 전용 페이지
        if (itemId == "0000")
        {
            if (specialPageManager != null)
                specialPageManager.Show();

            // 비동기 전환 완료 대기는 별도 코루틴으로 처리
            WaitAndUnlock();
            if (mainScreenManager != null) mainScreenManager.ResetActiveButton();
        }
        else
        {
            if (detailPageManager != null)
                detailPageManager.Show(itemId);

            WaitAndUnlock();
        }

        return true;
    }

    /// <summary>전환 애니메이션 시간만큼 대기한 후 락을 해제합니다.</summary>
    private async void WaitAndUnlock()
    {
        await WaitForTransition();
        isTransitioning = false;
    }

    /// <summary>
    /// 상세 페이지에서 0000 페이지로 이동합니다.
    /// 상세 페이지의 리소스를 정리한 후 0000을 엽니다.
    /// </summary>
    public async void OpenSpecialFromDetail()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // 1. 0000 페이지 먼저 열기 (화면을 우선 덮기)
        if (specialPageManager != null)
        {
            specialPageManager.Show();
            await WaitForTransition();
        }

        // 2. 0000 페이지가 완전히 나타난 뒤, 덮어씌워진 상세 페이지 닫기
        if (detailPageManager != null && detailPageManager.IsShowing)
        {
            detailPageManager.Hide();
        }

        isTransitioning = false;
        Debug.Log("[INFO] 상세 → 0000 전환 완료");
    }

    /// <summary>메인 화면으로 복귀합니다.</summary>
    public async void ReturnToMain()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        Debug.Log("[INFO] 메인 화면으로 복귀");

        // 상세 페이지 닫기
        if (detailPageManager != null && detailPageManager.IsShowing)
        {
            detailPageManager.Hide();
            await WaitForTransition();
        }

        // 0000 페이지 닫기
        if (specialPageManager != null && specialPageManager.IsShowing)
        {
            specialPageManager.Hide();
            await WaitForTransition();
        }

        // 메인 버튼 Off 복원
        if (mainScreenManager != null) mainScreenManager.ResetActiveButton();

        isTransitioning = false;
    }

    /// <summary>상세 페이지 로딩 완료 시 호출됩니다.</summary>
    private void OnDetailLoadComplete()
    {
        // 메인 버튼 Off 복원
        if (mainScreenManager != null) mainScreenManager.ResetActiveButton();
    }

    /// <summary>전환 효과 시간만큼 대기합니다.</summary>
    private async System.Threading.Tasks.Task WaitForTransition()
    {
        float transitionSpeed = AppConfig.TransitionSpeed;
        await System.Threading.Tasks.Task.Delay(
            (int)((transitionSpeed + 0.1f) * 1000));
    }

    private void OnDestroy()
    {
        if (detailPageManager != null)
            detailPageManager.OnLoadComplete -= OnDetailLoadComplete;
    }
}
