using UnityEngine;
using UnityEngine.UI;
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
    public BookMonsterSlot MonsterSlotPrefab;
    public GameObject TagsPrefab;
    public Transform ItemTagCotainer;
    public Transform ItemSlotCotainer;
    public Transform MonsterSlotCotainer;
    [Header("圖鑑按鈕")]
    public Button ItemBookButton;
    public Button MonsterBookButton;
    [Header("物品圖鑑")]
    public TextMeshProUGUI ItemName;
    public TextMeshProUGUI ItemDescription;
    public Image DetailIcon;
    public Image RarityIcon;
    public Image TypeIcon;
    public Image WorldIcon;
    [Header("妖怪圖鑑")]
    public TextMeshProUGUI MonsterName;
    public TextMeshProUGUI RaceName;
    public TextMeshProUGUI MonsterDescription;
    public Image MonsterDetailIcon;
    public Image MonsterRaceIcon;
    public Sprite GhoustRaceSprite;
    public Sprite OrcsRaceSprite;
    public Sprite ProtossRaceSprite;
    public Sprite FairyRaceSprite;
    public Sprite OnButtonSelectSprite;
    public Sprite OffButtonSelectSprite;
    public Button DescriptionButton;
    public Button StoryButton_1;
    public Button StoryButton_2;
    public List<Button> InformationButtonList;
    [Header("篩選按鈕物品")]
    public Button AllButton;
    public Button PropButton;
    public Button FoodButton;
    public Button EquipmentButton;
    public Button AllWorldButton;
    public Button MonsterWorldButton;
    public Button HumanWorldButton;
    [Header("篩選按鈕妖怪")]
    public Button AllMonsterButton;
    public Button GhoustButton;
    public Button OrcsButton;
    public Button ProtossButton;
    public Button FairyButton;
    [Header("圖示")]
    public Sprite PropSprite;//道具
    public Sprite FoodSprite;//食物
    public Sprite EquipmentSprite;//裝備
    public Sprite MonsterTagSprite;//妖界
    public Sprite HumanTagSprite;//人間
    public Sprite nullSprite;
    public int TargetLongEdgeSize = 55;

    // 篩選設定
    private enum TypeFilter { All, Prop, Food, Equipment }
    private enum WorldFilter { All, Human, Monster }
    private enum RaceFilter { All, Ghoust, Orcs, Protoss, Fairy }
    private TypeFilter _currentTypeFilter = TypeFilter.All;
    private WorldFilter _currentWorldFilter = WorldFilter.All;
    private RaceFilter _currentRaceFilter = RaceFilter.All;

    // 種族對應中文名稱
    private static readonly Dictionary<RaceFilter, string> RaceNameMap = new Dictionary<RaceFilter, string>
    {
        { RaceFilter.Ghoust, "幽靈" },
        { RaceFilter.Orcs, "獸族" },
        { RaceFilter.Protoss, "神族" },
        { RaceFilter.Fairy, "妖精" }
    };

    // 物品圖鑑
    private List<BookItemSlot> _activeSlots = new List<BookItemSlot>();
    private List<ItemDefinition> _allItems;
    private List<ItemDefinition> _filteredItems = new List<ItemDefinition>();

    // 妖怪圖鑑
    private List<BookMonsterSlot> _activeMonsterSlots = new List<BookMonsterSlot>();
    private List<MonsterProfessionDefinition> _allMonsters;
    private List<MonsterProfessionDefinition> _filteredMonsters = new List<MonsterProfessionDefinition>();

    // 按鈕正常/變暗顏色
    private readonly Color _activeColor = Color.white;
    private readonly Color _dimColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Awake()
    {
        // 綁定分類篩選按鈕
        if (AllButton != null)
            AllButton.onClick.AddListener(() => SetTypeFilter(TypeFilter.All));
        if (PropButton != null)
            PropButton.onClick.AddListener(() => SetTypeFilter(TypeFilter.Prop));
        if (FoodButton != null)
            FoodButton.onClick.AddListener(() => SetTypeFilter(TypeFilter.Food));
        if (EquipmentButton != null)
            EquipmentButton.onClick.AddListener(() => SetTypeFilter(TypeFilter.Equipment));

        // 綁定世界篩選按鈕
        if (AllWorldButton != null)
            AllWorldButton.onClick.AddListener(() => SetWorldFilter(WorldFilter.All));
        if (HumanWorldButton != null)
            HumanWorldButton.onClick.AddListener(() => SetWorldFilter(WorldFilter.Human));
        if (MonsterWorldButton != null)
            MonsterWorldButton.onClick.AddListener(() => SetWorldFilter(WorldFilter.Monster));

        // 綁定圖鑑切換按鈕
        if (ItemBookButton != null)
            ItemBookButton.onClick.AddListener(SwitchToItemBook);
        if (MonsterBookButton != null)
            MonsterBookButton.onClick.AddListener(SwitchToMonsterBook);

        // 綁定種族篩選按鈕
        if (AllMonsterButton != null)
            AllMonsterButton.onClick.AddListener(() => SetRaceFilter(RaceFilter.All));
        if (GhoustButton != null)
            GhoustButton.onClick.AddListener(() => SetRaceFilter(RaceFilter.Ghoust));
        if (OrcsButton != null)
            OrcsButton.onClick.AddListener(() => SetRaceFilter(RaceFilter.Orcs));
        if (ProtossButton != null)
            ProtossButton.onClick.AddListener(() => SetRaceFilter(RaceFilter.Protoss));
        if (FairyButton != null)
            FairyButton.onClick.AddListener(() => SetRaceFilter(RaceFilter.Fairy));
    }

    #region 圖鑑切換
    /// <summary>
    /// 開啟圖鑑面板
    /// </summary>
    public void ShowBook(bool isItemBook)
    {
        BookPanel.SetActive(true);
        if (isItemBook)
            SwitchToMonsterBook();
        else
            SwitchToItemBook();
    }

    /// <summary>
    /// 切換到物品圖鑑
    /// </summary>
    public void SwitchToItemBook()
    {
        ItemBook.SetActive(true);
        MonsterBook.SetActive(false);
        SetButtonAppearance(ItemBookButton, true);
        SetButtonAppearance(MonsterBookButton, false);
        OpenItemBook();
    }

    /// <summary>
    /// 切換到妖怪圖鑑
    /// </summary>
    public void SwitchToMonsterBook()
    {
        ItemBook.SetActive(false);
        MonsterBook.SetActive(true);
        SetButtonAppearance(ItemBookButton, false);
        SetButtonAppearance(MonsterBookButton, true);
        OpenMonsterBook();
    }
    #endregion

    #region 物品圖鑑
    /// <summary>
    /// 開啟物品圖鑑，顯示所有已載入的物品，已收錄的正常顯示，未收錄的黑色顯示
    /// </summary>
    public void OpenItemBook()
    {
        SaveBook = DataManager.Instance.GetBookData();
        _allItems = DataManager.Instance.ItemDict.Values.ToList();
        _currentTypeFilter = TypeFilter.All;
        _currentWorldFilter = WorldFilter.All;
        ClearItemBookSelected();
        ApplyFilter();
        ShowItemBookSlots();
        UpdateFilterButtonStates();
    }

    /// <summary>
    /// 顯示物品圖鑑所有物品欄位（篩選後）
    /// </summary>
    private void ShowItemBookSlots()
    {
        if (_filteredItems == null || _filteredItems.Count == 0)
        {
            foreach (var slot in _activeSlots)
            {
                slot.gameObject.SetActive(false);
            }
            return;
        }

        // 確保 Slot 數量足夠
        AdjustItemSlotCount(_filteredItems.Count);

        for (int i = 0; i < _filteredItems.Count; i++)
        {
            var itemDef = _filteredItems[i];

            // 檢查 SaveBook 中是否有收錄記錄，有則正常顯示，否則黑色顯示
            bool isBooked = IsItemBooked(itemDef.Id);

            _activeSlots[i].Setup(itemDef.Id, isBooked, OnItemBookSlotSelected);
            _activeSlots[i].gameObject.SetActive(true);
            _activeSlots[i].SetBlack(!isBooked);
        }

        // 隱藏多餘的 Slot
        for (int i = _filteredItems.Count; i < _activeSlots.Count; i++)
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

    private void AdjustItemSlotCount(int targetCount)
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
        if (TypeIcon != null)
        {
            if (slot.CurrentDefinition.Type == ItemType.Prop)
            {
                TypeIcon.sprite = PropSprite;
            }
            else if (slot.CurrentDefinition.Type == ItemType.Food)
            {
                TypeIcon.sprite = FoodSprite;
            }
            else if (slot.CurrentDefinition.Type == ItemType.Equipment)
            {
                TypeIcon.sprite = EquipmentSprite;
            }
        }
        if (WorldIcon != null)
        {
            if (slot.CurrentDefinition.World == ItemWorld.Human)
            {
                WorldIcon.sprite = HumanTagSprite;
            }
            else if (slot.CurrentDefinition.World == ItemWorld.Monster)
            {
                WorldIcon.sprite = MonsterTagSprite;
            }
        }
        string rarityId = slot.CurrentDefinition.Rarity.ToString();
        SpriteLoader.LoadSpriteAsync(rarityId, sprite =>
        {
            if (RarityIcon == null) return;
            if (sprite != null)
            {
                RarityIcon.sprite = sprite;
            }
            else
            {
                RarityIcon.sprite = nullSprite;
            }
        });
        // 顯示標籤
        ShowTags(slot.CurrentDefinition.Tags);
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
        if (RarityIcon != null)
            RarityIcon.sprite = nullSprite;
        if (TypeIcon != null)
            TypeIcon.sprite = nullSprite;
        if (WorldIcon != null)
            WorldIcon.sprite = nullSprite;
        // 清除標籤
        if (ItemTagCotainer != null)
        {
            foreach (Transform child in ItemTagCotainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
    #endregion

    #region 妖怪圖鑑
    /// <summary>
    /// 開啟妖怪圖鑑，顯示所有妖怪，已解鎖的正常顯示，未解鎖的黑色顯示
    /// </summary>
    public void OpenMonsterBook()
    {
        SaveBook = DataManager.Instance.GetBookData();
        _allMonsters = DataManager.Instance.MonsterProfessionDict.Values.ToList();
        _currentRaceFilter = RaceFilter.All;
        ClearMonsterBookSelected();
        ApplyMonsterFilter();
        ShowMonsterBookSlots();
        UpdateMonsterFilterButtonStates();
    }

    /// <summary>
    /// 設定種族篩選並更新顯示
    /// </summary>
    private void SetRaceFilter(RaceFilter filter)
    {
        _currentRaceFilter = filter;
        ClearMonsterBookSelected();
        ApplyMonsterFilter();
        ShowMonsterBookSlots();
        UpdateMonsterFilterButtonStates();
    }

    /// <summary>
    /// 套用妖怪種族篩選條件
    /// </summary>
    private void ApplyMonsterFilter()
    {
        _filteredMonsters.Clear();
        if (_allMonsters == null) return;

        foreach (var monster in _allMonsters)
        {
            if (_currentRaceFilter != RaceFilter.All)
            {
                if (RaceNameMap.TryGetValue(_currentRaceFilter, out string raceName))
                {
                    if (monster.Race != raceName) continue;
                }
            }
            _filteredMonsters.Add(monster);
        }
    }

    /// <summary>
    /// 更新種族篩選按鈕狀態
    /// </summary>
    private void UpdateMonsterFilterButtonStates()
    {
        SetButtonAppearance(AllMonsterButton, _currentRaceFilter == RaceFilter.All);
        SetButtonAppearance(GhoustButton, _currentRaceFilter == RaceFilter.Ghoust);
        SetButtonAppearance(OrcsButton, _currentRaceFilter == RaceFilter.Orcs);
        SetButtonAppearance(ProtossButton, _currentRaceFilter == RaceFilter.Protoss);
        SetButtonAppearance(FairyButton, _currentRaceFilter == RaceFilter.Fairy);
    }

    /// <summary>
    /// 顯示妖怪圖鑑所有欄位（篩選後）
    /// </summary>
    private void ShowMonsterBookSlots()
    {
        if (_filteredMonsters == null || _filteredMonsters.Count == 0)
        {
            foreach (var slot in _activeMonsterSlots)
            {
                slot.gameObject.SetActive(false);
            }
            return;
        }

        // 確保 Slot 數量足夠
        AdjustMonsterSlotCount(_filteredMonsters.Count);

        for (int i = 0; i < _filteredMonsters.Count; i++)
        {
            var monsterDef = _filteredMonsters[i];

            // 檢查是否有任何該妖怪的資訊已解鎖
            bool isUnlocked = IsMonsterUnlocked(monsterDef.Id);

            _activeMonsterSlots[i].Setup(monsterDef, isUnlocked, OnMonsterBookSlotSelected);
            _activeMonsterSlots[i].gameObject.SetActive(true);
            _activeMonsterSlots[i].SetBlack(!isUnlocked);
        }

        // 隱藏多餘的 Slot
        for (int i = _filteredMonsters.Count; i < _activeMonsterSlots.Count; i++)
        {
            _activeMonsterSlots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 檢查妖怪是否有任何資訊已解鎖
    /// </summary>
    private bool IsMonsterUnlocked(string monsterId)
    {
        if (SaveBook == null || SaveBook.MonsterBookData == null
            || SaveBook.MonsterBookData.UnlockMonsterInformationID == null)
            return false;

        // 查詢該妖怪的所有趣聞，檢查是否有任一已解鎖
        var infos = DataManager.Instance.GetMonsterInfosByMonsterID(monsterId);
        foreach (var info in infos)
        {
            if (SaveBook.MonsterBookData.UnlockMonsterInformationID.Contains(info.InformationID))
                return true;
        }
        return false;
    }

    private void AdjustMonsterSlotCount(int targetCount)
    {
        while (_activeMonsterSlots.Count < targetCount)
        {
            BookMonsterSlot newSlot = Instantiate(MonsterSlotPrefab, MonsterSlotCotainer);
            _activeMonsterSlots.Add(newSlot);
        }
    }

    /// <summary>
    /// 妖怪圖鑑欄位被點擊時，顯示妖怪詳細資訊
    /// </summary>
    private void OnMonsterBookSlotSelected(BookMonsterSlot slot, bool isUnlocked)
    {
        ClearMonsterBookSelected();

        if (slot.CurrentDefinition == null) return;

        if (MonsterName != null)
            MonsterName.text = slot.CurrentDefinition.ProfessionName;
        if (RaceName != null)
            RaceName.text = slot.CurrentDefinition.Race;
        if (MonsterDescription != null)
            MonsterDescription.text = slot.CurrentDefinition.Description;

        if (MonsterDetailIcon != null)
        {
            MonsterDetailIcon.sprite = slot.MonsterImage.sprite;
            AdjustImageScale(MonsterDetailIcon);
        }

        // 載入種族圖示
        if (MonsterRaceIcon != null && !string.IsNullOrEmpty(slot.CurrentDefinition.Race))
        {
            switch (slot.CurrentDefinition.Race)
            {
                case "幽靈":
                    MonsterRaceIcon.sprite = GhoustRaceSprite;
                    break;
                case "獸族":
                    MonsterRaceIcon.sprite = OrcsRaceSprite;
                    break;
                case "神族":
                    MonsterRaceIcon.sprite = ProtossRaceSprite;
                    break;
                case "妖精":
                    MonsterRaceIcon.sprite = FairyRaceSprite;
                    break;
            }
        }
    }

    /// <summary>
    /// 清空妖怪圖鑑選中狀態
    /// </summary>
    private void ClearMonsterBookSelected()
    {
        if (MonsterName != null)
            MonsterName.text = "";
        if (RaceName != null)
            RaceName.text = "";
        if (MonsterDescription != null)
            MonsterDescription.text = "";
        if (MonsterDetailIcon != null)
            MonsterDetailIcon.sprite = nullSprite;
        if (MonsterRaceIcon != null)
            MonsterRaceIcon.sprite = nullSprite;
    }
    #endregion

    #region 篩選邏輯
    /// <summary>
    /// 設定物品分類篩選並更新顯示
    /// </summary>
    private void SetTypeFilter(TypeFilter filter)
    {
        _currentTypeFilter = filter;
        ClearItemBookSelected();
        ApplyFilter();
        ShowItemBookSlots();
        UpdateFilterButtonStates();
    }

    /// <summary>
    /// 設定世界篩選並更新顯示
    /// </summary>
    private void SetWorldFilter(WorldFilter filter)
    {
        _currentWorldFilter = filter;
        ClearItemBookSelected();
        ApplyFilter();
        ShowItemBookSlots();
        UpdateFilterButtonStates();
    }

    /// <summary>
    /// 套用篩選條件
    /// </summary>
    private void ApplyFilter()
    {
        _filteredItems.Clear();
        if (_allItems == null) return;

        foreach (var item in _allItems)
        {
            // 分類篩選
            if (_currentTypeFilter != TypeFilter.All)
            {
                switch (_currentTypeFilter)
                {
                    case TypeFilter.Prop:
                        if (item.Type != ItemType.Prop) continue;
                        break;
                    case TypeFilter.Food:
                        if (item.Type != ItemType.Food) continue;
                        break;
                    case TypeFilter.Equipment:
                        if (item.Type != ItemType.Equipment) continue;
                        break;
                }
            }

            // 世界篩選
            if (_currentWorldFilter != WorldFilter.All)
            {
                switch (_currentWorldFilter)
                {
                    case WorldFilter.Human:
                        if (item.World != ItemWorld.Human) continue;
                        break;
                    case WorldFilter.Monster:
                        if (item.World != ItemWorld.Monster) continue;
                        break;
                }
            }

            _filteredItems.Add(item);
        }
    }

    /// <summary>
    /// 更新篩選按鈕狀態：選中的正常顯示，其他變暗
    /// </summary>
    private void UpdateFilterButtonStates()
    {
        // 分類按鈕狀態
        SetButtonAppearance(AllButton, _currentTypeFilter == TypeFilter.All);
        SetButtonAppearance(PropButton, _currentTypeFilter == TypeFilter.Prop);
        SetButtonAppearance(FoodButton, _currentTypeFilter == TypeFilter.Food);
        SetButtonAppearance(EquipmentButton, _currentTypeFilter == TypeFilter.Equipment);

        // 世界按鈕狀態
        SetButtonAppearance(AllWorldButton, _currentWorldFilter == WorldFilter.All);
        SetButtonAppearance(HumanWorldButton, _currentWorldFilter == WorldFilter.Human);
        SetButtonAppearance(MonsterWorldButton, _currentWorldFilter == WorldFilter.Monster);
    }

    /// <summary>
    /// 設定按鈕外觀：選中正常顯示，未選中變暗
    /// </summary>
    private void SetButtonAppearance(Button button, bool isActive)
    {
        if (button == null) return;
        Image btnImage = button.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = isActive ? _activeColor : _dimColor;
        }
    }
    #endregion

    #region 標籤顯示
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
    #endregion

    #region 共用工具
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
    #endregion
}