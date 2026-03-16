using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HumanMissionGeneator : MonoBehaviour
{
    /// <summary>
    /// 今日產生的隨機人類任務
    /// </summary>
    public List<NpcMission> CurrentDailyMissions { get; private set; } = new List<NpcMission>();
    public List<NpcOnMap> NpcOnMapList { get; private set; } = new List<NpcOnMap>();
    public List<Transform> NpcOnMapPositionList;
    public NpcOnMap NpcOnMapPrefab;
    void Start()
    {
        GenerateDailyMissions();
    }

    /// <summary>
    /// 每日隨機生成 2~4 個 NPC ID 不重複的人類任務 (ItemWorld.Human)
    /// </summary>
    public void GenerateDailyMissions()
    {
        CurrentDailyMissions.Clear();

        // 1. 取得所有人類任務 (合併含有與不含有情報獎勵的任務)
        var allHumanMissions = new List<NpcMission>();
        if (DataManager.Instance.HumanInfoMissions != null)
        {
            allHumanMissions.AddRange(DataManager.Instance.HumanInfoMissions);
        }
        if (DataManager.Instance.HumanNonInfoMissions != null)
        {
            allHumanMissions.AddRange(DataManager.Instance.HumanNonInfoMissions);
        }

        if (allHumanMissions.Count == 0)
        {
            Debug.LogWarning("[HumanMissionGeneator] 找不到任何人類任務！");
            return;
        }

        // 2. 將任務依 NpcID 分組，以確保不會抽選到同一個 NPC 的不同任務
        var missionsByNpc = allHumanMissions
            .Where(m => !string.IsNullOrEmpty(m.NpcID)) // 確保 NpcID 不為空
            .GroupBy(m => m.NpcID)
            .ToList();

        if (missionsByNpc.Count == 0)
        {
            return;
        }

        // 取得當前天數作為隨機 Key 的一部分，保證同一天結果固定
        int dayNumber = DataManager.Instance.CurrentPlayerData.DaysPlayed;
        int masterSeed = DataManager.Instance.CurrentPlayerData.MasterSeed;

        // 3. 決定要生成的任務數量 (2 ~ 4)
        // 如果可用的 NPC 數量不足，則最多只能生成可用數量的任務
        int maxPossible = Mathf.Min(4, missionsByNpc.Count);
        int targetCount = GameSystem.GameRng.RangeKeyed(Mathf.Min(2, maxPossible), maxPossible + 1, $"HumanMissionGen_Count_{masterSeed}_Day{dayNumber}");

        // 4. 將分組打亂順序 (Fisher-Yates shuffle)
        for (int i = 0; i < missionsByNpc.Count; i++)
        {
            int randomIndex = GameSystem.GameRng.RangeKeyed(i, missionsByNpc.Count, $"HumanMissionGen_Shuffle_{masterSeed}_Day{dayNumber}_{i}");
            var temp = missionsByNpc[i];
            missionsByNpc[i] = missionsByNpc[randomIndex];
            missionsByNpc[randomIndex] = temp;
        }

        // 5. 挑選前 targetCount 個 NPC 的隨機任務
        for (int i = 0; i < targetCount; i++)
        {
            var npcGroup = missionsByNpc[i].ToList();

            // 從這名 NPC 的任務堆中，隨機抽出一個任務
            if (npcGroup.Count > 0)
            {
                int randomMissionIndex = GameSystem.GameRng.RangeKeyed(0, npcGroup.Count, $"HumanMissionGen_Pick_{masterSeed}_Day{dayNumber}_{npcGroup[0].NpcID}");
                CurrentDailyMissions.Add(npcGroup[randomMissionIndex]);
            }
        }

        Debug.Log($"[HumanMissionGeneator] 成功生成 {CurrentDailyMissions.Count} 個任務");
        // --- 處理地圖上的實體生成 ---
        SpawnNpcsOnMap(masterSeed, dayNumber);
    }

    /// <summary>
    /// 將生成的任務 NPC 實例化並放置到地圖指定點位上
    /// </summary>
    private void SpawnNpcsOnMap(int masterSeed, int dayNumber)
    {
        // 1. 清空舊的 NPC 實體
        foreach (var oldNpc in NpcOnMapList)
        {
            if (oldNpc != null)
            {
                Destroy(oldNpc.gameObject);
            }
        }
        NpcOnMapList.Clear();

        if (NpcOnMapPrefab == null || NpcOnMapPositionList == null || NpcOnMapPositionList.Count == 0)
        {
            Debug.LogWarning("[HumanMissionGeneator] 無法生成地圖 NPC，可能有未綁定的 Prefab 或定位點列表(NpcOnMapPositionList)。");
            return;
        }

        // 2. 準備可用的點位列表並打亂順序 (使用 GameRng 保證重現性)
        var availablePositions = new List<Transform>(NpcOnMapPositionList);
        for (int i = 0; i < availablePositions.Count; i++)
        {
            int randomIndex = GameSystem.GameRng.RangeKeyed(i, availablePositions.Count, $"HumanMissionGen_PosShuffle_{masterSeed}_Day{dayNumber}_{i}");
            var tempPos = availablePositions[i];
            availablePositions[i] = availablePositions[randomIndex];
            availablePositions[randomIndex] = tempPos;
        }

        // 3. 依序實例化 NPC 並放在打亂後的點位上
        for (int i = 0; i < CurrentDailyMissions.Count; i++)
        {
            var mission = CurrentDailyMissions[i];

            if (i >= availablePositions.Count)
            {
                Debug.LogWarning($"[HumanMissionGeneator] 地圖定位點不足，無法放置 NPC: {mission.NpcID}");
                break; // 定位點不夠時跳過剩下的 NPC
            }

            var spawnPos = availablePositions[i];
            if (spawnPos == null) continue;

            NpcOnMap newNpc = Instantiate(NpcOnMapPrefab, spawnPos.position, spawnPos.rotation, spawnPos);
            newNpc.setNPC(mission);
            NpcOnMapList.Add(newNpc);
        }
    }
}
