using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// StreamingAssets/Content_[AppID]/ 폴더를 스캔하여
/// 파일명(확장자 제외)을 Key, 절대 경로를 Value로 캐싱합니다.
/// Key 예시: "0001_btn_off", "0001_page", "0001_page_on"
/// </summary>
public class ResourcePathCache : MonoBehaviour
{
    // ── 캐시 저장소 ──
    private readonly Dictionary<string, string> pathCache = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

    // ── 슬라이드 이미지 캐시 (Key: 아이템 번호 "1","2"..., Value: 정렬된 경로 리스트) ──
    private readonly Dictionary<string, List<string>> slideCache = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>캐싱된 항목 수를 반환합니다.</summary>
    public int CachedCount => pathCache.Count;

    /// <summary>경로 캐싱(스캔) 작업이 모두 완료되었는지 여부를 확인합니다.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 콘텐츠 폴더 및 Image 폴더를 스캔하여 허용 확장자에 맞는 파일을 캐싱합니다.
    /// AppBootstrapper에서 호출됩니다.
    /// </summary>
    public void Initialize()
    {
        IsInitialized = false;
        pathCache.Clear();
        slideCache.Clear();

        string basePath = AppConfig.ContentBasePath;

        // 콘텐츠 폴더 존재 여부 확인
        if (!Directory.Exists(basePath))
        {
            string msg = "[ERROR] 시스템 오류: 버튼 이미지들을 보관하는 'StreamingAssets/Btn' 폴더를 찾을 수 없습니다. 폴더가 삭제되었는지 확인해주세요.";
            Debug.LogError(msg);
            return;
        }

        // Settings.txt에서 로드할 확장자 목록 가져오기
        string extensionsRaw = AppConfig.ImageExtensions;
        HashSet<string> allowedExtensions = new HashSet<string>(
            extensionsRaw.Split(';')
                .Select(ext => ext.Trim().ToLowerInvariant())
                .Where(ext => !string.IsNullOrEmpty(ext))
        );

        // 1. 메인 콘텐츠 폴더 스캔 (btn, page 등)
        ScanFolder(basePath, allowedExtensions);

        // 2. Image 하위 폴더 스캔 (슬라이드쇼 사진)
        string imagePath = Path.Combine(basePath, "Image");
        if (Directory.Exists(imagePath))
        {
            ScanSlideFolder(imagePath, allowedExtensions);
        }
        else
        {
            Debug.LogWarning($"[WARN] 슬라이드쇼 이미지 폴더가 존재하지 않습니다: {imagePath}");
        }

        // 캐싱 결과 로그
        string resultMsg = $"[INFO] 리소스 경로 캐싱 완료 — " +
                           $"메인 캐시: {pathCache.Count}개, " +
                           $"슬라이드 그룹: {slideCache.Count}개";
        Debug.Log(resultMsg);

        IsInitialized = true;
    }

