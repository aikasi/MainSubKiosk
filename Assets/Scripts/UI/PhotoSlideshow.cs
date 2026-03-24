using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사진을 3초마다 자동으로 순환 표시하는 독립 슬라이드쇼 모듈입니다.
/// 좌/우 버튼으로 수동 이동도 가능하며, 사진이 1장이면 자동 전환과 버튼을 비활성화합니다.
/// </summary>
public class PhotoSlideshow : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("슬라이드쇼 사진을 표시할 RawImage")]
    [SerializeField] private RawImage displayImage;

    [Tooltip("왼쪽(이전) 이동 버튼")]
    [SerializeField] private Button prevButton;

    [Tooltip("오른쪽(다음) 이동 버튼")]
    [SerializeField] private Button nextButton;

    [Header("자동 전환 설정")]
    [Tooltip("사진 자동 전환 간격 (초)")]
    [SerializeField] private float autoAdvanceInterval = 3f;

    // ── 내부 상태 ──
    private readonly List<Texture2D> slideTextures = new List<Texture2D>();
    private int currentIndex = 0;
    private float timer = 0f;
    private bool isActive = false;

    private void Awake()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(GoPrev);
        if (nextButton != null)
            nextButton.onClick.AddListener(GoNext);
    }

    private void Update()
    {
        if (!isActive || slideTextures.Count <= 1) return;

        timer += Time.unscaledDeltaTime;
        if (timer >= autoAdvanceInterval)
        {
            GoNext();
            timer = 0f;
        }
    }

    /// <summary>
    /// 슬라이드쇼를 새로운 텍스처 목록으로 초기화하고 시작합니다.
    /// 이전 텍스처는 외부(DetailPageManager)에서 관리하므로 여기서 Destroy하지 않습니다.
    /// </summary>
    public void Initialize(List<Texture2D> textures)
    {
        slideTextures.Clear();
        currentIndex = 0;
        timer = 0f;

        if (textures == null || textures.Count == 0)
        {
            isActive = false;
            SetNavigationVisible(false);
            if (displayImage != null) displayImage.texture = null;
            return;
        }

        slideTextures.AddRange(textures);
        isActive = true;

        // 사진이 1장이면 버튼 비활성화 + 자동 전환 중단
        bool hasMultiple = slideTextures.Count > 1;
        SetNavigationVisible(hasMultiple);

        ShowSlide(0);
    }

    /// <summary>다음 사진으로 이동합니다. 마지막이면 첫 번째로 돌아갑니다.</summary>
    public void GoNext()
    {
        if (slideTextures.Count == 0) return;
        currentIndex = (currentIndex + 1) % slideTextures.Count;
        ShowSlide(currentIndex);
        timer = 0f; // 수동 이동 시 타이머 리셋
    }

    /// <summary>이전 사진으로 이동합니다. 첫 번째이면 마지막으로 돌아갑니다.</summary>
    public void GoPrev()
    {
        if (slideTextures.Count == 0) return;
        currentIndex = (currentIndex - 1 + slideTextures.Count) % slideTextures.Count;
        ShowSlide(currentIndex);
        timer = 0f;
    }

    /// <summary>슬라이드쇼를 중지하고 상태를 초기화합니다. 텍스처는 Destroy하지 않습니다.</summary>
    public void Stop()
    {
        isActive = false;
        slideTextures.Clear();
        currentIndex = 0;
        timer = 0f;
        if (displayImage != null) displayImage.texture = null;
        SetNavigationVisible(false);
    }

    /// <summary>지정된 인덱스의 사진을 화면에 표시합니다.</summary>
    private void ShowSlide(int index)
    {
        if (index < 0 || index >= slideTextures.Count) return;
        if (displayImage != null)
        {
            displayImage.texture = slideTextures[index];
        }
    }

    /// <summary>좌/우 버튼의 표시 여부를 설정합니다.</summary>
    private void SetNavigationVisible(bool visible)
    {
        if (prevButton != null) prevButton.gameObject.SetActive(visible);
        if (nextButton != null) nextButton.gameObject.SetActive(visible);
    }
}
