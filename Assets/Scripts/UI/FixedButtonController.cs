using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 그리드(ButtonBinder) 외부에 수동으로 배치한 UI 버튼에
/// Settings.txt의 Exclude_Grid_Buttons에 지정된 이미지를 씌우고
/// 클릭 이벤트를 자동으로 연결하는 스크립트입니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class FixedButtonController : MonoBehaviour
{
    [Header("시스템 매니저 연결 (필수)")]
    [SerializeField] private ResourcePathCache resourcePathCache;
    [SerializeField] private ImageLoader imageLoader;
    [SerializeField] private PageNavigator pageNavigator;

    // Settings.txt의 Exclude_Grid_Buttons에서 자동으로 읽어오는 버튼 키
    private string buttonKey;

    private Button myButton;
    private Texture2D _loadedTexture;

    private async void Start()
    {
        myButton = GetComponent<Button>();

        // 1. 필수 컴포넌트 유효성 검사
        if (resourcePathCache == null || imageLoader == null || pageNavigator == null)
        {
            Debug.LogError($"[ERROR] {gameObject.name}의 FixedButtonController: 필수 시스템 매니저 연결이 누락되어 버튼을 사용할 수 없습니다.");
            return;
        }

        // Settings.txt에서 외부 버튼 키를 자동으로 가져오기
        string excludeRaw = AppConfig.ExcludeGridButtons;
        if (string.IsNullOrWhiteSpace(excludeRaw))
        {
            Debug.LogWarning($"[WARN] {gameObject.name}: Settings.txt에 Exclude_Grid_Buttons 값이 비어있어 외부 버튼을 설정할 수 없습니다.");
            return;
        }

        // 첫 번째 키를 사용
        buttonKey = excludeRaw.Split(';')[0].Trim();

        if (string.IsNullOrEmpty(buttonKey))
        {
            Debug.LogWarning($"[WARN] {gameObject.name}: 할당할 Button Key가 비어있습니다.");
            return;
        }

        // 이미지 로딩 중 클릭 방지
        myButton.interactable = false;

        // ResourcePathCache 초기화 대기
        while (!resourcePathCache.IsInitialized)
        {
            await Task.Yield();
        }

        // 2. 캐시에서 지정된 키의 실제 파일 경로를 조회
        // Settings.txt에 "0000", "0000_btn", "0000-btn" 등 어떤 값이 적혀있든 호환되도록 처리
        string searchKey = buttonKey.ToLowerInvariant();
        if (!searchKey.EndsWith("_btn") && !searchKey.EndsWith("_page"))
        {
            // 순수 숫자(예: "0000")만 적혀있다면 기본적으로 "_btn"을 붙여서 검색
            searchKey = resourcePathCache.ExtractItemId(searchKey) + "_btn";
        }
        else if (searchKey.EndsWith("-btn"))
        {
            searchKey = resourcePathCache.ExtractItemId(searchKey) + "_btn";
        }

        if (!resourcePathCache.TryGetPath(searchKey, out string imagePath))
        {
            Debug.LogError($"[ERROR] 시스템 오류: 화면 고정 버튼에 쓸 '{searchKey}' (원본: {buttonKey}) 이미지를 찾을 수 없습니다.");
            return;
        }

        // 3. 비동기로 이미지를 불러와서 버튼에 씌우기
        _loadedTexture = await imageLoader.LoadTextureAsync(imagePath);
        if (_loadedTexture != null)
        {
            Sprite sprite = imageLoader.CreateSprite(_loadedTexture);
            if (sprite != null)
            {
                Image buttonImage = myButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = sprite;
                    buttonImage.preserveAspect = true;
                }
            }
            else
            {
                Destroy(_loadedTexture);
                _loadedTexture = null;
            }
        }
        else
        {
            Debug.LogError($"[ERROR] '{buttonKey}' 버튼의 이미지를 화면에 불러오는 중 에러가 발생했습니다.");
        }

        // 4. 클릭 이벤트 연결 — 아이템 ID 추출 (언더스코어 기준)
        string itemId = resourcePathCache.ExtractItemId(buttonKey);

        string capturedItemId = itemId;
        myButton.onClick.AddListener(() =>
        {
            pageNavigator.OpenDetail(capturedItemId);
        });

        // 5. 로딩 완료 및 클릭 허용
        myButton.interactable = true;
        Debug.Log($"[INFO] 수동 고정 버튼 '{buttonKey}' 설정 완료.");
    }

    private void OnDestroy()
    {
        if (_loadedTexture != null)
        {
            Destroy(_loadedTexture);
            _loadedTexture = null;
        }
    }
}
