using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StreamingAssets의 _btn_off / _btn_on 이미지를 기반으로 버튼을 동적 생성하고,
/// 외부 이미지를 비동기로 바인딩합니다.
/// On/Off 상태 전환을 지원하여 클릭 피드백을 제공합니다.
/// </summary>
public class ButtonBinder : MonoBehaviour
{
    [Tooltip("ResourcePathCache 참조 (Inspector에서 연결)")]
    [SerializeField] private ResourcePathCache resourcePathCache;

    [Tooltip("ImageLoader 참조 (Inspector에서 연결)")]
    [SerializeField] private ImageLoader imageLoader;

    [Tooltip("동적으로 생성할 버튼 프리팹")]
    [SerializeField] private GameObject buttonPrefab;

    [Tooltip("버튼을 배치할 부모 Transform (Layout Group 권장)")]
    [SerializeField] private Transform buttonContainer;

    // ── 런타임 데이터 ──
    private readonly List<Button> buttons = new List<Button>();
    private readonly Dictionary<int, string> buttonIdMap = new Dictionary<int, string>();
    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();

    // ── On/Off Sprite 쌍 저장 ──
    private readonly Dictionary<int, Sprite> offSprites = new Dictionary<int, Sprite>();
    private readonly Dictionary<int, Sprite> onSprites = new Dictionary<int, Sprite>();

    /// <summary>버튼 리스트 접근자</summary>
    public List<Button> Buttons => buttons;

    /// <summary>바인딩 완료 여부</summary>
    public bool IsBindingComplete { get; private set; }

    /// <summary>버튼 인덱스로부터 아이템 ID를 조회합니다.</summary>
    public string GetItemId(int buttonIndex)
    {
        return buttonIdMap.TryGetValue(buttonIndex, out string id) ? id : string.Empty;
    }

    /// <summary>
    /// 특정 버튼의 On/Off 상태를 전환합니다.
    /// </summary>
    public void SetButtonState(int buttonIndex, bool isOn)
    {
        if (!buttons.IsValidIndex(buttonIndex)) return;

        Button btn = buttons[buttonIndex];
        if (btn == null) return;

        Image buttonImage = btn.GetComponent<Image>();
        if (buttonImage == null) return;

        if (isOn && onSprites.TryGetValue(buttonIndex, out Sprite onSpr))
        {
            buttonImage.sprite = onSpr;
        }
        else if (!isOn && offSprites.TryGetValue(buttonIndex, out Sprite offSpr))
        {
            buttonImage.sprite = offSpr;
        }
    }

    /// <summary>
    /// 아이템 ID로 버튼 인덱스를 찾습니다.
    /// </summary>
    public int GetButtonIndex(string itemId)
    {
        foreach (var pair in buttonIdMap)
        {
            if (string.Equals(pair.Value, itemId, System.StringComparison.OrdinalIgnoreCase))
                return pair.Key;
        }
        return -1;
    }

