using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Shop;
using System;
using GameSystem;

/// <summary>
/// 飲料自動販賣機的 ShopView：每個格子對應一個獨立的購買按鈕（含價格文字），
/// 由本 View 統一管理按鈕的 callback 綁定與刷新。
/// 點擊物品本身只顯示詳情，不參與購買。
/// </summary>
public class VendingMachineShopView : ShopViewBase
{
    [Header("Root UI")]
    public GameObject PanelRoot;
    public GameObject ShopShelfUI;
    public Transform SlotContainer;
    public VendingMachineSlot SlotPrefab;

    public int TargetLongEdgeSize = 100;

    [Header("Buy Buttons")]
    public Button[] BuyButtons;        // 於編輯器預先配置，child(0) 為 TextMeshProUGUI 顯示價格
    public Sprite BuyButtonSprite_CanBuy;
    public Sprite BuyButtonSprite_SoldOut;

    [Header("Detail Panel")]
    public GameObject DetailRoot;
    public Image DetailIcon;
    public Image WorldIcon;
    public Image TypeIcon;
    public Image RarityIcon;
    public Sprite PropSprite;
    public Sprite FoodSprite;
    public Sprite EquipmentSprite;
    public Sprite MonsterTagSprite;
    public Sprite HumanTagSprite;
    public Sprite DetailIconSprite_Empty;
    public TextMeshProUGUI DetailNameText;
    public TextMeshProUGUI DetailDescText;
    public Button CloseButton;
    public GameObject TagsPrefab;
    public Transform ItemTagCotainer;

    [Header("SFX")]
    [SerializeField] private AudioClip buySuccessSfx;
    [SerializeField] private AudioClip buyFailedSfx;
    [SerializeField] private AudioClip itemClickSfx;
    [SerializeField] private AudioClip openPanelSfx;
    [SerializeField] private AudioClip closePanelSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolumeScale = 1f;

    void Awake()
    {
        if (CloseButton != null) CloseButton.onClick.AddListener(OnCloseButtonClicked);
    }

    public override bool IsVisible => PanelRoot.activeSelf;

    public override void SetVisible()
    {
        PanelisVisible = !PanelisVisible;
        PlaySfx(PanelisVisible ? openPanelSfx : closePanelSfx);
        ClearDetailPanel();
        PanelRoot.SetActive(PanelisVisible);
        ShopShelfUI.SetActive(PanelisVisible);
        DetailRoot.SetActive(false);
        CloseButton.gameObject.SetActive(PanelisVisible);
    }

    public override void ShowItems(List<ShelfSlot> items, Action<ShelfSlot> onBuyRequest)
    {
        _onBuyRequestCallback = onBuyRequest;
        AdjustSlotCount(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            _activeSlots[i].Setup(items[i], OnSlotSelected);
            _activeSlots[i].gameObject.SetActive(true);

            if (i < BuyButtons.Length && BuyButtons[i] != null)
            {
                SetupBuyButton(BuyButtons[i], items[i], onBuyRequest);
                BuyButtons[i].gameObject.SetActive(true);
            }
        }

        for (int i = items.Count; i < _activeSlots.Count; i++)
            _activeSlots[i].gameObject.SetActive(false);

        for (int i = items.Count; i < BuyButtons.Length; i++)
            if (BuyButtons[i] != null) BuyButtons[i].gameObject.SetActive(false);
    }

    public override void RefreshAll()
    {
        for (int i = 0; i < _activeSlots.Count; i++)
        {
            if (!_activeSlots[i].gameObject.activeSelf) continue;
            _activeSlots[i].RefreshView();
            if (i < BuyButtons.Length && BuyButtons[i] != null)
                RefreshBuyButton(BuyButtons[i], _activeSlots[i]._currentData);
        }
    }

    private void AdjustSlotCount(int targetCount)
    {
        while (_activeSlots.Count < targetCount)
            _activeSlots.Add(Instantiate(SlotPrefab, SlotContainer));
    }

    private void SetupBuyButton(Button btn, ShelfSlot slot, Action<ShelfSlot> onBuyRequest)
    {
        btn.onClick.RemoveAllListeners();
        var captured = slot;
        btn.onClick.AddListener(() =>
        {
            if (captured == null || captured.Purchased)
            {
                PlayBuyFailedSfx();
                return;
            }

            onBuyRequest?.Invoke(captured);
        });
        RefreshBuyButton(btn, slot);
    }

