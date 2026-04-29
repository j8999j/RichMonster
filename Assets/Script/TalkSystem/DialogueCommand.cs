using System;
using System.Collections.Generic;
using UnityEngine;

namespace Talksystem
{
    /// <summary>
    /// 指令處理委派
    /// </summary>
    public delegate void CommandHandler(List<string> parameters);

    /// <summary>
    /// 對話指令註冊中心
    /// 管理所有內建與自訂指令的註冊和查詢
    /// </summary>
    public class DialogueCommandRegistry
    {
        private readonly Dictionary<string, CommandHandler> _commands = new Dictionary<string, CommandHandler>();

        // 內建流程控制指令（由 TalkSystem 直接處理，不在此註冊 handler）
        private static readonly HashSet<string> BuiltInFlowCommands = new HashSet<string>
        {
            "w", "l", "r", "lr", "c",
            "wait", "speed",
            "fadein", "fadeout",
            "storypanel", "storyopen", "storyimage", "storyclose",
            "color", "/color",
            "size", "/size",
            "b", "/b",
            "i", "/i"
        };

        /// <summary>
        /// 註冊自訂指令
        /// </summary>
        /// <param name="keyword">指令關鍵字 (不含中括號)</param>
        /// <param name="handler">指令處理函式</param>
        public void RegisterCommand(string keyword, CommandHandler handler)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                Debug.LogWarning("[TalkSystem] 無法註冊空的指令關鍵字");
                return;
            }

            if (BuiltInFlowCommands.Contains(keyword))
            {
                Debug.LogWarning($"[TalkSystem] 指令 '{keyword}' 為內建指令，無法覆蓋");
                return;
            }

            if (_commands.ContainsKey(keyword))
            {
                Debug.LogWarning($"[TalkSystem] 指令 '{keyword}' 已存在，將被覆蓋");
            }

            _commands[keyword] = handler;
        }

        /// <summary>
        /// 取消註冊自訂指令
        /// </summary>
        public bool UnregisterCommand(string keyword)
        {
            return _commands.Remove(keyword);
        }

        /// <summary>
        /// 檢查是否為已知指令（內建 + 自訂）
        /// </summary>
        public bool IsKnownCommand(string keyword)
        {
            return BuiltInFlowCommands.Contains(keyword) || _commands.ContainsKey(keyword);
        }

        /// <summary>
        /// 檢查是否為內建流程控制指令
        /// </summary>
        public bool IsBuiltInCommand(string keyword)
        {
            return BuiltInFlowCommands.Contains(keyword);
        }

        /// <summary>
        /// 取得自訂指令的處理函式
        /// </summary>
        public CommandHandler GetHandler(string keyword)
        {
            _commands.TryGetValue(keyword, out CommandHandler handler);
            return handler;
        }

        /// <summary>
        /// 執行自訂指令
        /// </summary>
        /// <returns>是否成功執行</returns>
        public bool ExecuteCommand(string keyword, List<string> parameters)
        {
            if (_commands.TryGetValue(keyword, out CommandHandler handler))
            {
                try
                {
                    handler.Invoke(parameters);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TalkSystem] 執行指令 '{keyword}' 時發生錯誤: {ex.Message}");
                    return false;
                }
            }
            return false;
        }
    }
}
