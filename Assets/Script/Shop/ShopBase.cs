using UnityEngine;
using Player;
using System.Linq;
using System.Collections.Generic;
using GameSystem;
namespace Shop
{
    //貨架上的商品
    public class ShelfSlot
    {
        public int SlotIndex;
        public ItemDefinition Item;
        public int Price;//商品售價
        public bool Purchased; // true: 已被購買, false: 尚未購買
    }
    public class ShopBase : MonoBehaviour, IInteractable
    {

        [SerializeField] protected GameObject interactPrompt;
        [SerializeField] protected ShopUIView _shopUIView;
        protected string ShopID;
        protected string ShopName;
        //商店貨架數量
        protected int ShopInventorySize;
        //商店中可以刷新的物品
        protected List<ItemDefinition> ShopItemList = new List<ItemDefinition>();
        //商店貨架資料
        protected ShopShelfData ThisShopShelfData;
        //今日刷新的物品
        protected List<ShelfSlot> TodayShopItemList = new List<ShelfSlot>();
        public ShopSO ShopSet;
        void Awake()
        {
            ShopID = ShopSet.ShopID;
        }
        void Start()
        {
            GetShopData();
            _shopUIView = GetComponent<ShopUIView>();
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
            var shopData = DataManager.Instance.ShopDict;
            if (shopData.ContainsKey(ShopSet.ShopID))
            {
                ShopID = shopData[ShopSet.ShopID].ShopID;
                ShopName = shopData[ShopSet.ShopID].ShopName;
                ShopInventorySize = shopData[ShopSet.ShopID].ShelfCount;
                Debug.Log($"已載入商店名稱: {ShopName}, 貨架數量: {ShopInventorySize}");
            }
            ShopItemList = DataManager.Instance.GetItemsByShopType(ShopID);
            foreach (var item in ShopItemList)
            {
                //Debug.Log($"{ShopName}可以刷新{item.Name}");
            }
        }

        /// <summary>
        /// 將貨架的購買狀態與玩家存檔同步，並回傳更新後的清單。
        /// </summary>
        protected List<ShelfSlot> SyncPurchaseState(List<ShelfSlot> shelves)
        {
            var result = new List<ShelfSlot>();
            if (shelves == null) return result;

            var player = DataManager.Instance.CurrentPlayerData;
            var targetShopId = ShopID + "ShopShelfData";
            ThisShopShelfData = DataManager.Instance.GetPlayerData<ShopShelfData>(targetShopId);
            if (ThisShopShelfData.LastUpdatedDay != GameManager.Instance.gameFlow.CurrentDay)//如果不是同一天，重置商店
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

        /// <summary>
        /// 依事件倍率計算每個貨架商品最終售價（BasePrice * priceFactor），更新並回傳清單。
        /// </summary>
        protected List<ShelfSlot> ApplyPriceFactor(List<ShelfSlot> shelves)
        {
            if (shelves == null) return new List<ShelfSlot>();
            foreach (var slot in shelves)
            {
                slot.Price = PriceCalculationResult(slot);
            }
            return shelves;
        }
        /// <summary>
        /// 事件計算最終售價（BasePrice * priceFactor），更新並回傳清單。
        /// </summary>
        protected int PriceCalculationResult(ShelfSlot slot)
        {
            //根據事件與物品標籤計算價格後回傳
            return slot.Item.BasePrice;
        }
    }
}
