using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using GameSystem;
using Shop;

public class WanderingYokaiMerchantSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Possible spawn points for the wandering merchant.")]
    public Transform[] SpawnPoints;

    [FormerlySerializedAs("CandidateShops")]
    [Tooltip("Possible wandering merchant configs. Each entry defines one shop ID and one greeting dialogue.")]
    public List<WanderingSO> CandidateConfigs = new List<WanderingSO>();

    [Tooltip("Prefab with WanderingYokaiMerchant and its view.")]
    public WanderingYokaiMerchant MerchantPrefab;

    private WanderingYokaiMerchantSave _save;
    private WanderingYokaiMerchant _spawned;

    private void Start()
    {
        LoadOrGenerate();
        Spawn();
    }

    private void LoadOrGenerate()
    {
        int currentDay = GameManager.Instance.gameFlow.CurrentDay;
        var existing = DataManager.Instance.GetDailySaveData<WanderingYokaiMerchantSave>(SaveDataKeys.WanderingYokaiMerchant);

        if (existing != null
            && existing.LastUpdatedDay == currentDay
            && existing.SpawnIndex >= 0
            && existing.ShopIndex >= 0)
        {
            _save = existing;
            return;
        }

        _save = new WanderingYokaiMerchantSave
        {
            LastUpdatedDay = currentDay,
            SpawnIndex = PickSpawnIndex(currentDay),
            ShopIndex = PickShopIndex(currentDay)
        };
        DataManager.Instance.SetDailySaveData(_save);
    }

    private int PickSpawnIndex(int currentDay)
    {
        if (SpawnPoints == null || SpawnPoints.Length == 0) return -1;
        return GameRng.RangeKeyed(0, SpawnPoints.Length, $"WanderingMerchant_Day{currentDay}_Pos");
    }

    private int PickShopIndex(int currentDay)
    {
        if (CandidateConfigs == null || CandidateConfigs.Count == 0) return -1;
        return GameRng.RangeKeyed(0, CandidateConfigs.Count, $"WanderingMerchant_Day{currentDay}_Shop");
    }

    private void Spawn()
    {
        if (MerchantPrefab == null)
        {
            Debug.LogWarning("[WanderingYokaiMerchantSpawner] MerchantPrefab is not assigned.");
            return;
        }
        if (_save.SpawnIndex < 0 || _save.SpawnIndex >= SpawnPoints.Length) return;
        if (_save.ShopIndex < 0 || _save.ShopIndex >= CandidateConfigs.Count) return;

        Transform point = SpawnPoints[_save.SpawnIndex];
        WanderingSO config = CandidateConfigs[_save.ShopIndex];
        if (point == null || config == null) return;

        _spawned = Instantiate(MerchantPrefab, point.position, point.rotation, transform);
        _spawned.Initialize(config);
    }
}

public class WanderingYokaiMerchantSave : IDailySaveData
{
    public string UniqueID => SaveDataKeys.WanderingYokaiMerchant;
    public int LastUpdatedDay { get; set; } = 0;
    public int SpawnIndex = -1;
    public int ShopIndex = -1;
}
