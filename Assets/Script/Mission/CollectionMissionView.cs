using System;
using System.Collections.Generic;
using GameSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectionMissionView : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private GameObject missionPanel;

    [SerializeField]
    private TextMeshProUGUI titleText;

    [SerializeField]
    private TextMeshProUGUI descriptionText;

    [SerializeField]
    private RectTransform progressImageRect;

    [SerializeField]
    private float progressWidthPerPoint = 57f;

    [SerializeField]
    private Image selectedItemImage;

    [SerializeField]
    private TextMeshProUGUI selectedItemText;

    [SerializeField]
    private TextMeshProUGUI selectedPointText;

    [SerializeField]
    private float selectedImageLongEdgeSize = 160f;

    [SerializeField]
    private Button submitButton;

    [SerializeField]
    private Image submitButtonImage;

    [SerializeField]
    private Sprite submittableSubmitSprite;

    [SerializeField]
    private Sprite submittedSubmitSprite;

    [SerializeField]
    private Button closeButton;

    [Header("Reward Panel")]
    [SerializeField]
    private Button rewardPanelToggleButton;

    [SerializeField]
    private GameObject rewardPanel;

    [SerializeField]
    private Button rewardPanelCloseButton;

    [SerializeField]
    private bool openRewardPanelOnOpen;

    [SerializeField]
    private Button reward3Button;

    [SerializeField]
    private Image reward3ButtonImage;

    [SerializeField]
    private Button reward5Button;

    [SerializeField]
    private Image reward5ButtonImage;

    [SerializeField]
    private Button reward10Button;

    [SerializeField]
    private Image reward10ButtonImage;

    [SerializeField]
    private Sprite claimRewardSprite;

    [SerializeField]
    private Sprite claimedRewardSprite;

    [SerializeField]
    private Transform slotContainer;

    [SerializeField]
    private CollectionMissionRequirementSlot slotPrefab;

    [Header("Sound Effects")]
    [SerializeField]
    private AudioClip submitSfx;

    [SerializeField]
    private AudioClip selectSfx;

    [SerializeField]
    private AudioClip closeSfx;

    public event Action OnClosed;

    private readonly List<CollectionMissionRequirementSlot> activeSlots = new List<CollectionMissionRequirementSlot>();
    private CollectionMission mission;
    private CollectionMissionTracker tracker;
    private CollectionMissionRace currentRace;
    private CollectionMissionRequirementSlot selectedSlot;
    private string selectedImageItemId;
    private bool rewardPanelOpen;

    private void Awake()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitSelected);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (rewardPanelToggleButton != null)
            rewardPanelToggleButton.onClick.AddListener(ToggleRewardPanel);

        if (rewardPanelCloseButton != null)
            rewardPanelCloseButton.onClick.AddListener(() => SetRewardPanelOpen(false));

        if (reward3Button != null)
            reward3Button.onClick.AddListener(() => ClaimReward(CollectionMissionTracker.BronzeRewardPoints));

        if (reward5Button != null)
            reward5Button.onClick.AddListener(() => ClaimReward(CollectionMissionTracker.SilverRewardPoints));

        if (reward10Button != null)
            reward10Button.onClick.AddListener(() => ClaimReward(CollectionMissionTracker.GoldRewardPoints));

        if (missionPanel != null)
            missionPanel.SetActive(false);

        if (rewardPanel != null)
            rewardPanel.SetActive(false);
    }

    public void Open(CollectionMission collectionMission, CollectionMissionTracker collectionTracker, CollectionMissionRace race)
    {
        mission = collectionMission;
        tracker = collectionTracker;
        currentRace = race;
        selectedSlot = null;
        rewardPanelOpen = openRewardPanelOnOpen;

        if (tracker != null && mission != null)
            tracker.SetMission(mission);

        if (missionPanel != null)
            missionPanel.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (missionPanel != null)
            missionPanel.SetActive(false);

        SetRewardPanelOpen(false);
        PlaySfx(closeSfx);
        OnClosed?.Invoke();
    }

    public void Refresh()
    {
        var category = mission != null ? mission.GetCategory(currentRace) : null;

        if (titleText != null)
            titleText.text = category != null ? category.DisplayName : CollectionMissionRaceUtility.GetRaceName(currentRace);

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (category == null || category.Items == null)
        {
            HideAllSlots();
            RefreshProgress(null);
            RefreshRewards(null);
            RefreshSelection(null);
            return;
        }

        AdjustSlotCount(category.Items.Count);
        int visibleCount = Mathf.Min(category.Items.Count, activeSlots.Count);

        for (int i = 0; i < visibleCount; i++)
        {
            var entry = category.Items[i];
            var slotState = ResolveSlotState(entry);
            activeSlots[i].Setup(entry, slotState, SelectSlot);
            activeSlots[i].gameObject.SetActive(true);
        }

        for (int i = visibleCount; i < activeSlots.Count; i++)
            activeSlots[i].gameObject.SetActive(false);

        if (selectedSlot == null || !activeSlots.Contains(selectedSlot) || !selectedSlot.gameObject.activeSelf)
            selectedSlot = visibleCount > 0 ? activeSlots[0] : null;

        RefreshProgress(category);
        RefreshRewards(category);
        RefreshSelection(selectedSlot);
    }

    private CollectionMissionSlotState ResolveSlotState(CollectionMissionItemEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.ItemID))
            return CollectionMissionSlotState.Unknown;

        if (tracker != null && tracker.IsItemCollected(entry.ItemID))
            return CollectionMissionSlotState.Collected;

        if (!IsItemBooked(entry.ItemID))
            return CollectionMissionSlotState.Unknown;

        if (tracker != null && tracker.GetFirstSubmittableItem(entry.ItemID) != null)
            return CollectionMissionSlotState.ReadyToSubmit;

        return CollectionMissionSlotState.Recorded;
    }

    private bool IsItemBooked(string itemId)
    {
        var book = DataManager.Instance?.GetBookData();
        var itemBooks = book?.ItemBookData?.ItemBooks;
        if (itemBooks == null)
            return false;

        var entry = itemBooks.Find(item => item != null && item.ItemID == itemId);
        return entry != null && entry.IsBooked;
    }

    private void SelectSlot(CollectionMissionRequirementSlot slot)
    {
        selectedSlot = slot;
        PlaySfx(selectSfx);
        RefreshSelection(slot);
    }

    private void RefreshSelection(CollectionMissionRequirementSlot slot)
    {
        var entry = slot?.Entry;
        var itemDefinition = slot?.ItemDefinition;
        var slotState = slot != null ? slot.State : CollectionMissionSlotState.Unknown;
        bool canSubmit = slot != null && slot.CanSubmit && !HasReachedRewardLimit();
        bool isSubmitted = slotState == CollectionMissionSlotState.Collected;

        if (selectedItemText != null)
        {
            selectedItemText.text = IsSelectedItemNameVisible(slotState) && itemDefinition != null
                ? itemDefinition.Name
                : entry != null ? "???" : string.Empty;
        }

        if (descriptionText != null)
            descriptionText.text = entry != null ? $"\u53EF\u7372\u5F97\u9EDE\u6578\uFF1A{entry.Points}" : string.Empty;

        if (selectedPointText != null)
            selectedPointText.text = entry != null ? $"X{entry.Points}" : string.Empty;

        RefreshSelectedImage(entry?.ItemID, slotState);

        RefreshSubmitButton(canSubmit, isSubmitted);
    }

    private bool IsSelectedItemNameVisible(CollectionMissionSlotState slotState)
    {
        return slotState != CollectionMissionSlotState.Unknown;
    }

    private void RefreshSubmitButton(bool canSubmit, bool isSubmitted)
    {
        if (submitButton != null)
            submitButton.interactable = canSubmit;

        if (submitButtonImage == null && submitButton != null)
            submitButtonImage = submitButton.image;

        if (submitButtonImage == null)
            return;

        var sprite = isSubmitted ? submittedSubmitSprite : submittableSubmitSprite;
        if (sprite != null)
            submitButtonImage.sprite = sprite;
    }

    private void RefreshSelectedImage(string itemId, CollectionMissionSlotState slotState)
    {
        if (selectedItemImage == null)
            return;

        selectedImageItemId = itemId;

        if (string.IsNullOrEmpty(itemId))
        {
            selectedItemImage.sprite = null;
            selectedItemImage.enabled = false;
            return;
        }

        selectedItemImage.color = GetImageDisplayColor(slotState);
        selectedItemImage.enabled = false;
        SpriteLoader.LoadSpriteAsync(itemId, sprite =>
        {
            if (selectedItemImage == null || selectedImageItemId != itemId)
                return;

            selectedItemImage.sprite = sprite;
            selectedItemImage.color = GetImageDisplayColor(slotState);
            selectedItemImage.enabled = sprite != null;

            if (sprite != null)
                SpriteLoader.AdjustImageScale(selectedItemImage, selectedImageLongEdgeSize);
        });
    }

    private Color GetImageDisplayColor(CollectionMissionSlotState slotState)
    {
        return slotState switch
        {
            CollectionMissionSlotState.Unknown => Color.black,
            CollectionMissionSlotState.Collected => Color.white,
            _ => Color.gray
        };
    }

    private void SubmitSelected()
    {
        if (selectedSlot == null || selectedSlot.Entry == null || tracker == null || HasReachedRewardLimit())
            return;

        var item = tracker.GetFirstSubmittableItem(selectedSlot.Entry.ItemID);
        if (item == null)
        {
            Refresh();
            return;
        }

        if (tracker.TrySubmit(item, out _, out _))
        {
            PlaySfx(submitSfx);
            selectedSlot = null;
            Refresh();
        }
    }

    private void RefreshProgress(CollectionMissionCategory category)
    {
        int points = category != null && tracker != null ? tracker.GetRacePoints(category) : 0;
        int clampedPoints = Mathf.Clamp(points, 0, CollectionMissionTracker.MaxRewardPoints);

        if (category == null || category.Items == null)
        {
            SetProgressImageWidth(0);
            return;
        }

        SetProgressImageWidth(clampedPoints * progressWidthPerPoint);
    }

    private void RefreshRewards(CollectionMissionCategory category)
    {
        bool hasCategory = category != null && tracker != null;

        if (rewardPanel != null)
            rewardPanel.SetActive(hasCategory && rewardPanelOpen);

        if (!hasCategory)
            rewardPanelOpen = false;

        RefreshRewardButton(reward3Button, reward3ButtonImage, category, CollectionMissionTracker.BronzeRewardPoints);
        RefreshRewardButton(reward5Button, reward5ButtonImage, category, CollectionMissionTracker.SilverRewardPoints);
        RefreshRewardButton(reward10Button, reward10ButtonImage, category, CollectionMissionTracker.GoldRewardPoints);
    }

    private void ToggleRewardPanel()
    {
        SetRewardPanelOpen(!rewardPanelOpen);
    }

    private void SetRewardPanelOpen(bool isOpen)
    {
        rewardPanelOpen = isOpen && mission != null && tracker != null && mission.GetCategory(currentRace) != null;

        if (rewardPanel != null)
            rewardPanel.SetActive(rewardPanelOpen);
    }

    private void RefreshRewardButton(Button button, Image image, CollectionMissionCategory category, int milestone)
    {
        bool canClaim = category != null && tracker != null && tracker.CanClaimReward(category, milestone);
        bool claimed = category != null && tracker != null && tracker.HasClaimedReward(category, milestone);

        if (button != null)
            button.interactable = canClaim;

        if (image == null && button != null)
            image = button.image;

        if (image == null)
            return;

        var sprite = claimed ? claimedRewardSprite : claimRewardSprite;
        if (sprite != null)
            image.sprite = sprite;
    }

    private void ClaimReward(int milestone)
    {
        if (tracker == null)
            return;

        if (tracker.TryClaimReward(currentRace, milestone, out _))
            Refresh();
    }

    private bool HasReachedRewardLimit()
    {
        if (tracker == null)
            return false;

        return tracker.GetRacePoints(currentRace) >= CollectionMissionTracker.MaxRewardPoints;
    }

    private void SetProgressImageWidth(float width)
    {
        if (progressImageRect == null)
            return;

        var size = progressImageRect.sizeDelta;
        size.x = Mathf.Clamp(width, 0f, CollectionMissionTracker.MaxRewardPoints * progressWidthPerPoint);
        progressImageRect.sizeDelta = size;
    }

    private void AdjustSlotCount(int targetCount)
    {
        if (slotPrefab == null || slotContainer == null)
            return;

        while (activeSlots.Count < targetCount)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            activeSlots.Add(slot);
        }
    }

    private void HideAllSlots()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(clip);
    }
}
