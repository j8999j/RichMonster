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
    // 任務分類
    private List<NpcMission> _humanInfoMissions = new List<NpcMission>();
    private List<NpcMission> _humanNonInfoMissions = new List<NpcMission>();
    private List<NpcMission> _monsterInfoMissions = new List<NpcMission>();
    private List<NpcMission> _monsterNonInfoMissions = new List<NpcMission>();
    private Dictionary<string, AchievementConfig> _achievementDict = new Dictionary<string, AchievementConfig>();
    private Dictionary<string, MonsterInformationDatabase> _monsterInfoDict = new Dictionary<string, MonsterInformationDatabase>();
    private Dictionary<string, MonsterStoryDatabase> _monsterStoryDict = new Dictionary<string, MonsterStoryDatabase>();
    private Dictionary<string, NPCMissionData> _npcDataDict = new Dictionary<string, NPCMissionData>();

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
    public IReadOnlyList<NpcMission> HumanInfoMissions => _humanInfoMissions;
    public IReadOnlyList<NpcMission> HumanNonInfoMissions => _humanNonInfoMissions;
    public IReadOnlyList<NpcMission> MonsterInfoMissions => _monsterInfoMissions;
    public IReadOnlyList<NpcMission> MonsterNonInfoMissions => _monsterNonInfoMissions;
    public IReadOnlyDictionary<string, AchievementConfig> AchievementDict => _achievementDict;
    public IReadOnlyDictionary<string, MonsterInformationDatabase> MonsterInfoDict => _monsterInfoDict;
    public IReadOnlyDictionary<string, MonsterStoryDatabase> MonsterStoryDict => _monsterStoryDict;
    public IReadOnlyDictionary<string, NPCMissionData> NPCDataDict => _npcDataDict;
    public PlayerData InitialPlayerData => ClonePlayerData(_initialPlayerData);
    public IReadOnlyPlayerData CurrentPlayerData => _currentPlayerData;

    public bool IsInitialized { get; private set; }
    private Task _initTask;

    public event Action<int, int, DayPhase> PlayerMainViewUpdate;
    public event Action<string, bool> GameFlowNoticeUpdate;

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
        CategorizeMissions();
        _achievementDict = result.AchievementDict;
        _eventDict = result.EventDict;
        _monsterInfoDict = result.MonsterInfoDict;
        _monsterStoryDict = result.MonsterStoryDict;
        _npcDataDict = result.NPCDataDict;
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

    /// <summary>
    /// 將任務按照 MissionWorld 與是否含有 Information 獎勵分成四類
    /// </summary>
    private void CategorizeMissions()
    {
        _humanInfoMissions.Clear();
        _humanNonInfoMissions.Clear();
        _monsterInfoMissions.Clear();
        _monsterNonInfoMissions.Clear();

        foreach (var mission in _missionDict.Values)
        {
            if (mission == null) continue;

            bool hasInfo = mission.Rewards != null
                && mission.Rewards.Exists(r => r.RewardType == RewardType.Information);

            if (mission.MissionWorld == ItemWorld.Human)
            {
                if (hasInfo)
                    _humanInfoMissions.Add(mission);
                else
                    _humanNonInfoMissions.Add(mission);
            }
            else // ItemWorld.Monster
            {
                if (hasInfo)
                    _monsterInfoMissions.Add(mission);
                else
                    _monsterNonInfoMissions.Add(mission);
            }
        }

        Debug.Log($"[DataManager] 任務分類完成 - 人間(情報:{_humanInfoMissions.Count}, 一般:{_humanNonInfoMissions.Count}), 妖界(情報:{_monsterInfoMissions.Count}, 一般:{_monsterNonInfoMissions.Count})");
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
    /// <summary>
    /// 依照 InformationID 查找妖怪趣聞
    /// </summary>
    public MonsterInformationDatabase GetMonsterInfoById(string informationId)
    {
        if (_monsterInfoDict != null && _monsterInfoDict.TryGetValue(informationId, out var info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// 依照 MonsterID 取得該妖怪所有趣聞
    /// </summary>
    public List<MonsterInformationDatabase> GetMonsterInfosByMonsterID(string monsterID)
    {
        if (_monsterInfoDict == null || string.IsNullOrEmpty(monsterID))
            return new List<MonsterInformationDatabase>();
        return _monsterInfoDict.Values
            .Where(info => info.MonsterID == monsterID)
            .ToList();
    }

    /// <summary>
    /// 依照 MonsterStoryID 查找妖怪小故事
    /// </summary>
    public MonsterStoryDatabase GetMonsterStoryById(string storyId)
    {
        if (_monsterStoryDict != null && _monsterStoryDict.TryGetValue(storyId, out var story))
        {
            return story;
        }
        return null;
    }

    /// <summary>
    /// 依照 MonsterID 取得該妖怪所有小故事 (依 StoryIndex 排序)
    /// </summary>
    public List<MonsterStoryDatabase> GetMonsterStoriesByMonsterID(string monsterID)
    {
        if (_monsterStoryDict == null || string.IsNullOrEmpty(monsterID))
            return new List<MonsterStoryDatabase>();
        return _monsterStoryDict.Values
            .Where(s => s.MonsterID == monsterID)
            .OrderBy(s => s.StoryIndex)
            .ToList();
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
    private void AddItemToBook(string itemId)
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

        OnBookDataChanged = true;
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
            OnBookDataChanged = true;
            SaveManager.Instance.SaveBookData(_bookData);
        }
    }

    /// <summary>
    /// 解鎖隨機一個未解鎖的妖怪情報
    /// </summary>
    public void UnlockRandomMonsterInformation()
    {
        var allInfoKeys = _monsterInfoDict.Keys;
        var lockedInfos = new List<string>();
        foreach (var key in allInfoKeys)
        {
            if (!IsMonsterInfoUnlocked(key))
            {
                lockedInfos.Add(key);
            }
        }

        if (lockedInfos.Count > 0)
        {
            string randomInfo = lockedInfos[UnityEngine.Random.Range(0, lockedInfos.Count)];
            UnlockMonsterInformation(randomInfo);
            Debug.Log($"[DataManager] 隨機解鎖妖怪情報: {randomInfo}");
        }
        else
        {
            Debug.Log("[DataManager] 所有妖怪情報已解鎖，無可解鎖項目");
        }
    }


    /// <summary>
    /// 檢查物品是否已收錄在圖鑑中
    /// </summary>
    private bool IsItemInBook(string itemId)
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


    /// <summary>
    /// 新增或更新存檔資料到當前玩家的 GameSaveFile 中
    /// </summary>
    /// <param name="key">存檔資料的唯一鍵值</param>
    /// <param name="data">要儲存的資料 (必須實作 ISaveData)</param>
    public void SetPlayerData<T>(string key, T data) where T : class, ISaveData
    {
        if (_currentPlayerData == null)
        {
            Debug.LogError("[DataManager] _currentPlayerData is null，無法寫入存檔資料");
            return;
        }
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[DataManager] key 不可為空");
            return;
        }
        if (data == null)
        {
            Debug.LogError("[DataManager] data 不可為 null");
            return;
        }

        // 確保 GameSaveFile 與 GameData 存在
        if (_currentPlayerData.GameSaveFile == null)
            _currentPlayerData.GameSaveFile = new GameSaveFile();
        if (_currentPlayerData.GameSaveFile.GameData == null)
            _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();

        _currentPlayerData.GameSaveFile.GameData[key] = data;
        OnPlayerDataChanged = true;

        Debug.Log($"[DataManager] 已寫入存檔資料: key={key}, type={typeof(T).Name}");
    }

    /// <summary>
    /// 修改金幣 (正數為獲得，負數為扣除)
    /// </summary>
    public void ModifyGold(int amount)
    {
        if (_currentPlayerData == null) return;

        _currentPlayerData.Gold += amount;
        if (_currentPlayerData.Gold < 0) _currentPlayerData.Gold = 0;
        OnPlayerDataChanged = true;
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
        OnPlayerDataChanged = true;
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
    /// 設定交易狀態
    /// </summary>
    public void SetIsTrade(bool value)
    {
        if (_currentPlayerData == null) return;
        _currentPlayerData.IsTrade = value;
        OnPlayerDataChanged = true;
    }

    /// <summary>
    /// 加入物品到玩家背包
    /// </summary>
    public void AddItem(string itemId, int costPrice)
    {
        if (_currentPlayerData == null) return;
        if (_currentPlayerData.Inventory == null) _currentPlayerData.Inventory = new Inventory();
        AchievementEvents.GetItem(itemId);
        if (IsItemInBook(itemId) == false)
        {
            AddItemToBook(itemId);
        }
        var newItem = new Item
        {
            ItemId = itemId,
            CostPrice = costPrice
        };
        _currentPlayerData.Inventory.Items.Add(newItem);
        OnPlayerDataChanged = true;
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
            OnPlayerDataChanged = true;
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
    /// 取得背包中人間物品的總數量
    /// </summary>
    public int GetHumanItemCount()
    {
        if (_currentPlayerData?.Inventory?.Items == null) return 0;
        return _currentPlayerData.Inventory.Items
            .Count(item => _itemDict.TryGetValue(item.ItemId, out var def) && def.World == ItemWorld.Human);
    }

    /// <summary>
    /// 取得背包中妖界物品的總數量
    /// </summary>
    public int GetMonsterItemCount()
    {
        if (_currentPlayerData?.Inventory?.Items == null) return 0;
        return _currentPlayerData.Inventory.Items
            .Count(item => _itemDict.TryGetValue(item.ItemId, out var def) && def.World == ItemWorld.Monster);
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
        OnPlayerDataChanged = true;
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
            if (orderHistoryData.OrderHistory == null || orderHistoryData.LastUpdatedDay != _currentPlayerData.DaysPlayed)
            {
                orderHistoryData.OrderHistory = new List<OrderProgress>();
                orderHistoryData.LastUpdatedDay = _currentPlayerData.DaysPlayed;
            }
            orderHistoryData.OrderHistory.Add(new OrderProgress { OrderID = ID, IsCompleted = true });
        }
        else
        {
            _currentPlayerData.GameSaveFile.GameData.Add("OrderHistory", new OrderHistoryData());
            var orderHistoryData = _currentPlayerData.GameSaveFile.GameData["OrderHistory"] as OrderHistoryData;
            if (orderHistoryData.OrderHistory == null || orderHistoryData.LastUpdatedDay != _currentPlayerData.DaysPlayed)
            {
                orderHistoryData.OrderHistory = new List<OrderProgress>();
                orderHistoryData.LastUpdatedDay = _currentPlayerData.DaysPlayed;
            }
            orderHistoryData.OrderHistory.Add(new OrderProgress { OrderID = ID, IsCompleted = true });
        }
        OnPlayerDataChanged = true;
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
            OnPlayerDataChanged = true;
        }
    }

    public void ModifyCurrentDayPhase(DayPhase dayPhase)
    {
        _currentPlayerData.PlayingStatus = dayPhase;
        OnPlayerDataChanged = true;
        AdjustUpdateView();
    }

    private void AdjustUpdateView()
    {
        if(_currentPlayerData.PlayingStatus == DayPhase.HumanDay && _currentPlayerData.IsTrade == true)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed, _currentPlayerData.Gold, _currentPlayerData.PlayingStatus);
            GameFlowNoticeUpdate?.Invoke("採購商品或回家休息一下吧", true);
        }
        else if(_currentPlayerData.PlayingStatus == DayPhase.HumanDay && _currentPlayerData.IsTrade == false)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed + 1, _currentPlayerData.Gold, _currentPlayerData.PlayingStatus);
            GameFlowNoticeUpdate?.Invoke("採購商品並開店確認訂單", true);
        }
        else if(_currentPlayerData.PlayingStatus == DayPhase.AfterNoon)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed, _currentPlayerData.Gold, _currentPlayerData.PlayingStatus);
            GameFlowNoticeUpdate?.Invoke("準備前往妖界", true);
        }
        else if(_currentPlayerData.PlayingStatus == DayPhase.Night && _currentPlayerData.IsTrade == true)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed, _currentPlayerData.MonsterGold, _currentPlayerData.PlayingStatus);
            GameFlowNoticeUpdate?.Invoke("接待結束可選擇回家休息", true);
        }
        else if(_currentPlayerData.PlayingStatus == DayPhase.Night && _currentPlayerData.IsTrade == false)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed, _currentPlayerData.MonsterGold, _currentPlayerData.PlayingStatus);
            GameFlowNoticeUpdate?.Invoke("採購商品並迎接客人", true);
        }
    }
    public void ShowPlayerMainData()
    {
        AdjustUpdateView();
    }

    public void ModifyCurrentDay(int CurrentDay)
    {
        _currentPlayerData.DaysPlayed = CurrentDay;
        OnPlayerDataChanged = true;
        AdjustUpdateView();
    }
    #endregion

    #region GetPlayerSaveDataAPI
    public T GetPlayerSaveData<T>(string key) where T : class, ISaveData, new()
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
    #endregion
}
