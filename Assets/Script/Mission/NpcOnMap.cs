using UnityEngine;
using Player;
using GameSystem;

public class NpcOnMap : MonoBehaviour, IInteractable, IMapGuideTarget
{
    public NpcMission NpcMission;
    public SpriteRenderer NpcIcon;
    public NPCMissionView missionView;
    public GameObject prompt;
    public string ID => NpcMission.MissionID;
    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID, transform);
    }
    public void LoadData()
    {
        var data = DataManager.Instance.GetPlayerSaveData<NPCMissionSave>(NpcMission.MissionID);
        if (data != null && data.LastUpdatedDay == GameManager.Instance.gameFlow.CurrentDay)
        {
            NpcMission.IsFinish = data.IsFinish;
        }
        else
        {
            NpcMission.IsFinish = false;
        }
    }
    private void SaveData()
    {
        var data = new NPCMissionSave();
        data.IsFinish = NpcMission.IsFinish;
        data.LastUpdatedDay = GameManager.Instance.gameFlow.CurrentDay;
        DataManager.Instance.SetPlayerData(NpcMission.MissionID, data);
    }
    /// <summary>
    /// 設定 NPC 任務與顯示圖示
    /// </summary>
    public void setNPC(NpcMission mission)
    {
        NpcMission = mission;
        LoadData();

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
        if (missionView != null)
        {
            missionView.OnSubmitClick -= HandleMissionSubmit;
            missionView.OnSubmitClick += HandleMissionSubmit;
            missionView.Bind(mission);
        }
    }

    public bool CheckRequirementMet()
    {
        if (NpcMission == null || NpcMission.Requirement == null || NpcMission.Requirement.Type == RequirementType.None) return true;

        var inventory = DataManager.Instance.CurrentPlayerData.InventoryItems;
        ItemWorld targetWorld = GetSubmittableItemWorld();
        foreach (var item in inventory)
        {
            var def = DataManager.Instance.GetItemById(item.ItemId);
            if (def != null && def.World == targetWorld && NpcMission.Requirement.IsMatch(def))
            {
                return true;
            }
        }
        return false;
    }

    private void HandleMissionSubmit()
    {
        if (NpcMission == null || NpcMission.IsFinish) return;

        Item targetItem = missionView != null ? missionView.SubmitItem : null;
        bool validToSubmit = false;

        if (NpcMission.Requirement == null || NpcMission.Requirement.Type == RequirementType.None)
        {
            validToSubmit = true;
        }
        else if (targetItem != null)
        {
            var def = DataManager.Instance.GetItemById(targetItem.ItemId);
            if (def != null && def.World == GetSubmittableItemWorld() && NpcMission.Requirement.IsMatch(def))
            {
                validToSubmit = true;
            }
        }

        if (validToSubmit)
        {
            // 1. 扣除物品 (如果是 RequirementType.None 則 targetItem 為 null)
            if (targetItem != null)
            {
                DataManager.Instance.RemoveItem(targetItem);
            }

            // 2. 發放獎勵
            foreach (var reward in NpcMission.Rewards)
            {
                switch (reward.RewardType)
                {
                    case RewardType.Gold:
                        if (NpcMission.MissionWorld == ItemWorld.Human)
                            DataManager.Instance.ModifyGold(reward.GoldAmount);
                        else
                            DataManager.Instance.ModifyMonsterGold(reward.GoldAmount);
                        break;
                    case RewardType.Item:
                        DataManager.Instance.AddItem(reward.ItemID, 0);
                        break;
                    case RewardType.Information:
                        DataManager.Instance.UnlockRandomMonsterInformation();
                        break;
                }
            }

            // 3. 標記完成
            NpcMission.IsFinish = true;
            Debug.Log($"[NpcOnMap] 任務 '{NpcMission.MissionName}' 已提交完成！");

            // 4. 更新 UI 狀態
            if (missionView != null)
            {
                missionView.Bind(NpcMission);
            }
            SaveData();
        }
    }
    private void OnDestroy()
    {
        if (missionView != null)
        {
            missionView.OnSubmitClick -= HandleMissionSubmit;
        }
    }

    private ItemWorld GetSubmittableItemWorld()
    {
        return NpcMission != null && NpcMission.MissionWorld == ItemWorld.Monster
            ? ItemWorld.Human
            : ItemWorld.Monster;
    }
    public void Interact()
    {
        if (GameManager.Instance.IsPlayerMoveLocked(PlayerLockSources.NpcOnMap))
        {
            HidePanel();
        }
        else
        {
            if (missionView != null)
            {
                missionView.Bind(NpcMission);
            }
            missionView.ShowPanel();
            GameManager.Instance.LockPlayerMove(PlayerLockSources.NpcOnMap);
        }
    }
    public void HidePanel()
    {
        missionView.HidePanel();
        GameManager.Instance.UnlockPlayerMove(PlayerLockSources.NpcOnMap);
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
