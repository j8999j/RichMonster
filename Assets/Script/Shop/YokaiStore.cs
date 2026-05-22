using GameSystem;

namespace Shop
{
    public class YokaiStore : ShelfShopBase
    {
        protected override string LockSource => PlayerLockSources.YokaiStore;
        protected override GameCurrencyType CurrencyType => GameCurrencyType.MonsterGold;
    }
}
