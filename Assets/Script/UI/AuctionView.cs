using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// =============================================================================
// AuctionView：拍賣會 UI 顯示層 (View)
// -----------------------------------------------------------------------------
// 職責：
//   1. 純粹負責「畫面顯示」與「按鈕事件轉發」，不處理任何拍賣邏輯（出價規則、
//      AI 行為、倒數計時邏輯等）皆由外部的 Controller / Manager 處理。
//   2. 對外提供以下幾類 API：
//        (a) 顯示開關：SetVisible
//        (b) 設定按鈕：ConfigureBidButtons / SetBidButtonStates
//        (c) 一次性刷新整個 UI 狀態：RefreshState
//        (d) 主持人台詞：ShowStart / ShowBid / ShowFinalCall / ShowNoMoney /
//                       ShowAlreadyHighestBidder
//        (e) 角色頭頂出價泡泡：ShowBidBubble / HideAllBidBubbles
//        (f) 倒數計時更新：SetTimerSeconds
//
// 使用流程（Controller 端典型寫法）：
//   var view = auctionView; // 場景內掛上此元件的物件
//   view.SetVisible(true);  // 顯示拍賣面板
//   view.ConfigureBidButtons(new[] { 10, 50, 100 }, amount => OnPlayerBid(amount));
//   view.ShowStart(startingPrice: 100);
//   // 每次狀態更動時：
//   view.RefreshState(currentPrice, currentBidderName, secondsLeft,
//                     playerBudget, participants, bidAmounts, bidButtonStates);
//   // NPC 出價時：
//   view.ShowBid(npcId, npcName, bidAmount, isPlayer: false);
//   // 結束三聲喊價：
//   view.ShowFinalCall(callIndex: 1, currentPrice);
//   // 結束後：
//   view.SetVisible(false);
// =============================================================================
public class AuctionView : MonoBehaviour
{
    // ---- 主面板與基本資訊文字欄位 ----------------------------------------
    [Header("Panel")]
    [SerializeField]
    private GameObject auctionPanel;            // 拍賣 UI 的根物件，控制顯示／隱藏

    [SerializeField]
    private TextMeshProUGUI hostText;           // 主持人台詞區（開場、喊價、終局等訊息）

    [SerializeField]
    private TextMeshProUGUI currentPriceText;   // 顯示「目前最高價」

    [SerializeField]
    private TextMeshProUGUI currentBidderText;  // 顯示「目前出最高價的人」

    [SerializeField]
    private TextMeshProUGUI timerText;          // 顯示倒數秒數

    [SerializeField]
    private TextMeshProUGUI playerBudgetText;   // 顯示玩家剩餘金幣（預算）
    // ---- 出價按鈕陣列 ----------------------------------------------------
    [Header("Bid Buttons")]
    [SerializeField]
    private Button[] bidButtons;                // 加價按鈕（例如 +10、+50、+100）

    // ---- 角色頭頂出價泡泡 -----------------------------------------------
    [Header("Bid Bubbles")]
    [SerializeField]
    private List<AuctionBidBubbleBinding> bidBubbles = new(); // 各參與者頭頂泡泡綁定

    // ---- 出價者顯示名稱 -------------------------------------------------
    [Header("Bidder Names")]
    [SerializeField]
    private string playerBidderName = "主角";    // 玩家顯示名稱（預設「主角」）

    [SerializeField]
    private string mysteryBidderName = "神秘人"; // 神秘人顯示名稱（預設「神秘人」）

    // ---- 文字格式（皆使用 string.Format 以便外部翻譯）-------------------
    [Header("Text")]
    [SerializeField]
    private string startTextFormat = "拍賣會開始，起標價 {0} 金幣。"; // 拍賣會開始，起標價 {0} 金幣。

    [SerializeField]
    private string currentPriceFormat = "目前價格：{0}";    // 目前價格：{0}

    [SerializeField]
    private string currentBidderFormat = "最高出價：{0}";   // 最高出價：{0}

    [SerializeField]
    private string timerFormat = "倒數：{0}";                       // 倒數：{0}

    [SerializeField]
    private string playerBudgetFormat = "你的預算：{0}";    // 你的預算：{0}

    [SerializeField]
    private string participantsFormat = "參與者：{0}";          // 參與者：{0}

