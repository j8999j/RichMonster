using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Souvenir;

public class SouvenirBagItemData
{
    public string SouvenirID;
    public string SouvenirName;
    public string SouvenirDescription;
    public string FunctionOrConditionDesc;
    public bool IsSpecial;
}

public class SouvenirBagSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Button InteractButton;
    public Image SouvenirIcon;
    public GameObject SpecialMarker; // 標示是否為特殊紀念品
    
    [Header("Settings")]
    public float TargetLongEdgeSize = 100f;
    public Sprite DefaultSprite;

    public SouvenirBagItemData CurrentData { get; private set; }

    private Action<SouvenirBagSlot> _onClickedCallback;
    private string _currentSouvenirId;

    private void Awake()
    {
        if (InteractButton != null)
        {
            InteractButton.onClick.AddListener(OnClicked);
        }
    }

    public void Setup(SouvenirBagItemData data, Action<SouvenirBagSlot> onClick)
    {
        CurrentData = data;
        _onClickedCallback = onClick;
        _currentSouvenirId = null;

        RefreshView();
    }

    public void RefreshView()
    {
        if (CurrentData == null) return;

        if (SpecialMarker != null)
        {
            SpecialMarker.SetActive(CurrentData.IsSpecial);
        }

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