    /// <summary>
    /// 지정된 폴더의 이미지 파일을 스캔하여 pathCache에 등록합니다.
    /// </summary>
    private void ScanFolder(string folderPath, HashSet<string> allowedExtensions)
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(folderPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ERROR] 폴더 스캔 중 에러 발생: {ex.Message}");
            return;
        }

        foreach (string filePath in files)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension)) continue;

            // Key: 확장자 제외 파일명 (예: "0001_btn_off")
            string key = Path.GetFileNameWithoutExtension(filePath);

            if (pathCache.ContainsKey(key))
            {
                Debug.LogWarning($"[WARN] 화면 표시 오류: '{Path.GetFileName(filePath)}' 파일과 똑같은 이름의 파일이 이미 존재합니다. 나중에 발견된 파일로 덮어씁니다.");
                pathCache[key] = filePath;
            }
            else
            {
                pathCache.Add(key, filePath);
            }
        }
    }

    /// <summary>
    /// Image 하위 폴더를 스캔하여 slideCache에 그룹별로 등록합니다.
    /// 파일명 규칙: "1_1.png", "1_2.png" → 그룹 키 "1"
    /// </summary>
    private void ScanSlideFolder(string folderPath, HashSet<string> allowedExtensions)
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(folderPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ERROR] Image 폴더 스캔 중 에러 발생: {ex.Message}");
            return;
        }

        foreach (string filePath in files)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension)) continue;

            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // "1_2" 형식에서 언더스코어 앞의 그룹 번호 추출
            int underscoreIdx = fileName.IndexOf('_');
            if (underscoreIdx <= 0) continue;

            string groupKey = fileName.Substring(0, underscoreIdx);

            if (!slideCache.ContainsKey(groupKey))
                slideCache[groupKey] = new List<string>();

            slideCache[groupKey].Add(filePath);
        }

        // 각 그룹 내 자연 정렬 (1_1, 1_2, 1_10 순서 보장)
        foreach (var key in slideCache.Keys.ToList())
        {
            slideCache[key].Sort((a, b) => NaturalCompare(a, b));
        }
    }

    // ── 공개 API ──

    /// <summary>캐시에서 Key에 해당하는 절대 경로를 조회합니다.</summary>
    public bool TryGetPath(string key, out string path)
    {
        return pathCache.TryGetValue(key, out path);
    }

    /// <summary>
    /// "_btn_off" 접미사를 가진 모든 Key를 정렬된 리스트로 반환합니다.
    /// </summary>
    public List<string> GetButtonKeys()
    {
        return pathCache.Keys
            .Where(k => k.EndsWith("_btn_off", System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k)
            .ToList();
    }

    /// <summary>
    /// "_btn" 접미사만 가진 단독 버튼 Key를 반환합니다 (0000_btn 등 on/off 없는 버튼용).
    /// </summary>
    public List<string> GetSingleButtonKeys()
    {
        return pathCache.Keys
            .Where(k => k.EndsWith("_btn", System.StringComparison.OrdinalIgnoreCase)
                        && !k.EndsWith("_btn_on", System.StringComparison.OrdinalIgnoreCase)
                        && !k.EndsWith("_btn_off", System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k)
            .ToList();
    }

    /// <summary>
    /// 버튼 Key에서 아이템 ID를 추출합니다.
    /// 예: "0001_btn_off" → "0001", "0000_btn" → "0000"
    /// </summary>
    public string ExtractItemId(string btnKey)
    {
        if (string.IsNullOrEmpty(btnKey)) return string.Empty;

        // _btn_off, _btn_on, _btn 순서로 제거 시도
        string lower = btnKey.ToLowerInvariant();
        if (lower.EndsWith("_btn_off")) return btnKey.Substring(0, btnKey.Length - 8);
        if (lower.EndsWith("_btn_on")) return btnKey.Substring(0, btnKey.Length - 7);
        if (lower.EndsWith("_btn")) return btnKey.Substring(0, btnKey.Length - 4);
        return btnKey;
    }

    /// <summary>
    /// 특정 아이템의 슬라이드쇼 이미지 경로 목록을 반환합니다.
    /// itemId "0001" → 그룹 키 "1" 로 변환하여 조회합니다.
    /// </summary>
    public List<string> GetSlideImagePaths(string itemId)
    {
        // "0001" → 앞의 0을 제거하여 "1"로 변환
        string groupKey = itemId.TrimStart('0');
        if (string.IsNullOrEmpty(groupKey)) groupKey = "0"; // "0000"의 경우

        if (slideCache.TryGetValue(groupKey, out var paths))
        {
            return new List<string>(paths);
        }
        return new List<string>();
    }

    // ── 내부 유틸리티 ──

    /// <summary>
    /// 문자열 내부의 숫자를 실제 숫자 크기로 인식하여 정렬하는 함수 (Natural Sort)
    /// </summary>
    private int NaturalCompare(string x, string y)
    {
        if (x == null || y == null) return 0;

        string[] xParts = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
        string[] yParts = Regex.Split(y.Replace(" ", ""), "([0-9]+)");

        for (int i = 0; i < Mathf.Min(xParts.Length, yParts.Length); i++)
        {
            if (xParts[i] != yParts[i])
            {
                bool xIsNum = int.TryParse(xParts[i], out int xNum);
                bool yIsNum = int.TryParse(yParts[i], out int yNum);

                if (xIsNum && yIsNum)
                    return xNum.CompareTo(yNum);
                else
                    return string.Compare(xParts[i], yParts[i], System.StringComparison.OrdinalIgnoreCase);
            }
        }
        return x.Length.CompareTo(y.Length);
    }
}
