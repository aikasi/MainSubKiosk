using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 화면의 버튼 바인딩과 클릭 이벤트를 관리합니다.
/// ButtonBinder를 통해 외부 이미지를 로드하고,
/// 각 버튼 클릭 시 PageNavigator를 통해 상세 페이지로 이동합니다.
/// 클릭 시 On 상태로 전환, 로딩 완료 후 Off로 복원합니다.
/// </summary>
public class MainScreenManager : MonoBehaviour
{
    [Tooltip("ButtonBinder 참조 (Inspector에서 연결)")]
    [SerializeField] private ButtonBinder buttonBinder;

    [Tooltip("PageNavigator 참조 (Inspector에서 연결)")]
    [SerializeField] private PageNavigator pageNavigator;

    // 현재 On 상태인 버튼 인덱스 (-1이면 없음)
    private int activeButtonIndex = -1;

    /// <summary>
    /// 메인 화면을 초기화합니다.
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

        // 클릭 이벤트 등록
        RegisterButtonEvents();

        Debug.Log("[INFO] 메인 화면 초기화 완료");
    }

    /// <summary>
    /// 각 버튼에 클릭 이벤트를 등록합니다.
    /// </summary>
    private void RegisterButtonEvents()
    {
        List<Button> buttons = buttonBinder.Buttons;

        for (int i = 0; i < buttons.Count; i++)
        {
            Button btn = buttons[i];
            if (btn == null) continue;

            int index = i;
            btn.onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출됩니다.
    /// On 상태로 전환 후 상세 페이지를 엽니다.
    /// </summary>
    private void OnButtonClicked(int buttonIndex)
    {
        if (pageNavigator == null)
        {
            LogError("화면 설정 오류: 페이지 이동 시스템(PageNavigator) 기능이 연결되어 있지 않습니다.");
            return;
        }

        // 광클 방어: 이미 열려있거나 전환 중이면 무시
        if (pageNavigator.IsDetailOpen)
        {
            return;
        }

        string itemId = buttonBinder.GetItemId(buttonIndex);
        if (string.IsNullOrEmpty(itemId))
        {
            LogError($"설정 오류: {buttonIndex}번째 버튼에 열릴 페이지 정보(ID)가 연결되지 않았습니다.");
            return;
        }

        // 버튼 On 상태 전환 + 페이지 열기 (전환이 실제로 시작된 경우에만 On)
        bool accepted = pageNavigator.OpenDetail(itemId);
        if (accepted)
        {
            buttonBinder.SetButtonState(buttonIndex, true);
            activeButtonIndex = buttonIndex;
        }

        Debug.Log($"[INFO] 버튼 클릭: Button[{buttonIndex}] → 아이템 {itemId} (수락: {accepted})");
    }

    /// <summary>
    /// 상세 페이지 로딩이 완료되었거나 닫혔을 때 호출됩니다.
    /// 활성화된 버튼을 Off 상태로 복원합니다.
    /// PageNavigator에서 콜백으로 호출됩니다.
    /// </summary>
    public void ResetActiveButton()
    {
        if (activeButtonIndex >= 0)
        {
            buttonBinder.SetButtonState(activeButtonIndex, false);
            activeButtonIndex = -1;
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ERROR] MainScreenManager: {message}");
    }
}
