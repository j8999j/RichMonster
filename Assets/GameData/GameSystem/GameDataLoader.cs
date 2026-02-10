using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

/// <summary>
/// 遊戲資料載入器 - 負責從 Addressables 載入所有遊戲設定資料
/// </summary>
public class GameDataLoader
{
    // Addressable Keys
    private const string KEY_ITEMS = "items";
    private const string KEY_SHOPS = "shops";
    private const string KEY_PLAYER_INIT = "player_init";
    private const string KEY_EVENTS = "events";
    private const string KEY_ITEM_TAGS = "itemtags";
    private const string KEY_MONSTER_PROFESSIONS = "monster_professions";
    private const string KEY_MONSTER_TRAITS = "monster_traits";
    private const string KEY_HUMAN_LARGE_ORDERS = "EventsRequests";
    private const string KEY_HUMAN_SMALL_ORDERS = "HumanEvents";
    private const string KEY_MISSIONS = "NpcMission";
    private const string KEY_ACHIEVEMENTS = "Achievements";
    // Book save file
    private const string BOOK_SAVE_FILE = "illustrated_book.json";

    /// <summary>
    /// 載入所有遊戲資料的結果
    /// </summary>
    public class LoadResult
    {
        public Dictionary<string, ItemTags> ItemTagsDict = new Dictionary<string, ItemTags>();
        public Dictionary<string, ItemDefinition> ItemDict = new Dictionary<string, ItemDefinition>();
        public Dictionary<string, MonsterProfessionDefinition> MonsterProfessionDict = new Dictionary<string, MonsterProfessionDefinition>();
        public Dictionary<string, MonsterTraitDefinition> MonsterTraitDict = new Dictionary<string, MonsterTraitDefinition>();
        public Dictionary<string, GameEventDefinition> EventDict = new Dictionary<string, GameEventDefinition>();
        public Dictionary<string, ShopDefinition> ShopDict = new Dictionary<string, ShopDefinition>();
        public Dictionary<string, HumanLargeOrder> HumanLargeOrderDict = new Dictionary<string, HumanLargeOrder>();
        public Dictionary<string, HumanSmallOrder> HumanSmallOrderDict = new Dictionary<string, HumanSmallOrder>();
        public Dictionary<string, NpcMission> MissionDict = new Dictionary<string, NpcMission>();
        public Dictionary<string, AchievementConfig> AchievementDict = new Dictionary<string, AchievementConfig>();
        public PlayerData InitialPlayerData;
        public GameSaveBook BookData;
    }

    /// <summary>
    /// 載入所有遊戲資料
    /// </summary>
    public async Task<LoadResult> LoadAllGameDataAsync()
    {
        var result = new LoadResult();

        result.ItemTagsDict = await LoadItemTagsAsync();
        result.ItemDict = await LoadItemsAsync();
        result.ShopDict = await LoadShopDataAsync();
        result.MonsterProfessionDict = await LoadMonsterProfessionsAsync();
        result.MonsterTraitDict = await LoadMonsterTraitsAsync();
        result.HumanLargeOrderDict = await LoadHumanLargeOrdersAsync();
        result.HumanSmallOrderDict = await LoadHumanSmallOrdersAsync();
        result.MissionDict = await LoadMissionsAsync();
        result.AchievementDict = await LoadAchievementsAsync();
        result.InitialPlayerData = await LoadInitialPlayerDataAsync();
        result.EventDict = await LoadEventDataAsync();
        result.BookData = LoadBookData();

        return result;
    }

