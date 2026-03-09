using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

#if MEDIA_TABLE
namespace MediaTable
{
    /// <summary>
    /// 디스플레이 1개(단일 Canvas)에서 독립 구동되는 Book 에셋의 오케스트레이터(지휘자).
    /// 제공된 Book.cs 및 AutoFlip.cs 와 완벽하게 연동되어, 버튼 넘김을 제어합니다.
    /// </summary>
    public class MediaBookManager : MonoBehaviour
    {
        [Header("코어 조각 컴포넌트 연결 (필수)")]
        [SerializeField] private BookPageScanner scanner;
        [SerializeField] private PageTexturePool memoryPool;

        [Header("화살표 UI 연결 및 설정")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [Tooltip("물리적 연타 방지 쿨타임 (초)")]
        [SerializeField] private float cooldownTime = 0.5f;
        private float lastInteractionTime = 0f;
        private bool isInputBlocked = false;

        [Header("상세 페이지 전환 효과")]
        [SerializeField] private MonoBehaviour transitionEffectComponent;
        private ITransitionEffect transitionEffect;

        [Header("UI 캔버스 프레임 연결")]
        [SerializeField] private CanvasGroup detailPanel;
        [SerializeField] private Button closeButton;

        [Header("Book 에셋 연동부 (필수)")]
        [Tooltip("적용하신 Book 컴포넌트를 직접 연결하세요.")]
        [SerializeField] private Book targetBook; 
        
        [Tooltip("AutoFlip 컴포넌트 (버튼으로 넘기기 위해 필요)")]
        [SerializeField] private AutoFlip targetAutoFlip;

        private bool isShowing = false;
        private bool isOpening = false; // 연타 방지 락
        private string currentItemId = "";

        private void Awake()
        {
            if (transitionEffectComponent != null)
                transitionEffect = transitionEffectComponent as ITransitionEffect;

            if (prevButton != null)
                prevButton.onClick.AddListener(OnPrevBtnClicked);
            
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextBtnClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseBook);

            if (detailPanel != null)
            {
                detailPanel.alpha = 0f;
                detailPanel.blocksRaycasts = false;
                detailPanel.gameObject.SetActive(false);
            }

            // 책이 1장 넘어갈 때마다 Eager Load(다음 메모리 미리 로딩)를 수행하도록 이벤트 구독!
            if (targetBook != null)
            {
                targetBook.OnFlip.AddListener(OnBookFlipped);
            }
        }

        /// <summary>
        /// 외부에 있는 썸네일(리스트)을 눌렀을 때 책을 폅니다.
        /// </summary>
        public async void OpenBook(string itemId)
        {
            if (isShowing || isOpening) return;
            if (scanner == null || memoryPool == null || targetBook == null) return;

            isOpening = true;

            try
            {
                currentItemId = itemId;
                List<string> pagePaths = scanner.GetPagePaths(itemId);

                if (pagePaths.Count == 0)
                {
                    if (ErrorPopup.Instance != null) ErrorPopup.Instance.AddAndShow($"상세 페이지 이미지가 없습니다: {itemId}");
                    return;
                }

                // Book 에셋 초기화: 페이지 길이 세팅
                targetBook.currentPage = 0;
                memoryPool.InitializeBook(pagePaths, targetBook);

                // 로딩 중 UI 팝업(버튼) 차단
                isInputBlocked = true;

                // 초기 페이지 5장 Eager 로딩
                await memoryPool.UpdatePoolAsync(targetBook, targetBook.currentPage);

                // 로딩이 완료되었으므로 Book 내부 UI 갱신 (LeftNext, RightNext 등)
                targetBook.LeftNext.sprite = targetBook.background;
                targetBook.RightNext.sprite = (targetBook.bookPages.Length > 0) ? targetBook.bookPages[0] : targetBook.background;
                
                UpdateInputUI();

                // 팝업 창 활성화 애니메이션 (명시적 SetActive 호출)
                detailPanel.gameObject.SetActive(true); // 캔버스 최상단에서 SubPanel을 명시적으로 깨움
                detailPanel.blocksRaycasts = true;
                isShowing = true;
                if (transitionEffect != null)
                {
                    await transitionEffect.PlayEnterAsync(detailPanel);
                }
                else
                {
                    detailPanel.alpha = 1f;
                }

                // 팝업 로딩 블록 해제 (오직 UI 화살표 버튼만 클릭 가능해짐)
                isInputBlocked = false;
            }
            finally
            {
                // 애니메이션 연출까지 모두 끝난 뒤 락 해제
                isOpening = false;
            }
        }

