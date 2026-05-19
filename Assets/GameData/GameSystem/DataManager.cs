using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using GameSystem;
using System.Threading.Tasks;
using Souvenir;
public class DataManager : Singleton<DataManager>
{
    public const int BaseInventoryCapacity = 25;

    #region Game Static Data (遊戲靜態資料)
    // 資料字典 - 由 GameDataLoader 載入
    private Dictionary<string, ItemTags> _itemTagsDict = new Dictionary<string, ItemTags>();
    private Dictionary<string, ItemDefinition> _itemDict = new Dictionary<string, ItemDefinition>();
    private Dictionary<string, MonsterProfessionDefinition> _monsterProfessionDict = new Dictionary<string, MonsterProfessionDefinition>();
    private Dictionary<string, MonsterTraitDefinition> _monsterTraitDict = new Dictionary<string, MonsterTraitDefinition>();
    private Dictionary<string, GameEventDefinition> _eventDict = new Dictionary<string, GameEventDefinition>();
    private Dictionary<string, ShopDefinition> _shopDict = new Dictionary<string, ShopDefinition>();
    private Dictionary<string, HumanLargeOrder> _humanLargeOrderDict = new Dictionary<string, HumanLargeOrder>();
    private Dictionary<string, HumanSmallOrder> _humanSmallOrderDict = new Dictionary<string, HumanSmallOrder>();
    private Dictionary<string, NpcMission> _missionDict = new Dictionary<string, NpcMission>();
    private Dictionary<string, AchievementConfig> _achievementDict = new Dictionary<string, AchievementConfig>();
    private Dictionary<string, MonsterInformationDatabase> _monsterInfoDict = new Dictionary<string, MonsterInformationDatabase>();
    private Dictionary<string, MonsterStoryDatabase> _monsterStoryDict = new Dictionary<string, MonsterStoryDatabase>();
    private Dictionary<string, NPCMissionData> _npcDataDict = new Dictionary<string, NPCMissionData>();
    private Dictionary<string, AchievementSouvenirData> _achievementSouvenirDict = new Dictionary<string, AchievementSouvenirData>();
    private Dictionary<string, SpecialSouvenirData> _specialSouvenirDict = new Dictionary<string, SpecialSouvenirData>();
    #endregion

    #region Mission Caches (任務分類快取)
    // 任務分類快取清單
    private List<NpcMission> _humanInfoMissions = new List<NpcMission>();
    private List<NpcMission> _humanNonInfoMissions = new List<NpcMission>();
    private List<NpcMission> _monsterInfoMissions = new List<NpcMission>();
    private List<NpcMission> _monsterNonInfoMissions = new List<NpcMission>();
    #endregion

    #region Save & Runtime Data (存檔與即時資料)
    private PlayerData _initialPlayerData;
    private PlayerData _currentPlayerData;
    private GameSaveBook _bookData;
    private Dictionary<string, IAchievementSave> _achievementSaveDict = new Dictionary<string, IAchievementSave>();
    private Dictionary<string, ISpecialSouvenirSave> _specialSouvenirSaveDict = new Dictionary<string, ISpecialSouvenirSave>();
    #endregion

    #region State Flags (狀態旗標)
    /// <summary> 玩家資料是否已變更（用於判斷是否需要存檔） </summary>
    public bool OnPlayerDataChanged { get; private set; } = true;
    /// <summary> 圖鑑資料是否已變更（用於判斷是否需要存檔） </summary>
    public bool OnBookDataChanged { get; private set; } = true;
    /// <summary> 資料管理器是否已完成初始化 </summary>
    public bool IsInitialized { get; private set; }
    private Task _initTask;
    private IGameDataProvider _gameDataProvider;
    private IGameSaveRepository _saveRepository;
    #endregion

    #region Read-only Data Accessors (唯讀屬性)
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
    public IReadOnlyDictionary<string, AchievementSouvenirData> AchievementSouvenirDict => _achievementSouvenirDict;
    public IReadOnlyDictionary<string, SpecialSouvenirData> SpecialSouvenirDict => _specialSouvenirDict;
    public PlayerData InitialPlayerData => ClonePlayerData(_initialPlayerData);
    public IReadOnlyPlayerData CurrentPlayerData => _currentPlayerData;
    #endregion

