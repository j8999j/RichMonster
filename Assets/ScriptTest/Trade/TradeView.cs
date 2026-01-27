using UnityEngine;
using System;
using GameSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Runtime.ConstrainedExecution;
using Unity.VisualScripting;
using TMPro;
public class TradeView : MonoBehaviour
{
    private GameObject TradeUI;
    private Guest currentGuest;
    //TradeBag
    [Header("背包組件")]
    [SerializeField] private GameObject TradeBag;//交易背包根物件
    private List<Item> bagItemsList;
    public BagSlot TradeSlotPrefab;//背包欄位預製物件
    public Transform SlotContainer; // 生成 Slot 的父物件
    public Transform TagSlotContainer;//標籤容器
    public Image DetailIcon;//背包物品圖片
    public TextMeshProUGUI DetailNameText;//背包物品名稱
    public TextMeshProUGUI DetailDescText;//背包物品描述
    public TextMeshProUGUI DetailPriceText;//背包物品購買成本
    public Image QualityImage;//背包物品品質
    public Sprite GoodQuality;//優品質
    public Sprite NormalQuality;//普通品質
    public Sprite BadQuality;//差品質
    private List<BagSlot> _activeSlots = new List<BagSlot>();//背包列表
    //TradeData
    private Item OnSelectItem;
    private ItemQuality OnSelectQuality;
    private int PatienceMax;
    private int PatienceNow;
    private int NowPrice;
    public Item SelectedItem => OnSelectItem;
    //TradeUI
    [Header("交易組件")]
    [SerializeField] private GameObject TradeModeUI;//交易組件根物件
    [SerializeField] private GameObject QualityButtonUI;//品質按鈕根物件
    [SerializeField] private GameObject SliderUI;//價格條根物件
    [SerializeField] private GameObject PatienceUI;//耐心物件
    [SerializeField] private GameObject HavePatiencePrefab;//有耐心
    [SerializeField] private GameObject NoPatiencePrefab;//沒耐心
    [SerializeField] private Button OnOpenShopButton;//切換階段開始交易
    [SerializeField] private Button ConfirmItemButton;//確定提出物品
    [SerializeField] private Button ConfirmPriceButton;//確認議價
    [SerializeField] private TradeSlider PriceSlider;//調整議價用的條
    [SerializeField] private Button GoodQualityButton;//提出品質:優良
    [SerializeField] private Button NormalQualityButton;//提出品質:普通
    [SerializeField] private Button BadQualityButton;//提出品質:差
    [SerializeField] private Image PreferImage;//偏好圖片
    [SerializeField] private Sprite PreferSprite;
    [SerializeField] private Sprite NotPreferSprite;
    [SerializeField] private Sprite NoneSprite;
    [SerializeField] private Image CustomerImage;//客人圖片
    [SerializeField] private TextMeshProUGUI MarketPriceText;
    [SerializeField] private TextMeshProUGUI TradePriceText;

