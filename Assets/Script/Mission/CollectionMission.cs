using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Mission/CollectionMission", fileName = "CollectionMission")]
public class CollectionMission : ScriptableObject
{
    [SerializeField]
    private List<CollectionMissionCategory> categories = new List<CollectionMissionCategory>
    {
        new CollectionMissionCategory(CollectionMissionRace.Ghost),
        new CollectionMissionCategory(CollectionMissionRace.Beast),
        new CollectionMissionCategory(CollectionMissionRace.Divine),
        new CollectionMissionCategory(CollectionMissionRace.Fairy)
    };

    public IReadOnlyList<CollectionMissionCategory> Categories => categories;

    public CollectionMissionCategory GetCategory(CollectionMissionRace race)
    {
        return categories?.FirstOrDefault(category => category != null && category.Race == race);
    }

    public bool TryGetItemEntry(string itemId, out CollectionMissionCategory category, out CollectionMissionItemEntry entry)
    {
        category = null;
        entry = null;

        if (string.IsNullOrEmpty(itemId) || categories == null)
            return false;

        foreach (var candidateCategory in categories)
        {
            if (candidateCategory?.Items == null) continue;

            var candidateEntry = candidateCategory.Items.FirstOrDefault(item => item != null && item.ItemID == itemId);
            if (candidateEntry == null) continue;

            category = candidateCategory;
            entry = candidateEntry;
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (categories == null)
            categories = new List<CollectionMissionCategory>();

        foreach (CollectionMissionRace race in Enum.GetValues(typeof(CollectionMissionRace)))
        {
            if (!categories.Any(category => category != null && category.Race == race))
                categories.Add(new CollectionMissionCategory(race));
        }

        foreach (var category in categories)
        {
            category?.Normalize();
        }
    }
}

public enum CollectionMissionRace
{
    Ghost,
    Beast,
    Divine,
    Fairy
}

[Serializable]
public class CollectionMissionCategory
{
    public CollectionMissionRace Race;
    public string Title;
    public List<CollectionMissionItemEntry> Items = new List<CollectionMissionItemEntry>();

    public CollectionMissionCategory()
    {
    }

    public CollectionMissionCategory(CollectionMissionRace race)
    {
        Race = race;
        Title = CollectionMissionRaceUtility.GetRaceName(race);
    }

    public string RaceName => CollectionMissionRaceUtility.GetRaceName(Race);

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Title))
            Title = RaceName;

        if (Items == null)
            Items = new List<CollectionMissionItemEntry>();

        foreach (var item in Items)
        {
            if (item != null && item.Points < 0)
                item.Points = 0;
        }
    }
}

[Serializable]
public class CollectionMissionItemEntry
{
    [ItemIDSelect]
    public string ItemID;
    public int Points = 1;
    [TextArea]
    public string Description;
}

public static class CollectionMissionRaceUtility
{
    public static string GetRaceName(CollectionMissionRace race)
    {
        return race switch
        {
            CollectionMissionRace.Ghost => "\u5E7D\u9748",
            CollectionMissionRace.Beast => "\u7378\u65CF",
            CollectionMissionRace.Divine => "\u795E\u65CF",
            CollectionMissionRace.Fairy => "\u5996\u7CBE",
            _ => race.ToString()
        };
    }
}

[Serializable]
public class CollectionMissionSaveData : ISaveData
{
    public string UniqueID { get; set; } = CollectionMissionTracker.SaveKey;
    public int LastUpdatedDay { get; set; }
    public int TotalPoints;
    public List<CollectionMissionRaceProgress> RaceProgress = new List<CollectionMissionRaceProgress>();
}

[Serializable]
public class CollectionMissionRaceProgress
{
    public string Race;
    public int Points;
    public List<int> ClaimedRewardMilestones = new List<int>();
    public List<CollectionMissionItemProgress> Items = new List<CollectionMissionItemProgress>();

    public bool HasSubmitted(string itemId)
    {
        return Items != null && Items.Any(item => item != null && item.ItemID == itemId && item.SubmitCount > 0);
    }

    public bool HasClaimedReward(int milestone)
    {
        return ClaimedRewardMilestones != null && ClaimedRewardMilestones.Contains(milestone);
    }
}

[Serializable]
public class CollectionMissionItemProgress
{
    public string ItemID;
    public int SubmitCount;
    public int Points;
}
