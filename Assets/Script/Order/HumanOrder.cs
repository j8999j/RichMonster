using System.Collections.Generic;
public class HumanLargeOrder
{
    public string OrderId;
    public string OrderName;
    public string OrderDescription;
    public string OrderEventId;
    public bool IsFinish;
    public ItemType OrderType;
    public List<string> OrderNeedTags;
    public OrderRank OrderRank;
}
public class HumanLargeOrderDatabase
{
    public List<HumanLargeOrder> LargeOrders;
}
public class HumanSmallOrder
{
    public string OrderId;
    public string OrderName;
    public string OrderDescription;
    public bool IsFinish;
    public ItemType OrderType;
    public List<string> OrderNeedTags;
}
public class HumanSmallOrderDatabase
{
    public List<HumanSmallOrder> SmallOrders;
}
public enum OrderRank
{
    Common,
    Rare,
    SuperRare
}
