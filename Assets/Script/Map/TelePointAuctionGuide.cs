using System.Collections;
using System.Threading.Tasks;
using GameSystem;
using Player;
using Talksystem;
using UnityEngine;

public class TelePointAuctionGuide : TelePoint, IMapGuideTarget
{
    public string ID => GuideIDs.Interactable.TelePointAuctionGuide;

    [Header("Dialogue")]
    [SerializeField]
    private TalkSystem talkSystem;

    [SerializeField]
    private AuctionController auctionController;

    [Header("Auction Choices")]
    [SerializeField]
    private string readyPromptFormat = "目前持有：{0}\n已達成拍賣會目標，是否準備就緒開始拍賣會？";

    [SerializeField]
    private string settlePromptFormat = "目前持有：{0}\n尚未達成 {1}，是否結算？";

    [SerializeField]
    private string yesOptionText = "是";

    [SerializeField]
    private string noOptionText = "否";

    [SerializeField]
    private float auctionStartDelayAfterTeleport = 1f;

    private bool isInteracting;
    [SerializeField]
    private Transform GuideTransform;

    private void Awake()
    {
        if (talkSystem == null && GameManager.Instance != null)
            talkSystem = GameManager.Instance.talkSystem;

        ResolveTalkSystem();
    }

    private void OnEnable()
    {
        SetMapGuide();
        AuctionDayGuide.Refresh();
    }

    private void Start()
    {
        SetMapGuide();
        AuctionDayGuide.Refresh();
    }

    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID, GuideTransform);
    }

    public override void Interact()
    {
        if (isInteracting)
            return;

        StartInteraction();
    }

    private async void StartInteraction()
    {
        isInteracting = true;

        try
        {
            TalkSystem talk = ResolveTalkSystem();
            if (talk == null)
            {
                Debug.LogWarning("[TelePointAuctionGuide] TalkSystem not found.");
                return;
            }

            LockAuctionGuideInteraction();

            int currentGold = DataManager.Instance?.CurrentPlayerData?.Gold ?? 0;
            bool hasEnoughGold = currentGold >= EndingConditionDetector.RequiredAuctionGold;
            string promptText = hasEnoughGold
                ? string.Format(readyPromptFormat, FormatAmountForTemplate(readyPromptFormat, currentGold))
                : string.Format(
                    settlePromptFormat,
                    FormatAmountForTemplate(settlePromptFormat, currentGold),
                    FormatAmountForTemplate(settlePromptFormat, EndingConditionDetector.RequiredAuctionGold));
            promptText = NormalizeAuctionText(promptText);

            int selectedIndex = await talk.ShowChoicesAsync(promptText, new[] { yesOptionText, noOptionText });
            if (this == null)
                return;

            talk.StopDialogue();

            if (selectedIndex != 0)
                return;

            if (hasEnoughGold)
            {
                AuctionDayGuide.CompleteAuctionStartGuide();
                UnlockAuctionGuideInteraction();
                base.Interact();
                StartCoroutine(StartAuctionAfterTeleport());
                return;
            }

            await TriggerType3EndingAsync();
        }
        finally
        {
            UnlockAuctionGuideInteraction();
            if (this != null)
                isInteracting = false;
        }
    }

    private async Task TriggerType3EndingAsync()
    {
        if (DataManager.Instance == null)
            return;

        DataManager.Instance.SetEndingReached(EndingType.Type3);
        await SaveGameAsync();

        SceneTransitionManager sceneManager = GameManager.Instance?.SceneManager;
        if (sceneManager != null)
            sceneManager.GoToEndStoryScene();
    }

    private void LockAuctionGuideInteraction()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.LockPlayerMove(PlayerLockSources.TelePointAuctionGuide);
        manager.LockPlayerInteract(PlayerLockSources.TelePointAuctionGuide);
    }

    private void UnlockAuctionGuideInteraction()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.UnlockPlayerMove(PlayerLockSources.TelePointAuctionGuide);
        manager.UnlockPlayerInteract(PlayerLockSources.TelePointAuctionGuide);
    }

    private TalkSystem ResolveTalkSystem()
    {
        if (talkSystem != null)
            return talkSystem;

        if (GameManager.Instance != null)
            talkSystem = GameManager.Instance.talkSystem;

        if (talkSystem == null)
            talkSystem = FindObjectOfType<TalkSystem>(true);

        return talkSystem;
    }

    private IEnumerator StartAuctionAfterTeleport()
    {
        float delay = Mathf.Max(0f, auctionStartDelayAfterTeleport);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        AuctionController controller = ResolveAuctionController();
        if (controller != null)
            controller.StartAuction();
    }

    private AuctionController ResolveAuctionController()
    {
        if (auctionController != null)
            return auctionController;

        auctionController = FindObjectOfType<AuctionController>(true);
        return auctionController;
    }

    private static async Task SaveGameAsync()
    {
        if (GameManager.Instance?.gameFlow != null)
            await GameManager.Instance.gameFlow.SaveGameAsync();
    }

    private static string FormatMoney(int amount)
    {
        return $"{amount:N0} 元";
    }

    private static string FormatAmountForTemplate(string template, int amount)
    {
        if (!string.IsNullOrEmpty(template)
            && (template.Contains("元") || template.Contains("金幣")))
        {
            return amount.ToString("N0");
        }

        return FormatMoney(amount);
    }

    private static string NormalizeAuctionText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("目前持有金幣", "目前持有")
            .Replace("目前持有元", "目前持有")
            .Replace("金幣", "元");
    }
}
