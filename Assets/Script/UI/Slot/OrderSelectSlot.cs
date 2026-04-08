using System;
using UnityEngine;
using UnityEngine.UI;
public class OrderSelectSlot : MonoBehaviour
{
    public OrderBagSlot item;
    public Image icon;
    public Button CancelSelectButton;
    public int SizeMaxEdge = 60;
    private Action<OrderBagSlot> _onClickedCallback;
    private void Awake()
    {
        if (CancelSelectButton != null)
        {
            CancelSelectButton.onClick.AddListener(OnClicked);
        }
    }
    public void Setup(OrderBagSlot bagSlot, Action<OrderBagSlot> onCancel)
    {
        item = bagSlot;
        icon.sprite = bagSlot._targetImage.sprite;
        _onClickedCallback = onCancel;
        SpriteLoader.AdjustImageScale(icon, SizeMaxEdge);
    }
    private void OnClicked()
    {
        _onClickedCallback?.Invoke(item);
    }

}