    /// <summary>
    /// 모든 버튼을 프리팹에서 동적 생성하고 이미지를 비동기로 바인딩합니다.
    /// </summary>
    public async Task BindAllButtonsAsync()
    {
        IsBindingComplete = false;

        if (resourcePathCache == null || imageLoader == null)
        {
            LogError("시스템 통신 오류: 설정 장치(ResourcePathCache, ImageLoader) 연결이 누락되었습니다.");
            return;
        }

        if (buttonPrefab == null || buttonContainer == null)
        {
            LogError("화면 설정 오류: 버튼 프리팹 또는 컨테이너 설정이 누락되었습니다.");
            return;
        }

        // 기존 동적 버튼 정리
        ClearButtons();

        // 캐시에서 _btn_off 키 목록 가져오기
        List<string> btnKeys = resourcePathCache.GetButtonKeys();

        // Settings.txt에서 그리드 생성 제외 목록 읽어오기
        string excludeRaw = AppConfig.ExcludeGridButtons;
        HashSet<string> excludeKeys = new HashSet<string>(
            excludeRaw.Split(';')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // 제외 키를 _btn_off 형식으로도 변환하여 필터링
        if (excludeKeys.Count > 0)
        {
            btnKeys = btnKeys.Where(k =>
            {
                string itemId = resourcePathCache.ExtractItemId(k);
                // 제외 목록의 원본 ID나 _btn 형태와 매칭
                return !excludeKeys.Contains(k) &&
                       !excludeKeys.Contains(itemId + "_btn") &&
                       !excludeKeys.Contains(itemId + "_btn_off") &&
                       !excludeKeys.Contains(itemId);
            }).ToList();
        }

        if (btnKeys.Count == 0)
        {
            Debug.LogWarning("[WARN] _btn_off 이미지가 없습니다. 버튼이 생성되지 않습니다.");
            return;
        }

        Debug.Log($"[INFO] 버튼 {btnKeys.Count}개 동적 생성 시작");

        // 비동기 로딩 태스크 리스트
        var loadTasks = new List<Task>();

        for (int i = 0; i < btnKeys.Count; i++)
        {
            string btnOffKey = btnKeys[i];
            string itemId = resourcePathCache.ExtractItemId(btnOffKey);
            string btnOnKey = itemId + "_btn_on";

            // 프리팹 생성 (부모는 여전히 buttonContainer를 사용합니다)
            GameObject newObj = Instantiate(buttonPrefab, buttonContainer);
            Button newBtn = newObj.GetComponentInChildren<Button>();
            
            if (newBtn != null) newBtn.interactable = false;
            newObj.name = $"Button_{itemId}";
            buttons.Add(newBtn);
            buttonIdMap[i] = itemId;

            // 비동기 이미지 로딩 (병렬)
            int capturedIndex = i;
            loadTasks.Add(LoadButtonSpritesAsync(capturedIndex, btnOffKey, btnOnKey));
        }

        // 모든 버튼 이미지 병렬 로드 대기
        if (loadTasks.Count > 0)
        {
            await Task.WhenAll(loadTasks);
        }

        IsBindingComplete = true;
        Debug.Log($"[INFO] 전체 버튼 바인딩 완료: {btnKeys.Count}개 성공");
    }

    /// <summary>
    /// 단일 버튼의 Off/On Sprite를 비동기로 로드하고 할당합니다.
    /// </summary>
    private async Task LoadButtonSpritesAsync(int index, string offKey, string onKey)
    {
        Button button = buttons[index];

        // Off 이미지 로드 (필수)
        if (resourcePathCache.TryGetPath(offKey, out string offPath))
        {
            Texture2D offTex = await imageLoader.LoadTextureAsync(offPath);
            if (offTex != null)
            {
                Sprite offSprite = imageLoader.CreateSprite(offTex);
                loadedTextures.Add(offTex);
                offSprites[index] = offSprite;

                // 기본 상태는 Off
                Image btnImage = button.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.sprite = offSprite;
                    btnImage.preserveAspect = true; // 비율 유지 (프리팹 기준 크기 안에 꽉 차게 들어감)
                }
            }
        }

        // On 이미지 로드 (선택)
        if (resourcePathCache.TryGetPath(onKey, out string onPath))
        {
            Texture2D onTex = await imageLoader.LoadTextureAsync(onPath);
            if (onTex != null)
            {
                Sprite onSprite = imageLoader.CreateSprite(onTex);
                loadedTextures.Add(onTex);
                onSprites[index] = onSprite;
            }
        }

        // 로딩 완료 → 버튼 활성화
        button.interactable = true;
    }

    /// <summary>동적 생성된 버튼을 모두 파괴하고 리스트를 초기화합니다.</summary>
    private void ClearButtons()
    {
        foreach (var tex in loadedTextures)
        {
            if (tex != null) Object.Destroy(tex);
        }
        loadedTextures.Clear();

        foreach (var btn in buttons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        buttons.Clear();
        buttonIdMap.Clear();
        offSprites.Clear();
        onSprites.Clear();
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ERROR] ButtonBinder: {message}");
    }

    private void OnDestroy()
    {
        ClearButtons();
    }
}

/// <summary>
/// List 인덱스 유효성 검사 확장 메서드
/// </summary>
public static class ListExtensions
{
    public static bool IsValidIndex<T>(this List<T> list, int index)
    {
        return index >= 0 && index < list.Count;
    }
}
