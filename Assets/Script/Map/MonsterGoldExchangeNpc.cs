using System.Threading.Tasks;
using GameSystem;
using Player;
using UnityEngine;

public class MonsterGoldExchangeNpc : MonoBehaviour, IInteractable
{
    [SerializeField] private MonsterGoldExchangeView view;
    [SerializeField] private GameObject prompt;
    [SerializeField] private bool saveAfterExchange = true;

    private bool isOpen;

    private void Awake()
    {
        if (view == null)
            view = FindObjectOfType<MonsterGoldExchangeView>(true);
    }

    private void OnEnable()
    {
        if (view != null)
        {
            view.OnCloseRequested += ClosePanel;
            view.OnExchangeConfirmed += ConfirmExchange;
        }
    }

    private void OnDisable()
    {
        if (view != null)
        {
            view.OnCloseRequested -= ClosePanel;
            view.OnExchangeConfirmed -= ConfirmExchange;
        }

        UnlockPlayer();
    }

    public void Interact()
    {
        if (isOpen)
        {
            ClosePanel();
            return;
        }

        OpenPanel();
    }

    public void ShowPrompt()
    {
        if (prompt != null) prompt.SetActive(true);
    }

    public void HidePrompt()
    {
        if (prompt != null) prompt.SetActive(false);
    }

    private void OpenPanel()
    {
        if (view == null)
            return;

        isOpen = true;
        LockPlayer();
        RefreshView();
    }

    private void ClosePanel()
    {
        if (view != null)
            view.Close();

        isOpen = false;
        UnlockPlayer();
    }

    private async void ConfirmExchange()
    {
        if (DataManager.Instance == null)
            return;

        bool exchanged = DataManager.Instance.ExchangeAllMonsterGoldToGold(out _, out _);
        if (!exchanged)
        {
            RefreshView();
            return;
        }

        if (saveAfterExchange)
            await SaveGameAsync();

        RefreshView();
    }

    private void RefreshView()
    {
        IReadOnlyPlayerData playerData = DataManager.Instance?.CurrentPlayerData;
        int monsterGold = playerData?.MonsterGold ?? 0;
        int exchangeGold = CalculateExchangeGold(monsterGold);
        view.Open(monsterGold, exchangeGold);
    }

    private static int CalculateExchangeGold(int monsterGold)
    {
        if (monsterGold <= 0)
            return 0;

        long calculatedGold = ((long)monsterGold * 3 + 3) / 4;
        return calculatedGold > int.MaxValue ? int.MaxValue : (int)calculatedGold;
    }

    private static async Task SaveGameAsync()
    {
        if (GameManager.Instance?.gameFlow != null)
            await GameManager.Instance.gameFlow.SaveGameAsync();
    }

    private static void LockPlayer()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.LockPlayerMove(PlayerLockSources.MonsterGoldExchange);
    }

    private static void UnlockPlayer()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.UnlockPlayerMove(PlayerLockSources.MonsterGoldExchange);
    }
}
