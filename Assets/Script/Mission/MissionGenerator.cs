using System.Collections.Generic;
using UnityEngine;

public class MissionGenerator
{
    /// <summary>
    /// 取得所有任務 (從 DataManager)
    /// </summary>
    public List<NpcMission> GetAllMissions()
    {
        return DataManager.Instance.GetAllMissions();
    }

    /// <summary>
    /// 取得特定 ID 的任務
    /// </summary>
    public NpcMission GetMissionById(string missionId)
    {
        return DataManager.Instance.GetMissionById(missionId);
    }
}
