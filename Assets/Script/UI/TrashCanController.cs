using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Player;
using GameSystem;

public class TrashCanController : MonoBehaviour, IInteractable, IMapGuideTarget
{
    [Header("視圖")]
    public TrashCanView View;
    public GameObject Prompt;
    public string ID => GuideIDs.Interactable.TrashCan;
    [Header("設定")]
    public bool OnlyHumanWorld = true;
    private Item _pendingDiscardItem;
    private TradeSlot _pendingDiscardSlot;
    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID, transform);
    }
    void Start()
    {
        if (View == null)
        {
            View = GetComponent<TrashCanView>();
        }

        if (View != null)
        {
            View.OnItemDropToTrash += HandleItemDrop;
            View.OnConfirmDiscard += HandleConfirmDiscard;
            View.OnCancelDiscard += HandleCancelDiscard;
            View.OnCloseDiscardUI += HandleCloseDiscardUI;
        }
    }

    /// <summary>
    /// 開啟垃圾桶 UI
    /// </summary>
    public void OpenTrashCan()
    {
        if (View != null)
        {
            View.OpenUI();
            RefreshBagItems();
        }
    }

    /// <summary>
    /// 玩家與垃圾桶互動時呼叫
    /// </summary>
    public void Interact()
    {
        if (GameManager.Instance.IsPlayerMoveLocked(PlayerLockSources.TrashCan))
        {
            ClosePanel();
        }
        else
        {
            OpenTrashCan();
            GameManager.Instance.LockPlayerMove(PlayerLockSources.TrashCan);
        }
    }
    public void ClosePanel()
    {
        View.CloseUI();
        GameManager.Instance.UnlockPlayerMove(PlayerLockSources.TrashCan);
    }

    public void ShowPrompt()
    {
        if (Prompt != null) Prompt.SetActive(true);
    }
    public void HidePrompt()
    {
        if (Prompt != null) Prompt.SetActive(false);
    }

    /// <summary>
    /// 刷新背包物品顯示
    /// </summary>
    public void RefreshBagItems()
    {
        if (DataManager.Instance == null || View == null) return;

        var items = DataManager.Instance.CurrentPlayerData?.InventoryItems;
        if (items == null) return;

        // 根據設定過濾物品
        ItemWorld targetWorld = OnlyHumanWorld ? ItemWorld.Human : ItemWorld.Monster;
        List<Item> displayItems = items.Where(item =>
        {
            var definition = DataManager.Instance.GetItemById(item.ItemId);
            return definition != null && definition.World == targetWorld;
        }).ToList();

        View.ShowBagItems(displayItems);
    }

    private void HandleItemDrop(TradeSlot slot)
    {
        if (slot == null || slot._currentData == null) return;

        _pendingDiscardItem = slot._currentData;
        _pendingDiscardSlot = slot; // 記錄被拖曳的 Slot

        // 取得物品定義以顯示名稱
        var definition = DataManager.Instance.GetItemById(_pendingDiscardItem.ItemId);
        string itemName = definition != null ? definition.Name : _pendingDiscardItem.ItemId;

        if (View != null)
        {
            // 顯示中間固定位置
            if (slot._targetImage != null)
            {
                View.ShowItemAtCenter(slot._targetImage.sprite, slot._targetImage.rectTransform.sizeDelta);
            }

            // 暫時隱藏背包中的格位，營造物品移至垃圾桶的視覺效果
            slot.gameObject.SetActive(false);
            View.ShowConfirmUI(itemName);
        }
    }

    private void HandleConfirmDiscard()
    {
        if (_pendingDiscardItem == null) return;

        if (View != null)
        {
            // 立刻隱藏確認面板，才不會擋住動畫
            if (View.ConfirmPanel != null) View.ConfirmPanel.SetActive(false);

            // 播放動畫
            View.PlayDiscardAnimation(() =>
            {
                // 動畫結束後執行移除
                bool success = DataManager.Instance.RemoveItem(_pendingDiscardItem);

                if (success)
                {
                    Debug.Log($"[TrashCan] 成功丟棄物品: {_pendingDiscardItem.ItemId}");
                    RefreshBagItems(); // 刷新顯示
                }
                else
                {
                    Debug.LogWarning($"[TrashCan] 丟棄物品失敗: {_pendingDiscardItem.ItemId}");
                }

                _pendingDiscardItem = null;
                _pendingDiscardSlot = null;
                View.HideConfirmUI();
            });
        }
    }

    private void HandleCancelDiscard()
    {
        _pendingDiscardItem = null;
        if (_pendingDiscardSlot != null)
        {
            _pendingDiscardSlot.gameObject.SetActive(true); // 恢復顯示
            _pendingDiscardSlot = null;
        }

        if (View != null)
        {
            View.HideConfirmUI();
        }
    }

    private void HandleCloseDiscardUI()
    {
        _pendingDiscardItem = null;
        if (_pendingDiscardSlot != null)
        {
            _pendingDiscardSlot.gameObject.SetActive(true); // 恢復顯示
            _pendingDiscardSlot = null;
        }

        if (View != null)
        {
            View.CloseUI();
        }
    }

    private void OnDestroy()
    {
        if (View != null)
        {
            View.OnItemDropToTrash -= HandleItemDrop;
            View.OnConfirmDiscard -= HandleConfirmDiscard;
            View.OnCancelDiscard -= HandleCancelDiscard;
            View.OnCloseDiscardUI -= HandleCloseDiscardUI;
        }
    }
}
