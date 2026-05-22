using GameSystem;

namespace Shop
{
    public class YokaiEat : ShelfShopBase
    {
        protected override string LockSource => PlayerLockSources.YokaiEat;
        protected override GameCurrencyType CurrencyType => GameCurrencyType.MonsterGold;
    }
}
