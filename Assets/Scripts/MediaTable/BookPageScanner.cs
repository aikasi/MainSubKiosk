using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

#if MEDIA_TABLE
namespace MediaTable
{
    /// <summary>
    /// 미디어 테이블 전용 다중 페이지 스캐너.
    /// 디스플레이(좌/우)와 무관하게 모든 폴더를 한번만 스캔하여 캐시처럼 작동합니다.
    /// <para>하나의 스캐너를 씬에 두고 양쪽 캔버스가 전부 이 스크립트를 참조해 경로 데이터를 가져옵니다.</para>
    /// </summary>
    public class BookPageScanner : MonoBehaviour
    {
        // ── 캐시 저장소 (Key: 아이템 ID "0001", Value: 오름차순 정렬된 이미지 경로 리스트) ──
        private readonly Dictionary<string, List<string>> pageGroups = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

        // ── 썸네일 캐시 저장소 (오름차순 정렬된 -btn 이미지 경로 리스트) ──
        private readonly List<string> thumbnailPaths = new List<string>();

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (IsInitialized) return;
            pageGroups.Clear();
            thumbnailPaths.Clear();

            string pageBasePath = AppConfig.MediaTablePageBasePath;

            if (!Directory.Exists(pageBasePath))
            {
                Debug.LogError($"[ERROR] MediaTable 책 속지 이미지를 찾을 수 없습니다. 경로: {pageBasePath}");
                return;
            }

            string extensionsRaw = AppConfig.ImageExtensions;
            HashSet<string> allowedExtensions = new HashSet<string>(
                extensionsRaw.Split(';')
                    .Select(ext => ext.Trim().ToLowerInvariant())
                    .Where(ext => !string.IsNullOrEmpty(ext))
            );

            string[] files;
            try
            {
                files = Directory.GetFiles(pageBasePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ERROR] 속지 폴더 스캔 중 에러 발생: {ex.Message}");
                return;
            }

