using UnityEngine;
using GameSystem;
public class PlayerInfoUI : MonoBehaviour
{
    [SerializeField] private PlayerView playerView;
    [SerializeField] private Souvenir.SouvenirBagView souvenirBagView;
    [SerializeField] private AchievementViewFactory achievementViewFactory;
    [SerializeField] private GameBookView gameBookView;
    [SerializeField] private EventsView eventsView;
    [SerializeField] private ContractView contractView;

    private enum Page { None, Bag, SouvenirBag, Achievement, Book, News, Contract }
    private Page _activePage = Page.None;

    private void OnEnable()
    {
        PlayerInfoUIEvents.OnOpenBag += HandleOpenBag;
        PlayerInfoUIEvents.OnOpenSouvenirBag += HandleOpenSouvenirBag;
        PlayerInfoUIEvents.OnOpenAchievement += HandleOpenAchievement;
        PlayerInfoUIEvents.OnOpenBook += HandleOpenBook;
        PlayerInfoUIEvents.OnOpenNews += HandleOpenNews;
        PlayerInfoUIEvents.OnOpenContract += HandleOpenContract;
        PlayerInfoUIEvents.OnCloseAll += HandleCloseAll;
    }

    private void OnDisable()
    {
        PlayerInfoUIEvents.OnOpenBag -= HandleOpenBag;
        PlayerInfoUIEvents.OnOpenSouvenirBag -= HandleOpenSouvenirBag;
        PlayerInfoUIEvents.OnOpenAchievement -= HandleOpenAchievement;
        PlayerInfoUIEvents.OnOpenBook -= HandleOpenBook;
        PlayerInfoUIEvents.OnOpenNews -= HandleOpenNews;
        PlayerInfoUIEvents.OnOpenContract -= HandleOpenContract;
        PlayerInfoUIEvents.OnCloseAll -= HandleCloseAll;
    }

    private void HandleOpenBag()
    {
        if (playerView == null || _activePage == Page.Bag) return;
        CloseCurrentPage();
        playerView.OpenBagView();
        _activePage = Page.Bag;
        SetPlayerFrozen(true);
    }

    private void HandleOpenSouvenirBag()
    {
        if (souvenirBagView == null || _activePage == Page.SouvenirBag) return;
        CloseCurrentPage();
        souvenirBagView.OpenBag();
        _activePage = Page.SouvenirBag;
        SetPlayerFrozen(true);
    }

    private void HandleOpenAchievement()
    {
        if (achievementViewFactory == null || _activePage == Page.Achievement) return;
        CloseCurrentPage();
        achievementViewFactory.OpenAndRefresh();
        _activePage = Page.Achievement;
        SetPlayerFrozen(true);
    }

    private void HandleOpenBook()
    {
        if (gameBookView == null || _activePage == Page.Book) return;
        CloseCurrentPage();
        gameBookView.OpenBook();
        _activePage = Page.Book;
        SetPlayerFrozen(true);
    }

    private void HandleOpenNews()
    {
        if (eventsView == null || _activePage == Page.News) return;
        CloseCurrentPage();
        eventsView.OpenNewsPanel();
        _activePage = Page.News;
        SetPlayerFrozen(true);
    }

    private void HandleOpenContract()
    {
        if (contractView == null || _activePage == Page.Contract) return;
        CloseCurrentPage();
        contractView.OpenContractPanel();
        _activePage = Page.Contract;
        SetPlayerFrozen(true);
    }

    private void HandleCloseAll()
    {
        if (_activePage == Page.None) return;
        CloseCurrentPage();
        SetPlayerFrozen(false);
    }

    private void CloseCurrentPage()
    {
        switch (_activePage)
        {
            case Page.Bag:         if (playerView != null) playerView.CloseBagView(); break;
            case Page.SouvenirBag: if (souvenirBagView != null) souvenirBagView.CloseBag(); break;
            case Page.Achievement: if (achievementViewFactory != null) achievementViewFactory.ClosePanel(); break;
            case Page.Book:        if (gameBookView != null) gameBookView.CloseBook(); break;
            case Page.News:        if (eventsView != null) eventsView.CloseNewsPanel(); break;
            case Page.Contract:    if (contractView != null) contractView.CloseContractPanel(); break;
        }
        _activePage = Page.None;
    }

    private void SetPlayerFrozen(bool frozen)
    {
        if (frozen)
        {
            GameManager.Instance.LockPlayerMove(PlayerLockSources.PlayerInfoUI);
            GameManager.Instance.LockPlayerInteract(PlayerLockSources.PlayerInfoUI);
        }
        else
        {
            GameManager.Instance.UnlockPlayerMove(PlayerLockSources.PlayerInfoUI);
            GameManager.Instance.UnlockPlayerInteract(PlayerLockSources.PlayerInfoUI);
        }
    }
}