    [SerializeField]
    private string playerBidTextFormat = "主角出價 {0} 金幣。"; // 主角出價 {0} 金幣。

    [SerializeField]
    private string npcBidTextFormat = "{0} 出價 {1} 金幣。"; // {0} 出價 {1} 金幣。

    [SerializeField]
    private string finalCallOneTextFormat = "目前價格 {0} 金幣，一次。"; // 目前價格 {0} 金幣，一次。

    [SerializeField]
    private string finalCallTwoTextFormat = "目前價格 {0} 金幣，兩次。"; // 目前價格 {0} 金幣，兩次。

    [SerializeField]
    private string finalCallThreeTextFormat = "目前價格 {0} 金幣，三次，交易成立。"; // 目前價格 {0} 金幣，三次，交易成立。

    [SerializeField]
    private string noMoneyText = "金幣不足，無法出價。"; // 金幣不足，無法出價。

    [SerializeField]
    private string alreadyHighestBidderText = "目前最高出價已經是你。"; // 目前最高出價已經是你。

    [SerializeField]
    private string bidBubbleFormat = "{0}\n{1}"; // 泡泡顯示格式：第一行名稱、第二行金額

    // 紀錄已綁定到按鈕的 UnityAction，OnDestroy / 重新設定時可以正確解除監聽，
    // 避免 lambda closure 殘留導致記憶體或邏輯洩漏。
    private readonly List<ButtonBinding> buttonBindings = new();

    // 對外公開的玩家／神秘人名稱（含空字串保護）
    public string PlayerBidderName => string.IsNullOrWhiteSpace(playerBidderName) ? "主角" : playerBidderName;

    public string MysteryBidderName => string.IsNullOrWhiteSpace(mysteryBidderName) ? "神秘人" : mysteryBidderName;

    // -------------------------------------------------------------------------
    // Unity 生命週期
    // -------------------------------------------------------------------------

    /// <summary>
    /// Awake：場景載入時先把面板與所有泡泡藏起來，避免一進場就閃出 UI。
    /// </summary>
    private void Awake()
    {
        SetVisible(false);
        HideAllBidBubbles();
    }

    /// <summary>
    /// OnDestroy：清掉所有按鈕監聽，避免在物件銷毀後仍被外部 Action 持有 reference。
    /// </summary>
    private void OnDestroy()
    {
        ClearBidButtonActions();
    }

    // -------------------------------------------------------------------------
    // 公開 API：顯示控制
    // -------------------------------------------------------------------------

    /// <summary>
    /// 顯示／隱藏整個拍賣面板。隱藏時會一併把所有頭頂泡泡關閉。
    /// </summary>
    /// <param name="visible">true = 顯示；false = 隱藏</param>
    public void SetVisible(bool visible)
    {
        if (auctionPanel != null)
            auctionPanel.SetActive(visible);

        if (!visible)
            HideAllBidBubbles();
    }

    // -------------------------------------------------------------------------
    // 公開 API：按鈕設定
    // -------------------------------------------------------------------------

    /// <summary>
    /// 設定加價按鈕的金額與點擊回呼。會先清掉舊的監聽，再依序為每個 button 綁上對應金額。
    /// 若 Inspector 中的按鈕數量比 bidAmounts 多，會自動使用最後一筆金額遞補。
    /// </summary>
    /// <param name="bidAmounts">每個按鈕對應的加價金額（依索引一一對應）</param>
    /// <param name="onBidClicked">玩家點擊任一按鈕時的回呼，參數為該按鈕對應金額</param>
    public void ConfigureBidButtons(IReadOnlyList<int> bidAmounts, Action<int> onBidClicked)
    {
        ClearBidButtonActions();

        if (bidButtons == null)
            return;

        for (int i = 0; i < bidButtons.Length; i++)
        {
            Button button = bidButtons[i];
            if (button == null)
                continue;

            int amount = GetBidAmount(bidAmounts, i);
            // 用 closure 把當前金額包起來，避免迴圈變數被外部捕捉到最後一個值
            UnityAction action = () => onBidClicked?.Invoke(amount);
            button.onClick.AddListener(action);
            buttonBindings.Add(new ButtonBinding(button, action));
            SetButtonLabel(button, amount);
        }
    }

