using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameSystem;
using UnityEngine;

public class AuctionController : MonoBehaviour
{
    private const int TenThousand = 10000;
    private const string FallbackPlayerBidderName = "Player";
    private const string FallbackMysteryBidderName = "Mystery";

    private static class BidderIds
    {
        public const string Player = "Player";
        public const string Ghost = "Ghost";
        public const string Beast = "Beast";
        public const string Divine = "Divine";
        public const string Fairy = "Fairy";
        public const string Mystery = "Mystery";

        public static string FromRace(CollectionMissionRace race)
        {
            return race switch
            {
                CollectionMissionRace.Ghost => Ghost,
                CollectionMissionRace.Beast => Beast,
                CollectionMissionRace.Divine => Divine,
                CollectionMissionRace.Fairy => Fairy,
                _ => race.ToString()
            };
        }
    }

    [Header("Auction Settings")]
    [SerializeField]
    private bool startOnEnable;

    [SerializeField]
    private int startingPrice = EndingConditionDetector.RequiredAuctionGold;

    [SerializeField]
    private float roundSeconds = 10f;

    [SerializeField]
    private Vector2 npcBidIntervalRange = new Vector2(3f, 5f);

    [SerializeField]
    private int[] bidAmounts = { 50000, 10000, 5000, 1000, 500, 100 };

    [Header("View")]
    [SerializeField]
    private AuctionView auctionView;

    private readonly List<AuctionBidder> bidders = new();
    private AuctionBidder playerBidder;
    private AuctionBidder currentBidder;
    private Coroutine timerCoroutine;
    private Coroutine npcBidCoroutine;
    private int currentPrice;
    private float remainingTime;
    private bool auctionActive;
    private bool isResolving;
    private bool npcBiddingClosed;
    private int lastShownSecond = -1;

    private void Awake()
    {
        ResolveAuctionView();
        ConfigureView();
        auctionView?.SetVisible(false);
    }

    private void OnEnable()
    {
        if (startOnEnable)
            StartAuction();
    }

    private void OnDisable()
    {
        StopAuctionCoroutines();
        UnlockAuction();
    }

    public void StartAuction()
    {
        if (auctionActive || isResolving)
            return;

        ResolveAuctionView();
        ConfigureView();
        BuildBidders();

        currentPrice = Mathf.Max(0, startingPrice);
        currentBidder = null;
        remainingTime = Mathf.Max(1f, roundSeconds);
        auctionActive = true;
        lastShownSecond = -1;
        npcBiddingClosed = false;

        LockAuction();
        auctionView?.SetVisible(true);
        auctionView?.EnsureBidderNpcsSpawned();
        auctionView?.ApplyParticipants(bidders.Select(b => b.BidderId));
        auctionView?.HideAllBidBubbles();
        auctionView?.ShowStart(currentPrice);
        RefreshAll();

        timerCoroutine = StartCoroutine(TimerRoutine());
        npcBidCoroutine = StartCoroutine(NpcBidRoutine());
    }

    public void StopAuction()
    {
        auctionActive = false;
        npcBiddingClosed = false;
        StopAuctionCoroutines();
        UnlockAuction();
        auctionView?.SetVisible(false);
    }

    private void BuildBidders()
    {
        bidders.Clear();

        int playerBudget = DataManager.Instance?.CurrentPlayerData?.Gold ?? 0;
        string playerName = ResolveBidderName(BidderIds.Player, FallbackPlayerBidderName);
        playerBidder = new AuctionBidder(BidderIds.Player, playerName, true, null, playerBudget, playerBudget);
        bidders.Add(playerBidder);

        CollectionMissionSaveData progress = LoadCollectionMissionProgress();
        AddRaceBidderIfEligible(progress, CollectionMissionRace.Ghost, 105 * TenThousand, 110 * TenThousand);
        AddRaceBidderIfEligible(progress, CollectionMissionRace.Beast, 101 * TenThousand, 120 * TenThousand);
        AddRaceBidderIfEligible(progress, CollectionMissionRace.Divine, 115 * TenThousand, 120 * TenThousand);
        AddRaceBidderIfEligible(progress, CollectionMissionRace.Fairy, 100 * TenThousand, 130 * TenThousand);

        string mysteryName = ResolveBidderName(BidderIds.Mystery, FallbackMysteryBidderName);
        AddNpcBidder(BidderIds.Mystery, mysteryName, null, 100 * TenThousand, 120 * TenThousand);
    }

    private void AddRaceBidderIfEligible(CollectionMissionSaveData progress, CollectionMissionRace race, int minBudget, int maxBudget)
    {
        if (HasReachedCollectionGoal(progress, race))
            return;

        string raceName = CollectionMissionRaceUtility.GetRaceName(race);
        AddNpcBidder(BidderIds.FromRace(race), raceName, race, minBudget, maxBudget);
    }

