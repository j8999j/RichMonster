using UnityEngine;

[CreateAssetMenu(menuName = "Mission/Requirement/Specific Item")]
public class SpecificItemReq : MissionRequirement
{
    [Tooltip("必須是這個特定的 ItemID")]
    [ItemIDSelect]
    public string TargetItemID;

    public override bool IsMatch(ItemDefinition item)
    {
        return item.Id == TargetItemID;
    }
}
