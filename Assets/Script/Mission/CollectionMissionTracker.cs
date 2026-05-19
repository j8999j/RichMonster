using System.Collections.Generic;
using System.Linq;
using GameSystem;
using Player;
using UnityEngine;

public class CollectionMissionTracker : MonoBehaviour
{
    public const int MaxRewardPoints = 10;
    public const int BronzeRewardPoints = 3;
    public const int SilverRewardPoints = 5;
    public const int GoldRewardPoints = 10;

    [SerializeField]
    private CollectionMission collectionMission;

    [SerializeField]
    private CollectionMissionView collectionMissionView;

    [SerializeField]
    private CollectionMissionNpc collectionNpcPrefab;

    [SerializeField]
    private List<CollectionMissionNpcSpawnPoint> collectionNpcSpawnPoints = new();

    private readonly List<CollectionMissionNpc> spawnedCollectionNpcs = new();

    public CollectionMissionSaveData CurrentProgress => LoadProgress();

    private void Awake()
    {
        if (collectionMissionView == null)
            collectionMissionView = FindObjectOfType<CollectionMissionView>(true);

        InitializeSceneNpcs();
    }

    private void OnEnable()
    {
        if (collectionMissionView != null)
            collectionMissionView.OnClosed += HandleViewClosed;
    }

    private void OnDisable()
    {
        if (collectionMissionView != null)
            collectionMissionView.OnClosed -= HandleViewClosed;
    }

    private void Start()
    {
        SpawnCollectionNpcs();
    }

    public void SetMission(CollectionMission mission)
    {
        if (mission != null)
            collectionMission = mission;
    }

    public void OpenMission(CollectionMissionRace race)
    {
        if (collectionMission == null || collectionMissionView == null)
        {
            Debug.LogWarning($"[{nameof(CollectionMissionTracker)}] Missing collection mission or view reference on {name}.");
            return;
        }

        collectionMissionView.Open(collectionMission, this, race);
        GameManager.Instance.LockPlayerMove(PlayerLockSources.NpcOnMap);
    }

    public void CloseMission()
    {
        if (collectionMissionView != null)
            collectionMissionView.Close();

        GameManager.Instance.UnlockPlayerMove(PlayerLockSources.NpcOnMap);
    }

    public void SpawnCollectionNpcs()
    {
        if (collectionNpcPrefab == null || collectionNpcSpawnPoints == null || collectionNpcSpawnPoints.Count == 0)
            return;

        foreach (var spawnedNpc in spawnedCollectionNpcs)
        {
            if (spawnedNpc != null)
                Destroy(spawnedNpc.gameObject);
        }

        spawnedCollectionNpcs.Clear();

        foreach (var spawnPoint in collectionNpcSpawnPoints)
        {
            if (spawnPoint == null || spawnPoint.Point == null)
                continue;

            var npc = Instantiate(collectionNpcPrefab, spawnPoint.Point.position, spawnPoint.Point.rotation, spawnPoint.Point.parent);
            npc.Setup(this, spawnPoint.Race);
            ApplyCollectionNpcSprite(npc);
            spawnedCollectionNpcs.Add(npc);
        }
    }

    private void InitializeSceneNpcs()
    {
        var sceneNpcs = FindObjectsOfType<CollectionMissionNpc>(true);
        foreach (var npc in sceneNpcs)
        {
            if (npc != null && (npc.Tracker == null || npc.Tracker == this))
            {
                npc.Setup(this);
                ApplyCollectionNpcSprite(npc);
            }
        }
    }

    private void ApplyCollectionNpcSprite(CollectionMissionNpc npc)
    {
        if (npc == null || collectionMissionView == null)
            return;

        npc.ApplySprite(collectionMissionView.GetNpcSprite(npc.Race));
    }

    private void HandleViewClosed()
    {
        GameManager.Instance.UnlockPlayerMove(PlayerLockSources.NpcOnMap);
    }