    #region Events (事件)
    /// <summary> 玩家主畫面資料更新事件 (Day, Gold, PlayingStatus) </summary>
    public event Action<int, int, DayPhase> PlayerMainViewUpdate;
    public event Action OnItemPurchased;
    public event Action BookDataChanged;
    #endregion

    public IGameDataProvider GameDataProvider => _gameDataProvider ??= new GameDataLoader();
    public IGameSaveRepository SaveRepository => _saveRepository ??= SaveManager.Instance;

    public void ConfigureDataSources(IGameDataProvider gameDataProvider = null, IGameSaveRepository saveRepository = null)
    {
        if (IsInitialized)
        {
            Debug.LogWarning("[DataManager] ConfigureDataSources called after initialization. Reload data to apply a new game data provider.");
        }

        if (gameDataProvider != null)
            _gameDataProvider = gameDataProvider;

        if (saveRepository != null)
            _saveRepository = saveRepository;
    }

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
        var result = await GameDataProvider.LoadAllGameDataAsync();

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
        _achievementSouvenirDict = result.AchievementSouvenirDict;
        _specialSouvenirDict = result.SpecialSouvenirDict;
        _initialPlayerData = result.InitialPlayerData;
        _bookData = result.BookData;

        // 同步圖鑑快取到 SaveManager
        SaveRepository.SetBookDataCache(_bookData);

        // 將成就存檔 List 轉為 Dictionary 使用
        _achievementSaveDict = SaveRepository.GetAchievementDict();
        _specialSouvenirSaveDict = SaveRepository.GetSpecialSouvenirDict();

        InitializeProgressManagers();