    // ======= Events to Presenter =======
    public event Action OnOpenShop;//開始營業
    public event Action CheckItem;//確認商品是否符合需求
    public event Action<Item, ItemQuality> OnItemSelected;//計算市價
    public event Action<Item, ItemQuality> TradeItems;//提出商品/品質
    public event Action<int> SliderPrice;//計算拖曳Slider價格
    public event Action<int> TradePrice;//提出價格
    void Start()
    {
        OnOpenShopButton.onClick.AddListener(InvokeOnOpenButton);
        GoodQualityButton.onClick.AddListener(() => OnSelectQualityButton(ItemQuality.Good));
        NormalQualityButton.onClick.AddListener(() => OnSelectQualityButton(ItemQuality.Normal));
        BadQualityButton.onClick.AddListener(() => OnSelectQualityButton(ItemQuality.Bad));
        ConfirmPriceButton.onClick.AddListener(() => SetNowPrice(NowPrice));

    }
    void OnEnable()
    {
        PriceSlider.OnStageChanged += InvokeSliderPrice;
    }
    void OnDisable()
    {
        PriceSlider.OnStageChanged -= InvokeSliderPrice;
    }
    void OpenUI()
    {
        TradeUI.SetActive(!TradeUI.activeSelf);
    }
    #region InventoryUIView
    public void ShowBagItems(List<Item> items)
    {
        // 1. 確保 UI 數量足夠
        AdjustSlotCount(items.Count);
        // 2. 把資料填進去
        for (int i = 0; i < items.Count; i++)
        {
            // 將資料傳給 Slot，並把「購買請求」一路傳遞回去
            _activeSlots[i].Setup(items[i], OnTradeSelected);
            _activeSlots[i].gameObject.SetActive(true);
        }

        // 3. 隱藏多餘的 Slot
        for (int i = items.Count; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].gameObject.SetActive(false);
        }
    }
    private void AdjustSlotCount(int targetCount)
    {
        while (_activeSlots.Count < targetCount)
        {
            BagSlot newSlot = Instantiate(TradeSlotPrefab, SlotContainer);
            _activeSlots.Add(newSlot);
        }
    }
    #endregion
    #region UpdataView
    public void UpdateGuestInfo(Guest guest, TradeProgress tradeProgress)
    {
        currentGuest = guest;
        PatienceMax = guest.customer.Patience;
        if (tradeProgress.Patience <= 0)
        {
            PatienceNow = PatienceMax;
        }
        else
        {
            PatienceNow = tradeProgress.Patience;
        }
        UpdatePatienceUI();
    }
    public void UpdateTradeInfo(TradeProgress tradeProgress, Guest guest, List<Item> bagItems, bool OnSelect)//提出商品時的UI顯示
    {
        OnSelectQuality = ItemQuality.None;
        OnSelectItem = null;
        //調整階段
        if (OnSelect)
        {
            bagItemsList = bagItems;
            SetSelectTradeUI();
        }
        else
        {
            SetTradePriceUI();
        }
        //顯示客人
        UpdateGuestInfo(guest, tradeProgress);
    }
    public void UpdatePatienceUI()
    {
        foreach (Transform child in PatienceUI.transform)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < PatienceNow; i++)
        {
            Instantiate(HavePatiencePrefab, PatienceUI.transform);
        }
        for (int i = 0; i < PatienceMax - PatienceNow; i++)
        {
            Instantiate(NoPatiencePrefab, PatienceUI.transform);
        }
    }
    public void UpdateMarketPrice(int price)//設定市場價
    {
        MarketPriceText.text = price.ToString();
    }
    public void SetSelectTradeUI()//設定開始時的UI顯示
    {
        ResetButtonView();
        ResetPriceView();
        ClearBagImage();
        ClearImage();
        ShowBagItems(bagItemsList);
        TradeModeUI.SetActive(true);
        QualityButtonUI.SetActive(true);
        TradeBag.SetActive(true);
        ConfirmItemButton.gameObject.SetActive(true);
        SliderUI.SetActive(false);
        ConfirmPriceButton.gameObject.SetActive(false);
    }
    public void SetTradePriceUI()//設定議價中的UI顯示
    {
        QualityButtonUI.SetActive(false);
        TradeBag.SetActive(false);
        ConfirmItemButton.gameObject.SetActive(false);
        SliderUI.SetActive(true);
        ConfirmPriceButton.gameObject.SetActive(true);
        SetPriceSliderPos(0);
    }

    #endregion
    #region TradeUIView
    private void OnTradeSelected(BagSlot bagSlot)
    {
        OnSelectQuality = ItemQuality.None;
        OnSelectItem = bagSlot._currentData;
        SetTradeItem();
        ResetPriceView();
        ResetButtonView();
        CheckItem?.Invoke();
        DetailNameText.text = bagSlot._currentDefinition.Name;
        DetailDescText.text = bagSlot._currentDefinition.Description;
        DetailPriceText.text = bagSlot._currentData.CostPrice.ToString();
        DetailIcon.sprite = bagSlot._targetImage.sprite;
    }
    public void OnSelectQualityButton(ItemQuality itemQuality)
    {
        if (OnSelectItem == null)
            return;
        ResetButtonView();
        SetQualityButton(itemQuality);
        OnSelectQuality = itemQuality;
        OnItemSelected?.Invoke(OnSelectItem, OnSelectQuality);

    }
    public void ConfirmSelectItem()
    {
        if (OnSelectItem == null || OnSelectQuality == ItemQuality.None)
            return;
        SetTradePriceUI();

    }
    private void InvokeSliderPrice(int Stage)
    {
        SliderPrice?.Invoke(Stage);
    }
    public void SetPriceView(int CurrentPrice)//顯示出價
    {
        TradePriceText.text = CurrentPrice.ToString();
        SetNowPrice(CurrentPrice);
    }
    private void SetQualityButton(ItemQuality itemQuality)
    {
        switch (itemQuality)
        {
            case ItemQuality.Good:
                GoodQualityButton.image.color = Color.gray;
                break;
            case ItemQuality.Normal:
                NormalQualityButton.image.color = Color.gray;
                break;
            case ItemQuality.Bad:
                BadQualityButton.image.color = Color.gray;
                break;
        }
    }
    public void ClearBagImage()
    {
        DetailIcon.sprite = NoneSprite;
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
        QualityImage.sprite = NoneSprite;
    }
    public void ClearImage()//清空圖片
    {
        PreferImage.sprite = NoneSprite;
        DetailIcon.sprite = NoneSprite;
        CustomerImage.sprite = NoneSprite;
    }
    public void SetPreferImage(bool isSatisfied)
    {
        PreferImage.sprite = isSatisfied ? PreferSprite : NotPreferSprite;
    }
    public void SetPriceSliderPos(int stage)
    {
        PriceSlider.SetStage(stage);
    }
    private void ResetPriceView()
    {
        MarketPriceText.text = "??????";
        TradePriceText.text = "??????";
    }
    private void ResetButtonView()
    {
        GoodQualityButton.image.color = Color.white;
        NormalQualityButton.image.color = Color.white;
        BadQualityButton.image.color = Color.white;
    }
    #endregion
    #region SetUIButtonData
    public void SetTradeItem()
    {
        ConfirmItemButton.onClick.RemoveAllListeners();
        ConfirmItemButton.onClick.AddListener(ConfirmSelectItem);
        ConfirmItemButton.onClick.AddListener(() => InvokeTradeItems(OnSelectItem, OnSelectQuality));
    }
    public void SetNowPrice(int Price)
    {
        ConfirmPriceButton.onClick.RemoveAllListeners();
        ConfirmPriceButton.onClick.AddListener(() => InvokeTradePrice(Price));
    }
    #endregion
    #region ButtonMethon
    private void StartTradeUI()
    {
        OnOpenShopButton.gameObject.SetActive(false);
        SetSelectTradeUI();
    }
    #endregion
    #region ButtonEvent
    private void InvokeOnOpenButton()
    {
        OnOpenShop?.Invoke();
        StartTradeUI();
    }
    private void InvokeTradeItems(Item item, ItemQuality itemQuality)
    {
        TradeItems?.Invoke(item, itemQuality);
    }
    private void InvokeTradePrice(int price)
    {
        TradePrice?.Invoke(price);
    }
    #endregion
}