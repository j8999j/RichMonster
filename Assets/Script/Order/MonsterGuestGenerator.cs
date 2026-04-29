using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystem;

public class MonsterGuestGenerator
{
    private const int RegularWeight = 100;
    private const int RareWeight = 30;
    private const int RichWeight = 20;

    private const int MinTraitCount = 0;
    private const int MaxTraitCount = 2;

    private const int MinRequestTagCount = 0;
    private const int MaxRequestTagCount = 1;

    private const int DayMinGuestCount = 6;
    private const int DayMaxGuestCount = 10;

    private readonly Dictionary<string, MonsterProfessionDefinition> _professionData;
    private readonly Dictionary<string, MonsterTraitDefinition> _traitData;
    private readonly Dictionary<string, ItemTags> _itemTagsData;
    private readonly IReadOnlyDictionary<string, ItemDefinition> _itemData;

    public MonsterGuestGenerator(
        Dictionary<string, MonsterProfessionDefinition> professionData,
        Dictionary<string, MonsterTraitDefinition> traitData,
        Dictionary<string, ItemTags> itemTagsData,
        IReadOnlyDictionary<string, ItemDefinition> itemData = null)
    {
        _professionData = professionData;
        _traitData = traitData;
        _itemTagsData = itemTagsData;
        _itemData = itemData;
    }

    public List<MonsterGuest> GenerateGuestsForDay(int dayNumber, int? explicitCount = null)
    {
        int guestCount = explicitCount
            ?? GameRng.RangeKeyed(DayMinGuestCount, DayMaxGuestCount + 1, $"MonsterGuest:Day:{dayNumber}:Count");

        var guests = new List<MonsterGuest>();
        var usedProfessions = new HashSet<string>();

        for (int i = 0; i < guestCount; i++)
        {
            var guest = GenerateGuestForDay(dayNumber, i, usedProfessions);
            if (guest?.monsterCustomer?.Profession != null)
            {
                usedProfessions.Add(guest.monsterCustomer.Profession);
            }

            guests.Add(guest);
        }

        return guests;
    }

    private MonsterGuest GenerateGuestForDay(int dayNumber, int guestIndex, HashSet<string> usedProfessions = null)
    {
        string keyPrefix = $"Day:{dayNumber}:Guest:{guestIndex}";

        var profession = PickProfessionKeyed(keyPrefix, usedProfessions);
        var traits = GenerateTraitsKeyed(keyPrefix);
        var customer = new MonsterCustomer(profession, traits);
        var request = GenerateRequestKeyed(customer, keyPrefix);

        return new MonsterGuest
        {
            monsterCustomer = customer,
            monsterRequest = request
        };
    }

    public MonsterGuest GenerateGuest(int guestIndex)
    {
        var customer = GenerateCustomer(guestIndex);
        var request = GenerateRequest(customer, guestIndex);

        return new MonsterGuest
        {
            monsterCustomer = customer,
            monsterRequest = request
        };
    }

    public List<MonsterGuest> GenerateGuests(int count, int startIndex = 0)
    {
        var guests = new List<MonsterGuest>();
        for (int i = 0; i < count; i++)
        {
            guests.Add(GenerateGuest(startIndex + i));
        }

        return guests;
    }

    #region MonsterCustomer

    private MonsterCustomer GenerateCustomer(int guestIndex)
    {
        return GenerateCustomerKeyed($"MonsterGuest:{guestIndex}");
    }

    private MonsterCustomer GenerateCustomerKeyed(string keyPrefix)
    {
        var profession = PickProfessionKeyed(keyPrefix);
        var traits = GenerateTraitsKeyed(keyPrefix);
        return new MonsterCustomer(profession, traits);
    }

    private MonsterProfessionDefinition PickProfessionKeyed(string keyPrefix, HashSet<string> usedProfessions = null)
    {
        if (_professionData == null || _professionData.Count == 0) return null;

        var professionList = _professionData.Values.ToList();

        if (usedProfessions != null && usedProfessions.Count > 0)
        {
            var filtered = professionList.Where(p => !usedProfessions.Contains(p.Id)).ToList();
            if (filtered.Count > 0)
            {
                professionList = filtered;
            }
        }

        var weightedList = new List<(MonsterProfessionDefinition prof, int weight)>();

        foreach (var prof in professionList)
        {
            int weight = prof.professionType switch
            {
                ProfessionType.Regular => RegularWeight,
                ProfessionType.Rare => RareWeight,
                ProfessionType.Rich => RichWeight,
                _ => RegularWeight
            };
            weightedList.Add((prof, weight));
        }

        int total = weightedList.Sum(p => p.weight);
        if (total <= 0) return professionList.FirstOrDefault();

        int roll = GameRng.RangeKeyed(0, total, $"{keyPrefix}:Profession");
        int cumulative = 0;

        foreach (var entry in weightedList)
        {
            cumulative += entry.weight;
            if (roll < cumulative) return entry.prof;
        }

        return weightedList.Last().prof;
    }

