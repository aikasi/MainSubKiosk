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

        // 💡 Settings.txt에서 외부 버튼 키를 자동으로 가져오기
        string excludeRaw = AppConfig.ExcludeGridButtons;
        if (string.IsNullOrWhiteSpace(excludeRaw))
        {
            Debug.LogWarning($"[WARN] {gameObject.name}: Settings.txt에 Exclude_Grid_Buttons 값이 비어있어 외부 버튼을 설정할 수 없습니다.");
            return;
        }

        // 첫 번째 키를 사용 (현재 앱은 외부 버튼 1개)
        buttonKey = excludeRaw.Split(';')[0].Trim();

        if (string.IsNullOrEmpty(buttonKey))
        {
            Debug.LogWarning($"[WARN] {gameObject.name}: 할당할 Button Key가 비어있습니다.");
            return;
        }

        // 이미지 로딩 중 클릭 방지
        myButton.interactable = false;

        // 🚨 버그 방어 1 (Race Condition): 메인 시스템(ResourcePathCache)이 윈도우 폴더 스캔을 다 끝낼 때까지 기다림
        while (!resourcePathCache.IsInitialized)
        {
            await Task.Yield();
        }

        // 2. 캐시 메모리에서 지정된 키의 실제 파일 경로를 조회
        if (!resourcePathCache.TryGetPath(buttonKey, out string imagePath))
        {
            Debug.LogError($"[ERROR] 시스템 오류: 화면 고정 버튼에 쓸 '{buttonKey}' 이미지를 찾을 수 없습니다.");
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
                Destroy(_loadedTexture); // Sprite 생성 실패 시 메모리 정리
                _loadedTexture = null;
            }
        }
        else
        {
            Debug.LogError($"[ERROR] '{buttonKey}' 버튼의 이미지를 화면에 불러오는 중 에러가 발생했습니다.");
        }

        // 4. 클릭 시 대응하는 팝업 열기 이벤트 연결
        // 🚨 버그 방어 2 (-PAGE 중복 이름 버그): 기존 시스템은 '0000' 만 받아서 뒤에 '-PAGE'를 직접 붙입니다.
        // 따라서 '0000-BTN' 에서 '-BTN' 꼬리표를 떼어내서 순수 아이디(0000)만 추출해 전달해야 '0000-PAGE-PAGE' 버그가 생기지 않습니다.
        string itemId = buttonKey.EndsWith("-btn", System.StringComparison.OrdinalIgnoreCase) 
                        ? buttonKey.Substring(0, buttonKey.Length - 4) 
                        : buttonKey;
        
        // 주의: AddListener 내부에서 즉시 사용할 수 있도록 지역 변수로 캡처
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
        // 씬 전환이나 오브젝트 파괴 시 메모리 누수 방지
        if (_loadedTexture != null)
        {
            Destroy(_loadedTexture);
            _loadedTexture = null;
        }
    }
}
