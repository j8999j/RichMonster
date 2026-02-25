using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using GameSystem;
using System.Threading.Tasks;

public class DataManager : Singleton<DataManager>
{
    // 資料字典 - 由 GameDataLoader 載入
    private Dictionary<string, ItemTags> _itemTagsDict = new Dictionary<string, ItemTags>();
    private Dictionary<string, ItemDefinition> _itemDict = new Dictionary<string, ItemDefinition>();
    private Dictionary<string, ProfessionDefinition> _professionDict = new Dictionary<string, ProfessionDefinition>();
    private Dictionary<string, TraitDefinition> _traitDict = new Dictionary<string, TraitDefinition>();
    private Dictionary<string, MonsterProfessionDefinition> _monsterProfessionDict = new Dictionary<string, MonsterProfessionDefinition>();
    private Dictionary<string, MonsterTraitDefinition> _monsterTraitDict = new Dictionary<string, MonsterTraitDefinition>();
    private Dictionary<string, GameEventDefinition> _eventDict = new Dictionary<string, GameEventDefinition>();
    private Dictionary<string, ShopDefinition> _shopDict = new Dictionary<string, ShopDefinition>();
    private Dictionary<string, HumanLargeOrder> _humanLargeOrderDict = new Dictionary<string, HumanLargeOrder>();
    private Dictionary<string, HumanSmallOrder> _humanSmallOrderDict = new Dictionary<string, HumanSmallOrder>();
    private Dictionary<string, NpcMission> _missionDict = new Dictionary<string, NpcMission>();
    private Dictionary<string, AchievementConfig> _achievementDict = new Dictionary<string, AchievementConfig>();

    private PlayerData _initialPlayerData;
    private PlayerData _currentPlayerData;
    private GameSaveBook _bookData;
    private Dictionary<string, IAchievementSave> _achievementSaveDict = new Dictionary<string, IAchievementSave>();
    public bool OnPlayerDataChanged { get; private set; } = true;
    public bool OnBookDataChanged { get; private set; } = true;

    // Read-only accessors
    public IReadOnlyDictionary<string, ItemTags> ItemTagsDict => _itemTagsDict;
    public IReadOnlyDictionary<string, ItemDefinition> ItemDict => _itemDict;
    public IReadOnlyDictionary<string, MonsterProfessionDefinition> MonsterProfessionDict => _monsterProfessionDict;
    public IReadOnlyDictionary<string, MonsterTraitDefinition> MonsterTraitDict => _monsterTraitDict;
    public IReadOnlyDictionary<string, ShopDefinition> ShopDict => _shopDict;
    public IReadOnlyDictionary<string, GameEventDefinition> EventDict => _eventDict;
    public IReadOnlyDictionary<string, HumanLargeOrder> HumanLargeOrderDict => _humanLargeOrderDict;
    public IReadOnlyDictionary<string, HumanSmallOrder> HumanSmallOrderDict => _humanSmallOrderDict;
    public IReadOnlyDictionary<string, NpcMission> MissionDict => _missionDict;
    public IReadOnlyDictionary<string, AchievementConfig> AchievementDict => _achievementDict;
    public PlayerData InitialPlayerData => ClonePlayerData(_initialPlayerData);
    public IReadOnlyPlayerData CurrentPlayerData => _currentPlayerData;

    public bool IsInitialized { get; private set; }
    private Task _initTask;

    public event Action<int, int, DayPhase> PlayerMainViewUpdate;

    protected override void Awake()
    {
        base.Awake();
        _initTask = InitializeAsync();
    }

    public Task WhenInitialized() => _initTask;

    private async Task InitializeAsync()
    {
        await LoadGameDataAsync();
        IsInitialized = true;
    }

