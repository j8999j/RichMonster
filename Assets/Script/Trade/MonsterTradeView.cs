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
    public Image RarityIcon;//稀有度圖片
    public Image TypeIcon;//物品類型圖片
    public Sprite PropSprite;
    public Sprite FoodSprite;
    public Sprite EquipmentSprite;
    public GameObject TagsPrefab;//標籤預製物件
    public Transform ItemTagCotainer;//標籤容器
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
    [SerializeField] private Sprite PreferSprite;
    [SerializeField] private Sprite NotPreferSprite;
    [SerializeField] private Sprite NoneSprite;
    [SerializeField] private Image CustomerImage;//客人圖片
    [SerializeField] private TextMeshProUGUI SoulAddAnimationText;//妖界貨幣增加動畫
    [SerializeField] private TextMeshProUGUI CustomerIndex;//剩餘客人
    [SerializeField] private TextMeshProUGUI NowSoul;//目前妖界貨幣
    public TextMeshProUGUI CustomerDialogText;//客人對話
    [Header("妖怪資訊組件")]
    public TextMeshProUGUI MonsterText;
    public Transform PreferContain;
    public Transform HateContain;
    [Tooltip("妖怪標籤圖片長邊目標尺寸")]
    public float MonsterTagTargetSize = 70f;
    [Header("拖曳放置區域")]
    [SerializeField] private RectTransform TradeDropZone;//交易放置區域
    // ======= Events to Presenter =======
    public event Action OnOpenShop;//開始營業
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

        // 無論是否有已解鎖資訊，都顯示妖怪簡介
        if (MonsterText != null)
        {
            MonsterText.text = guest.monsterCustomer.Description ?? "";
        }

        // 清空舊的標籤
        ClearContainer(PreferContain);
        ClearContainer(HateContain);

        // 顯示已解鎖資訊對應的標籤
        string monsterId = guest.monsterCustomer.Profession;
        if (!string.IsNullOrEmpty(monsterId))
        {
            var saveBook = DataManager.Instance.GetBookData();
            if (saveBook?.MonsterBookData?.UnlockMonsterInformationID != null)
            {
                var infos = DataManager.Instance.GetMonsterInfosByMonsterID(monsterId);
                var likeTags = new List<string>();
                var hateTags = new List<string>();

                foreach (var info in infos)
                {
                    if (saveBook.MonsterBookData.UnlockMonsterInformationID.Contains(info.InformationID))
                    {
                        if (!string.IsNullOrEmpty(info.TagID))
                        {
                            if (guest.monsterCustomer.PreferredTags != null
                                && guest.monsterCustomer.PreferredTags.Contains(info.TagID))
                            {
                                if (!likeTags.Contains(info.TagID)) likeTags.Add(info.TagID);
                            }
                            else if (guest.monsterCustomer.HateTags != null
                                && guest.monsterCustomer.HateTags.Contains(info.TagID))
                            {
                                if (!hateTags.Contains(info.TagID)) hateTags.Add(info.TagID);
                            }
                        }
                    }
                }

                ShowMonsterTags(likeTags, PreferContain);
                ShowMonsterTags(hateTags, HateContain);
            }
        }
    }

    /// <summary>
    /// 清空容器內的所有子物件
    /// </summary>
    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 顯示妖怪標籤（參考 GameBookView 的 ShowMonsterTags）
    /// </summary>
    private void ShowMonsterTags(List<string> tags, Transform container)
    {
        if (tags == null || TagsPrefab == null || container == null) return;

        for (int i = 0; i < tags.Count; i++)
        {
            string tagId = tags[i];
            string tagName = DataManager.Instance.GetTagNameByTag(tagId);

            if (tagName != "")
            {
                GameObject newSlot = Instantiate(TagsPrefab, container);

                TextMeshProUGUI textComp = newSlot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                textComp.text = tagName;

                // 建立Tag圖片物件
                GameObject imgObj = new GameObject("TagImage");
                imgObj.transform.SetParent(newSlot.transform, false);
                Image tagImage = imgObj.AddComponent<Image>();
                imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                imgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(MonsterTagTargetSize, MonsterTagTargetSize);

                // 預設隱藏圖片，顯示文字
                imgObj.SetActive(false);
                textComp.gameObject.SetActive(true);

                Image capturedImage = tagImage;
                TextMeshProUGUI capturedText = textComp;
                GameObject capturedImgObj = imgObj;

                // 嘗試載入Tag圖片
                SpriteLoader.LoadSpriteAsync(tagId, sprite =>
                {
                    if (capturedImgObj == null) return;
                    if (sprite != null)
                    {
                        capturedImage.sprite = sprite;
                        capturedImage.SetNativeSize();
                        RectTransform rt = capturedImage.GetComponent<RectTransform>();
                        float ratio = MonsterTagTargetSize / Mathf.Max(rt.sizeDelta.x, rt.sizeDelta.y);
                        rt.sizeDelta = new Vector2(rt.sizeDelta.x * ratio, rt.sizeDelta.y * ratio);
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
    public void UpdateDialog(string dialog)
    {
        CustomerDialogText.text = dialog;
    }
    public void UpdateTradeInfo(MonsterGuest guest, List<Item> bagItems, int currentIndex, int totalCount, int currentSoul)//更新客人與背包資訊
    {
        //更新背包列表
        bagItemsList = bagItems;
        //更新剩餘人數
        CustomerIndex.text = $"剩餘客人{totalCount - (currentIndex + 1)}";
        //更新目前妖界貨幣
        NowSoul.text = currentSoul.ToString();
        //刷新背包顯示
        ShowBagItems(bagItemsList);
        //顯示客人
        UpdateGuestInfo(guest);
    }
    public void UpdateSoulDisplayAnimation(int price)//更新妖界貨幣顯示
    {
        SoulAddAnimationText.text = "+" + price.ToString() + "$";
        SoulAddAnimationText.gameObject.SetActive(true);
    }
    public void UpdateSoulDisplay(int currentSoul)//更新妖界貨幣顯示
    {
        NowSoul.text = currentSoul.ToString();
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
    private void OnTradeSelected(BagSlot bagSlot)//選中物品顯示
    {
        // 清除標籤
        if (ItemTagCotainer != null)
        {
            foreach (Transform child in ItemTagCotainer)
            {
                Destroy(child.gameObject);
            }
        }
        OnSelectQuality = ItemQuality.None;
        OnSelectItem = bagSlot._currentData;
        DetailNameText.text = bagSlot._currentDefinition.Name;
        DetailDescText.text = bagSlot._currentDefinition.Description;
        DetailPriceText.text = bagSlot._currentData.CostPrice.ToString();
        DetailIcon.sprite = bagSlot._targetImage.sprite;
        if (TypeIcon != null)
        {
            if (bagSlot._currentDefinition.Type == ItemType.Prop)
            {
                TypeIcon.sprite = PropSprite;
            }
            else if (bagSlot._currentDefinition.Type == ItemType.Food)
            {
                TypeIcon.sprite = FoodSprite;
            }
            else if (bagSlot._currentDefinition.Type == ItemType.Equipment)
            {
                TypeIcon.sprite = EquipmentSprite;
            }
        }
        string rarityId = bagSlot._currentDefinition.Rarity.ToString();
        SpriteLoader.LoadSpriteAsync(rarityId, sprite =>
        {
            if (RarityIcon == null) return;
            if (sprite != null)
            {
                RarityIcon.sprite = sprite;
            }
            else
            {
                RarityIcon.sprite = NoneSprite;
            }
        });
        // 顯示標籤
        ShowTags(bagSlot._currentDefinition.Tags);
        DetailIcon.SetNativeSize();
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

                // 建立Tag圖片物件
                GameObject imgObj = new GameObject("TagImage");
                imgObj.transform.SetParent(newSlot.transform, false);
                Image tagImage = imgObj.AddComponent<Image>();
                imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                imgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 65);

                // 預設隱藏圖片，顯示文字
                imgObj.SetActive(false);
                textComp.gameObject.SetActive(true);

                Image capturedImage = tagImage;
                TextMeshProUGUI capturedText = textComp;
                GameObject capturedImgObj = imgObj;

                // 嘗試載入Tag圖片，成功則顯示圖片並隱藏文字
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
        // 清除標籤
        if (ItemTagCotainer != null)
        {
            foreach (Transform child in ItemTagCotainer)
            {
                Destroy(child.gameObject);
            }
        }
        DetailIcon.sprite = NoneSprite;
        TypeIcon.sprite = NoneSprite;
        RarityIcon.sprite = NoneSprite;
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
    }
    public void ClearImage()//清空圖片
    {
        DetailIcon.sprite = NoneSprite;
        CustomerImage.sprite = NoneSprite;
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
    #endregion
}