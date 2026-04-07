namespace SouvenirLibrary
{
    using UnityEngine;
    using Souvenir;
    public class DefaultSpecialSouvenirEffect : SpecialSouvenirEffectBase
    {
        public override string SouvenirID => "SouSpe_Default";
        public override void ApplyEffect()
        {
            Debug.Log("預設的特殊紀念品效果");
        }
    }
}