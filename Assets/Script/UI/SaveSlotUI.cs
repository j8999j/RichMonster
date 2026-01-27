using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 存檔欄位 UI View (MVP 模式)
/// 純粹負責顯示和發送用戶交互事件
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("存檔欄位設定")]
    [SerializeField] private int maxSaveSlots = 3;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    
    [Header("存檔圖示設定")]
    [SerializeField] private Sprite MorningSlot;
    [SerializeField] private Sprite NoonSlot;
    [SerializeField] private Sprite EveningSlot;

    [Header("Tooltip 設定")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(15f, -15f);

    // View 對外事件 (給 Presenter 訂閱)
    public event Action<int, bool> OnSlotSelected;
    public event Action OnRefreshRequested;

    // 公開屬性
    public int MaxSaveSlots => maxSaveSlots;

    // Tooltip 控制
    private bool isTooltipActive = false;
    private string currentTooltipContent = "";
    private CanvasGroup tooltipCanvasGroup;

    private void Start()
    {
        // 初始化時隱藏 Tooltip 並設定 CanvasGroup
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            
            // 取得或添加 CanvasGroup，用於防止 Tooltip 攔截滑鼠事件
            tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (tooltipCanvasGroup == null)
            {
                tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }
            // 關閉 blocksRaycasts 防止 Tooltip 遮擋滑鼠導致閃爍
            tooltipCanvasGroup.blocksRaycasts = false;
            tooltipCanvasGroup.interactable = false;
        }
    }

    private void Update()
    {
        // 如果 Tooltip 啟用，跟隨滑鼠位置
        if (isTooltipActive && tooltipPanel != null)
        {
            UpdateTooltipPosition();
        }
    }

    /// <summary>
    /// 更新 Tooltip 位置跟隨滑鼠
    /// </summary>
    private void UpdateTooltipPosition()
    {
        Vector2 mousePos = Input.mousePosition;
        tooltipPanel.transform.position = mousePos + tooltipOffset;
    }

    /// <summary>
    /// 顯示 Tooltip
    /// </summary>
    public void ShowTooltip(string content)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        
        currentTooltipContent = content;
        tooltipText.text = content;
        tooltipPanel.SetActive(true);
        isTooltipActive = true;
        UpdateTooltipPosition();
    }

    /// <summary>
    /// 隱藏 Tooltip
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
        isTooltipActive = false;
        currentTooltipContent = "";
    }

    /// <summary>
    /// 顯示存檔欄位列表 (由 Presenter 呼叫)
    /// </summary>
    public void DisplaySaveSlots(List<SaveSlotData> slotDataList)
    {
        ClearSlots();

        for (int i = 0; i < slotDataList.Count; i++)
        {
            CreateSlotUI(slotDataList[i], i);
        }
    }

    /// <summary>
    /// 清除所有存檔欄位
    /// </summary>
    private void ClearSlots()
    {
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 創建單個存檔欄位 UI
    /// </summary>
    private void CreateSlotUI(SaveSlotData slotData, int slotIndex)
    {
        if (slotPrefab == null || slotContainer == null) return;

        GameObject slotObj = Instantiate(slotPrefab, slotContainer);
        slotObj.name = $"SaveSlot_{slotIndex}";

        // 取得 UI 元件
        var slotText = slotObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        var dayText = slotObj.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        var goldText = slotObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        var button = slotObj.GetComponent<Button>();
        var slotImage = slotObj.GetComponent<Image>();

        // 設定文字
        if (dayText != null)
        {
            if (slotData.IsEmpty)
            {
                dayText.text = $"存檔 {slotIndex + 1}\n- 空白 -";
            }
            else
            {
                slotText.text = $"存檔 {slotIndex + 1}";
                goldText.text = $"{slotData.Gold}";
                if(slotData.CurrentPhase == DayPhase.HumanDay)
                {
                    dayText.text = $"Day {slotData.DaysPlayed + 1}";
                }
                else
                {
                    dayText.text = $"Night {slotData.DaysPlayed}";
                }
                // 設定滑鼠懸停事件顯示存檔日期
                string saveTimeText = $"{slotData.SaveTime:yyyy/MM/dd HH:mm}";
                SetupTooltipEvents(slotObj, saveTimeText);
            }
        }

        // 設定階段圖片
        if (slotImage != null && !slotData.IsEmpty)
        {
            Sprite phaseSprite = GetPhaseSprite(slotData.CurrentPhase);
            if (phaseSprite != null)
            {
                slotImage.sprite = phaseSprite;
            }
        }

        // 設定按鈕點擊事件 (發送事件給 Presenter)
        if (button != null)
        {
            int capturedSlot = slotIndex;
            bool capturedIsEmpty = slotData.IsEmpty;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSlotSelected?.Invoke(capturedSlot, capturedIsEmpty));
        }
    }

    /// <summary>
    /// 設定滑鼠懸停事件來顯示 Tooltip
    /// </summary>
    private void SetupTooltipEvents(GameObject slotObj, string tooltipContent)
    {
        // 添加或取得 EventTrigger 元件
        EventTrigger eventTrigger = slotObj.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = slotObj.AddComponent<EventTrigger>();
        }

        // 滑鼠進入事件 (PointerEnter)
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => ShowTooltip(tooltipContent));
        eventTrigger.triggers.Add(pointerEnterEntry);

        // 滑鼠離開事件 (PointerExit)
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => HideTooltip());
        eventTrigger.triggers.Add(pointerExitEntry);
    }

    /// <summary>
    /// 請求刷新存檔列表
    /// </summary>
    public void RequestRefresh()
    {
        OnRefreshRequested?.Invoke();
    }

    /// <summary>
    /// 取得階段對應的圖片
    /// </summary>
    private Sprite GetPhaseSprite(DayPhase phase)
    {
        return phase switch
        {
            DayPhase.HumanDay => NoonSlot,
            DayPhase.Night => EveningSlot,
            DayPhase.NightTrade => EveningSlot,
            _ => null
        };
    }
}

/// <summary>
/// 存檔欄位資料結構
/// </summary>
[System.Serializable]
public class SaveSlotData
{
    public int SlotIndex;
    public bool IsEmpty = true;
    public int DaysPlayed;
    public int Gold;
    public DayPhase CurrentPhase;
    public System.DateTime SaveTime;
}
