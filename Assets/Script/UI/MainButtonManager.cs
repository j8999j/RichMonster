using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 畫面按鈕管理腳本
/// 監聽遊戲階段 (DayPhase) 變化，根據當前階段切換五個主要按鈕的顯示狀態
/// 成就、背包、紀念品、新聞、圖鑑
/// </summary>
public class MainButtonManager : MonoBehaviour
{
    [Header("主畫面按鈕")]
    [SerializeField] private GameObject achievementButton;   // 成就
    [SerializeField] private GameObject backpackButton;      // 背包
    [SerializeField] private GameObject souvenirButton;      // 紀念品
    [SerializeField] private GameObject newsButton;          // 新聞
    [SerializeField] private GameObject bookButton;          // 圖鑑

    private void OnEnable()
    {
        GameFlowEvents.OnDayPhaseChanged += OnDayPhaseChanged;
    }

    private void OnDisable()
    {
        GameFlowEvents.OnDayPhaseChanged -= OnDayPhaseChanged;
    }

    private void Start()
    {
        // 場景載入後主動根據當前階段設定按鈕，避免錯過已觸發的事件
        var currentPhase = DataManager.Instance.CurrentPlayerData.PlayingStatus;
        OnDayPhaseChanged(currentPhase);
    }

    /// <summary>
    /// 根據遊戲階段切換按鈕顯示
    /// </summary>
    private void OnDayPhaseChanged(DayPhase phase)
    {
        switch (phase)
        {
            case DayPhase.HumanDay:
                SetButtonsActive(
                    achievement: true,
                    backpack: true,
                    souvenir: true,
                    news: false,
                    book: true
                );
                break;

            case DayPhase.AfterNoon:
                SetButtonsActive(
                    achievement: true,
                    backpack: true,
                    souvenir: !AuctionDayGuide.ShouldHideSouvenirButton(DataManager.Instance?.CurrentPlayerData),
                    news: false,
                    book: true
                );
                break;

            case DayPhase.Night:
                SetButtonsActive(
                    achievement: true,
                    backpack: true,
                    souvenir: true,
                    news: true,
                    book: true
                );
                break;
        }
    }

    /// <summary>
    /// 統一設定五個按鈕的顯示/隱藏狀態
    /// </summary>
    private void SetButtonsActive(bool achievement, bool backpack, bool souvenir, bool news, bool book)
    {
        if (achievementButton != null) achievementButton.SetActive(achievement);
        if (backpackButton != null) backpackButton.SetActive(backpack);
        if (souvenirButton != null) souvenirButton.SetActive(souvenir);
        if (newsButton != null) newsButton.SetActive(news);
        if (bookButton != null) bookButton.SetActive(book);
    }
}
