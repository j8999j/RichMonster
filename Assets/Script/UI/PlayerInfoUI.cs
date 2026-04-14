using UnityEngine;
using GameSystem;
public class PlayerInfoUI : MonoBehaviour
{
    [SerializeField] private PlayerView playerView;
    [SerializeField] private Souvenir.SouvenirBagView souvenirBagView;
    [SerializeField] private AchievementViewFactory achievementViewFactory;

    private enum Page { None, Bag, SouvenirBag, Achievement }
    private Page _activePage = Page.None;

    private void OnEnable()
    {
        PlayerInfoUIEvents.OnOpenBag += HandleOpenBag;
        PlayerInfoUIEvents.OnOpenSouvenirBag += HandleOpenSouvenirBag;
        PlayerInfoUIEvents.OnOpenAchievement += HandleOpenAchievement;
        PlayerInfoUIEvents.OnCloseAll += HandleCloseAll;
    }

    private void OnDisable()
    {
        PlayerInfoUIEvents.OnOpenBag -= HandleOpenBag;
        PlayerInfoUIEvents.OnOpenSouvenirBag -= HandleOpenSouvenirBag;
        PlayerInfoUIEvents.OnOpenAchievement -= HandleOpenAchievement;
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
        }
        _activePage = Page.None;
    }

    private void SetPlayerFrozen(bool frozen)
    {
        GameManager.Instance.SetPlayerMove(!frozen);
        GameManager.Instance.SetPlayerInteract(!frozen);
    }
}
