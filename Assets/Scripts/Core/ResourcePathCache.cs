using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// StreamingAssets/Content_[AppID]/ 폴더를 스캔하여
/// 파일명(확장자 제외)을 Key, 절대 경로를 Value로 캐싱합니다.
/// Key 예시: "0001-btn", "0001-page"
/// </summary>
public class ResourcePathCache : MonoBehaviour
{

    // ── 캐시 저장소 ──
    private readonly Dictionary<string, string> pathCache = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 캐싱된 항목 수를 반환합니다.
    /// </summary>
    public int CachedCount => pathCache.Count;

    /// <summary>
    /// 경로 캐싱(스캔) 작업이 모두 완료되었는지 여부를 확인합니다.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 콘텐츠 폴더를 스캔하여 허용 확장자에 맞는 파일을 캐싱합니다.
    /// AppBootstrapper에서 호출됩니다.
    /// </summary>
    public void Initialize()
    {
        IsInitialized = false;
        pathCache.Clear();

        string basePath = AppConfig.ContentBasePath;

        // 콘텐츠 폴더 존재 여부 확인
        if (!Directory.Exists(basePath))
        {
            string msg = "[ERROR] 시스템 오류: 버튼 이미지들을 보관하는 'StreamingAssets/Btn' 폴더를 찾을 수 없습니다. 폴더가 삭제되었는지 확인해주세요.";
            Debug.LogError(msg);

            return;
        }

        // 💡 Settings.txt에서 로드할 확장자 목록 가져오기
        string extensionsRaw = AppConfig.ImageExtensions;
        HashSet<string> allowedExtensions = new HashSet<string>(
            extensionsRaw.Split(';')
                .Select(ext => ext.Trim().ToLowerInvariant())
                .Where(ext => !string.IsNullOrEmpty(ext))
        );

        // 폴더 스캔
        string[] files;
        try
        {
            files = Directory.GetFiles(basePath);
        }
        catch (System.Exception)
        {
            string msg = "[ERROR] 시스템 오류: 상세 페이지 이미지들을 보관하는 'StreamingAssets/Page' 폴더를 찾을 수 없습니다. 폴더가 삭제되었는지 확인해주세요.";
            Debug.LogError(msg);

            return;
        }

        int skippedCount = 0;

        foreach (string filePath in files)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            // 허용 확장자 필터링
            if (!allowedExtensions.Contains(extension))
            {
                skippedCount++;
                continue;
            }

            // Key: 확장자 제외 파일명 (예: "0001-btn")
            string key = Path.GetFileNameWithoutExtension(filePath);

            if (pathCache.ContainsKey(key))
            {
                string warnMsg = $"[WARN] 화면 표시 오류: '{Path.GetFileName(filePath)}' 파일과 똑같은 이름의 파일이 이미 존재합니다. 나중에 발견된 파일로 덮어씁니다.";
                Debug.LogWarning(warnMsg);

                pathCache[key] = filePath;
            }
            else
            {
                pathCache.Add(key, filePath);
            }
        }

        // 캐싱 결과 로그
        string resultMsg = $"[INFO] 리소스 경로 캐싱 완료 — " +
                           $"폴더: {basePath}, " +
                           $"캐싱: {pathCache.Count}개, " +
                           $"건너뜀: {skippedCount}개 (확장자 미허용)";
        Debug.Log(resultMsg);

        IsInitialized = true;
    }

    /// <summary>
    /// 캐시에서 Key에 해당하는 절대 경로를 조회합니다.
    /// </summary>
    public bool TryGetPath(string key, out string path)
    {
        return pathCache.TryGetValue(key, out path);
    }

    /// <summary>
    /// "-btn" / "-BTN" 접미사를 가진 모든 Key를 정렬된 리스트로 반환합니다.
    /// 대소문자를 구분하지 않습니다.
    public List<string> GetButtonKeys()
    {
        return pathCache.Keys
            .Where(k => k.EndsWith("-btn", System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k)
            .ToList();
    }

    /// <summary>
    /// btn Key로부터 대응하는 page Key를 생성합니다.
    /// 예: "0001-btn" → "0001-page"
    /// </summary>
    public string GetPageKey(string btnKey)
    {
        if (string.IsNullOrEmpty(btnKey)) return string.Empty;

        // "-btn" / "-BTN" 접미사를 "-PAGE"로 교체 (대소문자 무관)
        if (btnKey.EndsWith("-btn", System.StringComparison.OrdinalIgnoreCase))
        {
            return btnKey.Substring(0, btnKey.Length - 4) + "-PAGE";
        }

        return btnKey + "-PAGE";
    }
}
