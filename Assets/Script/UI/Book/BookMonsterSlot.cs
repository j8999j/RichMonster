using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 妖怪圖鑑專用欄位，顯示妖怪圖片並支援點擊回調與黑色/正常切換
/// </summary>
public class BookMonsterSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Button InteractButton;
    public Image MonsterImage;
    public TextMeshProUGUI MonsterName;
    public Image NewIcon;

    [Tooltip("預設圖 (載入失敗或載入中顯示)")]
    public Sprite DefaultSprite;

    [Tooltip("圖片長邊目標尺寸 (設為 0 則不調整)")]
    public float TargetLongEdgeSize;

    public MonsterProfessionDefinition CurrentDefinition { get; private set; }
    public bool IsUnlocked { get; private set; }

    private Action<BookMonsterSlot, bool> _onClickedCallback;
    private string _currentMonsterId;

    private void Awake()
    {
        if (MonsterImage == null)
            MonsterImage = GetComponentInChildren<Image>();

        if (DefaultSprite != null && MonsterImage != null)
            MonsterImage.sprite = DefaultSprite;

        if (InteractButton != null)
            InteractButton.onClick.AddListener(OnClicked);
    }

    /// <summary>
    /// 設定欄位資料並載入圖片
    /// </summary>
    public void Setup(MonsterProfessionDefinition definition, bool isUnlocked, Action<BookMonsterSlot, bool> onClick, bool hasNewInfo = false)
    {
        CurrentDefinition = definition;
        IsUnlocked = isUnlocked;
        _onClickedCallback = onClick;
        _currentMonsterId = null; // 重置以強制重新載入圖片
        LoadSprite(definition.Id);
        if (MonsterName != null)
            MonsterName.text = isUnlocked ? definition.ProfessionName : "???";
        if (NewIcon != null)
            NewIcon.gameObject.SetActive(hasNewInfo && isUnlocked);
    }

    /// <summary>
    /// 使用 SpriteLoader 非同步載入圖片
    /// </summary>
    private void LoadSprite(string monsterId)
    {
        if (_currentMonsterId == monsterId) return;
        _currentMonsterId = monsterId;

        if (MonsterImage == null) return;

        if (string.IsNullOrEmpty(monsterId))
        {
            MonsterImage.sprite = DefaultSprite;
            return;
        }

        SpriteLoader.LoadSpriteAsync(monsterId, sprite =>
        {
            if (MonsterImage != null && _currentMonsterId == monsterId)
            {
                MonsterImage.sprite = sprite ?? DefaultSprite;
                AdjustImageScale();
                MonsterImage.enabled = true;
            }
        });
    }

    /// <summary>
    /// 設定圖片黑色效果（未解鎖）或正常顏色（已解鎖）
    /// </summary>
    public void SetBlack(bool black)
    {
        if (MonsterImage == null) return;
        MonsterImage.color = black ? Color.black : Color.white;
    }

    /// <summary>
    /// 調整圖片縮放，使長邊達到目標尺寸
    /// </summary>
    private void AdjustImageScale()
    {
        if (MonsterImage == null || TargetLongEdgeSize <= 0) return;
        MonsterImage.SetNativeSize();
        RectTransform rt = MonsterImage.rectTransform;
        float width = rt.sizeDelta.x;
        float height = rt.sizeDelta.y;

        float longEdge = Mathf.Max(width, height);
        if (longEdge <= 0) return;

        float scale = TargetLongEdgeSize / longEdge;
        rt.sizeDelta = new Vector2(width * scale, height * scale);
    }

    private void OnClicked()
    {
        _onClickedCallback?.Invoke(this, IsUnlocked);
    }
}
