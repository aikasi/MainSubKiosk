using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 에러 경고 UI 팝업을 관리합니다.
/// 초기 무결성 검사 + 런타임 에러 모두 표시 가능합니다.
/// Show_Error_Popup 설정(Settings.txt)으로 표시 여부를 제어합니다.
/// </summary>
public class ErrorPopup : MonoBehaviour
{
    /// <summary>
    /// 전역 접근용 싱글 인스턴스
    /// </summary>
    public static ErrorPopup Instance { get; private set; }

    [Header("UI 참조")]
    [Tooltip("팝업 패널 (기본 비활성 상태)")]
    [SerializeField] private GameObject popupPanel;

    [Tooltip("에러 메시지를 표시할 텍스트")]
    [SerializeField] private TMP_Text errorMessageText;

    [Tooltip("닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("설정")]
    [Tooltip("최대 표시할 에러 항목 수")]
    [SerializeField] private int maxDisplayErrors = 20;

    // ── 내부 상태 ──
    private readonly List<string> errorMessages = new List<string>();

    private void Awake()
    {
        // 싱글 인스턴스 설정
        if (Instance == null)
            Instance = this;

        // 초기 상태: 숨김
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        // 닫기 버튼 이벤트 등록
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    /// <summary>
    /// 에러 메시지를 추가합니다 (팝업을 바로 띄우지 않음).
    /// IntegrityChecker 같은 배치 검사에서 사용합니다.
    /// </summary>
    public void AddError(string message)
    {
        errorMessages.Add(message);
    }

    /// <summary>
    /// 에러 메시지를 추가하고 즉시 팝업을 표시합니다.
    /// 런타임 에러(버튼 클릭 시 페이지 없음 등)에서 사용합니다.
    /// Settings.txt의 Show_Runtime_Error_Popup 설정을 확인합니다.
    /// </summary>
    public void AddAndShow(string message)
    {
        errorMessages.Add(message);

        // 런타임 에러 팝업은 별도 설정으로 제어 (기본값: true)
        if (!AppConfig.ShowRuntimeErrorPopup)
        {
            return;
        }

        Show();
    }

    /// <summary>
    /// 쌓인 에러 메시지가 있고, Settings에서 팝업 허용 시 팝업을 표시합니다.
    /// IntegrityChecker에서 배치 검사 완료 후 호출합니다.
    /// </summary>
    public bool ShowIfNeeded()
    {
        if (errorMessages.Count == 0) return false;

        if (!AppConfig.ShowErrorPopup)
        {
            Debug.Log("[INFO] ErrorPopup: Show_Error_Popup=false — 팝업 표시 생략");
            return false;
        }

        Show();
        return true;
    }


    /// <summary>
    /// 팝업을 표시합니다.
    /// </summary>
    public void Show()
    {
        if (popupPanel == null)
        {
            Debug.LogError("[ERROR] ErrorPopup: 'popupPanel' 참조가 누락되었습니다! Inspector에서 할당해주세요.");
            return;
        }

        if (errorMessageText != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>⚠ {errorMessages.Count}건의 문제가 발견되었습니다</b>");
            sb.AppendLine();

            int displayCount = Mathf.Min(errorMessages.Count, maxDisplayErrors);
            for (int i = 0; i < displayCount; i++)
            {
                sb.AppendLine($"• {errorMessages[i]}");
            }

            if (errorMessages.Count > maxDisplayErrors)
            {
                sb.AppendLine($"... 외 {errorMessages.Count - maxDisplayErrors}건");
            }

            errorMessageText.text = sb.ToString();
        }

        popupPanel.SetActive(true);
    }

    /// <summary>
    /// 팝업을 닫습니다.
    /// </summary>
    public void Hide()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        ClearErrors(); // 팝업 창을 닫을 때 누적된 에러 목록 초기화
    }

    /// <summary>
    /// 에러 목록을 초기화합니다.
    /// </summary>
    public void ClearErrors()
    {
        errorMessages.Clear();
    }
}
