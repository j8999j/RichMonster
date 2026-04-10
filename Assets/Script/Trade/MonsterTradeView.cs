using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
public class MonsterTradeView : MonoBehaviour
{
    // ==========================================
    // UI 元件參照
    // ==========================================

    [Header("UI根物件")]
    public GameObject TradeUI;

    [Header("背包組件")]
    [SerializeField, Tooltip("交易背包根物件")]
    private GameObject TradeBag;

    [Tooltip("背包欄位預製物件")]
    public TradeSlot TradeSlotPrefab;

    [Tooltip("生成 Slot 的父物件")]
    public Transform SlotContainer;

    [Tooltip("標籤容器")]
    public Transform TagSlotContainer;

    [Tooltip("背包物品圖片")]
    public Image DetailIcon;

    [Tooltip("稀有度圖片")]
    public Image RarityIcon;

    [Tooltip("物品類型圖片")]
    public Image TypeIcon;

    public Sprite PropSprite;
    public Sprite FoodSprite;
    public Sprite EquipmentSprite;

    [Tooltip("標籤預製物件")]
    public GameObject TagsPrefab;

    [Tooltip("標籤容器")]
    public Transform ItemTagCotainer;

    [Tooltip("背包物品名稱")]
    public TextMeshProUGUI DetailNameText;

    [Tooltip("背包物品描述")]
    public TextMeshProUGUI DetailDescText;

    [Tooltip("背包物品購買成本")]
    public TextMeshProUGUI DetailPriceText;

    [Header("交易組件")]
    [SerializeField, Tooltip("交易中面版")]
    private GameObject TradeModeUI;

    [SerializeField, Tooltip("切換階段開始交易")]
    private Button OnOpenShopButton;
    [SerializeField] private Sprite NoneSprite;

    [SerializeField, Tooltip("客人圖片")]
    private Image CustomerImage;

    [SerializeField, Tooltip("妖界貨幣增加動畫")]
    private TextMeshProUGUI SoulAddAnimationText;

    [SerializeField, Tooltip("剩餘客人")]
    private TextMeshProUGUI CustomerIndex;

    [SerializeField, Tooltip("目前妖界貨幣")]
    private TextMeshProUGUI NowSoul;

    [Tooltip("客人對話")]
    public TextMeshProUGUI CustomerDialogText;

    [Header("妖怪資訊組件")]
    public TextMeshProUGUI MonsterText;
    public Transform PreferContain;
    public Transform HateContain;

    [Tooltip("妖怪標籤圖片長邊目標尺寸")]
    public float MonsterTagTargetSize = 70f;

    [Header("拖曳放置區域")]
    [SerializeField, Tooltip("交易放置區域")]
    private RectTransform TradeDropZone;

    [Header("滿意度效果物件")]
    [SerializeField, Tooltip("厭惡效果物件")]
    private GameObject HatedEffect;
    [SerializeField, Tooltip("尚可效果物件")]
    private GameObject OkayEffect;
    [SerializeField, Tooltip("滿意效果物件")]
    private GameObject SatisfiedEffect;
    [SerializeField, Tooltip("非常滿意效果物件")]
    private GameObject VerySatisfiedEffect;

    // ==========================================
    // 內部資料與狀態
    // ==========================================

    private List<Item> bagItemsList;
    private List<TradeSlot> _activeSlots = new List<TradeSlot>(); // 背包列表緩存

    private Item OnSelectItem;
    private ItemQuality OnSelectQuality;
    public Item SelectedItem => OnSelectItem;

    // === 動畫控制 ===
    private bool _isFading = false; // 淡入淡出中，禁止交易
    private readonly WaitForSeconds _satisfactionWait = new(2f);
    [SerializeField, Tooltip("淡入淡出時長(秒)")]
    private float _fadeDuration = 0.3f;
    [SerializeField, Tooltip("交易結束後停頓秒數")]
    private float _exitDelay = 1.5f;
    private Sequence _fadeSequence; // 當前淡入淡出序列

    // ==========================================
    // 事件 (提供給 Presenter)
    // ==========================================

    /// <summary>開始營業事件</summary>
    public event Action OnOpenShop;

    /// <summary>提交商品事件</summary>
    public event Action<Item> TradePrice;

    // ==========================================
    // Unity 生命週期
    // ==========================================

    void Start()
    {
        OnOpenShopButton.onClick.AddListener(InvokeOnOpenButton);
    }

    // ==========================================
    // 基本 UI 控制
    // ==========================================

    /// <summary>
    /// 開啟商店主介面
    /// </summary>
    public void OpenShopUI()
    {
        TradeUI.SetActive(true);
    }

