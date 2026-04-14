using System;

public static class PlayerInfoUIEvents
{
    public static event Action OnOpenBag;
    public static event Action OnOpenSouvenirBag;
    public static event Action OnOpenAchievement;
    public static event Action OnCloseAll;

    public static void InvokeOpenBag()          => OnOpenBag?.Invoke();
    public static void InvokeOpenSouvenirBag()  => OnOpenSouvenirBag?.Invoke();
    public static void InvokeOpenAchievement()  => OnOpenAchievement?.Invoke();
    public static void InvokeCloseAll()         => OnCloseAll?.Invoke();
}