    private void AddNpcBidder(string bidderId, string bidderName, CollectionMissionRace? race, int minBudget, int maxBudget)
    {
        int low = Mathf.Min(minBudget, maxBudget);
        int high = Mathf.Max(minBudget, maxBudget);
        int budget = UnityEngine.Random.Range(low, high + 1);
        bidders.Add(new AuctionBidder(bidderId, bidderName, false, race, low, budget));
    }

    private static bool HasReachedCollectionGoal(CollectionMissionSaveData progress, CollectionMissionRace race)
    {
        if (progress?.RaceProgress == null)
            return false;

        string raceName = CollectionMissionRaceUtility.GetRaceName(race);
        CollectionMissionRaceProgress raceProgress = progress.RaceProgress
            .FirstOrDefault(item => item != null && item.Race == raceName);
        return raceProgress != null && raceProgress.Points >= CollectionMissionTracker.MaxRewardPoints;
    }

    private static CollectionMissionSaveData LoadCollectionMissionProgress()
    {
        if (DataManager.Instance == null)
            return new CollectionMissionSaveData();

        CollectionMissionSaveData progress =
            DataManager.Instance.GetPersistentSaveData<CollectionMissionSaveData>(CollectionMissionTracker.SaveKey);
        progress.RaceProgress ??= new List<CollectionMissionRaceProgress>();
        return progress;
    }