            foreach (string filePath in files)
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension)) continue;

                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string lowerFileName = fileName.ToLowerInvariant();
                
                // 버튼용 이미지(-btn)인 경우 썸네일 리스트에 따로 담습니다.
                if (lowerFileName.EndsWith("-btn"))
                {
                    thumbnailPaths.Add(filePath);
                    continue;
                }
                
                int pageIdx = lowerFileName.IndexOf("-page_");
                
                if (pageIdx != -1)
                {
                    string itemId = fileName.Substring(0, pageIdx);
                    
                    if (!pageGroups.ContainsKey(itemId))
                        pageGroups[itemId] = new List<string>();
                    
                    pageGroups[itemId].Add(filePath);
                }
                else
                {
                    // 예외: 딱 1페이지만 있는 파일("-page")일 경우
                    if (lowerFileName.EndsWith("-page"))
                    {
                        // "0000-page" -> length 9, "-page" length 5. -> "0000" 추출
                        int suffixIdx = lowerFileName.LastIndexOf("-page");
                        if (suffixIdx > 0)
                        {
                            string itemId = fileName.Substring(0, suffixIdx);
                            if (!pageGroups.ContainsKey(itemId))
                                pageGroups[itemId] = new List<string>();
                            
                            pageGroups[itemId].Add(filePath);
                        }
                    }
                }
            }

            // 모든 그룹의 경로를 윈도우 정렬순(자연 정렬, Natural Sort)으로 묶기
            // 예: page_1 -> page_2 -> page_10 순서가 올바르게 보장됨.
            foreach (var key in pageGroups.Keys.ToList())
            {
                pageGroups[key].Sort((a, b) => NaturalCompare(a, b));
                
                // 정렬 후, 누락된 페이지가 있는지 검증 시작
                CheckMissingPages(key, pageGroups[key]);
            }

            // 썸네일 버튼 경로 자연 정렬
            thumbnailPaths.Sort((a, b) => NaturalCompare(a, b));

            Debug.Log($"[INFO] BookPageScanner 초기화 완료. 총 {pageGroups.Count}개의 책 문서 그룹과 {thumbnailPaths.Count}개의 썸네일 스캔 완료.");
            IsInitialized = true;
        }

        /// <summary>
        /// 특정 아이템이 가진 모든 페이지의 파일 경로 목록을 반환합니다.
        /// (스캔되지 않은 아이템 ID를 넣으면 빈 리스트가 리턴됩니다)
        /// </summary>
        public List<string> GetPagePaths(string itemId)
        {
            if (!IsInitialized) Initialize();

            if (pageGroups.TryGetValue(itemId, out var paths))
            {
                return paths;
            }
            return new List<string>();
        }

        /// <summary>
        /// 캐싱된 모든 썸네일(-btn) 파일 경로 목록을 반환합니다.
        /// </summary>
        public List<string> GetThumbnailPaths()
        {
            if (!IsInitialized) Initialize();
            return new List<string>(thumbnailPaths);
        }


        /// <summary>
        /// 문자열 내부의 숫자를 실제 숫자 크기로 인식하여 정렬하는 함수 (Natural Sort)
        /// </summary>
        private int NaturalCompare(string x, string y)
        {
            if (x == null || y == null) return 0;
            
            // 문자열을 숫자와 문자로 분리
            string[] xParts = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
            string[] yParts = Regex.Split(y.Replace(" ", ""), "([0-9]+)");

            for (int i = 0; i < Mathf.Min(xParts.Length, yParts.Length); i++)
            {
                if (xParts[i] != yParts[i])
                {
                    int xNum, yNum;
                    bool xIsNum = int.TryParse(xParts[i], out xNum);
                    bool yIsNum = int.TryParse(yParts[i], out yNum);

                    if (xIsNum && yIsNum)
                        return xNum.CompareTo(yNum); // 숫자 대 숫자 비교
                    else
                        return string.Compare(xParts[i], yParts[i], System.StringComparison.OrdinalIgnoreCase); // 문자 비교
                }
            }
            return x.Length.CompareTo(y.Length);
        }

        /// <summary>
        /// 페이지 번호가 연속되는지 검사하고 누락이 있으면 리포트합니다.
        /// </summary>
        private void CheckMissingPages(string itemId, List<string> paths)
        {
            if (paths.Count == 0) return;

            List<int> pageNumbers = new List<int>();

            // 정렬된 리스트에서 페이지 번호만 추출
            foreach (string path in paths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                int idx = fileName.IndexOf("-page_");
                
                if (idx != -1)
                {
                    string numStr = fileName.Substring(idx + 6); // "-page_" 길이 6
                    if (int.TryParse(numStr, out int num))
                    {
                        pageNumbers.Add(num);
                    }
                }
                else if (fileName.EndsWith("-page"))
                {
                    pageNumbers.Add(1);
                }
            }

            if (pageNumbers.Count == 0) return;

            // 중복 제거 및 누락 검사
            pageNumbers = pageNumbers.Distinct().ToList();
            
            int firstPage = pageNumbers[0];
            int lastPage = pageNumbers[pageNumbers.Count - 1];
            List<int> missingNums = new List<int>();

            for (int i = firstPage; i <= lastPage; i++)
            {
                if (!pageNumbers.Contains(i))
                {
                    missingNums.Add(i);
                }
            }

            // 누락된 페이지가 존재할 경우
            if (missingNums.Count > 0)
            {
                string missingStr = string.Join(", ", missingNums);
                string errorMsg = $"[경고] '{itemId}' 책의 속지 번호가 연속되지 않습니다.\n누락된 페이지 번호: {missingStr}";
                
                Debug.LogWarning(errorMsg);

                if (AppConfig.ShowMissingPageError && ErrorPopup.Instance != null)
                {
                    ErrorPopup.Instance.AddAndShow(errorMsg);
                }
            }
        }
    }
}
#endif
