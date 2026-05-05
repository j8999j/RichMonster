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
    public Sprite MonsterGoldSprite;
    public Sprite InfoSprite;

    public void Setup(MissionReward reward)
    {
        Setup(reward, ItemWorld.Human);
    }

    public void Setup(MissionReward reward, ItemWorld missionWorld)
    {
        if (reward == null) return;

        switch (reward.RewardType)
        {
            case RewardType.Gold:
                if (RewardIcon != null)
                {
                    RewardIcon.sprite = missionWorld == ItemWorld.Monster && MonsterGoldSprite != null
                        ? MonsterGoldSprite
                        : GoldSprite;
                    SpriteLoader.AdjustImageScale(RewardIcon, 100);
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
                        SpriteLoader.AdjustImageScale(RewardIcon, 100);
                    });
                }
                break;
            case RewardType.Information:
                if (RewardIcon != null)
                {
                    RewardIcon.sprite = InfoSprite;
                    SpriteLoader.AdjustImageScale(RewardIcon, 100);
                }
                if (NameText != null) NameText.text = "妖怪情報";
                break;
        }
    }


}
