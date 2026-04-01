using System.Collections.Generic;

/// <summary>
/// 전시관 상세 페이지가 구현해야 하는 데이터 수신 인터페이스.
/// ExhibitManager는 이 인터페이스에만 의존하여 느슨한 결합을 유지합니다.
/// 구현 측(ExhibitDetailPage)에서 전달받은 이미지 경로를 PageTexturePool을 통해 Book에 주입합니다.
/// </summary>
public interface IExhibitDetailReceiver
{
    /// <summary>
    /// CSV에서 파싱한 텍스트 데이터와 로컬 이미지 경로 리스트를 받아
    /// 상세 페이지를 표시합니다.
    /// </summary>
    /// <param name="data">CSV 파싱 데이터 (Id, Title, Name, DetailContent)</param>
    /// <param name="imagePaths">해당 전시물의 이미지 로컬 절대 경로 리스트 (PageTexturePool 주입용)</param>
    void ShowExhibitDetail(SectionData data, List<string> imagePaths);
}
