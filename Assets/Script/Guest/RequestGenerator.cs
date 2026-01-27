using System.Collections.Generic;
using System.Linq;
using GameSystem;
using UnityEngine;

/// <summary>
/// 依據顧客清單與玩家持有物，生成顧客需求，需求生成器。
/// 規則：
/// 1) 需求可為指定物品、指定種類、或種類+標籤的組合。
/// 2) 依照當天顧客數量，至少一半需求會來自玩家目前持有的物品，其餘從全物品池隨機挑選。
/// 3) 若職業有 PreferredTags，會提高挑選到含有該標籤物品的機率。
/// </summary>
public class RequestGenerator
{
    private readonly IReadOnlyDictionary<string, ItemDefinition> _itemLookup;
    private readonly IReadOnlyDictionary<string, ProfessionDefinition> _professionLookup;

    public RequestGenerator(
        IReadOnlyDictionary<string, ItemDefinition> itemLookup = null,
        IReadOnlyDictionary<string, ProfessionDefinition> professionLookup = null)
    {
        // 若未注入資料表，直接向 DataManager 取用
        _itemLookup = itemLookup ?? DataManager.Instance?.ItemDict;
        _professionLookup = professionLookup ?? DataManager.Instance?.ProfessionDict;
    }

    /// <summary>
    /// 根據顧客與玩家資料生成需求列表，回傳順序與輸入的 customers 相同。
    /// </summary>
    public List<CustomerRequest> GenerateRequests(List<Customer> customers, IReadOnlyList<Item> playerInventory)
    {
        var results = new List<CustomerRequest>();
        if (customers == null || customers.Count == 0) return results;

        var allItems = BuildItemPool();
        if (allItems.Count == 0)
        {
            Debug.LogWarning("[RequestGenerator] No item data available to build requests.");
            return results;
        }

        var ownedItems = BuildOwnedPool(playerInventory, allItems);
        int inventoryQuota = customers.Count / 2; // 至少一半需求鎖定玩家當前庫存
        var inventorySlots = PickInventorySlots(customers.Count, inventoryQuota);

        for (int i = 0; i < customers.Count; i++)
        {
            bool useInventory = inventorySlots.Contains(i) && ownedItems.Count > 0;
            var pool = useInventory ? ownedItems : allItems;

            // 關鍵：將 customerIndex 作為 salt，避免同日同 key 造成結果過度一致
            ItemDefinition pick = SelectItemWithPreference(customers[i], pool, i);

            if (pick == null)
            {
                Debug.LogWarning("[RequestGenerator] Failed to pick an item definition for request.");
                continue;
            }

            results.Add(BuildRequestForCustomer(customers[i], pick, i));
        }

        return results;
    }

    private List<ItemDefinition> BuildItemPool()
    {
        var lookup = _itemLookup ?? DataManager.Instance?.ItemDict;
        return lookup?
            .Values
            .Where(item => item != null && !string.IsNullOrEmpty(item.Id))
            .ToList()
            ?? new List<ItemDefinition>();
    }

    private List<ItemDefinition> BuildOwnedPool(IReadOnlyList<Item> playerInventory, List<ItemDefinition> allItems)
    {
        var owned = new List<ItemDefinition>();
        // 檢查清單是否為空
        if (playerInventory == null || playerInventory.Count == 0) return owned;

        // 建立字典以加速查詢
        var dict = allItems.ToDictionary(i => i.Id, i => i);

        foreach (var inv in playerInventory)
        {
            if (inv == null || string.IsNullOrEmpty(inv.ItemId)) continue;

            if (dict.TryGetValue(inv.ItemId, out var def))
            {
                owned.Add(def);
            }
        }
        return owned;
    }

    private HashSet<int> PickInventorySlots(int customerCount, int inventoryQuota)
    {
        inventoryQuota = Mathf.Clamp(inventoryQuota, 0, customerCount);

        // 關鍵：每個 index 使用不同 key，維持「同日固定」但避免排序完全一致
        var slots = Enumerable.Range(0, customerCount)
            .OrderBy(i => GameRng.ValueKeyed($"RequestGen:InventorySlot:{i}"))
            .Take(inventoryQuota);

        return new HashSet<int>(slots);
    }

