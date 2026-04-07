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
    private const int MaxRequestTagCount = 1;

    // 每日客人數量範圍
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
        var usedProfessions = new HashSet<string>();

        for (int i = 0; i < guestCount; i++)
        {
            // 使用 dayNumber 和 index 組合作為唯一 key
            var guest = GenerateGuestForDay(dayNumber, i, usedProfessions);
            if (guest?.monsterCustomer?.Profession != null)
            {
                usedProfessions.Add(guest.monsterCustomer.Profession);
            }
            guests.Add(guest);
        }
        return guests;
    }
    /// <summary>
    /// 生成單一妖怪客人 (帶天數資訊)
    /// </summary>
    private MonsterGuest GenerateGuestForDay(int dayNumber, int guestIndex, HashSet<string> usedProfessions = null)
    {
        string keyPrefix = $"Day:{dayNumber}:Guest:{guestIndex}";
        
        // 先選擇職業以取得 HateTags，傳入已使用的職業清單避免重複
        var profession = PickProfessionKeyed(keyPrefix, usedProfessions);
        var traits = GenerateTraitsKeyed(keyPrefix);
        var customer = new MonsterCustomer(profession, traits);
        
        // 生成請求時傳入顧客資料以判定偏好標籤與 HateTags
        var request = GenerateRequestKeyed(customer, keyPrefix);

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
        var request = GenerateRequest(customer, guestIndex);

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
    private MonsterProfessionDefinition PickProfessionKeyed(string keyPrefix, HashSet<string> usedProfessions = null)
    {
        if (_professionData == null || _professionData.Count == 0) return null;

        var professionList = _professionData.Values.ToList();
        
        // 過濾掉已經出現過的職業
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
    /// 生成 MonsterRequest：處理第一類與第二類邏輯
    /// </summary>
    private MonsterRequest GenerateRequest(MonsterCustomer customer, int guestIndex)
    {
        return GenerateRequestKeyed(customer, $"MonsterGuest:{guestIndex}");
    }

    /// <summary>
    /// 使用指定的 key 前綴生成 MonsterRequest（支援雙類型及偏好機率）
    /// </summary>
    /// <param name="customer">顧各資料 (讀取 PreferredTags 與 HateTags)</param>
    private MonsterRequest GenerateRequestKeyed(MonsterCustomer customer, string keyPrefix)
    {
        var request = new MonsterRequest();
        var hateSet = customer.HateTags != null ? new HashSet<string>(customer.HateTags) : new HashSet<string>();

        // 步驟一：判定是否觸發偏好（25%）
        string targetPreferTag = null;
        if (customer.PreferredTags != null && customer.PreferredTags.Count > 0)
        {
            if (GameRng.ValueKeyed($"{keyPrefix}:PreferTrigger") < 0.25f)
            {
                // 隨機選一個偏好標籤
                int pIndex = GameRng.RangeKeyed(0, customer.PreferredTags.Count, $"{keyPrefix}:PreferTagIndex");
                targetPreferTag = customer.PreferredTags[pIndex];
            }
        }

        // 步驟二：判定需求種類（Type1: 2/3, Type2: 1/3）
        bool isType2 = GameRng.ValueKeyed($"{keyPrefix}:RequestCategory") < (1f / 3f);

        if (isType2 && _itemData != null && _itemData.Count > 0)
        {
            // 種類 2 (實際人界商品)
            var candidateItems = _itemData.Values.Where(item => item.World == ItemWorld.Human).ToList();
            
            // 排除本身含有 HateTags 的商品
            candidateItems = candidateItems.Where(item => !item.Tags.Any(t => hateSet.Contains(t))).ToList();

            if (!string.IsNullOrEmpty(targetPreferTag))
            {
                // 需要含有指定偏好標籤
                var preferItems = candidateItems.Where(item => item.Tags.Contains(targetPreferTag)).ToList();
                if (preferItems.Count > 0)
                {
                    candidateItems = preferItems;
                }
                else
                {
                    // 若剛好沒有任何人界商品同時滿足 (無 HateTag + 包含該 PreferTag)，退回為種類 1 並指定該標籤
                    isType2 = false; 
                }
            }
            
            if (isType2) 
            {
                 if(candidateItems.Count > 0)
                 {
                     int itemIndex = GameRng.RangeKeyed(0, candidateItems.Count, $"{keyPrefix}:TargetItemIndex");
                     var pickedItem = candidateItems[itemIndex];
                     request.itemType = pickedItem.Type;
            
                     // 擷取最多 3 個標籤
                     var itemTags = pickedItem.Tags.ToList();
                     if (itemTags.Count > 3)
                     {
                         // 先確保 targetPreferTag 一定會被選入
                         var selectedTags = new List<string>();
                         if (!string.IsNullOrEmpty(targetPreferTag) && itemTags.Contains(targetPreferTag))
                         {
                             selectedTags.Add(targetPreferTag);
                             itemTags.Remove(targetPreferTag);
                         }
                         
                         // 將剩下的洗牌取到滿 3 個
                         var shuffledRest = itemTags.OrderBy(t => GameRng.ValueKeyed($"{keyPrefix}:TagShuffle:{t}")).ToList();
                         int needed = 3 - selectedTags.Count;
                         selectedTags.AddRange(shuffledRest.Take(needed));
                         request.RequestTags = selectedTags;
                     }
                     else
                     {
                         request.RequestTags = itemTags;
                     }

                     request.TriggeredPreference = !string.IsNullOrEmpty(targetPreferTag);
                     request.IsType2Category = true;

                     return request; // 成功回傳 Type 2 請求
                 }
                 else
                 {
                     // 完全沒有任何可用人類商品，退回種類 1 處理
                     isType2 = false;
                 }
            }
        }

        // --- 若為種類 1 (或從種類 2 退回) ---
        // 隨機選擇物品類型 (3種，機率相同)
        int typeRoll = GameRng.RangeKeyed(0, 3, $"{keyPrefix}:RequestType_Type1");
        request.itemType = (ItemType)typeRoll;

        if (!string.IsNullOrEmpty(targetPreferTag))
        {
            // 強制包含偏好標籤 (數量為 1)
            request.RequestTags = new List<string> { targetPreferTag };
        }
        else
        {
            // 隨機抽選 0-1 個標籤，排除 HateTags
            request.RequestTags = GenerateRequestTagsKeyed(keyPrefix, customer.HateTags);
        }

        request.TriggeredPreference = !string.IsNullOrEmpty(targetPreferTag);
        request.IsType2Category = false; // 此區塊為種類 1

        return request;
    }

    /// <summary>
    /// 從所有標籤中隨機抽取 0-1 個，機率相同
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