using System;

public static class PlayerInfoUIEvents
{
    public static event Action OnOpenBag;
    public static event Action OnOpenSouvenirBag;
    public static event Action OnOpenAchievement;
    public static event Action OnOpenBook;
    public static event Action OnOpenNews;
    public static event Action OnOpenContract;
    public static event Action OnOpenSouvenirShop;
    public static event Action OnCloseAll;
    public static event Action<PlayerInfoPage> OnPageChanged;

    public static PlayerInfoPage ActivePage { get; private set; } = PlayerInfoPage.None;

    public static void InvokeOpenBag()          => OnOpenBag?.Invoke();
    public static void InvokeOpenSouvenirBag()  => OnOpenSouvenirBag?.Invoke();
    public static void InvokeOpenAchievement()  => OnOpenAchievement?.Invoke();
    public static void InvokeOpenBook()         => OnOpenBook?.Invoke();
    public static void InvokeOpenNews()         => OnOpenNews?.Invoke();
    public static void InvokeOpenContract()     => OnOpenContract?.Invoke();
    public static void InvokeOpenSouvenirShop() => OnOpenSouvenirShop?.Invoke();
    public static void InvokeCloseAll()         => OnCloseAll?.Invoke();

    public static void SetActivePage(PlayerInfoPage page)
    {
        if (ActivePage == page)
            return;

        ActivePage = page;
        OnPageChanged?.Invoke(page);
    }
}
