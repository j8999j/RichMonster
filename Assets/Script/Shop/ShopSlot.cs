using UnityEngine;

/// <summary>
/// 一般商店（GroceryStore / YokaiStore）使用的貨架格子：
/// 於基底 ShopSlotBase 之外，加上售罄遮罩與圖片變灰的表現。
/// </summary>
public class ShopSlot : ShopSlotBase
{
    [Header("Sold Out")]
    public GameObject SoldOutObj; //售罄遮罩

    public override void RefreshView()
    {
        base.RefreshView();
        if (_currentData == null || SoldOutObj == null) return;
        if (_currentData.Purchased)
        {
            SoldOutObj.SetActive(true);
            if (_targetImage != null) _targetImage.color = Color.gray;
        }
    }
}
