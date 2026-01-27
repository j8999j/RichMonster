using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Sprite 載入工具類，用於從 Addressables Atlas 載入圖片
/// </summary>
public class SpriteLoader
{
    private const string ATLAS_ADDRESS = "ItemsAtlas";

    // 快取已載入的 Sprite Handle
    private static readonly Dictionary<string, AsyncOperationHandle<Sprite>> _cachedHandles 
        = new Dictionary<string, AsyncOperationHandle<Sprite>>();

    /// <summary>
    /// 同步式取得 Sprite (如果已快取則直接返回，否則返回 null)
    /// </summary>
    public static Sprite GetCachedSprite(string itemId)
    {
        string path = $"{ATLAS_ADDRESS}[{itemId}]";
        if (_cachedHandles.TryGetValue(path, out var handle) && handle.IsValid() && handle.IsDone)
        {
            return handle.Result;
        }
        return null;
    }

    /// <summary>
    /// 非同步載入 Sprite，完成後透過 callback 回傳
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <param name="onComplete">載入完成回調 (成功時回傳 Sprite，失敗時回傳 null)</param>
    public static void LoadSpriteAsync(string itemId, Action<Sprite> onComplete)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            onComplete?.Invoke(null);
            return;
        }

        string path = $"{ATLAS_ADDRESS}[{itemId}]";

        // 檢查快取
        if (_cachedHandles.TryGetValue(path, out var existingHandle) && existingHandle.IsValid())
        {
            if (existingHandle.IsDone)
            {
                onComplete?.Invoke(existingHandle.Status == AsyncOperationStatus.Succeeded 
                    ? existingHandle.Result 
                    : null);
            }
            else
            {
                existingHandle.Completed += h => 
                    onComplete?.Invoke(h.Status == AsyncOperationStatus.Succeeded ? h.Result : null);
            }
            return;
        }

        // 開始載入
        var handle = Addressables.LoadAssetAsync<Sprite>(path);
        _cachedHandles[path] = handle;

        handle.Completed += h =>
        {
            if (h.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(h.Result);
            }
            else
            {
                Debug.LogWarning($"[SpriteLoader] 載入失敗: {path}");
                onComplete?.Invoke(null);
            }
        };
    }

    /// <summary>
    /// 釋放指定 ID 的快取
    /// </summary>
    public static void Release(string itemId)
    {
        string path = $"{ATLAS_ADDRESS}[{itemId}]";
        if (_cachedHandles.TryGetValue(path, out var handle))
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            _cachedHandles.Remove(path);
        }
    }

    /// <summary>
    /// 釋放所有快取
    /// </summary>
    public static void ReleaseAll()
    {
        foreach (var kvp in _cachedHandles)
        {
            if (kvp.Value.IsValid())
            {
                Addressables.Release(kvp.Value);
            }
        }
        _cachedHandles.Clear();
    }
}
