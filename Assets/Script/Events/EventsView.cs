using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameSystem;

public class EventsView : MonoBehaviour, IPlayerInfoPage
{
    private const string MonsterSceneAlias = "MonsterScene";
    private const float AutoOpenDelaySeconds = 1f;

    [Header("UI Components")]
    public GameObject NewsPanel;
    public GameObject MoreDetailPanel;
    public Image EventsImage;
    public Sprite NullSprite;
    public TextMeshProUGUI DetailTitleText; // 詳細面板標題文字
    public TextMeshProUGUI DetailContentText; // 詳細面板內容文字

    [Header("Buttons")]
    // 索引 0 通常為主新聞，其餘為列表按鈕。
    public List<Button> AllNewsButtons;
    public List<TextMeshProUGUI> AllNewsTitle;
    public List<TextMeshProUGUI> AllNewsDetail;
    public List<Image> AllNewsImage;
    public Button OpenNewsPanelButton; // 開啟新聞面板按鈕
    public Button ExitNewsButton;
    public Button MoreDetailExitButton;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip openPanelSfx;
    [SerializeField] private AudioClip closePanelSfx;
    [SerializeField] private AudioClip openDetailSfx;
    [SerializeField] private AudioClip closeDetailSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolumeScale = 1f;

    [Header("Data")]
    public List<MonsterEvent> TodayMonsterEvents;

    // 事件生成資料。
    private EventsGenerator _eventsGenerator;
    private List<GameEventDefinition> _todayEvents = new List<GameEventDefinition>();
    private SceneTransitionManager _boundSceneTransitionManager;
    private Coroutine _autoOpenRoutine;
    private string _autoOpenSceneName;

    public IReadOnlyList<GameEventDefinition> TodayEvents => _todayEvents;

    private void Awake()
    {
        InitializeGenerator();
        GenerateTodayEvents(GameManager.Instance.gameFlow.CurrentDay);
    }

    private void OnEnable()
    {
        BindSceneTransitionManager();
    }

    private void OnDisable()
    {
        UnbindSceneTransitionManager();
        StopAutoOpenRoutine();
    }

    private void Start()
    {
        BindSceneTransitionManager();

        // 綁定開啟、關閉與詳細面板按鈕。
        if (OpenNewsPanelButton != null)
            OpenNewsPanelButton.onClick.AddListener(OpenNewsPanel);
        if (ExitNewsButton != null)
            ExitNewsButton.onClick.AddListener(CloseNewsPanel);
        if (MoreDetailExitButton != null)
            MoreDetailExitButton.onClick.AddListener(CloseMoreDetailPanel);

        // 初始化面板狀態。
        if (NewsPanel != null)
            NewsPanel.SetActive(false);
        if (MoreDetailPanel != null)
            MoreDetailPanel.SetActive(false);

        if (_boundSceneTransitionManager != null
            && IsMonsterScene(_boundSceneTransitionManager.CurrentScene))
        {
            ScheduleAutoOpenNews(_boundSceneTransitionManager.CurrentScene);
        }
    }

    private void BindSceneTransitionManager()
    {
        SceneTransitionManager sceneTransitionManager = SceneTransitionManager.Instance;
        if (sceneTransitionManager == null || _boundSceneTransitionManager == sceneTransitionManager)
            return;

        UnbindSceneTransitionManager();
        _boundSceneTransitionManager = sceneTransitionManager;
        _boundSceneTransitionManager.OnSceneLoadStart += OnSceneLoadStart;
        _boundSceneTransitionManager.OnSceneLoadComplete += OnSceneLoadComplete;
    }

    private void UnbindSceneTransitionManager()
    {
        if (_boundSceneTransitionManager == null)
            return;

        _boundSceneTransitionManager.OnSceneLoadStart -= OnSceneLoadStart;
        _boundSceneTransitionManager.OnSceneLoadComplete -= OnSceneLoadComplete;
        _boundSceneTransitionManager = null;
    }

    private void OnSceneLoadStart(string sceneName)
    {
        if (IsMonsterScene(sceneName))
        {
            return;
        }

        StopAutoOpenRoutine();
    }

    private void OnSceneLoadComplete(string sceneName)
    {
        if (IsMonsterScene(sceneName))
        {
            ScheduleAutoOpenNews(sceneName);
            return;
        }

        StopAutoOpenRoutine();
        if ((NewsPanel != null && NewsPanel.activeSelf)
            || (MoreDetailPanel != null && MoreDetailPanel.activeSelf))
        {
            PlayerInfoUIEvents.InvokeCloseAll();
        }
        else
        {
            CloseNewsPanel(false);
        }
    }

