using Shop;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店貨架格子基底：只負責按鈕點擊回呼與物品圖片載入。
/// 售罄遮罩 / 購買後隱藏等視覺差異由子類 override RefreshView() 處理。
/// </summary>
public class ShopSlotBase : MonoBehaviour
{
    [Header("UI Components")]
    public Button InteractButton;
    public Image _targetImage;

    [Tooltip("圖片長邊目標尺寸 (設為 0 則不調整)")]
    public float TargetLongEdgeSize = 100f;

    public ShelfSlot _currentData { get; private set; }

    private Action<ShopSlotBase> _onClickedCallback;
    private string _currentItemId;

    protected virtual void Awake()
    {
        if (InteractButton != null)
        {
            InteractButton.onClick.AddListener(OnClicked);
        }
    }

    void OnEnable()
    {
        if (_currentData == null || _currentData.Item == null) return;
        LoadSprite(_currentData.Item.Id);
        RefreshView();
    }

    #region UIView
    public void Setup(ShelfSlot data, Action<ShopSlotBase> onClick)
    {
        _currentData = data;
        _onClickedCallback = onClick;
        _currentItemId = null;
        RefreshView();
    }

    public virtual void RefreshView()
    {
        if (_currentData == null || _currentData.Item == null) return;
        LoadSprite(_currentData.Item.Id);
    }
    #endregion

    #region LoadImage
    /// <summary>
    /// 外部呼叫此方法來載入圖片
    /// </summary>
    /// <param name="itemId">物品 ID（對應 atlas 中的 sprite 名稱）</param>
    public void LoadSprite(string itemId)
    {
        if (_currentItemId == itemId) return;
        _currentItemId = itemId;

        if (string.IsNullOrEmpty(itemId)) return;

        SpriteLoader.LoadSpriteAsync(itemId, sprite =>
        {
            if (_targetImage != null && _currentItemId == itemId && sprite != null)
            {
                _targetImage.sprite = sprite;
                SpriteLoader.AdjustImageScale(_targetImage, TargetLongEdgeSize);
            }
        });
    }

    #endregion

    #region Event
    private void OnClicked()
    {
        _onClickedCallback?.Invoke(this);
    }
    #endregion
}
