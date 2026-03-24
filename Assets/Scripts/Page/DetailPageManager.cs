using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상세 페이지 (0001~0006)의 표시/숨김, 탭 전환, 배경+슬라이드쇼를 총괄합니다.
/// 탭 전환 시 이전 리소스를 Destroy하고 새 리소스만 유지하여 메모리를 관리합니다.
/// </summary>
public class DetailPageManager : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("상세 페이지 컨테이너 (CanvasGroup 필수)")]
    [SerializeField] private CanvasGroup detailPanel;

    [Tooltip("상세 페이지 배경 이미지를 표시할 RawImage")]
    [SerializeField] private RawImage backgroundImage;

    [Header("하위 시스템")]
    [Tooltip("사진 슬라이드쇼 모듈")]
    [SerializeField] private PhotoSlideshow slideshow;

    [Tooltip("탭 바 컨트롤러")]
    [SerializeField] private TabBarController tabBar;

    [Header("시스템 참조")]
    [SerializeField] private ImageLoader imageLoader;
    [SerializeField] private ResourcePathCache resourcePathCache;

    [Header("닫기 / 0000 이동 버튼")]
    [Tooltip("상세 페이지 닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Tooltip("0000 페이지로 이동하는 버튼 (에디터에서 별도 위치에 배치)")]
    [SerializeField] private Button goToSpecialButton;

    [Tooltip("PageNavigator 참조")]
    [SerializeField] private PageNavigator pageNavigator;

    [Header("전환 효과")]
    [Tooltip("ITransitionEffect 구현체")]
    [SerializeField] private MonoBehaviour transitionEffectComponent;

    // ── 내부 상태 ──
    private ITransitionEffect transitionEffect;
    private string currentItemId = "";
    private bool isOpening = false;

    // ── 현재 로드된 리소스 추적 (②③계층) ──
    private Texture2D currentBackgroundTexture;
    private readonly List<Texture2D> currentSlideTextures = new List<Texture2D>();
    private Texture2D specialBtnTexture; // 0000 페이지 이동 버튼용 캐싱 텍스처

    /// <summary>현재 상세 페이지가 표시 중인지 여부</summary>
    public bool IsShowing { get; private set; }

    /// <summary>상세 페이지 로딩이 완료되면 발생하는 콜백</summary>
    public event System.Action OnLoadComplete;

    private void Awake()
    {
        // 전환 효과 인터페이스 변환
        if (transitionEffectComponent != null)
        {
            transitionEffect = transitionEffectComponent as ITransitionEffect;
            if (transitionEffect == null)
                Debug.LogError("[ERROR] DetailPageManager: ITransitionEffect 설정이 올바르지 않습니다.");
        }

        // 닫기 버튼 이벤트
        if (closeButton != null && pageNavigator != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                Debug.Log("[INFO] 닫기 버튼 클릭 — 메인 화면으로 복귀");
                pageNavigator.ReturnToMain();
            });
        }

        // 0000 이동 버튼 이벤트
        if (goToSpecialButton != null && pageNavigator != null)
        {
            goToSpecialButton.onClick.AddListener(() =>
            {
                Debug.Log("[INFO] 0000 버튼 클릭 — 0000 페이지로 이동");
                pageNavigator.OpenSpecialFromDetail();
            });
        }

        // 탭 클릭 콜백 구독
        if (tabBar != null)
        {
            tabBar.OnTabClicked += OnTabClicked;
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
    /// 배경 이미지 + 슬라이드쇼를 로드한 뒤 전환 애니메이션을 재생합니다.
    /// </summary>
    public async void Show(string itemId)
    {
        if (IsShowing || isOpening) return;
        if (detailPanel == null) return;

        isOpening = true;

        try
        {
            currentItemId = itemId;

            // 0000 페이지 이동 버튼 이미지 (0000_btn.png) 최초 1회 동적 로드 및 적용
            if (specialBtnTexture == null && goToSpecialButton != null)
            {
                // 캐시 목록에서 0000_btn 혹은 0000_btn_off 파일 경로 찾기
                if (resourcePathCache.TryGetPath("0000_btn", out string specialBtnPath))
                {
                    specialBtnTexture = await imageLoader.LoadTextureAsync(specialBtnPath);
                    if (specialBtnTexture != null)
                    {
                        Image btnImg = goToSpecialButton.GetComponent<Image>();
                        if (btnImg != null)
                        {
                            btnImg.sprite = imageLoader.CreateSprite(specialBtnTexture);
                            }
                    }
                }
            }

            // 탭 바 초기화 (0001~0006 목록 전달)
            if (tabBar != null)
            {
                List<string> tabIds = GetAllTabItemIds();
                await tabBar.InitializeAsync(tabIds);
                tabBar.SetActiveTabByItemId(itemId);
            }

            // 배경 + 슬라이드 로드
            await LoadContentAsync(itemId);

            // 패널 활성화 및 전환 애니메이션
            detailPanel.gameObject.SetActive(true);
            detailPanel.blocksRaycasts = true;
            detailPanel.interactable = true;
            IsShowing = true;

            if (transitionEffect != null)
            {
                await transitionEffect.PlayEnterAsync(detailPanel);
            }
            else
            {
                detailPanel.alpha = 1f;
            }

            // 로딩 완료 콜백 (메인 버튼 Off 복원용)
            OnLoadComplete?.Invoke();

            Debug.Log($"[INFO] 상세 페이지 표시: {itemId}");
        }
        finally
        {
            isOpening = false;
        }
    }

    /// <summary>상세 페이지를 숨기고 모든 리소스를 정리합니다.</summary>
    public async void Hide()
    {
        if (!IsShowing) return;
        if (detailPanel == null) return;

        // 전환 애니메이션
        if (transitionEffect != null)
        {
            await transitionEffect.PlayExitAsync(detailPanel);
        }
        else
        {
            detailPanel.alpha = 0f;
            detailPanel.gameObject.SetActive(false);
        }

        detailPanel.blocksRaycasts = false;
        detailPanel.interactable = false;
        IsShowing = false;

        // 슬라이드쇼 정지
        if (slideshow != null) slideshow.Stop();

        // ②③계층 리소스 전부 Destroy
        CleanupCurrentResources();

        currentItemId = "";
        Debug.Log("[INFO] 상세 페이지 숨김 및 리소스 정리 완료");
    }

    /// <summary>
    /// 탭 클릭 시 호출됩니다. 새 콘텐츠를 로드한 후 이전 것을 정리합니다.
    /// </summary>
    private async void OnTabClicked(string newItemId)
    {
        if (string.Equals(newItemId, currentItemId, System.StringComparison.OrdinalIgnoreCase))
            return;

        // 이전 리소스를 임시 보관 (새 로드 완료 전까지 화면에 표시 유지)
        Texture2D oldBackground = currentBackgroundTexture;
        List<Texture2D> oldSlides = new List<Texture2D>(currentSlideTextures);

        // 새 참조를 비워서 LoadContentAsync가 새 리소스를 할당하게 함
        currentBackgroundTexture = null;
        currentSlideTextures.Clear();

        // 새 콘텐츠 로드
        string oldItemId = currentItemId;
        currentItemId = newItemId;
        await LoadContentAsync(newItemId);

        // 탭 바 활성 탭 갱신
        if (tabBar != null) tabBar.SetActiveTabByItemId(newItemId);

        // 이전 리소스 Destroy (새 로드 완료 후이므로 백지 현상 없음)
        DestroyTexture(ref oldBackground);
        foreach (var tex in oldSlides)
        {
            Texture2D temp = tex;
            DestroyTexture(ref temp);
        }

        Debug.Log($"[INFO] 탭 전환 완료: {oldItemId} → {newItemId}");
    }

    /// <summary>
    /// 특정 아이템의 배경 + 슬라이드 이미지를 비동기 로드합니다.
    /// </summary>
    private async Task LoadContentAsync(string itemId)
    {
        // 1. 배경 이미지 (_page.png) 로드
        string pageKey = itemId + "_page";
        if (resourcePathCache.TryGetPath(pageKey, out string pagePath))
        {
            Texture2D bgTex = await imageLoader.LoadTextureAsync(pagePath);
            if (bgTex != null)
            {
                currentBackgroundTexture = bgTex;
                if (backgroundImage != null) backgroundImage.texture = bgTex;
            }
        }
        else
        {
            Debug.LogWarning($"[WARN] '{pageKey}' 배경 이미지를 찾을 수 없습니다.");
        }

        // 2. 슬라이드쇼 이미지 (Image/X_*.png) 로드
        List<string> slidePaths = resourcePathCache.GetSlideImagePaths(itemId);
        List<Texture2D> slideTexList = new List<Texture2D>();

        var loadTasks = new List<Task<Texture2D>>();
        foreach (string path in slidePaths)
        {
            loadTasks.Add(imageLoader.LoadTextureAsync(path));
        }

        if (loadTasks.Count > 0)
        {
            Texture2D[] results = await Task.WhenAll(loadTasks);
            foreach (var tex in results)
            {
                if (tex != null)
                {
                    slideTexList.Add(tex);
                    currentSlideTextures.Add(tex);
                }
            }
        }

        // 슬라이드쇼 시작
        if (slideshow != null)
        {
            slideshow.Initialize(slideTexList);
        }
    }

    /// <summary>현재 보유한 2,3계층 리소스를 모두 파괴합니다.</summary>
    private void CleanupCurrentResources()
    {
        // 배경
        if (backgroundImage != null) backgroundImage.texture = null;
        DestroyTexture(ref currentBackgroundTexture);

        // 슬라이드
        foreach (var tex in currentSlideTextures)
        {
            Texture2D temp = tex;
            DestroyTexture(ref temp);
        }
        currentSlideTextures.Clear();
    }

    /// <summary>텍스처를 안전하게 파괴합니다.</summary>
    private void DestroyTexture(ref Texture2D tex)
    {
        if (tex != null)
        {
            Object.Destroy(tex);
            tex = null;
        }
    }

    /// <summary>0001~0006 탭 ID 목록을 반환합니다.</summary>
    private List<string> GetAllTabItemIds()
    {
        // ResourcePathCache에서 _btn_off 키를 기반으로 탭 대상 ID 추출
        List<string> btnKeys = resourcePathCache.GetButtonKeys();
        List<string> ids = new List<string>();

        // Settings.txt 제외 목록
        string excludeRaw = AppConfig.ExcludeGridButtons;
        HashSet<string> excludeSet = new HashSet<string>(
            excludeRaw.Split(';')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k)),
            System.StringComparer.OrdinalIgnoreCase
        );

        foreach (string key in btnKeys)
        {
            string id = resourcePathCache.ExtractItemId(key);
            // 제외 목록 확인
            if (excludeSet.Contains(id) || excludeSet.Contains(id + "_btn") ||
                excludeSet.Contains(id + "_btn_off"))
                continue;
            ids.Add(id);
        }

        return ids;
    }

    private void OnDestroy()
    {
        CleanupCurrentResources();
        DestroyTexture(ref specialBtnTexture); // 프로그램 종료 시 0000 버튼 텍스처 해제
        if (tabBar != null) tabBar.OnTabClicked -= OnTabClicked;
    }
}