    private void RefreshBuyButton(Button btn, ShelfSlot slot)
    {
        if (btn == null || slot == null) return;

        if (btn.transform.childCount > 0 &&
            btn.transform.GetChild(0).TryGetComponent<TextMeshProUGUI>(out var priceText))
        {
            priceText.text = $"${slot.Price}";
        }

        if (btn.image != null)
            btn.image.sprite = slot.Purchased ? BuyButtonSprite_SoldOut : BuyButtonSprite_CanBuy;
    }

    private void OnSlotSelected(ShopSlotBase selectedSlot)
    {
        PlaySfx(itemClickSfx);
        _currentSelectedData = selectedSlot._currentData;
        UpdateDetailPanel(selectedSlot);
    }

    private void UpdateDetailPanel(ShopSlotBase slotUI)
    {
        foreach (Transform child in ItemTagCotainer)
            Destroy(child.gameObject);

        if (DetailRoot != null) DetailRoot.SetActive(true);
        var data = slotUI._currentData;
        var itemData = data.Item;

        if (DetailNameText != null) DetailNameText.text = itemData.Name;
        if (DetailDescText != null) DetailDescText.text = itemData.Description;
        if (WorldIcon != null) WorldIcon.sprite = itemData.World == ItemWorld.Human ? HumanTagSprite : MonsterTagSprite;
        if (TypeIcon != null) TypeIcon.sprite = itemData.Type == ItemType.Prop ? PropSprite : itemData.Type == ItemType.Food ? FoodSprite : EquipmentSprite;
        if (DetailIcon != null) DetailIcon.sprite = slotUI._targetImage.sprite;
        SpriteLoader.AdjustImageScale(DetailIcon, TargetLongEdgeSize);

        SpriteLoader.LoadSpriteAsync(itemData.Rarity.ToString(), sprite =>
        {
            if (RarityIcon == null) return;
            RarityIcon.sprite = sprite != null ? sprite : DetailIconSprite_Empty;
        });
        ShowTags(itemData.Tags);
    }

    private void ShowTags(List<string> tags)
    {
        if (tags == null || TagsPrefab == null || ItemTagCotainer == null) return;

        for (int i = 0; i < tags.Count; i++)
        {
            string tagId = tags[i];
            string tagName = DataManager.Instance.GetTagNameByTag(tagId);
            if (tagName == "") continue;

            GameObject newSlot = Instantiate(TagsPrefab, ItemTagCotainer);
            TextMeshProUGUI textComp = newSlot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            textComp.text = tagName;

            GameObject imgObj = new GameObject("TagImage");
            imgObj.transform.SetParent(newSlot.transform, false);
            Image tagImage = imgObj.AddComponent<Image>();
            imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            imgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 65);
            imgObj.SetActive(false);

            Image capturedImage = tagImage;
            TextMeshProUGUI capturedText = textComp;
            GameObject capturedImgObj = imgObj;

            SpriteLoader.LoadSpriteAsync(tagId, sprite =>
            {
                if (capturedImgObj == null) return;
                if (sprite != null)
                {
                    capturedImage.sprite = sprite;
                    capturedImage.SetNativeSize();
                    RectTransform rt = capturedImage.GetComponent<RectTransform>();
                    float ratio = 125f / rt.sizeDelta.x;
                    rt.sizeDelta = new Vector2(125f, rt.sizeDelta.y * ratio);
                    capturedImgObj.SetActive(true);
                    capturedText.gameObject.SetActive(false);
                }
                else
                {
                    capturedImgObj.SetActive(false);
                    capturedText.gameObject.SetActive(true);
                }
            });
        }
    }

    private void ClearDetailPanel()
    {
        foreach (Transform child in ItemTagCotainer)
            Destroy(child.gameObject);

        DetailNameText.text = "";
        DetailDescText.text = "";
        WorldIcon.sprite = DetailIconSprite_Empty;
        TypeIcon.sprite = DetailIconSprite_Empty;
        DetailIcon.sprite = DetailIconSprite_Empty;
        RarityIcon.sprite = DetailIconSprite_Empty;
        _currentSelectedData = null;
    }

    private void OnCloseButtonClicked()
    {
        SetVisible();
        InvokeCloseShopUI();
    }

    public override void PlayBuySuccessSfx()
    {
        PlaySfx(buySuccessSfx);
    }

    public override void PlayBuyFailedSfx()
    {
        PlaySfx(buyFailedSfx);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
    }
}
