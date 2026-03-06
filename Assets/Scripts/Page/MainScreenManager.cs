using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 화면의 버튼 바인딩과 클릭 이벤트를 관리합니다.
/// ButtonBinder를 통해 외부 이미지를 로드하고,
/// 각 버튼 클릭 시 PageNavigator를 통해 상세 페이지로 이동합니다.
/// </summary>
public class MainScreenManager : MonoBehaviour
{
    [Tooltip("ButtonBinder 참조 (Inspector에서 연결)")]
    [SerializeField] private ButtonBinder buttonBinder;

    [Tooltip("PageNavigator 참조 (Inspector에서 연결)")]
    [SerializeField] private PageNavigator pageNavigator;

    /// <summary>
    /// 메인 화면을 초기화합니다.
    /// 버튼 이미지 바인딩 및 클릭 이벤트를 등록합니다.
    /// AppBootstrapper에서 호출됩니다.
    /// </summary>
    public async void InitializeAsync()
    {
        if (buttonBinder == null)
        {
            LogError("화면 설정 오류: 버튼 관리 시스템(ButtonBinder) 설정이 누락되었습니다.");
            return;
        }

        // 버튼 이미지 비동기 바인딩
        await buttonBinder.BindAllButtonsAsync();

        // 클릭 이벤트 등록 (ButtonBinder의 버튼 리스트 사용)
        RegisterButtonEvents();

        string msg = "[INFO] 메인 화면 초기화 완료";
        Debug.Log(msg);

    }

    /// <summary>
    /// 각 버튼에 클릭 이벤트를 등록합니다.
    /// ButtonBinder의 버튼 리스트를 참조합니다.
    /// </summary>
    private void RegisterButtonEvents()
    {
        List<Button> buttons = buttonBinder.Buttons;

        for (int i = 0; i < buttons.Count; i++)
        {
            Button btn = buttons[i];
            if (btn == null) continue;

            int index = i; // 클로저 캡처용 로컬 변수
            btn.onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출됩니다.
    /// 해당 아이템의 상세 페이지를 엽니다.
    /// </summary>
    private void OnButtonClicked(int buttonIndex)
    {
        if (pageNavigator == null)
        {
            LogError("화면 설정 오류: 페이지 이동 시스템(PageNavigator) 기능이 연결되어 있지 않습니다.");
            return;
        }

        // 광클/멀티터치 방어: 이미 상세 페이지가 열려있거나 전환 중이면 추가 클릭 무시
        if (pageNavigator.IsDetailOpen)
        {
            Debug.Log("[INFO] 메인 화면: 상세 페이지가 이미 열려있어 추가 클릭을 무시합니다.");
            return;
        }

        string itemId = buttonBinder.GetItemId(buttonIndex);
        if (string.IsNullOrEmpty(itemId))
        {
            LogError($"설정 오류: {buttonIndex}번째 버튼에 열릴 페이지 정보(ID)가 연결되지 않았습니다.");
            return;
        }

        string msg = $"[INFO] 버튼 클릭: Button[{buttonIndex}] → 아이템 {itemId}";
        Debug.Log(msg);


        pageNavigator.OpenDetail(itemId);
    }

    /// <summary>
    /// 에러 로그를 기록합니다.
    /// </summary>
    private void LogError(string message)
    {
        string fullMsg = $"[ERROR] MainScreenManager: {message}";
        Debug.LogError(fullMsg);

    }
}
