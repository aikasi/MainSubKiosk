using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전시관 메인 오케스트레이터.
/// CSV에서 파싱된 데이터를 기반으로 버튼을 동적 생성하고,
/// 버튼 클릭 시 이미지 경로를 스캔하여 상세 페이지에 전달합니다.
/// </summary>
public class ExhibitManager : MonoBehaviour
{
    [Header("데이터 시스템")]
    [Tooltip("CSV 파싱 및 캐싱 매니저")]
    [SerializeField] private ExhibitDataCache dataCache;

    [Header("버튼 생성 설정")]
    [Tooltip("버튼 프리팹 (내부에 Id, Title, Name용 TextMeshProUGUI 3개 필요)")]
    [SerializeField] private GameObject buttonPrefab;

    [Tooltip("버튼이 배치될 부모 Transform (Layout Group 권장)")]
    [SerializeField] private Transform buttonContainer;

    [Header("상세 페이지 연동")]
    [Tooltip("IExhibitDetailReceiver를 구현한 MonoBehaviour를 연결하세요")]
    [SerializeField] private MonoBehaviour detailReceiverComponent;

    // ── 내부 상태 ──
    private IExhibitDetailReceiver detailReceiver;
    private bool isLoading = false; // 광클 방지 플래그
    private readonly List<GameObject> createdButtons = new List<GameObject>();

    private void Awake()
    {
        // 인터페이스 변환 (드래그한 오브젝트에서 직접 스크립트 탐색)
        if (detailReceiverComponent != null)
        {
            detailReceiver = detailReceiverComponent.GetComponent<IExhibitDetailReceiver>();
            if (detailReceiver == null)
            {
                Debug.LogError("[ERROR] ExhibitManager: 연결하신 오브젝트에 IExhibitDetailReceiver (예: MediaBookManager)가 부착되어 있지 않습니다.");
            }
        }
    }

    private void Start()
    {
        // 유니티 실행 시 자동으로 버튼 생성을 시작합니다.
        Initialize();
    }

    /// <summary>
    /// 외부에서 호출하여 전시관 시스템을 초기화합니다.
    /// ExhibitDataCache 초기화 후 버튼을 동적 생성합니다.
    /// </summary>
    public void Initialize()
    {
        if (!ValidateReferences()) return;

        // 데이터 캐시 초기화 (CSV 파싱)
        if (!dataCache.IsInitialized)
        {
            dataCache.Initialize();
        }

        // 버튼 동적 생성
        CreateButtons();

        Debug.Log("[INFO] ExhibitManager: 초기화 완료.");
    }

    /// <summary>
    /// 캐싱된 전체 SectionData를 기반으로 버튼 프리팹을 동적 생성합니다.
    /// 각 버튼에 Id, Title, Name 텍스트를 표시하고 onClick 이벤트를 연결합니다.
    /// </summary>
    private void CreateButtons()
    {
        IReadOnlyDictionary<int, SectionData> allData = dataCache.GetAllData();

        if (allData.Count == 0)
        {
            Debug.LogWarning("[WARN] ExhibitManager: 생성할 버튼 데이터가 없습니다.");
            return;
        }

        // ID 오름차순으로 정렬하여 버튼 생성 순서 보장
        var sortedIds = new List<int>(allData.Keys);
        sortedIds.Sort();

        foreach (int id in sortedIds)
        {
            SectionData data = allData[id];
            CreateSingleButton(data);
        }

        Debug.Log($"[INFO] ExhibitManager: 버튼 {sortedIds.Count}개 동적 생성 완료.");
    }

    /// <summary>
    /// 단일 버튼을 프리팹에서 생성하고 텍스트와 이벤트를 설정합니다.
    /// </summary>
    private void CreateSingleButton(SectionData data)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        buttonObj.name = $"ExhibitBtn_{data.Id:D4}";
        createdButtons.Add(buttonObj);

        // 버튼 내부 TextMeshProUGUI 텍스트 바인딩
        BindButtonTexts(buttonObj, data);

