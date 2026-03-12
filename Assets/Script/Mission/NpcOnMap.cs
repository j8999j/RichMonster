using UnityEngine;
using Player;

public class NpcOnMap : MonoBehaviour, IInteractable
{
    public NpcMission NpcMission;
    public SpriteRenderer NpcIcon;
    public NPCMissionView missionView;
    public GameObject prompt;

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
        missionView.Bind(mission);
    }

    public void Interact()
    {
        missionView.ShowPanel();
    }
    public void ShowPrompt()
    {
        prompt.SetActive(true);
    }
    public void HidePrompt()
    {
        prompt.SetActive(false);
    }
}