    public async Task LoadGameDataAsync()
    {
        var loader = new GameDataLoader();
        var result = await loader.LoadAllGameDataAsync();

        // 將載入結果設定到各個字典
        _itemTagsDict = result.ItemTagsDict;
        _itemDict = result.ItemDict;
        _shopDict = result.ShopDict;
        _monsterProfessionDict = result.MonsterProfessionDict;
        _monsterTraitDict = result.MonsterTraitDict;
        _humanLargeOrderDict = result.HumanLargeOrderDict;
        _humanSmallOrderDict = result.HumanSmallOrderDict;
        _missionDict = result.MissionDict;
        _achievementDict = result.AchievementDict;
        _eventDict = result.EventDict;
        _initialPlayerData = result.InitialPlayerData;
        _bookData = result.BookData;

        // 同步圖鑑快取到 SaveManager
        SaveManager.Instance.SetBookDataCache(_bookData);

        // 將成就存檔 List 轉為 Dictionary 使用
        _achievementSaveDict = SaveManager.Instance.GetAchievementDict();

        // 初始化成就系統
        AchievementManager.Instance.Initialize(_achievementDict);

        _currentPlayerData = ClonePlayerData(_initialPlayerData);
    }

    #region Data Queries
    public List<TraitDefinition> GetTraits(string effectType)
    {
        if (_traitDict == null) return new List<TraitDefinition>();
        return _traitDict.Values
            .Where(evt => evt.OtherEffect != null && evt.OtherEffect.Any(e => e.EffectType == effectType))
            .ToList();
    }

    public List<GameEventDefinition> GetEventsByPeriod(EventTime period)
    {
        if (_eventDict == null) return new List<GameEventDefinition>();
        return _eventDict.Values
            .Where(evt => evt.EventTimes.Contains(period))
            .ToList();
    }

    public NpcMission GetMissionById(string missionId)
    {
        if (_missionDict != null && _missionDict.TryGetValue(missionId, out var mission))
        {
            return mission;
        }
        return null;
    }

    public List<NpcMission> GetAllMissions()
    {
        if (_missionDict == null) return new List<NpcMission>();
        return _missionDict.Values.ToList();
    }

    public List<ItemDefinition> GetItemsByShopType(string shopType)
    {
        if (_itemDict == null || string.IsNullOrEmpty(shopType)) return new List<ItemDefinition>();
        return _itemDict.Values
            .Where(item => item != null && item.ShopType != null && item.ShopType.Contains(shopType))
            .ToList();
    }

    public ItemDefinition GetItemById(string itemId)
    {
        if (_itemDict != null && _itemDict.TryGetValue(itemId, out var item))
        {
            return item;
        }
        return null;
    }

    public string GetTagNameByTag(string tag)
    {
        if (_itemTagsDict == null || string.IsNullOrEmpty(tag) || !_itemTagsDict.ContainsKey(tag)) return "";
        return _itemTagsDict[tag].TagName;
    }
    #endregion

    #region Book Data Management (圖鑑資料管理)
    /// <summary>
    /// 取得圖鑑資料
    /// </summary>
    public GameSaveBook GetBookData()
    {
        return _bookData;
    }

    /// <summary>
    /// 新增物品到物品圖鑑
    /// </summary>
    public void AddItemToBook(string itemId)
    {
        if (_bookData == null) return;
        
        var existing = _bookData.ItemBookData.ItemBooks.Find(x => x.ItemID == itemId);
        if (existing != null)
        {
            existing.IsBooked = true;
        }
        else
        {
            _bookData.ItemBookData.ItemBooks.Add(new ItemBookDatabase
            {
                ItemID = itemId,
                IsBooked = true
            });
        }
        
        SaveManager.Instance.SaveBookData(_bookData);
    }

    /// <summary>
    /// 解鎖妖怪圖鑑資訊
    /// </summary>
    public void UnlockMonsterInformation(string informationId)
    {
        if (_bookData == null) return;
        
        if (!_bookData.MonsterBookData.UnlockMonsterInformationID.Contains(informationId))
        {
            _bookData.MonsterBookData.UnlockMonsterInformationID.Add(informationId);
            SaveManager.Instance.SaveBookData(_bookData);
        }
    }

    /// <summary>
    /// 檢查物品是否已收錄在圖鑑中
    /// </summary>
    public bool IsItemInBook(string itemId)
    {
        if (_bookData == null) return false;
        var item = _bookData.ItemBookData.ItemBooks.Find(x => x.ItemID == itemId);
        return item != null && item.IsBooked;
    }

