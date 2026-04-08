using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// 物品圖鑑專用欄位，顯示物品圖片並支援點擊回調與黑色/正常切換
/// </summary>
public class BookItemSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Button InteractButton;
    public Image ItemImage;
    public TextMeshProUGUI ItemName;

    [Tooltip("預設圖 (載入失敗或載入中顯示)")]
    public Sprite DefaultSprite;

    [Tooltip("圖片長邊目標尺寸 (設為 0 則不調整)")]
    public float TargetLongEdgeSize;

    public ItemDefinition CurrentDefinition { get; private set; }
    public bool IsUnlocked { get; private set; }

    private Action<BookItemSlot, bool> _onClickedCallback;
    private string _currentItemId;

    private void Awake()
    {
        if (ItemImage == null)
            ItemImage = GetComponentInChildren<Image>();

        if (DefaultSprite != null && ItemImage != null)
            ItemImage.sprite = DefaultSprite;

        if (InteractButton != null)
            InteractButton.onClick.AddListener(OnClicked);
    }

    /// <summary>
    /// 設定欄位資料並載入圖片
    /// </summary>
    public void Setup(string itemId, bool isUnlocked, Action<BookItemSlot, bool> onClick)
    {
        CurrentDefinition = DataManager.Instance.GetItemById(itemId);
        IsUnlocked = isUnlocked;
        _onClickedCallback = onClick;
        _currentItemId = null; // 重置以強制重新載入圖片
        LoadSprite(itemId);
        ItemName.text = isUnlocked ? CurrentDefinition.Name : "???";
    }

    /// <summary>
    /// 使用 SpriteLoader 非同步載入圖片
    /// </summary>
    private void LoadSprite(string itemId)
    {
        if (_currentItemId == itemId) return;
        _currentItemId = itemId;

        if (ItemImage == null) return;

        if (string.IsNullOrEmpty(itemId))
        {
            ItemImage.sprite = DefaultSprite;
            return;
        }

        SpriteLoader.LoadSpriteAsync(itemId, sprite =>
        {
            if (ItemImage != null && _currentItemId == itemId)
            {
                ItemImage.sprite = sprite ?? DefaultSprite;
                SpriteLoader.AdjustImageScale(ItemImage, TargetLongEdgeSize);
                ItemImage.enabled = true;
            }
        });
    }

    /// <summary>
    /// 設定圖片黑色效果（未收錄）或正常顏色（已收錄）
    /// </summary>
    public void SetBlack(bool black)
    {
        if (ItemImage == null) return;

        if (black)
        {
            // 設為黑色
            ItemImage.color = Color.black;
        }
        else
        {
            // 恢復正常顏色
            ItemImage.color = Color.white;
        }
    }


    private void OnClicked()
    {
        _onClickedCallback?.Invoke(this, IsUnlocked);
    }
}
