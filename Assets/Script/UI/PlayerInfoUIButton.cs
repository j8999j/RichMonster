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
        OpenNews = 5,
        OpenContract = 6,
        OpenSouvenirShop = 7
    }

    [SerializeField] private UIAction action;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        _button.onClick.AddListener(OnClick);
        PlayerInfoUIEvents.OnPageChanged += HandlePageChanged;
        HandlePageChanged(PlayerInfoUIEvents.ActivePage);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);

        PlayerInfoUIEvents.OnPageChanged -= HandlePageChanged;
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
            case UIAction.OpenContract: PlayerInfoUIEvents.InvokeOpenContract(); break;
            case UIAction.OpenSouvenirShop: PlayerInfoUIEvents.InvokeOpenSouvenirShop(); break;
            case UIAction.CloseAll: PlayerInfoUIEvents.InvokeCloseAll(); break;
        }
    }

    private void HandlePageChanged(PlayerInfoPage activePage)
    {
        if (_button == null)
            return;

        if (action == UIAction.CloseAll)
        {
            _button.interactable = activePage != PlayerInfoPage.None;
            return;
        }

        _button.interactable = TryGetTargetPage(out PlayerInfoPage targetPage)
            && activePage != targetPage;
    }

    private bool TryGetTargetPage(out PlayerInfoPage page)
    {
        switch (action)
        {
            case UIAction.OpenBag:
                page = PlayerInfoPage.Bag;
                return true;
            case UIAction.OpenSouvenirBag:
                page = PlayerInfoPage.SouvenirBag;
                return true;
            case UIAction.OpenAchievement:
                page = PlayerInfoPage.Achievement;
                return true;
            case UIAction.OpenBook:
                page = PlayerInfoPage.Book;
                return true;
            case UIAction.OpenNews:
                page = PlayerInfoPage.News;
                return true;
            case UIAction.OpenContract:
                page = PlayerInfoPage.Contract;
                return true;
            case UIAction.OpenSouvenirShop:
                page = PlayerInfoPage.SouvenirShop;
                return true;
            default:
                page = PlayerInfoPage.None;
                return false;
        }
    }
}
