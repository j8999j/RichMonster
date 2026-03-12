using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Image RewardIcon;
    public TextMeshProUGUI NameText;

    [Header("Settings")]
    public Sprite GoldSprite;
    public Sprite InfoSprite;

    public void Setup(MissionReward reward)
    {
        if (reward == null) return;

        switch (reward.RewardType)
        {
            case RewardType.Gold:
                if (RewardIcon != null)
                {
                    RewardIcon.sprite = GoldSprite;
                    AdjustImageScale(RewardIcon, 100);
                }
                if (NameText != null) NameText.text = $"x{reward.GoldAmount}";
                break;
            case RewardType.Item:
                var itemDef = DataManager.Instance.GetItemById(reward.ItemID);
                string itemName = itemDef != null ? itemDef.Name : "未知物品";
                if (NameText != null) NameText.text = $"{itemName} x{reward.ItemAmount}";
                if (RewardIcon != null)
                {
                    SpriteLoader.LoadSpriteAsync(reward.ItemID, s => 
                    {
                        RewardIcon.sprite = s;
                        AdjustImageScale(RewardIcon, 100);
                    });
                }
                break;
            case RewardType.Information:
                if (RewardIcon != null)
                {
                    RewardIcon.sprite = InfoSprite;
                    AdjustImageScale(RewardIcon, 100);
                }
                if (NameText != null) NameText.text = "妖怪情報";
                break;
        }
    }

    /// <summary>
    /// 調整圖片縮放，使長邊達到目標尺寸
    /// </summary>
    private void AdjustImageScale(Image targetImage, int targetLongEdgeSize)
    {
        if (targetImage == null || targetLongEdgeSize <= 0) return;
        targetImage.SetNativeSize();
        RectTransform rt = targetImage.rectTransform;
        float width = rt.sizeDelta.x;
        float height = rt.sizeDelta.y;

        float longEdge = Mathf.Max(width, height);
        if (longEdge <= 0) return;

        float scale = targetLongEdgeSize / longEdge;
        rt.sizeDelta = new Vector2(width * scale, height * scale);
    }
}
