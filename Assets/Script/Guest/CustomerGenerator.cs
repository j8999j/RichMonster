using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using GameSystem;
using CustomerTrait = System.String;

public class DayCustomerSummary
{
        public int Day;
        public int CustomerCount;
        public List<ProfessionType> Types = new List<ProfessionType>();
}

public class DayCustomerDetail
{
        public int Day;
        public List<Customer> Customers = new List<Customer>();
        public List<ProfessionType> Types = new List<ProfessionType>();
}

public class CustomerGenerator
{
        private const int MaxTraitCount = 3;
        private const int MinTraitCount = 0;
        private const int RegularWeight = 100;
        private const int RareWeight = 30;
        private const int RichWeight = 20;
        private const int DayMaxCustomerCount = 12;
        private const int DayMinCustomerCount = 8;

        // Key = trait id (string), Value = definition pulled from TraitDatabase
        private Dictionary<CustomerTrait, TraitDefinition> _traitData;
        private List<ProfessionDefinition> _professionData;
        public CustomerGenerator(Dictionary<CustomerTrait, TraitDefinition> traitData, List<ProfessionDefinition> professionData)
        {
                _traitData = traitData;
                _professionData = professionData;
        }

        /// <summary>
        /// 依照天數隨機生成當日所有客人與特性，富豪正常隨機，不做保底。
        /// 稀有客人每日至少一位（若資料中有稀有職業）。
        /// </summary>
        public List<Customer> GenerateCustomersForDay(int dayNumber, int? explicitCustomerCount = null)
        {
                if (dayNumber < 1) dayNumber = 1;

                if (_professionData == null || _professionData.Count == 0)
                {
                        Debug.LogWarning("[CustomerGenerator] No profession data available, cannot generate customers.");
                        return new List<Customer>();
                }

                // 保留原本「天數決定當日人數」的邏輯
                int randomCount = explicitCustomerCount
                    ?? GameRng.RangeKeyed(DayMinCustomerCount, DayMaxCustomerCount + 1, $"CustomerGen:Day:{dayNumber}:Count");

                randomCount = Mathf.Max(1, randomCount);

                bool hasRareProfession = _professionData.Any(p => p.Type == ProfessionType.Rare);
                int requiredCount = hasRareProfession ? 1 : 0;
                int targetCount = Mathf.Max(randomCount, requiredCount > 0 ? requiredCount : 1);

                var todaysProfessions = new List<ProfessionDefinition>();

                // 至少一位稀有
                if (hasRareProfession)
                {
                        var rarePick = PickProfession(dayNumber, ProfessionType.Rare, pickIndex: 0);
                        if (rarePick != null)
                        {
                                todaysProfessions.Add(rarePick);
                        }
                }

                // 填滿其餘名額（包含所有職業類型）
                // 關鍵修正：每次挑選使用不同 pickIndex 的 key，避免同一店(同日)重複抽到同一職業
                int fillIndex = todaysProfessions.Count;
                while (todaysProfessions.Count < targetCount)
                {
                        var p = PickProfession(dayNumber, null, pickIndex: fillIndex);
                        if (p != null) todaysProfessions.Add(p);
                        fillIndex++;
                }

                // 關鍵修正：建立 Customer 時把 customerIndex 帶入 trait 生成 key
                var customers = new List<Customer>(todaysProfessions.Count);
                for (int i = 0; i < todaysProfessions.Count; i++)
                {
                        var prof = todaysProfessions[i];
                        if (prof == null) continue;
                        customers.Add(CreateCustomer(dayNumber, prof, customerIndex: i));
                }

                return customers;
        }

        /// <summary>
        /// 給定遊戲總天數，輸出每天的客人數量與職業種類清單。
        /// </summary>
        public List<DayCustomerSummary> GenerateDaySummaries(int totalDays)
        {
                var result = new List<DayCustomerSummary>();
                for (int day = 1; day <= totalDays; day++)
                {
                        var customers = GenerateCustomersForDay(day);
                        result.Add(new DayCustomerSummary
                        {
                                Day = day,
                                CustomerCount = customers.Count,
                                Types = customers.Select(c => c.Type).ToList()
                        });
                }
                return result;
        }

