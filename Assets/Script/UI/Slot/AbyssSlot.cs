using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using GameSystem;

public class AbyssSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _targetImage;
    [Tooltip("圖片長邊目標尺寸 (設為 0 則不調整)")]
    public float TargetLongEdgeSize = 80f;
    
    private AbyssRewardType _rewardType;
    private int _goldAmount;
    private string _itemId;
    private Action<string> _onHoverEnter;
    private Action _onHoverExit;

    private void Awake()
    {
        if (_targetImage == null)
        {
            _targetImage = GetComponentInChildren<Image>();
        }
    }

    public void SetupGold(int amount, Sprite goldSprite, Action<string> onHoverEnter, Action onHoverExit)
    {
        _rewardType = AbyssRewardType.MonsterGold;
        _goldAmount = amount;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;

        if (_targetImage != null && goldSprite != null)
        {
            _targetImage.sprite = goldSprite;
            SpriteLoader.AdjustImageScale(_targetImage, TargetLongEdgeSize);
        }
    }

    public void SetupItem(string itemId, Action<string> onHoverEnter, Action onHoverExit)
    {
        _rewardType = AbyssRewardType.Item;
        _itemId = itemId;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;

        if (_targetImage != null)
        {
            SpriteLoader.LoadSpriteAsync(itemId, sprite =>
            {
                if (this != null && _targetImage != null && sprite != null)
                {
                    _targetImage.sprite = sprite;
                    SpriteLoader.AdjustImageScale(_targetImage, TargetLongEdgeSize);
                }
            });
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_rewardType == AbyssRewardType.MonsterGold)
        {
            _onHoverEnter?.Invoke($"妖怪幣 x{_goldAmount}");
        }
        else if (_rewardType == AbyssRewardType.Item)
        {
            // 若未來物品也需要提示數量或名稱，可在此加入
            var def = DataManager.Instance.GetItemById(_itemId);
            if (def != null)
            {
                _onHoverEnter?.Invoke(def.Name); // 滑鼠懸停顯示物品名稱
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onHoverExit?.Invoke();
    }
}
