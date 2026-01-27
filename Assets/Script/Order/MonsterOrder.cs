using System.Collections.Generic;
using System.Linq;

public class MonsterProfessionDefinition
{
    public string Id;
    public string ProfessionName;
    //偏好標籤
    public HashSet<string> PreferredTags = new HashSet<string>();
    //預算
    public float BaseBudgetMultiplier;
    //最大偏好加成
    public float PreferMaxPower;
    public string Description;
    public ProfessionType professionType;
    //厭惡標籤
    public HashSet<string> HateTags = new HashSet<string>();
    public string Race;
}
public class MonsterProfessionDatabase
{
    public List<MonsterProfessionDefinition> Professions;
}
public class MonsterTraitDefinition
{
    public string Id;
    public string TraitName;
    public List<string> AddTags;
    public List<string> RemoveTags;
    public float BudgetModifier = 1.0f;// 預算乘數
    // 互斥標籤
    public string MutexTag;
    public string Description;
}

public class MonsterTraitDefinitionDatabase
{
    public List<MonsterTraitDefinition> Traits;
}
public class MonsterCustomer
{
    public MonsterCustomer(MonsterProfessionDefinition professionDefinition, IEnumerable<MonsterTraitDefinition> traitDefinitions)
    {
        // Base identity from profession data
        Profession = professionDefinition?.Id;
        ProfessionName = professionDefinition?.ProfessionName;
        Type = professionDefinition?.professionType ?? ProfessionType.Regular;
        Race = professionDefinition?.Race;

        var traits = traitDefinitions?.ToList() ?? new List<MonsterTraitDefinition>();
        Traits = traits.Select(t => t.Id).ToList();
        TraitNames = traits.Select(t => t.TraitName).ToList();

        // Budget multiplier: base * average of all BudgetModifier
        float baseBudget = professionDefinition?.BaseBudgetMultiplier ?? 1f;
        var budgetMods = traits.Select(t => t.BudgetModifier);
        float averageBudgetMod = budgetMods.Any() ? budgetMods.Average() : 1f;
        BudgetMultiplier = baseBudget * averageBudgetMod;

        // PreferMaxPower from profession
        PreferMaxPower = professionDefinition?.PreferMaxPower ?? 1f;

        // Preferred tags from profession
        PreferredTags = professionDefinition?.PreferredTags?.ToList() ?? new List<string>();
        
        // Hate tags from profession
        HateTags = professionDefinition?.HateTags?.ToList() ?? new List<string>();

        // Apply Add/Remove tags from traits to modify PreferredTags
        // RemoveTags 優先級較高：先收集所有標籤，先加後移
        var allAddTags = new HashSet<string>();
        var allRemoveTags = new HashSet<string>();
        
        foreach (var trait in traits)
        {
            if (trait.AddTags != null)
            {
                foreach (var addTag in trait.AddTags)
                {
                    if (!string.IsNullOrEmpty(addTag))
                        allAddTags.Add(addTag);
                }
            }
            if (trait.RemoveTags != null)
            {
                foreach (var removeTag in trait.RemoveTags)
                {
                    if (!string.IsNullOrEmpty(removeTag))
                        allRemoveTags.Add(removeTag);
                }
            }
        }
        // 先加入 AddTags（排除已存在的）
        foreach (var tag in allAddTags)
        {
            if (!PreferredTags.Contains(tag))
                PreferredTags.Add(tag);
        }
        
        // 後移除 RemoveTags（優先級較高，會覆蓋 Add 的效果）
        foreach (var tag in allRemoveTags)
        {
            PreferredTags.Remove(tag);
        }
        Description = professionDefinition?.Description;
    }

    // Identity
    public string Profession;
    public string ProfessionName;
    public ProfessionType Type;
    public string Race;
    public List<string> Traits = new List<string>();
    public List<string> TraitNames = new List<string>();

    // Stats
    public float BudgetMultiplier;
    public float PreferMaxPower;

    // Tag preferences
    public List<string> PreferredTags = new List<string>();
    public List<string> HateTags = new List<string>();
    public string Description;
}
public class MonsterGuest
{
    public MonsterCustomer monsterCustomer;
    public MonsterRequest monsterRequest;
}

public class MonsterRequest
{
    public ItemType itemType;
    public List<string> RequestTags = new List<string>();
}



