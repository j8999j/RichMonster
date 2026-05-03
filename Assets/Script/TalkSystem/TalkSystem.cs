using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using GameSystem;

namespace Talksystem
{
    /// <summary>
    /// 對話系統主控制器
    /// 參考 Flower 系統設計，使用括號指令控制對話流程
    /// 
    /// 使用方式:
    ///   1. 將此腳本掛載到場景中的 GameObject
    ///   2. 指定 DialogueView 引用
    ///   3. 呼叫 StartDialogue(TextAsset) 或 StartDialogue(string) 開始對話
    ///   4. 玩家按鍵時呼叫 Next() 推進對話
    /// 
    /// 內建指令:
    ///   [w]          - 等待按鍵後清除文字
    ///   [l]          - 等待按鍵後繼續追加
    ///   [r]          - 換行
    ///   [lr]         - 等待按鍵後換行
    ///   [c]          - 立即清除文字
    ///   [wait,毫秒]  - 自動等待指定毫秒
    ///   [speed,數值] - 修改逐字顯示速度 (秒/字)
    ///   [color,#RGB] / [/color] - 文字顏色
    ///   [size,數值]  / [/size]  - 文字大小
    ///   [b] / [/b]   - 粗體
    ///   [i] / [/i]   - 斜體
    /// 
    /// 自訂指令:
    ///   RegisterCommand("keyword", handler) 註冊自訂指令
    /// </summary>
    public class TalkSystem : MonoBehaviour
    {
        [Header("UI 顯示")]
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private StoryPlaybackPanel storyPlaybackPanel;
        [SerializeField] private DialogueChoicePresenter dialogueChoicePresenter;

        [Header("打字機設定")]
        [Tooltip("每個字的顯示間隔 (秒)")]
        [SerializeField] private float defaultTypeSpeed = 0.05f;

        [Header("輸入設定")]
        [Tooltip("按鍵觸發下一步")]
        [SerializeField] private KeyCode nextKey = KeyCode.Space;
        [Tooltip("按鍵跳過逐字顯示")]
        [SerializeField] private KeyCode skipKey = KeyCode.Return;
        [Tooltip("跳過整段對話的按鈕。若未指定，會在對話框上自動建立一個。")]
        [SerializeField] private Button skipDialogueButton;
        [Tooltip("是否啟用按鍵輸入 (設為 false 時需手動呼叫 Next())")]
        [SerializeField] private bool enableKeyInput = true;

        [Header("玩家鎖定")]
        [Tooltip("開始對話時自動鎖定玩家移動與互動，結束或中斷時自動解除")]
        [SerializeField] private bool autoLockPlayer = true;
        // === 事件 ===
        /// <summary>對話結束時觸發</summary>
        public event Action OnDialogueEnd;
        /// <summary>文字更新時觸發 (帶有完整目前文字)</summary>
        public event Action<string> OnTextUpdated;
        /// <summary>等待按鍵時觸發</summary>
        public event Action OnWaitingForInput;

        // === 狀態 ===
        private List<DialogueNode> _nodes;
        private int _currentNodeIndex;
        private DialogueCommandRegistry _commandRegistry;
        private float _currentTypeSpeed;
        private bool _isTyping;
        private bool _isWaitingForInput;
        private bool _isDialogueActive;
        private bool _isPaused;
        private Coroutine _typewriterCoroutine;
        private string _currentDisplayText;
        private TaskCompletionSource<bool> _currentDialogueTaskSource;
        private bool _ownsAutoMoveLock;
        private bool _ownsAutoInteractLock;

        // 等待按鍵後的行為
        private bool _waitClearAfter;
        private bool _appendNewLineAfterWait;

        /// <summary>對話是否正在進行中</summary>
        public bool IsDialogueActive => _isDialogueActive;

        /// <summary>是否正在等待玩家按鍵</summary>
        public bool IsWaitingForInput => _isWaitingForInput;

