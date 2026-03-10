using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(menuName = "Mission/NpcMission")]
public class NpcMission : ScriptableObject
{
    [NpcIDSelect]
    public string NpcID;
    public string MissionID;
    public string MissionName;
    [TextArea]
    public string MissionDescription;
    public MissionRequirement Requirement;
    public List<MissionReward> Rewards;
    public MissionSaveData Data;
    public bool IsFinish;
    public ItemWorld MissionWorld;
}
public enum RewardType
{
    Gold,
    Item,
    Information
}

[System.Serializable]
public class MissionReward
{
    public RewardType RewardType;
    // 金幣獎勵
    public int GoldAmount;
    // 物品獎勵
    [ItemIDSelect]
    public string ItemID;
    public int ItemAmount;
}