        // onClick 이벤트 연결
        Button btn = buttonObj.GetComponentInChildren<Button>();
        if (btn != null)
        {
            SectionData capturedData = data;
            btn.onClick.AddListener(() => OnExhibitButtonClicked(capturedData));
        }
        else
        {
            Debug.LogError($"[ERROR] ExhibitManager: 버튼 프리팹에 Button 컴포넌트가 없습니다. ID={data.Id}");
        }
    }

    private void BindButtonTexts(GameObject buttonObj, SectionData data)
    {
        TextMeshProUGUI[] allTexts = buttonObj.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (var tmp in allTexts)
        {
            switch (tmp.gameObject.name)
            {
                case "TextId":
                    tmp.text = data.Id.ToString();
                    break;
                case "TextTitle":
                    tmp.text = data.Title ?? "";
                    break;
                case "TextName":
                    tmp.text = data.Name ?? "";
                    break;
            }
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출됩니다.
    /// 해당 ID의 이미지 경로를 스캔한 뒤 상세 페이지에 전달합니다.
    /// </summary>
    private async void OnExhibitButtonClicked(SectionData data)
    {
        // 광클 방지
        if (isLoading)
        {
            Debug.Log("[INFO] ExhibitManager: 처리 중입니다. 잠시 기다려 주세요.");
            return;
        }

        if (detailReceiver == null)
        {
            Debug.LogError("[ERROR] ExhibitManager: 상세 페이지 수신자(IExhibitDetailReceiver)가 연결되지 않았습니다.");
            return;
        }

        isLoading = true;

        try
        {
            Debug.Log($"[INFO] ExhibitManager: 버튼 클릭 — ID={data.Id}, 제목='{data.Title}'");

            // 이미지 경로 스캔
            List<string> paths = await ScanImagePathsAsync(data.Id);

            if (paths.Count == 0)
            {
                Debug.LogWarning($"[WARN] ExhibitManager: ID={data.Id}에 대한 이미지가 하나도 없습니다.");
            }

            // 상세 페이지에 데이터와 "파일 경로 리스트" 전달 (메모리 로딩은 상세 페이지의 Pool이 담당)
            detailReceiver.ShowExhibitDetail(data, paths);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ERROR] ExhibitManager: 이미지 경로 스캔 중 예외 발생 — {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// 특정 ID에 해당하는 모든 이미지의 경로를 순차 탐색하여 확인합니다.
    /// 파일명 규칙: {ID_4자리}_{인덱스}.jpg 또는 .png (인덱스는 1부터 시작)
    /// </summary>
    private async Task<List<string>> ScanImagePathsAsync(int id)
    {
        List<string> paths = new List<string>();
        string formattedId = id.ToString("D4"); 
        string basePath = GetImageBasePath();

        // I/O 순차 체크를 백그라운드 스레드에서 수행 (메인 스레드 멈춤 방지)
        await Task.Run(() =>
        {
            int imageIndex = 1;

            while (true)
            {
                string fileNameBase = $"{formattedId}_{imageIndex}";
                string jpgPath = Path.Combine(basePath, fileNameBase + ".jpg");
                string pngPath = Path.Combine(basePath, fileNameBase + ".png");

                string targetPath = null;

                if (File.Exists(jpgPath))
                {
                    targetPath = jpgPath;
                }
                else if (File.Exists(pngPath))
                {
                    targetPath = pngPath;
                }

                if (targetPath == null)
                {
                    break;
                }

                paths.Add(targetPath);
                imageIndex++;
            }
        });

        Debug.Log($"[INFO] ExhibitManager: ID={formattedId} 이미지 레퍼런스(경로) {paths.Count}개 발견 완료.");
        return paths;
    }

    /// <summary>
    /// 이미지 폴더의 절대 경로를 반환합니다.
    /// 에디터: Assets/StreamingAssets/Content_MediaTable/Images/
    /// 빌드: streamingAssetsPath/Content_MediaTable/Images/
    /// </summary>
    private string GetImageBasePath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "StreamingAssets", "Content_MediaTable");
#else
        return Path.Combine(Application.streamingAssetsPath, "Content_MediaTable");
#endif
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (dataCache == null)
        {
            Debug.LogError("[ERROR] ExhibitManager: ExhibitDataCache가 연결되지 않았습니다.");
            isValid = false;
        }

        if (buttonPrefab == null)
        {
            Debug.LogError("[ERROR] ExhibitManager: 버튼 프리팹이 연결되지 않았습니다.");
            isValid = false;
        }

        if (buttonContainer == null)
        {
            Debug.LogError("[ERROR] ExhibitManager: 버튼 컨테이너가 연결되지 않았습니다.");
            isValid = false;
        }

        if (detailReceiver == null)
        {
            Debug.LogWarning("[WARN] ExhibitManager: IExhibitDetailReceiver가 연결되지 않았습니다.");
        }

        return isValid;
    }

    private void CleanupButtons()
    {
        foreach (var btnObj in createdButtons)
        {
            if (btnObj != null) Object.Destroy(btnObj);
        }
        createdButtons.Clear();
    }

    private void OnDestroy()
    {
        CleanupButtons();
    }
}
