using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Talksystem
{
    /// <summary>
    /// 對話節點類型
    /// </summary>
    public enum DialogueNodeType
    {
        Text,    // 純文字（可能包含 TMPro rich text tag）
        Command  // 指令節點
    }

    /// <summary>
    /// 解析後的對話節點
    /// </summary>
    public class DialogueNode
    {
        public DialogueNodeType Type;
        public string Content;          // Text 類型時為文字內容
        public string CommandKeyword;   // Command 類型時的指令關鍵字
        public List<string> Parameters; // Command 類型時的參數列表

        /// <summary>
        /// 建立文字節點
        /// </summary>
        public static DialogueNode CreateText(string text)
        {
            return new DialogueNode
            {
                Type = DialogueNodeType.Text,
                Content = text
            };
        }

        /// <summary>
        /// 建立指令節點
        /// </summary>
        public static DialogueNode CreateCommand(string keyword, List<string> parameters = null)
        {
            return new DialogueNode
            {
                Type = DialogueNodeType.Command,
                CommandKeyword = keyword,
                Parameters = parameters ?? new List<string>()
            };
        }
    }

    /// <summary>
    /// 對話文本解析器
    /// 將原始文本解析為 DialogueNode 序列
    /// 
    /// 指令格式: [keyword] 或 [keyword,param1,param2,...]
    /// 轉義: [[ 顯示為 [
    /// 
    /// 格式指令 (color, size, b, i) 會轉換為 TMPro rich text tag 嵌入文字節點
    /// 流程控制指令 (w, l, r, lr, c, wait, speed) 保留為 Command 節點
    /// </summary>
    public static class DialogueParser
    {
        // 格式指令對應的 TMPro tag（這些不產生 Command 節點，而是轉成 rich text）
        private static readonly HashSet<string> FormatCommands = new HashSet<string>
        {
            "color", "/color",
            "size", "/size",
            "b", "/b",
            "i", "/i"
        };

        /// <summary>
        /// 從 TextAsset 解析
        /// </summary>
        public static List<DialogueNode> Parse(TextAsset textAsset)
        {
            if (textAsset == null)
            {
                Debug.LogError("[TalkSystem] TextAsset 為 null");
                return new List<DialogueNode>();
            }
            return Parse(textAsset.text);
        }

        /// <summary>
        /// 從字串解析對話文本為節點序列
        /// </summary>
        public static List<DialogueNode> Parse(string rawText)
        {
            var nodes = new List<DialogueNode>();
            if (string.IsNullOrEmpty(rawText))
                return nodes;

            var textBuffer = new StringBuilder();
            int i = 0;

            while (i < rawText.Length)
            {
                char c = rawText[i];

                if (c == '[')
                {
                    // 檢查轉義: [[
                    if (i + 1 < rawText.Length && rawText[i + 1] == '[')
                    {
                        textBuffer.Append('[');
                        i += 2;
                        continue;
                    }

                    // 尋找對應的 ]
                    int closeIndex = rawText.IndexOf(']', i + 1);
                    if (closeIndex == -1)
                    {
                        // 找不到閉合括號，當作普通文字
                        textBuffer.Append(c);
                        i++;
                        continue;
                    }

                    // 解析指令內容
                    string commandContent = rawText.Substring(i + 1, closeIndex - i - 1).Trim();
                    i = closeIndex + 1;

                    if (string.IsNullOrEmpty(commandContent))
                    {
                        textBuffer.Append("[]");
                        continue;
                    }

                    // 解析關鍵字與參數
                    ParseCommandContent(commandContent, out string keyword, out List<string> parameters);

                    // 判斷是格式指令還是流程指令
                    if (FormatCommands.Contains(keyword))
                    {
                        // 格式指令 → 轉換為 TMPro rich text tag
                        string richTag = ConvertToRichTextTag(keyword, parameters);
                        textBuffer.Append(richTag);
                    }
                    else
                    {
                        // 流程/自訂指令 → 先將累積的文字刷出，再建立 Command 節點
                        FlushTextBuffer(textBuffer, nodes);
                        nodes.Add(DialogueNode.CreateCommand(keyword, parameters));
                    }
                }
                else if (c == '\r')
                {
                    // 忽略 \r，只保留 \n
                    i++;
                }
                else if (c == '\n')
                {
                    // 換行符不自動插入，除非兩個連續換行表示段落
                    // 單一換行忽略（對話文本中換行只是編輯方便）
                    i++;
                }
                else
                {
                    textBuffer.Append(c);
                    i++;
                }
            }

            // 刷出最後殘留的文字
            FlushTextBuffer(textBuffer, nodes);

            return nodes;
        }

        /// <summary>
        /// 從字串列表解析（多行分別解析後合併）
        /// </summary>
        public static List<DialogueNode> Parse(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return new List<DialogueNode>();

            string combined = string.Join("", lines);
            return Parse(combined);
        }

        /// <summary>
        /// 解析指令內容 (keyword,param1,param2,...) 
        /// </summary>
        private static void ParseCommandContent(string content, out string keyword, out List<string> parameters)
        {
            parameters = new List<string>();

            int commaIndex = content.IndexOf(',');
            if (commaIndex == -1)
            {
                keyword = content;
                return;
            }

            keyword = content.Substring(0, commaIndex).Trim();
            string paramStr = content.Substring(commaIndex + 1);

            // 以逗號分隔參數
            string[] parts = paramStr.Split(',');
            for (int p = 0; p < parts.Length; p++)
            {
                string trimmed = parts[p].Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    parameters.Add(trimmed);
                }
            }
        }

        /// <summary>
        /// 將格式指令轉換為 TMPro rich text tag
        /// </summary>
        private static string ConvertToRichTextTag(string keyword, List<string> parameters)
        {
            switch (keyword)
            {
                case "color":
                    if (parameters.Count > 0)
                        return $"<color={parameters[0]}>";
                    return "<color=#FFFFFF>";

                case "/color":
                    return "</color>";

                case "size":
                    if (parameters.Count > 0)
                        return $"<size={parameters[0]}>";
                    return "<size=100%>";

                case "/size":
                    return "</size>";

                case "b":
                    return "<b>";

                case "/b":
                    return "</b>";

                case "i":
                    return "<i>";

                case "/i":
                    return "</i>";

                default:
                    return "";
            }
        }

        /// <summary>
        /// 將文字緩衝區的內容刷出為 Text 節點
        /// </summary>
        private static void FlushTextBuffer(StringBuilder buffer, List<DialogueNode> nodes)
        {
            if (buffer.Length > 0)
            {
                nodes.Add(DialogueNode.CreateText(buffer.ToString()));
                buffer.Clear();
            }
        }
    }
}
