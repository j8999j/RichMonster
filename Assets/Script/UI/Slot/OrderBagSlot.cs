using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class OrderBagSlot : BagSlot
{
    public OrderSelectSlot selectSlot;
    public bool OnSelected {get;private set;}
    private void OnEnable()
    {
        OnSelected = false;
    }
    public void SetOrderSelect(OrderSelectSlot bagSlot)
    {
        selectSlot = bagSlot;
    }
    public void RemoveOrderSelect()
    {
        SetOnSelected(false);
        Destroy(selectSlot.gameObject);
        selectSlot = null;
    }
    public void SetOnSelected(bool onSelected)
    {
        OnSelected = onSelected;
        if(OnSelected)
        {
            SetGrayscale(true);  // 選中時設為灰色
        }
        else
        {
            SetGrayscale(false); // 取消選中時恢復正常顏色
        }
    }
}