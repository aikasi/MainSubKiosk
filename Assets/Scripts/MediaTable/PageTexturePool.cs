using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if MEDIA_TABLE
namespace MediaTable
{
    /// <summary>
    /// 실제 'Book - Page Curl' 에셋의 구조(Sprite[] bookPages)에 완벽히 호환되도록 수정한 메모리 관리자.
    /// 현재 페이지를 기준으로 전/후 스프레드(총 5~6장)의 Sprite만 생성하여 Book.bookPages 배열에 꽂아주고,
    /// 멀어진 페이지의 Sprite는 파괴하여 RAM을 방어합니다.
    /// </summary>
    public class PageTexturePool : MonoBehaviour
    {
        [Tooltip("비동기 이미지 로딩을 수행할 로더 시스템 연결")]
        [SerializeField] private ImageLoader imageLoader;

        // Key: 페이지 인덱스(0부터 시작), Value: 로드된 Sprite
        private readonly Dictionary<int, Sprite> activeSprites = new Dictionary<int, Sprite>();
        private readonly HashSet<int> loadingIndices = new HashSet<int>();
        private List<string> currentPagePaths = new List<string>();

        public int TotalPages => currentPagePaths.Count;

        public void InitializeBook(List<string> pagePaths, Book targetBook)
        {
            ClearAllTextures(targetBook);
            // 주의: 복사본을 만들어 원본 스캐너 캐시가 날아가지 않도록 보호합니다.
            currentPagePaths = pagePaths != null ? new List<string>(pagePaths) : new List<string>();
            
            // 핵심: Book 에셋은 bookPages 배열의 길이를 기준으로 TotalPageCount를 인식하므로,
            // 새 책을 열 때 전체 길이만큼 빈 배열을 생성해 주입합니다.
            if (targetBook != null)
            {
                targetBook.bookPages = new Sprite[currentPagePaths.Count];
            }
        }

        /// <summary>
        /// 주어진 현재 페이지를 기준으로 과거(Settings 값)와 미래(+3장)의 Sprite를 준비하고, 나머지는 파괴합니다.
        /// (2페이지씩 넘어가므로 현재페이지 기준으로 양옆 스프레드 커버)
        /// </summary>
        public async Task UpdatePoolAsync(Book targetBook, int currentPageIndex)
        {
            if (currentPagePaths.Count == 0 || targetBook == null) return;

            // 로드 상태를 유지할 과거 인덱스 계산 (Settings.txt 설정값 반영)
            int keepPast = AppConfig.KeepPreviousPages;
            int startIdx = Mathf.Max(0, currentPageIndex - keepPast);
            int endIdx = Mathf.Min(currentPagePaths.Count - 1, currentPageIndex + 3);

            var targetIndices = new HashSet<int>();
            for (int i = startIdx; i <= endIdx; i++)
            {
                targetIndices.Add(i);
            }

            // Book.cs 구조상 LeftNext는 currentPage-1, RightNext는 currentPage에 바인딩됩니다.
            // 애니메이션을 위해 뒷면인 currentPage+1, 다음 페이지인 currentPage+2 까지 모두 살려야 합니다.
            targetIndices.Add(targetBook.currentPage - 1);
            targetIndices.Add(targetBook.currentPage);
            targetIndices.Add(targetBook.currentPage + 1);
            targetIndices.Add(targetBook.currentPage + 2);

            // 1. 타겟에 속하지 않는 과도한 과거/미래의 Sprite는 메모리 완전 파괴
            var indicesToRemove = new List<int>();
            foreach (var kvp in activeSprites)
            {
                if (!targetIndices.Contains(kvp.Key))
                {
                    indicesToRemove.Add(kvp.Key);
                }
            }

            foreach (int idx in indicesToRemove)
            {
                Sprite s = activeSprites[idx];
                targetBook.bookPages[idx] = null; // 에셋 연결 해제
                ResourceCleaner.DestroySprite(ref s); // 메모리 해제
                activeSprites.Remove(idx);
            }

            // 2. 타겟 범위 내에 배정되지 않은 빈 공간을 비동기 스레드로 로드 (Eager Load)
            var loadTasks = new List<Task>();
            foreach (int targetIdx in targetIndices)
            {
                if (!activeSprites.ContainsKey(targetIdx) && !loadingIndices.Contains(targetIdx))
                {
                    loadTasks.Add(LoadPageAsync(targetBook, targetIdx));
                }
            }

            if (loadTasks.Count > 0)
            {
                await Task.WhenAll(loadTasks);
            }
        }

        private async Task LoadPageAsync(Book targetBook, int index)
        {
            if (index < 0 || index >= currentPagePaths.Count) return;

            loadingIndices.Add(index);
            string path = currentPagePaths[index];
            
            Texture2D texture = await imageLoader.LoadTextureAsync(path);
            
            if (texture != null)
            {
                // 로드 완료된 시점에 이 책이 여전히 열려있는지(경로 비교) 방어
                if (currentPagePaths.Count > index && currentPagePaths[index] == path)
                {
                    // Texture를 UI용 Sprite로 즉시 변환 (메모리를 위해 임시 texture는 내부에서 관리되게끔 Sprite.Create 필요)
                    Sprite newSprite = imageLoader.CreateSprite(texture);
                    
                    activeSprites[index] = newSprite;
                    
                    // Book 에셋의 전역 배열에 우선 주입
                    if (targetBook != null && targetBook.bookPages != null && index < targetBook.bookPages.Length)
                    {
                        targetBook.bookPages[index] = newSprite;
                        
                        // [중요] 비동기 로드가 책 넘김/대기 중에 끝났을 경우, 사용자가 지금 보고 있는 페이지라면 '표면'에 즉시 렌더링 갱신
                        int currentViewPage = targetBook.currentPage;
                        
                        // 현재 보고 있는 '왼쪽' 페이지가 방금 로드된 거라면 덧씌움
                        if (currentViewPage > 0 && index == currentViewPage - 1)
                        {
                            targetBook.LeftNext.sprite = newSprite;
                        }
                        // 현재 보고 있는 '오른쪽' 페이지가 방금 로드된 거라면 덧씌움
                        else if (index == currentViewPage)
                        {
                            targetBook.RightNext.sprite = newSprite;
                        }
                    }
                }
                else
                {
                    ResourceCleaner.DestroyTexture(ref texture);
                }
            }
            else
            {
                Debug.LogError($"[ERROR] MediaTable 페이지 이미지 로드 실패: {path}");
            }
            
            loadingIndices.Remove(index);
        }

        public void ClearAllTextures(Book targetBook)
        {
            if (targetBook != null && targetBook.bookPages != null)
            {
                for (int i = 0; i < targetBook.bookPages.Length; i++)
                {
                    targetBook.bookPages[i] = null;
                }
            }

            foreach (var kvp in activeSprites)
            {
                Sprite s = kvp.Value;
                ResourceCleaner.DestroySprite(ref s);
            }
            activeSprites.Clear();
            loadingIndices.Clear();
            // 주의: .Clear()를 부르면 원본 스캐너의 캐시 리스트까지 파괴되므로, 무조건 새 인스턴스로 비웁니다.
            currentPagePaths = new List<string>();
        }

        private void OnDestroy()
        {
            ClearAllTextures(null);
        }
    }
}
#endif
