using System.Collections.Generic;
using UnityEngine;
using GameSystem;

namespace Shop
{
    public class WanderingYokaiMerchant : ShopBase, Itrade
    {
        private string _greetingDialogueId;
        private bool _initialized;
        private bool _hasGreetedToday;
        private bool _inDialogue;

        private readonly Dictionary<Rarity, int> _rarityWeight = new Dictionary<Rarity, int>
        {
            { Rarity.Common, 80 },
            { Rarity.Uncommon, 60 },
            { Rarity.Rare, 40 },
            { Rarity.Epic, 20 },
            { Rarity.Legendary, 10 }
        };

        private void OnEnable()
        {
            if (_shopUIView != null)
            {
                _shopUIView.OnCloseShopUI += EndInteract;
            }
        }

        private void OnDisable()
        {
            if (_shopUIView != null)
            {
                _shopUIView.OnCloseShopUI -= EndInteract;
            }
        }

        protected override void Awake()
        {
        }

        protected override void Start()
        {
            if (_shopUIView == null)
            {
                _shopUIView = GetComponent<ShopViewBase>();
            }
        }

        public void Initialize(WanderingSO config)
        {
            if (config == null)
            {
                Debug.LogError("[WanderingYokaiMerchant] Initialize received a null WanderingSO.");
                return;
            }
            if (string.IsNullOrEmpty(config.ShopID))
            {
                Debug.LogError("[WanderingYokaiMerchant] WanderingSO is missing ShopID.");
                return;
            }

            ShopID = config.ShopID;
            _greetingDialogueId = config.GreetingDialogueId;
            GetShopData();

            if (_shopUIView == null)
            {
                _shopUIView = GetComponent<ShopViewBase>();
            }
            if (_shopUIView != null && !_initialized)
            {
                _shopUIView.OnCloseShopUI -= EndInteract;
                _shopUIView.OnCloseShopUI += EndInteract;
            }

            _initialized = true;
        }

        protected override void OnInteract()
        {
            if (!_initialized || _inDialogue) return;

            if (GameManager.Instance.IsPlayerMoveLocked(PlayerLockSources.WanderingYokaiMerchant))
            {
                _shopUIView.SetVisible();
                GameManager.Instance.UnlockPlayerMove(PlayerLockSources.WanderingYokaiMerchant);
                return;
            }

            if (_hasGreetedToday)
            {
                OpenShop();
            }
            else
            {
                StartGreetingDialogue();
            }
        }

        private async void StartGreetingDialogue()
        {
            var talk = GameManager.Instance.talkSystem;
            if (talk == null)
            {
                _hasGreetedToday = true;
                OpenShop();
                return;
            }

            _inDialogue = true;
            string dialogueText = await GameDataLoader.LoadDialogueTextAsync(_greetingDialogueId);
            if (this == null)
            {
                return;
            }

            talk = GameManager.Instance.talkSystem;
            if (talk == null || string.IsNullOrEmpty(dialogueText))
            {
                _inDialogue = false;
                _hasGreetedToday = true;
                OpenShop();
                return;
            }

            bool completed = await talk.PlayDialogueAsync(dialogueText);
            if (this == null)
            {
                return;
            }

            _inDialogue = false;
            if (!completed)
            {
                return;
            }

            _hasGreetedToday = true;
            OpenShop();
        }

        private void OpenShop()
        {
            if (!GameManager.Instance.IsPlayerMoveLocked(PlayerLockSources.WanderingYokaiMerchant))
            {
                GameManager.Instance.LockPlayerMove(PlayerLockSources.WanderingYokaiMerchant);
            }

            int currentDay = GameManager.Instance.gameFlow.CurrentDay;
            var items = SyncPurchaseState(GenerateTodayShopItems(currentDay));
            items = ApplyPriceFactor(items);

            if (_shopUIView != null)
            {
                _shopUIView.ShowItems(items, OnPlayerTryToBuyItem);
            }
            _shopUIView.SetVisible();
        }

        private async void EndInteract()
        {
            await GameManager.Instance.gameFlow.SaveGameAsync();
            GameManager.Instance.UnlockPlayerMove(PlayerLockSources.WanderingYokaiMerchant);
        }

        private void OnPlayerTryToBuyItem(ShelfSlot slotData)
        {
            tradeitem(slotData);
        }

        public List<ShelfSlot> GenerateTodayShopItems(int currentDay)
        {
            int slotCount = Mathf.Max(1, ShopInventorySize);
            TodayShopItemList = new List<ShelfSlot>(slotCount);

            if (ShopItemList == null || ShopItemList.Count == 0)
            {
                return TodayShopItemList;
            }

            int actualCount = Mathf.Min(slotCount, ShopItemList.Count);
            var pool = new List<ItemDefinition>(ShopItemList);

            for (int i = 0; i < actualCount; i++)
            {
                var item = PickWeighted(pool, i);
                if (item == null) break;
                pool.Remove(item);
                TodayShopItemList.Add(new ShelfSlot
                {
                    SlotIndex = i,
                    Item = item,
                    Purchased = false
                });
            }

            return TodayShopItemList;
        }

        private ItemDefinition PickWeighted(List<ItemDefinition> pool, int index)
        {
            if (pool == null || pool.Count == 0) return null;

            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += GetItemWeight(pool[i]);
            }

            if (totalWeight <= 0) return null;

            int roll = GameRng.RangeKeyed(0, totalWeight, ShopID + index);
            for (int i = 0; i < pool.Count; i++)
            {
                int weight = GetItemWeight(pool[i]);
                if (roll < weight) return pool[i];
                roll -= weight;
            }

            return pool[pool.Count - 1];
        }

        private int GetItemWeight(ItemDefinition item)
        {
            if (item == null) return 0;
            return _rarityWeight.TryGetValue(item.Rarity, out int weight) ? weight : 1;
        }

        public void tradeitem(ShelfSlot shelfSlot)
        {
            if (shelfSlot.Purchased || shelfSlot.Item == null)
            {
                _shopUIView.PlayBuyFailedSfx();
                return;
            }

            if (DataManager.Instance.TrySpendMonsterGoldForItemPurchase(shelfSlot.Price))
            {
                DataManager.Instance.AddItem(shelfSlot.Item.Id, shelfSlot.Price);
                Debug.Log($"[WanderingYokaiMerchant] Bought item: {shelfSlot.Item.Name} (price: {shelfSlot.Price})");
                shelfSlot.Purchased = true;
                NewShopShelfData(shelfSlot);
                SyncPurchaseState(TodayShopItemList);
                _shopUIView.RefreshAll();
                _shopUIView.PlayBuySuccessSfx();
            }
            else
            {
                _shopUIView.PlayBuyFailedSfx();
                Debug.Log("[WanderingYokaiMerchant] Not enough MonsterGold.");
            }
        }

        public void NewShopShelfData(ShelfSlot shelfSlot)
        {
            ThisShopShelfData.UniqueID = ShopID + "ShopShelfData";
            ThisShopShelfData.LastUpdatedDay = GameManager.Instance.gameFlow.CurrentDay;
            ThisShopShelfData.Changes[shelfSlot.SlotIndex] = new ShopInventoryChange
            {
                SlotIndex = shelfSlot.SlotIndex,
                ItemId = shelfSlot.Item.Id,
                Purchased = true
            };
            DataManager.Instance.AddShopShelfData(ThisShopShelfData);
        }
    }
}