        /// <summary>是否正在逐字顯示中</summary>
        public bool IsTyping => _isTyping;

        /// <summary>目前使用的 DialogueView。</summary>
        public DialogueView CurrentDialogueView => dialogueView;

        public DialogueChoicePresenter CurrentChoicePresenter => ResolveDialogueChoicePresenter();

        /// <summary>指令註冊中心</summary>
        public DialogueCommandRegistry CommandRegistry => _commandRegistry;

        private void Awake()
        {
            _commandRegistry = new DialogueCommandRegistry();
            _currentTypeSpeed = defaultTypeSpeed;
            _currentDisplayText = "";
            dialogueView?.HidePanel();
            ResolveDialogueChoicePresenter()?.HideChoices();
            BindSkipDialogueButton();
        }

        private void OnDestroy()
        {
            UnbindSkipDialogueButton();
        }

        private void Update()
        {
            if (!enableKeyInput || !_isDialogueActive || _isPaused)
                return;

            // 跳過逐字顯示 (立即顯示完)
            if (_isTyping && Input.GetKeyDown(skipKey))
            {
                SkipTypewriter();
                return;
            }

            // 等待按鍵繼續
            if (_isWaitingForInput && Input.GetKeyDown(nextKey))
            {
                Next();
            }
        }

        // ===========================
        //  公開 API
        // ===========================

        /// <summary>
        /// 從 TextAsset 載入並開始對話
        /// </summary>
        public void StartDialogue(TextAsset textAsset)
        {
            if (textAsset == null)
            {
                Debug.LogError("[TalkSystem] TextAsset 為 null，無法開始對話");
                return;
            }
            StartDialogueInternal(DialogueParser.Parse(textAsset), null);
        }

        /// <summary>
        /// 從字串載入並開始對話
        /// </summary>
        public void StartDialogue(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                Debug.LogError("[TalkSystem] 文本為空，無法開始對話");
                return;
            }
            StartDialogueInternal(DialogueParser.Parse(rawText), null);
        }

        /// <summary>
        /// 從字串列表載入並開始對話
        /// </summary>
        public void StartDialogue(List<string> lines)
        {
            StartDialogueInternal(DialogueParser.Parse(lines), null);
        }

        /// <summary>
        /// 開始對話並等待自然結束。若被中斷或停止，回傳 false。
        /// </summary>
        public Task<bool> PlayDialogueAsync(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                Debug.LogError("[TalkSystem] 文本為空，無法開始對話");
                return Task.FromResult(false);
            }

