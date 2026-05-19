using UnityEngine;

public class PlayerInfoUIMenu : MonoBehaviour
{
    [SerializeField] private Souvenir.SouvenirBagView souvenirBagView;
    [SerializeField] private AchievementViewFactory achievementViewFactory;
    [SerializeField] private GameBookView gameBookView;
    [SerializeField] private SouvenirShopView souvenirShopView;
    [SerializeField] private PlayerInfoPage initialPage = PlayerInfoPage.None;
    [SerializeField] private bool closePagesOnStart = true;

    private PlayerInfoPage _activePage = PlayerInfoPage.None;

    private void OnEnable()
    {
        PlayerInfoUIEvents.OnOpenSouvenirBag += HandleOpenSouvenirBag;
        PlayerInfoUIEvents.OnOpenAchievement += HandleOpenAchievement;
        PlayerInfoUIEvents.OnOpenBook += HandleOpenBook;
        PlayerInfoUIEvents.OnOpenSouvenirShop += HandleOpenSouvenirShop;
        PlayerInfoUIEvents.OnCloseAll += HandleCloseAll;
        PlayerInfoUIEvents.SetActivePage(_activePage);
    }

    private void Start()
    {
        if (closePagesOnStart)
            CloseAllPages();

        if (initialPage != PlayerInfoPage.None)
            OpenPage(initialPage);
        else
            PlayerInfoUIEvents.SetActivePage(_activePage);
    }

    private void OnDisable()
    {
        PlayerInfoUIEvents.OnOpenSouvenirBag -= HandleOpenSouvenirBag;
        PlayerInfoUIEvents.OnOpenAchievement -= HandleOpenAchievement;
        PlayerInfoUIEvents.OnOpenBook -= HandleOpenBook;
        PlayerInfoUIEvents.OnOpenSouvenirShop -= HandleOpenSouvenirShop;
        PlayerInfoUIEvents.OnCloseAll -= HandleCloseAll;
    }

    private void HandleOpenSouvenirBag()
    {
        OpenPage(PlayerInfoPage.SouvenirBag);
    }

    private void HandleOpenAchievement()
    {
        OpenPage(PlayerInfoPage.Achievement);
    }

    private void HandleOpenBook()
    {
        OpenPage(PlayerInfoPage.Book);
    }

    private void HandleOpenSouvenirShop()
    {
        OpenPage(PlayerInfoPage.SouvenirShop);
    }

    private void HandleCloseAll()
    {
        if (_activePage == PlayerInfoPage.None)
            return;

        CloseCurrentPage();
        PlayerInfoUIEvents.SetActivePage(_activePage);
    }

    private void OpenPage(PlayerInfoPage page)
    {
        IPlayerInfoPage pageController = GetPageController(page);
        if (pageController == null || _activePage == page)
            return;

        CloseCurrentPage();
        pageController.OpenPage();
        _activePage = page;
        PlayerInfoUIEvents.SetActivePage(_activePage);
    }

    private void CloseCurrentPage()
    {
        IPlayerInfoPage pageController = GetPageController(_activePage);
        pageController?.ClosePage();
        _activePage = PlayerInfoPage.None;
    }

    private void CloseAllPages()
    {
        souvenirBagView?.ClosePage();
        achievementViewFactory?.ClosePage();
        gameBookView?.ClosePage();
        souvenirShopView?.ClosePage();
        _activePage = PlayerInfoPage.None;
    }

    private IPlayerInfoPage GetPageController(PlayerInfoPage page)
    {
        switch (page)
        {
            case PlayerInfoPage.SouvenirBag:
                return souvenirBagView;
            case PlayerInfoPage.Achievement:
                return achievementViewFactory;
            case PlayerInfoPage.Book:
                return gameBookView;
            case PlayerInfoPage.SouvenirShop:
                return souvenirShopView;
            default:
                return null;
        }
    }
}
