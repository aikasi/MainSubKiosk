using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// ITransitionEffect 구현: alpha 크로스페이드 + DOScale(0→1) 팝업 효과를 병합합니다.
/// 상세 페이지 등장 시 점에서 원래 크기로 커지면서 나타나는 연출입니다.
/// </summary>
public class PopupTransition : MonoBehaviour, ITransitionEffect
{
    /// <summary>
    /// 전환 속도를 반환합니다 (Settings.txt의 Transition_Speed, 기본값 0.5초).
    /// </summary>
    private float TransitionSpeed => AppConfig.TransitionSpeed;

    /// <summary>
    /// 등장 애니메이션: alpha 0→1 + scale 0→1 (팝업)
    /// </summary>
    public async Task PlayEnterAsync(CanvasGroup target)
    {
        if (target == null) return;

        float duration = TransitionSpeed;
        RectTransform rectTransform = target.GetComponent<RectTransform>();

        // 초기 상태
        target.alpha = 0f;
        if (rectTransform != null) rectTransform.localScale = Vector3.zero;
        target.gameObject.SetActive(true);

        // 동시에 페이드인 + 스케일업 실행
        Sequence sequence = DOTween.Sequence();
        sequence.Join(target.DOFade(1f, duration).SetEase(Ease.OutCubic));

        if (rectTransform != null)
        {
            sequence.Join(rectTransform.DOScale(Vector3.one, duration)
                .SetEase(Ease.OutBack) // 약간 튕기는 효과
                );
        }

        sequence.SetUpdate(true);

        // 완료 대기
        while (sequence.IsActive() && !sequence.IsComplete())
        {
            await Task.Yield();
        }
    }

    /// <summary>
    /// 퇴장 애니메이션: alpha 1→0 + scale 1→0 (축소)
    /// </summary>
    public async Task PlayExitAsync(CanvasGroup target)
    {
        if (target == null) return;

        float duration = TransitionSpeed;
        RectTransform rectTransform = target.GetComponent<RectTransform>();

        // 동시에 페이드아웃 + 스케일다운 실행
        Sequence sequence = DOTween.Sequence();
        sequence.Join(target.DOFade(0f, duration).SetEase(Ease.InCubic));

        if (rectTransform != null)
        {
            sequence.Join(rectTransform.DOScale(Vector3.zero, duration)
                .SetEase(Ease.InBack)// 약간 튕기는 효과
                ); 
        }

        sequence.SetUpdate(true);

        // 완료 대기
        while (sequence.IsActive() && !sequence.IsComplete())
        {
            await Task.Yield();
        }

        target.gameObject.SetActive(false);

        // 다음 등장을 위해 스케일 복원
        if (rectTransform != null) rectTransform.localScale = Vector3.one;
    }
}