    /// <summary>
    /// 只更新按鈕的「金額顯示」與「是否可點」，不會重新綁定 click 事件。
    /// 通常在 RefreshState 中呼叫，以便依玩家當前金幣動態鎖定／開啟按鈕。
    /// </summary>
    public void SetBidButtonStates(IReadOnlyList<int> bidAmounts, IReadOnlyList<bool> interactableStates)
    {
        if (bidButtons == null)
            return;

        for (int i = 0; i < bidButtons.Length; i++)
        {
            Button button = bidButtons[i];
            if (button == null)
                continue;

            SetButtonLabel(button, GetBidAmount(bidAmounts, i));
            button.interactable = interactableStates != null
                && i < interactableStates.Count
                && interactableStates[i];
        }
    }

    // -------------------------------------------------------------------------
    // 公開 API：一次性刷新整個 UI
    // -------------------------------------------------------------------------

    /// <summary>
    /// 一次刷新所有資訊欄位（價格、最高出價者、倒數、預算、參與者、按鈕狀態）。
    /// Controller 在每個 Tick / 每次出價變動時呼叫即可。
    /// </summary>
    /// <param name="currentPrice">目前最高價</param>
    /// <param name="currentBidderName">目前最高出價者顯示名稱（空字串會顯示為「無」）</param>
    /// <param name="seconds">倒數剩餘秒數</param>
    /// <param name="playerBudget">玩家可用金幣</param>
    /// <param name="participants">本場參與者名稱清單</param>
    /// <param name="bidAmounts">各按鈕的加價金額</param>
    /// <param name="bidButtonStates">各按鈕是否可點（與 bidAmounts 一一對應）</param>
    public void RefreshState(
        int currentPrice,
        string currentBidderName,
        int seconds,
        int playerBudget,
        IReadOnlyList<string> participants,
        IReadOnlyList<int> bidAmounts,
        IReadOnlyList<bool> bidButtonStates)
    {
        SetCurrentPrice(string.Format(currentPriceFormat, currentPrice));
        SetCurrentBidder(string.Format(currentBidderFormat, string.IsNullOrWhiteSpace(currentBidderName) ? "無" : currentBidderName));
        SetTimerSeconds(seconds);
        SetPlayerBudget(string.Format(playerBudgetFormat, playerBudget));
        SetBidButtonStates(bidAmounts, bidButtonStates);
    }

    // -------------------------------------------------------------------------
    // 公開 API：主持人台詞
    // -------------------------------------------------------------------------

    /// <summary>顯示「拍賣會開始，起標價 X 金幣」。</summary>
    public void ShowStart(int startingPrice)
    {
        SetHostText(string.Format(startTextFormat, startingPrice));
    }

    /// <summary>
    /// 顯示出價訊息，並在出價者頭頂彈出泡泡。
    /// </summary>
    /// <param name="bidderId">出價者唯一 ID（用於對應 bidBubbles 內的 BidderId）</param>
    /// <param name="bidderName">出價者顯示名稱</param>
    /// <param name="bidAmount">本次成交金額</param>
    /// <param name="isPlayer">true = 玩家本人；false = NPC</param>
    public void ShowBid(string bidderId, string bidderName, int bidAmount, bool isPlayer)
    {
        string text = isPlayer
            ? string.Format(playerBidTextFormat, bidAmount)
            : string.Format(npcBidTextFormat, bidderName, bidAmount);
        SetHostText(text);
        ShowBidBubble(bidderId, bidderName, bidAmount);
    }

    /// <summary>
    /// 終局喊價（一次／兩次／三次）。callIndex 1 代表第一聲，2 代表第二聲，
    /// 其他值（一般傳 3）代表第三聲並宣告交易成立。
    /// </summary>
    public void ShowFinalCall(int callIndex, int currentPrice)
    {
        string format = callIndex switch
        {
            1 => finalCallOneTextFormat,
            2 => finalCallTwoTextFormat,
            _ => finalCallThreeTextFormat
        };
        SetHostText(string.Format(format, currentPrice));
    }

    /// <summary>
    /// 玩家金幣不足時呼叫。除了顯示主持人台詞外，也會透過 SystemInfoEvent 跳出全局提示。
    /// </summary>
    public void ShowNoMoney()
    {
        SetHostText(noMoneyText);
        SystemInfoEvent.Show(noMoneyText);
    }