    public List<Item> GetSubmittableItems()
    {
        var items = DataManager.Instance?.CurrentPlayerData?.InventoryItems;
        return items == null ? new List<Item>() : items.Where(CanSubmit).ToList();
    }

    public Item GetFirstSubmittableItem(string itemId)
    {
        var items = DataManager.Instance?.CurrentPlayerData?.InventoryItems;
        if (items == null || string.IsNullOrEmpty(itemId))
            return null;

        return items.FirstOrDefault(item => item != null && item.ItemId == itemId && CanSubmit(item));
    }

    public bool CanSubmit(Item item)
    {
        if (item == null || collectionMission == null || string.IsNullOrEmpty(item.ItemId))
            return false;

        var itemDefinition = DataManager.Instance?.GetItemById(item.ItemId);
        bool isMissionItem = collectionMission.TryGetItemEntry(item.ItemId, out var category, out _);
        return itemDefinition != null
            && itemDefinition.World == ItemWorld.Monster
            && isMissionItem
            && GetRacePoints(category) < MaxRewardPoints
            && !IsItemCollected(item.ItemId);
    }

    public bool IsItemCollected(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        var progress = LoadProgress();
        return progress.RaceProgress != null
            && progress.RaceProgress.Any(race => race != null && race.HasSubmitted(itemId));
    }

    public int GetRacePoints(CollectionMissionRace race)
    {
        var category = collectionMission != null ? collectionMission.GetCategory(race) : null;
        return GetRacePoints(category);
    }

    public int GetRacePoints(CollectionMissionCategory category)
    {
        if (category == null)
            return 0;

        return GetRacePoints(category.RaceName);
    }

    public int GetRacePoints(string raceName)
    {
        if (string.IsNullOrEmpty(raceName))
            return 0;

        var progress = LoadProgress();
        var raceProgress = progress.RaceProgress?.FirstOrDefault(p => p != null && p.Race == raceName);
        return raceProgress != null ? raceProgress.Points : 0;
    }

    public bool CanClaimReward(CollectionMissionRace race, int milestone)
    {
        var category = collectionMission != null ? collectionMission.GetCategory(race) : null;
        return CanClaimReward(category, milestone);
    }

    public bool CanClaimReward(CollectionMissionCategory category, int milestone)
    {
        if (category == null || GetRewardGold(milestone) <= 0)
            return false;

        var progress = LoadProgress();
        var raceProgress = GetRaceProgress(progress, category.RaceName, false);
        return raceProgress != null
            && raceProgress.Points >= milestone
            && !raceProgress.HasClaimedReward(milestone);
    }

    public bool HasClaimedReward(CollectionMissionCategory category, int milestone)
    {
        if (category == null || GetRewardGold(milestone) <= 0)
            return false;

        var progress = LoadProgress();
        var raceProgress = GetRaceProgress(progress, category.RaceName, false);
        return raceProgress != null && raceProgress.HasClaimedReward(milestone);
    }

    public bool TryClaimReward(CollectionMissionRace race, int milestone, out int goldAmount)
    {
        goldAmount = 0;

        if (collectionMission == null)
            return false;

        var category = collectionMission.GetCategory(race);
        if (category == null)
            return false;

        goldAmount = GetRewardGold(milestone);
        if (goldAmount <= 0)
            return false;

        var dataManager = DataManager.Instance;
        if (dataManager == null)
            return false;

        var progress = LoadProgress();
        var raceProgress = GetRaceProgress(progress, category.RaceName, false);
        if (raceProgress == null || raceProgress.Points < milestone || raceProgress.HasClaimedReward(milestone))
            return false;

        raceProgress.ClaimedRewardMilestones ??= new List<int>();
        raceProgress.ClaimedRewardMilestones.Add(milestone);
        progress.UniqueID = SaveDataKeys.CollectionMission;
        progress.LastUpdatedDay = dataManager.CurrentPlayerData?.DaysPlayed ?? 0;

        dataManager.ModifyGold(goldAmount);
        dataManager.SetPlayerData(SaveDataKeys.CollectionMission, progress);
        return true;
    }

