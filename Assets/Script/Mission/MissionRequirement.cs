using UnityEngine;
using System.Collections.Generic;

public enum RequirementType
{
    None,
    SpecificItem,
    SpecificTag,
    SpecificType
}

[System.Serializable]
public class MissionRequirement
{
    public RequirementType Type = RequirementType.None;

    [Tooltip("指定物品 ID (SpecificItem 時使用)")]
    [ItemIDSelect]
    public string TargetItemID;

    [Tooltip("指定標籤 (SpecificTag 時使用)")]
    [ItemTagSelect]
    public string TargetTag;

    [Tooltip("指定物品類型 (SpecificType 時使用)")]
    public ItemType TargetType;

    /// <summary>
    /// 檢查物品是否符合此需求
    /// </summary>
    public bool IsMatch(ItemDefinition item)
    {
        if (item == null) return false;

        switch (Type)
        {
            case RequirementType.SpecificItem:
                return item.Id == TargetItemID;

            case RequirementType.SpecificTag:
                return item.Tags != null && item.Tags.Contains(TargetTag);

            case RequirementType.SpecificType:
                return item.Type == TargetType;

            case RequirementType.None:
            default:
                return true;
        }
    }
}
