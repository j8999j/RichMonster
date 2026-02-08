using UnityEngine;

[CreateAssetMenu(menuName = "Mission/Requirement/Specific Type")]
public class SpecificTypeReq : MissionRequirement
{
    [Tooltip("必須是這個類型")]
    public ItemType TargetType;

    public override bool IsMatch(ItemDefinition item)
    {
        return item.Type == TargetType;
    }
}