    private void ScheduleAutoOpenNews(string sceneName)
    {
        StopAutoOpenRoutine();
        _autoOpenSceneName = sceneName;
        _autoOpenRoutine = StartCoroutine(AutoOpenNewsRoutine());
    }

    private IEnumerator AutoOpenNewsRoutine()
    {
        yield return new WaitForSeconds(AutoOpenDelaySeconds);
        _autoOpenRoutine = null;
        string targetScene = _autoOpenSceneName;
        _autoOpenSceneName = null;

        if (_boundSceneTransitionManager == null
            || !IsMonsterScene(targetScene)
            || _boundSceneTransitionManager.CurrentScene != targetScene)
        {
            yield break;
        }

        PlayerInfoUIEvents.InvokeOpenNews();
    }

    private void StopAutoOpenRoutine()
    {
        if (_autoOpenRoutine == null)
            return;

        StopCoroutine(_autoOpenRoutine);
        _autoOpenRoutine = null;
        _autoOpenSceneName = null;
    }

    private bool IsMonsterScene(string sceneName)
    {
        return sceneName == SceneTransitionManager.SCENE_MONSTER || sceneName == MonsterSceneAlias;
    }

    /// <summary>
    /// 開啟新聞面板並載入今天事件。
    /// </summary>
    public void OpenPage() => OpenNewsPanel();

    public void OpenNewsPanel()
    {
        if (NewsPanel != null)
        {
            bool wasActive = NewsPanel.activeSelf;
            NewsPanel.SetActive(true);
            SetButtonEventsFromGameEvents();
            if (!wasActive)
            {
                PlaySfx(openPanelSfx);
            }
        }
    }

    /// <summary>
    /// 關閉新聞面板。
    /// </summary>
    public void ClosePage() => CloseNewsPanel();

    public void CloseNewsPanel()
    {
        CloseNewsPanel(true);
    }

    private void CloseNewsPanel(bool playSound)
    {
        bool wasNewsPanelActive = NewsPanel != null && NewsPanel.activeSelf;
        if (NewsPanel != null)
            NewsPanel.SetActive(false);
        if (MoreDetailPanel != null)
            MoreDetailPanel.SetActive(false);
        if (playSound && wasNewsPanelActive)
        {
            PlaySfx(closePanelSfx);
        }
    }

