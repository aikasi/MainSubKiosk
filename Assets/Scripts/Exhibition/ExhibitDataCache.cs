using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEngine;

/// <summary>
/// CSV 파일(Data.csv)을 파싱하여 Dictionary&lt;int, SectionData&gt;에 캐싱하는 매니저.
/// 앱 시작 시 Initialize()를 호출하면 전체 데이터를 메모리에 올려 Pre-parsing 합니다.
/// </summary>
public class ExhibitDataCache : MonoBehaviour
{
    // ── 캐시 저장소 (Key: 고유 ID, Value: SectionData) ──
    private readonly Dictionary<int, SectionData> dataCache = new Dictionary<int, SectionData>();

    /// <summary>초기화 완료 여부</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>캐싱된 전체 데이터 건수</summary>
    public int Count => dataCache.Count;

    /// <summary>
    /// CSV 파일을 파싱하여 Dictionary에 캐싱합니다.
    /// 중복 ID가 발견되면 Debug.LogError로 명확히 표시합니다.
    /// </summary>
    public void Initialize()
    {
        if (IsInitialized) return;

        dataCache.Clear();

        string csvPath = GetCsvPath();

        // ── 파일 존재 여부 검사 ──
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[ERROR] ExhibitDataCache: CSV 파일을 찾을 수 없습니다. 경로: {csvPath}");
            return;
        }

        try
        {
            ParseCsvFile(csvPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ERROR] ExhibitDataCache: CSV 파싱 중 예외 발생 — {ex.Message}");
            return;
        }

        if (dataCache.Count == 0)
        {
            Debug.LogWarning("[WARN] ExhibitDataCache: CSV 파일에 유효한 데이터가 없습니다.");
        }
        else
        {
            Debug.Log($"[INFO] ExhibitDataCache: 초기화 완료. 총 {dataCache.Count}건 캐싱.");
        }

        IsInitialized = true;
    }

    /// <summary>
    /// CsvHelper를 사용하여 CSV 파일을 파싱합니다.
    /// 헤더 없는 CSV, UTF-8 인코딩, 중복 ID 검사를 수행합니다.
    /// </summary>
    private void ParseCsvFile(string csvPath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,          // 헤더 없는 CSV
            MissingFieldFound = null,         // 누락 필드 무시 (로그로 처리)
            BadDataFound = context =>         // 손상 데이터 발견 시 로그
            {
                Debug.LogWarning($"[WARN] ExhibitDataCache: 손상된 CSV 데이터 발견 — {context.RawRecord}");
            }
        };

        // UTF-8 BOM 포함/미포함 모두 대응
        using (var reader = new StreamReader(csvPath, System.Text.Encoding.UTF8))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<SectionDataMap>();

            int lineNumber = 0;

            while (csv.Read())
            {
                lineNumber++;

                SectionData record;
                try
                {
                    record = csv.GetRecord<SectionData>();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[WARN] ExhibitDataCache: {lineNumber}번째 행 파싱 실패 (스킵) — {ex.Message}");
                    continue;
                }

                if (record == null)
                {
                    Debug.LogWarning($"[WARN] ExhibitDataCache: {lineNumber}번째 행이 null입니다 (스킵).");
                    continue;
                }

                // ── 중복 ID 검사 (핵심 요구사항) ──
                if (dataCache.ContainsKey(record.Id))
                {
                    Debug.LogError(
                        $"[ERROR] ExhibitDataCache: 중복된 ID 발견! ID={record.Id}, " +
                        $"제목='{record.Title}' (CSV {lineNumber}번째 행). " +
                        $"해당 행은 무시됩니다.");
                    continue;
                }

                dataCache.Add(record.Id, record);
            }
        }
    }

    /// <summary>
    /// ID로 SectionData를 조회합니다.
    /// </summary>
    /// <param name="id">고유 ID</param>
    /// <param name="data">조회된 데이터 (없으면 null)</param>
    /// <returns>데이터 존재 여부</returns>
    public bool TryGetData(int id, out SectionData data)
    {
        return dataCache.TryGetValue(id, out data);
    }

    /// <summary>
    /// 캐싱된 모든 SectionData를 반환합니다. (버튼 생성용)
    /// </summary>
    public IReadOnlyDictionary<int, SectionData> GetAllData()
    {
        if (!IsInitialized) Initialize();
        return dataCache;
    }

    /// <summary>
    /// CSV 파일의 절대 경로를 반환합니다.
    /// 에디터: Assets/StreamingAssets/Content_MediaTable/CSV/Data.csv
    /// 빌드: streamingAssetsPath/Content_MediaTable/CSV/Data.csv
    /// </summary>
    private string GetCsvPath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "StreamingAssets","Content_MediaTable" ,"CSV", "Data.csv");
#else
        return Path.Combine(Application.streamingAssetsPath, "Content_MediaTable", "CSV", "Data.csv");
#endif
    }
}