    private IEnumerator TimerRoutine()
    {
        while (auctionActive)
        {
            remainingTime -= Time.deltaTime;
            RefreshTimer();

            if (remainingTime <= 0f)
            {
                CompleteAuctionAsync();
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator NpcBidRoutine()
    {
        while (auctionActive && !npcBiddingClosed)
        {
            float minInterval = Mathf.Min(npcBidIntervalRange.x, npcBidIntervalRange.y);
            float maxInterval = Mathf.Max(npcBidIntervalRange.x, npcBidIntervalRange.y);
            yield return new WaitForSeconds(UnityEngine.Random.Range(minInterval, maxInterval));

            if (!auctionActive || npcBiddingClosed)
                yield break;

            TryNpcBid();
        }
    }

    private void TryNpcBid()
    {
        if (ShouldCloseNpcBidding())
        {
            npcBiddingClosed = true;
            return;
        }

        List<AuctionBidder> candidates = bidders
            .Where(bidder => bidder != null
                && !bidder.IsPlayer
                && bidder != currentBidder
                && CanBid(bidder))
            .ToList();

        if (candidates.Count == 0)
            return;

        AuctionBidder bidder = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        int bidAmount = GetRandomAffordableBidAmount(bidder);
        if (bidAmount <= currentPrice)
            return;

        PlaceBid(bidder, bidAmount);
    }

    private int GetRandomAffordableBidAmount(AuctionBidder bidder)
    {
        List<int> affordableIncrements = bidAmounts
            .Where(amount => amount > 0 && currentPrice + amount <= bidder.Budget)
            .ToList();

        if (affordableIncrements.Count == 0)
            return currentPrice;

        int increment = affordableIncrements[UnityEngine.Random.Range(0, affordableIncrements.Count)];
        return currentPrice + increment;
    }

    private bool CanBid(AuctionBidder bidder)
    {
        if (bidder == null)
            return false;

        return bidAmounts.Any(amount => amount > 0 && currentPrice + amount <= bidder.Budget);
    }

    private void HandlePlayerBid(int amount)
    {
        if (!auctionActive || playerBidder == null)
            return;

        if (currentBidder == playerBidder)
        {
            auctionView?.ShowAlreadyHighestBidder();
            return;
        }

        int bidAmount = currentPrice + Mathf.Max(0, amount);
        if (bidAmount > playerBidder.Budget)
        {
            auctionView?.ShowNoMoney();
            return;
        }

        PlaceBid(playerBidder, bidAmount);
    }

    private void PlaceBid(AuctionBidder bidder, int bidAmount)
    {
        currentBidder = bidder;
        currentPrice = bidAmount;
        remainingTime = Mathf.Max(1f, roundSeconds);
        lastShownSecond = -1;

        RefreshAll();
        auctionView?.ShowBid(bidder.BidderId, bidder.DisplayName, currentPrice, bidder.IsPlayer);

        if (!bidder.IsPlayer && ShouldCloseNpcBidding())
        {
            npcBiddingClosed = true;
            auctionView?.ShowPlayerOutbidLimit(currentPrice);
        }
    }

    private async void CompleteAuctionAsync()
    {
        if (isResolving)
            return;

        isResolving = true;
        auctionActive = false;
        StopAuctionCoroutines();

        auctionView?.ShowFinalCall(3, currentPrice);
        RefreshBidButtons();

        bool playerWon = currentBidder != null && currentBidder.IsPlayer;
        if (playerWon)
            DataManager.Instance?.ModifyGold(-currentPrice);

        DataManager.Instance?.SetEndingReached(playerWon ? EndingType.Type5 : EndingType.Type4);
        await SaveGameAsync();

        UnlockAuction();
        auctionView?.SetVisible(false);

        SceneTransitionManager sceneManager = GameManager.Instance?.SceneManager;
        if (sceneManager != null)
            sceneManager.GoToEndStoryScene();
    }

    private void RefreshAll()
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
        int budget = playerBidder != null ? playerBidder.Budget : DataManager.Instance?.CurrentPlayerData?.Gold ?? 0;
        List<string> participantNames = bidders.Select(bidder => bidder.DisplayName).ToList();

        auctionView?.RefreshState(
            currentPrice,
            currentBidder?.BidderId,
            seconds,
            budget,
            participantNames,
            bidAmounts,
            BuildBidButtonStates());
    }

    private void RefreshTimer()
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
        auctionView?.SetTimerSeconds(seconds);

        if (seconds == lastShownSecond)
            return;

        lastShownSecond = seconds;
        if (seconds == 3)
            auctionView?.ShowFinalCall(1, currentPrice);
        else if (seconds == 2)
            auctionView?.ShowFinalCall(2, currentPrice);
        else if (seconds == 1)
            auctionView?.ShowFinalCall(3, currentPrice);
    }

    private bool ShouldCloseNpcBidding()
    {
        return currentBidder != null
            && !currentBidder.IsPlayer
            && playerBidder != null
            && currentPrice >= playerBidder.Budget;
    }

    private void RefreshBidButtons()
    {
        auctionView?.SetBidButtonStates(bidAmounts, BuildBidButtonStates());
    }

    private int GetBidAmount(int index)
    {
        if (bidAmounts == null || bidAmounts.Length == 0)
            return 0;

        if (index < 0 || index >= bidAmounts.Length)
            return bidAmounts[bidAmounts.Length - 1];

        return bidAmounts[index];
    }

    private bool[] BuildBidButtonStates()
    {
        int buttonCount = bidAmounts != null ? bidAmounts.Length : 0;
        bool[] states = new bool[Mathf.Max(0, buttonCount)];

        for (int i = 0; i < states.Length; i++)
        {
            int amount = GetBidAmount(i);
            states[i] = auctionActive
                && playerBidder != null
                && currentBidder != playerBidder
                && currentPrice + amount <= playerBidder.Budget;
        }

        return states;
    }

    private void ConfigureView()
    {
        auctionView?.ConfigureBidButtons(bidAmounts, HandlePlayerBid);
    }

    private AuctionView ResolveAuctionView()
    {
        if (auctionView != null)
            return auctionView;

        auctionView = FindObjectOfType<AuctionView>(true);
        return auctionView;
    }

    private string ResolveBidderName(string bidderId, string fallback)
    {
        if (auctionView == null)
            return fallback;

        string name = auctionView.GetBidderDisplayName(bidderId);
        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }

    private void StopAuctionCoroutines()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (npcBidCoroutine != null)
        {
            StopCoroutine(npcBidCoroutine);
            npcBidCoroutine = null;
        }
    }

    private void LockAuction()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.LockPlayerMove(PlayerLockSources.Auction);
        manager.LockPlayerInteract(PlayerLockSources.Auction);
    }

    private void UnlockAuction()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.UnlockPlayerMove(PlayerLockSources.Auction);
        manager.UnlockPlayerInteract(PlayerLockSources.Auction);
    }

    private static async System.Threading.Tasks.Task SaveGameAsync()
    {
        if (GameManager.Instance?.gameFlow != null)
            await GameManager.Instance.gameFlow.SaveGameAsync();
    }

    [Serializable]
    private class AuctionBidder
    {
        public string BidderId;
        public string DisplayName;
        public bool IsPlayer;
        public CollectionMissionRace? Race;
        public int MinimumBudget;
        public int Budget;

        public AuctionBidder(string bidderId, string displayName, bool isPlayer, CollectionMissionRace? race, int minimumBudget, int budget)
        {
            BidderId = bidderId;
            DisplayName = displayName;
            IsPlayer = isPlayer;
            Race = race;
            MinimumBudget = minimumBudget;
            Budget = budget;
        }
    }
}
