using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameSystem;

/// <summary>
/// 通知取得物品的類型
/// </summary>
public enum NoticeGetItemType
{
    MonsterGold,  // 妖怪幣
    Gold,         // 金幣
    Item,         // 一般物品
    Others        // 其他
}

/// <summary>
/// 通知取得物品的資料結構
/// </summary>
[System.Serializable]
public struct NoticeItemEntry
{

    public NoticeGetItemType Type;
    public string ItemId;
    public int Amount;
    public string DisplayName;  // Others 類型用
    public Sprite CustomSprite; // Others 類型用

    public NoticeItemEntry(NoticeGetItemType type, string itemId, int amount, string displayName = "", Sprite customSprite = null)
    {
        Type = type;
        ItemId = itemId;
        Amount = amount;
        DisplayName = displayName;
        CustomSprite = customSprite;
    }

    /// <summary>
    /// 快速建立妖怪幣類型
    /// </summary>
    public static NoticeItemEntry MonsterGold(int amount)
    {
        return new NoticeItemEntry(NoticeGetItemType.MonsterGold, "", amount);
    }

    /// <summary>
    /// 快速建立金幣類型
    /// </summary>
    public static NoticeItemEntry Gold(int amount)
    {
        return new NoticeItemEntry(NoticeGetItemType.Gold, "", amount);
    }

    /// <summary>
    /// 快速建立物品類型
    /// </summary>
    public static NoticeItemEntry ItemEntry(string itemId, int amount = 1)
    {
        return new NoticeItemEntry(NoticeGetItemType.Item, itemId, amount);
    }

    /// <summary>
    /// 快速建立其他類型
    /// </summary>
    public static NoticeItemEntry Other(string displayName, Sprite sprite, int amount = 0)
    {
        return new NoticeItemEntry(NoticeGetItemType.Others, "", amount, displayName, sprite);
    }
}

public class NoticeGetItem : MonoBehaviour
{
    [Header("通知設定")]
    [SerializeField] private GameObject NoticePanel;
    [SerializeField] private TextMeshProUGUI NoticeText;
    [SerializeField] private Button ConfirmButton;
    private const string LockSource = PlayerLockSources.NoticeGetItem;
    private bool _moveLocked;
    [Header("Slot 設定")]
    [SerializeField] private NoticeSlot SlotPrefab;
    [SerializeField] private Transform SlotContainer;
    [SerializeField] private Sprite MonsterGoldSprite;
    [SerializeField] private Sprite GoldSprite;

    [Header("Tooltip 設定")]
    [SerializeField] private GameObject TooltipPanel;
    [SerializeField] private TextMeshProUGUI TooltipText;
    [SerializeField] private Vector2 TooltipOffset = new Vector2(15f, -15f);

    [Header("SFX")]
    [SerializeField] private AudioClip clickSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolumeScale = 1f;

    private bool _isTooltipActive = false;
    private List<NoticeSlot> _activeSlots = new List<NoticeSlot>();

    private void OnEnable()
    {
        NoticeGetItemEvents.OnShowNotice += Show;
        NoticeGetItemEvents.OnClearNotice += Clear;
        if (ConfirmButton != null) ConfirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnDisable()
    {
        if (ConfirmButton != null) ConfirmButton.onClick.RemoveListener(OnConfirmClicked);
    }

    private void OnDestroy()
    {
        NoticeGetItemEvents.OnShowNotice -= Show;
        NoticeGetItemEvents.OnClearNotice -= Clear;
    }

    private void Update()
    {
        if (_isTooltipActive && TooltipPanel != null)
        {
            Vector2 mousePos = Input.mousePosition;
            TooltipPanel.transform.position = mousePos + TooltipOffset;
        }
    }

    /// <summary>
    /// 顯示取得物品的通知，根據 NoticeGetItemType 設定顯示邏輯
    /// </summary>
    /// <param name="source">獎勵來源說明</param>
    /// <param name="items">物品清單</param>
    public void Show(string source, List<NoticeItemEntry> items)
    {
        if (SlotPrefab == null || SlotContainer == null) return;
        if (items == null || items.Count == 0) return;

        Clear();

        // 顯示獎勵來源說明
        if (NoticePanel != null) NoticePanel.SetActive(true);
        if (NoticeText != null) NoticeText.text = source;

        if (!_moveLocked && GameManager.Instance != null)
        {
            GameManager.Instance.LockPlayerMove(LockSource);
            _moveLocked = true;
        }

        // 合併相同類型+相同識別的項目
        var merged = MergeEntries(items);

        foreach (var entry in merged)
        {
            NoticeSlot slot = Instantiate(SlotPrefab, SlotContainer);
            _activeSlots.Add(slot);

            switch (entry.Type)
            {
                case NoticeGetItemType.MonsterGold:
                    slot.SetupMonsterGold(entry.Amount, MonsterGoldSprite, ShowTooltip, HideTooltip);
                    break;

                case NoticeGetItemType.Gold:
                    slot.SetupGold(entry.Amount, GoldSprite, ShowTooltip, HideTooltip);
                    break;

                case NoticeGetItemType.Item:
                    slot.SetupItem(entry.ItemId, entry.Amount, ShowTooltip, HideTooltip);
                    break;

                case NoticeGetItemType.Others:
                    slot.SetupOthers(entry.DisplayName, entry.CustomSprite, entry.Amount, ShowTooltip, HideTooltip);
                    break;
            }
        }
    }

    /// <summary>
    /// 將相同類型且相同識別的項目合併為一筆，數量累加
    /// MonsterGold / Gold 各自合併為一筆；Item 以 ItemId 為 key；Others 以 DisplayName 為 key
    /// </summary>
    private List<NoticeItemEntry> MergeEntries(List<NoticeItemEntry> items)
    {
        var dict = new Dictionary<string, NoticeItemEntry>();

        foreach (var entry in items)
        {
            string key;
            switch (entry.Type)
            {
                case NoticeGetItemType.MonsterGold:
                    key = "__MonsterGold__";
                    break;
                case NoticeGetItemType.Gold:
                    key = "__Gold__";
                    break;
                case NoticeGetItemType.Item:
                    key = $"Item_{entry.ItemId}";
                    break;
                case NoticeGetItemType.Others:
                    key = $"Others_{entry.DisplayName}";
                    break;
                default:
                    key = entry.GetHashCode().ToString();
                    break;
            }

            if (dict.TryGetValue(key, out var existing))
            {
                var updated = existing;
                updated.Amount += entry.Amount;
                dict[key] = updated;
            }
            else
            {
                dict[key] = entry;
            }
        }

        return new List<NoticeItemEntry>(dict.Values);
    }

    /// <summary>
    /// 清除所有已顯示的 Slot
    /// </summary>
    public void Clear()
    {
        foreach (var slot in _activeSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        _activeSlots.Clear();
        HideTooltip();
    }

    private void OnConfirmClicked()
    {
        PlaySfx(clickSfx);
        if (NoticePanel != null) NoticePanel.SetActive(false);
        Clear();
        if (_moveLocked && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockPlayerMove(LockSource);
            _moveLocked = false;
        }
    }

    // ── Tooltip ──────────────────────────────────────
    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
    }

    public void ShowTooltip(string content)
    {
        if (TooltipPanel != null && TooltipText != null)
        {
            TooltipText.text = content;
            TooltipPanel.SetActive(true);
            _isTooltipActive = true;
            TooltipPanel.transform.position = (Vector2)Input.mousePosition + TooltipOffset;
        }
    }

    public void HideTooltip()
    {
        if (TooltipPanel != null) TooltipPanel.SetActive(false);
        _isTooltipActive = false;
    }
}