    /// <summary>
    /// 關閉商店主介面
    /// </summary>
    public void ExitShopUI()
    {
        TradeUI.SetActive(false);
    }

    #region InventoryUIView

    /// <summary>
    /// 顯示背包物品列表 (只顯示人類世界的物品)
    /// </summary>
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

    /// <summary>
    /// 調整實例化的 Slot 數量以符合需求
    /// </summary>
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

    /// <summary>
    /// 根據交易滿意度顯示對應效果物件，2秒後自動隱藏
    /// </summary>
    public void ShowSatisfactionVisual(TradeSatisfaction satisfaction)
    {
        HideAllSatisfactionEffects();

        GameObject target = satisfaction switch
        {
            TradeSatisfaction.Hated => HatedEffect,
            TradeSatisfaction.Okay => OkayEffect,
            TradeSatisfaction.Satisfied => SatisfiedEffect,
            TradeSatisfaction.VerySatisfied => VerySatisfiedEffect,
            _ => null
        };

        if (target != null) StartCoroutine(ShowThenHide(target));
    }

    private IEnumerator ShowThenHide(GameObject effect)
    {
        effect.SetActive(true);
        yield return _satisfactionWait;
        effect.SetActive(false);
    }

    private void HideAllSatisfactionEffects()
    {
        StopAllCoroutines();
        if (HatedEffect != null) HatedEffect.SetActive(false);
        if (OkayEffect != null) OkayEffect.SetActive(false);
        if (SatisfiedEffect != null) SatisfiedEffect.SetActive(false);
        if (VerySatisfiedEffect != null) VerySatisfiedEffect.SetActive(false);
    }