        _currentPlayerData = ClonePlayerData(_initialPlayerData);
    }

    public void InitializeProgressManagers()
    {
        if (AchievementManager.Instance != null)
        {
            if (AchievementManager.Instance.IsInitialized)
            {
                AchievementManager.Instance.Reset();
            }

            AchievementManager.Instance.Initialize(_achievementDict);
        }

        if (SouvenirManager.Instance != null)
        {
            if (SouvenirManager.Instance.IsInitialized)
            {
                SouvenirManager.Instance.Reset();
            }

            SouvenirManager.Instance.Initialize();
        }
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

    #region Data Queries (資料查詢)
    /// <summary>
    /// 根據時段取得對應的遊戲事件
    /// </summary>
    public List<GameEventDefinition> GetEventsByPeriod(EventTime period)
    {
        if (_eventDict == null) return new List<GameEventDefinition>();
        return _eventDict.Values
            .Where(evt => evt.EventTimes.Contains(period))
            .ToList();
    }

    /// <summary>
    /// 根據任務 ID 取得任務資料
    /// </summary>
    public NpcMission GetMissionById(string missionId)
    {
        if (_missionDict != null && _missionDict.TryGetValue(missionId, out var mission))
        {
            return mission;
        }
        return null;
    }

    /// <summary>
    /// 取得所有任務資料
    /// </summary>
    public List<NpcMission> GetAllMissions()
    {
        if (_missionDict == null) return new List<NpcMission>();
        return _missionDict.Values.ToList();
    }

    /// <summary>
    /// 根據商店類型取得在此販售的所有物品
    /// </summary>
    public List<ItemDefinition> GetItemsByShopType(string shopType)
    {
        if (_itemDict == null || string.IsNullOrEmpty(shopType)) return new List<ItemDefinition>();
        return _itemDict.Values
            .Where(item => item != null && item.ShopType != null && item.ShopType.Contains(shopType))
            .ToList();
    }

    /// <summary>
    /// 根據物品 ID 取得物品定義
    /// </summary>
    public ItemDefinition GetItemById(string itemId)
    {
        if (_itemDict != null && _itemDict.TryGetValue(itemId, out var item))
        {
            return item;
        }
        return null;
    }

    /// <summary>
    /// 根據標籤 ID 取得標籤名稱
    /// </summary>
    public string GetTagNameByTag(string tag)
    {
        if (_itemTagsDict == null || string.IsNullOrEmpty(tag) || !_itemTagsDict.ContainsKey(tag)) return "";
        return _itemTagsDict[tag].TagName;
    }

    /// <summary>
    /// 從所有物品庫抽選指定數量的不重複物品 ID
    /// </summary>
    /// <param name="world">界域</param>
    /// <param name="rarity">稀有度</param>
    /// <param name="count">抽選數量</param>
    /// <returns>隨機的不重複物品 ID 列表</returns>
    public List<string> GetRandomDistinctItemIds(ItemWorld world, Rarity rarity, int count)
    {
        if (_itemDict == null) return new List<string>();

        return _itemDict
            .Where(kvp => kvp.Value != null && kvp.Value.World == world && kvp.Value.Rarity == rarity)
            .Select(kvp => kvp.Key)
            .OrderBy(x => UnityEngine.Random.value)
            .Take(count)
            .ToList();
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
    /// 清空圖鑑資料快取 (由 SaveManager.ClearBookData 呼叫，重置為預設空資料)
    /// </summary>
    public void ClearBookDataCache()
    {
        _bookData = new GameSaveBook
        {
            ItemBookData = new ItemBookData { ItemBooks = new List<ItemBookDatabase>() },
            MonsterBookData = new MonsterBookData
            {
                UnlockMonsterInformationID = new List<string>(),
                NewMonsterInformationID = new List<string>(),
                NewMonsterStoryID = new List<string>()
            },
            AchievementData = new List<IAchievementSave>(),
            SpecialSouvenirProgressData = new List<ISpecialSouvenirSave>(),
            UnLockSpecialSouvenirID = new List<string> { "Sou_key" }
        };
        _achievementSaveDict = new Dictionary<string, IAchievementSave>();
        _specialSouvenirSaveDict = new Dictionary<string, ISpecialSouvenirSave>();
        OnBookDataChanged = false;
        BookDataChanged?.Invoke();
        Debug.Log("[DataManager] 圖鑑資料快取已清空");
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

        MarkBookDataChanged();
        SaveRepository.SaveBookData(_bookData);
    }

    /// <summary>
    /// 解鎖妖怪圖鑑資訊
    /// </summary>
    public void UnlockMonsterInformation(string informationId)
    {
        if (_bookData == null) return;

        if (!_bookData.MonsterBookData.UnlockMonsterInformationID.Contains(informationId))
        {
            // 解鎖前計算該妖怪已解鎖數量（用於判斷是否跨越故事門檻）
            var infoData = GetMonsterInfoById(informationId);
            string monsterId = infoData?.MonsterID;
            int prevCount = 0;
            if (!string.IsNullOrEmpty(monsterId))
            {
                var allInfos = GetMonsterInfosByMonsterID(monsterId);
                foreach (var info in allInfos)
                {
                    if (_bookData.MonsterBookData.UnlockMonsterInformationID.Contains(info.InformationID))
                        prevCount++;
                }
            }

            _bookData.MonsterBookData.UnlockMonsterInformationID.Add(informationId);

            // 記錄為新情報（尚未在圖鑑中確認）
            _bookData.MonsterBookData.NewMonsterInformationID ??= new List<string>();
            _bookData.MonsterBookData.NewMonsterInformationID.Add(informationId);

            // 檢查是否跨越故事門檻（每 2 個情報解鎖 1 個故事）
            if (!string.IsNullOrEmpty(monsterId))
            {
                int newCount = prevCount + 1;
                int prevStoryCount = prevCount / 2;
                int newStoryCount = newCount / 2;
                if (newStoryCount > prevStoryCount)
                {
                    // 新故事解鎖，找到對應的故事並記錄
                    var stories = GetMonsterStoriesByMonsterID(monsterId);
                    if (stories != null && newStoryCount <= stories.Count)
                    {
                        var newStory = stories[newStoryCount - 1]; // StoryIndex 從 0 開始排序
                        _bookData.MonsterBookData.NewMonsterStoryID ??= new List<string>();
                        if (!string.IsNullOrEmpty(newStory.MonsterStoryID))
                        {
                            _bookData.MonsterBookData.NewMonsterStoryID.Add(newStory.MonsterStoryID);
                        }
                    }
                }
            }

            MarkBookDataChanged();
            SaveRepository.SaveBookData(_bookData);
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
    /// 檢查是否有任何尚未確認的新妖怪情報或新故事（全域）
    /// </summary>
    public bool HasAnyNewMonsterInfo()
    {
        if (_bookData == null) return false;
        if (_bookData.MonsterBookData.NewMonsterInformationID != null
            && _bookData.MonsterBookData.NewMonsterInformationID.Count > 0)
            return true;
        if (_bookData.MonsterBookData.NewMonsterStoryID != null
            && _bookData.MonsterBookData.NewMonsterStoryID.Count > 0)
            return true;
        return false;
    }

    /// <summary>
    /// 檢查妖怪是否有尚未確認的新情報或新故事
    /// </summary>
    public bool HasNewMonsterInfo(string monsterId)
    {
        if (_bookData == null || string.IsNullOrEmpty(monsterId)) return false;

        // 檢查新情報
        if (_bookData.MonsterBookData.NewMonsterInformationID != null)
        {
            var infos = GetMonsterInfosByMonsterID(monsterId);
            foreach (var info in infos)
            {
                if (_bookData.MonsterBookData.NewMonsterInformationID.Contains(info.InformationID))
                    return true;
            }
        }

        // 檢查新故事
        if (_bookData.MonsterBookData.NewMonsterStoryID != null)
        {
            var stories = GetMonsterStoriesByMonsterID(monsterId);
            foreach (var story in stories)
            {
                if (_bookData.MonsterBookData.NewMonsterStoryID.Contains(story.MonsterStoryID))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 確認妖怪的新情報與新故事（從 NewMonsterInformationID / NewMonsterStoryID 移除）
    /// </summary>
    public void ConfirmMonsterNewInfo(string monsterId)
    {
        if (_bookData == null || string.IsNullOrEmpty(monsterId)) return;

        bool changed = false;

        // 移除該妖怪的所有新情報
        if (_bookData.MonsterBookData.NewMonsterInformationID != null)
        {
            var infos = GetMonsterInfosByMonsterID(monsterId);
            foreach (var info in infos)
            {
                if (_bookData.MonsterBookData.NewMonsterInformationID.Remove(info.InformationID))
                    changed = true;
            }
        }

        // 移除該妖怪的所有新故事
        if (_bookData.MonsterBookData.NewMonsterStoryID != null)
        {
            var stories = GetMonsterStoriesByMonsterID(monsterId);
            foreach (var story in stories)
            {
                if (_bookData.MonsterBookData.NewMonsterStoryID.Remove(story.MonsterStoryID))
                    changed = true;
            }
        }

        if (changed)
        {
            MarkBookDataChanged();
            SaveRepository.SaveBookData(_bookData);
        }
    }

    /// <summary>
    /// 確認單筆新情報（從 NewMonsterInformationID 移除）
    /// </summary>
    public bool ConfirmSingleNewInfo(string informationId)
    {
        if (_bookData == null || string.IsNullOrEmpty(informationId)) return false;
        if (_bookData.MonsterBookData.NewMonsterInformationID != null
            && _bookData.MonsterBookData.NewMonsterInformationID.Remove(informationId))
        {
            MarkBookDataChanged();
            SaveRepository.SaveBookData(_bookData);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 確認單筆新故事（從 NewMonsterStoryID 移除）
    /// </summary>
    public bool ConfirmSingleNewStory(string storyId)
    {
        if (_bookData == null || string.IsNullOrEmpty(storyId)) return false;
        if (_bookData.MonsterBookData.NewMonsterStoryID != null
            && _bookData.MonsterBookData.NewMonsterStoryID.Remove(storyId))
        {
            MarkBookDataChanged();
            SaveRepository.SaveBookData(_bookData);
            return true;
        }
        return false;
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
        SaveRepository.SaveAchievementData(_achievementSaveDict);
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
        SaveRepository.SaveAchievementData(_achievementSaveDict);
    }

    /// <summary>
    /// 非同步儲存成就資料
    /// </summary>
    public async Task SaveAchievementAsync()
    {
        await SaveRepository.SaveAchievementDataAsync(_achievementSaveDict);
        OnBookDataChanged = false;
    }

    /// <summary>
    /// 取得特殊紀念品進度存檔資料 (從字典中查詢)
    /// </summary>
    public ISpecialSouvenirSave GetSpecialSouvenirSaveData(string souvenirId)
    {
        _specialSouvenirSaveDict.TryGetValue(souvenirId, out var save);
        return save;
    }

    /// <summary>
    /// 更新單筆特殊紀念品進度存檔資料 (新增或覆蓋)
    /// </summary>
    public void UpdateSpecialSouvenirSaveData(ISpecialSouvenirSave saveData)
    {
        if (saveData == null || string.IsNullOrEmpty(saveData.SouvenirID)) return;

        _specialSouvenirSaveDict[saveData.SouvenirID] = saveData;
        OnBookDataChanged = true;
        SaveRepository.SaveSpecialSouvenirData(_specialSouvenirSaveDict);
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

    #region Player Save/Load (玩家存檔/讀取)

    /// <summary>
    /// 非同步儲存目前玩家資料至指定槽位
    /// </summary>
    public async Task SaveCurrentPlayerAsync(int slot = 0)
    {
        var dataToSave = _currentPlayerData ?? _initialPlayerData ?? new PlayerData();
        await SaveRepository.SaveGameAsync(dataToSave, slot);
    }

    /// <summary>
    /// 從指定的槽位讀取玩家存檔，覆蓋 _currentPlayerData。與 SaveCurrentPlayerAsync(slot) 對稱。
    /// </summary>
    public void LoadCurrentPlayerFromSlot(int slot = 0)
    {
        var save = SaveRepository.Load(slot);
        _currentPlayerData = ClonePlayerData(save?.Player ?? _initialPlayerData ?? new PlayerData());
    }

    /// <summary>
    /// 設定目前的玩家資料 (覆蓋)
    /// </summary>
    public void SetCurrentPlayer(PlayerData data)
    {
        _currentPlayerData = ClonePlayerData(data);
    }

    /// <summary>
    /// 非同步儲存圖鑑資料
    /// </summary>
    public async Task SaveBookAsync()
    {
        if (OnBookDataChanged)
        {
            await SaveRepository.SaveBookDataAsync(GetBookData());
            OnBookDataChanged = false;
        }
    }

    /// <summary>
    /// 設定玩家資料是否已被變更之標籤
    /// </summary>
    public void SetPlayerDataChanged(bool value)
    {
        OnPlayerDataChanged = value;
    }

    /// <summary>
    /// 設定圖鑑資料是否已被變更之標籤
    /// </summary>
    public void SetBookDataChanged(bool value)
    {
        OnBookDataChanged = value;
        BookDataChanged?.Invoke();
    }

    private void MarkBookDataChanged()
    {
        OnBookDataChanged = true;
        BookDataChanged?.Invoke();
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
            OnItemPurchased?.Invoke();
            return true;
        }
        return false;
    }

    public int GetInventoryCapacity()
    {
        int extraCapacity = 0;
        if (SouvenirManager.Instance != null && SouvenirManager.Instance.IsInitialized)
        {
            extraCapacity = SouvenirManager.Instance.GetExtraBagCapacity();
        }

        return BaseInventoryCapacity + Mathf.Max(0, extraCapacity);
    }

    public int GetInventoryItemCount()
    {
        return _currentPlayerData?.Inventory?.Items?.Count ?? 0;
    }

    public bool CanAddItemsToInventory(int amount = 1)
    {
        if (amount <= 0) return true;
        return GetInventoryItemCount() + amount <= GetInventoryCapacity();
    }

    public bool TrySpendGoldForItemPurchase(int amount, int itemAmount = 1)
    {
        if (!CanAddItemsToInventory(itemAmount))
        {
            SystemInfoEvent.Show("背包已滿");
            return false;
        }

        return TrySpendGold(amount);
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

    public bool TrySpendMonsterGoldForItemPurchase(int amount, int itemAmount = 1)
    {
        if (!CanAddItemsToInventory(itemAmount))
        {
            SystemInfoEvent.Show("背包已滿");
            return false;
        }

        return TrySpendMonsterGold(amount);
    }

    public bool ExchangeAllMonsterGoldToGold(out int spentMonsterGold, out int gainedGold)
    {
        spentMonsterGold = 0;
        gainedGold = 0;

        if (_currentPlayerData == null || _currentPlayerData.MonsterGold <= 0)
            return false;

        spentMonsterGold = _currentPlayerData.MonsterGold;
        long calculatedGold = ((long)spentMonsterGold * 3 + 3) / 4;
        gainedGold = calculatedGold > int.MaxValue ? int.MaxValue : (int)calculatedGold;

        _currentPlayerData.MonsterGold = 0;
        long totalGold = (long)_currentPlayerData.Gold + gainedGold;
        _currentPlayerData.Gold = totalGold > int.MaxValue ? int.MaxValue : (int)totalGold;

        OnPlayerDataChanged = true;
        AchievementEvents.GoldChanged(_currentPlayerData.Gold, gainedGold);
        AdjustUpdateView();
        return true;
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

    public void SetEndingReached(EndingType endingType)
    {
        if (_currentPlayerData == null) return;

        _currentPlayerData.HasReachedEnding = endingType != EndingType.None;
        _currentPlayerData.ReachedEndingType = endingType;
        OnPlayerDataChanged = true;
    }

    public bool TryPayGuaranteeDeposit()
    {
        if (_currentPlayerData == null)
            return false;

        if (_currentPlayerData.HasPaidGuaranteeDeposit)
            return true;

        if (_currentPlayerData.Gold < EndingConditionDetector.GuaranteeDepositAmount)
            return false;

        _currentPlayerData.Gold -= EndingConditionDetector.GuaranteeDepositAmount;
        _currentPlayerData.HasPaidGuaranteeDeposit = true;
        OnPlayerDataChanged = true;
        AdjustUpdateView();
        return true;
    }

    public bool TryPayAuctionEntryFee()
    {
        if (_currentPlayerData == null)
            return false;

        if (_currentPlayerData.HasPaidAuctionEntryFee)
            return true;

        if (_currentPlayerData.Gold < EndingConditionDetector.AuctionEntryFeeAmount)
            return false;

        _currentPlayerData.Gold -= EndingConditionDetector.AuctionEntryFeeAmount;
        _currentPlayerData.HasPaidAuctionEntryFee = true;
        OnPlayerDataChanged = true;
        AdjustUpdateView();
        return true;
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

        if (_currentPlayerData.GameSaveFile.GameData.ContainsKey(SaveDataKeys.OrderHistory))
        {
            var orderHistoryData = _currentPlayerData.GameSaveFile.GameData[SaveDataKeys.OrderHistory] as OrderHistoryData;
            if (orderHistoryData.OrderHistory == null || orderHistoryData.LastUpdatedDay != _currentPlayerData.DaysPlayed)
            {
                orderHistoryData.OrderHistory = new List<OrderProgress>();
                orderHistoryData.LastUpdatedDay = _currentPlayerData.DaysPlayed;
            }
            orderHistoryData.OrderHistory.Add(new OrderProgress { OrderID = ID, IsCompleted = true });
        }
        else
        {
            _currentPlayerData.GameSaveFile.GameData.Add(SaveDataKeys.OrderHistory, new OrderHistoryData());
            var orderHistoryData = _currentPlayerData.GameSaveFile.GameData[SaveDataKeys.OrderHistory] as OrderHistoryData;
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

        if (_currentPlayerData.GameSaveFile.GameData.ContainsKey(SaveDataKeys.OrderHistory))
        {
            var orderHistoryData = _currentPlayerData.GameSaveFile.GameData[SaveDataKeys.OrderHistory] as OrderHistoryData;
            if (orderHistoryData.OrderHistory == null)
            {
                orderHistoryData.OrderHistory = new List<OrderProgress>();
            }
            orderHistoryData.OrderHistory.Clear();
            OnPlayerDataChanged = true;
        }
    }

    /// <summary>
    /// 變更當前時段並觸發相關 UI 視圖更新
    /// </summary>
    public void ModifyCurrentDayPhase(DayPhase dayPhase)
    {
        _currentPlayerData.PlayingStatus = dayPhase;
        OnPlayerDataChanged = true;
        GameFlowEvents.InvokeDayPhaseChanged(dayPhase);
        AdjustUpdateView();
    }

    /// <summary>
    /// 根據玩家當前狀態(時段與是否在開店)，調整並觸發主畫面數值(天數/對應貨幣)的更新通知
    /// </summary>
    private void AdjustUpdateView()
    {
        if (_currentPlayerData.PlayingStatus == DayPhase.HumanDay && _currentPlayerData.IsTrade == true)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed, _currentPlayerData.Gold, _currentPlayerData.PlayingStatus);
        }
        else if (_currentPlayerData.PlayingStatus == DayPhase.HumanDay && _currentPlayerData.IsTrade == false)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed, _currentPlayerData.Gold, _currentPlayerData.PlayingStatus);
        }
        else if (_currentPlayerData.PlayingStatus == DayPhase.AfterNoon)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed, _currentPlayerData.Gold, _currentPlayerData.PlayingStatus);
        }
        else if (_currentPlayerData.PlayingStatus == DayPhase.Night && _currentPlayerData.IsTrade == true)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed - 1, _currentPlayerData.MonsterGold, _currentPlayerData.PlayingStatus);
        }
        else if (_currentPlayerData.PlayingStatus == DayPhase.Night && _currentPlayerData.IsTrade == false)
        {
            PlayerMainViewUpdate?.Invoke(_currentPlayerData.DaysPlayed - 1, _currentPlayerData.MonsterGold, _currentPlayerData.PlayingStatus);
        }
    }

    /// <summary>
    /// 對當前玩家狀態進行主畫面資料更新（觸發 PlayerMainViewUpdate 事件刷新 HUD）
    /// </summary>
    public void RefreshPlayerMainView()
    {
        AdjustUpdateView();
    }

    /// <summary>
    /// 修改當前天數
    /// </summary>
    public void ModifyCurrentDay(int CurrentDay)
    {
        bool isNewDay = _currentPlayerData.DaysPlayed != CurrentDay;
        _currentPlayerData.DaysPlayed = CurrentDay;
        if (isNewDay)
        {
            _currentPlayerData.IsTrade = false;
        }
        OnPlayerDataChanged = true;
        AdjustUpdateView();
    }
    #endregion

    #region GetPlayerSaveDataAPI (存檔紀錄查詢)
    /// <summary>
    /// 取得玩家存檔中特定鍵值的資料，如果資料不存在或為舊的(非今日)，則回傳一個新的實例
    /// </summary>
    /// <typeparam name="T">必須實作 ISaveData 的資料類型</typeparam>
    /// <param name="key">對應的存檔鍵值</param>
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

    /// <summary>
    /// 取得玩家存檔中特定鍵值的資料 (此方法不會因為跨日而重置資料)
    /// </summary>
    /// <typeparam name="T">必須實作 ISaveData 的資料類型</typeparam>
    /// <param name="key">對應的存檔鍵值</param>
    public T GetPersistentSaveData<T>(string key) where T : class, ISaveData, new()
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
        return data ?? new T();
    }
    /// <summary>
    /// 讀取妖怪交易紀錄，如果不存在則回傳新的紀錄
    /// </summary>
    public MonsterTradeProgress GetMonsterTradeHistory()
    {
        if (_currentPlayerData.GameSaveFile.GameData.ContainsKey(SaveDataKeys.MonsterTradeProgress))
        {
            return _currentPlayerData.GameSaveFile.GameData[SaveDataKeys.MonsterTradeProgress] as MonsterTradeProgress;
        }
        else
        {
            return new MonsterTradeProgress();
        }
    }
    #endregion
}