        /// <summary>
        /// 給定天數，產出該日所有客人列表與職業種類。
        /// </summary>
        public DayCustomerDetail GenerateDayDetail(int dayNumber, int? explicitCustomerCount = null)
        {
                var customers = GenerateCustomersForDay(dayNumber, explicitCustomerCount);
                return new DayCustomerDetail
                {
                        Day = dayNumber,
                        Customers = customers,
                        Types = customers.Select(c => c.Type).ToList()
                };
        }
        /// <summary>
        /// 保留原邏輯：指定類型就從該類型池挑；否則走加權池。
        /// 關鍵修正：加入 dayNumber + pickIndex 做為 key salt，避免同日多次呼叫回傳相同結果。
        /// </summary>
        private ProfessionDefinition PickProfession(int dayNumber, ProfessionType? preferredType, int pickIndex)
        {
                if (preferredType.HasValue)
                {
                        var poolByType = _professionData.Where(p => p.Type == preferredType.Value).ToList();
                        if (poolByType == null || poolByType.Count == 0) return null;

                        int index = GameRng.RangeKeyed(
                            0, poolByType.Count,
                            $"CustomerGen:Day:{dayNumber}:PickType:{preferredType.Value}:Idx:{pickIndex}"
                        );

                        return poolByType[index];
                }

                return PickProfessionFromPool(dayNumber, _professionData, pickIndex);
        }

        /// <summary>
        /// 保留原邏輯：依職業類型加權抽選。
        /// 關鍵修正：加入 dayNumber + pickIndex 做為 key salt。
        /// </summary>
        private ProfessionDefinition PickProfessionFromPool(int dayNumber, List<ProfessionDefinition> pool, int pickIndex)
        {
                if (pool == null || pool.Count == 0) return null;

                var weightedList = new List<(ProfessionDefinition prof, int weight)>();
                foreach (var prof in pool)
                {
                        int weight = prof.Type switch
                        {
                                ProfessionType.Regular => RegularWeight,
                                ProfessionType.Rare => RareWeight,
                                ProfessionType.Rich => RichWeight,
                                _ => RegularWeight
                        };
                        weightedList.Add((prof, weight));
                }

                int total = weightedList.Sum(p => p.weight);
                if (total <= 0) return null;

                int roll = GameRng.RangeKeyed(
                    0, total,
                    $"CustomerGen:Day:{dayNumber}:PickWeighted:Idx:{pickIndex}"
                );

                int cumulative = 0;
                foreach (var entry in weightedList)
                {
                        cumulative += entry.weight;
                        if (roll < cumulative) return entry.prof;
                }

                return weightedList.Last().prof;
        }

        /// <summary>
        /// 保留原邏輯：每位客人生成一組 traits。
        /// 關鍵修正：把 dayNumber + customerIndex 帶進 trait 抽選 key。
        /// </summary>
        private Customer CreateCustomer(int dayNumber, ProfessionDefinition profession, int customerIndex)
        {
                var traits = GenerateTraits(dayNumber, customerIndex);
                return new Customer(profession, traits);
        }

        /// <summary>
        /// 保留原邏輯：
        /// 1) traitCount 在 [Min, Max] 之間
        /// 2) 候選 traits 做一次「隨機排序」
        /// 3) 依 MutexTag 排除衝突
        /// 關鍵修正：
        /// - traitCount 使用 day + customerIndex key
        /// - 候選排序使用 day + customerIndex + traitId key
        ///   避免所有客人同日排序完全一致。
        /// </summary>
        private List<TraitDefinition> GenerateTraits(int dayNumber, int customerIndex)
        {
                if (_traitData == null || _traitData.Count == 0) return new List<TraitDefinition>();

                List<CustomerTrait> assignedTraits = new List<CustomerTrait>();
                HashSet<string> usedMutexTags = new HashSet<string>();

                int traitCount = Mathf.Clamp(
                    GameRng.RangeKeyed(
                        MinTraitCount, MaxTraitCount + 1,
                        $"CustomerGen:Day:{dayNumber}:TraitCount:C{customerIndex}"
                    ),
                    MinTraitCount, MaxTraitCount
                );

                var candidateTraits = _traitData.Values
                    .Where(t => !string.IsNullOrEmpty(t.Id))
                    .OrderBy(t => GameRng.ValueKeyed(
                        $"CustomerGen:Day:{dayNumber}:TraitShuffle:C{customerIndex}:T{t.Id}"
                    ))
                    .ToList();

                foreach (var candidate in candidateTraits)
                {
                        if (assignedTraits.Count >= traitCount || assignedTraits.Count >= MaxTraitCount) break;

                        string tag = candidate.MutexTag;
                        if (!string.IsNullOrEmpty(tag) && usedMutexTags.Contains(tag))
                        {
                                continue;
                        }

                        assignedTraits.Add(candidate.Id);

                        if (!string.IsNullOrEmpty(tag))
                        {
                                usedMutexTags.Add(tag);
                        }
                }

                return assignedTraits
                    .Where(id => _traitData.ContainsKey(id))
                    .Select(id => _traitData[id])
                    .ToList();
        }
}
