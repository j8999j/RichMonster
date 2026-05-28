using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Shop;
using System;
using GameSystem;

/// <summary>
/// 雜貨店 / 妖界商店使用的 ShopView：詳情面板下方有統一的購買按鈕，
/// 玩家須先點選商品再按購買。
/// </summary>
public class ShopUIView : ShopViewBase
{
    private List<ShelfSlot> _currentItems;

    [Header("Root UI")]
    public GameObject PanelRoot;
    public GameObject ShopShelfUI;
    public Transform SlotContainer;
    public ShopSlot SlotPrefab;
    public int TargetLongEdgeSize = 100;

    [Header("Detail Panel")]
    public GameObject DetailRoot;
    public Image DetailIcon;
    public Image WorldIcon;
    public Image TypeIcon;
    public Image RarityIcon;
    public Image DiscountItemIcon;
    public Sprite PropSprite;
    public Sprite FoodSprite;
    public Sprite EquipmentSprite;
    public Sprite MonsterTagSprite;
    public Sprite HumanTagSprite;
    public Sprite DetailIconSprite_Empty;
    public TextMeshProUGUI DetailNameText;
    public TextMeshProUGUI DetailDescText;
    public TextMeshProUGUI DetailPriceText;
    public Button CloseButton;
    public GameObject TagsPrefab;
    public Transform ItemTagCotainer;

    [Header("Buy Button (Detail Panel)")]
    public Button BuyButton;
    public Sprite BuyButtonSprite_CanBuy;
    public Sprite BuyButtonSprite_Buyed;

    [Header("SFX")]
    [SerializeField] private AudioClip buySuccessSfx;
    [SerializeField] private AudioClip buyFailedSfx;
    [SerializeField] private AudioClip itemClickSfx;
    [SerializeField] private AudioClip openPanelSfx;
    [SerializeField] private AudioClip closePanelSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolumeScale = 1f;

    void Awake()
    {
        if (BuyButton != null) BuyButton.onClick.AddListener(OnBuyButtonClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(OnCloseButtonClicked);
        SetDiscountItemIconVisible(false);
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
        UpdateDiscountItemIconVisible();
    }

    public override void ShowItems(List<ShelfSlot> items, Action<ShelfSlot> onBuyRequest)
    {
        _onBuyRequestCallback = onBuyRequest;
        _currentItems = items;
        AdjustSlotCount(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            _activeSlots[i].Setup(items[i], OnSlotSelected);
            _activeSlots[i].gameObject.SetActive(true);
        }

        for (int i = items.Count; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].gameObject.SetActive(false);
        }
    }

    public override void RefreshAll()
    {
        foreach (var slot in _activeSlots)
        {
            if (slot.gameObject.activeSelf) slot.RefreshView();
        }
        UpdateButtonState();
    }

    private void AdjustSlotCount(int targetCount)
    {
        while (_activeSlots.Count < targetCount)
        {
            ShopSlot newSlot = Instantiate(SlotPrefab, SlotContainer);
            _activeSlots.Add(newSlot);
        }
    }

    private void OnSlotSelected(ShopSlotBase selectedSlot)
    {
        PlaySfx(itemClickSfx);
        if (BuyButton != null) BuyButton.gameObject.SetActive(true);
        _currentSelectedData = selectedSlot._currentData;
        UpdateDetailPanel(selectedSlot);
    }

    private void UpdateDetailPanel(ShopSlotBase slotUI)
    {
        foreach (Transform child in ItemTagCotainer)
        {
            Destroy(child.gameObject);
        }
        if (DetailRoot != null) DetailRoot.SetActive(true);
        var data = slotUI._currentData;
        var ItemData = data.Item;

        if (DetailNameText != null) DetailNameText.text = ItemData.Name;
        if (DetailDescText != null) DetailDescText.text = ItemData.Description;
        if (DetailPriceText != null) DetailPriceText.text = $"${data.Price}";
        if (WorldIcon != null) WorldIcon.sprite = ItemData.World == ItemWorld.Human ? HumanTagSprite : MonsterTagSprite;
        if (TypeIcon != null) TypeIcon.sprite = ItemData.Type == ItemType.Prop ? PropSprite : ItemData.Type == ItemType.Food ? FoodSprite : EquipmentSprite;
        if (DetailIcon != null) DetailIcon.sprite = slotUI._targetImage.sprite;
        UpdateDiscountItemIconVisible();
        SpriteLoader.AdjustImageScale(DetailIcon, TargetLongEdgeSize);
        UpdateButtonState();
        string rarityId = ItemData.Rarity.ToString();
        SpriteLoader.LoadSpriteAsync(rarityId, sprite =>
        {
            if (RarityIcon == null) return;
            RarityIcon.sprite = sprite != null ? sprite : DetailIconSprite_Empty;
        });
        ShowTags(ItemData.Tags);
    }

    private void UpdateButtonState()
    {
        if (_currentSelectedData == null) return;
        if (BuyButton == null) return;

        bool isPurchased = _currentSelectedData.Purchased;
        BuyButton.interactable = !isPurchased;

        if (BuyButton.image != null)
        {
            BuyButton.image.sprite = isPurchased ? BuyButtonSprite_Buyed : BuyButtonSprite_CanBuy;
        }
    }

    private void ShowTags(List<string> tags)
    {
        if (tags == null || TagsPrefab == null || ItemTagCotainer == null) return;

        for (int i = 0; i < tags.Count; i++)
        {
            string tagId = tags[i];
            string tagName = DataManager.Instance.GetTagNameByTag(tagId);

            if (tagName != "")
            {
                GameObject newSlot = Instantiate(TagsPrefab, ItemTagCotainer);

                TextMeshProUGUI textComp = newSlot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                textComp.text = tagName;

                GameObject imgObj = new GameObject("TagImage");
                imgObj.transform.SetParent(newSlot.transform, false);
                Image tagImage = imgObj.AddComponent<Image>();
                imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                imgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 65);

                imgObj.SetActive(false);
                textComp.gameObject.SetActive(true);

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
    }

    private void ClearDetailPanel()
    {
        foreach (Transform child in ItemTagCotainer)
        {
            Destroy(child.gameObject);
        }
        if (BuyButton != null) BuyButton.gameObject.SetActive(false);
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
        WorldIcon.sprite = DetailIconSprite_Empty;
        TypeIcon.sprite = DetailIconSprite_Empty;
        DetailIcon.sprite = DetailIconSprite_Empty;
        RarityIcon.sprite = DetailIconSprite_Empty;
        _currentSelectedData = null;
    }

    private bool IsDiscountItem(ShelfSlot data)
    {
        if (data?.VisualInfo == null) return false;

        return data.VisualInfo.HasEffects
            || data.VisualInfo.IsDailySpecial
            || !string.IsNullOrEmpty(data.VisualInfo.DiscountLabel)
            || (data.Item != null && data.Price < data.Item.BasePrice);
    }

    private void UpdateDiscountItemIconVisible()
    {
        bool visible = PanelisVisible
            && _currentItems != null
            && _currentItems.Exists(IsDiscountItem);

        SetDiscountItemIconVisible(visible);
    }

    private void SetDiscountItemIconVisible(bool visible)
    {
        if (DiscountItemIcon == null) return;
        DiscountItemIcon.gameObject.SetActive(visible);
    }

    private void OnBuyButtonClicked()
    {
        if (_currentSelectedData != null && !_currentSelectedData.Purchased)
        {
            _onBuyRequestCallback?.Invoke(_currentSelectedData);
        }
        else
        {
            PlayBuyFailedSfx();
        }
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
