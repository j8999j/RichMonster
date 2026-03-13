using System;
using UnityEngine.UI;
using UnityEngine;

public class NPCTradeSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Button InteractButton;
    public Item _currentData { get; private set; }
    public ItemDefinition _currentDefinition { get; private set; }
    private Action<NPCTradeSlot> _onClickedCallback;
    
    [Tooltip("預設圖 (載入失敗或載入中顯示)")]
    public Sprite DefaultSprite;
    
    [Tooltip("圖片長邊目標尺寸 (設為 0 則不調整)")]
    public float TargetLongEdgeSize = 100f;
    public Image _targetImage { get; private set; }
    private string _currentItemId;

    private void Awake()
    {
        if (DefaultSprite != null && _targetImage != null)
        {
            _targetImage.sprite = DefaultSprite;
        }

        if (InteractButton != null)
        {
            InteractButton.onClick.AddListener(OnClicked);
        }
        else if (TryGetComponent(out Button btn))
        {
            InteractButton = btn;
            btn.onClick.AddListener(OnClicked);
        }
    }
    
    void OnEnable()
    {
        RefreshView();
    }
    
    public void Setup(Item data, Action<NPCTradeSlot> onClick)
    {
        _currentData = data;
        _currentDefinition = DataManager.Instance.GetItemById(data.ItemId);
        _onClickedCallback = onClick;
        _currentItemId = null;
        RefreshView();
    }
    
    public void RefreshView()
    {
        if (_currentData == null) return;
        LoadSprite(_currentData.ItemId);
    }

    public void LoadSprite(string itemId)
    {
        if (_currentItemId == itemId) return;
        _currentItemId = itemId;

        if (_targetImage == null)
        {
            if (transform.childCount > 1)
            {
                _targetImage = transform.GetChild(1).GetComponent<Image>();
            }
            else
            {
                _targetImage = GetComponentInChildren<Image>();
            }
        }

        if (string.IsNullOrEmpty(itemId))
        {
            if (_targetImage != null) _targetImage.sprite = DefaultSprite;
            return;
        }

        SpriteLoader.LoadSpriteAsync(itemId, sprite =>
        {
            if (_targetImage != null && _currentItemId == itemId)
            {
                _targetImage.sprite = sprite ?? DefaultSprite;
                AdjustImageScale();
                _targetImage.enabled = true;
            }
        });
    }
    
    private void AdjustImageScale()
    {
        if (_targetImage == null || TargetLongEdgeSize <= 0) return;
        _targetImage.SetNativeSize();
        RectTransform rt = _targetImage.rectTransform;
        float width = rt.sizeDelta.x;
        float height = rt.sizeDelta.y;
        
        float longEdge = Mathf.Max(width, height);
        if (longEdge <= 0) return;
        
        float scale = TargetLongEdgeSize / longEdge;
        rt.sizeDelta = new Vector2(width * scale, height * scale);
    }

    protected void OnClicked()
    {
        _onClickedCallback?.Invoke(this);
    }
}
