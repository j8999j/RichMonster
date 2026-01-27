using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Shop;
using System;

public class ShopUIView : MonoBehaviour
{
    public GameObject PanelRoot; // 整個 UI 的根節點
    public GameObject ShopShelfUI; // 商店貨架UI
    public Transform SlotContainer; // 生成 Slot 的父物件
    public ShopSlot SlotPrefab;     // Slot 的 Prefab
    public int TargetLongEdgeSize = 100;//顯示最大邊長限制

    private List<ShopSlot> _activeSlots = new List<ShopSlot>();
    [Header("Detail Panel (詳細資訊區)")]
    public GameObject DetailRoot;       // 詳細資訊的根物件 (沒選東西時可隱藏)
    public Image DetailIcon;            // 商品圖片
    public Image WorldIcon;            // 分界圖片
    public Image TypeIcon;            // 類型圖片
    public Sprite PropSprite;//道具
    public Sprite FoodSprite;//食物
    public Sprite EquipmentSprite;//裝備
    public Sprite MonsterTagSprite;//妖界
    public Sprite HumanTagSprite;//妖界
    public Sprite DetailIconSprite_Empty;
    public TextMeshProUGUI DetailNameText;         // 商品名稱
    public TextMeshProUGUI DetailDescText;         // 商品描述
    public TextMeshProUGUI DetailPriceText;        // 商品價格
    public Button BuyButton;            // 購買按鈕
    public TextMeshProUGUI BuyButtonText;          // 按鈕文字 (例如顯示 "已購買" 或 "購買")
    public Button CloseButton;// 關閉按鈕

    private ShelfSlot _currentSelectedData; // 目前選中的資料
    public event Action OnCloseShopUI;
    private Action<ShelfSlot> _onBuyRequestCallback; // 來自 Presenter 的購買邏輯

    void Awake()
    {
        // 綁定購買按鈕事件
        BuyButton.onClick.AddListener(OnBuyButtonClicked);
        CloseButton.onClick.AddListener(OnCloseButtonClicked);
    }

    // 開關介面
    public void SetVisible(bool isVisible)
    {
        ClearDetailPanel();
        PanelRoot.SetActive(isVisible);
        ShopShelfUI.SetActive(isVisible);
        DetailRoot.SetActive(isVisible);
        CloseButton.gameObject.SetActive(isVisible);
    }

    public bool IsVisible => PanelRoot.activeSelf;

    // 接收來自 Presenter (Store) 的資料列表
    public void ShowItems(List<ShelfSlot> items, Action<ShelfSlot> onBuyRequest)
    {
        _onBuyRequestCallback = onBuyRequest;
        // 1. 確保 UI 數量足夠 (Object Pooling 的簡易版概念)
        AdjustSlotCount(items.Count);

        // 2. 把資料填進去
        for (int i = 0; i < items.Count; i++)
        {
            // 將資料傳給 Slot，並把「購買請求」一路傳遞回去
            _activeSlots[i].Setup(items[i], OnSlotSelected);
            _activeSlots[i].gameObject.SetActive(true);
        }

        // 3. 隱藏多餘的 Slot
        for (int i = items.Count; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].gameObject.SetActive(false);
        }
    }

    // 刷新單一格子的顯示 (購買後呼叫)
    public void RefreshAll()
    {
        //刷新格子
        foreach (var slot in _activeSlots)
        {
            if (slot.gameObject.activeSelf) slot.RefreshView();
        }
        //刷新詳情
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
    private void AdjustImageScale(Image targetImage)
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
    #region View & Buy
    //選定物品
    private void OnSlotSelected(ShopSlot selectedSlot)
    {
        BuyButton.gameObject.SetActive(true);
        _currentSelectedData = selectedSlot._currentData;
        UpdateDetailPanel(selectedSlot);
    }
    // 3. 更新 UI
    private void UpdateDetailPanel(ShopSlot slotUI)
    {
        if (DetailRoot != null) DetailRoot.SetActive(true);
        var data = slotUI._currentData;

        // 更新文字
        if (DetailNameText != null) DetailNameText.text = data.Item.Name;
        // 假設 ItemDefinition 有 Description 欄位
        if (DetailDescText != null) DetailDescText.text = data.Item.Description;
        if (DetailPriceText != null) DetailPriceText.text = $"${data.Price}";
        //更新分界
        if (WorldIcon != null) WorldIcon.sprite = data.Item.World == ItemWorld.Human ? HumanTagSprite : MonsterTagSprite;
        //更新類型
        if (TypeIcon != null) TypeIcon.sprite = data.Item.Type == ItemType.Prop ? PropSprite : data.Item.Type == ItemType.Food ? FoodSprite : EquipmentSprite;
        // 更新圖片 (直接拿 Slot 已經載好的圖，省效能)
        if (DetailIcon != null) DetailIcon.sprite = slotUI._targetImage.sprite;
        //調整圖片大小
        AdjustImageScale(DetailIcon);
        // 更新按鈕狀態
        UpdateButtonState();
    }
    // 更新按鈕狀態
    private void UpdateButtonState()
    {
        if (_currentSelectedData == null) return;

        bool isPurchased = _currentSelectedData.Purchased;
        BuyButton.interactable = !isPurchased; // 如果買過了就不能按

        if (BuyButtonText != null)
        {
            BuyButtonText.text = isPurchased ? "已售罄" : "購買";
        }
    }
    private void ClearDetailPanel()
    {
        BuyButton.gameObject.SetActive(false);
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
        WorldIcon.sprite = DetailIconSprite_Empty;
        TypeIcon.sprite = DetailIconSprite_Empty;
        DetailIcon.sprite = DetailIconSprite_Empty;
        _currentSelectedData = null;
    }
    //按下購買
    private void OnBuyButtonClicked()
    {
        if (_currentSelectedData != null)
        {
            // 通知 Presenter 處理交易
            _onBuyRequestCallback?.Invoke(_currentSelectedData);
        }
    }
    //按下關閉
    private void OnCloseButtonClicked()
    {
        SetVisible(false);
        OnCloseShopUI?.Invoke();
    }
    #endregion
}