using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class EditorDialogueDataLoader : AssetPostprocessor
{
    public sealed class DialogueOption
    {
        public string DialogueId;
        public string AssetPath;
    }

    private static readonly string[] DialogueFolders =
    {
        "Assets/GameSet/TalkText",
        "Assets/Resources/TalkData"
    };

    private static List<DialogueOption> _cachedDialogues;
    private static bool _hasSyncedThisSession;
    private static bool _isSyncing;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += EnsureDialogueEntriesSynced;
    }

    public static List<DialogueOption> GetAllDialogues()
    {
        EnsureDialogueEntriesSynced();

        if (_cachedDialogues != null)
        {
            return _cachedDialogues;
        }

        _cachedDialogues = LoadDialogueOptions();
        return _cachedDialogues;
    }

    public static DialogueOption GetDialogueById(string dialogueId)
    {
        return GetAllDialogues().FirstOrDefault(x => x.DialogueId == dialogueId);
    }

    private static List<DialogueOption> LoadDialogueOptions()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            return new List<DialogueOption>();
        }

        return settings.groups
            .Where(g => g != null)
            .SelectMany(g => g.entries)
            .Where(e =>
                e != null &&
                e.labels.Contains(GameDataLoader.DIALOGUE_LABEL) &&
                AssetDatabase.GetMainAssetTypeAtPath(e.AssetPath) == typeof(TextAsset))
            .Select(e => new DialogueOption
            {
                DialogueId = e.address,
                AssetPath = e.AssetPath
            })
            .OrderBy(e => e.DialogueId)
            .ToList();
    }

    private static void EnsureDialogueEntriesSynced()
    {
        if (_hasSyncedThisSession || _isSyncing)
        {
            return;
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            return;
        }

        AddressableAssetGroup targetGroup = settings.DefaultGroup;
        if (targetGroup == null)
        {
            Debug.LogError("[DialogueEditor] 找不到 Addressables Default Group，無法同步對話資產");
            return;
        }

        _isSyncing = true;
        try
        {
            settings.AddLabel(GameDataLoader.DIALOGUE_LABEL);

            Dictionary<string, DialogueCandidate> preferredById = CollectPreferredCandidates();
            HashSet<string> preferredGuids = new HashSet<string>(preferredById.Values.Select(x => x.Guid));

            foreach (DialogueCandidate candidate in preferredById.Values)
            {
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(candidate.Guid, targetGroup);
                if (entry.address != candidate.DialogueId)
                {
                    entry.SetAddress(candidate.DialogueId);
                }

                if (!entry.labels.Contains(GameDataLoader.DIALOGUE_LABEL))
                {
                    entry.SetLabel(GameDataLoader.DIALOGUE_LABEL, true);
                }
            }

            foreach (AddressableAssetGroup group in settings.groups.Where(g => g != null))
            {
                foreach (AddressableAssetEntry entry in group.entries.Where(e => e != null))
                {
                    if (!entry.labels.Contains(GameDataLoader.DIALOGUE_LABEL))
                    {
                        continue;
                    }

                    if (!IsDialogueAssetPath(entry.AssetPath) || preferredGuids.Contains(entry.guid))
                    {
                        continue;
                    }

                    entry.SetLabel(GameDataLoader.DIALOGUE_LABEL, false);
                }
            }

            AssetDatabase.SaveAssets();
        }
        finally
        {
            _cachedDialogues = null;
            _hasSyncedThisSession = true;
            _isSyncing = false;
        }
    }

    private static Dictionary<string, DialogueCandidate> CollectPreferredCandidates()
    {
        var candidates = new Dictionary<string, DialogueCandidate>();

        for (int priority = 0; priority < DialogueFolders.Length; priority++)
        {
            string folder = DialogueFolders[priority];
            string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { folder });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string dialogueId = Path.GetFileNameWithoutExtension(assetPath);
                var candidate = new DialogueCandidate
                {
                    Guid = guid,
                    AssetPath = assetPath,
                    DialogueId = dialogueId,
                    Priority = priority
                };

                if (candidates.TryGetValue(dialogueId, out DialogueCandidate existing))
                {
                    if (candidate.Priority < existing.Priority)
                    {
                        candidates[dialogueId] = candidate;
                    }

                    Debug.LogWarning($"[DialogueEditor] 偵測到重複的對話 ID '{dialogueId}'，保留較高優先序資產: {candidates[dialogueId].AssetPath}");
                    continue;
                }

                candidates.Add(dialogueId, candidate);
            }
        }

        return candidates;
    }

    private static bool IsDialogueAssetPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        return DialogueFolders.Any(folder => assetPath.StartsWith(folder));
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths).Any(IsRelevantAsset))
        {
            return;
        }

        _cachedDialogues = null;
        _hasSyncedThisSession = false;
        EditorApplication.delayCall += EnsureDialogueEntriesSynced;
    }

    private static bool IsRelevantAsset(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        return IsDialogueAssetPath(assetPath)
            || assetPath.StartsWith("Assets/AddressableAssetsData");
    }

    private sealed class DialogueCandidate
    {
        public string Guid;
        public string AssetPath;
        public string DialogueId;
        public int Priority;
    }
}