    /// <summary>玩家想再出價但自己已是最高價時的提示。</summary>
    public void ShowAlreadyHighestBidder()
    {
        SetHostText(alreadyHighestBidderText);
    }

    /// <summary>單獨更新倒數秒數欄位（不影響其他 UI）。</summary>
    public void SetTimerSeconds(int seconds)
    {
        SetTimer(string.Format(timerFormat, seconds));
    }

    // -------------------------------------------------------------------------
    // 內部：實際寫入 TMP 文字欄位
    // -------------------------------------------------------------------------

    private void SetHostText(string text)
    {
        if (hostText != null)
            hostText.text = text ?? string.Empty;
    }

    private void SetCurrentPrice(string text)
    {
        if (currentPriceText != null)
            currentPriceText.text = text ?? string.Empty;
    }

    private void SetCurrentBidder(string text)
    {
        if (currentBidderText != null)
            currentBidderText.text = text ?? string.Empty;
    }

    private void SetTimer(string text)
    {
        if (timerText != null)
            timerText.text = text ?? string.Empty;
    }

    private void SetPlayerBudget(string text)
    {
        if (playerBudgetText != null)
            playerBudgetText.text = text ?? string.Empty;
    }

    // -------------------------------------------------------------------------
    // 公開 API：頭頂出價泡泡
    // -------------------------------------------------------------------------

    /// <summary>
    /// 在指定出價者頭頂顯示出價泡泡，數秒後自動隱藏。
    /// 會優先以 bidderId 比對 bidBubbles 中的綁定，找不到時退回用 bidderName 比對。
    /// </summary>
    public void ShowBidBubble(string bidderId, string bidderName, int bidAmount)
    {
        AuctionBidBubbleBinding binding = FindBubbleBinding(bidderId, bidderName);
        if (binding == null)
            return;

        // 若尚未產生實體泡泡 GameObject，動態建立一份預設外觀的泡泡
        EnsureBubbleObjects(binding);
        if (binding.BubbleRoot == null || binding.BubbleText == null)
            return;

        binding.BubbleText.text = string.Format(bidBubbleFormat, bidderName, bidAmount);
        binding.BubbleRoot.SetActive(true);

        // 連續出價時：先停掉前一次的 hide 協程，重置顯示時間
        if (binding.HideRoutine != null)
            StopCoroutine(binding.HideRoutine);

        binding.HideRoutine = StartCoroutine(HideBubbleAfterDelay(binding));
    }

    /// <summary>關閉所有頭頂泡泡（包含未到期的）。</summary>
    public void HideAllBidBubbles()
    {
        if (bidBubbles == null)
            return;

        foreach (AuctionBidBubbleBinding binding in bidBubbles)
        {
            if (binding == null)
                continue;

            if (binding.HideRoutine != null)
            {
                StopCoroutine(binding.HideRoutine);
                binding.HideRoutine = null;
            }

            if (binding.BubbleRoot != null)
                binding.BubbleRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 等待 VisibleSeconds 秒後隱藏泡泡。最低保證 0.1 秒，避免 Inspector 設 0 造成立即消失。
    /// </summary>
    private IEnumerator HideBubbleAfterDelay(AuctionBidBubbleBinding binding)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, binding.VisibleSeconds));

        if (binding.BubbleRoot != null)
            binding.BubbleRoot.SetActive(false);

