using System.Collections.Generic;

public class NPCMissionData
{
    public string NpcID;
    public string NpcName;
}

public class NPCMissionDataDatabase
{
    public List<NPCMissionData> NPCMissionData;
}
public class NPCMissionSave : ISaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    public string MissionID;
    public bool IsFinish;
}