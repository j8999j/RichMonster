// ============================================================
// ManualGuideButtonRegister.cs
// 通用腳本：用於預先將未啟用的 UI 按鈕註冊到引導系統
// ============================================================
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛載於任何會在 Awake 時就存在、但底下子面板可能隱藏的父物件上
/// 統一負責將隱藏按鈕提早註冊入 GuideLookupRegistry，避免引導任務找不到
/// </summary>
public class ManualGuideButtonRegister : MonoBehaviour
{
    [Serializable]
    public struct ButtonRegistration
    {
        public Button button;
        public GuideIDs.ButtonType buttonType;
    }

    [Header("要提早註冊的引導按鈕清單")]
    public ButtonRegistration[] buttonsToRegister;

    private void Awake()
    {
        if (buttonsToRegister == null) return;

        foreach (var reg in buttonsToRegister)
        {
            if (reg.button != null)
            {
                GuideLookupRegistry.Instance.RegisterButton(new RegisteredButton(GuideIDs.ToId(reg.buttonType), reg.button));
            }
        }
    }

    /// <summary>
    /// 內部的 IGuideButton 代理物件，不依賴 MonoBehaviour 生命週期
    /// </summary>
    private class RegisteredButton : IGuideButton
    {
        public string ButtonId { get; }
        public GameObject ButtonObject { get; }
        public event Action<string> OnClicked;

        public RegisteredButton(string id, Button btn)
        {
            ButtonId = id;
            ButtonObject = btn.gameObject;
            btn.onClick.AddListener(() => OnClicked?.Invoke(id));
        }
    }
}
