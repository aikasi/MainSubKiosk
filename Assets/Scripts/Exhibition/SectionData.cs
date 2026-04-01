using CsvHelper.Configuration;

/// <summary>
/// CSV 한 행에 대응하는 전시관 데이터 모델.
/// 헤더 없는 CSV의 0-based 인덱스로 매핑됩니다.
/// Index 0: Id, Index 1: Title, Index 2: Name, Index 3: DetailContent
/// </summary>
public class SectionData
{
    /// <summary>고유 식별자 (예: 1, 2, 3...)</summary>
    public int Id { get; set; }

    /// <summary>제목 (버튼 및 상세용)</summary>
    public string Title { get; set; }

    /// <summary>명칭 (버튼 및 상세용)</summary>
    public string Name { get; set; }

    /// <summary>상세 내용 (상세 페이지 전용)</summary>
    public string DetailContent { get; set; }
}

/// <summary>
/// CsvHelper용 ClassMap — 헤더 없는 CSV에서 0-based 인덱스로 필드를 매핑합니다.
/// </summary>
public sealed class SectionDataMap : ClassMap<SectionData>
{
    public SectionDataMap()
    {
        Map(m => m.Id).Index(0);
        Map(m => m.Title).Index(1);
        Map(m => m.Name).Index(2);
        Map(m => m.DetailContent).Index(3);
    }
}
