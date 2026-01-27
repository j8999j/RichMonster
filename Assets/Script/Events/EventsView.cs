using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameSystem;

public class EventsView : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject NewsPanel;
    public GameObject MoreDetailPanel;
    public Image EventsImage;
    public TextMeshProUGUI DetailTitleText; // 顯示詳細資訊的標題
    public TextMeshProUGUI DetailContentText; // 顯示詳細資訊的內容

    [Header("Buttons")]
    // 順序：0=Main, 1=Btn1, 2=Btn2...
    public List<Button> AllNewsButtons;
    public List<TextMeshProUGUI> AllNewsTitle;
    public List<TextMeshProUGUI> AllNewsDetail;
    public List<Image> AllNewsImage;
    public Button OpenNewsPanelButton; // 開啟新聞面板按鈕
    public Button ExitNewsButton;
    public Button MoreDetailExitButton;
    [Header("Data")]
    public List<MonsterEvent> TodayMonsterEvents;
    
    // 事件生成器
    private EventsGenerator _eventsGenerator;
    private List<GameEventDefinition> _todayEvents = new List<GameEventDefinition>();
    
    public IReadOnlyList<GameEventDefinition> TodayEvents => _todayEvents;
    
    private void Awake()
    {
        InitializeGenerator();
        GenerateTodayEvents(GameManager.Instance.gameFlow.CurrentDay);
    }
    
    private void Start()
    {
        // 綁定開啟/關閉面板按鈕
        if (OpenNewsPanelButton != null)
            OpenNewsPanelButton.onClick.AddListener(OpenNewsPanel);
        if (ExitNewsButton != null)
            ExitNewsButton.onClick.AddListener(CloseNewsPanel);
        if (MoreDetailExitButton != null)
            MoreDetailExitButton.onClick.AddListener(CloseMoreDetailPanel);
            
        // 初始化時隱藏面板
        if (NewsPanel != null)
            NewsPanel.SetActive(false);
        if (MoreDetailPanel != null)
            MoreDetailPanel.SetActive(false);
    }
    
    /// <summary>
    /// 開啟新聞面板並更新內容
    /// </summary>
    public void OpenNewsPanel()
    {
        if (NewsPanel != null)
        {
            NewsPanel.SetActive(true);
            SetButtonEventsFromGameEvents();
        }
    }
    
    /// <summary>
    /// 關閉新聞面板
    /// </summary>
    public void CloseNewsPanel()
    {
        if (NewsPanel != null)
            NewsPanel.SetActive(false);
        if (MoreDetailPanel != null)
            MoreDetailPanel.SetActive(false);
    }
    
    /// <summary>
    /// 初始化事件生成器
    /// </summary>
    public void InitializeGenerator()
    {
        var dataManager = DataManager.Instance;
        if (dataManager == null || !dataManager.IsInitialized)
        {
            Debug.LogWarning("[EventsView] DataManager 尚未初始化");
            return;
        }
        
        _eventsGenerator = new EventsGenerator(
            dataManager.EventDict.ToDictionary(kv => kv.Key, kv => kv.Value)
        );
    }
    
    /// <summary>
    /// 生成當日事件
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    public void GenerateTodayEvents(int dayNumber)
    {
        if (_eventsGenerator == null)
        {
            InitializeGenerator();
        }
        
        if (_eventsGenerator == null)
        {
            Debug.LogWarning("[EventsView] 事件生成器未初始化");
            return;
        }
        
        _todayEvents = _eventsGenerator.GenerateEventsForDay(dayNumber);
        
        Debug.Log($"[EventsView] Day {dayNumber} 生成完成: 事件數量={_todayEvents.Count}");
    }
    
    /// <summary>
    /// 取得今日事件列表
    /// </summary>
    public List<GameEventDefinition> GetTodayEvents()
    {
        return _todayEvents;
    }

    /// <summary>
    /// 核心邏輯：將資料綁定到按鈕
    /// </summary>
    public void SetButtonEvents()
    {
        // 1. 防呆：確保資料跟按鈕都有東西
        if (TodayMonsterEvents == null || AllNewsButtons == null) return;

        // 2. 迴圈遍歷所有按鈕
        for (int i = 0; i < AllNewsButtons.Count; i++)
        {
            Button btn = AllNewsButtons[i];
            
            // 3. 重要習慣：先移除舊的監聽，避免重複綁定導致點一次跑兩次
            btn.onClick.RemoveAllListeners();

            // 4. 檢查資料是否足夠 (例如按鈕有5個，但事件只有3個)
            if (i < TodayMonsterEvents.Count)
            {
                btn.gameObject.SetActive(true);
                MonsterEvent currentEvent = TodayMonsterEvents[i];
                // 5. 綁定點擊事件
                btn.onClick.AddListener(() => 
                {
                    OnNewsClicked(currentEvent);
                });
            }
            else
            {
                // 沒資料：隱藏多餘的按鈕
                btn.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 使用 GameEventDefinition 設定按鈕事件
    /// 根據稀有度排序（最高排頭條），相同稀有度保持原抽取順序
    /// </summary>
    public void SetButtonEventsFromGameEvents()
    {
        if (_todayEvents == null || AllNewsButtons == null) return;

        // 根據稀有度降序排序，使用穩定排序保持相同稀有度的原始順序
        var sortedEvents = _todayEvents
            .Select((eventDef, index) => new { Event = eventDef, OriginalIndex = index })
            .OrderByDescending(x => x.Event.eventRareity)
            .ThenBy(x => x.OriginalIndex)
            .Select(x => x.Event)
            .ToList();

        for (int i = 0; i < AllNewsButtons.Count; i++)
        {
            Button btn = AllNewsButtons[i];
            btn.onClick.RemoveAllListeners();

            if (i < sortedEvents.Count)
            {
                btn.gameObject.SetActive(true);
                GameEventDefinition currentEvent = sortedEvents[i];
                
                // 更新按鈕上的文字
                if (AllNewsTitle != null && i < AllNewsTitle.Count)
                {
                    AllNewsTitle[i].text = currentEvent.Name;
                }
                if (AllNewsDetail != null && i < AllNewsDetail.Count)
                {
                    string description = currentEvent.EventDescription;
                    if (description != null && description.Length > 15)
                    {
                        AllNewsDetail[i].text = description.Substring(0, 15) + "......";
                    }
                    else
                    {
                        AllNewsDetail[i].text = description ?? "";
                    }
                }
                
                btn.onClick.AddListener(() => 
                {
                    OnGameEventClicked(currentEvent);
                });
            }
            else
            {
                // 多餘的按鈕清空並隱藏
                if (AllNewsTitle != null && i < AllNewsTitle.Count)
                {
                    AllNewsTitle[i].text = "";
                }
                if (AllNewsDetail != null && i < AllNewsDetail.Count)
                {
                    AllNewsDetail[i].text = "";
                }
                btn.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 當 MonsterEvent 按鈕被點擊時觸發
    /// </summary>
    private void OnNewsClicked(MonsterEvent monsterEvent)
    {
        Debug.Log($"玩家點擊了事件：{monsterEvent.EventName}");

        // 1. 顯示詳細面板
        if (MoreDetailPanel != null)
        {
            MoreDetailPanel.SetActive(true);
            
            // 更新詳細面板的 UI
            if (DetailTitleText != null) DetailTitleText.text = monsterEvent.EventName;
            if (DetailContentText != null) DetailContentText.text = monsterEvent.EventDescription;
        }
    }
    
    /// <summary>
    /// 當 GameEventDefinition 按鈕被點擊時觸發
    /// </summary>
    private void OnGameEventClicked(GameEventDefinition gameEvent)
    {
        Debug.Log($"玩家點擊了事件：{gameEvent.Name}");

        if (MoreDetailPanel != null)
        {
            MoreDetailPanel.SetActive(true);
            
            if (DetailTitleText != null) DetailTitleText.text = gameEvent.Name;
            if (DetailContentText != null) DetailContentText.text = gameEvent.EventDescription;
        }
    }
    public void CloseMoreDetailPanel()
    {
        if (MoreDetailPanel != null)
            MoreDetailPanel.SetActive(false);
    }
}