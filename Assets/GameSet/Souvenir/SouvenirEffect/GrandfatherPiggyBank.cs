namespace Souvenir
{
    using UnityEngine;
    [SouvenirDefinition("SouAch_PiggyBank")]
    public class GrandfatherPiggyBank : AchievementSouvenir, IApplyStartEffect
    {
        public override string SouvenirID => "SouAch_PiggyBank";
        
        public void ApplyStartEffect()
        {
            DataManager.Instance.ModifyGold(3000);
            Debug.Log("[Souvenir] 爺爺的存錢筒觸發：增加 3000 金幣");
        }
    }
}
