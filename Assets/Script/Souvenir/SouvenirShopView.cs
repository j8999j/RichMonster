using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using Souvenir;
public class SouvenirShopView : MonoBehaviour
{
    [Header("Panels")]
    public GameObject MainPanel;
    public GameObject DetailPanel;

    [Header("Slot Containers")]
    public Transform SlotContainer;
    public SouvenirSlot SlotPrefab;

    [Header("Pagination")]
    public Button PreviousPageButton;
    public Button NextPageButton;
    private const int ItemsPerPage = 8;
    private int _currentPage = 0;
    
    [Header("Points Display")]
    public TextMeshProUGUI RemainingPointsText; // 目前剩餘成就點數

    [Header("Detail Display")]
    public TextMeshProUGUI DetailNameText;
    public TextMeshProUGUI DetailDescriptionText;
    public TextMeshProUGUI DetailFunctionText;
    public TextMeshProUGUI DetailPriceText;
    public Image DetailIcon;
    public Button ExchangeButton;
    public Image ExchangeButtonImage; // 兌換按鈕的替換圖片
    public Sprite CanExchangeSprite;
    public Sprite OwnedSprite;
    public float TargetLongEdgeSize = 150f;

    private List<SouvenirSlot> _spawnedSlots = new List<SouvenirSlot>();
    private List<AchievementSouvenirData> _catalogItems = new List<AchievementSouvenirData>();
    private SouvenirSlot _currentSelectedSlot;

    private void Awake()
    {
        if (PreviousPageButton != null) PreviousPageButton.onClick.AddListener(OnPreviousPage);
        if (NextPageButton != null) NextPageButton.onClick.AddListener(OnNextPage);
        if (ExchangeButton != null) ExchangeButton.onClick.AddListener(OnExchangeClicked);
    }

    /// <summary>
    /// 開啟紀念品商店
    /// </summary>
    public void OpenShop()
    {
        if (MainPanel != null) MainPanel.SetActive(true);
        if (DetailPanel != null) DetailPanel.SetActive(false);
        _currentSelectedSlot = null;

        LoadCatalog();
        UpdatePointsDisplay();

        _currentPage = 0;
        RefreshPage();
    }

    /// <summary>
    /// 關閉紀念品商店
    /// </summary>
    public void CloseShop()
    {
        if (MainPanel != null) MainPanel.SetActive(false);
    }

    /// <summary>
    /// 取得所有的成就紀念品目錄 (包含靜態資料)
    /// </summary>
    private void LoadCatalog()
    {
        _catalogItems.Clear();
        // 透過 DataManager 取得全體紀念品資料
        var dict = DataManager.Instance.AchievementSouvenirDict;
        if (dict != null)
        {
            // 將字典內容加進列表並排除預設擁有的 Sou_key
            foreach (var kvp in dict)
            {
                if (kvp.Key != "Sou_key")
                {
                    _catalogItems.Add(kvp.Value);
                }
            }
            _catalogItems.Sort((a, b) => string.Compare(b.SouvenirID, a.SouvenirID, StringComparison.Ordinal));
        }
    }

    private void RefreshPage()
    {
        if (_catalogItems == null || _catalogItems.Count == 0)
        {
            for (int i = 0; i < _spawnedSlots.Count; i++) _spawnedSlots[i].gameObject.SetActive(false);
            if (PreviousPageButton != null) PreviousPageButton.interactable = false;
            if (NextPageButton != null) NextPageButton.interactable = false;
            return;
        }

        int totalPages = Mathf.CeilToInt((float)_catalogItems.Count / ItemsPerPage);

        // 安全限制
        if (_currentPage < 0) _currentPage = 0;
        if (_currentPage >= totalPages) _currentPage = totalPages - 1;

        int startIndex = _currentPage * ItemsPerPage;
        int endIndex = Mathf.Min(startIndex + ItemsPerPage, _catalogItems.Count);
        int displayCount = endIndex - startIndex;

        // 確保 UI 格數足夠
        AdjustSlotCount(displayCount);

        // 設定格位資料
        for (int i = 0; i < displayCount; i++)
        {
            var data = _catalogItems[startIndex + i];
            bool isOwned = SouvenirManager.Instance.IsPurchased(data.SouvenirID);

            _spawnedSlots[i].Setup(data, isOwned, OnSlotClicked);
            _spawnedSlots[i].gameObject.SetActive(true);
        }

        // 隱藏多餘的格位
        for (int i = displayCount; i < _spawnedSlots.Count; i++)
        {
            _spawnedSlots[i].gameObject.SetActive(false);
        }

        // 預設選中該頁第一項
        if (displayCount > 0)
        {
            OnSlotClicked(_spawnedSlots[0]);
        }

        // 更新翻頁按鈕狀態
        if (PreviousPageButton != null) PreviousPageButton.interactable = _currentPage > 0;
        if (NextPageButton != null) NextPageButton.interactable = _currentPage < totalPages - 1;
    }

