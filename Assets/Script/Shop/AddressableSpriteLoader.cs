using Shop;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[RequireComponent(typeof(Image))]
public class AddressableSpriteLoader : MonoBehaviour
{
    [Tooltip("預設圖 (載入失敗或載入中顯示)")]
    public Sprite DefaultSprite;

    private Image _targetImage;
    private AsyncOperationHandle<Sprite> _currentHandle;
    private const string ATLAS_ADDRESS = "ItemsAtlas";
    private string _currentAddress;

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
        if (DefaultSprite != null)
        {
            _targetImage.sprite = DefaultSprite;
        }
    }

    /// <summary>
    /// 外部呼叫此方法來載入圖片
    /// </summary>
    /// <param name="address">Addressables 的地址字串 (例如 ItemID)</param>
    public void LoadSprite(string itemId)
    {
        string path = $"{ATLAS_ADDRESS}[{itemId}]";
        // 1. 如果地址沒變，就不重新載入 (優化)
        if (_currentAddress == path && _currentHandle.IsValid()) return;

        // 2. 如果地址是空的，顯示預設圖並清理舊資源
        if (string.IsNullOrEmpty(path))
        {
            ReleaseCurrentHandle();
            _targetImage.sprite = DefaultSprite;
            _currentAddress = null;
            return;
        }

        // 3. 釋放上一次的圖片 (重要！避免切換商品時記憶體一直疊加)
        ReleaseCurrentHandle();

        _currentAddress = path;

        // 4. 開始非同步載入
        // 這裡不需要 await，我們使用 Completed 事件回調，讓 UI 不會卡住
        var handle = Addressables.LoadAssetAsync<Sprite>(path);
        _currentHandle = handle; // 先存起來以便管理

        handle.Completed += OnLoadCompleted;
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
            Debug.LogError($"[SpriteLoader] 載入失敗 Address: {_currentAddress}");
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
    // 當這個 UI 物件被銷毀時 (例如關閉視窗)，自動釋放記憶體
    private void OnDestroy()
    {
        ReleaseCurrentHandle();
    }
}