using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using GameSystem;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

public class DataManager : Singleton<DataManager>
{
    // Dictionaries keyed by string IDs
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

    private PlayerData _initialPlayerData;
    private PlayerData _currentPlayerData;
    // Read-only accessors
    public IReadOnlyDictionary<string, ItemTags> ItemTagsDict => _itemTagsDict;
    public IReadOnlyDictionary<string, ItemDefinition> ItemDict => _itemDict;
    public IReadOnlyDictionary<string, ProfessionDefinition> ProfessionDict => _professionDict;
    public IReadOnlyDictionary<string, TraitDefinition> TraitDict => _traitDict;
    public IReadOnlyDictionary<string, MonsterProfessionDefinition> MonsterProfessionDict => _monsterProfessionDict;
    public IReadOnlyDictionary<string, MonsterTraitDefinition> MonsterTraitDict => _monsterTraitDict;
    public IReadOnlyDictionary<string, ShopDefinition> ShopDict => _shopDict;
    public IReadOnlyDictionary<string, GameEventDefinition> EventDict => _eventDict;
    public IReadOnlyDictionary<string, HumanLargeOrder> HumanLargeOrderDict => _humanLargeOrderDict;
    public IReadOnlyDictionary<string, HumanSmallOrder> HumanSmallOrderDict => _humanSmallOrderDict;
    public PlayerData InitialPlayerData => ClonePlayerData(_initialPlayerData);
    public IReadOnlyPlayerData CurrentPlayerData => _currentPlayerData;
    //LoadKey
    private const string KEY_ITEMS = "items";
    private const string KEY_PROFESSIONS = "professions";
    private const string KEY_TRAITS = "traits";
    private const string KEY_SHOPS = "shops";
    private const string KEY_PLAYER_INIT = "player_init";
    private const string KEY_EVENTS = "events";
    private const string KEY_ITEM_TAGS = "itemtags";
    private const string KEY_MONSTER_PROFESSIONS = "monster_professions";
    private const string KEY_MONSTER_TRAITS = "monster_traits";
    private const string KEY_HUMAN_LARGE_ORDERS = "EventsRequests";
    private const string KEY_HUMAN_SMALL_ORDERS = "HumanEvents";

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
        await LoadItemTagsAsync();
        await LoadItemsAsync();
        await LoadProfessionsAsync();
        await LoadTraitsAsync();
        await LoadShopDataAsync();
        await LoadMonsterProfessionsAsync();
        await LoadMonsterTraitsAsync();
        await LoadHumanLargeOrdersAsync();
        await LoadHumanSmallOrdersAsync();
        await LoadInitialPlayerDataAsync();
        await LoadEventDataAsync();

