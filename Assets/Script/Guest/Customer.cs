using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CustomerProfession = System.String;
using CustomerTrait = System.String;
//職業包含特性與其數值
public class Customer
{
        public Customer(ProfessionDefinition professionDefinition, IEnumerable<TraitDefinition> traitDefinitions)
        {
                // Base identity from profession data
                Profession = professionDefinition?.ProfessionId;
                Type = professionDefinition?.Type ?? ProfessionType.Regular;

                var traits = traitDefinitions?.ToList() ?? new List<TraitDefinition>();
                Traits = traits.Select(t => t.Id).ToList();

                // Base stats from profession
                float baseBudget = professionDefinition?.BaseBudgetMultiplier ?? 1f;
                float baseAppraisal = professionDefinition?.BaseAppraisalChance ?? 0f;
                float baseBargain = professionDefinition?.BaseBargainingPower ?? 0f;
                float baseIdentification = professionDefinition?.IdentificationAbility ?? 0f;

                // Budget multiplier: base * average of all BudgetModifier
                var budgetMods = traits.Select(t => t.BudgetModifier);
                float averageBudgetMod = budgetMods.Any() ? budgetMods.Average() : 1f;
                BudgetMultiplier = baseBudget * averageBudgetMod;

                // AppraisalChance: base appraisal scaled by average BargainModifier (no extra appraisal bonus)
                var bargainMods = traits.Select(t => t.BargainModifier);
                float averageBargainMod = bargainMods.Any() ? bargainMods.Average() : 1f;
                AppraisalChance = baseAppraisal * averageBargainMod;

                // Identification uses appraisal effects (clamped)
                float appraisalBonus = SumEffects(traits, "Appraisal");
                IdentificationAbility = Mathf.Clamp01(baseIdentification + appraisalBonus);

                // Bargaining power keeps profession base (traits未指定加成)
                BargainingPower = baseBargain;

                // Patience: additive from effects, minimum 1
                int patienceBonus = Mathf.RoundToInt(SumEffects(traits, "Patience"));
                Patience = Mathf.Max(1, patienceBonus + 5); // 基本耐心值5

                // LoseUp: take the highest LoseUp among traits
                LoseUp = traits.Any() ? traits.Max(t => t.LoseUp) : 0f;

                // Quality preferences: union of all Quality targets in effects
                Quality = CollectQualityTargets(traits);
                //偏好標籤
                PreferredTags = professionDefinition.PreferredTags;

        }

        public string Name;
        public CustomerProfession Profession;
        public ProfessionType Type;
        public List<CustomerTrait> Traits = new List<CustomerTrait>();

        // Runtime stats resolved from base profession and traits
        public int Patience;
        public float BudgetMultiplier;
        public float AppraisalChance;
        public float BargainingPower;
        public float IdentificationAbility;
        public float LoseUp;
        public List<string> Quality = new List<string>();
        public List<string> PreferredTags = new List<string>();

        private static float SumEffects(IEnumerable<TraitDefinition> traits, string effectType)
        {
                return traits
                        .SelectMany(t => t.OtherEffect ?? new List<EffectDefinition>())
                        .Where(e => e != null && e.EffectType == effectType)
                        .Sum(e => e.Value);
        }

        private static List<string> CollectQualityTargets(IEnumerable<TraitDefinition> traits)
        {
                var qualitySets = new List<HashSet<string>>();

                foreach (var eff in traits.SelectMany(t => t.OtherEffect ?? new List<EffectDefinition>()))
                {
                        if (eff == null || eff.EffectType != "Quality" || string.IsNullOrEmpty(eff.Target)) continue;
                        var parts = eff.Target.Split(',');
                        var set = new HashSet<string>();
                        foreach (var part in parts)
                        {
                                var trimmed = part.Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                {
                                        set.Add(trimmed);
                                }
                        }
                        if (set.Count > 0)
                        {
                                qualitySets.Add(set);
                        }
                }

                if (qualitySets.Count == 0) return new List<string>();

                var intersection = new HashSet<string>(qualitySets[0]);
                for (int i = 1; i < qualitySets.Count; i++)
                {
                        intersection.IntersectWith(qualitySets[i]);
                }

                var result = intersection.ToList();
                // 若未取得任何交集，視為三種品質皆可
                if (result.Count == 0)
                {
                        return new List<string> { "Perfect", "Normal", "Damaged" };
                }
                return result;
        }
}
