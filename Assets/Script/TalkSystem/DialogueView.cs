using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace Talksystem
{
    /// <summary>
    /// 對話 UI 顯示層
    /// 掛載在含有 TMP_Text 的 UI 物件上
    /// 負責文字顯示、清除、以及「繼續」指示器的控制
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        [Header("文字顯示")]
        [SerializeField] private TMP_Text dialogueText;

        [Header("繼續指示器 (可選)")]
        [SerializeField] private GameObject continueIndicator;

        [Header("說話者名稱 (可選)")]
        [SerializeField] private TMP_Text speakerNameText;

        [Header("對話面板")]
        [SerializeField] private CanvasGroup dialoguePanel;

        /// <summary>
        /// 取得 TMP_Text 的引用 (供 TalkSystem 直接控制 maxVisibleCharacters)
        /// </summary>
        public TMP_Text DialogueTextComponent => dialogueText;

        /// <summary>
        /// 設定顯示文字
        /// </summary>
        public void SetText(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
            }
        }

        /// <summary>
        /// 取得目前顯示文字
        /// </summary>
        public string GetText()
        {
            return dialogueText != null ? dialogueText.text : "";
        }

        /// <summary>
        /// 追加文字
        /// </summary>
        public void AppendText(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text += text;
            }
        }

        /// <summary>
        /// 清除文字
        /// </summary>
        public void ClearText()
        {
            if (dialogueText != null)
            {
                dialogueText.text = "";
            }
        }

        /// <summary>
        /// 設定最大可見字元數 (逐字顯示用)
        /// </summary>
        public void SetMaxVisibleCharacters(int count)
        {
            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = count;
            }
        }

        /// <summary>
        /// 取得文字總字元數 (排除 rich text tag)
        /// </summary>
        public int GetParsedTextLength()
        {
            if (dialogueText != null)
            {
                dialogueText.ForceMeshUpdate();
                return dialogueText.textInfo.characterCount;
            }
            return 0;
        }

        /// <summary>
        /// 顯示全部文字
        /// </summary>
        public void ShowAllCharacters()
        {
            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
        }

        /// <summary>
        /// 設定說話者名稱
        /// </summary>
        public void SetSpeakerName(string name)
        {
            if (speakerNameText != null)
            {
                speakerNameText.text = name;
                speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(name));
            }
        }

        /// <summary>
        /// 顯示繼續指示器
        /// </summary>
        public void ShowContinueIndicator()
        {
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
            }
        }

        /// <summary>
        /// 隱藏繼續指示器
        /// </summary>
        public void HideContinueIndicator()
        {
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// 顯示對話面板
        /// </summary>
        public void ShowPanel()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.alpha = 1f;
                dialoguePanel.interactable = true;
                dialoguePanel.blocksRaycasts = true;
            }
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隱藏對話面板
        /// </summary>
        public void HidePanel()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.alpha = 0f;
                dialoguePanel.interactable = false;
                dialoguePanel.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 淡入對話面板
        /// </summary>
        /// <param name="duration">淡入時間 (秒)</param>
        /// <param name="onComplete">完成後回呼</param>
        public IEnumerator FadeIn(float duration, Action onComplete = null)
        {
            gameObject.SetActive(true);

            if (dialoguePanel == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            dialoguePanel.interactable = false;
            dialoguePanel.blocksRaycasts = true;

            float elapsed = 0f;
            float startAlpha = dialoguePanel.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                dialoguePanel.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
                yield return null;
            }

            dialoguePanel.alpha = 1f;
            dialoguePanel.interactable = true;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 淡出對話面板
        /// </summary>
        /// <param name="duration">淡出時間 (秒)</param>
        /// <param name="onComplete">完成後回呼</param>
        public IEnumerator FadeOut(float duration, Action onComplete = null)
        {
            if (dialoguePanel == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            dialoguePanel.interactable = false;

            float elapsed = 0f;
            float startAlpha = dialoguePanel.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                dialoguePanel.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }

            dialoguePanel.alpha = 0f;
            dialoguePanel.blocksRaycasts = false;
            onComplete?.Invoke();
        }
    }
}
