using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StreamingAssets의 -btn 이미지 갯수에 따라 버튼을 프리팹에서 동적 생성하고,
/// 외부 이미지를 비동기로 바인딩합니다.
/// 로딩 완료 전까지 버튼을 비활성화(Interactable=false)하여
/// 미완성 상태에서의 터치를 차단합니다.
/// </summary>
public class ButtonBinder : MonoBehaviour
{
    [Tooltip("ResourcePathCache 참조 (Inspector에서 연결)")]
    [SerializeField] private ResourcePathCache resourcePathCache;

    [Tooltip("ImageLoader 참조 (Inspector에서 연결)")]
    [SerializeField] private ImageLoader imageLoader;

    [Tooltip("동적으로 생성할 버튼 프리팹")]
    [SerializeField] private Button buttonPrefab;

    [Tooltip("버튼을 배치할 부모 Transform (Layout Group 권장)")]
    [SerializeField] private Transform buttonContainer;

    // ── 런타임에 동적 생성된 버튼 리스트 ──
    private readonly List<Button> buttons = new List<Button>();

    // ── 버튼별 ID 매핑 (버튼 인덱스 → 아이템 ID) ──
    private readonly Dictionary<int, string> buttonIdMap = new Dictionary<int, string>();

    // ── 로드된 텍스처 추적 (메모리 누수 방지용) ──
    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();

    /// <summary>
    /// 버튼 리스트 접근자 (MainScreenManager에서 참조용)
    /// </summary>
    public List<Button> Buttons => buttons;

    /// <summary>
    /// 바인딩 완료 여부
    /// </summary>
    public bool IsBindingComplete { get; private set; }

    /// <summary>
    /// 버튼 인덱스로부터 아이템 ID를 조회합니다.
    /// 예: 버튼 0 → "0001"
    /// </summary>
    public string GetItemId(int buttonIndex)
    {
        return buttonIdMap.TryGetValue(buttonIndex, out string id) ? id : string.Empty;
    }

    /// <summary>
    /// 모든 버튼을 프리팹에서 동적 생성하고 이미지를 비동기로 바인딩합니다.
    /// 로딩 완료 후 Interactable을 활성화합니다.
    /// </summary>
    public async Task BindAllButtonsAsync()
    {
        IsBindingComplete = false;

        if (resourcePathCache == null || imageLoader == null)
        {
            LogError("시스템 통신 오류: 설정 장치(ResourcePathCache, ImageLoader) 연결이 누락되었습니다.");
            return;
        }

        if (buttonPrefab == null)
        {
            LogError("화면 설정 오류: 버튼을 생성하기 위한 원본 디자인(buttonPrefab)이 설정되지 않았습니다.");
            return;
        }

        if (buttonContainer == null)
        {
            LogError("화면 설정 오류: 버튼들이 나열될 부모 폴더(buttonContainer) 설정이 빠져 있습니다.");
            return;
        }

        // 기존 동적 버튼 정리 (재초기화 대응)
        ClearButtons();

        // 캐시에서 버튼 키 목록 가져오기
        List<string> btnKeys = resourcePathCache.GetButtonKeys();

        // 💡 Settings.txt에서 그리드 생성 제외 목록 읽어오기
        string excludeRaw = AppConfig.ExcludeGridButtons;
        HashSet<string> excludeKeys = new HashSet<string>(
            excludeRaw.Split(';')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k)),
            System.StringComparer.OrdinalIgnoreCase // 대소문자 무시 비교
        );

        // 제외 목록에 있는 키 제거(필터링)
        if (excludeKeys.Count > 0)
        {
            btnKeys = btnKeys.Where(k => !excludeKeys.Contains(k)).ToList();
        }

        if (btnKeys.Count == 0)
        {
            string warnMsg = "[WARN] -btn 이미지가 없습니다. 버튼이 생성되지 않습니다.";
            Debug.LogWarning(warnMsg);

            return;
        }

        string createMsg = $"[INFO] 버튼 {btnKeys.Count}개 동적 생성 시작";
        Debug.Log(createMsg);


        // btn 키 개수만큼 프리팹에서 버튼 생성
        for (int i = 0; i < btnKeys.Count; i++)
        {
            Button newBtn = Instantiate(buttonPrefab, buttonContainer);
            newBtn.interactable = false;
            newBtn.gameObject.name = $"Button_{btnKeys[i]}";
            buttons.Add(newBtn);
        }

        // 버튼 바인딩
        for (int i = 0; i < btnKeys.Count; i++)
        {
            string btnKey = btnKeys[i];
            Button button = buttons[i];

            // 아이템 ID 추출 (예: "0001-BTN" → "0001")
            string itemId = btnKey.EndsWith("-btn", System.StringComparison.OrdinalIgnoreCase)
                ? btnKey.Substring(0, btnKey.Length - 4)
                : btnKey;
            buttonIdMap[i] = itemId;

            // 이미지 경로 조회
            if (!resourcePathCache.TryGetPath(btnKey, out string imagePath))
            {
                LogError($"시스템 오류: 화면에 표시할 '{btnKey}' 버튼의 이미지를 찾을 수 없습니다.");
                continue;
            }

            // 비동기 이미지 로딩
            Texture2D texture = await imageLoader.LoadTextureAsync(imagePath);
            if (texture == null)
            {
                LogError($"'{btnKey}' 버튼의 이미지를 화면에 불러오는 중 에러가 발생했습니다.");
                continue;
            }

            // Sprite 생성 및 할당
            Sprite sprite = imageLoader.CreateSprite(texture);
            if (sprite == null)
            {
                Object.Destroy(texture);
                continue;
            }

            // 텍스처 추적 리스트에 등록 (메모리 누수 방지)
            loadedTextures.Add(texture);

            // UI.Image에 Sprite 할당
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = sprite;
            }

            // 로딩 완료 → 버튼 활성화
            button.interactable = true;

            string infoMsg = $"[INFO] 버튼 바인딩 완료: {btnKey} → Button[{i}]";
            Debug.Log(infoMsg);

        }

        IsBindingComplete = true;
        string completeMsg = $"[INFO] 전체 버튼 바인딩 완료: {btnKeys.Count}개 성공";
        Debug.Log(completeMsg);

    }

    /// <summary>
    /// 동적 생성된 버튼을 모두 파괴하고 리스트를 초기화합니다.
    /// </summary>
    private void ClearButtons()
    {
        // 로드된 텍스처 메모리 해제 (버튼 GameObject 파괴만으로는 Texture가 해제되지 않음)
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
    }

    /// <summary>
    /// 에러 로그를 기록합니다.
    /// </summary>
    private void LogError(string message)
    {
        string fullMsg = $"[ERROR] ButtonBinder: {message}";
        Debug.LogError(fullMsg);

    }

    private void OnDestroy()
    {
        ClearButtons();
    }
}