    private List<MonsterTraitDefinition> GenerateTraitsKeyed(string keyPrefix)
    {
        if (_traitData == null || _traitData.Count == 0)
            return new List<MonsterTraitDefinition>();

        int traitCount = GameRng.RangeKeyed(
            MinTraitCount, MaxTraitCount + 1,
            $"{keyPrefix}:TraitCount"
        );

        if (traitCount == 0)
            return new List<MonsterTraitDefinition>();

        var assignedTraits = new List<MonsterTraitDefinition>();
        var usedMutexTags = new HashSet<string>();

        var candidateTraits = _traitData.Values
            .Where(t => !string.IsNullOrEmpty(t.Id))
            .OrderBy(t => GameRng.ValueKeyed($"{keyPrefix}:TraitShuffle:T{t.Id}"))
            .ToList();

        foreach (var candidate in candidateTraits)
        {
            if (assignedTraits.Count >= traitCount) break;

            string mutexTag = candidate.MutexTag;
            if (!string.IsNullOrEmpty(mutexTag) && usedMutexTags.Contains(mutexTag))
            {
                continue;
            }

            assignedTraits.Add(candidate);

            if (!string.IsNullOrEmpty(mutexTag))
            {
                usedMutexTags.Add(mutexTag);
            }
        }

        return assignedTraits;
    }

    #endregion

    #region MonsterRequest

    private MonsterRequest GenerateRequest(MonsterCustomer customer, int guestIndex)
    {
        return GenerateRequestKeyed(customer, $"MonsterGuest:{guestIndex}");
    }

    private MonsterRequest GenerateRequestKeyed(MonsterCustomer customer, string keyPrefix)
    {
        var request = new MonsterRequest();
        var hateSet = customer.HateTags != null ? new HashSet<string>(customer.HateTags) : new HashSet<string>();

        string targetPreferTag = null;
        if (customer.PreferredTags != null && customer.PreferredTags.Count > 0)
        {
            if (GameRng.ValueKeyed($"{keyPrefix}:PreferTrigger") < 0.25f)
            {
                int pIndex = GameRng.RangeKeyed(0, customer.PreferredTags.Count, $"{keyPrefix}:PreferTagIndex");
                targetPreferTag = customer.PreferredTags[pIndex];
            }
        }

        bool isType2 = GameRng.ValueKeyed($"{keyPrefix}:RequestCategory") < (1f / 3f);
        var type2CandidateItems = GetEligibleHumanRequestItems(hateSet, true);
        var type1CandidateItems = GetEligibleHumanRequestItems(hateSet, false);

        string type2PreferTag = ResolvePreferTagForItems(targetPreferTag, type2CandidateItems);
        string type1PreferTag = ResolvePreferTagForItems(targetPreferTag, type1CandidateItems);

        if (!string.IsNullOrEmpty(type2PreferTag))
        {
            type2CandidateItems = type2CandidateItems
                .Where(item => item.Tags != null && item.Tags.Contains(type2PreferTag))
                .ToList();
        }

        if (!string.IsNullOrEmpty(type1PreferTag))
        {
            type1CandidateItems = type1CandidateItems
                .Where(item => item.Tags != null && item.Tags.Contains(type1PreferTag))
                .ToList();
        }

        if (isType2 && type2CandidateItems.Count > 0)
        {
            int itemIndex = GameRng.RangeKeyed(0, type2CandidateItems.Count, $"{keyPrefix}:TargetItemIndex");
            var pickedItem = type2CandidateItems[itemIndex];
            request.itemType = pickedItem.Type;

            var itemTags = (pickedItem.Tags ?? new List<string>()).ToList();
            if (itemTags.Count > 3)
            {
                var selectedTags = new List<string>();
                if (!string.IsNullOrEmpty(type2PreferTag) && itemTags.Contains(type2PreferTag))
                {
                    selectedTags.Add(type2PreferTag);
                    itemTags.Remove(type2PreferTag);
                }

                var shuffledRest = itemTags.OrderBy(t => GameRng.ValueKeyed($"{keyPrefix}:TagShuffle:{t}")).ToList();
                int needed = 3 - selectedTags.Count;
                selectedTags.AddRange(shuffledRest.Take(needed));
                request.RequestTags = selectedTags;
            }
            else
            {
                request.RequestTags = itemTags;
            }

            request.TriggeredPreference = !string.IsNullOrEmpty(type2PreferTag);
            request.IsType2Category = true;
            return request;
        }

        if (type1CandidateItems.Count > 0)
        {
            var pickedItem = type1CandidateItems[
                GameRng.RangeKeyed(0, type1CandidateItems.Count, $"{keyPrefix}:Type1TargetItemIndex")
            ];

            request.itemType = pickedItem.Type;
            request.RequestTags = GenerateRequestTagsFromItemKeyed(
                $"{keyPrefix}:Type1",
                pickedItem.Tags,
                MinRequestTagCount,
                MaxRequestTagCount,
                customer.HateTags,
                type1PreferTag
            );
        }
        else
        {
            int typeRoll = GameRng.RangeKeyed(0, 3, $"{keyPrefix}:RequestType_Type1");
            request.itemType = (ItemType)typeRoll;

            if (!string.IsNullOrEmpty(type1PreferTag))
            {
                request.RequestTags = new List<string> { type1PreferTag };
            }
            else
            {
                request.RequestTags = GenerateRequestTagsKeyed(keyPrefix, customer.HateTags);
            }
        }

        request.TriggeredPreference = !string.IsNullOrEmpty(type1PreferTag) && request.RequestTags.Contains(type1PreferTag);
        request.IsType2Category = false;
        return request;
    }

