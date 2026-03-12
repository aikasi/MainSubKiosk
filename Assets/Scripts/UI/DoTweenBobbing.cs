using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class DoTweenBobbing : MonoBehaviour
{
    [Header("흔들림 설정")]
    [Tooltip("상하로 움직일 거리 (픽셀 단위)")]
    [SerializeField] private float amplitude = 10f;

    [Tooltip("한 번 왕복하는 데 걸리는 시간 (초 단위)")]
    [SerializeField] private float duration = 1.2f;

    [Tooltip("애니메이션의 부드러움 정도 (Ease 설정)")]
    [SerializeField] private Ease easeType = Ease.InOutQuad;

    [Header("작동 옵션")]
    [Tooltip("시작 시 임의의 딜레이를 주어 여러 버튼이 동시에 움직이지 않게 합니다.")]
    [SerializeField] private bool randomStart = true;

    [Tooltip("TimeScale의 영향을 받지 않도록 합니다.")]
    [SerializeField] private bool isIndependentUpdate = true;

    private RectTransform rectTransform;
    private Tweener bobbingTweener;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        StartAnimation();
    }

    private void OnDisable()
    {
        KillAnimation();
    }

    private void StartAnimation()
    {
        KillAnimation();

        // 현재 위치를 기준으로 상대적으로 움직이기 위해 SetRelative(true) 사용
        // Y축으로 amplitude만큼 이동했다가 다시 돌아오는 루프 구성
        bobbingTweener = rectTransform.DOAnchorPosY(amplitude, duration)
            .SetRelative(true)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(isIndependentUpdate);

        // 모든 버튼이 똑같이 움직이면 어색하므로 랜덤한 지점에서 시작하게 함
        if (randomStart)
        {
            float randomOffset = Random.Range(0f, duration);
            bobbingTweener.Goto(randomOffset, true);
        }
    }

    private void KillAnimation()
    {
        if (bobbingTweener != null && bobbingTweener.IsActive())
        {
            bobbingTweener.Kill();
        }
    }
    private void OnDestroy()
    {
        KillAnimation();
    }
}
