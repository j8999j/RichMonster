using System;
using Shop;

/// <summary>
/// 飲料自動販賣機專用的貨架格子：只提供圖片顯示與點擊詳情，
/// 售罄提示由 VendingMachineShopView 以購買按鈕的 sprite 切換呈現。
/// </summary>
public class VendingMachineSlot : ShopSlotBase
{
    private Action<ShelfSlot> _onBuyClick;

    public void Setup(ShelfSlot data, Action<ShopSlotBase> onClick, Action<ShelfSlot> onBuy)
    {
        Setup(data, onClick);
        _onBuyClick = onBuy;
    }
}
