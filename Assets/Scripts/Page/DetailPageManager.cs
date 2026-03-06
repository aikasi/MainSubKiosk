using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 상세 페이지의 표시/숨김 및 이미지 Lazy Loading을 관리합니다.
/// ITransitionEffect를 통해 등장/퇴장 애니메이션을 처리합니다.
/// </summary>
public class DetailPageManager : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("상세 페이지 컨테이너 (CanvasGroup 필수)")]
    [SerializeField] private CanvasGroup detailPanel;

    [Tooltip("상세 이미지를 표시할 RawImage")]
    [SerializeField] private RawImage detailImage;

    [Header("시스템 참조")]
    [Tooltip("이미지 로더 (Inspector에서 연결)")]
    [SerializeField] private ImageLoader imageLoader;

    [Tooltip("리소스 경로 캐시 (Inspector에서 연결)")]
    [SerializeField] private ResourcePathCache resourcePathCache;


    [Header("닫기 버튼")]
    [Tooltip("상세 페이지 닫기 버튼 (Inspector에서 연결)")]
    [SerializeField] private Button closeButton;

    [Tooltip("PageNavigator 참조 (닫기 버튼 경유, Inspector에서 연결)")]
    [SerializeField] private PageNavigator pageNavigator;

    [Header("전환 효과")]
    [Tooltip("사용할 전환 효과 MonoBehaviour (ITransitionEffect 구현체)")]
    [SerializeField] private MonoBehaviour transitionEffectComponent;

    // ── 내부 상태 ──
    private ITransitionEffect transitionEffect;
    private Texture2D currentTexture;

    /// <summary>
    /// 현재 상세 페이지가 표시 중인지 여부
    /// </summary>
    public bool IsShowing { get; private set; }

    private void Awake()
    {
        // MonoBehaviour → ITransitionEffect 변환
        if (transitionEffectComponent != null)
        {
            transitionEffect = transitionEffectComponent as ITransitionEffect;
            if (transitionEffect == null)
            {
                LogError("화면 효과 설정 오류: 화면 전환 효과(ITransitionEffect) 설정이 올바르지 않습니다.");
            }
        }

        // 닫기 버튼 이벤트 등록
        if (closeButton != null && pageNavigator != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                string msg = "[INFO] 닫기 버튼 클릭 — 메인 화면으로 복귀";
                Debug.Log(msg);

                pageNavigator.ReturnToMain();
            });
        }
        else if (closeButton == null)
        {
            LogError("화면 설정 오류: 상세화면 내 '닫기 버튼'이 시스템에 연결되지 않았습니다.");
        }

        // 초기 상태: 숨김
        if (detailPanel != null)
        {
            detailPanel.alpha = 0f;
            detailPanel.blocksRaycasts = false;
            detailPanel.interactable = false;
            detailPanel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 상세 페이지를 표시합니다.
    /// 이미지를 Lazy Load한 뒤 전환 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="itemId">아이템 ID (예: "0001")</param>
    public async void Show(string itemId)
    {
        if (IsShowing) return;
        if (detailPanel == null || detailImage == null) return;

        string pageKey = itemId + "-PAGE";

        // 경로 캐시에서 조회
        if (!resourcePathCache.TryGetPath(pageKey, out string pagePath))
        {
            LogError($"시스템 오류: 화면에 띄울 '{pageKey}' 상세 페이지 이미지를 찾을 수 없습니다.");
            return;
        }

        // 이전 텍스처 정리
        CleanupCurrentTexture();

        // 비동기 이미지 로딩 (전환 전에 미리 로드)
        Texture2D texture = await imageLoader.LoadTextureAsync(pagePath);
        if (texture == null)
        {
            LogError($"시스템 오류: '{pageKey}' 상세 페이지의 이미지를 윈도우에서 불러오는데 실패했습니다.");
            return;
        }

        // RawImage에 텍스처 할당
        currentTexture = texture;
        detailImage.texture = currentTexture;

        // 인터랙션 활성화
        detailPanel.blocksRaycasts = true;
        detailPanel.interactable = true;
        IsShowing = true;

        // 전환 애니메이션 재생
        if (transitionEffect != null)
        {
            await transitionEffect.PlayEnterAsync(detailPanel);
        }
        else
        {
            // 전환 효과 없으면 즉시 표시
            detailPanel.alpha = 1f;
            detailPanel.gameObject.SetActive(true);
        }

        string infoMsg = $"[INFO] 상세 페이지 표시: {pageKey}";
        Debug.Log(infoMsg);

    }

    /// <summary>
    /// 상세 페이지를 숨깁니다.
    /// 퇴장 애니메이션 완료 후 텍스처를 정리합니다.
    /// </summary>
    public async void Hide()
    {
        if (!IsShowing) return;
        if (detailPanel == null) return;

        // 전환 애니메이션 재생
        if (transitionEffect != null)
        {
            await transitionEffect.PlayExitAsync(detailPanel);
        }
        else
        {
            detailPanel.alpha = 0f;
            detailPanel.gameObject.SetActive(false);
        }

        // 인터랙션 비활성화
        detailPanel.blocksRaycasts = false;
        detailPanel.interactable = false;
        IsShowing = false;

        // 텍스처 메모리 해제
        CleanupCurrentTexture();
    }

    /// <summary>
    /// 현재 로드된 텍스처를 안전하게 파괴합니다.
    /// </summary>
    private void CleanupCurrentTexture()
    {
        if (detailImage != null)
        {
            detailImage.texture = null;
        }
        ResourceCleaner.DestroyTexture(ref currentTexture);
    }

    /// <summary>
    /// 에러 로그를 기록합니다.
    /// </summary>
    private void LogError(string message)
    {
        string fullMsg = $"[ERROR] DetailPageManager: {message}";
        Debug.LogError(fullMsg);


        // 런타임 에러도 ErrorPopup에 표시 (Settings.txt의 Show_Runtime_Error_Popup 연동)
        if (ErrorPopup.Instance != null)
        {
            ErrorPopup.Instance.AddAndShow(message);
        }
        else
        {
            Debug.LogWarning("[WARN] 시스템 경고: 화면에 띄울 팝업 창(ErrorPopup)이 존재하지 않습니다. Unity 씬에 ErrorPopup 추가가 필요합니다.");
        }
    }

    private void OnDestroy()
    {
        CleanupCurrentTexture();
    }
}