    #region Book Data Loader (File System)
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    /// <summary>
    /// 從檔案系統載入圖鑑資料
    /// </summary>
    public GameSaveBook LoadBookData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, BOOK_SAVE_FILE);

        if (!File.Exists(filePath))
        {
            Debug.Log($"[GameDataLoader] 找不到圖鑑存檔，建立新的資料");
            return CreateDefaultBookData();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var bookData = JsonConvert.DeserializeObject<GameSaveBook>(json, _jsonSettings);

            if (bookData == null)
            {
                return CreateDefaultBookData();
            }

            EnsureBookLists(bookData);
            Debug.Log($"[GameDataLoader] 圖鑑讀檔成功: {filePath}");
            return bookData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDataLoader] 圖鑑讀檔失敗: {ex.Message}");
            return CreateDefaultBookData();
        }
    }

    private GameSaveBook CreateDefaultBookData()
    {
        return new GameSaveBook
        {
            ItemBookData = new ItemBookData
            {
                ItemBooks = new List<ItemBookDatabase>()
            },
            MonsterBookData = new MonsterBookData
            {
                UnlockMonsterInformationID = new List<string>()
            }
        };
    }

    private void EnsureBookLists(GameSaveBook bookData)
    {
        if (bookData == null) return;
        bookData.ItemBookData ??= new ItemBookData();
        bookData.ItemBookData.ItemBooks ??= new List<ItemBookDatabase>();
        bookData.MonsterBookData ??= new MonsterBookData();
        bookData.MonsterBookData.UnlockMonsterInformationID ??= new List<string>();
    }
    #endregion

    #region Individual Loaders
    private async Task<Dictionary<string, ItemTags>> LoadItemTagsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_ITEM_TAGS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 item_tags (Addressables)");
                return new Dictionary<string, ItemTags>();
            }

            List<ItemTags> tagsList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                tagsList = JsonConvert.DeserializeObject<List<ItemTags>>(jsonFile.text);
            }
            else
            {
                ItemTagsDatabase db = JsonConvert.DeserializeObject<ItemTagsDatabase>(jsonFile.text);
                tagsList = db?.ItemTags;
            }

            var dict = tagsList?
                .Where(it => it != null && !string.IsNullOrEmpty(it.TagID))
                .GroupBy(it => it.TagID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ItemTags>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆 item_tags 資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadItemTagsAsync failed: {e}");
            return new Dictionary<string, ItemTags>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, ItemDefinition>> LoadItemsAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_ITEMS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 items (Addressables)");
                Addressables.Release(handle);
                return new Dictionary<string, ItemDefinition>();
            }

            ItemDatabase db = JsonConvert.DeserializeObject<ItemDatabase>(jsonFile.text);
            var dict = db?.Items?
                .Where(i => i != null && !string.IsNullOrEmpty(i.Id))
                .GroupBy(i => i.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ItemDefinition>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆物品資料");
            Addressables.Release(handle);
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadItemsAsync failed: {e.Message}");
            return new Dictionary<string, ItemDefinition>();
        }
    }
    private async Task<Dictionary<string, ShopDefinition>> LoadShopDataAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_SHOPS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 shops (Addressables)");
                Addressables.Release(handle);
                return new Dictionary<string, ShopDefinition>();
            }

            ShopDatabase db = JsonConvert.DeserializeObject<ShopDatabase>(jsonFile.text);
            var dict = db?.Shops?
                .Where(s => s != null && !string.IsNullOrEmpty(s.ShopID))
                .GroupBy(s => s.ShopID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ShopDefinition>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆商店資料");
            Addressables.Release(handle);
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadShopDataAsync failed: {e.Message}");
            return new Dictionary<string, ShopDefinition>();
        }
    }

    private async Task<Dictionary<string, MonsterProfessionDefinition>> LoadMonsterProfessionsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_MONSTER_PROFESSIONS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 monster_professions (Addressables)");
                return new Dictionary<string, MonsterProfessionDefinition>();
            }

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

            var dict = professionsList?
                .Where(p => p != null && !string.IsNullOrEmpty(p.Id))
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterProfessionDefinition>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆妖怪職業資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadMonsterProfessionsAsync failed: {e}");
            return new Dictionary<string, MonsterProfessionDefinition>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, MonsterTraitDefinition>> LoadMonsterTraitsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_MONSTER_TRAITS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 monster_traits (Addressables)");
                return new Dictionary<string, MonsterTraitDefinition>();
            }

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

            var dict = traitsList?
                .Where(t => t != null && !string.IsNullOrEmpty(t.Id))
                .GroupBy(t => t.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterTraitDefinition>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆妖怪特質資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadMonsterTraitsAsync failed: {e}");
            return new Dictionary<string, MonsterTraitDefinition>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, HumanLargeOrder>> LoadHumanLargeOrdersAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_HUMAN_LARGE_ORDERS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 human_large_orders (Addressables)");
                return new Dictionary<string, HumanLargeOrder>();
            }

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

            var dict = ordersList?
                .Where(o => o != null && !string.IsNullOrEmpty(o.OrderId))
                .GroupBy(o => o.OrderId)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, HumanLargeOrder>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆大型訂單資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadHumanLargeOrdersAsync failed: {e}");
            return new Dictionary<string, HumanLargeOrder>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, HumanSmallOrder>> LoadHumanSmallOrdersAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_HUMAN_SMALL_ORDERS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 human_small_orders (Addressables)");
                return new Dictionary<string, HumanSmallOrder>();
            }

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

            var dict = ordersList?
                .Where(o => o != null && !string.IsNullOrEmpty(o.OrderId))
                .GroupBy(o => o.OrderId)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, HumanSmallOrder>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆小型訂單資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadHumanSmallOrdersAsync failed: {e}");
            return new Dictionary<string, HumanSmallOrder>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, NpcMission>> LoadMissionsAsync()
    {
        AsyncOperationHandle<IList<NpcMission>> handle = default;
        try
        {
            handle = Addressables.LoadAssetsAsync<NpcMission>(KEY_MISSIONS, null);
            IList<NpcMission> results = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && results != null)
            {
                var dict = results
                    .Where(m => m != null && !string.IsNullOrEmpty(m.MissionID))
                    .GroupBy(m => m.MissionID)
                    .ToDictionary(g => g.Key, g => g.First());

                Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆任務資料");
                return dict;
            }
            else
            {
                Debug.LogError("[GameDataLoader] 任務載入失敗！");
                return new Dictionary<string, NpcMission>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadMissionsAsync failed: {e}");
            return new Dictionary<string, NpcMission>();
        }
        // 注意：ScriptableObject 資產不需要 Release
    }

    private async Task<PlayerData> LoadInitialPlayerDataAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_PLAYER_INIT);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 player_init (Addressables)");
                Addressables.Release(handle);
                return new PlayerData();
            }

            var data = JsonConvert.DeserializeObject<PlayerData>(jsonFile.text) ?? new PlayerData();
            Debug.Log("[GameDataLoader] 載入初始玩家資料");
            Addressables.Release(handle);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadInitialPlayerDataAsync failed: {e.Message}");
            return new PlayerData();
        }
    }

    private async Task<Dictionary<string, GameEventDefinition>> LoadEventDataAsync()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(KEY_EVENTS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 events (Addressables)");
                return new Dictionary<string, GameEventDefinition>();
            }

            EventDatabase db = JsonConvert.DeserializeObject<EventDatabase>(jsonFile.text);
            var dict = db?.Events?
                .Where(evt => evt != null && !string.IsNullOrEmpty(evt.Id))
                .GroupBy(evt => evt.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, GameEventDefinition>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆事件資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadEventDataAsync failed: {e.Message}");
            return new Dictionary<string, GameEventDefinition>();
        }
    }

    private async Task<Dictionary<string, AchievementConfig>> LoadAchievementsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_ACHIEVEMENTS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 Achievements (Addressables)");
                return new Dictionary<string, AchievementConfig>();
            }

            List<AchievementConfig> achievementList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                achievementList = JsonConvert.DeserializeObject<List<AchievementConfig>>(jsonFile.text);
            }
            else
            {
                AchievementDatabase db = JsonConvert.DeserializeObject<AchievementDatabase>(jsonFile.text);
                achievementList = db?.Achievements;
            }

            var dict = achievementList?
                .Where(a => a != null && !string.IsNullOrEmpty(a.AchievementID))
                .GroupBy(a => a.AchievementID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, AchievementConfig>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆成就資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadAchievementsAsync failed: {e}");
            return new Dictionary<string, AchievementConfig>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }
    #endregion
}
