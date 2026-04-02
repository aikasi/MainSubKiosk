using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사진을 3초마다 자동으로 순환 표시하는 독립 슬라이드쇼 모듈입니다.
/// 좌/우 버튼으로 수동 이동도 가능하며, 사진이 1장이면 자동 전환과 버튼을 비활성화합니다.
/// 메모리 최적화를 위해 외부 경로를 받아 백그라운드에서 현재/앞/뒤 3장만 Lazy Loading 합니다.
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

    // ── 내부 상태 (Lazy Loading) ──
    private ImageLoader imageLoader;
    private readonly List<string> slidePaths = new List<string>();
    private Texture2D[] loadedTextures;
    private bool[] isCurrentlyLoading;
    
    // 현재 모듈 활성화 세션 토큰 (닫기 광클 대비 메모리 릭 방지)
    private int currentSessionId = 0;
    
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
        if (!isActive || slidePaths.Count <= 1) return;

        timer += Time.unscaledDeltaTime;
        if (timer >= autoAdvanceInterval)
        {
            GoNext();
            timer = 0f;
        }
    }

    /// <summary>
    /// 슬라이드쇼를 새로운 경로 목록과 ImageLoader로 초기화하고 시작합니다.
    /// Overlapping 패턴: 새 이미지가 준비될 때까지 이전 이미지를 화면에 유지하여 깜빡임을 방지합니다.
    /// </summary>
    public async Task InitializeAsync(List<string> paths, ImageLoader loader)
    {
        // ── Overlapping 패턴: 화면에 표시 중인 이전 텍스처를 임시 보관 ──
        Texture2D oldDisplayTexture = (displayImage != null)
            ? displayImage.texture as Texture2D
            : null;

        // 이전 세션 무효화 및 리소스 정리 (displayImage는 건드리지 않음)
        isActive = false;
        currentSessionId++;

        // 이전 텍스처 배열 정리 (화면에 표시 중인 것만 보존)
        if (loadedTextures != null)
        {
            for (int i = 0; i < loadedTextures.Length; i++)
            {
                if (loadedTextures[i] != null && loadedTextures[i] != oldDisplayTexture)
                {
                    Destroy(loadedTextures[i]);
                }
                loadedTextures[i] = null;
            }
        }

        slidePaths.Clear();
        currentIndex = 0;
        timer = 0f;
        // displayImage.texture를 null로 설정하지 않음 (이전 이미지 유지)
        SetNavigationVisible(false);

        // ── 새 초기화 시작 ──
        imageLoader = loader;

        if (paths == null || paths.Count == 0 || loader == null)
        {
            isActive = false;
            SetNavigationVisible(false);
            if (displayImage != null) displayImage.texture = null;
            // 이전 텍스처 안전 파괴
            if (oldDisplayTexture != null) Destroy(oldDisplayTexture);
            return;
        }

        slidePaths.AddRange(paths);
        loadedTextures = new Texture2D[paths.Count];
        isCurrentlyLoading = new bool[paths.Count];
        isActive = true;

        // 사진이 1장이면 버튼 비활성화 + 자동 전환 중단
        bool hasMultiple = slidePaths.Count > 1;
        SetNavigationVisible(hasMultiple);

        // 첫 번째 슬라이드를 await로 로드하여 까만 화면 방지
        int sessionId = currentSessionId;
        Texture2D firstTex = await imageLoader.LoadTextureAsync(slidePaths[0]);

        // 로딩 완료 후 세션 만료 체크 (로딩 중 닫기/전환 발생 시 안전 파기)
        if (sessionId != currentSessionId || !isActive)
        {
            if (firstTex != null) Destroy(firstTex);
            if (oldDisplayTexture != null) Destroy(oldDisplayTexture);
            return;
        }

        loadedTextures[0] = firstTex;
        ShowSlide(0); // 새 이미지가 화면에 표시됨

        // ── Overlapping 완료: 이전 텍스처를 안전하게 파괴 (새 이미지가 화면에 올라간 후) ──
        if (oldDisplayTexture != null && oldDisplayTexture != firstTex)
        {
            Destroy(oldDisplayTexture);
        }

        // 앞/뒤 이웃 슬라이드 백그라운드 프리로드 시작
        if (slidePaths.Count > 1)
        {
            int nextIdx = 1;
            int prevIdx = slidePaths.Count - 1;

            if (loadedTextures[nextIdx] == null && !isCurrentlyLoading[nextIdx])
                LoadSlideAsync(nextIdx, currentSessionId);
            if (prevIdx != nextIdx && loadedTextures[prevIdx] == null && !isCurrentlyLoading[prevIdx])
                LoadSlideAsync(prevIdx, currentSessionId);
        }
    }

    /// <summary>다음 사진으로 이동합니다. 마지막이면 첫 번째로 돌아갑니다.</summary>
    public void GoNext()
    {
        if (slidePaths.Count <= 1) return;
        int nextIdx = (currentIndex + 1) % slidePaths.Count;
        ChangeSlide(nextIdx);
        timer = 0f; // 수동 이동 시 타이머 리셋
    }

    /// <summary>이전 사진으로 이동합니다. 첫 번째이면 마지막으로 돌아갑니다.</summary>
    public void GoPrev()
    {
        if (slidePaths.Count <= 1) return;
        int prevIdx = (currentIndex - 1 + slidePaths.Count) % slidePaths.Count;
        ChangeSlide(prevIdx);
        timer = 0f; // 수동 이동 시 타이머 리셋
    }

    /// <summary>
    /// 지정된 인덱스로 이동하며, 현재/앞/뒤 이미지만 메모리에 올리고 나머지는 즉시 해제합니다.
    /// Zero GC: HashSet을 사용하지 않고 직접 인덱스를 비교합니다.
    /// </summary>
    private void ChangeSlide(int index)
    {
        currentIndex = index;

        // 1. Zero GC: 필요한 인덱스를 직접 계산 (HashSet 미사용 — 힙 할당 0)
        int neededNext = slidePaths.Count > 1
            ? (currentIndex + 1) % slidePaths.Count
            : currentIndex;
        int neededPrev = slidePaths.Count > 1
            ? (currentIndex - 1 + slidePaths.Count) % slidePaths.Count
            : currentIndex;

        // 2. 필요 없는 인덱스의 리소스만 파기 (Unload)
        for (int i = 0; i < slidePaths.Count; i++)
        {
            if (loadedTextures[i] != null
                && i != currentIndex && i != neededNext && i != neededPrev)
            {
                UnloadSlide(i);
            }
        }

        // 3. 현재 슬라이드 즉시 표시 시도
        ShowSlide(currentIndex);

        // 4. 필요한 인덱스 로딩 (아직 로딩 안 된 것들만, 중복 요청 방지)
        if (loadedTextures[currentIndex] == null && !isCurrentlyLoading[currentIndex])
            LoadSlideAsync(currentIndex, currentSessionId);

        if (neededNext != currentIndex
            && loadedTextures[neededNext] == null && !isCurrentlyLoading[neededNext])
            LoadSlideAsync(neededNext, currentSessionId);

        if (neededPrev != currentIndex && neededPrev != neededNext
            && loadedTextures[neededPrev] == null && !isCurrentlyLoading[neededPrev])
            LoadSlideAsync(neededPrev, currentSessionId);
    }

    /// <summary>백그라운드에서 이미지를 로드하고 파기해야 할지 결정합니다.</summary>
    private async void LoadSlideAsync(int index, int sessionId)
    {
        if (index < 0 || index >= slidePaths.Count || imageLoader == null) return;
        
        isCurrentlyLoading[index] = true;
        
        Texture2D tex = await imageLoader.LoadTextureAsync(slidePaths[index]);
        
        // 1. 로딩 완료 후 검증: 세션 만료, 중지됨 체크
        if (sessionId != currentSessionId || !isActive)
        {
            if (tex != null) Destroy(tex);
            if (index < isCurrentlyLoading.Length) isCurrentlyLoading[index] = false;
            return;
        }

        // 2. 광클 등으로 인해 로딩 끝나는 시점에 이미 현재 화면에서 너무 멀리 벗어났나 체크
        bool stillNeeded = IsIndexNeeded(index);
        
        if (!stillNeeded && tex != null)
        {
            Destroy(tex);
            isCurrentlyLoading[index] = false;
            return;
        }

        // 정상 할당
        loadedTextures[index] = tex;
        isCurrentlyLoading[index] = false;

        // 방금 로딩 완료된 사진이 하필 '현재 봐야하는' 화면이라면 즉시 갱신
        if (currentIndex == index && displayImage != null)
        {
            displayImage.texture = tex;
        }
    }

    /// <summary>
    /// 특정 인덱스의 텍스처를 파기합니다.
    /// </summary>
    private void UnloadSlide(int index)
    {
        if (index < 0 || index >= loadedTextures.Length) return;
        
        if (loadedTextures[index] != null)
        {
            Destroy(loadedTextures[index]);
            loadedTextures[index] = null;
        }
    }

    /// <summary>
    /// 해당 인덱스가 '현재/앞/뒤' 중 하나에 속하는지 판별합니다.
    /// </summary>
    private bool IsIndexNeeded(int index)
    {
        if (slidePaths.Count <= 1) return index == 0;
        if (index == currentIndex) return true;
        if (index == (currentIndex + 1) % slidePaths.Count) return true;
        if (index == (currentIndex - 1 + slidePaths.Count) % slidePaths.Count) return true;
        return false;
    }

    /// <summary>
    /// 지정된 인덱스의 사진을 화면에 표시합니다.
    /// </summary>
    private void ShowSlide(int index)
    {
        if (index < 0 || index >= slidePaths.Count) return;
        if (displayImage != null)
        {
            // 아직 로딩 중이라면 null이 들어가 빈 화면(또는 직전거) 상태로 잠깐 노출될 수 있음
            // 이는 광클 시 메모리 폭주를 막는 정상적인 Trade-off
            Texture2D tex = loadedTextures[index];
            displayImage.texture = tex;
            UpdateAspectRatio(tex);
        }
    }

    /// <summary>좌/우 버튼의 표시 여부를 설정합니다.</summary>
    private void SetNavigationVisible(bool visible)
    {
        if (prevButton != null) prevButton.gameObject.SetActive(visible);
        if (nextButton != null) nextButton.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 사진마다 높이가 다를 경우(예: 829 vs 914), 비율이 찌그러지지 않도록
    /// AspectRatioFitter를 찾아 비율을 실시간으로 업데이트합니다.
    /// </summary>
    private void UpdateAspectRatio(Texture2D tex)
    {
        if (displayImage == null || tex == null) return;
        
        AspectRatioFitter fitter = displayImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.aspectRatio = (float)tex.width / tex.height;
        }
    }

    /// <summary>슬라이드쇼를 중지하고 모든 메모리를 강제 회수합니다.</summary>
    public void Stop()
    {
        isActive = false;
        currentSessionId++; // 진행 중인 비동기 태스크들의 소유권을 끊어버림
        
        if (loadedTextures != null)
        {
            for (int i = 0; i < loadedTextures.Length; i++)
            {
                UnloadSlide(i);
            }
        }
        
        slidePaths.Clear();
        currentIndex = 0;
        timer = 0f;
        
        if (displayImage != null) displayImage.texture = null;
        SetNavigationVisible(false);
    }

    private void OnDestroy()
    {
        Stop();
    }
}