    private ItemDefinition SelectItemWithPreference(Customer customer, List<ItemDefinition> pool, int customerIndex)
    {
        if (pool == null || pool.Count == 0) return null;

        var preferredTags = GetPreferredTags(customer);
        if (preferredTags.Count == 0)
        {
            // 不改變「隨機挑一個」的原有邏輯，只改用 keyed 取得 index
            int idx = GameRng.RangeKeyed(0, pool.Count, $"RequestGen:BasePick:{customerIndex}");
            return pool[idx];
        }

        // 加權：符合任一偏好標籤者額外 +3 權重
        var weighted = new List<(ItemDefinition item, int weight)>();
        foreach (var item in pool)
        {
            if (item == null) continue;
            int weight = 1;
            if (item.Tags != null && item.Tags.Any(tag => preferredTags.Contains(tag)))
            {
                weight += 3;
            }
            weighted.Add((item, weight));
        }

        int total = weighted.Sum(w => w.weight);
        if (total <= 0)
        {
            // 同上：只換成 keyed 版本避免同日同 key 重複
            int idx = GameRng.RangeKeyed(0, pool.Count, $"RequestGen:FallbackPick:{customerIndex}");
            return pool[idx];
        }

        int roll = GameRng.RangeKeyed(0, total, $"RequestGen:WeightedPick:{customerIndex}");
        int cumulative = 0;
        foreach (var entry in weighted)
        {
            cumulative += entry.weight;
            if (roll < cumulative) return entry.item;
        }

        return weighted.Last().item;
    }

    private List<string> GetPreferredTags(Customer customer)
    {
        var lookup = _professionLookup ?? DataManager.Instance?.ProfessionDict;
        if (lookup == null || customer == null || string.IsNullOrEmpty(customer.Profession)) return new List<string>();

        if (lookup.TryGetValue(customer.Profession, out var definition) && definition?.PreferredTags != null)
        {
            return definition.PreferredTags
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList();
        }

        return new List<string>();
    }

    private CustomerRequest BuildRequestForCustomer(Customer customer, ItemDefinition item, int customerIndex)
    {
        bool hasTags = item.Tags != null && item.Tags.Count > 0;

        // 不改變模式機率，只加入 keyed salt
        int mode = hasTags
            ? GameRng.RangeKeyed(0, 4, $"RequestGen:Mode:{customerIndex}")
            : GameRng.RangeKeyed(0, 2, $"RequestGen:Mode:{customerIndex}");

        var request = new CustomerRequest
        {
            TargetItemType = item.Type,
        };

        switch (mode)
        {
            case 0:
                request.Type = RequestType.SpecificItem;
                request.TargetItemId = item.Id;
                break;

            case 1:
                request.Type = RequestType.ItemType;
                break;

            case 2:
                // 新增：只看 Tag
                request.Type = RequestType.ItemTag;
                int tagIndexOnly = GameRng.RangeKeyed(0, item.Tags.Count, $"RequestGen:TagOnly:{customerIndex}");
                request.TargetTag = item.Tags[tagIndexOnly];
                break;

            default:
                request.Type = RequestType.ItemTypeWithTag;
                int tagIndex = GameRng.RangeKeyed(0, item.Tags.Count, $"RequestGen:Tag:{customerIndex}");
                request.TargetTag = item.Tags[tagIndex];
                break;
        }

        request.DialogText = BuildDialogText(request, item);
        return request;
    }
    private string BuildDialogText(CustomerRequest request, ItemDefinition item)
    {
        switch (request.Type)
        {
            case RequestType.SpecificItem:
                return $"我想買 {item.Name ?? item.Id}";
            case RequestType.ItemType:
                return $"有 {request.TargetItemType} 類的商品嗎？";
            case RequestType.ItemTag:
                return $"想找帶有 {request.TargetTag} 標籤的商品";
            case RequestType.ItemTypeWithTag:
                return $"想找 {request.TargetItemType}，最好有 {request.TargetTag} 標籤";
            default:
                return "幫我推薦點東西吧。";
        }
    }
}
