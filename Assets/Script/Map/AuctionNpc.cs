using System.Threading.Tasks;
using GameSystem;
using Player;
using Talksystem;
using UnityEngine;

public class AuctionNpc : MonoBehaviour, IInteractable, IMapGuideTarget
{
    private const int EntryFee = EndingConditionDetector.AuctionEntryFeeAmount;

    [SerializeField]
    private GameObject prompt;

    [Header("Dialogue")]
    [DialogueIdSelect]
    [SerializeField]
    private string beforePaidDialogueId = "AuctionNpc_BeforePaid_Dialogue";

    [DialogueIdSelect]
    [SerializeField]
    private string paidDialogueId = "AuctionNpc_AfterPaid_Dialogue";

    [DialogueIdSelect]
    [SerializeField]
    private string paidSuccessDialogueId = "AuctionNpc_PaidSuccess_Dialogue";

    [DialogueIdSelect]
    [SerializeField]
    private string notEnoughGoldDialogueId = "AuctionNpc_NotEnoughGold_Dialogue";

    [DialogueIdSelect]
    [SerializeField]
    private string declineDialogueId = "AuctionNpc_Decline_Dialogue";

    [SerializeField]
    private TalkSystem talkSystem;

    [Header("Entry Fee Choices")]
    [SerializeField]
    private string entryFeePromptFormat = "\u662F\u5426\u7E73\u4EA4\u62CD\u8CE3\u6703\u5165\u5834\u8CBB {0} \u91D1\u5E63\uFF1F";

    [SerializeField]
    private string payEntryFeeOptionText = "\u7E73\u4EA4\u5165\u5834\u8CBB";

    [SerializeField]
    private string declineEntryFeeOptionText = "\u66AB\u6642\u4E0D\u8981";

    [SerializeField]
    private bool saveAfterPayingEntryFee = true;

    public string ID => GuideIDs.Interactable.AuctionNpc;

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
        AuctionEntryFeeGuide.Refresh();
    }

    private void Start()
    {
        SetMapGuide();
        AuctionEntryFeeGuide.Refresh();
    }

    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID, GuideTransform);
    }

    public void ShowPrompt()
    {
        if (prompt != null)
            prompt.SetActive(true);
    }

    public void HidePrompt()
    {
        if (prompt != null)
            prompt.SetActive(false);
    }

    public void Interact()
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
            if (DataManager.Instance?.CurrentPlayerData?.HasPaidAuctionEntryFee == true)
            {
                AuctionEntryFeeGuide.Hide();
                await PlayDialogueAsync(paidDialogueId);
                return;
            }

            bool completed = await PlayDialogueAsync(beforePaidDialogueId);
            if (!completed || this == null)
                return;

            LockAuctionInteraction();
            int selectedIndex = await ShowEntryFeeChoicesAsync();
            UnlockAuctionInteraction();

            if (this == null || selectedIndex < 0)
                return;

            if (selectedIndex == 0)
                await PayEntryFeeAsync();
            else
                await PlayDialogueAsync(declineDialogueId);
        }
        finally
        {
            UnlockAuctionInteraction();
            if (this != null)
                isInteracting = false;
        }
    }

    private async Task<int> ShowEntryFeeChoicesAsync()
    {
        TalkSystem talk = ResolveTalkSystem();
        if (talk == null)
        {
            Debug.LogWarning("[AuctionNpc] TalkSystem not found.");
            return -1;
        }

        string entryFeePrompt = string.Format(entryFeePromptFormat, EntryFee);
        string promptText = $"{entryFeePrompt}\n\u6301\u6709\u91D1\u5E63: {DataManager.Instance?.CurrentPlayerData?.Gold ?? 0}";
        string[] options = { payEntryFeeOptionText, declineEntryFeeOptionText };
        return await talk.ShowChoicesAsync(promptText, options);
    }

    private async Task PayEntryFeeAsync()
    {
        bool paid = DataManager.Instance != null && DataManager.Instance.TryPayAuctionEntryFee();
        if (paid)
        {
            AuctionEntryFeeGuide.Hide();

            if (saveAfterPayingEntryFee)
                await SaveGameAsync();

            await PlayDialogueAsync(paidSuccessDialogueId);
        }
        else
        {
            await PlayDialogueAsync(notEnoughGoldDialogueId);
        }
    }

    private void LockAuctionInteraction()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.LockPlayerMove(PlayerLockSources.AuctionNpc);
        manager.LockPlayerInteract(PlayerLockSources.AuctionNpc);
    }

    private void UnlockAuctionInteraction()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.UnlockPlayerMove(PlayerLockSources.AuctionNpc);
        manager.UnlockPlayerInteract(PlayerLockSources.AuctionNpc);
    }

    private async Task<bool> PlayDialogueAsync(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
            return false;

        TalkSystem talk = ResolveTalkSystem();
        if (talk == null)
            return false;

        string dialogueText = await GameDataLoader.LoadDialogueTextAsync(dialogueId);
        if (this == null || string.IsNullOrEmpty(dialogueText))
            return false;

        talk = ResolveTalkSystem();
        return talk != null && await talk.PlayDialogueAsync(dialogueText);
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

    private static async Task SaveGameAsync()
    {
        if (GameManager.Instance?.gameFlow != null)
            await GameManager.Instance.gameFlow.SaveGameAsync();
    }
}
