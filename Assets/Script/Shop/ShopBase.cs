using UnityEngine;
using Player;
using System.Linq;
using System.Collections.Generic;
using GameSystem;

namespace Shop
{
    public class ShelfSlot
    {
        public int SlotIndex;
        public ItemDefinition Item;
        public int Price;
        public bool Purchased;
        public ShelfSlotVisualInfo VisualInfo;
    }

    public class ShopBase : MonoBehaviour, IInteractable, IMapGuideTarget
    {
        [SerializeField] protected GameObject interactPrompt;
        [SerializeField] protected ShopViewBase _shopUIView;

        [SerializeField]
        [ShopIDSelector]
        protected string ShopID;
        protected string ShopName;
        protected int ShopInventorySize;
        protected List<ItemDefinition> ShopItemList = new List<ItemDefinition>();
        protected ShopShelfData ThisShopShelfData;
        protected List<ShelfSlot> TodayShopItemList = new List<ShelfSlot>();

        public string ID => ShopID;

        public void SetMapGuide()
        {
            NoticeGetItemEvents.InvokeSetMapGuide(ID, transform);
        }

        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
            if (!string.IsNullOrEmpty(ShopID))
            {
                GetShopData();
            }

            if (_shopUIView == null)
            {
                _shopUIView = GetComponent<ShopViewBase>();
            }
        }

        public void ShowPrompt()
        {
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
            }
        }

        public void HidePrompt()
        {
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
        }

        public void Interact()
        {
            OnInteract();
        }

        protected virtual void OnInteract()
        {
        }

        protected virtual void GetShopData()
        {
            if (string.IsNullOrEmpty(ShopID))
            {
                Debug.LogError($"[{nameof(ShopBase)}] ShopID is not assigned on {name}.");
                return;
            }

            var shopData = DataManager.Instance.ShopDict;
            if (shopData.ContainsKey(ShopID))
            {
                ShopID = shopData[ShopID].ShopID;
                ShopName = shopData[ShopID].ShopName;
                ShopInventorySize = shopData[ShopID].ShelfCount;
                Debug.Log($"Loaded shop: {ShopName}, shelf count: {ShopInventorySize}");
            }

            ShopItemList = DataManager.Instance.GetItemsByShopType(ShopID);
        }

        protected List<ShelfSlot> SyncPurchaseState(List<ShelfSlot> shelves)
        {
            var result = new List<ShelfSlot>();
            if (shelves == null) return result;

            var targetShopId = SaveDataKeys.BuildShopShelf(ShopID);
            ThisShopShelfData = DataManager.Instance.GetDailySaveData<ShopShelfData>(targetShopId);
            if (ThisShopShelfData.LastUpdatedDay != GameManager.Instance.gameFlow.CurrentDay)
            {
                ThisShopShelfData = new ShopShelfData { UniqueID = targetShopId, Changes = new List<ShopInventoryChange>() };
            }

            ThisShopShelfData.Changes ??= new List<ShopInventoryChange>();
            foreach (var slot in shelves.Where(s => s != null))
            {
                var change = ThisShopShelfData.Changes.FirstOrDefault(c => c.SlotIndex == slot.SlotIndex);
                bool purchased = change != null ? change.Purchased : slot.Purchased;

                if (change == null)
                {
                    ThisShopShelfData.Changes.Add(new ShopInventoryChange
                    {
                        SlotIndex = slot.SlotIndex,
                        ItemId = slot.Item?.Id,
                        Purchased = purchased
                    });
                }
                else
                {
                    change.ItemId = slot.Item?.Id ?? change.ItemId;
                    change.Purchased = purchased;
                }

                result.Add(new ShelfSlot
                {
                    SlotIndex = slot.SlotIndex,
                    Item = slot.Item,
                    Purchased = purchased
                });
            }

            if (targetShopId == ShopID)
            {
                TodayShopItemList = result;
            }

            return result;
        }

        protected List<ShelfSlot> ApplyPriceFactor(List<ShelfSlot> shelves)
        {
            if (shelves == null) return new List<ShelfSlot>();

            foreach (var slot in shelves)
            {
                slot.Price = PriceCalculationResult(slot);
            }

            Souvenir.SouvenirManager.Instance.ApplyAllShopDiscounts(ShopID, shelves);
            return shelves;
        }

        protected int PriceCalculationResult(ShelfSlot slot)
        {
            return slot.Item.BasePrice;
        }
    }
}
