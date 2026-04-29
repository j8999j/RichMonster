using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

/// <summary>
/// 遊戲資料載入器 - 負責從 Addressables 載入所有遊戲設定資料
/// </summary>
public class GameDataLoader
{
    public const string DIALOGUE_LABEL = "Dialogue";

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
    private const string KEY_NPC_DATA = "NPCData";
    private const string KEY_ACHIEVEMENTS = "Achievements";
    private const string KEY_MONSTER_INFO = "MonsterInfo";
    private const string KEY_MONSTER_STORY = "MonsterStory";
    private const string KEY_ACHIEVEMENT_SOUVENIRS = "Souvenirs_Achievement";
    private const string KEY_SPECIAL_SOUVENIRS = "Souvenirs_Special";
    // Book save file
    private const string BOOK_SAVE_FILE = "illustrated_book.json";
    private static readonly Dictionary<string, string> _dialogueTextCache = new Dictionary<string, string>();
    private static Task _dialoguePreloadTask;
    private static readonly ReadOnlyDictionary<string, string> _readonlyDialogueTextCache =
        new ReadOnlyDictionary<string, string>(_dialogueTextCache);

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
        public Dictionary<string, MonsterInformationDatabase> MonsterInfoDict = new Dictionary<string, MonsterInformationDatabase>();
        public Dictionary<string, MonsterStoryDatabase> MonsterStoryDict = new Dictionary<string, MonsterStoryDatabase>();
        public Dictionary<string, NPCMissionData> NPCDataDict = new Dictionary<string, NPCMissionData>();
        public Dictionary<string, AchievementSouvenirData> AchievementSouvenirDict = new Dictionary<string, AchievementSouvenirData>();
        public Dictionary<string, SpecialSouvenirData> SpecialSouvenirDict = new Dictionary<string, SpecialSouvenirData>();
        public PlayerData InitialPlayerData;
        public GameSaveBook BookData;
    }

    /// <summary>
    /// 載入所有遊戲資料
    /// </summary>
    public async Task<LoadResult> LoadAllGameDataAsync()
    {
        var result = new LoadResult();

        await PreloadDialoguesByLabelAsync();
        result.ItemTagsDict = await LoadItemTagsAsync();
        result.ItemDict = await LoadItemsAsync();
        result.ShopDict = await LoadShopDataAsync();
        result.MonsterProfessionDict = await LoadMonsterProfessionsAsync();
        result.MonsterTraitDict = await LoadMonsterTraitsAsync();
        result.HumanLargeOrderDict = await LoadHumanLargeOrdersAsync();
        result.HumanSmallOrderDict = await LoadHumanSmallOrdersAsync();
        result.MissionDict = await LoadMissionsAsync();
        result.AchievementDict = await LoadAchievementsAsync();
        result.MonsterInfoDict = await LoadMonsterInfoAsync();
        result.MonsterStoryDict = await LoadMonsterStoryAsync();
        result.NPCDataDict = await LoadNPCDataAsync();
        result.InitialPlayerData = await LoadInitialPlayerDataAsync();
        result.EventDict = await LoadEventDataAsync();
        result.AchievementSouvenirDict = await LoadAchievementSouvenirsAsync();
        result.SpecialSouvenirDict = await LoadSpecialSouvenirsAsync();
        result.BookData = LoadBookData();

        return result;
    }

    /// <summary>
    /// 取得已預載的對話快取。
    /// </summary>
    public static IReadOnlyDictionary<string, string> CachedDialogueTexts => _readonlyDialogueTextCache;

    /// <summary>
    /// 以 Addressables Label 預載所有對話，並以對話 ID 快取文本。
    /// </summary>
    public static async Task PreloadDialoguesByLabelAsync(string label = DIALOGUE_LABEL)
    {
        if (_dialogueTextCache.Count > 0)
        {
            return;
        }

        _dialoguePreloadTask ??= PreloadDialoguesByLabelInternalAsync(label);
        await _dialoguePreloadTask;
    }

    /// <summary>
    /// 從快取取得單筆對話。若快取尚未建立會先預載。
    /// </summary>
    public static async Task<string> LoadDialogueTextAsync(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId))
        {
            Debug.LogError("[GameDataLoader] 對話 ID 為空");
            return null;
        }

        await PreloadDialoguesByLabelAsync();

        if (_dialogueTextCache.TryGetValue(dialogueId, out string dialogueText))
        {
            return dialogueText;
        }

        Debug.LogError($"[GameDataLoader] 找不到對話文本: {dialogueId}");
        return null;
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
                UnlockMonsterInformationID = new List<string>(),
                NewMonsterInformationID = new List<string>(),
                NewMonsterStoryID = new List<string>()
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
        bookData.MonsterBookData.NewMonsterInformationID ??= new List<string>();
        bookData.MonsterBookData.NewMonsterStoryID ??= new List<string>();
        bookData.UnLockAchievementSouvenirID ??= new List<string>();
        bookData.UnLockSpecialSouvenirID ??= new List<string>();
    }

    #endregion

    #region Dialogue Loader
    private static async Task PreloadDialoguesByLabelInternalAsync(string label)
    {
        AsyncOperationHandle<IList<TextAsset>> handle = default;
        try
        {
            handle = Addressables.LoadAssetsAsync<TextAsset>(label, _ => { });
            IList<TextAsset> textAssets = await handle.Task;

            _dialogueTextCache.Clear();
            if (handle.Status != AsyncOperationStatus.Succeeded || textAssets == null)
            {
                Debug.LogError($"[GameDataLoader] 無法以 Label 載入對話資源: {label}");
                return;
            }

            foreach (TextAsset textAsset in textAssets)
            {
                if (textAsset == null)
                {
                    continue;
                }

                string dialogueId = textAsset.name;
                if (_dialogueTextCache.ContainsKey(dialogueId))
                {
                    Debug.LogError($"[GameDataLoader] 對話 ID 重複，已跳過後續資源: {dialogueId}");
                    continue;
                }

                _dialogueTextCache.Add(dialogueId, textAsset.text);
            }

            Debug.Log($"[GameDataLoader] 已預載 {_dialogueTextCache.Count} 筆對話資源，Label: {label}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDataLoader] 對話 Label 預載失敗: {label}, Error: {ex}");
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
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
    private async Task<Dictionary<string, MonsterInformationDatabase>> LoadMonsterInfoAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_MONSTER_INFO);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 MonsterInfo (Addressables)");
                return new Dictionary<string, MonsterInformationDatabase>();
            }

            List<MonsterInformationDatabase> infoList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                infoList = JsonConvert.DeserializeObject<List<MonsterInformationDatabase>>(jsonFile.text);
            }
            else
            {
                var db = JsonConvert.DeserializeObject<MonsterInformationDatabaseRoot>(jsonFile.text);
                infoList = db?.MonsterInformations;
            }

            var dict = infoList?
                .Where(i => i != null && !string.IsNullOrEmpty(i.InformationID))
                .GroupBy(i => i.InformationID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterInformationDatabase>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆妖怪趣聞資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadMonsterInfoAsync failed: {e}");
            return new Dictionary<string, MonsterInformationDatabase>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, MonsterStoryDatabase>> LoadMonsterStoryAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_MONSTER_STORY);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 MonsterStory (Addressables)");
                return new Dictionary<string, MonsterStoryDatabase>();
            }

            List<MonsterStoryDatabase> storyList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                storyList = JsonConvert.DeserializeObject<List<MonsterStoryDatabase>>(jsonFile.text);
            }
            else
            {
                var db = JsonConvert.DeserializeObject<MonsterStoryDatabaseRoot>(jsonFile.text);
                storyList = db?.MonsterStories;
            }

            var dict = storyList?
                .Where(s => s != null && !string.IsNullOrEmpty(s.MonsterStoryID))
                .GroupBy(s => s.MonsterStoryID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterStoryDatabase>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆妖怪小故事資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadMonsterStoryAsync failed: {e}");
            return new Dictionary<string, MonsterStoryDatabase>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, NPCMissionData>> LoadNPCDataAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_NPC_DATA);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 NPCData (Addressables)");
                return new Dictionary<string, NPCMissionData>();
            }

            List<NPCMissionData> npcList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                npcList = JsonConvert.DeserializeObject<List<NPCMissionData>>(jsonFile.text);
            }
            else
            {
                var db = JsonConvert.DeserializeObject<NPCMissionDataDatabase>(jsonFile.text);
                npcList = db?.NPCMissionData;
            }

            var dict = npcList?
                .Where(n => n != null && !string.IsNullOrEmpty(n.NpcID))
                .GroupBy(n => n.NpcID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, NPCMissionData>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆 NPC 資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadNPCDataAsync failed: {e}");
            return new Dictionary<string, NPCMissionData>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, AchievementSouvenirData>> LoadAchievementSouvenirsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_ACHIEVEMENT_SOUVENIRS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 AchievementSouvenirs (Addressables)");
                return new Dictionary<string, AchievementSouvenirData>();
            }

            List<AchievementSouvenirData> souvenirList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                souvenirList = JsonConvert.DeserializeObject<List<AchievementSouvenirData>>(jsonFile.text);
            }
            else
            {
                var db = JsonConvert.DeserializeObject<AchievementSouvenirDatabaseRoot>(jsonFile.text);
                souvenirList = db?.AchievementSouvenirs;
            }

            var dict = souvenirList?
                .Where(s => s != null && !string.IsNullOrEmpty(s.SouvenirID))
                .GroupBy(s => s.SouvenirID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, AchievementSouvenirData>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆成就紀念品資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadAchievementSouvenirsAsync failed: {e}");
            return new Dictionary<string, AchievementSouvenirData>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private async Task<Dictionary<string, SpecialSouvenirData>> LoadSpecialSouvenirsAsync()
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(KEY_SPECIAL_SOUVENIRS);
            TextAsset jsonFile = await handle.Task;

            if (jsonFile == null)
            {
                Debug.LogError("[GameDataLoader] 找不到 SpecialSouvenirs (Addressables)");
                return new Dictionary<string, SpecialSouvenirData>();
            }

            List<SpecialSouvenirData> souvenirList = null;
            string jsonText = jsonFile.text.TrimStart();
            if (jsonText.StartsWith("["))
            {
                souvenirList = JsonConvert.DeserializeObject<List<SpecialSouvenirData>>(jsonFile.text);
            }
            else
            {
                var db = JsonConvert.DeserializeObject<SpecialSouvenirDatabaseRoot>(jsonFile.text);
                souvenirList = db?.SpecialSouvenirs;
            }

            var dict = souvenirList?
                .Where(s => s != null && !string.IsNullOrEmpty(s.SouvenirID))
                .GroupBy(s => s.SouvenirID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, SpecialSouvenirData>();

            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆特別紀念品資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] LoadSpecialSouvenirsAsync failed: {e}");
            return new Dictionary<string, SpecialSouvenirData>();
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }
    #endregion
}