    /// <summary>
    /// 初始化事件生成器。
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
    /// 生成指定天數的事件。
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
            Debug.LogWarning("[EventsView] 事件生成器尚未初始化");
            return;
        }

        _todayEvents = _eventsGenerator.GenerateEventsForDay(dayNumber);

        Debug.Log($"[EventsView] Day {dayNumber} 生成完成: 事件數量={_todayEvents.Count}");
    }

    /// <summary>
    /// 取得今天生成的事件。
    /// </summary>
    public List<GameEventDefinition> GetTodayEvents()
    {
        return _todayEvents;
    }

    /// <summary>
    /// 使用舊版 MonsterEvent 資料設定新聞按鈕。
    /// </summary>
    public void SetButtonEvents()
    {
        // 1. 沒有舊版 MonsterEvent 資料或按鈕時直接返回。
        if (TodayMonsterEvents == null || AllNewsButtons == null) return;

        // 2. 依序綁定新聞按鈕。
        for (int i = 0; i < AllNewsButtons.Count; i++)
        {
            Button btn = AllNewsButtons[i];

            // 3. 先清除舊監聽，避免重複觸發。
            btn.onClick.RemoveAllListeners();

            // 4. 有對應事件時顯示按鈕並綁定點擊。
            if (i < TodayMonsterEvents.Count)
            {
                btn.gameObject.SetActive(true);
                MonsterEvent currentEvent = TodayMonsterEvents[i];
                // 5. 點擊後開啟事件詳細面板。
                btn.onClick.AddListener(() =>
                {
                    OnNewsClicked(currentEvent);
                });
            }
            else
            {
                // 沒有對應事件的按鈕隱藏。
                btn.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 使用 GameEventDefinition 設定新聞按鈕。
    /// 索引 0 顯示最高稀有度事件作為頭條；索引 1-4 顯示當日生成事件並保留生成順序。
    /// </summary>
    public void SetButtonEventsFromGameEvents()
    {
        if (_todayEvents == null || AllNewsButtons == null) return;

        List<GameEventDefinition> displayEvents = BuildHeadlineEvents(_todayEvents);

        for (int i = 0; i < AllNewsButtons.Count; i++)
        {
            Button btn = AllNewsButtons[i];
            btn.onClick.RemoveAllListeners();

            if (i < displayEvents.Count)
            {
                btn.gameObject.SetActive(true);
                GameEventDefinition currentEvent = displayEvents[i];

                // 更新按鈕顯示文字。
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

                // 依事件 ID 載入對應圖片。
                if (AllNewsImage != null && i < AllNewsImage.Count && AllNewsImage[i] != null)
                {
                    Image targetImage = AllNewsImage[i];
                    float targetSize = (i == 0) ? 600f : 250f;
                    SpriteLoader.LoadSpriteAsync(currentEvent.Id, sprite =>
                    {
                        if (targetImage != null)
                        {
                            targetImage.sprite = sprite != null ? sprite : NullSprite;
                            SpriteLoader.AdjustImageScale(targetImage, targetSize);
                        }
                    });
                }

                btn.onClick.AddListener(() =>
                {
                    OnGameEventClicked(currentEvent);
                });
            }
            else
            {
                // 沒有資料的位置清空顯示並隱藏按鈕。
                if (AllNewsTitle != null && i < AllNewsTitle.Count)
                {
                    AllNewsTitle[i].text = "";
                }
                if (AllNewsDetail != null && i < AllNewsDetail.Count)
                {
                    AllNewsDetail[i].text = "";
                }
                if (AllNewsImage != null && i < AllNewsImage.Count && AllNewsImage[i] != null)
                {
                    AllNewsImage[i].sprite = NullSprite;
                }
                btn.gameObject.SetActive(false);
            }
        }
    }

    private static List<GameEventDefinition> BuildHeadlineEvents(List<GameEventDefinition> events)
    {
        var generatedEvents = new List<GameEventDefinition>();
        if (events != null)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null)
                {
                    generatedEvents.Add(events[i]);
                }
            }
        }

        if (generatedEvents.Count == 0)
        {
            return generatedEvents;
        }

        int headlineIndex = 0;
        for (int i = 1; i < generatedEvents.Count; i++)
        {
            if (generatedEvents[i].eventRareity > generatedEvents[headlineIndex].eventRareity)
            {
                headlineIndex = i;
            }
        }

        var result = new List<GameEventDefinition>(generatedEvents.Count + 1)
        {
            generatedEvents[headlineIndex]
        };
        result.AddRange(generatedEvents);

        return result;
    }

    /// <summary>
    /// 點擊舊版 MonsterEvent 時顯示詳細內容。
    /// </summary>
    private void OnNewsClicked(MonsterEvent monsterEvent)
    {
        PlaySfx(openDetailSfx);

        // 顯示舊版 MonsterEvent 詳細面板。
        if (MoreDetailPanel != null)
        {
            MoreDetailPanel.SetActive(true);

            // 更新詳細面板 UI。
            if (DetailTitleText != null) DetailTitleText.text = monsterEvent.EventName;
            if (DetailContentText != null) DetailContentText.text = monsterEvent.EventDescription;
        }
    }

    /// <summary>
    /// 點擊 GameEventDefinition 時顯示詳細內容。
    /// </summary>
    private void OnGameEventClicked(GameEventDefinition gameEvent)
    {
        PlaySfx(openDetailSfx);

        if (MoreDetailPanel != null)
        {
            MoreDetailPanel.SetActive(true);

            if (DetailTitleText != null) DetailTitleText.text = gameEvent.Name;
            if (DetailContentText != null) DetailContentText.text = gameEvent.EventDescription;

            if (EventsImage != null)
            {
                SpriteLoader.LoadSpriteAsync(gameEvent.Id, sprite =>
                {
                    if (EventsImage != null)
                    {
                        EventsImage.sprite = sprite != null ? sprite : NullSprite;
                        SpriteLoader.AdjustImageScale(EventsImage, 566f);
                    }
                });
            }
        }
    }
    public void CloseMoreDetailPanel()
    {
        bool wasDetailPanelActive = MoreDetailPanel != null && MoreDetailPanel.activeSelf;
        if (MoreDetailPanel != null)
            MoreDetailPanel.SetActive(false);
        if (wasDetailPanelActive)
        {
            PlaySfx(closeDetailSfx);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
    }
}
