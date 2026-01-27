using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystem;

/// <summary>
/// 妖怪客人生成器，使用 GameRng 種子確保可重現
/// </summary>
public class MonsterGuestGenerator
{
    // 職業稀有度權重
    private const int RegularWeight = 100;
    private const int RareWeight = 30;
    private const int RichWeight = 20;

    // 特質數量範圍
    private const int MinTraitCount = 0;
    private const int MaxTraitCount = 2;

    // 請求標籤數量範圍
    private const int MinRequestTagCount = 0;
    private const int MaxRequestTagCount = 3;

    // 每日客人數量範圍
    private const int DayMinGuestCount = 6;
    private const int DayMaxGuestCount = 10;

    private readonly Dictionary<string, MonsterProfessionDefinition> _professionData;
    private readonly Dictionary<string, MonsterTraitDefinition> _traitData;
    private readonly Dictionary<string, ItemTags> _itemTagsData;

    public MonsterGuestGenerator(
        Dictionary<string, MonsterProfessionDefinition> professionData,
        Dictionary<string, MonsterTraitDefinition> traitData,
        Dictionary<string, ItemTags> itemTagsData)
    {
        _professionData = professionData;
        _traitData = traitData;
        _itemTagsData = itemTagsData;
    }

    /// <summary>
    /// 根據天數生成當日所有妖怪客人 (數量隨機 6-10)
    /// </summary>
    /// <param name="dayNumber">天數，用於 RNG key</param>
    /// <param name="explicitCount">明確指定客人數量，若為 null 則隨機 6-10</param>
    public List<MonsterGuest> GenerateGuestsForDay(int dayNumber, int? explicitCount = null)
    {
        // 決定當日客人數量
        int guestCount = explicitCount 
            ?? GameRng.RangeKeyed(DayMinGuestCount, DayMaxGuestCount + 1, $"MonsterGuest:Day:{dayNumber}:Count");

        var guests = new List<MonsterGuest>();
        for (int i = 0; i < guestCount; i++)
        {
            // 使用 dayNumber 和 index 組合作為唯一 key
            guests.Add(GenerateGuestForDay(dayNumber, i));
        }
        return guests;
    }
    /// <summary>
    /// 生成單一妖怪客人 (帶天數資訊)
    /// </summary>
    private MonsterGuest GenerateGuestForDay(int dayNumber, int guestIndex)
    {
        string keyPrefix = $"Day:{dayNumber}:Guest:{guestIndex}";
        
        // 先選擇職業以取得 HateTags
        var profession = PickProfessionKeyed(keyPrefix);
        var traits = GenerateTraitsKeyed(keyPrefix);
        var customer = new MonsterCustomer(profession, traits);
        
        // 生成請求時排除 HateTags
        var hateTags = profession?.HateTags ?? new HashSet<string>();
        var request = GenerateRequestKeyed(keyPrefix, hateTags);

        return new MonsterGuest
        {
            monsterCustomer = customer,
            monsterRequest = request
        };
    }

    /// <summary>
    /// 生成單一妖怪客人
    /// </summary>
    /// <param name="guestIndex">客人索引，用於 RNG key</param>
    public MonsterGuest GenerateGuest(int guestIndex)
    {
        var customer = GenerateCustomer(guestIndex);
        var request = GenerateRequest(guestIndex);

        return new MonsterGuest
        {
            monsterCustomer = customer,
            monsterRequest = request
        };
    }

    /// <summary>
    /// 生成指定數量的妖怪客人列表
    /// </summary>
    public List<MonsterGuest> GenerateGuests(int count, int startIndex = 0)
    {
        var guests = new List<MonsterGuest>();
        for (int i = 0; i < count; i++)
        {
            guests.Add(GenerateGuest(startIndex + i));
        }
        return guests;
    }

    #region MonsterCustomer 生成

    /// <summary>
    /// 生成 MonsterCustomer：按稀有度權重選職業，隨機 0-2 個特質
    /// </summary>
    private MonsterCustomer GenerateCustomer(int guestIndex)
    {
        return GenerateCustomerKeyed($"MonsterGuest:{guestIndex}");
    }

