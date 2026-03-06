using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// ResourcePathCache에 캐싱된 파일들의 무결성을 검사합니다.
/// 파일 존재 여부, 파일 크기 0 여부 등을 확인하고 결과를 로깅합니다.
/// </summary>
public class IntegrityChecker : MonoBehaviour
{
    [Tooltip("ResourcePathCache 참조 (Inspector에서 연결)")]
    [SerializeField] private ResourcePathCache resourcePathCache;


    [Tooltip("에러 팝업 UI (Inspector에서 연결)")]
    [SerializeField] private ErrorPopup errorPopup;

    // ── 검사 결과 ──
    private int totalChecked;
    private int errorCount;

    /// <summary>
    /// 캐싱된 모든 리소스의 무결성을 검사합니다.
    /// AppBootstrapper에서 ResourcePathCache.Initialize() 이후 호출됩니다.
    /// </summary>
    /// <returns>에러가 없으면 true, 있으면 false</returns>
    public bool Validate()
    {
        totalChecked = 0;
        errorCount = 0;

        // 에러 팝업 초기화
        if (errorPopup != null) errorPopup.ClearErrors();

        if (resourcePathCache == null)
        {
            string msg = "[ERROR] 무결성 검사 시스템 오류: 경로 캐시 시스템(ResourcePathCache) 연결이 누락되었습니다.";
            Debug.LogError(msg);

            return false;
        }

        //  1. 사용자 필수 버튼 명단 가져와서 최우선으로 검사
        string requiredRaw = AppConfig.RequiredButtons;
        if (!string.IsNullOrWhiteSpace(requiredRaw))
        {
            var requiredKeys = requiredRaw.Split(';')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k));

            foreach (string reqKey in requiredKeys)
            {
                totalChecked++;
                if (!resourcePathCache.TryGetPath(reqKey, out _))
                {
                    errorCount++;
                    string warnMsg = $"필수 지정된 '{reqKey}' 이미지가 시스템(폴더)에 누락되어 있습니다! 파일을 확인해주세요.";
                    Debug.LogWarning($"[WARN] {warnMsg}");
                    if (errorPopup != null) errorPopup.AddError(warnMsg);
                }
            }
        }

        //  2. 존재하는 모든 버튼에 대한 page 짝꿍 검사
        var buttonKeys = resourcePathCache.GetButtonKeys();

        foreach (string btnKey in buttonKeys)
        {
            // 버튼 이미지 검사
            if (resourcePathCache.TryGetPath(btnKey, out string btnPath))
            {
                CheckFile(btnKey, btnPath);
            }

            // 대응하는 페이지 이미지 검사
            string pageKey = resourcePathCache.GetPageKey(btnKey);
            if (resourcePathCache.TryGetPath(pageKey, out string pagePath))
            {
                CheckFile(pageKey, pagePath);
            }
            else
            {
                string warnMsg = $"'{btnKey}' 버튼 이미지는 존재하는데, 연결될 '{pageKey}' 페이지 파일은 누락되었습니다. 파일을 채워주세요.";
                Debug.LogWarning($"[WARN] {warnMsg}");

                if (errorPopup != null) errorPopup.AddError(warnMsg);
            }
        }

        // 검사 결과 요약 로그
        string resultMsg = $"[INFO] 무결성 검사 완료 — 검사: {totalChecked}개, 에러: {errorCount}개";
        if (errorCount > 0)
        {
            Debug.LogWarning(resultMsg);
        }
        else
        {
            Debug.Log(resultMsg);
        }


        // 에러 팝업 표시 (ErrorPopup 내부에서 에러 0개면 자동으로 표시하지 않음)
        if (errorPopup != null)
        {
            errorPopup.ShowIfNeeded();
        }

        return errorCount == 0;
    }

    /// <summary>
    /// 개별 파일의 존재 여부 및 크기를 확인합니다.
    /// </summary>
    private void CheckFile(string key, string filePath)
    {
        totalChecked++;

        // 파일 존재 확인
        if (!File.Exists(filePath))
        {
            string errDetail = $"필수 이미지가 윈도우 폴더에 없습니다: {key}";
            string msg = $"[ERROR] 파일을 찾을 수 없습니다: {key} (확인 경로: {filePath})";
            Debug.LogError(msg);

            if (errorPopup != null) errorPopup.AddError(errDetail);
            errorCount++;
            return;
        }

        // 파일 크기 확인 (0바이트 = 빈 파일)
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                string errDetail = $"파일 내용이 텅 비어있습니다(손상의심, 0바이트): {key}";
                string msg = $"[ERROR] 파일이 손상되었거나 내용이 없습니다: {key} (확인 경로: {filePath})";
                Debug.LogError(msg);

                if (errorPopup != null) errorPopup.AddError(errDetail);
                errorCount++;
            }
        }
        catch (System.Exception ex)
        {
            string errDetail = $"파일을 검사하는 중 알 수 없는 윈도우 에러가 발생했습니다: {key} (사유: {ex.Message})";
            string msg = $"[ERROR] {errDetail}";
            Debug.LogError(msg);

            if (errorPopup != null) errorPopup.AddError(errDetail);
            errorCount++;
        }
    }
}
