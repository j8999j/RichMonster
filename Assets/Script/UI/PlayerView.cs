using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;
using GameSystem;
//遊戲主UI(View)
public class PlayerView : MonoBehaviour
{
    //玩家背包相關
    public GameObject PlayerBag;//背包根物件
    public BagSlot BagSlotPrefab;//背包欄位預製物件
    public GameObject TagsPrefab;//標籤預製物件
    public Transform BagSlotContainer;//背包欄位容器
    public Transform TagSlotContainer;//標籤容器
    public Image DetailIcon;//背包物品圖片
    public Image TypeIcon;//背包物品類型圖片
    public Image WorldIcon;//世界標籤圖片
    public Sprite nullSprite;//空圖片
    public Sprite PropSprite;//道具
    public Sprite FoodSprite;//食物
    public Sprite EquipmentSprite;//裝備
    public Sprite MonsterTagSprite;//妖界
    public Sprite HumanTagSprite;//人間
    public TextMeshProUGUI DetailNameText;//背包物品名稱
    public TextMeshProUGUI DetailDescText;//背包物品描述
    public TextMeshProUGUI DetailPriceText;//背包物品購買成本
    public Button NextPageButton;//下一頁背包
    public Button PrePageButton;//上一頁背包
    public Button HumanItemButton;//人間物品按鈕
    public Button MonsterItemButton;//妖界物品按鈕
    public Button AllItemButton;//所有物品按鈕
    public int TargetLongEdgeSize;//顯示最大邊長限制
    
    // 分頁設定
    private const int ItemsPerPage = 15;  // 每頁顯示數量
    private const int PageScrollAmount = 5; // 每次翻頁移動數量 (15-10=5 個重疊)
    private int _currentStartIndex = 0;   // 當前起始索引
    private IReadOnlyList<Item> _currentItems; // 當前物品列表參照
    private List<Item> _filteredItems = new List<Item>(); // 篩選後的物品列表
    
    // 篩選設定
    private enum ItemFilter { All, Human, Monster }
    private ItemFilter _currentFilter = ItemFilter.All;
    
    private List<BagSlot> _activeSlots = new List<BagSlot>();//背包列表
    //玩家UI相關                                  
    public GameObject PlayerState;//狀態根物件
    
    private void Awake()
    {
        // 綁定分頁按鈕事件
        if (NextPageButton != null)
            NextPageButton.onClick.AddListener(OnNextPage);
        if (PrePageButton != null)
            PrePageButton.onClick.AddListener(OnPreviousPage);
        
        // 綁定篩選按鈕事件
        if (AllItemButton != null)
            AllItemButton.onClick.AddListener(() => SetFilter(ItemFilter.All));
        if (HumanItemButton != null)
            HumanItemButton.onClick.AddListener(() => SetFilter(ItemFilter.Human));
        if (MonsterItemButton != null)
            MonsterItemButton.onClick.AddListener(() => SetFilter(ItemFilter.Monster));
    }
    
    /// <summary>
    /// 設定篩選並更新顯示
    /// </summary>
    private void SetFilter(ItemFilter filter)
    {
        _currentFilter = filter;
        _currentStartIndex = 0; // 重置到第一頁
        ApplyFilter();
        ShowBagItems();
        ClearSelected();
        UpdateFilterButtonStates();
    }
    
    /// <summary>
    /// 套用篩選條件
    /// </summary>
    private void ApplyFilter()
    {
        if (_currentItems == null)
        {
            _filteredItems.Clear();
            return;
        }
        _filteredItems.Clear();
        foreach (var item in _currentItems)
        {
            var definition = DataManager.Instance.GetItemById(item.ItemId);
            if (definition == null) continue;
            
            switch (_currentFilter)
            {
                case ItemFilter.All:
                    _filteredItems.Add(item);
                    break;
                case ItemFilter.Human:
                    if (definition.World == ItemWorld.Human)
                        _filteredItems.Add(item);
                    break;
                case ItemFilter.Monster:
                    if (definition.World == ItemWorld.Monster)
                        _filteredItems.Add(item);
                    break;
            }
        }
    }
    
    /// <summary>
    /// 更新篩選按鈕狀態（選中的變灰色不可點擊）
    /// </summary>
    private void UpdateFilterButtonStates()
    {
        if (AllItemButton != null)
            AllItemButton.interactable = _currentFilter != ItemFilter.All;
        if (HumanItemButton != null)
            HumanItemButton.interactable = _currentFilter != ItemFilter.Human;
        if (MonsterItemButton != null)
            MonsterItemButton.interactable = _currentFilter != ItemFilter.Monster;
    }
    
    public void OpenBags()
    {
        ClearSelected();
        PlayerBag.SetActive(true);
        _currentStartIndex = 0; // 重置到第一頁
        _currentFilter = ItemFilter.All; // 預設顯示全部
        _currentItems = DataManager.Instance.CurrentPlayerData.InventoryItems;
        ApplyFilter();
        ShowBagItems();
        UpdateFilterButtonStates();
        GameManager.Instance.SetPlayerMove(false);
        GameManager.Instance.SetPlayerInteract(false);
    }
    
