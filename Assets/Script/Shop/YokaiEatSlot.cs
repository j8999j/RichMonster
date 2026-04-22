/// <summary>
/// 妖界食堂（YokaiEat）使用的貨架格子：購買後隱藏物品圖片，
/// Button 仍可點（可再次查看詳情面板、由 BuyButton 顯示已購狀態）。
/// </summary>
public class YokaiEatSlot : ShopSlotBase
{
    public override void RefreshView()
    {
        base.RefreshView();
        if (_currentData == null || _targetImage == null) return;
        _targetImage.enabled = !_currentData.Purchased;
    }
}