    /// <summary>
    /// 檢查妖怪資訊是否已解鎖
    /// </summary>
    public bool IsMonsterInfoUnlocked(string informationId)
    {
        if (_bookData == null) return false;
        return _bookData.MonsterBookData.UnlockMonsterInformationID.Contains(informationId);
    }
    /// <summary>
    /// 檢查成就是否已完成 (從字典中查詢)
    /// </summary>
    public bool IsAchievementCompleted(string achievementId)
    {
        if (_achievementSaveDict.TryGetValue(achievementId, out var save))
        {
            return save.IsCompleted;
        }
        return false;
    }

    /// <summary>
    /// 取得成就存檔資料 (從字典中查詢)
    /// </summary>
    public IAchievementSave GetAchievementSaveData(string achievementId)
    {
        _achievementSaveDict.TryGetValue(achievementId, out var save);
        return save;
    }

    /// <summary>
    /// 取得所有成就存檔資料 (字典)
    /// </summary>
    public Dictionary<string, IAchievementSave> GetAllAchievementSaveData()
    {
        return _achievementSaveDict;
    }

    /// <summary>
    /// 更新單筆成就存檔資料 (新增或覆蓋)
    /// </summary>
    public void UpdateAchievementSaveData(IAchievementSave saveData)
    {
        if (saveData == null || string.IsNullOrEmpty(saveData.AchievementID)) return;

        _achievementSaveDict[saveData.AchievementID] = saveData;
        OnBookDataChanged = true;
        SaveManager.Instance.SaveAchievementData(_achievementSaveDict);
    }

    /// <summary>
    /// 從 AchievementManager 取得所有成就實例並批次更新存檔資料
    /// </summary>
    public void UpdateAllAchievementSaveData()
    {
        var allAchievements = AchievementManager.Instance.GetCompletedAchievements();
        allAchievements.AddRange(AchievementManager.Instance.GetIncompleteAchievements());

        foreach (var achievement in allAchievements)
        {
            if (!string.IsNullOrEmpty(achievement.AchievementID))
            {
                _achievementSaveDict[achievement.AchievementID] = achievement;
            }
        }
        OnBookDataChanged = true;
        SaveManager.Instance.SaveAchievementData(_achievementSaveDict);
    }

    /// <summary>
    /// 非同步儲存成就資料
    /// </summary>
    public async Task SaveAchievementAsync()
    {
        await SaveManager.Instance.SaveAchievementDataAsync(_achievementSaveDict);
        OnBookDataChanged = false;
    }
    #endregion

    #region Player Data Utilities
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    private static PlayerData ClonePlayerData(PlayerData source)
    {
        if (source == null) return null;
        var json = JsonConvert.SerializeObject(source, _jsonSettings);
        return JsonConvert.DeserializeObject<PlayerData>(json, _jsonSettings);
    }
    #endregion

    #region Player Save/Load
    public async Task SaveCurrentPlayerAsync(int slot = 0)
    {
        var dataToSave = _currentPlayerData ?? _initialPlayerData ?? new PlayerData();
        await SaveManager.Instance.SaveGameAsync(dataToSave, slot);
    }
    public void LoadPlayerFromSave(int slot = 0)
    {
        var save = SaveManager.Instance.Load(slot);
        _currentPlayerData = ClonePlayerData(save?.Player ?? _initialPlayerData ?? new PlayerData());
    }

    public void SetCurrentPlayer(PlayerData data)
    {
        _currentPlayerData = ClonePlayerData(data);
    }
    public async Task SaveBookAsync()
    {
        if (OnBookDataChanged)
        {
            await SaveManager.Instance.SaveBookDataAsync(GetBookData());
            OnBookDataChanged = false;
        }
    }
    public void SetPlayerDataChanged(bool value)
    {
        OnPlayerDataChanged = value;
    }

    public void SetBookDataChanged(bool value)
    {
        OnBookDataChanged = value;
    }


    #endregion

