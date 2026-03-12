using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class NPCMissionView : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject MissionPanel;
    public TextMeshProUGUI NPCNameText;
    public TextMeshProUGUI MissionNameText;
    public TextMeshProUGUI DescriptionText;
    public TextMeshProUGUI RequirementText;
    public Image RequirementImage;
    public Sprite PropSprite;
    public Sprite FoodSprite;
    public Sprite EquipmentSprite;
    public Transform RewardsContainer;
    public GameObject RewardPrefab;
    public Button SubmitButton;
    private NpcMission _currentMission;
    private List<GameObject> _spawnedRewards = new List<GameObject>();

    private void Start()
    {
        if (SubmitButton != null)
            SubmitButton.onClick.AddListener(SubmitMission);
    }

    public void ShowPanel()
    {
        MissionPanel.SetActive(true);
    }
    public void HidePanel()
    {
        MissionPanel.SetActive(false);
    }
    /// <summary>
    /// 綁定任務資料並更新 UI
    /// </summary>
    public void Bind(NpcMission mission)
    {
        _currentMission = mission;
        if (mission == null) return;

        // 1. 基本資訊
        if (NPCNameText != null)
        {
            var npcData = DataManager.Instance.NPCDataDict.Values.FirstOrDefault(n => n.NpcID == mission.NpcID);
            NPCNameText.text = npcData != null ? npcData.NpcName : "神秘人";
        }

        if (MissionNameText != null) MissionNameText.text = mission.MissionName;
        if (DescriptionText != null) DescriptionText.text = mission.MissionDescription;

        // 2. 需求顯示
        UpdateRequirementUI();

        // 3. 獎勵列表
        UpdateRewardsUI();

        // 4. 檢查是否可提交
        RefreshSubmitButton();

        gameObject.SetActive(true);
    }

    private void UpdateRequirementUI()
    {
        if (RequirementText == null || _currentMission.Requirement == null) return;

        string reqStr = "任務需求：";
        var req = _currentMission.Requirement;

        if (RequirementImage != null)
        {
            RequirementImage.gameObject.SetActive(req.Type != RequirementType.None);
        }

        switch (req.Type)
        {
            case RequirementType.SpecificItem:
                var itemDef = DataManager.Instance.GetItemById(req.TargetItemID);
                reqStr += itemDef != null ? itemDef.Name : "未知物品";
                if (RequirementImage != null)
                {
                    SpriteLoader.LoadSpriteAsync(req.TargetItemID, s => 
                    {
                        RequirementImage.sprite = s;
                        AdjustImageScale(RequirementImage, 100);
                    });
                }
                break;
            case RequirementType.SpecificTag:
                reqStr += $"任意 [{DataManager.Instance.GetTagNameByTag(req.TargetTag)}] 物品";
                if (RequirementImage != null)
                {
                    SpriteLoader.LoadSpriteAsync(req.TargetTag, s => 
                    {
                        RequirementImage.sprite = s;
                        AdjustImageScale(RequirementImage, 100);
                    });
                }
                break;
            case RequirementType.SpecificType:
                reqStr += $"任意 {req.TargetType} 類型物品";
                if (RequirementImage != null)
                {
                    switch (req.TargetType)
                    {
                        case ItemType.Prop:
                            RequirementImage.sprite = PropSprite;
                            break;
                        case ItemType.Food:
                            RequirementImage.sprite = FoodSprite;
                            break;
                        case ItemType.Equipment:
                            RequirementImage.sprite = EquipmentSprite;
                            break;
                    }
                    AdjustImageScale(RequirementImage, 100);
                }
                break;
            case RequirementType.None:
                reqStr += "無";
                break;
        }

        RequirementText.text = reqStr;
    }

    private void UpdateRewardsUI()
    {
        // 清除舊獎勵
        foreach (var obj in _spawnedRewards) Destroy(obj);
        _spawnedRewards.Clear();

        if (RewardsContainer == null || RewardPrefab == null || _currentMission.Rewards == null) return;

        foreach (var reward in _currentMission.Rewards)
        {
            GameObject go = Instantiate(RewardPrefab, RewardsContainer);
            _spawnedRewards.Add(go);

            var slot = go.GetComponent<RewardSlot>();
            if (slot != null)
            {
                slot.Setup(reward);
            }
        }
    }

    private void RefreshSubmitButton()
    {
        if (SubmitButton == null) return;

        if (_currentMission.IsFinish)
        {
            SubmitButton.interactable = false;
            var btnText = SubmitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "已完成";
            return;
        }

        bool canSubmit = CheckRequirementMet(out _);
        SubmitButton.interactable = canSubmit;
    }

    private bool CheckRequirementMet(out Item targetItem)
    {
        targetItem = null;
        if (_currentMission.Requirement == null || _currentMission.Requirement.Type == RequirementType.None) return true;

        var inventory = DataManager.Instance.CurrentPlayerData.InventoryItems;
        foreach (var item in inventory)
        {
            var def = DataManager.Instance.GetItemById(item.ItemId);
            if (_currentMission.Requirement.IsMatch(def))
            {
                targetItem = item;
                return true;
            }
        }

        return false;
    }

    public void SubmitMission()
    {
        if (_currentMission == null || _currentMission.IsFinish) return;

        if (CheckRequirementMet(out Item targetItem))
        {
            // 1. 扣除物品 (如果是 RequirementType.None 則 targetItem 為 null)
            if (targetItem != null)
            {
                DataManager.Instance.RemoveItem(targetItem);
            }

            // 2. 發放獎勵
            foreach (var reward in _currentMission.Rewards)
            {
                switch (reward.RewardType)
                {
                    case RewardType.Gold:
                        if (_currentMission.MissionWorld == ItemWorld.Human)
                            DataManager.Instance.ModifyGold(reward.GoldAmount);
                        else
                            DataManager.Instance.ModifyMonsterGold(reward.GoldAmount);
                        break;
                    case RewardType.Item:
                        DataManager.Instance.AddItem(reward.ItemID, 0); // 獎勵物品成本設為 0
                        break;
                    case RewardType.Information:
                        // TODO: 處理情報解鎖邏輯，例如 DataManager.Instance.UnlockMonsterInformation(...)
                        Debug.Log("[NPCMissionView] 獲得情報獎勵，需視具體任務設定解鎖內容");
                        break;
                }
            }
            // 3. 標記完成
            _currentMission.IsFinish = true;
            Debug.Log($"[NPCMissionView] 任務 '{_currentMission.MissionName}' 已提交完成！");
            // 4. 關閉或刷新
            RefreshSubmitButton();
        }
    }

    public void CloseView()
    {
        gameObject.SetActive(false);
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

