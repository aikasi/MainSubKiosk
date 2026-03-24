using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상세 페이지 하단의 탭 바 [1] [2] [3] [4] [5] [6] 를 제어합니다.
/// 각 탭은 _page_on / _page_off 외부 이미지를 사용하며,
/// 현재 선택된 탭만 On, 나머지는 Off 상태로 표시됩니다.
/// </summary>
public class TabBarController : MonoBehaviour
{
    [Header("시스템 참조")]
    [SerializeField] private ResourcePathCache resourcePathCache;
    [SerializeField] private ImageLoader imageLoader;

    [Header("탭 버튼 (Inspector에서 순서대로 할당)")]
    [SerializeField] private List<Button> tabButtons = new List<Button>();

    // ── 내부 데이터 ──
    private readonly Dictionary<int, Sprite> tabOnSprites = new Dictionary<int, Sprite>();
    private readonly Dictionary<int, Sprite> tabOffSprites = new Dictionary<int, Sprite>();
    private readonly Dictionary<int, string> tabItemIds = new Dictionary<int, string>();
    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();

    private int currentTabIndex = -1;

    /// <summary>탭 클릭 시 외부에서 구독할 콜백 (인자: 새로운 아이템 ID)</summary>
    public event System.Action<string> OnTabClicked;

    /// <summary>현재 활성 탭의 아이템 ID</summary>
    public string CurrentItemId =>
        tabItemIds.TryGetValue(currentTabIndex, out string id) ? id : string.Empty;

    /// <summary>
    /// 탭 바를 초기화합니다. 0001~0006의 _page_on/_page_off 이미지를 로드합니다.
    /// </summary>
    public async Task InitializeAsync(List<string> itemIds)
    {
        ClearTabData();

        if (resourcePathCache == null || imageLoader == null)
        {
            Debug.LogWarning("[WARN] TabBarController: 시스템 참조가 누락되었습니다.");
            return;
        }

        var loadTasks = new List<Task>();

        for (int i = 0; i < itemIds.Count && i < tabButtons.Count; i++)
        {
            string itemId = itemIds[i];
            tabItemIds[i] = itemId;

            int capturedIndex = i;

            // 탭 버튼 클릭 이벤트 등록
            Button btn = tabButtons[i];
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnTabButtonClicked(capturedIndex));
                
                // 초기화 시(새로운 상세 페이지 진입) 이전에 커져 있던 탭 버튼들을 확실히 원상복구
                Image img = (btn.targetGraphic as Image) ?? btn.GetComponentInChildren<Image>();
                if (img != null) img.transform.localScale = Vector3.one;

                btn.gameObject.SetActive(true);
            }

            // 비동기 이미지 로딩
            loadTasks.Add(LoadTabSpritesAsync(capturedIndex, itemId));
        }

        // 사용하지 않는 남은 탭 버튼 숨기기
        for (int i = itemIds.Count; i < tabButtons.Count; i++)
        {
            if (tabButtons[i] != null)
                tabButtons[i].gameObject.SetActive(false);
        }

        if (loadTasks.Count > 0)
        {
            await Task.WhenAll(loadTasks);
        }

        Debug.Log($"[INFO] TabBarController: {itemIds.Count}개 탭 초기화 완료");
    }

    /// <summary>지정된 인덱스의 탭을 활성(On) 상태로 전환합니다.</summary>
    public void SetActiveTab(int tabIndex)
    {
        // 이전 탭 Off
        if (currentTabIndex >= 0 && currentTabIndex < tabButtons.Count)
        {
            SetTabImage(currentTabIndex, false);
        }

        // 새 탭 On
        currentTabIndex = tabIndex;
        if (currentTabIndex >= 0 && currentTabIndex < tabButtons.Count)
        {
            SetTabImage(currentTabIndex, true);
        }
    }

    /// <summary>아이템 ID로 탭 인덱스를 찾아 활성화합니다.</summary>
    public void SetActiveTabByItemId(string itemId)
    {
        foreach (var pair in tabItemIds)
        {
            if (string.Equals(pair.Value, itemId, System.StringComparison.OrdinalIgnoreCase))
            {
                SetActiveTab(pair.Key);
                return;
            }
        }
    }

    /// <summary>특정 탭 버튼 이미지를 On 또는 Off로 설정합니다.</summary>
    private void SetTabImage(int index, bool isOn)
    {
        if (index < 0 || index >= tabButtons.Count) return;

        Button btn = tabButtons[index];
        if (btn == null) return;

        //  버튼과 이미지를 분리를 지원하기 위해 우선 targetGraphic을 찾아봄
        Image img = (btn.targetGraphic as Image) ?? btn.GetComponentInChildren<Image>();
        if (img == null) return;

        if (isOn)
        {
            if (tabOnSprites.TryGetValue(index, out Sprite onSpr)) img.sprite = onSpr;
            // On 상태: 높이를 1.4배로 튀어나오게 강조 (X, Z 유지)
            img.transform.localScale = new Vector3(1f, 1.4f, 1f); 
        }
        else
        {
            if (tabOffSprites.TryGetValue(index, out Sprite offSpr)) img.sprite = offSpr;
            // Off 상태: 이미지 로딩 성공 여부와 관계없이 무조건 기본 비율 복원
            img.transform.localScale = Vector3.one;
        }
    }

    /// <summary>탭 버튼 클릭 시 호출됩니다.</summary>
    private void OnTabButtonClicked(int tabIndex)
    {
        // 이미 선택된 탭이면 무시
        if (tabIndex == currentTabIndex) return;

        if (tabItemIds.TryGetValue(tabIndex, out string itemId))
        {
            OnTabClicked?.Invoke(itemId);
        }
    }

    /// <summary>단일 탭의 On/Off Sprite를 비동기 로드합니다.</summary>
    private async Task LoadTabSpritesAsync(int index, string itemId)
    {
        string onKey = itemId + "_page_on";
        string offKey = itemId + "_page_off";

        // Off 이미지
        if (resourcePathCache.TryGetPath(offKey, out string offPath))
        {
            Texture2D offTex = await imageLoader.LoadTextureAsync(offPath);
            if (offTex != null)
            {
                loadedTextures.Add(offTex);
                tabOffSprites[index] = imageLoader.CreateSprite(offTex);

                // 초기 상태는 Off
                Button btn = tabButtons[index];
                if (btn != null)
                {
                    Image img = (btn.targetGraphic as Image) ?? btn.GetComponentInChildren<Image>();
                    if (img != null) img.sprite = tabOffSprites[index];
                }
            }
        }

        // On 이미지
        if (resourcePathCache.TryGetPath(onKey, out string onPath))
        {
            Texture2D onTex = await imageLoader.LoadTextureAsync(onPath);
            if (onTex != null)
            {
                loadedTextures.Add(onTex);
                tabOnSprites[index] = imageLoader.CreateSprite(onTex);
            }
        }
    }

    /// <summary>탭 데이터를 정리합니다. 텍스처는 경량이므로 앱 종료 시에만 해제합니다.</summary>
    private void ClearTabData()
    {
        tabOnSprites.Clear();
        tabOffSprites.Clear();
        tabItemIds.Clear();
        currentTabIndex = -1;

        foreach (var btn in tabButtons)
        {
            if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }

    private void OnDestroy()
    {
        foreach (var tex in loadedTextures)
        {
            if (tex != null) Object.Destroy(tex);
        }
        loadedTextures.Clear();
    }
}