    #region ModifyPlayerAPI
    public T GetPlayerData<T>(string key) where T : class, ISaveData, new()
    {
        if (_currentPlayerData == null)
        {
            Debug.LogError("[DataManager] _currentPlayerData is null");
            return new T();
        }
        if (_currentPlayerData.GameSaveFile == null)
        {
            _currentPlayerData.GameSaveFile = new GameSaveFile();
            _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();
            return new T();
        }
        if (_currentPlayerData.GameSaveFile.GameData == null)
        {
            _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();
            return new T();
        }
        if (!_currentPlayerData.GameSaveFile.GameData.ContainsKey(key))
        {
            return new T();
        }
        T data = _currentPlayerData.GameSaveFile.GameData[key] as T;
        if (data != null && data.LastUpdatedDay != _currentPlayerData.DaysPlayed)
        {
            return new T();
        }
        return data;
    }
    /// <summary>
    /// 修改金幣 (正數為獲得，負數為扣除)
    /// </summary>
    public void ModifyGold(int amount)
    {
        if (_currentPlayerData == null) return;
        
        _currentPlayerData.Gold += amount;
        if (_currentPlayerData.Gold < 0) _currentPlayerData.Gold = 0;
        AchievementEvents.GoldChanged(_currentPlayerData.Gold, amount);
        AdjustUpdateView();
    }
    /// <summary>
    /// 修改妖怪金幣 (正數為獲得，負數為扣除)
    /// </summary>
    public void ModifyMonsterGold(int amount)
    {
        if (_currentPlayerData == null) return;

        _currentPlayerData.MonsterGold += amount;
        if (_currentPlayerData.MonsterGold < 0) _currentPlayerData.MonsterGold = 0;

        AdjustUpdateView();
    }

    /// <summary>
    /// 嘗試消費金幣 (如果足夠則扣除並回傳 true，否則 false)
    /// </summary>
    public bool TrySpendGold(int amount)
    {
        if (_currentPlayerData == null) return false;
        if (_currentPlayerData.Gold >= amount)
        {
            ModifyGold(-amount);
            return true;
        }
        return false;
    }

