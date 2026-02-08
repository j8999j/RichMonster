using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// 專用於 Editor 的資料結構 (使用 Newtonsoft.Json)
[System.Serializable]
public class EditorItemDataWrapper
{
    public List<EditorItemData> Items; // 對應 JSON 的 "Items"
}

[System.Serializable]
public class EditorItemData
{
    public string Id;
    public string Name;
    public List<string> Tags;
    public int Type;
    public int World;
    public int Rarity;
    public int BasePrice;
    public string Description;
    public List<string> ShopType;
}

[System.Serializable]
public class EditorTagDataWrapper
{
    public List<EditorTagData> ItemTags; // 對應 JSON 的 "ItemTags"
}

[System.Serializable]
public class EditorTagData
{
    public string TagID;
    public string TagName;
}

public class EditorMissionDataLoader : AssetPostprocessor
{
    // 設定檔案路徑 (相對於 Assets 資料夾)
    private const string JSON_RELATIVE_PATH = "GameResources/items.json";
    private const string TAGS_JSON_PATH = "GameResources/itemtags.json";

    // 緩存資料
    private static string[] _cachedItemIDs;
    private static List<EditorItemData> _cachedItems;
    private static string[] _cachedTagIDs;
    private static List<EditorTagData> _cachedTags;

    // --- 公開接口 ---

    public static string[] GetItemIDs()
    {
        if (_cachedItemIDs == null)
        {
            LoadItemData();
        }
        return _cachedItemIDs;
    }

    public static List<EditorItemData> GetAllItems()
    {
        if (_cachedItems == null)
        {
            LoadItemData();
        }
        return _cachedItems ?? new List<EditorItemData>();
    }

    public static EditorItemData GetItemById(string id)
    {
        if (_cachedItems == null) LoadItemData();
        return _cachedItems?.FirstOrDefault(x => x.Id == id);
    }

    public static string[] GetTagIDs()
    {
        if (_cachedTagIDs == null)
        {
            LoadTagData();
        }
        return _cachedTagIDs;
    }

    public static List<EditorTagData> GetAllTags()
    {
        if (_cachedTags == null)
        {
            LoadTagData();
        }
        return _cachedTags ?? new List<EditorTagData>();
    }

    public static EditorTagData GetTagById(string tagId)
    {
        if (_cachedTags == null) LoadTagData();
        return _cachedTags?.FirstOrDefault(x => x.TagID == tagId);
    }

    // --- 核心讀取邏輯 (使用 Newtonsoft.Json) ---

    private static void LoadItemData()
    {
        string fullPath = Path.Combine(Application.dataPath, JSON_RELATIVE_PATH);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[MissionEditor] 找不到檔案: {fullPath}");
            _cachedItemIDs = new string[] { "Missing_File" };
            _cachedItems = new List<EditorItemData>();
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(fullPath);
            var wrapper = JsonConvert.DeserializeObject<EditorItemDataWrapper>(jsonContent);

            if (wrapper != null && wrapper.Items != null && wrapper.Items.Count > 0)
            {
                _cachedItems = wrapper.Items;
                _cachedItemIDs = _cachedItems.Select(x => x.Id).ToArray();
                Debug.Log($"[MissionEditor] 成功載入 {_cachedItems.Count} 個物品");
            }
            else
            {
                Debug.LogWarning("[MissionEditor] JSON 解析成功但沒有物品資料");
                _cachedItemIDs = new string[] { "No_Items" };
                _cachedItems = new List<EditorItemData>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MissionEditor] 解析失敗: {e.Message}");
            _cachedItemIDs = new string[] { "Error" };
            _cachedItems = new List<EditorItemData>();
        }
    }

    private static void LoadTagData()
    {
        string fullPath = Path.Combine(Application.dataPath, TAGS_JSON_PATH);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[MissionEditor] 找不到標籤檔案: {fullPath}");
            _cachedTagIDs = new string[] { "Missing_File" };
            _cachedTags = new List<EditorTagData>();
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(fullPath);
            var wrapper = JsonConvert.DeserializeObject<EditorTagDataWrapper>(jsonContent);

            if (wrapper != null && wrapper.ItemTags != null && wrapper.ItemTags.Count > 0)
            {
                _cachedTags = wrapper.ItemTags;
                _cachedTagIDs = _cachedTags.Select(x => x.TagID).ToArray();
                Debug.Log($"[MissionEditor] 成功載入 {_cachedTags.Count} 個標籤");
            }
            else
            {
                Debug.LogWarning("[MissionEditor] JSON 解析成功但沒有標籤資料");
                _cachedTagIDs = new string[] { "No_Tags" };
                _cachedTags = new List<EditorTagData>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MissionEditor] 標籤解析失敗: {e.Message}");
            _cachedTagIDs = new string[] { "Error" };
            _cachedTags = new List<EditorTagData>();
        }
    }

    // --- 自動化監聯 ---

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string str in importedAssets)
        {
            if (str.EndsWith(JSON_RELATIVE_PATH))
            {
                Debug.Log($"[MissionEditor] 偵測到 {str} 變更，自動刷新物品緩存！");
                _cachedItemIDs = null;
                _cachedItems = null;
            }
            
            if (str.EndsWith(TAGS_JSON_PATH))
            {
                Debug.Log($"[MissionEditor] 偵測到 {str} 變更，自動刷新標籤緩存！");
                _cachedTagIDs = null;
                _cachedTags = null;
            }
        }
    }
}
