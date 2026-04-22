using UnityEngine;
using System;
using System.Linq;
using GameSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using Player;
using TMPro;

public class HumanOrderView : MonoBehaviour, IGuideInteractable
{
    [Header("面板")]
    public GameObject Panel;
    public GameObject Prompt;
    public GameObject CheckSwitchToAfterNoonPanel;
    public string ID => GuideIDs.Interactable.GuideOrderShop;

    [Header("背包UI")]
    public OrderBagSlot BagSlotPrefab;//背包欄位預製物件
    public ScrollRect BagscrollRect;
    public Transform BagSlotContainer;//生成 Slot 的父物件
    public Transform TagSlotContainer;//標籤容器
    public Transform OrderObjContainer;//訂單物件容器

    [Header("背包物品詳情")]
    public Image DetailIcon;//背包物品圖片
    public Image WorldIcon;
    public Image TypeIcon;
    public TextMeshProUGUI DetailNameText;//背包物品名稱
    public TextMeshProUGUI DetailDescText;//背包物品描述
    public TextMeshProUGUI DetailPriceText;//背包物品購買成本

    [Header("訂單選擇")]
    public OrderSlot OrderSlotPrefab;//訂單選擇預製物件
    public OrderSelectSlot OrderSelectSlotPrefab;//訂單提交物件預製物件
    public Transform OrderSlotContainer;//訂單選擇容器
    public TextMeshProUGUI OrderSelectCountText;//目前選擇數量

    [Header("訂單詳情")]
    public GameObject OrderFinishImage;
    public Transform OrderTagContainer;//訂單標籤容器
    public GameObject TagsPrefab;//標籤預製物件
    public Image OrderImage;//訂單圖片
    public Image OrderTypeIcon;//訂單需求類型圖示
    public TextMeshProUGUI OrderNameText;//訂單名稱
    public TextMeshProUGUI OrderDescText;//訂單描述
    public TextMeshProUGUI OrderRewardText;//訂單獎勵

    [Header("共用圖片資源")]
    public Sprite PropSprite;//道具
    public Sprite FoodSprite;//食物
    public Sprite EquipmentSprite;//裝備
    public Sprite MonsterTagSprite;//妖界
    public Sprite emptySprite;

    [Header("按鈕")]
    public Button ExitButton;
    public Button Confirmbutton;
    public Button OpenAfterNoonPanelButton;
    public Button ConfirmAfterNoonButton;

    [Header("二次確認面板")]
    [SerializeField] private TextMeshProUGUI confirmAfterNoonText;
    [SerializeField] private string defaultConfirmMessage = "確定要休息嗎？";
    private bool _alreadyRested = false;

    [Header("其他")]
    [SerializeField] private Transform GuideTransform;
    [SerializeField] private GridLayoutGroup LayoutGroupItem;
    [SerializeField] private GridLayoutGroup LayoutGroupGrid;
    private List<BagSlot> _activeSlots = new List<BagSlot>();//背包列表
    private List<OrderSlot> _orderSlots = new List<OrderSlot>();//訂單選擇列表
    private List<BagSlot> _unmatchedSlots = new List<BagSlot>();//不符合類型物品列表