        public async void CloseBook()
        {
            if (!isShowing) return;

            isInputBlocked = true;

            if (transitionEffect != null)
            {
                await transitionEffect.PlayExitAsync(detailPanel);
            }
            else
            {
                detailPanel.alpha = 0f;
            }

            detailPanel.blocksRaycasts = false;
            detailPanel.gameObject.SetActive(false); // 애니메이션 후 패널을 완전히 꺼서 성능 확보
            isShowing = false;

            memoryPool.ClearAllTextures(targetBook);
        }

        private bool CanInteract()
        {
            if (isInputBlocked) return false;
            
            if (Time.time - lastInteractionTime < cooldownTime)
            {
                Debug.Log("[INFO] 너무 빠른 연속 터치(버튼 클릭)를 대기합니다.");
                return false;
            }
            return true;
        }

        private void OnPrevBtnClicked()
        {
            if (CanInteract())
            {
                lastInteractionTime = Time.time;
                GoToPrevPage();
            }
        }

        private void OnNextBtnClicked()
        {
            if (CanInteract())
            {
                lastInteractionTime = Time.time;
                GoToNextPage();
            }
        }

        /// <summary>
        /// 좌측 화살표 버튼 클릭 시 AutoFlip의 FlipLeftPage 호출
        /// (이미지가 완전히 메모리에 올라올 때까지 기다렸다가 넘깁니다)
        /// </summary>
        private async void GoToPrevPage()
        {
            if (targetBook == null || targetAutoFlip == null) return;
            if (targetBook.currentPage <= 0) return;

            // 더블 클릭(연타) 방지 터치 블록
            isInputBlocked = true;

            // 미래에 보여질 대상 페이지
            int prevPage = targetBook.currentPage - 2;
            
            // 핵심: 이미지가 100% 준비 완료될 때까지 책장을 넘기지 않고 대기(Wait)
            await memoryPool.UpdatePoolAsync(targetBook, prevPage);
            
            // 준비 완료. 하얀색 백지의 위협 없이 안심하고 책장 넘기기 돌입!
            targetAutoFlip.FlipLeftPage();
            
        }

        /// <summary>
        /// 우측 화살표 버튼 클릭 시 AutoFlip의 FlipRightPage 호출
        /// (이미지가 완전히 메모리에 올라올 때까지 기다렸다가 넘깁니다)
        /// </summary>
        private async void GoToNextPage()
        {
            if (targetBook == null || targetAutoFlip == null) return;
            if (targetBook.currentPage >= targetBook.TotalPageCount - 1) return;

            // 더블 클릭(연타) 방지 터치 블록
            isInputBlocked = true;

            // 미래에 보여질 대상 페이지
            int nextPage = targetBook.currentPage + 2;

            // 핵심: 이미지가 100% 준비 완료될 때까지 책장을 넘기지 않고 대기(Wait)
            await memoryPool.UpdatePoolAsync(targetBook, nextPage);

            // 준비 완료. 하얀색 백지의 위협 없이 안심하고 책장 넘기기 돌입!
            targetAutoFlip.FlipRightPage();
        }

        /// <summary>
        /// AutoFlip 버튼으로든, 마우스 드래그(스와이프)로든 책이 넘어가는 애니메이션이 '완료'된 순간 불립니다.
        /// 플립 진행 중 막아두었던 터치 반응을 해제해주고, 혹시모를 메모리 갱신을 보험용으로 한 번 더 호출합니다.
        /// </summary>
        private async void OnBookFlipped()
        {
            UpdateInputUI();
            
            // 애니메이션 종료, 터치 블록 해제
            isInputBlocked = false;

            // 보험 로딩 (마우스 드래그로 스와이프해서 넘겼을 경우를 대비)
            await memoryPool.UpdatePoolAsync(targetBook, targetBook.currentPage);
        }

        private void UpdateInputUI()
        {
            if (targetBook != null)
            {
                // 책은 무조건 2장씩(왼쪽/오른쪽) 넘어가므로, 뒤로가기는 2p 이상일때만 허용해야 안전함.
                bool showPrev = targetBook.currentPage >= 2;
                bool showNext = targetBook.currentPage < targetBook.TotalPageCount - 1;
                if (prevButton != null) prevButton.gameObject.SetActive(showPrev);
                if (nextButton != null) nextButton.gameObject.SetActive(showNext);
            }
        }
    }
}
#endif