    /// <summary>
    /// 使用指定的 key 前綴生成 MonsterCustomer
    /// </summary>
    private MonsterCustomer GenerateCustomerKeyed(string keyPrefix)
    {
        var profession = PickProfessionKeyed(keyPrefix);
        var traits = GenerateTraitsKeyed(keyPrefix);

        return new MonsterCustomer(profession, traits);
    }

    /// <summary>
    /// 按職業稀有度權重選擇職業
    /// </summary>
    private MonsterProfessionDefinition PickProfessionKeyed(string keyPrefix)
    {
        if (_professionData == null || _professionData.Count == 0) return null;

        var professionList = _professionData.Values.ToList();
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

    /// <summary>
    /// 生成 0-2 個特質，考慮 MutexTag 互斥
    /// </summary>
    private List<MonsterTraitDefinition> GenerateTraitsKeyed(string keyPrefix)
    {
        if (_traitData == null || _traitData.Count == 0)
            return new List<MonsterTraitDefinition>();

        // 隨機決定特質數量 (0-2)
        int traitCount = GameRng.RangeKeyed(
            MinTraitCount, MaxTraitCount + 1,
            $"{keyPrefix}:TraitCount"
        );

        if (traitCount == 0)
            return new List<MonsterTraitDefinition>();

        var assignedTraits = new List<MonsterTraitDefinition>();
        var usedMutexTags = new HashSet<string>();

        // 隨機排序候選特質
        var candidateTraits = _traitData.Values
            .Where(t => !string.IsNullOrEmpty(t.Id))
            .OrderBy(t => GameRng.ValueKeyed($"{keyPrefix}:TraitShuffle:T{t.Id}"))
            .ToList();

        foreach (var candidate in candidateTraits)
        {
            if (assignedTraits.Count >= traitCount) break;

            // 檢查互斥標籤
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

    #region MonsterRequest 生成

    /// <summary>
    /// 生成 MonsterRequest：隨機選擇物品類型，隨機 0-3 個標籤
    /// </summary>
    private MonsterRequest GenerateRequest(int guestIndex)
    {
        return GenerateRequestKeyed($"MonsterGuest:{guestIndex}");
    }

    /// <summary>
    /// 使用指定的 key 前綴生成 MonsterRequest
    /// </summary>
    /// <param name="excludeTags">要排除的標籤 (HateTags)</param>
    private MonsterRequest GenerateRequestKeyed(string keyPrefix, IEnumerable<string> excludeTags = null)
    {
        var request = new MonsterRequest();

        // 隨機選擇物品類型 (3種，機率相同)
        int typeRoll = GameRng.RangeKeyed(0, 3, $"{keyPrefix}:RequestType");
        request.itemType = (ItemType)typeRoll;

        // 隨機選擇 0-3 個標籤 (排除 HateTags)
        request.RequestTags = GenerateRequestTagsKeyed(keyPrefix, excludeTags);

        return request;
    }

    /// <summary>
    /// 從所有標籤中隨機抽取 0-3 個，機率相同
    /// </summary>
    /// <param name="excludeTags">要排除的標籤 (HateTags)</param>
    private List<string> GenerateRequestTagsKeyed(string keyPrefix, IEnumerable<string> excludeTags = null)
    {
        var tags = new List<string>();

        if (_itemTagsData == null || _itemTagsData.Count == 0)
            return tags;

        // 隨機決定標籤數量 (0-3)
        int tagCount = GameRng.RangeKeyed(
            MinRequestTagCount, MaxRequestTagCount + 1,
            $"{keyPrefix}:TagCount"
        );

        if (tagCount == 0)
            return tags;

        // 所有標籤列表，排除 HateTags
        var excludeSet = excludeTags != null ? new HashSet<string>(excludeTags) : new HashSet<string>();
        var allTags = _itemTagsData.Keys
            .Where(t => !excludeSet.Contains(t))
            .ToList();
            
        if (allTags.Count == 0)
            return tags;

        // 隨機排序後取前 tagCount 個
        var shuffledTags = allTags
            .OrderBy(t => GameRng.ValueKeyed($"{keyPrefix}:TagShuffle:Tag{t}"))
            .Take(tagCount)
            .ToList();

        return shuffledTags;
    }
    #endregion
}