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
public class GameDataLoader : GameSystem.IGameDataProvider
{
    public const string DIALOGUE_LABEL = "Dialogue";
    public const string CONFIG_JSON_LABEL = "ConfigJson";

    // Addressable Keys — 同時是該 TextAsset 的資產名稱（透過 Label 批次載入後以 name 對應）
    private const string KEY_ITEMS = "items";
    private const string KEY_SHOPS = "shops";
    private const string KEY_PLAYER_INIT = "player_init";
    private const string KEY_EVENTS = "events_Monster";
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
    public class LoadResult : GameSystem.GameDataLoadResult { }

    /// <summary>
    /// 載入所有遊戲資料。
    /// 三條獨立的 IO 流並行：
    ///   1. Dialogue TextAssets (Label = "Dialogue")
    ///   2. NpcMission ScriptableObjects (Key = "NpcMission")
    ///   3. 所有 Config JSON TextAssets (Label = "ConfigJson")，回傳後在 thread pool 並行解析
    /// </summary>
    public async Task<GameSystem.GameDataLoadResult> LoadAllGameDataAsync()
    {
        var result = new GameSystem.GameDataLoadResult();

        Task dialogueTask = PreloadDialoguesByLabelAsync();
        Task<Dictionary<string, NpcMission>> missionsTask = LoadMissionsAsync();
        Task<Dictionary<string, string>> jsonBatchTask = LoadConfigJsonBatchAsync();

        await Task.WhenAll(dialogueTask, missionsTask, jsonBatchTask);

        Dictionary<string, string> jsonByName = jsonBatchTask.Result;

        // Thread pool 並行反序列化所有 Config JSON
        var itemTagsT = Task.Run(() => ParseItemTags(jsonByName));
        var itemsT = Task.Run(() => ParseItems(jsonByName));
        var shopsT = Task.Run(() => ParseShops(jsonByName));
        var professionsT = Task.Run(() => ParseMonsterProfessions(jsonByName));
        var traitsT = Task.Run(() => ParseMonsterTraits(jsonByName));
        var largeOrdersT = Task.Run(() => ParseHumanLargeOrders(jsonByName));
        var smallOrdersT = Task.Run(() => ParseHumanSmallOrders(jsonByName));
        var achievementsT = Task.Run(() => ParseAchievements(jsonByName));
        var monsterInfoT = Task.Run(() => ParseMonsterInfo(jsonByName));
        var monsterStoryT = Task.Run(() => ParseMonsterStory(jsonByName));
        var npcDataT = Task.Run(() => ParseNPCData(jsonByName));
        var playerInitT = Task.Run(() => ParseInitialPlayerData(jsonByName));
        var eventsT = Task.Run(() => ParseEventData(jsonByName));
        var achSouvenirsT = Task.Run(() => ParseAchievementSouvenirs(jsonByName));
        var specialSouvenirsT = Task.Run(() => ParseSpecialSouvenirs(jsonByName));

        await Task.WhenAll(
            itemTagsT, itemsT, shopsT, professionsT, traitsT,
            largeOrdersT, smallOrdersT, achievementsT, monsterInfoT,
            monsterStoryT, npcDataT, playerInitT, eventsT,
            achSouvenirsT, specialSouvenirsT);

        result.ItemTagsDict = itemTagsT.Result;
        result.ItemDict = itemsT.Result;
        result.ShopDict = shopsT.Result;
        result.MonsterProfessionDict = professionsT.Result;
        result.MonsterTraitDict = traitsT.Result;
        result.HumanLargeOrderDict = largeOrdersT.Result;
        result.HumanSmallOrderDict = smallOrdersT.Result;
        result.AchievementDict = achievementsT.Result;
        result.MonsterInfoDict = monsterInfoT.Result;
        result.MonsterStoryDict = monsterStoryT.Result;
        result.NPCDataDict = npcDataT.Result;
        result.InitialPlayerData = playerInitT.Result;
        result.EventDict = eventsT.Result;
        result.AchievementSouvenirDict = achSouvenirsT.Result;
        result.SpecialSouvenirDict = specialSouvenirsT.Result;

        result.MissionDict = missionsTask.Result;
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

    #region Config JSON Batch Loader
    /// <summary>
    /// 以 Addressables Label 一次抓取所有 Config JSON，回傳 {assetName: jsonText} 字典。
    /// 取出 .text（string 是 immutable copy）後即可釋放 handle，反序列化交由 thread pool 並行處理。
    /// </summary>
    private async Task<Dictionary<string, string>> LoadConfigJsonBatchAsync()
    {
        AsyncOperationHandle<IList<TextAsset>> handle = default;
        var dict = new Dictionary<string, string>();
        try
        {
            handle = Addressables.LoadAssetsAsync<TextAsset>(CONFIG_JSON_LABEL, null);
            IList<TextAsset> textAssets = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || textAssets == null)
            {
                Debug.LogError($"[GameDataLoader] 無法以 Label 載入 Config JSON: {CONFIG_JSON_LABEL}");
                return dict;
            }

            foreach (TextAsset ta in textAssets)
            {
                if (ta == null) continue;
                if (dict.ContainsKey(ta.name))
                {
                    Debug.LogError($"[GameDataLoader] Config JSON 名稱重複，已跳過: {ta.name}");
                    continue;
                }
                dict[ta.name] = ta.text;
            }

            Debug.Log($"[GameDataLoader] Config JSON Label 載入 {dict.Count} 筆");
            return dict;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDataLoader] LoadConfigJsonBatchAsync 失敗: {ex}");
            return dict;
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    private static bool TryGetJson(Dictionary<string, string> jsonByName, string key, out string json)
    {
        if (jsonByName.TryGetValue(key, out json) && !string.IsNullOrEmpty(json))
            return true;
        Debug.LogError($"[GameDataLoader] Config JSON 中找不到 {key}（請確認 Addressable 資產 \"{key}\" 已加上 \"{CONFIG_JSON_LABEL}\" Label）");
        return false;
    }
    #endregion

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

    #region Mission Loader (ScriptableObject)
    /// <summary>
    /// 任務是 ScriptableObject，走獨立 Addressables Label。
    /// </summary>
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
        // 註：ScriptableObject 資產不需要 Release
    }
    #endregion