        binding.HideRoutine = null;
    }

    /// <summary>
    /// 從 bidBubbles 清單裡找出對應的泡泡綁定。
    /// 比對順序：BidderId 優先，找不到再用 DisplayName。
    /// </summary>
    private AuctionBidBubbleBinding FindBubbleBinding(string bidderId, string bidderName)
    {
        if (bidBubbles == null)
            return null;

        foreach (AuctionBidBubbleBinding binding in bidBubbles)
        {
            if (binding == null)
                continue;

            if (!string.IsNullOrEmpty(bidderId) && binding.BidderId == bidderId)
                return binding;

            if (!string.IsNullOrEmpty(bidderName) && binding.DisplayName == bidderName)
                return binding;
        }

        return null;
    }

    /// <summary>
    /// 若 binding 上尚未指派現成的 BubbleRoot / BubbleText，依照 HeadAnchor 動態建立
    /// 一個簡易的世界座標泡泡（黑底白字）。已存在則不重建。
    ///
    /// 通常美術會直接在場景中放好 BubbleRoot + BubbleText 並拖入 binding，
    /// 此方法是給沒設定的情境提供 fallback。
    /// </summary>
    private void EnsureBubbleObjects(AuctionBidBubbleBinding binding)
    {
        if (binding.BubbleRoot != null && binding.BubbleText != null)
            return;

        if (binding.HeadAnchor == null)
            return;

        // 1) 建立泡泡根節點（World Space Canvas）
        GameObject root = new GameObject("AuctionBidBubble", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        root.transform.SetParent(binding.HeadAnchor, false);
        root.transform.localPosition = binding.LocalOffset;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(180f, 72f);
        rootRect.localScale = Vector3.one * 0.01f; // World space canvas 必須縮小才合理

        // 2) 建立黑底背景
        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image image = background.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        // 3) 建立白色文字
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.color = Color.white;

        // 4) 寫回 binding，供下一次重複使用
        binding.BubbleRoot = root;
        binding.BubbleText = text;
        root.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // 內部：按鈕 / 工具函式
    // -------------------------------------------------------------------------

    /// <summary>
    /// 解除所有先前綁定的按鈕監聽，防止重複註冊或在物件銷毀後仍持有 reference。
    /// </summary>
    private void ClearBidButtonActions()
    {
        foreach (ButtonBinding binding in buttonBindings)
        {
            if (binding.Button != null && binding.Action != null)
                binding.Button.onClick.RemoveListener(binding.Action);
        }

        buttonBindings.Clear();
    }

    /// <summary>
    /// 取得指定索引的加價金額；當 bidAmounts 為空時回傳 0，
    /// 索引超出範圍時回傳最後一個值（讓多餘的按鈕沿用最高加價）。
    /// </summary>
    private static int GetBidAmount(IReadOnlyList<int> bidAmounts, int index)
    {
        if (bidAmounts == null || bidAmounts.Count == 0)
            return 0;

        if (index < 0 || index >= bidAmounts.Count)
            return bidAmounts[bidAmounts.Count - 1];

        return bidAmounts[index];
    }

    /// <summary>把按鈕底下第一個 TMP 文字設為「+金額」。</summary>
    private static void SetButtonLabel(Button button, int amount)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = $"+{amount}";
    }

    /// <summary>
    /// 將 Button 與其註冊的 UnityAction 綁成一組，方便在 ClearBidButtonActions 中正確
    /// 對應地 RemoveListener（lambda 必須留住同一個 reference 才能解除）。
    /// </summary>
    private readonly struct ButtonBinding
    {
        public readonly Button Button;
        public readonly UnityAction Action;

        public ButtonBinding(Button button, UnityAction action)
        {
            Button = button;
            Action = action;
        }
    }
}

// =============================================================================
// AuctionBidBubbleBinding：單一參與者的頭頂泡泡綁定資料（在 Inspector 拖設定）
// -----------------------------------------------------------------------------
// 使用方式：
//   - BidderId / DisplayName 至少擇一填寫，作為 ShowBidBubble 時的對應 key。
//   - HeadAnchor 指向場景中的角色頭頂節點，泡泡會生成在此之下。
//   - BubbleRoot / BubbleText 若預先擺好可直接使用；留空則由 EnsureBubbleObjects
//     依 HeadAnchor 自動產生。
//   - LocalOffset 為相對 HeadAnchor 的位移。
//   - VisibleSeconds 為泡泡顯示時間（秒）。
// =============================================================================
[Serializable]
public class AuctionBidBubbleBinding
{
    public string BidderId;                                    // 出價者唯一識別字串（推薦填）
    public string DisplayName;                                 // 顯示名稱，可作為備援比對 key
    public Transform HeadAnchor;                               // 角色頭頂節點，泡泡生成位置
    public GameObject BubbleRoot;                              // 泡泡根 GameObject（可預先擺）
    public TextMeshProUGUI BubbleText;                         // 泡泡內的 TMP 文字元件
    public Vector3 LocalOffset = new Vector3(0f, 1.2f, 0f);    // 相對 HeadAnchor 的位移
    public float VisibleSeconds = 1.5f;                        // 顯示秒數（最低 0.1 秒）

    [NonSerialized]
    public Coroutine HideRoutine;                              // 目前進行中的隱藏協程（runtime only）
}
