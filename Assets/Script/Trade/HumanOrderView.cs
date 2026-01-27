using UnityEngine;
using System;
using System.Linq;
using GameSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using Player;
using TMPro;

public class HumanOrderView : MonoBehaviour, IInteractable
{
    public GameObject Panel;
    public GameObject Prompt;
    //可提交背包
    public OrderBagSlot BagSlotPrefab;//背包欄位預製物件
    public OrderSlot OrderSlotPrefab;//訂單選擇預製物件
    public OrderSelectSlot OrderSelectSlotPrefab;//訂單提交物件預製物件
    public Transform OrderSlotContainer;//訂單選擇容器
    public Transform BagSlotContainer; // 生成 Slot 的父物件
    public Transform TagSlotContainer;//標籤容器
    public Image DetailIcon;//背包物品圖片
    public Image WorldIcon;
    public Image TypeIcon;
    public Sprite PropSprite;//道具
    public Sprite FoodSprite;//食物
    public Sprite EquipmentSprite;//裝備
    public Sprite MonsterTagSprite;//妖界
    public Sprite emptySprite;
    public TextMeshProUGUI DetailNameText;//背包物品名稱
    public TextMeshProUGUI DetailDescText;//背包物品描述
    public TextMeshProUGUI DetailPriceText;//背包物品購買成本
    public TextMeshProUGUI OrderSelectCountText;//目前選擇數量
    private List<BagSlot> _activeSlots = new List<BagSlot>();//背包列表
    private List<OrderSlot> _orderSlots = new List<OrderSlot>();//訂單選擇列表
    //交易組件
    public GameObject OrderFinishImage;
    public Transform OrderTagContainer;//訂單標籤容器
    public TextMeshProUGUI OrderNameText;//訂單名稱
    public TextMeshProUGUI OrderDescText;//訂單描述
    public TextMeshProUGUI OrderRewardText;//訂單獎勵
    public Button ExitButton;
    public Button Confirmbutton;
    //不符合類型物品
    public Transform OrderObjContainer;//訂單物件容器
    private List<BagSlot> _unmatchedSlots = new List<BagSlot>();//不符合類型物品列表
    //當前選擇訂單
    public event Action<OrderBagSlot> AddItemToOrder;
    public event Action<OrderBagSlot> OnOrderCancelSelected;
    public event Action<HumanLargeOrder> OnSelectedLargeOrder;
    public event Action<HumanSmallOrder> OnSelectedSmallOrder;
    public event Action OnOpenOrderPanel;
    public event Action OnConfirmOrder;

    private void Start()
    {
        Prompt.SetActive(false);
        Confirmbutton.onClick.AddListener(OnConfirmOrderClick);
        ExitButton.onClick.AddListener(ExitOrderPanel);
    }
    public void ShowPrompt()
    {
        Prompt.SetActive(true);
    }

    public void HidePrompt()
    {
        Prompt.SetActive(false);
    }

