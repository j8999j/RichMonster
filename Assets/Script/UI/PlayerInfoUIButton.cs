using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlayerInfoUIButton : MonoBehaviour
{
    public enum UIAction { OpenBag, OpenSouvenirBag, OpenAchievement, CloseAll }

    [SerializeField] private UIAction action;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        switch (action)
        {
            case UIAction.OpenBag: PlayerInfoUIEvents.InvokeOpenBag(); break;
            case UIAction.OpenSouvenirBag: PlayerInfoUIEvents.InvokeOpenSouvenirBag(); break;
            case UIAction.OpenAchievement: PlayerInfoUIEvents.InvokeOpenAchievement(); break;
            case UIAction.CloseAll: PlayerInfoUIEvents.InvokeCloseAll(); break;
        }
    }

    private void OnDestroy()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.RemoveListener(OnClick);
    }
}
