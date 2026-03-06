#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 빌드 시 현재 앱의 Content 폴더만 StreamingAssets에 포함되도록 제어합니다.
/// 다른 앱의 Content_* 폴더를 임시로 이동시키고, 빌드 완료 후 복원합니다.
/// </summary>
public class BuildContentFilter : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    // 임시 폴더 경로 (프로젝트 루트에 생성)
    private static readonly string TempFolderName = "_TempHidden_Content";

    /// <summary>
    /// 실행 순서 (낮을수록 먼저 실행)
    /// </summary>
    public int callbackOrder => 0;

    /// <summary>
    /// 빌드 직전: 다른 앱의 Content 폴더를 임시 이동합니다.
    /// </summary>
    public void OnPreprocessBuild(BuildReport report)
    {
        string streamingAssetsPath = Application.dataPath + "/StreamingAssets";
        string tempBasePath = Path.Combine(Application.dataPath, "..", TempFolderName);
        string currentContentFolder = AppConfig.ContentFolderName;

        // StreamingAssets 폴더가 없으면 작업 불필요
        if (!Directory.Exists(streamingAssetsPath))
        {
            Debug.Log($"[BuildContentFilter] StreamingAssets 폴더 없음 — 스킵");
            return;
        }

        // 임시 폴더 생성
        if (!Directory.Exists(tempBasePath))
        {
            Directory.CreateDirectory(tempBasePath);
        }

        // Content_* 폴더 목록 조회
        string[] contentDirs = Directory.GetDirectories(streamingAssetsPath, "Content_*");
        int movedCount = 0;

        foreach (string dirPath in contentDirs)
        {
            string dirName = Path.GetFileName(dirPath);

            // 현재 앱의 폴더는 유지
            if (dirName == currentContentFolder)
            {
                Debug.Log($"[BuildContentFilter] 유지: {dirName}");
                continue;
            }

            // 다른 앱의 폴더를 임시 이동
            string destPath = Path.Combine(tempBasePath, dirName);

            try
            {
                // 폴더 이동
                if (Directory.Exists(destPath))
                    Directory.Delete(destPath, true);
                Directory.Move(dirPath, destPath);

                // .meta 파일도 이동
                string metaPath = dirPath + ".meta";
                if (File.Exists(metaPath))
                {
                    string metaDest = destPath + ".meta";
                    if (File.Exists(metaDest)) File.Delete(metaDest);
                    File.Move(metaPath, metaDest);
                }

                movedCount++;
                Debug.Log($"[BuildContentFilter] 임시 이동: {dirName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BuildContentFilter] 폴더 이동 실패: {dirName} — {ex.Message}");
            }
        }

        Debug.Log($"[BuildContentFilter] 빌드 전처리 완료 — " +
                  $"현재 앱: {currentContentFolder}, " +
                  $"임시 이동: {movedCount}개 폴더");
    }

    /// <summary>
    /// 빌드 완료/실패 후: 임시 이동한 폴더를 원래 위치로 복원합니다.
    /// </summary>
    public void OnPostprocessBuild(BuildReport report)
    {
        string streamingAssetsPath = Application.dataPath + "/StreamingAssets";
        string tempBasePath = Path.Combine(Application.dataPath, "..", TempFolderName);

        // 임시 폴더가 없으면 복원 불필요
        if (!Directory.Exists(tempBasePath))
        {
            return;
        }

        // 임시 폴더 내 Content_* 폴더 복원
        string[] tempDirs = Directory.GetDirectories(tempBasePath, "Content_*");
        int restoredCount = 0;

        foreach (string tempDirPath in tempDirs)
        {
            string dirName = Path.GetFileName(tempDirPath);
            string originalPath = Path.Combine(streamingAssetsPath, dirName);

            try
            {
                // 원래 위치로 복원
                if (Directory.Exists(originalPath))
                    Directory.Delete(originalPath, true);
                Directory.Move(tempDirPath, originalPath);

                // .meta 파일 복원
                string metaTemp = tempDirPath + ".meta";
                if (File.Exists(metaTemp))
                {
                    string metaOriginal = originalPath + ".meta";
                    if (File.Exists(metaOriginal)) File.Delete(metaOriginal);
                    File.Move(metaTemp, metaOriginal);
                }

                restoredCount++;
                Debug.Log($"[BuildContentFilter] 복원: {dirName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BuildContentFilter] 폴더 복원 실패: {dirName} — {ex.Message}");
            }
        }

        // 임시 폴더 삭제
        try
        {
            if (Directory.Exists(tempBasePath))
            {
                Directory.Delete(tempBasePath, true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[BuildContentFilter] 임시 폴더 삭제 실패: {ex.Message}");
        }

        Debug.Log($"[BuildContentFilter] 빌드 후처리 완료 — 복원: {restoredCount}개 폴더");

        // Unity에 에셋 변경 알림
        UnityEditor.AssetDatabase.Refresh();
    }
}
#endif