    // 顯示當前頁面的物品
    private void ShowBagItems()
    {
        if (_filteredItems == null || _filteredItems.Count == 0)
        {
            // 隱藏所有 Slot
            foreach (var slot in _activeSlots)
            {
                slot.gameObject.SetActive(false);
            }
            UpdatePageButtons();
            return;
        }
        
        // 計算當前頁面要顯示的物品範圍
        int startIndex = _currentStartIndex;
        int endIndex = Mathf.Min(startIndex + ItemsPerPage, _filteredItems.Count);
        int displayCount = endIndex - startIndex;
        
        // 1. 確保 UI 數量足夠
        AdjustSlotCount(ItemsPerPage);

        // 2. 把資料填進去
        for (int i = 0; i < displayCount; i++)
        {
            int itemIndex = startIndex + i;
            _activeSlots[i].Setup(_filteredItems[itemIndex], OnBagSelected);
            _activeSlots[i].gameObject.SetActive(true);
        }

        // 3. 隱藏多餘的 Slot
        for (int i = displayCount; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].gameObject.SetActive(false);
        }
        
        // 4. 更新分頁按鈕狀態
        UpdatePageButtons();
    }
    
    // 接收來自外部的資料列表 (相容舊介面)
    public void ShowBagItems(IReadOnlyList<Item> items)
    {
        _currentItems = items;
        _currentStartIndex = 0;
        ApplyFilter();
        ShowBagItems();
    }
    
    /// <summary>
    /// 下一頁
    /// </summary>
    private void OnNextPage()
    {
        if (_filteredItems == null || _filteredItems.Count == 0) return;
        
        int newStartIndex = _currentStartIndex + PageScrollAmount;
        int maxStartIndex = Mathf.Max(0, _filteredItems.Count - 1);
        
        Debug.Log($"OnNextPage: current={_currentStartIndex}, new={newStartIndex}, max={maxStartIndex}, count={_filteredItems.Count}");
        
        // 只要新起始位置不超過最後一個物品即可
        if (newStartIndex < _filteredItems.Count)
        {
            _currentStartIndex = Mathf.Min(newStartIndex, maxStartIndex);
            ShowBagItems();
        }
    }
    
    /// <summary>
    /// 上一頁
    /// </summary>
    private void OnPreviousPage()
    {
        if (_filteredItems == null || _filteredItems.Count == 0) return;
        
        int newStartIndex = _currentStartIndex - PageScrollAmount;
        
        Debug.Log($"OnPreviousPage: current={_currentStartIndex}, new={newStartIndex}");
        
        // 允許回到 0 或更前面（會被限制在 0）
        if (_currentStartIndex > 0)
        {
            _currentStartIndex = Mathf.Max(0, newStartIndex);
            ShowBagItems();
        }
    }
    
    /// <summary>
    /// 更新分頁按鈕的啟用狀態
    /// </summary>
    private void UpdatePageButtons()
    {
        if (_filteredItems == null || _filteredItems.Count == 0)
        {
            if (PrePageButton != null) PrePageButton.interactable = false;
            if (NextPageButton != null) NextPageButton.interactable = false;
            return;
        }
        
        int maxStartIndex = Mathf.Max(0, _filteredItems.Count - ItemsPerPage);
        
        // 上一頁按鈕：當起始索引 > 0 時可用
        if (PrePageButton != null)
            PrePageButton.interactable = _currentStartIndex > 0;
        
        // 下一頁按鈕：當還有更多物品時可用
        if (NextPageButton != null)
            NextPageButton.interactable = _currentStartIndex < maxStartIndex;
    }
    
    private void AdjustSlotCount(int targetCount)
    {
        while (_activeSlots.Count < targetCount)
        {
            BagSlot newSlot = Instantiate(BagSlotPrefab, BagSlotContainer);
            _activeSlots.Add(newSlot);
        }
    }
    private void ShowTags(List<string> tags)
    {
        for(int i = 0; i < tags.Count; i++)
        {
            string tagName = DataManager.Instance.GetTagNameByTag(tags[i]);
            if(tagName != "")
            {
                GameObject newSlot = Instantiate(TagsPrefab, TagSlotContainer);
                newSlot.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = tagName;
            }
        }
    }
    private void OnBagSelected(BagSlot slot)
    {
        ClearSelected();
        //處理選中背包物品的邏輯
        DetailNameText.text = slot._currentDefinition.Name;
        DetailDescText.text = slot._currentDefinition.Description;
        DetailPriceText.text = slot._currentData.CostPrice.ToString();
        DetailIcon.sprite = slot._targetImage.sprite;
        switch(slot._currentDefinition.World)
        {
            case ItemWorld.Human:
            WorldIcon.sprite = HumanTagSprite;
            break;
            case ItemWorld.Monster:
            WorldIcon.sprite = MonsterTagSprite;
            break;
        }
        switch(slot._currentDefinition.Type)
        {
            case ItemType.Food:
            TypeIcon.sprite = FoodSprite;
            break;
            case ItemType.Equipment:
            TypeIcon.sprite = EquipmentSprite;
            break;
            case ItemType.Prop:
            TypeIcon.sprite = PropSprite;
            break;
        }
        ShowTags(slot._currentDefinition.Tags);
        AdjustImageScale(DetailIcon);
    }
    private void ClearSelected()
    {
        //清空選中背包物品的邏輯
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
        DetailIcon.sprite = nullSprite;
        WorldIcon.sprite = nullSprite;
        TypeIcon.sprite = nullSprite;
        foreach(Transform child in TagSlotContainer)
        {
            Destroy(child.gameObject);
        }
    }
    public void CloseBags()
    {
        PlayerBag.SetActive(false);
        ClearSelected();
        GameManager.Instance.SetPlayerMove(true);
        GameManager.Instance.SetPlayerInteract(true);
    }
    /// <summary>
    /// 調整圖片縮放，使長邊達到目標尺寸
    /// </summary>
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
}