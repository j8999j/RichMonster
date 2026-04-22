using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

/// <summary>
/// Sprite 載入工具類，從 Addressables 載入 SpriteAtlas 後以 GetSprite(name) 取得子圖。
/// 找不到名稱時改載 LossImage，避免 Addressables sub-object 路徑拋出例外。
/// </summary>
public class SpriteLoader
{
    private const string ATLAS_ADDRESS = "ItemsAtlas";
    private const string FALLBACK_ID = "LossImage";

    private static AsyncOperationHandle<SpriteAtlas> _atlasHandle;
    private static bool _atlasRequested;
    private static readonly List<Action<SpriteAtlas>> _atlasWaitList = new();
    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    /// <summary>
    /// 同步式取得 Sprite (如果已快取則直接返回，否則返回 null)
    /// </summary>
    public static Sprite GetCachedSprite(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        return _spriteCache.TryGetValue(itemId, out var sprite) ? sprite : null;
    }

    /// <summary>
    /// 非同步載入 Sprite，完成後透過 callback 回傳
    /// </summary>
    /// <param name="itemId">物品 ID (atlas 內的 sprite 名稱)</param>
    /// <param name="onComplete">載入完成回調 (成功時回傳 Sprite，失敗時回傳 null)</param>
    public static void LoadSpriteAsync(string itemId, Action<Sprite> onComplete)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            onComplete?.Invoke(null);
            return;
        }

        if (_spriteCache.TryGetValue(itemId, out var cached))
        {
            onComplete?.Invoke(cached);
            return;
        }

        GetAtlas(atlas =>
        {
            if (atlas == null)
            {
                Debug.LogError($"[SpriteLoader] ItemsAtlas 載入失敗，無法取得 {itemId}");
                onComplete?.Invoke(null);
                return;
            }

            var sprite = atlas.GetSprite(itemId);
            if (sprite != null)
            {
                _spriteCache[itemId] = sprite;
                onComplete?.Invoke(sprite);
                return;
            }

            if (itemId == FALLBACK_ID)
            {
                Debug.LogWarning($"[SpriteLoader] fallback 圖 {FALLBACK_ID} 也不存在於 atlas");
                onComplete?.Invoke(null);
                return;
            }

            Debug.LogWarning($"[SpriteLoader] 找不到 {itemId}，改用 {FALLBACK_ID}");
            LoadSpriteAsync(FALLBACK_ID, onComplete);
        });
    }

    private static void GetAtlas(Action<SpriteAtlas> onReady)
    {
        if (_atlasHandle.IsValid() && _atlasHandle.IsDone)
        {
            onReady?.Invoke(_atlasHandle.Status == AsyncOperationStatus.Succeeded ? _atlasHandle.Result : null);
            return;
        }

        if (_atlasRequested)
        {
            _atlasWaitList.Add(onReady);
            return;
        }

        _atlasRequested = true;
        _atlasWaitList.Add(onReady);
        _atlasHandle = Addressables.LoadAssetAsync<SpriteAtlas>(ATLAS_ADDRESS);
        _atlasHandle.Completed += h =>
        {
            var result = h.Status == AsyncOperationStatus.Succeeded ? h.Result : null;
            var waiters = _atlasWaitList.ToArray();
            _atlasWaitList.Clear();
            foreach (var cb in waiters) cb?.Invoke(result);
        };
    }

    /// <summary>
    /// 釋放指定 ID 的 Sprite 快取 (atlas 本體不釋放)
    /// </summary>
    public static void Release(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        _spriteCache.Remove(itemId);
    }

    /// <summary>
    /// 釋放所有快取 (包含 atlas handle)
    /// </summary>
    public static void ReleaseAll()
    {
        _spriteCache.Clear();
        _atlasWaitList.Clear();
        if (_atlasHandle.IsValid())
        {
            Addressables.Release(_atlasHandle);
        }
        _atlasHandle = default;
        _atlasRequested = false;
    }

    /// <summary>
    /// 調整圖片縮放，使長邊達到目標尺寸
    /// </summary>
    public static void AdjustImageScale(UnityEngine.UI.Image targetImage, float targetLongEdgeSize)
    {
        if (targetImage == null || targetLongEdgeSize <= 0) return;

        targetImage.SetNativeSize();
        RectTransform rt = targetImage.rectTransform;
        float width = rt.sizeDelta.x;
        float height = rt.sizeDelta.y;

        float longEdge = Mathf.Max(width, height);
        if (longEdge <= 0) return;

        float scale = targetLongEdgeSize / longEdge;
        rt.sizeDelta = new Vector2(width * scale, height * scale);
    }
}
