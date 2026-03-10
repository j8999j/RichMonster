using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        [Header("打字機設定")]
        [Tooltip("每個字的顯示間隔 (秒)")]
        [SerializeField] private float defaultTypeSpeed = 0.05f;

        [Header("輸入設定")]
        [Tooltip("按鍵觸發下一步")]
        [SerializeField] private KeyCode nextKey = KeyCode.Space;
        [Tooltip("按鍵跳過逐字顯示")]
        [SerializeField] private KeyCode skipKey = KeyCode.Return;
        [Tooltip("是否啟用按鍵輸入 (設為 false 時需手動呼叫 Next())")]
        [SerializeField] private bool enableKeyInput = true;

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

        // 等待按鍵後的行為
        private bool _waitClearAfter;
        private bool _appendNewLineAfterWait;

        /// <summary>對話是否正在進行中</summary>
        public bool IsDialogueActive => _isDialogueActive;

        /// <summary>是否正在等待玩家按鍵</summary>
        public bool IsWaitingForInput => _isWaitingForInput;

        /// <summary>是否正在逐字顯示中</summary>
        public bool IsTyping => _isTyping;

        /// <summary>指令註冊中心</summary>
        public DialogueCommandRegistry CommandRegistry => _commandRegistry;

        private void Awake()
        {
            _commandRegistry = new DialogueCommandRegistry();
            _currentTypeSpeed = defaultTypeSpeed;
            _currentDisplayText = "";
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
            StartDialogueInternal(DialogueParser.Parse(textAsset));
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
            StartDialogueInternal(DialogueParser.Parse(rawText));
        }

        /// <summary>
        /// 從字串列表載入並開始對話
        /// </summary>
        public void StartDialogue(List<string> lines)
        {
            StartDialogueInternal(DialogueParser.Parse(lines));
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

        /// <summary>
        /// 停止對話
        /// </summary>
        public void StopDialogue()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _isDialogueActive = false;
            _isTyping = false;
            _isWaitingForInput = false;
            _currentDisplayText = "";
            _nodes = null;
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
        }

        // ===========================
        //  內部實作
        // ===========================

        private void StartDialogueInternal(List<DialogueNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                Debug.LogWarning("[TalkSystem] 解析後無對話節點");
                return;
            }

            // 停止之前的對話
            StopDialogue();

            _nodes = nodes;
            _currentNodeIndex = 0;
            _currentTypeSpeed = defaultTypeSpeed;
            _currentDisplayText = "";
            _isDialogueActive = true;
            _isTyping = false;
            _isWaitingForInput = false;
            _isPaused = false;

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
                if (fadeIn)
                    yield return StartCoroutine(dialogueView.FadeIn(duration));
                else
                    yield return StartCoroutine(dialogueView.FadeOut(duration));
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

            // 立即隱藏新增的字元，避免在協程啟動前闃現全部文字
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
            _isDialogueActive = false;
            _isTyping = false;
            _isWaitingForInput = false;
            OnDialogueEnd?.Invoke();
        }
    }
}
