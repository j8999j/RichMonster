using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Souvenir;

public class SouvenirSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Button InteractButton;
    public Image SouvenirIcon;
    public TextMeshProUGUI PriceText;
    public GameObject ExchangedMarker; // 已兌換圖章
    public GameObject PriceTagObj; // 價格標籤物件 (尚未兌換才顯示)
    
    [Header("Settings")]
    public float TargetLongEdgeSize = 100f;
    public Sprite DefaultSprite;

    public AchievementSouvenirData CurrentData { get; private set; }
    public bool IsOwned { get; private set; }

    private Action<SouvenirSlot> _onClickedCallback;
    private string _currentSouvenirId;

    private void Awake()
    {
        if (InteractButton != null)
        {
            InteractButton.onClick.AddListener(OnClicked);
        }
    }

    public void Setup(AchievementSouvenirData data, bool isOwned, Action<SouvenirSlot> onClick)
    {
        CurrentData = data;
        IsOwned = isOwned;
        _onClickedCallback = onClick;
        _currentSouvenirId = null;

        RefreshView();
    }

    public void RefreshView()
    {
        if (CurrentData == null) return;

        // 價格與標記
        if (PriceText != null) PriceText.text = CurrentData.PointsFee.ToString();
        if (PriceTagObj != null) PriceTagObj.SetActive(!IsOwned);
        if (ExchangedMarker != null) ExchangedMarker.SetActive(IsOwned);

        // 如果兌換了可以讓圖片變暗
        if (SouvenirIcon != null && IsOwned)
        {
            SouvenirIcon.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        else if (SouvenirIcon != null)
        {
            SouvenirIcon.color = Color.white;
        }

        // 載入圖片
        LoadSprite(CurrentData.SouvenirID);
    }

    private void LoadSprite(string souvenirId)
    {
        if (_currentSouvenirId == souvenirId) return;
        _currentSouvenirId = souvenirId;

        if (SouvenirIcon == null) return;

        if (string.IsNullOrEmpty(souvenirId))
        {
            SouvenirIcon.sprite = DefaultSprite;
            return;
        }

        SpriteLoader.LoadSpriteAsync(souvenirId, sprite =>
        {
            if (SouvenirIcon != null && _currentSouvenirId == souvenirId)
            {
                SouvenirIcon.sprite = sprite ?? DefaultSprite;
                SpriteLoader.AdjustImageScale(SouvenirIcon, TargetLongEdgeSize);
                SouvenirIcon.enabled = true;
            }
        });
    }

    private void OnClicked()
    {
        _onClickedCallback?.Invoke(this);
    }
}