    private void AdjustSlotCount(int targetCount)
    {
        while (_spawnedSlots.Count < targetCount)
        {
            // 動態生成物件池
            if (SlotPrefab != null && SlotContainer != null)
            {
                SouvenirSlot newSlot = Instantiate(SlotPrefab, SlotContainer);
                _spawnedSlots.Add(newSlot);
            }
            else
            {
                Debug.LogWarning("[SouvenirShopView] 未設定 SlotPrefab 或 SlotContainer。");
                break;
            }
        }
    }

    private void OnSlotClicked(SouvenirSlot slot)
    {
        _currentSelectedSlot = slot;
        if (DetailPanel != null) DetailPanel.SetActive(true);

        var data = slot.CurrentData;
        if (data == null) return;

        // 更新詳細資訊文字
        if (DetailNameText != null) DetailNameText.text = data.SouvenirName;
        if (DetailDescriptionText != null) DetailDescriptionText.text = data.SouvenirDescription;
        if (DetailFunctionText != null) DetailFunctionText.text = data.SouvenirFunctionDescription;
        if (DetailPriceText != null) DetailPriceText.text = data.PointsFee.ToString();

        // 更新圖片
        if (DetailIcon != null)
        {
            DetailIcon.color = Color.white; // 在細節面板中確保是正常顏色
            
            // 使用非同步載入，確保首次打開時即使 Slot 圖片還沒載入完也能正確取得
            SpriteLoader.LoadSpriteAsync(data.SouvenirID, sprite =>
            {
                // 防連點被後續載入覆蓋
                if (DetailIcon != null && _currentSelectedSlot == slot)
                {
                    DetailIcon.sprite = sprite != null ? sprite : slot.DefaultSprite;
                    SpriteLoader.AdjustImageScale(DetailIcon, TargetLongEdgeSize);
                }
            });
        }

        UpdateExchangeButtonState();
    }

    private void UpdateExchangeButtonState()
    {
        if (_currentSelectedSlot == null || ExchangeButton == null) return;
        
        var data = _currentSelectedSlot.CurrentData;

        bool isOwned = SouvenirManager.Instance.IsPurchased(data.SouvenirID);
        int myPoints = SouvenirManager.Instance.GetRemainingPoints();
        bool canAfford = myPoints >= data.PointsFee;

        if (isOwned)
        {
            if (ExchangeButtonImage != null) ExchangeButtonImage.sprite = OwnedSprite;
        }
        else if (!canAfford)
        {
            if (ExchangeButtonImage != null) ExchangeButtonImage.sprite = CanExchangeSprite;
        }
        else
        {
            if (ExchangeButtonImage != null) ExchangeButtonImage.sprite = CanExchangeSprite;
        }
    }

    private void OnExchangeClicked()
    {
        if (_currentSelectedSlot == null) return;

        var data = _currentSelectedSlot.CurrentData;

        bool success = SouvenirManager.Instance.TryPurchaseSouvenir(data.SouvenirID);
        if (success)
        {
            // 購買後立刻存檔
            _ = DataManager.Instance.SaveBookAsync();

            UpdatePointsDisplay();
            
            // 重新刷新這個 slot 的狀態跟 UI
            _currentSelectedSlot.Setup(data, true, OnSlotClicked);
            
            // 更新右側按鈕的顯示為「已擁有」
            UpdateExchangeButtonState();
        }
    }

    private void UpdatePointsDisplay()
    {
        if (RemainingPointsText != null)
        {
            RemainingPointsText.text = SouvenirManager.Instance.GetRemainingPoints().ToString();
        }
    }

    private void OnPreviousPage()
    {
        _currentPage--;
        RefreshPage();
    }

    private void OnNextPage()
    {
        _currentPage++;
        RefreshPage();
    }
}