    #region Parsers (thread-safe, no Unity API calls)
    private static Dictionary<string, ItemTags> ParseItemTags(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_ITEM_TAGS, out string json))
            return new Dictionary<string, ItemTags>();
        try
        {
            List<ItemTags> tagsList;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                tagsList = JsonConvert.DeserializeObject<List<ItemTags>>(json);
            else
                tagsList = JsonConvert.DeserializeObject<ItemTagsDatabase>(json)?.ItemTags;

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
            Debug.LogError($"[GameDataLoader] ParseItemTags failed: {e}");
            return new Dictionary<string, ItemTags>();
        }
    }

    private static Dictionary<string, ItemDefinition> ParseItems(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_ITEMS, out string json))
            return new Dictionary<string, ItemDefinition>();
        try
        {
            ItemDatabase db = JsonConvert.DeserializeObject<ItemDatabase>(json);
            var dict = db?.Items?
                .Where(i => i != null && !string.IsNullOrEmpty(i.Id))
                .GroupBy(i => i.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ItemDefinition>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆物品資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseItems failed: {e.Message}");
            return new Dictionary<string, ItemDefinition>();
        }
    }

    private static Dictionary<string, ShopDefinition> ParseShops(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_SHOPS, out string json))
            return new Dictionary<string, ShopDefinition>();
        try
        {
            ShopDatabase db = JsonConvert.DeserializeObject<ShopDatabase>(json);
            var dict = db?.Shops?
                .Where(s => s != null && !string.IsNullOrEmpty(s.ShopID))
                .GroupBy(s => s.ShopID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, ShopDefinition>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆商店資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseShops failed: {e.Message}");
            return new Dictionary<string, ShopDefinition>();
        }
    }

    private static Dictionary<string, MonsterProfessionDefinition> ParseMonsterProfessions(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_MONSTER_PROFESSIONS, out string json))
            return new Dictionary<string, MonsterProfessionDefinition>();
        try
        {
            List<MonsterProfessionDefinition> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<MonsterProfessionDefinition>>(json);
            else
                list = JsonConvert.DeserializeObject<MonsterProfessionDatabase>(json)?.Professions;

            var dict = list?
                .Where(p => p != null && !string.IsNullOrEmpty(p.Id))
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterProfessionDefinition>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆妖怪職業資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseMonsterProfessions failed: {e}");
            return new Dictionary<string, MonsterProfessionDefinition>();
        }
    }

    private static Dictionary<string, MonsterTraitDefinition> ParseMonsterTraits(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_MONSTER_TRAITS, out string json))
            return new Dictionary<string, MonsterTraitDefinition>();
        try
        {
            List<MonsterTraitDefinition> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<MonsterTraitDefinition>>(json);
            else
                list = JsonConvert.DeserializeObject<MonsterTraitDefinitionDatabase>(json)?.Traits;

            var dict = list?
                .Where(t => t != null && !string.IsNullOrEmpty(t.Id))
                .GroupBy(t => t.Id)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterTraitDefinition>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆妖怪特質資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseMonsterTraits failed: {e}");
            return new Dictionary<string, MonsterTraitDefinition>();
        }
    }

    private static Dictionary<string, HumanLargeOrder> ParseHumanLargeOrders(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_HUMAN_LARGE_ORDERS, out string json))
            return new Dictionary<string, HumanLargeOrder>();
        try
        {
            List<HumanLargeOrder> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<HumanLargeOrder>>(json);
            else
                list = JsonConvert.DeserializeObject<HumanLargeOrderDatabase>(json)?.LargeOrders;

            var dict = list?
                .Where(o => o != null && !string.IsNullOrEmpty(o.OrderId))
                .GroupBy(o => o.OrderId)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, HumanLargeOrder>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆大型訂單資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseHumanLargeOrders failed: {e}");
            return new Dictionary<string, HumanLargeOrder>();
        }
    }

    private static Dictionary<string, HumanSmallOrder> ParseHumanSmallOrders(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_HUMAN_SMALL_ORDERS, out string json))
            return new Dictionary<string, HumanSmallOrder>();
        try
        {
            List<HumanSmallOrder> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<HumanSmallOrder>>(json);
            else
                list = JsonConvert.DeserializeObject<HumanSmallOrderDatabase>(json)?.SmallOrders;

            var dict = list?
                .Where(o => o != null && !string.IsNullOrEmpty(o.OrderId))
                .GroupBy(o => o.OrderId)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, HumanSmallOrder>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆小型訂單資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseHumanSmallOrders failed: {e}");
            return new Dictionary<string, HumanSmallOrder>();
        }
    }

    private static PlayerData ParseInitialPlayerData(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_PLAYER_INIT, out string json))
            return new PlayerData();
        try
        {
            var data = JsonConvert.DeserializeObject<PlayerData>(json) ?? new PlayerData();
            Debug.Log("[GameDataLoader] 載入初始玩家資料");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseInitialPlayerData failed: {e.Message}");
            return new PlayerData();
        }
    }

    private static Dictionary<string, GameEventDefinition> ParseEventData(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_EVENTS, out string json))
            return new Dictionary<string, GameEventDefinition>();
        try
        {
            EventDatabase db = JsonConvert.DeserializeObject<EventDatabase>(json);
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
            Debug.LogError($"[GameDataLoader] ParseEventData failed: {e.Message}");
            return new Dictionary<string, GameEventDefinition>();
        }
    }

    private static Dictionary<string, AchievementConfig> ParseAchievements(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_ACHIEVEMENTS, out string json))
            return new Dictionary<string, AchievementConfig>();
        try
        {
            List<AchievementConfig> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<AchievementConfig>>(json);
            else
                list = JsonConvert.DeserializeObject<AchievementDatabase>(json)?.Achievements;

            var dict = list?
                .Where(a => a != null && !string.IsNullOrEmpty(a.AchievementID))
                .GroupBy(a => a.AchievementID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, AchievementConfig>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆成就資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseAchievements failed: {e}");
            return new Dictionary<string, AchievementConfig>();
        }
    }

    private static Dictionary<string, MonsterInformationDatabase> ParseMonsterInfo(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_MONSTER_INFO, out string json))
            return new Dictionary<string, MonsterInformationDatabase>();
        try
        {
            List<MonsterInformationDatabase> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<MonsterInformationDatabase>>(json);
            else
                list = JsonConvert.DeserializeObject<MonsterInformationDatabaseRoot>(json)?.MonsterInformations;

            var dict = list?
                .Where(i => i != null && !string.IsNullOrEmpty(i.InformationID))
                .GroupBy(i => i.InformationID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterInformationDatabase>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆妖怪趣聞資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseMonsterInfo failed: {e}");
            return new Dictionary<string, MonsterInformationDatabase>();
        }
    }

    private static Dictionary<string, MonsterStoryDatabase> ParseMonsterStory(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_MONSTER_STORY, out string json))
            return new Dictionary<string, MonsterStoryDatabase>();
        try
        {
            List<MonsterStoryDatabase> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<MonsterStoryDatabase>>(json);
            else
                list = JsonConvert.DeserializeObject<MonsterStoryDatabaseRoot>(json)?.MonsterStories;

            var dict = list?
                .Where(s => s != null && !string.IsNullOrEmpty(s.MonsterStoryID))
                .GroupBy(s => s.MonsterStoryID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, MonsterStoryDatabase>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆妖怪小故事資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseMonsterStory failed: {e}");
            return new Dictionary<string, MonsterStoryDatabase>();
        }
    }

    private static Dictionary<string, NPCMissionData> ParseNPCData(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_NPC_DATA, out string json))
            return new Dictionary<string, NPCMissionData>();
        try
        {
            List<NPCMissionData> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<NPCMissionData>>(json);
            else
                list = JsonConvert.DeserializeObject<NPCMissionDataDatabase>(json)?.NPCMissionData;

            var dict = list?
                .Where(n => n != null && !string.IsNullOrEmpty(n.NpcID))
                .GroupBy(n => n.NpcID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, NPCMissionData>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆 NPC 資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseNPCData failed: {e}");
            return new Dictionary<string, NPCMissionData>();
        }
    }

    private static Dictionary<string, AchievementSouvenirData> ParseAchievementSouvenirs(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_ACHIEVEMENT_SOUVENIRS, out string json))
            return new Dictionary<string, AchievementSouvenirData>();
        try
        {
            List<AchievementSouvenirData> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<AchievementSouvenirData>>(json);
            else
                list = JsonConvert.DeserializeObject<AchievementSouvenirDatabaseRoot>(json)?.AchievementSouvenirs;

            var dict = list?
                .Where(s => s != null && !string.IsNullOrEmpty(s.SouvenirID))
                .GroupBy(s => s.SouvenirID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, AchievementSouvenirData>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆成就紀念品資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseAchievementSouvenirs failed: {e}");
            return new Dictionary<string, AchievementSouvenirData>();
        }
    }

    private static Dictionary<string, SpecialSouvenirData> ParseSpecialSouvenirs(Dictionary<string, string> jsonByName)
    {
        if (!TryGetJson(jsonByName, KEY_SPECIAL_SOUVENIRS, out string json))
            return new Dictionary<string, SpecialSouvenirData>();
        try
        {
            List<SpecialSouvenirData> list;
            string jsonText = json.TrimStart();
            if (jsonText.StartsWith("["))
                list = JsonConvert.DeserializeObject<List<SpecialSouvenirData>>(json);
            else
                list = JsonConvert.DeserializeObject<SpecialSouvenirDatabaseRoot>(json)?.SpecialSouvenirs;

            var dict = list?
                .Where(s => s != null && !string.IsNullOrEmpty(s.SouvenirID))
                .GroupBy(s => s.SouvenirID)
                .ToDictionary(g => g.Key, g => g.First())
                ?? new Dictionary<string, SpecialSouvenirData>();
            Debug.Log($"[GameDataLoader] 載入 {dict.Count} 筆特別紀念品資料");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] ParseSpecialSouvenirs failed: {e}");
            return new Dictionary<string, SpecialSouvenirData>();
        }
    }
    #endregion
}
