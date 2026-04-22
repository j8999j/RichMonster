using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlayerInfoUIButton : MonoBehaviour
{
    public enum UIAction
    {
        OpenBag = 0,
        OpenSouvenirBag = 1,
        OpenAchievement = 2,
        CloseAll = 3,
        OpenBook = 4,
        OpenNews = 5
    }

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
            case UIAction.OpenBook: PlayerInfoUIEvents.InvokeOpenBook(); break;
            case UIAction.OpenNews: PlayerInfoUIEvents.InvokeOpenNews(); break;
            case UIAction.CloseAll: PlayerInfoUIEvents.InvokeCloseAll(); break;
        }
    }

    private void OnDestroy()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.RemoveListener(OnClick);
    }
}