    public bool TrySpendMonsterGold(int amount)
    {
        if (_currentPlayerData == null) return false;
        if (_currentPlayerData.MonsterGold >= amount)
        {
            ModifyMonsterGold(-amount);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 加入物品到玩家背包
    /// </summary>
    public void AddItem(string itemId, int costPrice)
    {
        if (_currentPlayerData == null) return;
        if (_currentPlayerData.Inventory == null) _currentPlayerData.Inventory = new Inventory();

        var newItem = new Item
        {
            ItemId = itemId,
            CostPrice = costPrice
        };
        _currentPlayerData.Inventory.Items.Add(newItem);
    }

    /// <summary>
    /// 從背包移除物品 (需同時符合 ID 與 成本)
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (_currentPlayerData?.Inventory?.Items == null) return false;

        var target = _currentPlayerData.Inventory.Items
            .FirstOrDefault(i => i.ItemId == item.ItemId && i.CostPrice == item.CostPrice);

        if (target != null)
        {
            _currentPlayerData.Inventory.Items.Remove(target);
            Debug.Log($"[DataManager] 已移除物品: {item.ItemId} (成本: {item.CostPrice})");
            return true;
        }
        Debug.LogWarning($"[DataManager] 移除失敗，找不到: {item.ItemId} (成本: {item.CostPrice})");
        return false;
    }

    /// <summary>
    /// 取得背包中指定稀有度的物品數量
    /// </summary>
    public int GetItemCountByRarity(Rarity rarity)
    {
        if (_currentPlayerData?.Inventory?.Items == null) return 0;
        return _currentPlayerData.Inventory.Items
            .Count(item => _itemDict.TryGetValue(item.ItemId, out var def) && def.Rarity == rarity);
    }

    /// <summary>
    /// 取得背包中指定物品種類與世界的不重複物品種類數量
    /// </summary>
    public int GetDistinctItemCountByTypeAndWorld(ItemType type, ItemWorld world)
    {
        if (_currentPlayerData?.Inventory?.Items == null) return 0;
        return _currentPlayerData.Inventory.Items
            .Select(item => item.ItemId)
            .Distinct()
            .Count(itemId => _itemDict.TryGetValue(itemId, out var def) && def.Type == type && def.World == world);
    }

    /// <summary>
    /// 新增商店存貨狀態
    /// </summary>
    public void AddShopShelfData(ShopShelfData newShelfData)
    {
        if (_currentPlayerData == null || newShelfData == null) return;

        newShelfData.LastUpdatedDay = _currentPlayerData.DaysPlayed;

        if (_currentPlayerData.GameSaveFile.GameData == null)
            _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();
        
        if (_currentPlayerData.GameSaveFile.GameData.ContainsKey(newShelfData.UniqueID))
        {
            _currentPlayerData.GameSaveFile.GameData[newShelfData.UniqueID] = newShelfData;
        }
        else
        {
            _currentPlayerData.GameSaveFile.GameData.Add(newShelfData.UniqueID, newShelfData);
        }
    }

    /// <summary>
    /// 新增完成訂單紀錄
    /// </summary>
    public void AddOrderProgress(string ID)
    {
        if (_currentPlayerData.GameSaveFile.GameData == null)
            _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();

        if (_currentPlayerData.GameSaveFile.GameData.ContainsKey("OrderHistory"))
        {
            var orderHistoryData = _currentPlayerData.GameSaveFile.GameData["OrderHistory"] as OrderHistoryData;
            if (orderHistoryData.OrderHistory == null)
            {
                orderHistoryData.OrderHistory = new List<OrderProgress>();
            }
            orderHistoryData.OrderHistory.Add(new OrderProgress { OrderID = ID, IsCompleted = true });
        }
        else
        {
            _currentPlayerData.GameSaveFile.GameData.Add("OrderHistory", new OrderHistoryData());
            var orderHistoryData = _currentPlayerData.GameSaveFile.GameData["OrderHistory"] as OrderHistoryData;
            if (orderHistoryData.OrderHistory == null)
            {
                orderHistoryData.OrderHistory = new List<OrderProgress>();
            }
            orderHistoryData.OrderHistory.Add(new OrderProgress { OrderID = ID, IsCompleted = true });
        }
    }

    /// <summary>
    /// 清空完成訂單紀錄
    /// </summary>
    public void ClearOrderProgress()
    {
        if (_currentPlayerData.GameSaveFile.GameData == null)
            _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();

        if (_currentPlayerData.GameSaveFile.GameData.ContainsKey("OrderHistory"))
        {
            var orderHistoryData = _currentPlayerData.GameSaveFile.GameData["OrderHistory"] as OrderHistoryData;
            if (orderHistoryData.OrderHistory == null)
            {
                orderHistoryData.OrderHistory = new List<OrderProgress>();
            }
            orderHistoryData.OrderHistory.Clear();
        }
    }

    public void ModifyCurrentDayPhase(DayPhase dayPhase)
    {
        _currentPlayerData.PlayingStatus = dayPhase;
        AdjustUpdateView();
    }

    private void AdjustUpdateView()
    {
        if (_currentPlayerData.PlayingStatus == DayPhase.HumanDay)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed + 1, _currentPlayerData.Gold, _currentPlayerData.PlayingStatus);
        }
        else
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed, _currentPlayerData.MonsterGold, _currentPlayerData.PlayingStatus);
        }
    }

    public void ShowPlayerMainData()
    {
        AdjustUpdateView();
    }

    public void ModifyCurrentDay(int CurrentDay)
    {
        _currentPlayerData.DaysPlayed = CurrentDay;
        AdjustUpdateView();
    }
    #endregion

    #region GetPlayerAPI
    public MonsterTradeProgress LoadMonsterTradeHistory()
    {
        if (_currentPlayerData.GameSaveFile.GameData.ContainsKey("MonsterTradeHistory"))
        {
            return _currentPlayerData.GameSaveFile.GameData["MonsterTradeHistory"] as MonsterTradeProgress;
        }
        else
        {
            return new MonsterTradeProgress();
        }
    }

    public DayPhase GetCurrentDayPhase()
    {
        return CurrentPlayerData.PlayingStatus;
    }

    public int GetCurrentDay()
    {
        return CurrentPlayerData.DaysPlayed;
    }
    #endregion
}
