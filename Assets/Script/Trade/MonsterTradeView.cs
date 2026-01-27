using UnityEngine;
using System;
using GameSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class MonsterTradeView : MonoBehaviour
{
    public GameObject TradeUI;
    //TradeBag
    [Header("背包組件")]
    [SerializeField] private GameObject TradeBag;//交易背包根物件
    private List<Item> bagItemsList;
    public TradeSlot TradeSlotPrefab;//背包欄位預製物件
    public Transform SlotContainer; // 生成 Slot 的父物件
    public Transform TagSlotContainer;//標籤容器
    public Image DetailIcon;//背包物品圖片
    public TextMeshProUGUI DetailNameText;//背包物品名稱
    public TextMeshProUGUI DetailDescText;//背包物品描述
    public TextMeshProUGUI DetailPriceText;//背包物品購買成本
    private List<TradeSlot> _activeSlots = new List<TradeSlot>();//背包列表
    //TradeData
    private Item OnSelectItem;
    private ItemQuality OnSelectQuality;
    public Item SelectedItem => OnSelectItem;
    //TradeUI
    [Header("交易組件")]
    [SerializeField] private GameObject TradeModeUI;//交易組件根物件
    [SerializeField] private Button OnOpenShopButton;//切換階段開始交易
    [SerializeField] private Image PreferImage;//偏好圖片
    [SerializeField] private Sprite PreferSprite;
    [SerializeField] private Sprite NotPreferSprite;
    [SerializeField] private Sprite NoneSprite;
    [SerializeField] private Image CustomerImage;//客人圖片
    public TextMeshProUGUI CustomerDialogText;//客人對話
    [Header("拖曳放置區域")]
    [SerializeField] private RectTransform TradeDropZone;//交易放置區域
    // ======= Events to Presenter =======
    public event Action OnOpenShop;//開始營業
    public event Action<Item> TradeItems;//點擊商品
    public event Action<Item> TradePrice;//提交商品
    void Start()
    {
        OnOpenShopButton.onClick.AddListener(InvokeOnOpenButton);
    }
    public void OpenShopUI()
    {
        TradeUI.SetActive(true);
    }
    public void ExitShopUI()
    {
        TradeUI.SetActive(false);
    }
    #region InventoryUIView
    public void ShowBagItems(List<Item> items)
    {
        // 過濾只顯示人類世界的物品
        var humanWorldItems = items.FindAll(item =>
        {
            var definition = DataManager.Instance.GetItemById(item.ItemId);
            return definition != null && definition.World == ItemWorld.Human;
        });

        // 1. 確保 UI 數量足夠
        AdjustSlotCount(humanWorldItems.Count);
        // 2. 把資料填進去
        for (int i = 0; i < humanWorldItems.Count; i++)
        {
            // 將資料傳給 Slot，並把「購買請求」一路傳遞回去
            _activeSlots[i].Setup(humanWorldItems[i], OnTradeSelected);
            _activeSlots[i].gameObject.SetActive(true);
        }

        // 3. 隱藏多餘的 Slot
        for (int i = humanWorldItems.Count; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].gameObject.SetActive(false);
        }
    }
    private void AdjustSlotCount(int targetCount)
    {
        while (_activeSlots.Count < targetCount)
        {
            TradeSlot newSlot = Instantiate(TradeSlotPrefab, SlotContainer);
            // 訂閱拖曳結束事件
            newSlot.OnDragEnded += OnEndTradeDrag;
            _activeSlots.Add(newSlot);
        }
    }
    #endregion
    #region UpdataView
    public void UpdateGuestInfo(MonsterGuest guest)
    {
        // 根據顧客職業 ID 載入圖片
        SpriteLoader.LoadSpriteAsync(guest.monsterCustomer.Profession, sprite =>
        {
            if (sprite != null)
            {
                CustomerImage.sprite = sprite;
                CustomerImage.SetNativeSize();
            }
        });
    }
    public void UpdateDialog(string dialog)
    {
        CustomerDialogText.text = dialog;
    }
    public void UpdateTradeInfo(MonsterGuest guest, List<Item> bagItems)//更新客人與背包資訊
    {
        //更新背包列表
        bagItemsList = bagItems;
        //刷新背包顯示
        ShowBagItems(bagItemsList);
        //顯示客人
        UpdateGuestInfo(guest);
    }
    public void SetSelectTradeUI()//設定開始時的UI顯示
    {
        ClearBagImage();
        ClearImage();
        ShowBagItems(bagItemsList);
        TradeModeUI.SetActive(true);
        TradeBag.SetActive(true);
    }
    public void SetTradePriceUI()//設定議價中的UI顯示
    {
        TradeBag.SetActive(false);
    }

    #endregion
    #region TradeUIView
    private void OnTradeSelected(BagSlot bagSlot)
    {
        OnSelectQuality = ItemQuality.None;
        OnSelectItem = bagSlot._currentData;
        InvokeTradeItems(bagSlot._currentData);
        DetailNameText.text = bagSlot._currentDefinition.Name;
        DetailDescText.text = bagSlot._currentDefinition.Description;
        DetailPriceText.text = bagSlot._currentData.CostPrice.ToString();
        DetailIcon.sprite = bagSlot._targetImage.sprite;
        DetailIcon.SetNativeSize();
    }
    private void OnEndTradeDrag(TradeSlot slot, PointerEventData eventData)
    {
        if (OnSelectItem == null)
            return;
        // 檢測是否在放置區域內
        if (IsPointerInsideDropZone(eventData))
        {
            TradePrice?.Invoke(OnSelectItem);
        }
    }

    /// <summary>
    /// 檢測滑鼠位置是否在放置區域內
    /// </summary>
    private bool IsPointerInsideDropZone(PointerEventData eventData)
    {
        if (TradeDropZone == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            TradeDropZone,
            eventData.position,
            eventData.pressEventCamera
        );
    }
    public void ConfirmSelectItem()
    {
        if (OnSelectItem == null || OnSelectQuality == ItemQuality.None)
            return;
        SetTradePriceUI();
    }
    public void EndTradeMode()
    {
        TradeUI.SetActive(false);
    }
    public void ClearBagImage()
    {
        DetailIcon.sprite = NoneSprite;
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
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
    private void InvokeTradeItems(Item item)
    {
        TradeItems?.Invoke(item);
    }
    #endregion
}