using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 可拖曳的交易物品欄位，繼承 BagSlot 的功能
/// 拖曳時只移動圖片，背景格保持原位，放開後圖片返回原位
/// </summary>
public class TradeSlot : BagSlot, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _imageRectTransform;
    private Canvas _canvas;
    private CanvasGroup _imageCanvasGroup;
    private Vector2 _originalImagePosition;
    private Transform _originalImageParent;
    private int _originalImageSiblingIndex;
    private Image _placeholderImage;
    
    /// <summary>
    /// 拖曳結束時的回調 (可用於判斷是否放置在有效區域)
    /// </summary>
    public event Action<TradeSlot, PointerEventData> OnDragEnded;

    private void Start()
    {
        InitializeDragComponents();
    }

    private void OnDisable()
    {
        // 當 Slot 被隱藏時，清理占位
        DestroyPlaceholder();
        // 注意：不在 OnDisable 中設定 parent，避免 Unity 錯誤
        // 改為在 OnEnable 和 OnEndDrag 中處理
        ResetImageState();
    }

    private new void OnEnable()
    {
        // 確保圖片在正確位置
        EnsureImageReturned();
        InitializeDragComponents();
        // 調用基類的 RefreshView 邏輯
        RefreshView();
    }

    /// <summary>
    /// 重置圖片狀態（不改變 parent）
    /// </summary>
    private void ResetImageState()
    {
        if (_imageCanvasGroup != null)
        {
            _imageCanvasGroup.alpha = 1f;
            _imageCanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// 確保圖片返回到正確的父物件
    /// </summary>
    private void EnsureImageReturned()
    {
        if (_targetImage == null) return;
        
        // 避免在父物件啟用/停用過程中設定 parent
        if (!gameObject.activeInHierarchy) return;
        
        // 如果圖片不在 Slot 下(例如還在 Canvas 根層級)，就放回來
        if (_targetImage.transform.parent != transform)
        {
            _targetImage.transform.SetParent(transform);
            _targetImage.transform.SetSiblingIndex(1); // 根據 BagSlot 結構
            
            if (_imageRectTransform != null)
            {
                _imageRectTransform.anchoredPosition = Vector2.zero;
            }
        }
        
        ResetImageState();
    }

    private void InitializeDragComponents()
    {
        // 取得圖片的 RectTransform (繼承自 BagSlot 的 _targetImage)
        if (_targetImage != null)
        {
            _imageRectTransform = _targetImage.GetComponent<RectTransform>();
            
            // 確保圖片有 CanvasGroup
            _imageCanvasGroup = _targetImage.GetComponent<CanvasGroup>();
            if (_imageCanvasGroup == null)
            {
                _imageCanvasGroup = _targetImage.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 取得 Canvas (向上查找)
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_imageRectTransform == null) InitializeDragComponents();
        if (_imageRectTransform == null || _targetImage == null) return;
        
        // 記錄圖片的原始位置、父物件和排序索引
        _originalImagePosition = _imageRectTransform.anchoredPosition;
        _originalImageParent = _targetImage.transform.parent;
        _originalImageSiblingIndex = _targetImage.transform.GetSiblingIndex();
        
        // 創建灰階占位圖片 (在原位置)
        CreatePlaceholder();
        
        // 設定拖曳時的視覺效果
        _imageCanvasGroup.alpha = 0.9f;
        _imageCanvasGroup.blocksRaycasts = false;
        
        // 將圖片移到 Canvas 根層級，確保顯示在最上層
        _targetImage.transform.SetParent(_canvas.transform);
        _targetImage.transform.SetAsLastSibling();
        // 顯示資訊
        OnClicked();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_canvas == null || _imageRectTransform == null) return;
        
        // 圖片跟隨滑鼠移動
        _imageRectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_imageRectTransform == null) return;
        
        // 恢復視覺效果
        _imageCanvasGroup.alpha = 1f;
        _imageCanvasGroup.blocksRaycasts = true;
        
        // 觸發拖曳結束事件 (外部可判斷是否放置成功)
        OnDragEnded?.Invoke(this, eventData);
        
        // 銷毀占位並返回原始位置
        DestroyPlaceholder();
        ReturnImageToOriginalPosition();
    }

    /// <summary>
    /// 創建灰階占位圖片
    /// </summary>
    private void CreatePlaceholder()
    {
        if (_targetImage == null || _targetImage.sprite == null) return;
        
        // 在原位置創建灰階占位圖片
        var placeholderObj = new GameObject("PlaceholderImage");
        placeholderObj.transform.SetParent(_originalImageParent);
        placeholderObj.transform.SetSiblingIndex(_originalImageSiblingIndex);
        
        // 複製 RectTransform 設定
        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchoredPosition = _originalImagePosition;
        placeholderRect.sizeDelta = _imageRectTransform.sizeDelta;
        placeholderRect.anchorMin = _imageRectTransform.anchorMin;
        placeholderRect.anchorMax = _imageRectTransform.anchorMax;
        placeholderRect.pivot = _imageRectTransform.pivot;
        placeholderRect.localScale = _imageRectTransform.localScale;
        
        // 複製圖片並設為灰階
        _placeholderImage = placeholderObj.AddComponent<Image>();
        _placeholderImage.sprite = _targetImage.sprite;
        _placeholderImage.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        _placeholderImage.raycastTarget = false;
        _placeholderImage.type = _targetImage.type;
        _placeholderImage.preserveAspect = _targetImage.preserveAspect;
    }

    /// <summary>
    /// 銷毀占位圖片
    /// </summary>
    private void DestroyPlaceholder()
    {
        if (_placeholderImage != null)
        {
            Destroy(_placeholderImage.gameObject);
            _placeholderImage = null;
        }
    }

    /// <summary>
    /// 將圖片返回原始位置
    /// </summary>
    public void ReturnImageToOriginalPosition()
    {
        if (_targetImage == null || _imageRectTransform == null) return;
        
        _targetImage.transform.SetParent(_originalImageParent);
        _targetImage.transform.SetSiblingIndex(_originalImageSiblingIndex);
        _imageRectTransform.anchoredPosition = _originalImagePosition;
    }

    private void OnDestroy()
    {
        DestroyPlaceholder();
    }
}
