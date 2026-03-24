using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 0000 전용 페이지를 관리합니다.
/// 단일 이미지(0000_page.png)를 표시하고, Close 버튼으로 메인 화면에 복귀합니다.
/// </summary>
public class SpecialPageManager : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("0000 페이지 컨테이너 (CanvasGroup 필수)")]
    [SerializeField] private CanvasGroup specialPanel;

    [Tooltip("0000 페이지 이미지를 표시할 RawImage")]
    [SerializeField] private RawImage pageImage;

    [Header("시스템 참조")]
    [SerializeField] private ImageLoader imageLoader;
    [SerializeField] private ResourcePathCache resourcePathCache;

    [Header("닫기 버튼")]
    [Tooltip("Close 버튼 (내장 Sprite 사용)")]
    [SerializeField] private Button closeButton;

    [Tooltip("PageNavigator 참조")]
    [SerializeField] private PageNavigator pageNavigator;

    [Header("전환 효과")]
    [Tooltip("ITransitionEffect 구현체")]
    [SerializeField] private MonoBehaviour transitionEffectComponent;

    // ── 내부 상태 ──
    private ITransitionEffect transitionEffect;
    private Texture2D currentTexture;
    private Texture2D closeBtnTexture; // 0000_close 버튼 이미지 캐싱용
    private bool isOpening = false;

    /// <summary>0000 페이지가 표시 중인지 여부</summary>
    public bool IsShowing { get; private set; }

    private void Awake()
    {
        if (transitionEffectComponent != null)
        {
            transitionEffect = transitionEffectComponent as ITransitionEffect;
        }

        if (closeButton != null && pageNavigator != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                Debug.Log("[INFO] 0000 페이지 Close 클릭 — 메인 화면으로 복귀");
                pageNavigator.ReturnToMain();
            });
        }

        // 초기 상태: 숨김
        if (specialPanel != null)
        {
            specialPanel.alpha = 0f;
            specialPanel.blocksRaycasts = false;
            specialPanel.interactable = false;
            specialPanel.gameObject.SetActive(false);
        }
    }

    /// <summary>0000 페이지를 표시합니다.</summary>
    public async void Show()
    {
        if (IsShowing || isOpening) return;
        if (specialPanel == null) return;

        isOpening = true;

        try
        {
            // 이전 텍스처 정리
            CleanupTexture();

            // 0000_close 버튼 이미지 동적 1회 로드 및 적용
            if (closeBtnTexture == null && closeButton != null)
            {
                if (resourcePathCache.TryGetPath("0000_close", out string closeBtnPath) || 
                    resourcePathCache.TryGetPath("0000_close_btn", out closeBtnPath))
                {
                    closeBtnTexture = await imageLoader.LoadTextureAsync(closeBtnPath);
                    if (closeBtnTexture != null)
                    {
                        Image btnImg = closeButton.GetComponent<Image>();
                        if (btnImg != null)
                        {
                            btnImg.sprite = imageLoader.CreateSprite(closeBtnTexture);
                            
                        }
                    }
                }
            }

            // 0000_page 이미지 로드
            string pageKey = "0000_page";
            if (resourcePathCache.TryGetPath(pageKey, out string pagePath))
            {
                Texture2D tex = await imageLoader.LoadTextureAsync(pagePath);
                if (tex != null)
                {
                    currentTexture = tex;
                    if (pageImage != null) pageImage.texture = tex;
                }
                else
                {
                    Debug.LogError("[ERROR] 0000_page 이미지 로드에 실패했습니다.");
                    return;
                }
            }
            else
            {
                Debug.LogError("[ERROR] 0000_page 키가 캐시에 존재하지 않습니다.");
                return;
            }

            // 패널 활성화
            specialPanel.gameObject.SetActive(true);
            specialPanel.blocksRaycasts = true;
            specialPanel.interactable = true;
            IsShowing = true;

            if (transitionEffect != null)
            {
                await transitionEffect.PlayEnterAsync(specialPanel);
            }
            else
            {
                specialPanel.alpha = 1f;
            }

            Debug.Log("[INFO] 0000 전용 페이지 표시 완료");
        }
        finally
        {
            isOpening = false;
        }
    }

    /// <summary>0000 페이지를 숨기고 텍스처를 정리합니다.</summary>
    public async void Hide()
    {
        if (!IsShowing) return;
        if (specialPanel == null) return;

        if (transitionEffect != null)
        {
            await transitionEffect.PlayExitAsync(specialPanel);
        }
        else
        {
            specialPanel.alpha = 0f;
            specialPanel.gameObject.SetActive(false);
        }

        specialPanel.blocksRaycasts = false;
        specialPanel.interactable = false;
        IsShowing = false;

        CleanupTexture();
        Debug.Log("[INFO] 0000 전용 페이지 숨김 및 리소스 정리 완료");
    }

    private void CleanupTexture()
    {
        if (pageImage != null) pageImage.texture = null;
        if (currentTexture != null)
        {
            Object.Destroy(currentTexture);
            currentTexture = null;
        }
    }

    private void OnDestroy()
    {
        CleanupTexture();
        if (closeBtnTexture != null)
        {
            Object.Destroy(closeBtnTexture);
            closeBtnTexture = null;
        }
    }
}
