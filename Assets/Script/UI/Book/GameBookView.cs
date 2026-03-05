using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using System.Linq;
public class GameBookView : MonoBehaviour
{
    private GameSaveBook SaveBook;
    public GameObject BookPanel;
    public GameObject ItemBook;
    public GameObject MonsterBook;
    public BookItemSlot SlotPrefab;
    public GameObject TagsPrefab;
    public Transform ItemTagCotainer;
    public Transform ItemSlotCotainer;
    [Header("物品圖鑑")]
    public TextMeshProUGUI ItemName;
    public TextMeshProUGUI ItemDescription;
    public Image DetailIcon;
    public Image RarityIcon;
    public Image TypeIcon;
    public Sprite nullSprite;
    public int TargetLongEdgeSize = 55;

    private List<BookItemSlot> _activeSlots = new List<BookItemSlot>();
    private List<ItemDefinition> _allItems;

    public void ShowBook(bool isItemBook)
    {
        BookPanel.SetActive(true);
        ItemBook.SetActive(isItemBook);
        MonsterBook.SetActive(!isItemBook);
        if (isItemBook) OpenItemBook();
    }
    public void ShowItemBook()
    {
        ItemBook.SetActive(true);
        MonsterBook.SetActive(false);
    }
    public void ShowMonsterBook()
    {
        ItemBook.SetActive(false);
        MonsterBook.SetActive(true);
    }

    /// <summary>
    /// 開啟物品圖鑑，顯示所有已載入的物品，已收錄的正常顯示，未收錄的黑色顯示
    /// </summary>
    public void OpenItemBook()
    {
        SaveBook = DataManager.Instance.GetBookData();
        _allItems = DataManager.Instance.ItemDict.Values.ToList();
        ClearItemBookSelected();
        ShowItemBookSlots();
    }

    /// <summary>
    /// 顯示物品圖鑑所有物品欄位
    /// </summary>
    private void ShowItemBookSlots()
    {
        if (_allItems == null || _allItems.Count == 0)
        {
            foreach (var slot in _activeSlots)
            {
                slot.gameObject.SetActive(false);
            }
            return;
        }

        // 確保 Slot 數量足夠
        AdjustSlotCount(_allItems.Count);

        for (int i = 0; i < _allItems.Count; i++)
        {
            var itemDef = _allItems[i];

            // 檢查 SaveBook 中是否有收錄記錄，有則正常顯示，否則黑色顯示
            bool isBooked = IsItemBooked(itemDef.Id);

            _activeSlots[i].Setup(itemDef.Id, isBooked, OnItemBookSlotSelected);
            _activeSlots[i].gameObject.SetActive(true);
            _activeSlots[i].SetBlack(!isBooked);
        }

        // 隱藏多餘的 Slot
        for (int i = _allItems.Count; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 檢查物品是否已收錄在圖鑑中
    /// </summary>
    private bool IsItemBooked(string itemId)
    {
        if (SaveBook == null || SaveBook.ItemBookData == null || SaveBook.ItemBookData.ItemBooks == null)
            return false;
        var entry = SaveBook.ItemBookData.ItemBooks.Find(x => x.ItemID == itemId);
        return entry != null && entry.IsBooked;
    }

    private void AdjustSlotCount(int targetCount)
    {
        while (_activeSlots.Count < targetCount)
        {
            BookItemSlot newSlot = Instantiate(SlotPrefab, ItemSlotCotainer);
            _activeSlots.Add(newSlot);
        }
    }

    /// <summary>
    /// 物品圖鑑欄位被點擊時，顯示物品詳細資訊
    /// </summary>
    private void OnItemBookSlotSelected(BookItemSlot slot, bool isUnlocked)
    {
        ClearItemBookSelected();

        if (slot.CurrentDefinition == null) return;

        ItemName.text = slot.CurrentDefinition.Name;
        ItemDescription.text = slot.CurrentDefinition.Description;

        if (DetailIcon != null)
        {
            DetailIcon.sprite = slot.ItemImage.sprite;
            AdjustImageScale(DetailIcon);
        }

        // 顯示標籤
        ShowTags(slot.CurrentDefinition.Tags);
    }

    /// <summary>
    /// 顯示物品標籤（參考 PlayerView 的 ShowTags）
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

                // 嘗試載入Tag圖片，成功則顯示圖片並隱藏文字
                SpriteLoader.LoadSpriteAsync(tagId, sprite =>
                {
                    if (capturedImgObj == null) return;
                    if (sprite != null)
                    {
                        capturedImage.sprite = sprite;
                        capturedImage.SetNativeSize();
                        RectTransform rt = capturedImage.GetComponent<RectTransform>();
                        float ratio = 175f / rt.sizeDelta.x;
                        rt.sizeDelta = new Vector2(175f, rt.sizeDelta.y * ratio);
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
    /// 清空物品圖鑑選中狀態
    /// </summary>
    private void ClearItemBookSelected()
    {
        ItemName.text = "";
        ItemDescription.text = "";

        if (DetailIcon != null)
            DetailIcon.sprite = nullSprite;

        // 清除標籤
        if (ItemTagCotainer != null)
        {
            foreach (Transform child in ItemTagCotainer)
            {
                Destroy(child.gameObject);
            }
        }
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

        float longEdge = Mathf.Max(width, height);
        if (longEdge <= 0) return;

        float scale = TargetLongEdgeSize / longEdge;
        rt.sizeDelta = new Vector2(width * scale, height * scale);
    }
}