using UnityEngine;

public class NpcOnMap : MonoBehaviour
{
    public NpcMission NpcMission;
    public SpriteRenderer NpcIcon;

    /// <summary>
    /// 設定 NPC 任務與顯示圖示
    /// </summary>
    public void setNPC(NpcMission mission)
    {
        NpcMission = mission;
        
        if (mission != null && !string.IsNullOrEmpty(mission.NpcID))
        {
            // 由於 SpriteLoader 目前是依照 ID 去 ItemsAtlas 尋找
            SpriteLoader.LoadSpriteAsync(mission.NpcID, sprite =>
            {
                if (NpcIcon != null)
                {
                    if (sprite != null)
                    {
                        NpcIcon.sprite = sprite;
                        NpcIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning($"[NpcOnMap] 找不到 NPC 圖片: {mission.NpcID}");
                        NpcIcon.sprite = null;
                        NpcIcon.gameObject.SetActive(false);
                    }
                }
            });
        }
        else
        {
            if (NpcIcon != null)
            {
                NpcIcon.sprite = null;
                NpcIcon.gameObject.SetActive(false);
            }
        }
    }
}