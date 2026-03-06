using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 화면 전환 효과의 공통 인터페이스.
/// 구현체를 교체하면 다른 코드 수정 없이 전환 효과를 변경할 수 있습니다.
/// </summary>
public interface ITransitionEffect
{
    /// <summary>
    /// 등장 애니메이션을 재생합니다 (UI가 나타남).
    /// </summary>
    /// <param name="target">애니메이션 대상 CanvasGroup</param>
    Task PlayEnterAsync(CanvasGroup target);

    /// <summary>
    /// 퇴장 애니메이션을 재생합니다 (UI가 사라짐).
    /// </summary>
    /// <param name="target">애니메이션 대상 CanvasGroup</param>
    Task PlayExitAsync(CanvasGroup target);
}
