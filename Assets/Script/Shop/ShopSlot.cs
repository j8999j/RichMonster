using Shop;
using System;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
/// <summary>
/// 商店欄位元件，綁定單一貨架欄位資料。與圖片顯示
/// </summary>
public class ShopSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Button InteractButton;
    public GameObject SoldOutObj; //售罄遮罩
    public ShelfSlot _currentData { get; private set; }
    private Action<ShopSlot> _onClickedCallback; // 當被點擊時，通知選擇與顯示
    [Tooltip("圖片長邊目標尺寸 (設為 0 則不調整)")]
    public float TargetLongEdgeSize = 100f;

    [Tooltip("預設圖 (載入失敗或載入中顯示)")]
    public Sprite DefaultSprite;
    public Image _targetImage { get; private set; }
    private AsyncOperationHandle<Sprite> _currentHandle;
    private const string ATLAS_ADDRESS = "ItemsAtlas";
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
        LoadSprite(_currentData.Item.Id);
        // 更新 UI 狀態
        RefreshView();
    }
    #region UIView
    public void Setup(ShelfSlot data, Action<ShopSlot> onClick)
    {
        _currentData = data;
        _onClickedCallback = onClick;
        _currentItemId = null;
    }
    public void RefreshView()
    {
        if (_currentData == null) return;

        // 1. 載入圖片
        LoadSprite(_currentData.Item.Id);

        // 2. 處理售罄狀態
        if (SoldOutObj != null)
        {
            bool isSoldOut = _currentData.Purchased;
            if (isSoldOut)
            {
                SoldOutObj.SetActive(isSoldOut);
                _targetImage.color = Color.gray;
            }
        }
    }
    #endregion
    #region LoadImage
    /// <summary>
    /// 外部呼叫此方法來載入圖片
    /// </summary>
    /// <param name="address">Addressables 的地址字串 (例如 ItemID)</param>
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

    private void OnLoadCompleted(AsyncOperationHandle<Sprite> handle)
    {
        // 檢查載入是否成功
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 檢查這個 Handle 是否還是當前需要的 (防止快速切換導致圖片錯亂)
            if (_currentHandle.Equals(handle))
            {
                _targetImage.sprite = handle.Result;
                _targetImage.enabled = true;
            }
        }
        else
        {
            Debug.LogError($"[SpriteLoader] 載入失敗 Address: {_currentItemId}");
            _targetImage.sprite = DefaultSprite;
        }
    }

    private void ReleaseCurrentHandle()
    {
        if (_currentHandle.IsValid())
        {
            Addressables.Release(_currentHandle);
        }
    }
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
    // 當這個 UI 物件被銷毀時 (例如關閉視窗)，自動釋放記憶體
    private void OnDestroy()
    {
        ReleaseCurrentHandle();
    }
    #endregion
    #region Event
    private void OnClicked()
    {
        _onClickedCallback?.Invoke(this);
    }
    #endregion 
}
