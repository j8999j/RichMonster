// ============================================================
// GuideButton.cs - 通用引導按鈕元件
// 掛載到任何 UI Button 上，自動向 GuideLookupRegistry 註冊
// ============================================================
using System;
using UnityEngine;
using UnityEngine.UI;

public class GuideButton : MonoBehaviour, IGuideButton
{
    [SerializeField] private GuideIDs.ButtonType buttonType;
    public string ButtonId => GuideIDs.ToId(buttonType);
    public GameObject ButtonObject => gameObject;
    public event Action<string> OnClicked;
    void OnEnable()
    {
        GuideLookupRegistry.Instance.RegisterButton(this);
        GetComponent<Button>().onClick.AddListener(HandleClick);
    }
    private void OnDisable()
    {
        GuideLookupRegistry.Instance.UnregisterButton(this);
        GetComponent<Button>().onClick.RemoveListener(HandleClick);
    }
    private void HandleClick()
    {
        OnClicked?.Invoke(ButtonId);
    }
}