    private List<ItemDefinition> GetEligibleHumanRequestItems(HashSet<string> hateSet, bool excludeHateTags)
    {
        if (_itemData == null || _itemData.Count == 0)
            return new List<ItemDefinition>();

        var query = _itemData.Values.Where(item => item.World == ItemWorld.Human);
        if (excludeHateTags)
        {
            query = query.Where(item => item.Tags == null || !item.Tags.Any(tag => hateSet.Contains(tag)));
        }

        return query.ToList();
    }

    private string ResolvePreferTagForItems(string targetPreferTag, IEnumerable<ItemDefinition> candidateItems)
    {
        if (string.IsNullOrEmpty(targetPreferTag) || candidateItems == null)
            return null;

        return candidateItems.Any(item => item.Tags != null && item.Tags.Contains(targetPreferTag))
            ? targetPreferTag
            : null;
    }

    private List<string> GenerateRequestTagsFromItemKeyed(
        string keyPrefix,
        IEnumerable<string> sourceTags,
        int minTagCount,
        int maxTagCount,
        IEnumerable<string> excludeTags = null,
        string forcedTag = null)
    {
        var result = new List<string>();
        if (sourceTags == null)
            return result;

        var excludeSet = excludeTags != null ? new HashSet<string>(excludeTags) : new HashSet<string>();
        var candidateTags = sourceTags
            .Where(tag => !string.IsNullOrEmpty(tag) && !excludeSet.Contains(tag))
            .Distinct()
            .ToList();

        if (candidateTags.Count == 0)
            return result;

        if (!string.IsNullOrEmpty(forcedTag) && candidateTags.Contains(forcedTag))
        {
            result.Add(forcedTag);
            candidateTags.Remove(forcedTag);
            minTagCount = Mathf.Max(minTagCount, 1);
        }

        int targetCount = GameRng.RangeKeyed(minTagCount, maxTagCount + 1, $"{keyPrefix}:TagCount");
        targetCount = Mathf.Min(targetCount, result.Count + candidateTags.Count);

        if (targetCount <= result.Count)
            return result;

        var shuffledTags = candidateTags
            .OrderBy(tag => GameRng.ValueKeyed($"{keyPrefix}:TagShuffle:Tag{tag}"))
            .Take(targetCount - result.Count);

        result.AddRange(shuffledTags);
        return result;
    }

    private List<string> GenerateRequestTagsKeyed(string keyPrefix, IEnumerable<string> excludeTags = null)
    {
        var tags = new List<string>();

        if (_itemTagsData == null || _itemTagsData.Count == 0)
            return tags;

        int tagCount = GameRng.RangeKeyed(
            MinRequestTagCount, MaxRequestTagCount + 1,
            $"{keyPrefix}:TagCount"
        );

        if (tagCount == 0)
            return tags;

        var excludeSet = excludeTags != null ? new HashSet<string>(excludeTags) : new HashSet<string>();
        var allTags = _itemTagsData.Keys
            .Where(t => !excludeSet.Contains(t))
            .ToList();

        if (allTags.Count == 0)
            return tags;

        return allTags
            .OrderBy(t => GameRng.ValueKeyed($"{keyPrefix}:TagShuffle:Tag{t}"))
            .Take(tagCount)
            .ToList();
    }

    #endregion
}
