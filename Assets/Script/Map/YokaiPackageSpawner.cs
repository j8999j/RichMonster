using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystem;

/// <summary>
/// 遺落的妖怪包裹生成器：使用 GameRng 依當前天數固定式地從 SpawnPoints 中抽出 SpawnCount
/// 個位置生成包裹物件。包裹被拾取後隱藏、給予獎勵並顯示 NoticeGetItem，狀態會存檔（每日重置）。
/// </summary>
public class YokaiPackageSpawner : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("所有可能的包裹生成位置")]
    public Transform[] SpawnPoints;

    [Tooltip("每日要抽選生成的包裹數量")]
    public int SpawnCount = 3;

    [Tooltip("包裹預製物（需掛 YokaiPackage 元件）")]
    public YokaiPackage PackagePrefab;

    [Tooltip("獎勵設定檔")]
    public YokaiPackageRewardConfig RewardConfig;

    [Tooltip("NoticeGetItem 顯示的來源名稱")]
    public string NoticeSource = "遺落的妖怪包裹";

    [Header("SFX")]
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolumeScale = 1f;

    private YokaiPackageSave _save;
    private readonly Dictionary<int, YokaiPackage> _spawned = new Dictionary<int, YokaiPackage>();

    private void Start()
    {
        LoadOrGenerate();
        SpawnPackages();
    }

    private void LoadOrGenerate()
    {
        int currentDay = GameManager.Instance.gameFlow.CurrentDay;
        var existing = DataManager.Instance.GetPlayerSaveData<YokaiPackageSave>(SaveDataKeys.YokaiPackage);

        if (existing != null && existing.LastUpdatedDay == currentDay && existing.SpawnIndices.Count > 0)
        {
            _save = existing;
            return;
        }

        _save = new YokaiPackageSave
        {
            LastUpdatedDay = currentDay,
            SpawnIndices = PickSpawnIndices(currentDay),
            PickedIndices = new List<int>()
        };
        DataManager.Instance.SetPlayerData(SaveDataKeys.YokaiPackage, _save);
    }

    private List<int> PickSpawnIndices(int currentDay)
    {
        var result = new List<int>();
        if (SpawnPoints == null || SpawnPoints.Length == 0) return result;

        var candidates = Enumerable.Range(0, SpawnPoints.Length).ToList();
        int count = Mathf.Min(SpawnCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            int pick = GameRng.RangeKeyed(0, candidates.Count, $"YokaiPackage_Day{currentDay}_Pos{i}");
            result.Add(candidates[pick]);
            candidates.RemoveAt(pick);
        }
        return result;
    }

    private void SpawnPackages()
    {
        if (PackagePrefab == null || SpawnPoints == null) return;

        foreach (int index in _save.SpawnIndices)
        {
            if (_save.PickedIndices.Contains(index)) continue;
            if (index < 0 || index >= SpawnPoints.Length) continue;
            Transform point = SpawnPoints[index];
            if (point == null) continue;

            YokaiPackage pkg = Instantiate(PackagePrefab, point.position, point.rotation, transform);
            pkg.Initialize(this, index);
            _spawned[index] = pkg;
        }
    }

    /// <summary>
    /// 由 YokaiPackage 在互動時呼叫：抽獎、給獎、顯示通知、寫入存檔並隱藏包裹。
    /// </summary>
    public void OnPackagePicked(int index)
    {
        if (_save.PickedIndices.Contains(index)) return;

        PlaySfx(pickupSfx);

        int currentDay = GameManager.Instance.gameFlow.CurrentDay;
        var reward = RollReward(currentDay, index);
        var noticeItems = new List<NoticeItemEntry>();

        if (reward != null)
        {
            if (reward.RewardType == AbyssRewardType.MonsterGold)
            {
                DataManager.Instance.ModifyMonsterGold(reward.GoldAmount);
                noticeItems.Add(NoticeItemEntry.MonsterGold(reward.GoldAmount));
            }
            else if (reward.RewardType == AbyssRewardType.Item)
            {
                for (int i = 0; i < reward.ItemAmount; i++)
                {
                    DataManager.Instance.AddItem(reward.ItemID, 0);
                }
                noticeItems.Add(NoticeItemEntry.ItemEntry(reward.ItemID, reward.ItemAmount));
            }
        }

        if (noticeItems.Count > 0)
        {
            NoticeGetItemEvents.InvokeShowNotice(NoticeSource, noticeItems);
        }

        _save.PickedIndices.Add(index);
        DataManager.Instance.SetPlayerData(SaveDataKeys.YokaiPackage, _save);

        if (_spawned.TryGetValue(index, out var pkg) && pkg != null)
        {
            pkg.gameObject.SetActive(false);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
    }

    private AbyssRewardItem RollReward(int currentDay, int index)
    {
        if (RewardConfig == null || RewardConfig.Rewards == null || RewardConfig.Rewards.Count == 0) return null;

        int totalWeight = RewardConfig.Rewards.Sum(r => r.Weight);
        if (totalWeight <= 0) return null;

        int roll = GameRng.RangeKeyed(0, totalWeight, $"YokaiPackage_Day{currentDay}_Reward{index}");
        int acc = 0;
        foreach (var r in RewardConfig.Rewards)
        {
            acc += r.Weight;
            if (roll < acc) return r;
        }
        return RewardConfig.Rewards[RewardConfig.Rewards.Count - 1];
    }
}

public class YokaiPackageSave : ISaveData
{
    public string UniqueID => SaveDataKeys.YokaiPackage;
    public int LastUpdatedDay { get; set; } = 0;
    public List<int> SpawnIndices = new List<int>();
    public List<int> PickedIndices = new List<int>();
}
