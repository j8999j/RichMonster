using UnityEngine;

[CreateAssetMenu(menuName = "Mission/NpcMission")]
public class NpcMission : ScriptableObject
{
    public string NpcID;
    public string MissionID;
    public string MissionName;
    [TextArea]
    public string MissionDescription;
    public MissionRequirement Requirement;
    [ItemIDSelect]
    public string RewardItemID;
    public NpcMissionData Data;
    public bool IsFinish;
    public ItemWorld MissionWorld;
}
