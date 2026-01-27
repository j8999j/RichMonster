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
        AdjustImageScale(icon, SizeMaxEdge);
    }
    private void OnClicked()
    {
        _onClickedCallback?.Invoke(item);
    }
    private void AdjustImageScale(Image targetImage, int TargetLongEdgeSize)
    {
        if (targetImage == null || TargetLongEdgeSize <= 0) return;
        targetImage.SetNativeSize();
        RectTransform rt = targetImage.rectTransform;
        float width = rt.sizeDelta.x;
        float height = rt.sizeDelta.y;

        // 取得長邊
        float longEdge = Mathf.Max(width, height);
        if (longEdge <= 0) return;

        // 計算縮放倍數
        float scale = TargetLongEdgeSize / longEdge;

        // 調整尺寸
        rt.sizeDelta = new Vector2(width * scale, height * scale);
    }
}