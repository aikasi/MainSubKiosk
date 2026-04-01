
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 미디어 테이블의 메인 화면 썸네일 버튼(-btn)들을 스캔하고, 화면에 생성하며,
    /// 터치 시 MediaBookManager를 호출하여 상세 책을 팝업 시키는 클래스입니다.
    /// <para>좌/우 디스플레이마다 캔버스에 하나씩 부착되어 독립적으로 버튼을 그립니다.</para>
    /// </summary>
    public class MediaMainScreenManager : MonoBehaviour
    {
        [Header("연결 시스템")]
        [Tooltip("전역 스캐너 캐시 저장소 ")]
        [SerializeField] private BookPageScanner scanner;
        [Tooltip("이미지를 비동기로 불러오는 공통 로더 ")]
        [SerializeField] private ImageLoader imageLoader;
        [Tooltip("책을 열어줄 상세 페이지 매니저 (현재 캔버스의 MediaBookManager 연결)")]
        [SerializeField] private MediaBookManager bookManager;
        
        [Header("UI 생성 셋팅")]
        [Tooltip("버튼들이 생성되어 들어갈 부모 객체 ")]
        [SerializeField] private Transform buttonContainer;
        [Tooltip("복제해서 사용할 버튼 프리팹 ")]
        [SerializeField] private Button buttonPrefab;
        
        private async void Start()
        {
            if (buttonPrefab == null || buttonContainer == null || imageLoader == null || bookManager == null || scanner == null)
            {
                Debug.LogError("[ERROR] MediaMainScreenManager: 필수 컴포넌트가 모두 연결되지 않았습니다.");
                return;
            }

            await LoadAndCreateButtonsAsync();
        }

        /// <summary>
        /// BookPageScanner에서 썸네일(-btn) 경로 목록을 받아와 화면에 버튼들을 비동기 생성합니다.
        /// </summary>
        private async Task LoadAndCreateButtonsAsync()
        {
            // 1. 스캐너에서 미리 읽어둔 경로 리스트 획득 (디스크 I/O 최적화)
            List<string> btnFilePaths = scanner.GetThumbnailPaths();

            if (btnFilePaths.Count == 0)
            {
                Debug.LogWarning("[WARN] 메인 화면에 띄울 버튼 이미지(-btn)가 폴더에 없거나 스캔되지 않았습니다.");
                return;
            }

            // 2. 비동기 타스크 명단 준비
            var loadTasks = new List<Task>();

            // 3. 프리팹 복제 및 썸네일 동시 다발적(Parallel) 로딩
            for (int i = 0; i < btnFilePaths.Count; i++)
            {
                string filePath = btnFilePaths[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string itemId = fileName.Substring(0, fileName.Length - 4); // "0001-btn" 에서 "0001" 추출

                // UI 생성 전 페이지 유효성 검사
                List<string> pagePaths = scanner.GetPagePaths(itemId);
                if (pagePaths.Count == 0)
                {
                    string errorMsg = $"[경고] '{itemId}' 썸네일(-btn)은 존재하지만 속지(-page_x) 파일이 한 장도 없습니다.";
                    Debug.LogWarning(errorMsg);
                    if (ErrorPopup.Instance != null)
                    {
                        ErrorPopup.Instance.AddAndShow(errorMsg);
                    }
                    continue;
                }

                // 껍데기 UI 즉시 생성
                Button newBtn = Instantiate(buttonPrefab, buttonContainer);
                newBtn.gameObject.name = $"Button_{itemId}";
                
                string capturedItemId = itemId; // 클로저 이슈 방지를 위한 지역 변수 복사
                newBtn.onClick.AddListener(() => OnButtonClicked(capturedItemId));

                // 이미지 로딩 임무를 Task 리스트에 추가 (이 시점에서는 await 하지 않음)
                loadTasks.Add(LoadAndApplyGraphicAsync(newBtn, filePath));
            }

            // 4. 모인 모든 Task들이 병렬로 끝날 때까지 한 번에 기다림.
            if (loadTasks.Count > 0)
            {
                await Task.WhenAll(loadTasks);
            }

            Debug.Log($"[INFO] MediaTable: 총 {btnFilePaths.Count}개의 메인화면 버튼 생성 및 병렬 로드 완료.");
        }

        /// <summary>
        /// 단일 버튼의 그래픽(이미지)을 비동기로 로드하고 화면에 씌워주는 개별 Task 함수입니다.
        /// </summary>
        private async Task LoadAndApplyGraphicAsync(Button targetButton, string path)
        {
            Texture2D tex = await imageLoader.LoadTextureAsync(path);
            if (tex != null && targetButton != null)
            {
                Sprite sprite = imageLoader.CreateSprite(tex);
                if (targetButton.image != null)
                {
                    targetButton.image.sprite = sprite;
                }
            }
        }

        /// <summary>
        /// 동적으로 생성된 썸네일 버튼이 클릭되었을 때 호출됩니다.
        /// 동일 캔버스에 달린 MediaBookManager에게 해당 ID의 책을 띄우라고 명령합니다.
        /// </summary>
        private void OnButtonClicked(string itemId)
        {
            Debug.Log($"[INFO] 메인 화면 썸네일 클릭: {itemId}");
            bookManager.OpenBook(itemId);
        }
    }

