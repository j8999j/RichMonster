using UnityEngine;

[CreateAssetMenu(menuName = "Mission/Requirement/Specific Tag")]
public class SpecificTagReq : MissionRequirement
{
    [Tooltip("必須含有這個標籤")]
    [ItemTagSelect]
    public string TargetTag;
    
    public override bool IsMatch(ItemDefinition item)
    {
        return item.Tags.Contains(TargetTag);
    }
}
