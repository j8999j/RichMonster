using UnityEngine;
using UnityEngine.UI;
using System;
public class OrderSlot : MonoBehaviour
{
    public Action<HumanLargeOrder> OnSelectedLargeOrder;
    public Action<HumanSmallOrder> OnSelectedSmallOrder;
    private HumanLargeOrder humanLargeOrder;
    private HumanSmallOrder humanSmallOrder;
    public GameObject FinishOrderObject;//完成訂單標示物
    public Image _targetImage;
    public Sprite _SmallOrderSprite;
    public Sprite _LargeOrderSprite;
    public Sprite _BigLargeOrderSprite;
    public Sprite _ExLargeOrderSprite;
    public Button InteractButton;
    public void Setup(HumanLargeOrder order, Action<HumanLargeOrder> onClick)
    {
        InteractButton.onClick.RemoveAllListeners();
        humanLargeOrder = order;
        OnSelectedLargeOrder = onClick;
        InteractButton.onClick.AddListener(() => OnSelectedLargeOrder?.Invoke(humanLargeOrder));
        SetOrderView();
    }
    public void Setup(HumanSmallOrder order, Action<HumanSmallOrder> onClick)
    {
        InteractButton.onClick.RemoveAllListeners();
        humanSmallOrder = order;
        OnSelectedSmallOrder = onClick;
        InteractButton.onClick.AddListener(() => OnSelectedSmallOrder?.Invoke(humanSmallOrder));
        SetOrderView();
    }
    private void SetOrderView()
    {
        // 設定訂單圖片
        if(humanLargeOrder != null)
        {
            switch (humanLargeOrder.OrderRank)
            {
                case OrderRank.Common:
                    _targetImage.sprite = _LargeOrderSprite;
                    break;
                case OrderRank.Rare:
                    _targetImage.sprite = _BigLargeOrderSprite;
                    break;
                case OrderRank.SuperRare:
                    _targetImage.sprite = _ExLargeOrderSprite;
                    break;
            }
        }
        else if(humanSmallOrder != null)
        {
            _targetImage.sprite = _SmallOrderSprite;
        }
        
        // 設定完成狀態（大訂單或小訂單，只檢查對應的那個）
        bool isFinished = false;
        
        if(humanLargeOrder != null)
        {
            isFinished = humanLargeOrder.IsFinish;
        }
        else if(humanSmallOrder != null)
        {
            isFinished = humanSmallOrder.IsFinish;
        }
        
        FinishOrderObject.SetActive(isFinished);
        SetGrayscale(isFinished);
    }
    private void SetGrayscale(bool isGray)
    {
        _targetImage.color = isGray ? Color.gray : Color.white;
    }
}