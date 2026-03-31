using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using GameSystem;

public class TrashCanView : MonoBehaviour
{
    [Header("丟棄 UI 組件")]
    public GameObject DiscardPanel; // 丟棄背包根物件
    public TradeSlot TradeSlotPrefab; // 背包欄位預製物件
    public Transform SlotContainer; // 生成 Slot 的父物件
    public RectTransform DropZone; // 拖放放置區域 (垃圾桶)
    public Button CloseButton; // 關閉按鈕

    [Header("二次確認 UI 組件")]
    public GameObject ConfirmPanel; // 二次確認面板
    public TextMeshProUGUI ConfirmMessageText; // 確認訊息
    public Button ConfirmButton; // 確認按鈕
    public Button CancelButton; // 取消按鈕

    [Header("進階功能組件 (暫時固定與動畫)")]
    public RectTransform CenterAnchor; // 指定中間位置
    public Image CenterItemImage; // 暫時固定的圖片預覽

    private List<TradeSlot> _activeSlots = new List<TradeSlot>();
    private Item _onSelectDiscardItem;
    private bool PanelIsVisible = false;
    // ======= Events to Presenter =======
    public event Action<TradeSlot> OnItemDropToTrash; // 當物品拖入垃圾桶
    public event Action OnConfirmDiscard; // 當點擊確認
    public event Action OnCancelDiscard; // 當點擊取消
    public event Action OnCloseDiscardUI; // 當關閉 UI

    void Start()
    {
        if (CloseButton != null) CloseButton.onClick.AddListener(() => OnCloseDiscardUI?.Invoke());
        if (ConfirmButton != null) ConfirmButton.onClick.AddListener(() => OnConfirmDiscard?.Invoke());
        if (CancelButton != null) CancelButton.onClick.AddListener(() => OnCancelDiscard?.Invoke());

        // 預設關閉
        if (DiscardPanel != null) DiscardPanel.SetActive(false);
        if (ConfirmPanel != null) ConfirmPanel.SetActive(false);
    }

    public void OpenUI()
    {
        PanelIsVisible = !PanelIsVisible;
        DiscardPanel.SetActive(PanelIsVisible);
        ConfirmPanel.SetActive(!PanelIsVisible);
    }
    public void CloseUI()
    {
        PanelIsVisible = false;
        DiscardPanel.SetActive(false);
        ConfirmPanel.SetActive(false);
        HideCenterItem();
    }

    public void ShowConfirmUI(string itemName)
    {
        if (ConfirmPanel != null)
        {
            ConfirmPanel.SetActive(true);
            if (ConfirmMessageText != null)
            {
                ConfirmMessageText.text = $"確定要丟棄 <color=red>{itemName}</color> 嗎？\n該物品將永久消失";
            }
        }
    }

    public void ShowItemAtCenter(Sprite sprite, Vector2 size)
    {
        if (CenterItemImage != null && sprite != null)
        {
            CenterItemImage.sprite = sprite;
            CenterItemImage.gameObject.SetActive(true);

            if (CenterAnchor != null)
            {
                CenterItemImage.rectTransform.anchoredPosition = CenterAnchor.anchoredPosition;
            }
            CenterItemImage.rectTransform.sizeDelta = size;

            // 重置動畫狀態
            CenterItemImage.rectTransform.localScale = Vector3.one;
            CenterItemImage.rectTransform.localRotation = Quaternion.identity;
            CenterItemImage.color = Color.white;
        }
    }

    public void PlayDiscardAnimation(Action onComplete)
    {
        if (CenterItemImage != null)
        {
            // 旋轉
            CenterItemImage.rectTransform.DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360);
            // 縮小
            CenterItemImage.rectTransform.DOScale(Vector3.zero, 0.5f);
            // 變淡
            CenterItemImage.DOFade(0, 0.5f).OnComplete(() =>
            {
                CenterItemImage.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    public void HideCenterItem()
    {
        if (CenterItemImage != null) CenterItemImage.gameObject.SetActive(false);
    }

    public void HideConfirmUI()
    {
        if (ConfirmPanel != null) ConfirmPanel.SetActive(false);
        HideCenterItem();
    }

    public void ShowBagItems(List<Item> items)
    {
        if (SlotContainer == null || TradeSlotPrefab == null) return;

        // 確保 UI 數量足夠
        AdjustSlotCount(items.Count);

        // 填入資料
        for (int i = 0; i < items.Count; i++)
        {
            _activeSlots[i].Setup(items[i], OnItemSelected);
            _activeSlots[i].gameObject.SetActive(true);
        }

        // 隱藏多餘的 Slot
        for (int i = items.Count; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].gameObject.SetActive(false);
        }
    }

    private void AdjustSlotCount(int targetCount)
    {
        while (_activeSlots.Count < targetCount)
        {
            TradeSlot newSlot = Instantiate(TradeSlotPrefab, SlotContainer);
            newSlot.OnDragEnded += OnEndDrag;
            _activeSlots.Add(newSlot);
        }
    }

    private void OnItemSelected(BagSlot slot)
    {
        if (slot != null)
        {
            _onSelectDiscardItem = slot._currentData;
        }
    }

    private void OnEndDrag(TradeSlot slot, PointerEventData eventData)
    {
        if (slot == null || slot._currentData == null) return;

        // 檢測是否在放置區域內
        if (IsPointerInsideDropZone(eventData))
        {
            OnItemDropToTrash?.Invoke(slot);
        }
    }

    private bool IsPointerInsideDropZone(PointerEventData eventData)
    {
        if (DropZone == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            DropZone,
            eventData.position,
            eventData.pressEventCamera
        );
    }
}