        _currentPlayerData = ClonePlayerData(_initialPlayerData);
    }
    #region Loaders
    private async Task LoadItemTagsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_ITEM_TAGS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 item_tags (Addressables)");
                return;
            }

            // 嘗試解析 JSON：支援物件格式 {"ItemTags":[...]} 或直接陣列格式 [...]
            List<ItemTags> tagsList = null;

            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                // 直接陣列格式
                tagsList = JsonConvert.DeserializeObject<List<ItemTags>>(jsonFile.text);
            }
            else
            {
                // 物件包裹格式
                ItemTagsDatabase db = JsonConvert.DeserializeObject<ItemTagsDatabase>(jsonFile.text);
                tagsList = db?.ItemTags;
            }

            _itemTagsDict = tagsList?
                .Where(it => it != null && !string.IsNullOrEmpty(it.TagID))
                .GroupBy(it => it.TagID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ItemTags>();

            Debug.Log($"[DataManager] 載入 {_itemTagsDict.Count} 筆 item_tags 資料");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadItemTagsAsync failed: {e}");
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
    private async Task LoadItemsAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_ITEMS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 items (Addressables)");
                Addressables.Release(handle);
                return;
            }

            ItemDatabase db = JsonConvert.DeserializeObject<ItemDatabase>(jsonFile.text);

            _itemDict = db?.Items?
                .Where(i => i != null && !string.IsNullOrEmpty(i.Id))
                .GroupBy(i => i.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ItemDefinition>();

            Debug.Log($"[DataManager] 載入 {_itemDict.Count} 筆物品資料");

            Addressables.Release(handle);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadItemsAsync failed: {e.Message}");
        }
    }
    private async Task LoadProfessionsAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_PROFESSIONS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 items (Addressables)");
                Addressables.Release(handle);
                return;
            }

            ProfessionDatabase db = JsonConvert.DeserializeObject<ProfessionDatabase>(jsonFile.text);
            _professionDict = db?.Professions?
                .Where(p => p != null && !string.IsNullOrEmpty(p.ProfessionId))
                .GroupBy(p => p.ProfessionId)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ProfessionDefinition>();

            Debug.Log($"[DataManager] 載入 {_professionDict.Count} 筆職業資料");

            Addressables.Release(handle);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadItemsAsync failed: {e.Message}");
        }
    }
    private async Task LoadTraitsAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_TRAITS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 items (Addressables)");
                Addressables.Release(handle);
                return;
            }

            TraitDatabase db = JsonConvert.DeserializeObject<TraitDatabase>(jsonFile.text);
            _traitDict = db?.Traits?
                .Where(t => t != null && !string.IsNullOrEmpty(t.Id))
                .GroupBy(t => t.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, TraitDefinition>();

            Debug.Log($"[DataManager] 載入 {_traitDict.Count} 筆特質資料");

            Addressables.Release(handle);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadItemsAsync failed: {e.Message}");
        }
    }
    private async Task LoadShopDataAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_SHOPS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 items (Addressables)");
                Addressables.Release(handle);
                return;
            }

            ShopDatabase db = JsonConvert.DeserializeObject<ShopDatabase>(jsonFile.text);
            _shopDict = db?.Shops?
                .Where(s => s != null && !string.IsNullOrEmpty(s.ShopID))
                .GroupBy(s => s.ShopID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ShopDefinition>();

            Debug.Log($"[DataManager] 載入 {_shopDict.Count} 筆商店資料");

            Addressables.Release(handle);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadItemsAsync failed: {e.Message}");
        }
    }
    private async Task LoadMonsterProfessionsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_MONSTER_PROFESSIONS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 monster_professions (Addressables)");
                return;
            }

            // 支援物件格式 {"Professions":[...]} 或直接陣列格式 [...]
            List<MonsterProfessionDefinition> professionsList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                professionsList = JsonConvert.DeserializeObject<List<MonsterProfessionDefinition>>(jsonFile.text);
            }
            else
            {
                MonsterProfessionDatabase db = JsonConvert.DeserializeObject<MonsterProfessionDatabase>(jsonFile.text);
                professionsList = db?.Professions;
            }

            _monsterProfessionDict = professionsList?
                .Where(p => p != null && !string.IsNullOrEmpty(p.Id))
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterProfessionDefinition>();

            Debug.Log($"[DataManager] 載入 {_monsterProfessionDict.Count} 筆妖怪職業資料");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadMonsterProfessionsAsync failed: {e}");
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
    private async Task LoadMonsterTraitsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_MONSTER_TRAITS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 monster_traits (Addressables)");
                return;
            }

            // 支援物件格式 {"Traits":[...]} 或直接陣列格式 [...]
            List<MonsterTraitDefinition> traitsList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                traitsList = JsonConvert.DeserializeObject<List<MonsterTraitDefinition>>(jsonFile.text);
            }
            else
            {
                MonsterTraitDefinitionDatabase db = JsonConvert.DeserializeObject<MonsterTraitDefinitionDatabase>(jsonFile.text);
                traitsList = db?.Traits;
            }

            _monsterTraitDict = traitsList?
                .Where(t => t != null && !string.IsNullOrEmpty(t.Id))
                .GroupBy(t => t.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterTraitDefinition>();

            Debug.Log($"[DataManager] 載入 {_monsterTraitDict.Count} 筆妖怪特質資料");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadMonsterTraitsAsync failed: {e}");
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
    private async Task LoadHumanLargeOrdersAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_HUMAN_LARGE_ORDERS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 human_large_orders (Addressables)");
                return;
            }

            // 嘗試解析 JSON：支援物件格式 {"Orders":[...]} 或直接陣列格式 [...]
            List<HumanLargeOrder> ordersList = null;

            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                ordersList = JsonConvert.DeserializeObject<List<HumanLargeOrder>>(jsonFile.text);
            }
            else
            {
                HumanLargeOrderDatabase db = JsonConvert.DeserializeObject<HumanLargeOrderDatabase>(jsonFile.text);
                ordersList = db?.LargeOrders;
            }

            _humanLargeOrderDict = ordersList?
                .Where(o => o != null && !string.IsNullOrEmpty(o.OrderId))
                .GroupBy(o => o.OrderId)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, HumanLargeOrder>();

            Debug.Log($"[DataManager] 載入 {_humanLargeOrderDict.Count} 筆大型訂單資料");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadHumanLargeOrdersAsync failed: {e}");
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
    private async Task LoadHumanSmallOrdersAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_HUMAN_SMALL_ORDERS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 human_small_orders (Addressables)");
                return;
            }

            // 嘗試解析 JSON：支援物件格式 {"Orders":[...]} 或直接陣列格式 [...]
            List<HumanSmallOrder> ordersList = null;

            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                ordersList = JsonConvert.DeserializeObject<List<HumanSmallOrder>>(jsonFile.text);
            }
            else
            {
                HumanSmallOrderDatabase db = JsonConvert.DeserializeObject<HumanSmallOrderDatabase>(jsonFile.text);
                ordersList = db?.SmallOrders;
            }

            _humanSmallOrderDict = ordersList?
                .Where(o => o != null && !string.IsNullOrEmpty(o.OrderId))
                .GroupBy(o => o.OrderId)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, HumanSmallOrder>();

            Debug.Log($"[DataManager] 載入 {_humanSmallOrderDict.Count} 筆小型訂單資料");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadHumanSmallOrdersAsync failed: {e}");
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
    private async Task LoadInitialPlayerDataAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_PLAYER_INIT);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[DataManager] 找不到 items (Addressables)");
                Addressables.Release(handle);
                return;
            }

            _initialPlayerData = JsonConvert.DeserializeObject<PlayerData>(jsonFile.text) ?? new PlayerData();

            Addressables.Release(handle);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] LoadItemsAsync failed: {e.Message}");
        }
    }
    #endregion
    #region Event Data
    public async Task LoadEventDataAsync()
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_EVENTS);
        TextAsset jsonFile = await handle.Task;

        if (jsonFile == null)
        {
            Debug.LogError("[EventManager] 找不到 events_export.json 檔案");
            return;
        }

        try
        {
            EventDatabase db = JsonConvert.DeserializeObject<EventDatabase>(jsonFile.text);
            _eventDict = db?.Events?
                .Where(evt => evt != null && !string.IsNullOrEmpty(evt.Id))
                .GroupBy(evt => evt.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, GameEventDefinition>();

            Debug.Log($"[EventManager] 載入 {_eventDict.Count} 筆事件");
        }
        catch (Exception e)
        {
            Debug.LogError($"[EventManager] JSON 解析失敗: {e.Message}");
        }
    }
    #endregion

    public List<TraitDefinition> GetTraits(string effectType)
    {
        if (_traitDict == null) return new List<TraitDefinition>();
        return _traitDict.Values
            .Where(evt => evt.OtherEffect != null && evt.OtherEffect.Any(e => e.EffectType == effectType))
            .ToList();
    }

    #region Event Queries
    public List<GameEventDefinition> GetEventsByPeriod(EventTime period)
    {
        if (_eventDict == null) return new List<GameEventDefinition>();
        return _eventDict.Values
            .Where(evt => evt.EventTimes.Contains(period))
            .ToList();
    }
    #endregion

    #region Item Queries
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
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    private static PlayerData ClonePlayerData(PlayerData source)
    {
        if (source == null) return null;
        // Json round-trip clone to avoid accidental shared references
        var json = JsonConvert.SerializeObject(source, _jsonSettings);
        return JsonConvert.DeserializeObject<PlayerData>(json, _jsonSettings);
    }
    #endregion

    #region Player Save/Load
    public async Task SaveCurrentPlayerAsync(int slot = 0)
    {
        // 確保有玩家資料再存檔
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
    public void initialPlayerGameSaveFile()
    {
        _currentPlayerData.GameSaveFile = new GameSaveFile();
        _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();
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
        return _currentPlayerData.GameSaveFile.GameData[key] as T;
    }
    /// <summary>
    /// 取得當前金幣
    /// </summary>
    public int GetGold()
    {
        return _currentPlayerData != null ? _currentPlayerData.Gold : 0;
    }
    /// <summary>
    /// 修改金幣 (正數為獲得，負數為扣除)
    /// </summary>
    public void ModifyGold(int amount)
    {
        if (_currentPlayerData == null) return;

        _currentPlayerData.Gold += amount;
        if (_currentPlayerData.Gold < 0) _currentPlayerData.Gold = 0; // 防止負債

        AdjustUpdateView();
    }
    /// <summary>
    /// 修改妖怪金幣 (正數為獲得，負數為扣除)
    /// </summary>
    public void ModifyMonsterGold(int amount)
    {
        if (_currentPlayerData == null) return;

        _currentPlayerData.MonsterGold += amount;
        if (_currentPlayerData.MonsterGold < 0) _currentPlayerData.MonsterGold = 0; // 防止負債

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
    /// 從背包移除物品 (需同時符合 ID 與 品質)
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="quality">物品品質</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveItem(Item item)
    {
        if (_currentPlayerData?.Inventory?.Items == null) return false;

        // 尋找第一個 ID、成本 都相符的物件
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
    // --- 商店存貨狀態操作 ---
    /// <summary>
    /// 新增商店存貨狀態
    /// </summary>
    /// <param name="newShelfData">新增的商店購買紀錄</param>
    public void AddShopShelfData(ShopShelfData newShelfData)
    {
        if (_currentPlayerData == null || newShelfData == null) return;

        // 從當前玩家資料取得日期並更新庫存的紀錄日期
        newShelfData.LastUpdatedDay = _currentPlayerData.DaysPlayed;

        // 確保列表已初始化
        if (_currentPlayerData.GameSaveFile.GameData == null)
            _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();
        
        // 如果已存在相同ID的資料，則更新；否則新增
        if (_currentPlayerData.GameSaveFile.GameData.ContainsKey(newShelfData.UniqueID))
        {
            // 更新現有資料
            _currentPlayerData.GameSaveFile.GameData[newShelfData.UniqueID] = newShelfData;
        }
        else
        {
            // 加入新的資料
            _currentPlayerData.GameSaveFile.GameData.Add(newShelfData.UniqueID, newShelfData);
        }
    }
    //新增完成訂單紀錄
    public void AddOrderProgress(string ID)
    {
        if (_currentPlayerData.GameSaveFile.GameData == null)
        _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();
        if(_currentPlayerData.GameSaveFile.GameData.ContainsKey("OrderHistory"))
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
    //清空完成訂單紀錄
    public void ClearOrderProgress()
    {
        if (_currentPlayerData.GameSaveFile.GameData == null)
        _currentPlayerData.GameSaveFile.GameData = new Dictionary<string, ISaveData>();
        if(_currentPlayerData.GameSaveFile.GameData.ContainsKey("OrderHistory"))
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
    private void AdjustUpdateView()//廣播調整日期後的主畫面更新
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
        if(_currentPlayerData.GameSaveFile.GameData.ContainsKey("MonsterTradeHistory"))
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
