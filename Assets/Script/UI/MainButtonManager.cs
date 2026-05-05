using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 管理主畫面按鈕與玩家狀態列。
/// </summary>
public class MainButtonManager : MonoBehaviour
{
    [Header("主畫面按鈕")]
    [SerializeField] private GameObject achievementButton;   // 成就
    [SerializeField] private GameObject backpackButton;      // 背包
    [SerializeField] private GameObject souvenirButton;      // 紀念品
    [SerializeField] private GameObject newsButton;          // 新聞
    [SerializeField] private GameObject bookButton;          // 圖鑑

    [Header("玩家資料顯示")]
    [SerializeField] private TextMeshProUGUI daysPlayedText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Image statusIconImage;
    [SerializeField] private GameObject HumanIcon;
    [SerializeField] private GameObject MonsterIcon;

    [Header("狀態圖示")]
    [SerializeField] private Sprite morningSprite;
    [SerializeField] private Sprite noonSprite;
    [SerializeField] private Sprite nightSprite;

    private DataManager subscribedDataManager;
    private bool isAuctionHidden;

    private void OnEnable()
    {
        GameFlowEvents.OnDayPhaseChanged += OnDayPhaseChanged;
        SubscribePlayerMainView();
    }

    private void OnDisable()
    {
        GameFlowEvents.OnDayPhaseChanged -= OnDayPhaseChanged;
        UnsubscribePlayerMainView();
    }

    private void Start()
    {
        // 場景載入後主動根據當前階段設定按鈕，避免錯過已觸發的事件
        if (DataManager.Instance?.CurrentPlayerData != null)
        {
            SubscribePlayerMainView();
            OnDayPhaseChanged(DataManager.Instance.CurrentPlayerData.PlayingStatus);
            DataManager.Instance.RefreshPlayerMainView();
        }
    }

    /// <summary>
    /// 根據遊戲階段切換按鈕顯示
    /// </summary>
    private void OnDayPhaseChanged(DayPhase phase)
    {
        if (isAuctionHidden)
        {
            HideAllManagedUI();
            return;
        }

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
                    souvenir: true,
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

    public void UpdateUI(int daysPlayed, int gold, DayPhase playingStatus)
    {
        UpdatePlayingStatus(daysPlayed, playingStatus);
        UpdateGold(gold);

        if (isAuctionHidden)
        {
            SetPlayerDataUIActive(false);
        }
    }

    private void UpdatePlayingStatus(int daysPlayed, DayPhase playingStatus)
    {
        if (statusIconImage != null)
        {
            statusIconImage.sprite = GetStatusSprite(playingStatus);
        }

        if (daysPlayedText != null)
        {
            daysPlayedText.text = "距離拍賣會剩下" + (21 - daysPlayed).ToString() + "天";
        }

        SetPlayerDataUIActive(!isAuctionHidden);
        if (isAuctionHidden)
            return;

        bool isHumanSide = playingStatus == DayPhase.HumanDay || playingStatus == DayPhase.AfterNoon;
        if (HumanIcon != null) HumanIcon.SetActive(isHumanSide);
        if (MonsterIcon != null) MonsterIcon.SetActive(playingStatus == DayPhase.Night);
    }

    private void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            goldText.text = gold.ToString();
        }
    }

    private Sprite GetStatusSprite(DayPhase status)
    {
        return status switch
        {
            DayPhase.HumanDay => morningSprite,
            DayPhase.Night => nightSprite,
            DayPhase.AfterNoon => noonSprite,
            _ => null
        };
    }

    public void SetAuctionHidden(bool hidden)
    {
        if (isAuctionHidden == hidden)
            return;

        isAuctionHidden = hidden;
        if (hidden)
        {
            HideAllManagedUI();
            return;
        }

        if (DataManager.Instance?.CurrentPlayerData != null)
        {
            OnDayPhaseChanged(DataManager.Instance.CurrentPlayerData.PlayingStatus);
            DataManager.Instance.RefreshPlayerMainView();
        }
        else
        {
            SetPlayerDataUIActive(true);
        }
    }

    private void HideAllManagedUI()
    {
        SetButtonsActive(false, false, false, false, false);
        SetPlayerDataUIActive(false);
    }

    private void SetPlayerDataUIActive(bool active)
    {
        if (daysPlayedText != null) daysPlayedText.gameObject.SetActive(active);
        if (goldText != null) goldText.gameObject.SetActive(active);
        if (statusIconImage != null) statusIconImage.gameObject.SetActive(active);
        if (HumanIcon != null) HumanIcon.SetActive(active);
        if (MonsterIcon != null) MonsterIcon.SetActive(active);
    }

    private void SubscribePlayerMainView()
    {
        if (subscribedDataManager != null || DataManager.Instance == null)
            return;

        DataManager.Instance.PlayerMainViewUpdate += UpdateUI;
        subscribedDataManager = DataManager.Instance;
    }

    private void UnsubscribePlayerMainView()
    {
        if (subscribedDataManager == null)
            return;

        subscribedDataManager.PlayerMainViewUpdate -= UpdateUI;
        subscribedDataManager = null;
    }
}
