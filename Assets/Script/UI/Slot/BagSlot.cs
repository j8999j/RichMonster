using System;
using UnityEngine.UI;
using UnityEngine;
public class BagSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Button InteractButton;
    public Item _currentData { get; private set; }
    public ItemDefinition _currentDefinition { get; private set; }
    private Action<BagSlot> _onClickedCallback; // 當被點擊時，通知選擇與顯示
    [Tooltip("預設圖 (載入失敗或載入中顯示)")]
    public Sprite DefaultSprite;
    
    [Tooltip("圖片長邊目標尺寸 (設為 0 則不調整)")]
    public float TargetLongEdgeSize = 100f;
    public Image _targetImage { get; private set; }
    private string _currentItemId;
    private void Awake()
    {
        _targetImage = transform.GetChild(1).GetComponent<Image>();
        if (DefaultSprite != null)
        {
            _targetImage.sprite = DefaultSprite;
        }
        if (InteractButton != null)
        {
            InteractButton.onClick.AddListener(OnClicked);
        }
    }
    
    void OnEnable()
    {
        // 更新 UI 狀態
        RefreshView();
        
    }
    public void Setup(Item data, Action<BagSlot> onClick)
    {
        _currentData = data;
        _currentDefinition = DataManager.Instance.GetItemById(data.ItemId);
        _onClickedCallback = onClick;
        _currentItemId = null; // 重置以強制重新載入圖片
        RefreshView();
    }
    
    public void RefreshView()
    {
        if (_currentData == null) return;

        // 載入圖片
        LoadSprite(_currentData.ItemId);
    }

    /// <summary>
    /// 使用 SpriteLoader 載入圖片
    /// </summary>
    public void LoadSprite(string itemId)
    {
        // 如果 ID 沒變，不重複載入
        if (_currentItemId == itemId) return;
        _currentItemId = itemId;

        // 確保 _targetImage 已初始化 (處理 Setup 在 Awake 之前被呼叫的情況)
        if (_targetImage == null)
        {
            _targetImage = transform.GetChild(1).GetComponent<Image>();
        }

        if (string.IsNullOrEmpty(itemId))
        {
            _targetImage.sprite = DefaultSprite;
            return;
        }

        // 使用 SpriteLoader 非同步載入
        SpriteLoader.LoadSpriteAsync(itemId, sprite =>
        {
            // 確保 _targetImage 存在且還是同一個 itemId (防止快速切換導致圖片錯亂)
            if (_targetImage != null && _currentItemId == itemId)
            {
                _targetImage.sprite = sprite ?? DefaultSprite;
                AdjustImageScale();
                _targetImage.enabled = true;
            }
        });
        
    }
    
    /// <summary>
    /// 調整圖片縮放，使長邊達到目標尺寸
    /// </summary>
    private void AdjustImageScale()
    {
        if (_targetImage == null || TargetLongEdgeSize <= 0) return;
        _targetImage.SetNativeSize();
        RectTransform rt = _targetImage.rectTransform;
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

    private void OnDestroy()
    {
        // SpriteLoader 使用全域快取，不需要在這裡釋放
    }
    
    /// <summary>
    /// 設定圖片灰階效果
    /// </summary>
    /// <param name="grayscale">true 為灰階，false 為正常顏色</param>
    public void SetGrayscale(bool grayscale)
    {
        if (_targetImage == null) return;
        
        if (grayscale)
        {
            // 設為灰色調
            _targetImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        else
        {
            // 恢復正常顏色
            _targetImage.color = Color.white;
        }
    }

    #region Event
    protected void OnClicked()
    {
        _onClickedCallback?.Invoke(this);
    }
    #endregion 
}