    public event Action<OrderBagSlot> AddItemToOrder;
    public event Action<OrderBagSlot> OnOrderCancelSelected;
    public event Action<HumanLargeOrder> OnSelectedLargeOrder;
    public event Action<HumanSmallOrder> OnSelectedSmallOrder;
    public event Action OnOpenOrderPanel;
    public event Action OnConfirmOrder;
    public event Action SwitchToAfterNoonClick;
    public event Action<string> OnInteracted;
    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID, GuideTransform);
    }
    public void OnEnable()
    {
        GuideLookupRegistry.Instance.RegisterInteractable(this);
    }
    public void OnDisable()
    {
        GuideLookupRegistry.Instance.UnregisterInteractable(this);
    }
    private void Start()
    {
        SetMapGuide();
        Prompt.SetActive(false);
        Confirmbutton.onClick.AddListener(OnConfirmOrderClick);
        ExitButton.onClick.AddListener(ExitOrderPanel);
        OpenAfterNoonPanelButton.onClick.AddListener(OpenAfterNoonPanel);
        ConfirmAfterNoonButton.onClick.AddListener(ConfirmSwitchToAfterNoon);
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
        OnInteracted?.Invoke(ID);
        Panel.SetActive(!Panel.activeSelf);

        if (Panel.activeSelf)
        {
            GameManager.Instance.LockPlayerMove("HumanOrderView");
            ClearBagDetail();
            ClearOrderView();
            OnOpenOrderPanel?.Invoke();
        }
        else
        {
            GameManager.Instance.UnlockPlayerMove("HumanOrderView");
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

    #endregion
    #region ClickEvent
    private void OnBagSelected(BagSlot slot)
    {
        //處理選中背包物品的邏輯
        DetailNameText.text = slot._currentDefinition.Name;
        DetailDescText.text = slot._currentDefinition.Description;
        DetailPriceText.text = slot._currentData.CostPrice.ToString();
        DetailIcon.sprite = slot._targetImage.sprite;
        SpriteLoader.AdjustImageScale(DetailIcon, 120);
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
        ShowBagItemTags(slot._currentDefinition.Tags);
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
        BagscrollRect.verticalNormalizedPosition = 1f;
    }
    private void InvokeSelectedOrder(HumanSmallOrder order)
    {
        OnSelectedSmallOrder?.Invoke(order);
        UpdateOrderView(order);
        BagscrollRect.verticalNormalizedPosition = 1f;
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
            SpriteLoader.AdjustImageScale(_orderSlots[slotIndex]._targetImage, 120);
            slotIndex++;
        }
        // 再顯示小訂單
        for (int i = 0; i < smallOrders.Count; i++)
        {
            _orderSlots[slotIndex].Setup(smallOrders[i], InvokeSelectedOrder);
            _orderSlots[slotIndex].gameObject.SetActive(true);
            SpriteLoader.AdjustImageScale(_orderSlots[slotIndex]._targetImage, 120);
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
    void ClearOnSelectGrid()
    {
        foreach (Transform child in LayoutGroupGrid.gameObject.transform)
        {
            child.gameObject.SetActive(false);
        }
    }
    public void ClearBagDetail()
    {
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
        WorldIcon.sprite = emptySprite;
        TypeIcon.sprite = emptySprite;
        DetailIcon.sprite = emptySprite;
        if (TagSlotContainer != null)
        {
            foreach (Transform child in TagSlotContainer)
            {
                Destroy(child.gameObject);
            }
        }
        foreach (var slot in OrderObjContainer.GetComponentsInChildren<OrderSelectSlot>())
        {
            Destroy(slot.gameObject);
        }
    }
    public void ClearOrderView()
    {
        ClearOnSelectGrid();
        OrderNameText.text = "";
        OrderDescText.text = "";
        OrderRewardText.text = "";
        OrderSelectCountText.text = "";
        OrderFinishImage.SetActive(false);
        Confirmbutton.gameObject.SetActive(false);
        if (OrderImage != null) OrderImage.sprite = emptySprite;
        if (OrderTypeIcon != null) OrderTypeIcon.sprite = emptySprite;
        if (OrderTagContainer != null)
        {
            foreach (Transform child in OrderTagContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
    public void UpdateTradePrice(int price)
    {
        OrderRewardText.text = price.ToString();
    }
    public void UpdateOrderView(HumanLargeOrder order)
    {
        UpdataOnSelectGridPos(true);
        OrderNameText.text = order.OrderName;
        OrderDescText.text = order.OrderDescription;
        LoadOrderImage(order.OrderId);
        SetOrderTypeIcon(order.OrderType);
        ShowOrderTags(order.OrderNeedTags);

    }
    public void UpdateOrderView(HumanSmallOrder order)
    {
        UpdataOnSelectGridPos(false);
        OrderNameText.text = order.OrderName;
        OrderDescText.text = order.OrderDescription;
        LoadOrderImage(order.OrderId);
        SetOrderTypeIcon(order.OrderType);
        ShowOrderTags(order.OrderNeedTags);
    }
    //根據目前訂單類型顯示與調整放置UI的方法
    public void UpdataOnSelectGridPos(bool IsBigOrder)
    {
        if (IsBigOrder)
        {
            LayoutGroupGrid.childAlignment = TextAnchor.UpperLeft;
            LayoutGroupItem.childAlignment = TextAnchor.UpperLeft;
            foreach (Transform child in LayoutGroupGrid.gameObject.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
        else
        {
            LayoutGroupGrid.childAlignment = TextAnchor.UpperCenter;
            LayoutGroupItem.childAlignment = TextAnchor.UpperCenter;
            foreach (Transform child in LayoutGroupGrid.gameObject.transform)
            {
                child.gameObject.SetActive(false);
            }
            LayoutGroupGrid.gameObject.transform.GetChild(0).gameObject.SetActive(true);

        }

    }
    private void LoadOrderImage(string orderId)
    {
        if (OrderImage == null) return;
        OrderImage.sprite = emptySprite;
        SpriteLoader.LoadSpriteAsync(orderId, sprite =>
        {
            if (sprite != null)
            {
                OrderImage.sprite = sprite;
                SpriteLoader.AdjustImageScale(OrderImage, 150);
            }
        });
    }
    private void SetOrderTypeIcon(ItemType orderType)
    {
        if (OrderTypeIcon == null) return;
        switch (orderType)
        {
            case ItemType.Equipment:
                OrderTypeIcon.sprite = EquipmentSprite;
                break;
            case ItemType.Food:
                OrderTypeIcon.sprite = FoodSprite;
                break;
            case ItemType.Prop:
                OrderTypeIcon.sprite = PropSprite;
                break;
            default:
                OrderTypeIcon.sprite = PropSprite;
                break;
        }
    }
    private void ShowBagItemTags(List<string> tags)
    {
        if (TagSlotContainer == null || TagsPrefab == null || tags == null) return;
        // 清除舊標籤
        foreach (Transform child in TagSlotContainer)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < tags.Count; i++)
        {
            string tagId = tags[i];
            string tagName = DataManager.Instance.GetTagNameByTag(tagId);
            if (tagName != "")
            {
                GameObject newSlot = Instantiate(TagsPrefab, TagSlotContainer);
                TextMeshProUGUI textComp = newSlot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                textComp.text = tagName;

                GameObject imgObj = new GameObject("TagImage");
                imgObj.transform.SetParent(newSlot.transform, false);
                Image tagImage = imgObj.AddComponent<Image>();
                imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                imgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 100);

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
                        SpriteLoader.AdjustImageScale(capturedImage, 120);
                        capturedImgObj.SetActive(true);
                        capturedText.gameObject.SetActive(false);
                    }
                });
            }
        }
    }
    private void ShowOrderTags(List<string> tags)
    {
        if (OrderTagContainer == null || TagsPrefab == null || tags == null) return;
        // 清除舊標籤
        foreach (Transform child in OrderTagContainer)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < tags.Count; i++)
        {
            string tagId = tags[i];
            string tagName = DataManager.Instance.GetTagNameByTag(tagId);
            if (tagName != "")
            {
                GameObject newSlot = Instantiate(TagsPrefab, OrderTagContainer);
                TextMeshProUGUI textComp = newSlot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                textComp.text = tagName;

                // 建立Tag圖片物件
                GameObject imgObj = new GameObject("TagImage");
                imgObj.transform.SetParent(newSlot.transform, false);
                Image tagImage = imgObj.AddComponent<Image>();
                imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                imgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 100);

                // 預設隱藏圖片，顯示文字
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
                        SpriteLoader.AdjustImageScale(capturedImage, 120);
                        capturedImgObj.SetActive(true);
                        capturedText.gameObject.SetActive(false);
                    }
                });
            }
        }
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
        GameManager.Instance.UnlockPlayerMove("HumanOrderView");
        ClearBagDetail();
        ClearOrderView();
        Panel.SetActive(false);
    }
    private void OpenAfterNoonPanel()
    {
        // 檢查是否已經切換過下午階段
        _alreadyRested = DataManager.Instance.CurrentPlayerData.PlayingStatus == DayPhase.AfterNoon;

        if (confirmAfterNoonText != null)
        {
            confirmAfterNoonText.text = _alreadyRested ? "今日已經休息過了" : defaultConfirmMessage;
        }

        CheckSwitchToAfterNoonPanel.SetActive(true);
    }
    private void ConfirmSwitchToAfterNoon()
    {
        CheckSwitchToAfterNoonPanel.SetActive(false);

        // 已經休息過則僅關閉面板，不觸發切換
        if (_alreadyRested)
        {
            return;
        }

        GameManager.Instance.UnlockPlayerMove("HumanOrderView");
        Panel.SetActive(false);
        SwitchToAfterNoonClick?.Invoke();
    }
    #endregion
}