    /// <summary>
    /// 更新顧客資訊(圖片、對話、偏好標籤等)
    /// </summary>
    public void UpdateGuestInfo(MonsterGuest guest)
    {
        // 根據顧客職業 ID 載入圖片（淡出 → 換圖 → 淡入）
        SpriteLoader.LoadSpriteAsync(guest.monsterCustomer.Profession, sprite =>
        {
            if (sprite == null) return;

            // 殺掉前一個未完成的序列
            _fadeSequence?.Kill();
            _isFading = true;

            _fadeSequence = DOTween.Sequence();
            // 淡出
            _fadeSequence.Append(CustomerImage.DOFade(0f, _fadeDuration));
            // 換圖（Alpha 為 0 時執行）
            _fadeSequence.AppendCallback(() =>
            {
                CustomerImage.sprite = sprite;
                CustomerImage.SetNativeSize();
            });
            // 淡入
            _fadeSequence.Append(CustomerImage.DOFade(1f, _fadeDuration));
            // 完成後解除禁止交易
            _fadeSequence.OnComplete(() => _isFading = false);
        });

        // 顯示妖怪簡介 (無論是否有已解鎖資訊)
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
    /// 顯示妖怪標籤圖片或文字（參考 GameBookView 的 ShowMonsterTags）
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

                // 建立 Tag 圖片物件
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

                // 嘗試載入 Tag 圖片
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

    /// <summary>
    /// 更新顧客對話文字
    /// </summary>
    public void UpdateDialog(string dialog)
    {
        CustomerDialogText.text = dialog;
    }

    /// <summary>
    /// 載入下一位客人後，更新 UI (包含客人資訊與背包資料)
    /// </summary>
    public void UpdateTradeInfo(MonsterGuest guest, List<Item> bagItems, int currentIndex, int totalCount, int currentSoul)
    {
        // 更新背包列表
        bagItemsList = bagItems;
        // 更新剩餘人數
        CustomerIndex.text = $"剩餘客人{totalCount - (currentIndex + 1)}";
        // 更新目前妖界貨幣
        NowSoul.text = currentSoul.ToString();

        // 刷新背包顯示
        ShowBagItems(bagItemsList);
        // 顯示客人
        UpdateGuestInfo(guest);
    }

    /// <summary>
    /// 更新妖界貨幣增加時的文字動畫
    /// </summary>
    public void UpdateSoulDisplayAnimation(int price)
    {
        SoulAddAnimationText.text = "+" + price.ToString() + "$";
        SoulAddAnimationText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 直接更新妖界貨幣文字
    /// </summary>
    public void UpdateSoulDisplay(int currentSoul)
    {
        NowSoul.text = currentSoul.ToString();
    }

    /// <summary>
    /// 設定開始選擇交易物品時的 UI 狀態
    /// </summary>
    public void SetSelectTradeUI()
    {
        ClearBagImage();
        ClearImage();
        ShowBagItems(bagItemsList);

        TradeModeUI.SetActive(true);
        TradeBag.SetActive(true);
    }

    /// <summary>
    /// 設定議價中的 UI 顯示 (隱藏背包)
    /// </summary>
    public void SetTradePriceUI()
    {
        TradeBag.SetActive(false);
    }

    /// <summary>
    /// 交易結束：停頓 _exitDelay 秒 → 顧客圖片淡出 → 執行 onComplete callback
    /// (淡出期間 _isFading=true，禁止拖曳交易)
    /// </summary>
    public void FadeOutCustomerThenCallback(Action onComplete)
    {
        _fadeSequence?.Kill();
        _isFading = true;

        _fadeSequence = DOTween.Sequence();
        // 停頓
        _fadeSequence.AppendInterval(_exitDelay);
        // 淡出
        _fadeSequence.Append(CustomerImage.DOFade(0f, _fadeDuration));
        // 完成後解除旗標並執行 callback
        _fadeSequence.OnComplete(() =>
        {
            _isFading = false;
            onComplete?.Invoke();
        });
    }

    #endregion

    #region TradeUIView

    /// <summary>
    /// 當玩家點擊或選中某個物品時，顯示其詳細資訊與標籤
    /// </summary>
    private void OnTradeSelected(BagSlot bagSlot)
    {
        // 清除現有物品標籤
        if (ItemTagCotainer != null)
        {
            foreach (Transform child in ItemTagCotainer)
            {
                Destroy(child.gameObject);
            }
        }

        OnSelectQuality = ItemQuality.None;
        OnSelectItem = bagSlot._currentData;

        // 顯示基本資料
        DetailNameText.text = bagSlot._currentDefinition.Name;
        DetailDescText.text = bagSlot._currentDefinition.Description;
        DetailPriceText.text = bagSlot._currentData.CostPrice.ToString();
        DetailIcon.sprite = bagSlot._targetImage.sprite;

        // 載入類型圖片
        if (TypeIcon != null)
        {
            if (bagSlot._currentDefinition.Type == ItemType.Prop)
                TypeIcon.sprite = PropSprite;
            else if (bagSlot._currentDefinition.Type == ItemType.Food)
                TypeIcon.sprite = FoodSprite;
            else if (bagSlot._currentDefinition.Type == ItemType.Equipment)
                TypeIcon.sprite = EquipmentSprite;
        }

        // 載入稀有度圖片
        string rarityId = bagSlot._currentDefinition.Rarity.ToString();
        SpriteLoader.LoadSpriteAsync(rarityId, sprite =>
        {
            if (RarityIcon == null) return;
            RarityIcon.sprite = sprite != null ? sprite : NoneSprite;
        });

        // 顯示該物品的標籤
        ShowTags(bagSlot._currentDefinition.Tags);
        DetailIcon.SetNativeSize();
    }

    /// <summary>
    /// 顯示被選中物品的所有標籤圖片
    /// </summary>
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

                // 建立 Tag 圖片物件
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

                // 嘗試載入 Tag 圖片，成功則顯示圖片並隱藏文字
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

    /// <summary>
    /// 物品拖曳結束時觸發
    /// </summary>
    private void OnEndTradeDrag(TradeSlot slot, PointerEventData eventData)
    {
        if (OnSelectItem == null) return;

        // 淡入淡出中，禁止任何交易操作
        if (_isFading) return;

        // 檢測是否在放置區域內
        if (IsPointerInsideDropZone(eventData))
        {
            TradePrice?.Invoke(OnSelectItem);
        }
    }

    /// <summary>
    /// 檢測滑鼠位置是否成功落入交易放置區域內
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

    /// <summary>
    /// 確認選中物品 (尚未供其他按鈕呼叫)
    /// </summary>
    public void ConfirmSelectItem()
    {
        if (OnSelectItem == null || OnSelectQuality == ItemQuality.None)
            return;
        SetTradePriceUI();
    }

    /// <summary>
    /// 關閉整個交易模式 UI
    /// </summary>
    public void EndTradeMode()
    {
        TradeUI.SetActive(false);
    }

    /// <summary>
    /// 清除物品詳細資訊面板的圖文內容
    /// </summary>
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

    /// <summary>
    /// 清空顧客或物品圖示 (退回無選擇狀態)
    /// </summary>
    public void ClearImage()
    {
        DetailIcon.sprite = NoneSprite;
        CustomerImage.sprite = NoneSprite;
    }

    #endregion

    #region ButtonEvent

    /// <summary>
    /// 按下「開始營業」按鈕觸發
    /// </summary>
    private void InvokeOnOpenButton()
    {
        OnOpenShop?.Invoke();
        StartTradeUI();
    }

    /// <summary>
    /// 啟動交易的初始顯示
    /// </summary>
    private void StartTradeUI()
    {
        OnOpenShopButton.gameObject.SetActive(false);
        SetSelectTradeUI();
    }

    #endregion
}