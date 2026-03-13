using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class NPCMissionView : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject MissionPanel;
    public GameObject NoneItemCanTradeText;
    public TextMeshProUGUI NPCNameText;
    public TextMeshProUGUI MissionNameText;
    public TextMeshProUGUI DescriptionText;
    public TextMeshProUGUI RequirementText;
    public Image RequirementImage;
    public Image SelectedImage;
    public Sprite PropSprite;
    public Sprite FoodSprite;
    public Sprite EquipmentSprite;
    public Transform RewardsContainer;
    public GameObject RewardPrefab;
    public Button SubmitButton;
    private NpcMission _currentMission;
    private List<GameObject> _spawnedRewards = new List<GameObject>();
    
    [Header("Bag UI")]
    public Transform BagContainer;             // 顯示背包的容器
    public NPCTradeSlot NPCTradeSlotPrefab;    // 背包道具的 Prefab
    public Sprite NullSprite;
    private List<NPCTradeSlot> _spawnedBagSlots = new List<NPCTradeSlot>();
    [Header("Item Detail UI")]
    public Transform TagSlotContainer;//標籤容器
    public Image DetailIcon;//背包物品圖片
    public Image TypeIcon;//背包物品類型圖片
    public Image WorldIcon;//世界標籤圖片
    public Image RareLevelImage;//稀有度圖標
    public Sprite MonsterTagSprite;//妖界
    public TextMeshProUGUI DetailNameText;//背包物品名稱
    public TextMeshProUGUI DetailDescText;//背包物品描述
    public TextMeshProUGUI DetailPriceText;//背包物品購買成本
    public GameObject TagsPrefab; // 標籤 Prefab
    
    public Item SubmitItem { get; private set; }
    public event Action OnSubmitClick;


    private void Start()
    {
        ClearSelected();
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
        SubmitItem = null;
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

        // 4. 重置與顯示背包
        SubmitItem = null;
        ClearSelected();
        ShowBagItems();
        RefreshSubmitButton(_currentMission.Requirement == null || _currentMission.Requirement.Type == RequirementType.None);

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

    private void ShowBagItems()
    {
        foreach (var slot in _spawnedBagSlots) Destroy(slot.gameObject);
        _spawnedBagSlots.Clear();

        if (BagContainer == null || NPCTradeSlotPrefab == null) return;
        
        var inventory = DataManager.Instance.CurrentPlayerData.InventoryItems;
        int Count = 0;
        NoneItemCanTradeText.SetActive(false);
        foreach (var item in inventory)
        {
            var def = DataManager.Instance.GetItemById(item.ItemId);
            if (def == null) continue;

            // 條件：妖界物品且符合任務需求
            if (def.World == ItemWorld.Monster && (_currentMission == null || _currentMission.Requirement.Type == RequirementType.None || _currentMission.Requirement.IsMatch(def)))
            {
                NPCTradeSlot slot = Instantiate(NPCTradeSlotPrefab, BagContainer);
                slot.Setup(item, OnBagSlotClicked);
                _spawnedBagSlots.Add(slot);
                Count++;
            }
        }
        if (Count == 0)
        {
            NoneItemCanTradeText.SetActive(true);
        }
    }

    private void OnBagSlotClicked(NPCTradeSlot slot)
    {
        ClearSelected();
        SubmitItem = slot._currentData;
        SpriteLoader.LoadSpriteAsync(slot._currentData.ItemId, sprite =>
        {
                SelectedImage.sprite = sprite;
                AdjustImageScale(SelectedImage, 70);
        });

        //處理選中背包物品的邏輯
        if (DetailNameText != null) DetailNameText.text = slot._currentDefinition.Name;
        if (DetailDescText != null) DetailDescText.text = slot._currentDefinition.Description;
        if (DetailPriceText != null) DetailPriceText.text = slot._currentData.CostPrice.ToString();
        if (DetailIcon != null) DetailIcon.sprite = slot._targetImage.sprite;

        if (WorldIcon != null)
        {
            switch(slot._currentDefinition.World)
            {
                case ItemWorld.Monster:
                    WorldIcon.sprite = MonsterTagSprite;
                    break;
            }
        }

        if (TypeIcon != null)
        {
            switch(slot._currentDefinition.Type)
            {
                case ItemType.Food:
                    TypeIcon.sprite = FoodSprite;
                    break;
                case ItemType.Equipment:
                    TypeIcon.sprite = EquipmentSprite;
                    break;
                case ItemType.Prop:
                    TypeIcon.sprite = PropSprite;
                    break;
            }
        }

        // 載入對應稀有度ID的圖片
        if (RareLevelImage != null)
        {
            string rarityId = slot._currentDefinition.Rarity.ToString();
            SpriteLoader.LoadSpriteAsync(rarityId, sprite =>
            {
                if (RareLevelImage == null) return;
                RareLevelImage.sprite = sprite != null ? sprite : NullSprite;
            });
        }

        ShowTags(slot._currentDefinition.Tags);
        if (DetailIcon != null) AdjustImageScale(DetailIcon, 100);
        RefreshSubmitButton(true);
    }

    private void ClearSelected()
    {
        //清空選中背包物品的邏輯
        
        DetailNameText.text = "";
        DetailDescText.text = "";
        DetailPriceText.text = "";
        SelectedImage.sprite = NullSprite;
        DetailIcon.sprite = NullSprite;
        WorldIcon.sprite = NullSprite;
        TypeIcon.sprite = NullSprite;
        RareLevelImage.sprite = NullSprite;
        if (TagSlotContainer != null)
        {
            foreach(Transform child in TagSlotContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void ShowTags(List<string> tags)
    {
        if (TagSlotContainer == null || TagsPrefab == null || tags == null) return;

        for(int i = 0; i < tags.Count; i++)
        {
            string tagId = tags[i];
            string tagName = DataManager.Instance.GetTagNameByTag(tagId);

            if(tagName != "")
            {
                GameObject newSlot = Instantiate(TagsPrefab, TagSlotContainer);
                
                TextMeshProUGUI textComp = newSlot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                textComp.text = tagName;

                // 建立Tag圖片物件
                GameObject imgObj = new GameObject("TagImage");
                imgObj.transform.SetParent(newSlot.transform, false);
                Image tagImage = imgObj.AddComponent<Image>();
                imgObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                imgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 100);

                // 預設隱藏圖片，顯示文字
                imgObj.SetActive(false);
                textComp.gameObject.SetActive(true);

                Image capturedImage = tagImage;
                TextMeshProUGUI capturedText = textComp;
                GameObject capturedImgObj = imgObj;

                // 嘗試載入Tag圖片，成功則顯示圖片並隱藏文字，失敗則顯示文字
                SpriteLoader.LoadSpriteAsync(tagId, sprite =>
                {
                    if (capturedImgObj == null) return; // 物件已被銷毀
                    if (sprite != null)
                    {
                        capturedImage.sprite = sprite;
                        capturedImage.SetNativeSize();
                        // 等比例將寬設為175
                        RectTransform rt = capturedImage.GetComponent<RectTransform>();
                        float ratio = 175f / rt.sizeDelta.x;
                        rt.sizeDelta = new Vector2(175f, rt.sizeDelta.y * ratio);
                        capturedImgObj.SetActive(true);
                        capturedText.gameObject.SetActive(false);
                    }
                    else
                    {
                        // 圖片載入失敗，保持顯示文字
                        capturedImgObj.SetActive(false);
                        capturedText.gameObject.SetActive(true);
                    }
                });
            }
        }
    }


    public void RefreshSubmitButton(bool canSubmit)
    {
        if (SubmitButton == null || _currentMission == null) return;

        if (_currentMission.IsFinish)
        {
            SubmitButton.interactable = false;
            var btnText = SubmitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "已完成";
            return;
        }

        SubmitButton.interactable = canSubmit;
        var normalText = SubmitButton.GetComponentInChildren<TextMeshProUGUI>();
        if (normalText != null && normalText.text == "已完成") normalText.text = "提交任務";
    }

    public void SubmitMission()
    {
        if (_currentMission == null || _currentMission.IsFinish) return;
        OnSubmitClick?.Invoke();
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