    public void Interact()
    {
        Panel.SetActive(!Panel.activeSelf);
        if(Panel.activeSelf)
        {
            ClearBagDetail();
            ClearOrderView();
            OnOpenOrderPanel?.Invoke();
        }
    }
    #region InventoryUIView
    public void ShowBagItems(List<Item> items)
    {
        // 0. 先隱藏所有舊的 Slot
        foreach (var slot in _activeSlots)
        {
            slot.gameObject.SetActive(false);
        }
        
        // 1. 確保 UI 數量足夠
        AdjustSlotCount(items.Count);
        // 2. 把資料填進去
        for (int i = 0; i < items.Count; i++)
        {
            // 將資料傳給 Slot，並把「購買請求」一路傳遞回去
            _activeSlots[i].Setup(items[i], OnBagSelected);
            _activeSlots[i].SetGrayscale(false); // 確保正常顏色
            _activeSlots[i].transform.SetSiblingIndex(i); // 確保排在最前面
            _activeSlots[i].gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 顯示不符合類型的物品（灰階顯示）
    /// </summary>
    public void ShowUnmatchedBagItems(List<Item> items)
    {
        // 0. 先隱藏所有舊的 Slot
        foreach (var slot in _unmatchedSlots)
        {
            slot.gameObject.SetActive(false);
        }
        
        // 1. 確保 UI 數量足夠
        AdjustUnmatchedSlotCount(items.Count);
        // 2. 把資料填進去並設為灰階
        for (int i = 0; i < items.Count; i++)
        {
            _unmatchedSlots[i].Setup(items[i], OnBagSelected);
            _unmatchedSlots[i].SetGrayscale(true); // 設為灰階
            _unmatchedSlots[i].transform.SetAsLastSibling(); // 確保排在最後面
            _unmatchedSlots[i].gameObject.SetActive(true);
        }
    }

    private void AdjustSlotCount(int targetCount)
    {
        while (_activeSlots.Count < targetCount)
        {
            OrderBagSlot newSlot = Instantiate(BagSlotPrefab, BagSlotContainer);
            _activeSlots.Add(newSlot);
        }
    }
    private void AdjustUnmatchedSlotCount(int targetCount)
    {
        while (_unmatchedSlots.Count < targetCount)
        {
            OrderBagSlot newSlot = Instantiate(BagSlotPrefab, BagSlotContainer);
            _unmatchedSlots.Add(newSlot);
        }
    }
    private void AdjustOrderSlotCount(int targetCount)
    {
        while (_orderSlots.Count < targetCount)
        {
            OrderSlot newSlot = Instantiate(OrderSlotPrefab, OrderSlotContainer);
            _orderSlots.Add(newSlot);
        }
    }
    private void AdjustImageScale(Image targetImage, int TargetLongEdgeSize)
    {
        if (targetImage == null || TargetLongEdgeSize <= 0) return;
        targetImage.SetNativeSize();
        RectTransform rt = targetImage.rectTransform;
        float width = rt.sizeDelta.x;
        float height = rt.sizeDelta.y;

        // 取得長邊
        float longEdge = Mathf.Max(width, height);
        if (longEdge <= 0) return;

        // 計算縮放倍數
        float scale = TargetLongEdgeSize / longEdge;

        // 調整尺寸
        rt.sizeDelta = new Vector2(width * scale, height * scale);
    }
    #endregion
    #region ClickEvent
    private void OnBagSelected(BagSlot slot)
    {
        //處理選中背包物品的邏輯
        DetailNameText.text = slot._currentDefinition.Name;
        DetailDescText.text = slot._currentDefinition.Description;
        DetailPriceText.text = slot._currentData.CostPrice.ToString();
        DetailIcon.sprite = slot._targetImage.sprite;
        AdjustImageScale(DetailIcon, 150);
        switch (slot._currentDefinition.Type)
        {
            case ItemType.Equipment:
                TypeIcon.sprite = EquipmentSprite;
                break;
            case ItemType.Food:
                TypeIcon.sprite = FoodSprite;
                break;
            case ItemType.Prop:
                TypeIcon.sprite = PropSprite;
                break;
            default:
                TypeIcon.sprite = PropSprite;
                break;
        }
        WorldIcon.sprite = MonsterTagSprite;
        if (slot is OrderBagSlot orderBagSlot)
        {
            AddItemToOrder?.Invoke(orderBagSlot);
        }
    }
    private void OnConfirmOrderClick()
    {
        OnConfirmOrder?.Invoke();
    }
    private void InvokeSelectedOrder(HumanLargeOrder order)
    {
        OnSelectedLargeOrder?.Invoke(order);
        UpdateOrderView(order);
    }
    private void InvokeSelectedOrder(HumanSmallOrder order)
    {
        OnSelectedSmallOrder?.Invoke(order);
        UpdateOrderView(order);
    }
    /// <summary>
    /// 顯示所有訂單（大訂單根據稀有度排序，小訂單在後面）
    /// </summary>
    public void ShowAllOrderSlots(List<HumanLargeOrder> largeOrders, List<HumanSmallOrder> smallOrders)
    {
        // 根據稀有度排序大訂單（降序：SuperRare > Rare > Common）
        var sortedLargeOrders = largeOrders.OrderByDescending(o => o.OrderRank).ToList();
        
        int totalCount = sortedLargeOrders.Count + smallOrders.Count;
        AdjustOrderSlotCount(totalCount);
        
        int slotIndex = 0;
        // 先顯示大訂單
        for (int i = 0; i < sortedLargeOrders.Count; i++)
        {
            _orderSlots[slotIndex].Setup(sortedLargeOrders[i], InvokeSelectedOrder);
            _orderSlots[slotIndex].gameObject.SetActive(true);
            AdjustImageScale(_orderSlots[slotIndex]._targetImage, 180);
            slotIndex++;
        }
        // 再顯示小訂單
        for (int i = 0; i < smallOrders.Count; i++)
        {
            _orderSlots[slotIndex].Setup(smallOrders[i], InvokeSelectedOrder);
            _orderSlots[slotIndex].gameObject.SetActive(true);
            AdjustImageScale(_orderSlots[slotIndex]._targetImage, 120);
            slotIndex++;
        }
        // 隱藏多餘的 Slot
        for (int i = totalCount; i < _orderSlots.Count; i++)
        {
            _orderSlots[i].gameObject.SetActive(false);
        }
    }
    #endregion
    #region UpdateView
    public void ClearBagDetail()
    {
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
        WorldIcon.sprite = emptySprite;
        TypeIcon.sprite = emptySprite;
        DetailIcon.sprite = emptySprite;
        foreach (var slot in OrderObjContainer.GetComponentsInChildren<OrderSelectSlot>())
        {
            Destroy(slot.gameObject);
        }
    }
    public void ClearOrderView()
    {
        OrderNameText.text = "";
        OrderDescText.text = "";
        OrderRewardText.text = "";
        OrderSelectCountText.text = "";
        OrderFinishImage.SetActive(false);
    }
    public void UpdateTradePrice(int price)
    {
        OrderRewardText.text = price.ToString();
    }
    public void UpdateOrderView(HumanLargeOrder order)
    {
        OrderNameText.text = order.OrderName;
        OrderDescText.text = order.OrderDescription;
    }
    public void UpdateOrderView(HumanSmallOrder order)
    {
        OrderNameText.text = order.OrderName;
        OrderDescText.text = order.OrderDescription;
    }
    public void UpdateOrderSelectCount(int count, int maxCount)
    {
        OrderSelectCountText.text = count.ToString() + "/" + maxCount.ToString();
        if (count >= 1)
        {
            Confirmbutton.gameObject.SetActive(true);
        }
        else
        {
            OrderRewardText.text = "";
            Confirmbutton.gameObject.SetActive(false);
        }
    }
    public void ShowOrderFinish()
    {
        OrderFinishImage.SetActive(true);
    }
    public void NewSelectItem(OrderBagSlot slot)
    {
        slot.SetGrayscale(true);
        OrderSelectSlot newSlot = Instantiate(OrderSelectSlotPrefab, OrderObjContainer);
        newSlot.Setup(slot, CancelSelected);
        slot.SetOrderSelect(newSlot);
    }
    private void CancelSelected(OrderBagSlot slot)
    {
        OnOrderCancelSelected?.Invoke(slot);
        slot.SetOnSelected(false);
        slot.RemoveOrderSelect();
        
    }
    private void ExitOrderPanel()
    {
        ClearBagDetail();
        ClearOrderView();
        Panel.SetActive(false);
    }
    #endregion
}