            var taskSource = new TaskCompletionSource<bool>();
            StartDialogueInternal(DialogueParser.Parse(rawText), taskSource);
            return taskSource.Task;
        }

        /// <summary>
        /// 從 TextAsset 開始對話並等待自然結束。若被中斷或停止，回傳 false。
        /// </summary>
        public Task<bool> PlayDialogueAsync(TextAsset textAsset)
        {
            if (textAsset == null)
            {
                Debug.LogError("[TalkSystem] TextAsset 為 null，無法開始對話");
                return Task.FromResult(false);
            }

            var taskSource = new TaskCompletionSource<bool>();
            StartDialogueInternal(DialogueParser.Parse(textAsset), taskSource);
            return taskSource.Task;
        }

        /// <summary>
        /// 從字串列表開始對話並等待自然結束。若被中斷或停止，回傳 false。
        /// </summary>
        public Task<bool> PlayDialogueAsync(List<string> lines)
        {
            var taskSource = new TaskCompletionSource<bool>();
            StartDialogueInternal(DialogueParser.Parse(lines), taskSource);
            return taskSource.Task;
        }

        /// <summary>
        /// 推進對話 (玩家按下繼續鍵時呼叫)
        /// </summary>
        public void Next()
        {
            if (!_isDialogueActive)
                return;

            // 如果正在打字，先完成當前文字
            if (_isTyping)
            {
                SkipTypewriter();
                return;
            }

            if (_isWaitingForInput)
            {
                _isWaitingForInput = false;
                dialogueView?.HideContinueIndicator();

                // 根據等待模式決定後續動作
                if (_waitClearAfter)
                {
                    // [w] — 清除後繼續
                    _currentDisplayText = "";
                    dialogueView?.ClearText();
                    dialogueView?.ShowAllCharacters();
                }
                else if (_appendNewLineAfterWait)
                {
                    // [lr] — 追加換行後繼續
                    _currentDisplayText += "\n";
                    UpdateDisplayText();
                }
                // [l] — 直接繼續追加

                ProcessNodes();
            }
        }

        /// <summary>
        /// 註冊自訂指令
        /// </summary>
        public void RegisterCommand(string keyword, CommandHandler handler)
        {
            _commandRegistry.RegisterCommand(keyword, handler);
        }

        public Task<int> ShowChoicesAsync(string prompt, IReadOnlyList<string> options)
        {
            DialogueChoicePresenter presenter = ResolveDialogueChoicePresenter();
            if (presenter == null)
            {
                Debug.LogWarning("[TalkSystem] DialogueChoicePresenter not found.");
                return Task.FromResult(-1);
            }

            return presenter.ShowChoicesAsync(prompt, options);
        }

        public void HideChoices()
        {
            ResolveDialogueChoicePresenter()?.HideChoices();
        }

        /// <summary>
        /// 停止對話
        /// </summary>
        public void StopDialogue()
        {
            FinishDialogue(false, false, true);
        }

        /// <summary>
        /// 跳過整段對話，關閉對話框與故事面板，並以正常完成流程觸發對話結束事件。
        /// </summary>
        public void SkipDialogue()
        {
            if (!_isDialogueActive)
                return;

            FinishDialogue(true, true, true);
        }

        /// <summary>
        /// 暫停對話系統
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// 恢復對話系統
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// 設定 DialogueView (程式碼動態設定用)
        /// </summary>
        public void SetDialogueView(DialogueView view)
        {
            dialogueView = view;
            ResolveDialogueChoicePresenter();
        }

        private void OnDisable()
        {
            StopDialogue();
        }

        // ===========================
        //  內部實作
        // ===========================

        private void StartDialogueInternal(List<DialogueNode> nodes, TaskCompletionSource<bool> taskSource)
        {
            if (nodes == null || nodes.Count == 0)
            {
                Debug.LogWarning("[TalkSystem] 解析後無對話節點");
                taskSource?.TrySetResult(false);
                return;
            }

            // 停止之前的對話
            StopDialogue();
            _currentDialogueTaskSource = taskSource;

            _nodes = nodes;
            _currentNodeIndex = 0;
            _currentTypeSpeed = defaultTypeSpeed;
            _currentDisplayText = "";
            _isDialogueActive = true;
            _isTyping = false;
            _isWaitingForInput = false;
            _isPaused = false;
            AcquireAutoPlayerLocks();
            BindSkipDialogueButton();
            SetSkipDialogueButtonVisible(true);

            // 初始化 UI
            if (dialogueView != null)
            {
                dialogueView.ClearText();
                dialogueView.SetMaxVisibleCharacters(0);
                dialogueView.HideContinueIndicator();

                // 檢查第一個節點是否為 fadein 指令
                bool startsWithFadeIn = _nodes.Count > 0
                    && _nodes[0].Type == DialogueNodeType.Command
                    && _nodes[0].CommandKeyword == "fadein";

                if (startsWithFadeIn)
                {
                    // fadein 指令會處理淡入，先以 alpha=0 啟動
                    dialogueView.HidePanel();
                    dialogueView.gameObject.SetActive(true);
                }
                else
                {
                    dialogueView.ShowPanel();
                }
            }

            // 開始處理節點
            ProcessNodes();
        }

        /// <summary>
        /// 持續處理節點直到遇到需要等待的指令
        /// </summary>
        private void ProcessNodes()
        {
            while (_currentNodeIndex < _nodes.Count)
            {
                var node = _nodes[_currentNodeIndex];
                _currentNodeIndex++;

                if (node.Type == DialogueNodeType.Text)
                {
                    // 文字節點 → 逐字顯示
                    StartTypewriter(node.Content);
                    return; // 等待打字完成
                }
                else if (node.Type == DialogueNodeType.Command)
                {
                    bool shouldPause = ProcessCommand(node);
                    if (shouldPause)
                    {
                        return; // 等待玩家操作或計時器
                    }
                    // 不需要暫停的指令，繼續處理下一個節點
                }
            }

            // 所有節點處理完畢 → 對話結束
            EndDialogue();
        }

        private void AcquireAutoPlayerLocks()
        {
            _ownsAutoMoveLock = false;
            _ownsAutoInteractLock = false;

            if (!autoLockPlayer)
            {
                return;
            }

            GameManager manager = GameManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.LockPlayerMove(PlayerLockSources.TalkSystem);
            manager.LockPlayerInteract(PlayerLockSources.TalkSystem);
            _ownsAutoMoveLock = true;
            _ownsAutoInteractLock = true;
        }

        private void ReleaseAutoPlayerLocks()
        {
            if (!_ownsAutoMoveLock && !_ownsAutoInteractLock)
            {
                return;
            }

            GameManager manager = GameManager.Instance;
            if (manager != null)
            {
                if (_ownsAutoMoveLock)
                {
                    manager.UnlockPlayerMove(PlayerLockSources.TalkSystem);
                }

                if (_ownsAutoInteractLock)
                {
                    manager.UnlockPlayerInteract(PlayerLockSources.TalkSystem);
                }
            }

            _ownsAutoMoveLock = false;
            _ownsAutoInteractLock = false;
        }

        private void FinishDialogue(bool completed, bool raiseEndEvent, bool closePanels)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _isDialogueActive = false;
            _isTyping = false;
            _isWaitingForInput = false;
            _isPaused = false;
            _waitClearAfter = false;
            _appendNewLineAfterWait = false;
            _currentDisplayText = "";
            _nodes = null;

            SetSkipDialogueButtonVisible(false);

            if (closePanels)
            {
                CloseDialoguePanels();
            }

            CompleteCurrentDialogue(completed, raiseEndEvent);
        }

        private void CloseDialoguePanels()
        {
            HideChoices();
            CloseTalkPanel();

            StoryPlaybackPanel panel = ResolveStoryPlaybackPanel();
            if (panel != null)
            {
                panel.CloseImmediate();
            }
        }

        private void CloseTalkPanel()
        {
            if (dialogueView != null)
            {
                dialogueView.HideContinueIndicator();
                dialogueView.ClearText();
                dialogueView.ShowAllCharacters();
                dialogueView.HidePanel();
            }
        }

        private void BindSkipDialogueButton()
        {
            EnsureSkipDialogueButton();

            if (skipDialogueButton == null)
                return;

            skipDialogueButton.onClick.RemoveListener(SkipDialogue);
            skipDialogueButton.onClick.AddListener(SkipDialogue);
            SetSkipDialogueButtonVisible(_isDialogueActive);
        }

        private void UnbindSkipDialogueButton()
        {
            if (skipDialogueButton != null)
            {
                skipDialogueButton.onClick.RemoveListener(SkipDialogue);
            }
        }

        private void EnsureSkipDialogueButton()
        {
            if (skipDialogueButton != null || dialogueView == null)
                return;

            GameObject buttonObject = new GameObject("SkipDialogueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(dialogueView.transform, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-24f, -24f);
            rectTransform.sizeDelta = new Vector2(120f, 48f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = "跳過";
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            skipDialogueButton = buttonObject.GetComponent<Button>();
        }

        private void SetSkipDialogueButtonVisible(bool visible)
        {
            if (skipDialogueButton != null)
            {
                skipDialogueButton.gameObject.SetActive(visible);
            }
        }

        private void CompleteCurrentDialogue(bool completed, bool raiseEndEvent)
        {
            TaskCompletionSource<bool> taskSource = _currentDialogueTaskSource;
            _currentDialogueTaskSource = null;

            ReleaseAutoPlayerLocks();

            if (raiseEndEvent)
            {
                OnDialogueEnd?.Invoke();
            }

            taskSource?.TrySetResult(completed);
        }

        /// <summary>
        /// 處理指令節點
        /// </summary>
        /// <returns>是否需要暫停流程</returns>
        private bool ProcessCommand(DialogueNode node)
        {
            string keyword = node.CommandKeyword;

            switch (keyword)
            {
                case "w": // 等待按鍵後清除文字
                    WaitForInput(clearAfter: true, newLineAfter: false);
                    return true;

                case "l": // 等待按鍵後繼續追加
                    WaitForInput(clearAfter: false, newLineAfter: false);
                    return true;

                case "lr": // 等待按鍵後換行
                    WaitForInput(clearAfter: false, newLineAfter: true);
                    return true;

                case "r": // 換行
                    _currentDisplayText += "\n";
                    UpdateDisplayText();
                    return false;

                case "c": // 立即清除
                    _currentDisplayText = "";
                    dialogueView?.ClearText();
                    dialogueView?.ShowAllCharacters();
                    return false;

                case "wait": // 自動等待
                    if (node.Parameters.Count > 0 && int.TryParse(node.Parameters[0], out int waitMs))
                    {
                        _typewriterCoroutine = StartCoroutine(WaitCoroutine(waitMs / 1000f));
                        return true;
                    }
                    return false;

                case "speed": // 修改打字速度
                    if (node.Parameters.Count > 0 && float.TryParse(node.Parameters[0], out float speed))
                    {
                        _currentTypeSpeed = speed;
                    }
                    return false;

                case "fadein": // 對話面板淡入
                {
                    float fadeDuration = 0.5f;
                    if (node.Parameters.Count > 0 && float.TryParse(node.Parameters[0], out float fi))
                        fadeDuration = fi;
                    _typewriterCoroutine = StartCoroutine(FadeCoroutine(true, fadeDuration));
                    return true;
                }

                case "fadeout": // 對話面板淡出
                {
                    float fadeDuration = 0.5f;
                    if (node.Parameters.Count > 0 && float.TryParse(node.Parameters[0], out float fo))
                        fadeDuration = fo;
                    _typewriterCoroutine = StartCoroutine(FadeCoroutine(false, fadeDuration));
                    return true;
                }

                case "storypanel":
                    return ProcessStoryPanelCommand(node.Parameters);

                case "storyopen":
                    return StartStoryPanelShow(GetOptionalFloat(node.Parameters, 0, -1f));

                case "storyimage":
                    if (node.Parameters.Count > 0)
                        return StartStoryImageLoad(node.Parameters[0]);
                    Debug.LogWarning("[TalkSystem] [storyimage] 缺少圖片 Addressables ID");
                    return false;

                case "storyclose":
                    return StartStoryPanelHide(GetOptionalFloat(node.Parameters, 0, -1f));

                default:
                    // 嘗試執行自訂指令
                    if (_commandRegistry.ExecuteCommand(keyword, node.Parameters))
                    {
                        return false;
                    }
                    else
                    {
                        Debug.LogWarning($"[TalkSystem] 未知的指令: [{keyword}]");
                        return false;
                    }
            }
        }

        /// <summary>
        /// 進入等待按鍵狀態
        /// </summary>
        private bool ProcessStoryPanelCommand(List<string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                Debug.LogWarning("[TalkSystem] [storypanel] 缺少 action，請使用 show、image 或 close");
                return false;
            }

            string action = parameters[0].ToLowerInvariant();
            switch (action)
            {
                case "show":
                case "open":
                    return StartStoryPanelShow(GetOptionalFloat(parameters, 1, -1f));

                case "image":
                case "load":
                    if (parameters.Count > 1)
                        return StartStoryImageLoad(parameters[1]);
                    Debug.LogWarning("[TalkSystem] [storypanel,image] 缺少圖片 Addressables ID");
                    return false;

                case "close":
                case "hide":
                    return StartStoryPanelHide(GetOptionalFloat(parameters, 1, -1f));

                default:
                    Debug.LogWarning($"[TalkSystem] 未知的 storypanel action: {parameters[0]}");
                    return false;
            }
        }

        private bool StartStoryPanelShow(float duration)
        {
            StoryPlaybackPanel panel = ResolveStoryPlaybackPanel();
            if (panel == null)
            {
                Debug.LogWarning("[TalkSystem] 找不到 StoryPlaybackPanel，無法顯示故事面板");
                return false;
            }

            _typewriterCoroutine = StartCoroutine(StoryPanelTaskCoroutine(panel.ShowAsync(duration), "顯示故事面板"));
            return true;
        }

        private bool StartStoryImageLoad(string imageId)
        {
            if (string.IsNullOrWhiteSpace(imageId))
            {
                Debug.LogWarning("[TalkSystem] 故事圖片 Addressables ID 為空");
                return false;
            }

            _typewriterCoroutine = StartCoroutine(StoryImageLoadCoroutine(imageId));
            return true;
        }

        private IEnumerator StoryImageLoadCoroutine(string imageId)
        {
            StoryPlaybackPanel panel = ResolveStoryPlaybackPanel();
            if (panel == null)
            {
                Debug.LogWarning("[TalkSystem] 找不到 StoryPlaybackPanel，無法載入故事圖片");
                _typewriterCoroutine = null;
                ProcessNodes();
                yield break;
            }

            Task loadTask = panel.LoadImageAsync(imageId);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.IsFaulted)
            {
                Debug.LogError($"[TalkSystem] 載入故事圖片失敗: {imageId}, Error: {loadTask.Exception}");
            }

            _typewriterCoroutine = null;
            ProcessNodes();
        }

        private bool StartStoryPanelHide(float duration)
        {
            StoryPlaybackPanel panel = ResolveStoryPlaybackPanel();
            if (panel == null)
            {
                Debug.LogWarning("[TalkSystem] 找不到 StoryPlaybackPanel，無法關閉故事面板");
                return false;
            }

            _typewriterCoroutine = StartCoroutine(StoryPanelTaskCoroutine(panel.HideAsync(duration), "關閉故事面板"));
            return true;
        }

        private IEnumerator StoryPanelTaskCoroutine(Task panelTask, string actionName)
        {
            while (!panelTask.IsCompleted)
            {
                yield return null;
            }

            if (panelTask.IsFaulted)
            {
                Debug.LogError($"[TalkSystem] {actionName}失敗: {panelTask.Exception}");
            }

            _typewriterCoroutine = null;
            ProcessNodes();
        }

        private float GetOptionalFloat(List<string> parameters, int index, float fallback)
        {
            if (parameters == null || parameters.Count <= index)
                return fallback;

            return float.TryParse(parameters[index], out float value) ? value : fallback;
        }

        private StoryPlaybackPanel ResolveStoryPlaybackPanel()
        {
            if (storyPlaybackPanel != null)
                return storyPlaybackPanel;

            storyPlaybackPanel = FindObjectOfType<StoryPlaybackPanel>(true);
            return storyPlaybackPanel;
        }

        private DialogueChoicePresenter ResolveDialogueChoicePresenter()
        {
            if (dialogueChoicePresenter != null)
            {
                dialogueChoicePresenter.Configure(dialogueView);
                return dialogueChoicePresenter;
            }

            if (dialogueView != null)
            {
                dialogueChoicePresenter = dialogueView.GetComponentInChildren<DialogueChoicePresenter>(true);
                if (dialogueChoicePresenter == null)
                    dialogueChoicePresenter = dialogueView.gameObject.AddComponent<DialogueChoicePresenter>();
            }

            if (dialogueChoicePresenter == null)
                dialogueChoicePresenter = FindObjectOfType<DialogueChoicePresenter>(true);

            if (dialogueChoicePresenter != null)
                dialogueChoicePresenter.Configure(dialogueView);

            return dialogueChoicePresenter;
        }

        private void WaitForInput(bool clearAfter, bool newLineAfter)
        {
            _waitClearAfter = clearAfter;
            _appendNewLineAfterWait = newLineAfter;
            _isWaitingForInput = true;
            dialogueView?.ShowContinueIndicator();
            OnWaitingForInput?.Invoke();
        }

        /// <summary>
        /// 自動等待協程
        /// </summary>
        private IEnumerator WaitCoroutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _typewriterCoroutine = null;
            ProcessNodes();
        }

        /// <summary>
        /// 淡入/淡出協程
        /// </summary>
        private IEnumerator FadeCoroutine(bool fadeIn, float duration)
        {
            if (dialogueView != null)
            {
                IEnumerator fadeRoutine = fadeIn
                    ? dialogueView.FadeIn(duration)
                    : dialogueView.FadeOut(duration);
                yield return fadeRoutine;
            }
            _typewriterCoroutine = null;
            ProcessNodes();
        }

        // ===========================
        //  逐字顯示 (Typewriter)
        // ===========================

        /// <summary>
        /// 開始逐字顯示一段文字
        /// </summary>
        private void StartTypewriter(string text)
        {
            // 記錄新文字之前已可見的字元數
            int previousVisibleCount = 0;
            if (dialogueView?.DialogueTextComponent != null)
            {
                int currentMax = dialogueView.DialogueTextComponent.maxVisibleCharacters;
                if (currentMax != int.MaxValue)
                    previousVisibleCount = currentMax;
            }

            _currentDisplayText += text;
            UpdateDisplayText();

            // 立即隱藏新增的字元，避免在協程啟動前顯示全部文字
            dialogueView?.SetMaxVisibleCharacters(previousVisibleCount);

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(previousVisibleCount));
        }

        /// <summary>
        /// 逐字顯示協程
        /// </summary>
        private IEnumerator TypewriterCoroutine(int startVisibleCount)
        {
            _isTyping = true;

            if (dialogueView == null)
            {
                _isTyping = false;
                ProcessNodes();
                yield break;
            }

            int totalChars = dialogueView.GetParsedTextLength();
            int visibleChars = startVisibleCount;

            // 先設定到起始可見數
            dialogueView.SetMaxVisibleCharacters(visibleChars);

            while (visibleChars < totalChars)
            {
                // 暫停檢查
                while (_isPaused)
                {
                    yield return null;
                }

                visibleChars++;
                dialogueView.SetMaxVisibleCharacters(visibleChars);

                OnTextUpdated?.Invoke(dialogueView.GetText());

                yield return new WaitForSeconds(_currentTypeSpeed);
            }

            _isTyping = false;
            _typewriterCoroutine = null;

            // 打字完成後繼續處理下一個節點
            ProcessNodes();
        }

        /// <summary>
        /// 跳過逐字顯示，立即顯示完整文字
        /// </summary>
        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _isTyping = false;
            dialogueView?.ShowAllCharacters();

            // 繼續處理後續節點
            ProcessNodes();
        }

        /// <summary>
        /// 更新 UI 顯示文字
        /// </summary>
        private void UpdateDisplayText()
        {
            dialogueView?.SetText(_currentDisplayText);
        }

        /// <summary>
        /// 對話結束
        /// </summary>
        private void EndDialogue()
        {
            CloseTalkPanel();
            FinishDialogue(true, true, false);
        }
    }
}
