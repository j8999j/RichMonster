using System.Collections.Generic;
using System.Threading.Tasks;
using Souvenir;

namespace GameSystem
{
    public class GameDataLoadResult
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

    public interface IGameDataProvider
    {
        Task<GameDataLoadResult> LoadAllGameDataAsync();
    }

    public interface IPlayerSaveRepository
    {
        bool IsSaving { get; }
        SaveFileData LastLoaded { get; }
        Task SaveGameAsync(PlayerData playerData, int slot = 0);
        SaveFileData Load(int slot = 0);
        SaveSlotData LoadSlotInfo(int slot);
        bool DeleteSaveSlot(int slot);
        int GetNextAvailableSlot(int maxSlots = 10);
    }

    public interface IBookSaveRepository
    {
        bool IsSavingBook { get; }
        Task SaveBookDataAsync(GameSaveBook bookData);
        void SaveBookData(GameSaveBook bookData);
        void SetBookDataCache(GameSaveBook bookData);
        GameSaveBook GetBookDataCache();
        Dictionary<string, IAchievementSave> GetAchievementDict();
        Dictionary<string, ISpecialSouvenirSave> GetSpecialSouvenirDict();
        Task SaveAchievementDataAsync(Dictionary<string, IAchievementSave> achievementDict);
        void SaveAchievementData(Dictionary<string, IAchievementSave> achievementDict);
        Task SaveSpecialSouvenirDataAsync(Dictionary<string, ISpecialSouvenirSave> specialSouvenirDict);
        void SaveSpecialSouvenirData(Dictionary<string, ISpecialSouvenirSave> specialSouvenirDict);
    }

    public interface IGameSaveRepository : IPlayerSaveRepository, IBookSaveRepository
    {
    }
}
