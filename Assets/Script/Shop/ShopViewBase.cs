using UnityEngine;
using System.Collections.Generic;
using Shop;
using System;

/// <summary>
/// 商店 UI 的抽象基類，定義所有 ShopView 必須提供的契約：
/// - 開關介面 (SetVisible)
/// - 顯示商品列表 (ShowItems)
/// - 刷新顯示 (RefreshAll, 含售罄狀態)
/// - 點擊物品顯示詳細資訊面板（由子類實作）
/// - 互動後顯示邏輯（由子類實作）
/// 子類各自持有自己的 UI 元件 reference 並負責實作。
/// </summary>
public abstract class ShopViewBase : MonoBehaviour
{
    protected List<ShopSlotBase> _activeSlots = new List<ShopSlotBase>();
    protected ShelfSlot _currentSelectedData;
    protected Action<ShelfSlot> _onBuyRequestCallback;
    protected bool PanelisVisible = false;

    public event Action OnCloseShopUI;

    public abstract bool IsVisible { get; }

    /// <summary> 切換商店 UI 開關。 </summary>
    public abstract void SetVisible();

    /// <summary> 顯示商品清單，購買時觸發 onBuyRequest 回呼。 </summary>
    public abstract void ShowItems(List<ShelfSlot> items, Action<ShelfSlot> onBuyRequest);

    /// <summary> 刷新所有貨架格與詳情面板（購買後呼叫）。 </summary>
    public abstract void RefreshAll();

    /// <summary> 子類關閉 UI 時呼叫此方法，觸發外部監聽 (ShopBase.EndInteract)。 </summary>
    protected void InvokeCloseShopUI()
    {
        OnCloseShopUI?.Invoke();
    }
}