    public int GetRewardGold(int milestone)
    {
        return milestone switch
        {
            BronzeRewardPoints => 5000,
            SilverRewardPoints => 30000,
            GoldRewardPoints => 100000,
            _ => 0
        };
    }

    public bool TrySubmit(Item item, out int addedPoints, out string raceName)
    {
        addedPoints = 0;
        raceName = string.Empty;

        if (item == null || collectionMission == null || string.IsNullOrEmpty(item.ItemId))
            return false;

        var dataManager = DataManager.Instance;
        if (dataManager == null)
            return false;

        var itemDefinition = dataManager.GetItemById(item.ItemId);
        if (itemDefinition == null || itemDefinition.World != ItemWorld.Monster)
            return false;

        if (!collectionMission.TryGetItemEntry(item.ItemId, out var category, out var entry))
            return false;

        if (IsItemCollected(item.ItemId))
            return false;

        int racePoints = GetRacePoints(category);
        int remainingPoints = MaxRewardPoints - racePoints;
        if (remainingPoints <= 0)
            return false;

        int points = Mathf.Min(Mathf.Max(0, entry.Points), remainingPoints);
        if (points <= 0)
            return false;

        if (!dataManager.RemoveItem(item))
            return false;

        var progress = LoadProgress();
        progress.UniqueID = SaveDataKeys.CollectionMission;
        progress.LastUpdatedDay = dataManager.CurrentPlayerData?.DaysPlayed ?? 0;

        string targetRaceName = category.RaceName;
        raceName = targetRaceName;
        var raceProgress = GetRaceProgress(progress, targetRaceName, true);

        var itemProgress = raceProgress.Items.FirstOrDefault(p => p.ItemID == item.ItemId);
        if (itemProgress == null)
        {
            itemProgress = new CollectionMissionItemProgress { ItemID = item.ItemId };
            raceProgress.Items.Add(itemProgress);
        }

        itemProgress.SubmitCount++;
        itemProgress.Points += points;
        raceProgress.Points += points;
        progress.TotalPoints += points;
        addedPoints = points;

        dataManager.SetPlayerData(SaveDataKeys.CollectionMission, progress);
        return true;
    }

    private CollectionMissionRaceProgress GetRaceProgress(CollectionMissionSaveData progress, string raceName, bool createIfMissing)
    {
        if (progress == null || string.IsNullOrEmpty(raceName))
            return null;

        progress.RaceProgress ??= new List<CollectionMissionRaceProgress>();
        var raceProgress = progress.RaceProgress.FirstOrDefault(p => p != null && p.Race == raceName);
        if (raceProgress == null && createIfMissing)
        {
            raceProgress = new CollectionMissionRaceProgress { Race = raceName };
            progress.RaceProgress.Add(raceProgress);
        }

        if (raceProgress != null)
        {
            raceProgress.Items ??= new List<CollectionMissionItemProgress>();
            raceProgress.ClaimedRewardMilestones ??= new List<int>();
        }

        return raceProgress;
    }

    private CollectionMissionSaveData LoadProgress()
    {
        if (DataManager.Instance == null)
            return new CollectionMissionSaveData();

        var progress = DataManager.Instance.GetPersistentSaveData<CollectionMissionSaveData>(SaveDataKeys.CollectionMission);
        progress.UniqueID = SaveDataKeys.CollectionMission;
        progress.RaceProgress ??= new System.Collections.Generic.List<CollectionMissionRaceProgress>();
        return progress;
    }

    [System.Serializable]
    private class CollectionMissionNpcSpawnPoint
    {
        public CollectionMissionRace Race;
        public Transform Point;
    }
}
