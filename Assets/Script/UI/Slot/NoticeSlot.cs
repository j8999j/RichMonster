using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class NoticeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _targetImage;
    [SerializeField] private TextMeshProUGUI _amountText;
    [Tooltip("圖片長邊目標尺寸 (設為 0 則不調整)")]
    [SerializeField] private float _targetLongEdgeSize = 64f;

    private NoticeGetItemType _type;
    private int _amount;
    private string _itemId;
    private string _displayName;
    private Action<string> _onHoverEnter;
    private Action _onHoverExit;

    private void Awake()
    {
        if (_targetImage == null)
        {
            _targetImage = GetComponentInChildren<Image>();
        }
    }

    /// <summary>
    /// 設定妖怪幣顯示
    /// </summary>
    public void SetupMonsterGold(int amount, Sprite goldSprite, Action<string> onHoverEnter, Action onHoverExit)
    {
        _type = NoticeGetItemType.MonsterGold;
        _amount = amount;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;

        if (_targetImage != null && goldSprite != null)
        {
            _targetImage.sprite = goldSprite;
            SpriteLoader.AdjustImageScale(_targetImage, _targetLongEdgeSize);
        }
        if (_amountText != null) _amountText.text = $"x{amount}";
    }

    /// <summary>
    /// 設定金幣顯示
    /// </summary>
    public void SetupGold(int amount, Sprite goldSprite, Action<string> onHoverEnter, Action onHoverExit)
    {
        _type = NoticeGetItemType.Gold;
        _amount = amount;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;

        if (_targetImage != null && goldSprite != null)
        {
            _targetImage.sprite = goldSprite;
            SpriteLoader.AdjustImageScale(_targetImage, _targetLongEdgeSize);
        }
        if (_amountText != null) _amountText.text = $"x{amount}";
    }

    /// <summary>
    /// 設定一般物品顯示
    /// </summary>
    public void SetupItem(string itemId, int amount, Action<string> onHoverEnter, Action onHoverExit)
    {
        _type = NoticeGetItemType.Item;
        _itemId = itemId;
        _amount = amount;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;

        var def = DataManager.Instance.GetItemById(itemId);
        _displayName = def != null ? def.Name : itemId;

        if (_targetImage != null)
        {
            SpriteLoader.LoadSpriteAsync(itemId, sprite =>
            {
                if (this != null && _targetImage != null && sprite != null)
                {
                    _targetImage.sprite = sprite;
                    SpriteLoader.AdjustImageScale(_targetImage, _targetLongEdgeSize);
                }
            });
        }
        if (_amountText != null) _amountText.text = $"x{amount}";
    }

    /// <summary>
    /// 設定其他類型顯示（自訂名稱與圖示）
    /// </summary>
    public void SetupOthers(string displayName, Sprite sprite, int amount, Action<string> onHoverEnter, Action onHoverExit)
    {
        _type = NoticeGetItemType.Others;
        _displayName = displayName;
        _amount = amount;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;

        if (_targetImage != null && sprite != null)
        {
            _targetImage.sprite = sprite;
            SpriteLoader.AdjustImageScale(_targetImage, _targetLongEdgeSize);
        }
        if (_amountText != null) _amountText.text = $"x{amount}";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        switch (_type)
        {
            case NoticeGetItemType.MonsterGold:
                _onHoverEnter?.Invoke("妖怪幣");
                break;
            case NoticeGetItemType.Gold:
                _onHoverEnter?.Invoke("金幣");
                break;
            case NoticeGetItemType.Item:
                _onHoverEnter?.Invoke(_displayName);
                break;
            case NoticeGetItemType.Others:
                _onHoverEnter?.Invoke(_displayName);
                break;
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        _onHoverExit?.Invoke();
    }
}


