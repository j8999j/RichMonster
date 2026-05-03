using System;
using UnityEngine;
using UnityEngine.UI;

public enum CollectionMissionSlotState
{
    Unknown,
    Recorded,
    ReadyToSubmit,
    Collected
}

public class CollectionMissionRequirementSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private Button interactButton;

    [SerializeField]
    private Image itemImage;

    [SerializeField]
    private GameObject exclamationIcon;

    [Header("Display")]
    [SerializeField]
    private Sprite defaultSprite;

    [SerializeField]
    private float targetLongEdgeSize = 100f;

    private CollectionMissionItemEntry entry;
    private ItemDefinition itemDefinition;
    private CollectionMissionSlotState state;
    private Action<CollectionMissionRequirementSlot> onClick;
    private string currentItemId;

    public CollectionMissionItemEntry Entry => entry;
    public ItemDefinition ItemDefinition => itemDefinition;
    public CollectionMissionSlotState State => state;
    public bool CanSubmit => state == CollectionMissionSlotState.ReadyToSubmit;

    private void Awake()
    {
        if (interactButton == null)
            interactButton = GetComponent<Button>();

        if (itemImage == null)
            itemImage = GetComponentInChildren<Image>();

        if (interactButton != null)
            interactButton.onClick.AddListener(HandleClick);
    }

    public void Setup(CollectionMissionItemEntry itemEntry, CollectionMissionSlotState slotState, Action<CollectionMissionRequirementSlot> clicked)
    {
        entry = itemEntry;
        state = slotState;
        onClick = clicked;
        itemDefinition = entry != null ? DataManager.Instance.GetItemById(entry.ItemID) : null;
        currentItemId = null;

        Refresh();
    }

    public void RefreshState(CollectionMissionSlotState slotState)
    {
        state = slotState;
        RefreshVisualState();
    }

    private void Refresh()
    {
        string itemId = entry?.ItemID;
        LoadSprite(itemId);

        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        if (itemImage != null)
        {
            itemImage.color = state switch
            {
                CollectionMissionSlotState.Unknown => Color.black,
                CollectionMissionSlotState.Collected => Color.white,
                _ => Color.gray
            };
        }

        if (exclamationIcon != null)
            exclamationIcon.SetActive(state == CollectionMissionSlotState.ReadyToSubmit);

        if (interactButton != null)
            interactButton.interactable = true;
    }

    private void LoadSprite(string itemId)
    {
        if (itemImage == null)
            return;

        if (currentItemId == itemId)
            return;

        currentItemId = itemId;

        if (string.IsNullOrEmpty(itemId))
        {
            itemImage.sprite = defaultSprite;
            return;
        }

        SpriteLoader.LoadSpriteAsync(itemId, sprite =>
        {
            if (itemImage == null || currentItemId != itemId)
                return;

            itemImage.sprite = sprite ?? defaultSprite;
            SpriteLoader.AdjustImageScale(itemImage, targetLongEdgeSize);
            itemImage.enabled = true;
            RefreshVisualState();
        });
    }

    private void HandleClick()
    {
        onClick?.Invoke(this);
    }
}
