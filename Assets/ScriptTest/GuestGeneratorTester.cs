using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;

/// <summary>
/// 簡單在 Editor / Play 模式下呼叫，生成今天的 Guest 並列印結果。
/// </summary>
public class GuestGeneratorTester : MonoBehaviour
{
        private GuestGenerator guestGenerator;
        private CustomerGenerator customerGenerator;
        private DataManager dataManager;

        private void Awake()
        {
                guestGenerator = new GuestGenerator();
                var traitDictArg = new Dictionary<string, TraitDefinition>(DataManager.Instance.TraitDict);
                var professionListArg = DataManager.Instance.ProfessionDict.Values.ToList();
                customerGenerator = new CustomerGenerator(traitDictArg, professionListArg);
                dataManager = DataManager.Instance;
        }

        [ContextMenu("Run Guest Generator Test")]
        public void GuestTest()
        {
                var customers = customerGenerator.GenerateCustomersForDay(dayNumber: 1, explicitCustomerCount: 10);
                var playerData = DataManager.Instance.CurrentPlayerData;

                var guests = guestGenerator.BuildGuestsForToday(customers, playerData.InventoryItems);
                foreach (var guest in guests)
                {
                        var request = guest.request;
                        var itemInfo = request == null
                            ? "no request"
                            : $"{request.Type} ({request.TargetItemId ?? request.TargetItemType.ToString()})";
                        Debug.Log($"Guest: {guest.customer?.Profession ?? "unknown"} -> {itemInfo}");
                }
        }
}
