using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// ITransitionEffect 구현: CanvasGroup의 alpha를 DOTween으로 0↔1 전환합니다.
/// 부드러운 크로스페이드 효과를 제공합니다.
/// </summary>
public class CrossfadeTransition : MonoBehaviour, ITransitionEffect
{
    /// <summary>
    /// 전환 속도를 반환합니다 (Settings.txt의 Transition_Speed, 기본값 0.5초).
    /// </summary>
    private float TransitionSpeed => AppConfig.TransitionSpeed;

    /// <summary>
    /// 등장 애니메이션: alpha 0 → 1 (페이드인)
    /// </summary>
    public async Task PlayEnterAsync(CanvasGroup target)
    {
        if (target == null) return;

        float duration = TransitionSpeed;

        // 초기 상태 설정
        target.alpha = 0f;
        target.gameObject.SetActive(true);

        // DOTween 페이드인
        var tween = target.DOFade(1f, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true); // TimeScale 영향 받지 않음

        // 완료 대기
        while (tween.IsActive() && !tween.IsComplete())
        {
            await Task.Yield();
        }
    }

    /// <summary>
    /// 퇴장 애니메이션: alpha 1 → 0 (페이드아웃)
    /// </summary>
    public async Task PlayExitAsync(CanvasGroup target)
    {
        if (target == null) return;

        float duration = TransitionSpeed;

        // DOTween 페이드아웃
        var tween = target.DOFade(0f, duration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true);

        // 완료 대기
        while (tween.IsActive() && !tween.IsComplete())
        {
            await Task.Yield();
        }

        target.gameObject.SetActive(false);
    }
}
