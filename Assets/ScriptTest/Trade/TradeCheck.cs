using System.Collections.Generic;
using System.Linq;
public class TradeCheck
{
    public bool SelectToRequest(Guest nowGuest, Item SelectItem)
    {
        if (nowGuest == null || nowGuest.request == null || SelectItem == null)
        {
            return false;
        }

        var request = nowGuest.request;
        if (string.IsNullOrEmpty(SelectItem.ItemId))
        {
            return false;
        }

        if (!DataManager.Instance.ItemDict.TryGetValue(SelectItem.ItemId, out var itemDef) || itemDef == null)
        {
            //"[TradeMode] 找不到對應的 ItemDefinition: {SelectItem.ItemId}");
            return false;
        }

        switch (request.Type)
        {
            case RequestType.SpecificItem:
                return request.TargetItemId == SelectItem.ItemId;

            case RequestType.ItemType:
                return itemDef.Type == request.TargetItemType;

            case RequestType.ItemTypeWithTag:
                bool typeMatch = itemDef.Type == request.TargetItemType;
                bool tagMatch = itemDef.Tags != null && itemDef.Tags.Contains(request.TargetTag);
                return typeMatch && tagMatch;

            default:
                return false;
        }
    }
    /// <summary>
    /// 根據當日顧客清單生成對應的 Guest 清單（內含需求）。
    /// </summary>
    public List<Guest> BuildGuestsForToday(List<Customer> customers, IReadOnlyPlayerData playerData)
    {
        if (customers == null || customers.Count == 0) return new List<Guest>();

        var requestGenerator = new RequestGenerator();
        var requests = requestGenerator.GenerateRequests(customers, playerData.InventoryItems);

        var guests = new List<Guest>();
        for (int i = 0; i < customers.Count; i++)
        {
            var customer = customers[i];
            var request = i < requests.Count ? requests[i] : null;
            var guest = new Guest
            {
                customer = customer,
                request = request
            };
            guests.Add(guest);
        }
        return guests;
    }
    public int MarketPriceCalculate(ItemDefinition item, ItemQuality itemQuality, Customer customer)
    {
        var basePrice = item.BasePrice;
        float priceIndex = 1f;
        float EventPrice = 1f;
        float qualityPrice = 1f;

        switch (itemQuality)
        {
            case ItemQuality.Good:
                qualityPrice = 1.2f;
                break;
            case ItemQuality.Normal:
                qualityPrice = 1f;
                break;
            case ItemQuality.Bad:
                qualityPrice = 0.6f;
                break;
        }
        if (item.Tags.Any(tag => customer.PreferredTags.Contains(tag)))
        {
            return (int)(basePrice * priceIndex * qualityPrice * EventPrice * customer.BargainingPower);
        }
        else
        {
            return (int)(basePrice * priceIndex * qualityPrice * EventPrice);
        }
    }
}