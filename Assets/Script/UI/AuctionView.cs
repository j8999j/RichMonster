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
    private TextMeshProUGUI timerText;          // 顯示倒數秒數

    [SerializeField]
    private TextMeshProUGUI playerBudgetText;   // 顯示玩家金幣（預算）
    // ---- 出價按鈕陣列 ----------------------------------------------------
    [Header("Bid Buttons")]
    [SerializeField]
    private Button[] bidButtons;                // 加價按鈕（例如 +10、+50、+100）

    // ---- 拍賣會 NPC 生成設定 ---------------------------------------------
    // 設計參考自 CollectionMissionTracker：以 prefab + 生成點的方式，在
    // 拍賣開場時把每位參與者 NPC 實體化到場上，每個 NPC 自帶 sprite 與對話框。
    [Header("Bidder NPCs")]
    [SerializeField]
    private AuctionBidderNpc bidderNpcPrefab;                 // 出價者 NPC 的 prefab（含 sprite + 對話框 + 對話文字）

    [SerializeField]
    private List<AuctionBidderSpawnInfo> bidderSpawnPoints = new(); // NPC 出價者一筆：ID／名稱／Sprite／生成點（不含 Player）

    [Header("Player Bubble / Marker")]
    [SerializeField]
    private AuctionPlayerSpawnInfo playerSpawnInfo;            // Player 不需要 AuctionBidderNpc，只需指定對話框與最高價標示停靠點

    [SerializeField]
    private GameObject bubblePrefab;                          // 對話框 prefab（含背景 + 一顆 TextMeshProUGUI），由 View 統一生成於每隻 NPC 的 BubbleAnchor 下

    [Header("Highest Bidder Marker")]
    [SerializeField]
    private GameObject highestBidderMarker;                   // 場景中預先擺好的單一「目前最高出價者」標示（UI 層）；View 依當前最高者把它搬到對應 HighestMarkerPoint

    [SerializeField]
    private bool logInvalidTransformReferences;

    private const string PlayerBidderId = "Player"; // 與 AuctionController.BidderIds.Player 對齊

    // 已生成的 NPC：以 BidderId 對應，供 ShowBidBubble 查找
    private readonly Dictionary<string, AuctionBidderNpc> spawnedBidderNpcs = new();

    // 已生成的對話框：與 spawnedBidderNpcs 同 key，由 View 直接管理顯示／隱藏
    private readonly Dictionary<string, BubbleEntry> spawnedBubbles = new();
    private bool bidderNpcsSpawned;

    private class BubbleEntry
    {
        public GameObject Root;
        public TextMeshProUGUI Text;
        public Coroutine HideRoutine;
    }

    // ---- 文字格式（皆使用 string.Format 以便外部翻譯）-------------------
    [Header("Text")]
    [SerializeField]
    private string startTextFormat = "拍賣會開始，起標價 {0} 元。"; // 拍賣會開始，起標價 {0} 元。

    [SerializeField]
    private string timerFormat = string.Empty;

    [SerializeField]
    private string participantsFormat = "參與者：{0}";          // 參與者：{0}

    [SerializeField]
    private string playerBidTextFormat = "主角出價 {0} 元。"; // 主角出價 {0} 元。

    [SerializeField]
    private string npcBidTextFormat = "{0} 出價 {1} 元。"; // {0} 出價 {1} 元。

    [SerializeField]
    private string finalCallOneTextFormat = "目前價格 {0} 元，還有沒有更高？一次。"; // 目前價格 {0} 元，還有沒有更高？一次。

    [SerializeField]
    private string finalCallTwoTextFormat = "最後機會，目前價格 {0} 元，兩次。"; // 最後機會，目前價格 {0} 元，兩次。

    [SerializeField]
    private string finalCallThreeTextFormat = "目前價格 {0} 元，三次，交易成立。"; // 目前價格 {0} 元，三次，交易成立。

    [SerializeField]
    private string noMoneyText = "金額不足，無法出價。"; // 金額不足，無法出價。

    [SerializeField]
    private string alreadyHighestBidderText = "目前最高出價已經是你。"; // 目前最高出價已經是你。

    [SerializeField]
    private string bidBubbleFormat = "{0}"; // 泡泡顯示格式：只顯示出價金額

    [SerializeField]
    private float bubbleVisibleSeconds = 1.5f;   // 所有 NPC 對話框統一顯示秒數

    // 紀錄已綁定到按鈕的 UnityAction，OnDestroy / 重新設定時可以正確解除監聽，
    // 避免 lambda closure 殘留導致記憶體或邏輯洩漏。
    private readonly List<ButtonBinding> buttonBindings = new();

    /// <summary>
    /// 依 BidderId 從 bidderSpawnPoints 取得顯示名稱（單一資料源）。
    /// 找不到時回傳空字串（呼叫端自行決定 fallback）。
    /// </summary>
    public string GetBidderDisplayName(string bidderId)
    {
        if (string.IsNullOrEmpty(bidderId) || bidderSpawnPoints == null)
            return string.Empty;

        foreach (AuctionBidderSpawnInfo info in bidderSpawnPoints)
        {
            if (info != null && info.BidderId == bidderId && !string.IsNullOrEmpty(info.DisplayName))
                return info.DisplayName;
        }

        return string.Empty;
    }

    // -------------------------------------------------------------------------
    // Unity 生命週期
    // -------------------------------------------------------------------------

    /// <summary>
    /// OnDestroy：清掉所有按鈕監聽，避免在物件銷毀後仍被外部 Action 持有 reference。
    /// </summary>
    private void OnDestroy()
    {
        ClearBidButtonActions();
    }

    /// <summary>
    /// 由 AuctionController 在 StartAuction 時呼叫，做為拍賣會開始時「檢測 + 生成」的單一進入點：
    ///   - 若已成功生成過 NPC（dict 非空），直接 return，避免重複 Destroy / Instantiate。
    ///   - 否則執行 SpawnBidderNpcs()，內部會列出所有失敗原因到 Console。
    /// </summary>
    public void EnsureBidderNpcsSpawned()
    {
        if (bidderNpcsSpawned && spawnedBidderNpcs.Count > 0)
            return;

        SpawnBidderNpcs();
    }

    // -------------------------------------------------------------------------
    // 公開 / 內部：拍賣會 NPC 生成
    // -------------------------------------------------------------------------

    /// <summary>
    /// 依照 bidderSpawnPoints 把每個出價者 NPC 從 prefab 生出來，
    /// 並寫入 BidderId / DisplayName / Sprite。可重複呼叫，重新生成前會先清掉舊的。
    /// </summary>
    public void SpawnBidderNpcs()
    {
        ClearSpawnedBidderNpcs();

        if (bidderNpcPrefab == null)
        {
            Debug.LogWarning("[AuctionView] bidderNpcPrefab 未指定，無法生成 NPC。", this);
            return;
        }

        if (bidderSpawnPoints == null || bidderSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[AuctionView] bidderSpawnPoints 為空，沒有任何 NPC 可生成。", this);
            return;
        }

        int spawnedCount = 0;
        for (int idx = 0; idx < bidderSpawnPoints.Count; idx++)
        {
            AuctionBidderSpawnInfo info = bidderSpawnPoints[idx];

            if (info == null)
            {
                if (logInvalidTransformReferences)
                    Debug.Log($"[AuctionView] bidderSpawnPoints[{idx}] = null，已跳過。", this);
                continue;
            }

            Transform spawnPoint = ResolveUsableTransform(info.SpawnPoint, "SpawnPoint", idx, info.BidderId);
            if (spawnPoint == null)
            {
                // 沒設這位出價者的 SpawnPoint 視為「本場不顯示這隻 NPC」，安靜跳過即可
                if (logInvalidTransformReferences)
                    Debug.Log($"[AuctionView] bidderSpawnPoints[{idx}] (BidderId='{info.BidderId}') 未設定 SpawnPoint，已跳過。", this);
                continue;
            }

            if (!TryGetTransformPose(spawnPoint, out Vector3 spawnPosition, out Quaternion spawnRotation))
            {
                if (logInvalidTransformReferences)
                    Debug.Log($"[AuctionView] bidderSpawnPoints[{idx}] (BidderId='{info.BidderId}') SpawnPoint 已失效，略過生成。", this);
                continue;
            }

            // 與 HumanMissionGeneator 相同的生成模式：parent 到 spawn point 自身，
            // 確保 NPC 會繼承生成點所在 hierarchy 的 active 狀態。
            AuctionBidderNpc npc = Instantiate(
                bidderNpcPrefab,
                spawnPosition,
                spawnRotation,
                spawnPoint);

            // 確保 NPC 物件本身為啟用狀態，避免 prefab 預設 inactive 導致看不到
            if (!npc.gameObject.activeSelf)
                npc.gameObject.SetActive(true);

            npc.ApplySprite(info.Sprite);

            // 以 BidderId 為主 key；缺少時退回用 DisplayName，避免完全找不到
            string key = !string.IsNullOrEmpty(info.BidderId) ? info.BidderId : info.DisplayName;
            if (string.IsNullOrEmpty(key))
            {
                if (logInvalidTransformReferences)
                    Debug.Log($"[AuctionView] bidderSpawnPoints[{idx}] 同時缺少 BidderId 與 DisplayName，無法註冊到查找字典。", this);
                continue;
            }

            spawnedBidderNpcs[key] = npc;

            // 為這隻 NPC 生成一個對話框；優先掛在 info.BubbleSpawnPoint（UI 層）下
            BubbleEntry entry = CreateBubbleFor(info);
            if (entry != null)
                spawnedBubbles[key] = entry;

            spawnedCount++;
        }

        if (spawnedCount == 0)
            Debug.LogWarning("[AuctionView] 一隻 AuctionBidderNpc 都沒生成，請檢查 bidderSpawnPoints / SpawnPoint 設定。", this);
        else
            Debug.Log($"[AuctionView] 成功生成 {spawnedCount} 隻 AuctionBidderNpc（bidderSpawnPoints 共 {bidderSpawnPoints.Count} 筆）。", this);
        bidderNpcsSpawned = spawnedCount > 0;

        // Player 不需要 NPC 立繪，只註冊對話框（marker 在 UpdateHighestBidderMarker 直接讀 playerSpawnInfo）
        RegisterPlayerBubble();
    }

    private void RegisterPlayerBubble()
    {
        if (playerSpawnInfo == null || playerSpawnInfo.BubbleSpawnPoint == null || bubblePrefab == null)
            return;

        Transform parent = ResolveUsableTransform(playerSpawnInfo.BubbleSpawnPoint, "BubbleSpawnPoint", -1, PlayerBidderId);
        if (parent == null)
            return;

        GameObject root = Instantiate(bubblePrefab, parent, false);
        TextMeshProUGUI text = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null && logInvalidTransformReferences)
            Debug.LogWarning("[AuctionView] bubblePrefab 內找不到 TextMeshProUGUI（Player）。", root);

        BubbleEntry entry = new()
        {
            Root = root,
            Text = text
        };
        root.SetActive(false);
        spawnedBubbles[PlayerBidderId] = entry;
    }

    private BubbleEntry CreateBubbleFor(AuctionBidderSpawnInfo info)
    {
        if (bubblePrefab == null)
        {
            Debug.LogWarning("[AuctionView] bubblePrefab 未指定，無法生成對話框。");
            return null;
        }

        // 一律使用 AuctionBidderSpawnInfo.BubbleSpawnPoint 作為對話框 parent
        Transform parent = ResolveUsableTransform(info?.BubbleSpawnPoint, "BubbleSpawnPoint", -1, info?.BidderId);
        if (parent == null)
            return null;

        GameObject root = Instantiate(bubblePrefab, parent, false);
        TextMeshProUGUI text = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null)
            Debug.LogWarning($"[AuctionView] bubblePrefab 內找不到 TextMeshProUGUI（bidder = {info?.BidderId}）。", root);

        BubbleEntry entry = new()
        {
            Root = root,
            Text = text
        };
        root.SetActive(false);
        return entry;
    }

    private void ClearSpawnedBidderNpcs()
    {
        // 對話框是 NPC 子物件，會隨 NPC 一起被 Destroy；這裡先停掉協程並清空字典
        foreach (var pair in spawnedBubbles)
        {
            BubbleEntry entry = pair.Value;
            if (entry == null)
                continue;

            if (entry.HideRoutine != null)
                StopCoroutine(entry.HideRoutine);

            // bubble 是 parent 在 BubbleSpawnPoint（UI 層），不會跟著 NPC 一起被 Destroy，這裡明確清掉
            if (entry.Root != null)
                Destroy(entry.Root);
        }
        spawnedBubbles.Clear();

        foreach (var pair in spawnedBidderNpcs)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        spawnedBidderNpcs.Clear();
        bidderNpcsSpawned = false;
    }

    /// <summary>
    /// 依 Controller 給的「本場實際參賽者 BidderId 清單」，啟用對應 NPC、關閉其餘 NPC。
    /// 傳 null 代表全部顯示（debug／測試用途）。本方法應在 Controller 的 BuildBidders 後呼叫一次。
    /// </summary>
    public void ApplyParticipants(IEnumerable<string> activeBidderIds)
    {
        EnsureBidderNpcsSpawned();

        HashSet<string> activeSet = activeBidderIds != null
            ? new HashSet<string>(activeBidderIds)
            : null;

        foreach (var pair in spawnedBidderNpcs)
        {
            AuctionBidderNpc npc = pair.Value;
            if (npc == null)
                continue;

            bool active = activeSet == null || activeSet.Contains(pair.Key);
            npc.gameObject.SetActive(active);
            if (!active && spawnedBubbles.TryGetValue(pair.Key, out BubbleEntry entry))
                HideBubbleEntry(entry);
        }
    }

    // -------------------------------------------------------------------------
    // 公開 API：NPC 圖片指派
    // -------------------------------------------------------------------------

    /// <summary>
    /// 指派／覆寫單一出價者 NPC 的 Sprite。
    /// 通常在 Controller 端依種族或本場狀況動態決定圖片時呼叫
    /// （Inspector 的 spawn point 已預先指派的 Sprite 會被覆蓋）。
    /// </summary>
    /// <param name="bidderId">出價者 ID（與 spawn 設定 / Controller 的 BidderId 對齊）</param>
    /// <param name="sprite">要套用的 Sprite；null 則保留原圖（不清空）</param>
    /// <returns>是否成功找到並套用</returns>
    public bool SetBidderSprite(string bidderId, Sprite sprite)
    {
        if (string.IsNullOrEmpty(bidderId) || sprite == null)
            return false;

        if (!spawnedBidderNpcs.TryGetValue(bidderId, out AuctionBidderNpc npc) || npc == null)
            return false;

        npc.ApplySprite(sprite);
        return true;
    }

    /// <summary>
    /// 批次指派 NPC 圖片：key = BidderId，value = Sprite。
    /// 字典中沒列到的 NPC 會保留原本 spawn point 上的圖。
    /// </summary>
    public void ApplyBidderSprites(IReadOnlyDictionary<string, Sprite> spritesByBidderId)
    {
        if (spritesByBidderId == null)
            return;

        foreach (var pair in spritesByBidderId)
            SetBidderSprite(pair.Key, pair.Value);
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
        {
            HideAllBidBubbles();
            UpdateHighestBidderMarker(null);
        }
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
        string currentBidderId,
        int seconds,
        int playerBudget,
        IReadOnlyList<string> participants,
        IReadOnlyList<int> bidAmounts,
        IReadOnlyList<bool> bidButtonStates)
    {
        SetCurrentPrice(FormatMoney(currentPrice));
        SetTimerSeconds(seconds);
        SetPlayerBudget(FormatMoney(playerBudget));
        SetBidButtonStates(bidAmounts, bidButtonStates);
        UpdateHighestBidderMarker(currentBidderId);
    }

    /// <summary>
    /// 將場景中的單一 highestBidderMarker 搬到當前最高出價者對應的 UI 點位 (HighestMarkerPoint)
    /// 並顯示；傳 null／空字串、或找不到對應點位時，直接隱藏 marker。
    /// </summary>
    private void UpdateHighestBidderMarker(string currentBidderId)
    {
        if (highestBidderMarker == null)
            return;

        if (string.IsNullOrEmpty(currentBidderId))
        {
            highestBidderMarker.SetActive(false);
            return;
        }

        // Player 從 playerSpawnInfo 取點位；其餘 NPC 從 bidderSpawnPoints 找對應 BidderId
        Transform target = null;
        if (currentBidderId == PlayerBidderId)
        {
            if (playerSpawnInfo != null)
                target = ResolveUsableTransform(playerSpawnInfo.HighestMarkerPoint, "HighestMarkerPoint", -1, PlayerBidderId);
        }
        else if (bidderSpawnPoints != null)
        {
            foreach (AuctionBidderSpawnInfo info in bidderSpawnPoints)
            {
                if (info != null && info.BidderId == currentBidderId)
                {
                    target = ResolveUsableTransform(info.HighestMarkerPoint, "HighestMarkerPoint", -1, currentBidderId);
                    break;
                }
            }
        }

        if (target == null)
        {
            if (logInvalidTransformReferences)
                Debug.LogWarning($"[AuctionView] BidderId='{currentBidderId}' 沒設定 HighestMarkerPoint，marker 無法顯示。", this);
            highestBidderMarker.SetActive(false);
            return;
        }

        Transform markerTransform = highestBidderMarker.transform;
        markerTransform.SetParent(target, false);
        Vector3 markerScale = new(0.15f, 0.15f, 1f);
        if (markerTransform is RectTransform rect)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = markerScale;
        }
        else
        {
            markerTransform.localPosition = Vector3.zero;
            markerTransform.localRotation = Quaternion.identity;
            markerTransform.localScale = markerScale;
        }
        highestBidderMarker.SetActive(true);
    }

    private Transform ResolveUsableTransform(Transform transformValue, string fieldName, int index, string bidderId)
    {
        if (transformValue != null)
        {
            try
            {
                _ = transformValue.gameObject;
                return transformValue;
            }
            catch (MissingReferenceException)
            {
                if (logInvalidTransformReferences)
                {
                    string indexText = index >= 0 ? $"[{index}]" : string.Empty;
                    Debug.LogWarning($"[AuctionView] bidderSpawnPoints{indexText} (BidderId='{bidderId}') 的 {fieldName} 已被銷毀，會嘗試自動查找或使用 fallback。", this);
                }
            }
        }

        return FindSceneTransformByBidderField(fieldName, bidderId);
    }

    private Transform FindSceneTransformByBidderField(string fieldName, string bidderId)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(bidderId))
            return null;

        string shortFieldName = fieldName.Replace("Point", string.Empty);
        string[] candidateNames =
        {
            $"{bidderId}_{fieldName}",
            $"{bidderId}{fieldName}",
            $"{fieldName}_{bidderId}",
            $"{bidderId}_{shortFieldName}",
            $"{shortFieldName}_{bidderId}"
        };

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate == null)
                continue;

            for (int i = 0; i < candidateNames.Length; i++)
            {
                if (candidate.name == candidateNames[i])
                    return candidate;
            }
        }

        return null;
    }

    private static bool TryGetTransformPose(Transform target, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (target == null)
            return false;

        try
        {
            position = target.position;
            rotation = target.rotation;
            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // 公開 API：主持人台詞
    // -------------------------------------------------------------------------

    /// <summary>顯示「拍賣會開始，起標價 X 金幣」。</summary>
    public void ShowStart(int startingPrice)
    {
        SetHostText(string.Format(startTextFormat, FormatAmountForTemplate(startTextFormat, startingPrice)));
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
            ? string.Format(playerBidTextFormat, FormatAmountForTemplate(playerBidTextFormat, bidAmount))
            : string.Format(npcBidTextFormat, bidderName, FormatAmountForTemplate(npcBidTextFormat, bidAmount));
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
        SetHostText(string.Format(format, FormatAmountForTemplate(format, currentPrice)));
    }

    /// <summary>
    /// 玩家金幣不足時呼叫。除了顯示主持人台詞外，也會透過 SystemInfoEvent 跳出全局提示。
    /// </summary>
    public void ShowNoMoney()
    {
        string text = NormalizeAuctionText(noMoneyText);
        SetHostText(text);
        SystemInfoEvent.Show(text);
    }

    /// <summary>玩家想再出價但自己已是最高價時的提示。</summary>
    public void ShowAlreadyHighestBidder()
    {
        SetHostText(alreadyHighestBidderText);
    }

    /// <summary>單獨更新倒數秒數欄位（不影響其他 UI）。</summary>
    public void SetTimerSeconds(int seconds)
    {
        SetTimer(string.Empty);
    }

    // -------------------------------------------------------------------------
    // 內部：實際寫入 TMP 文字欄位
    // -------------------------------------------------------------------------

    private void SetHostText(string text)
    {
        if (hostText != null)
            hostText.text = NormalizeAuctionText(text);
    }

    private void SetCurrentPrice(string text)
    {
        if (currentPriceText != null)
            currentPriceText.text = "當前出價:  " + text ?? string.Empty;
    }

    private void SetTimer(string text)
    {
        if (timerText != null)
        {
            timerText.text = text ?? string.Empty;
            timerText.gameObject.SetActive(!string.IsNullOrEmpty(timerText.text));
        }
    }

    private void SetPlayerBudget(string text)
    {
        if (playerBudgetText != null)
            playerBudgetText.text = text ?? string.Empty;
    }

    // -------------------------------------------------------------------------
    // 公開 API：對話框 / 出價提示
    // -------------------------------------------------------------------------

    /// <summary>
    /// 在指定出價者的對話框顯示出價金額，bubbleVisibleSeconds 秒後自動隱藏。
    /// 會優先以 bidderId 查找對應對話框，找不到時用 bidderName 解析。
    /// </summary>
    public void ShowBidBubble(string bidderId, string bidderName, int bidAmount)
    {
        BubbleEntry entry = FindBubble(bidderId, bidderName);
        if (entry == null)
        {
            // 該出價者沒有對應 spawn 設定（例如該位置被刻意留空）→ 不顯示對話框、安靜結束
            if (logInvalidTransformReferences)
                Debug.Log($"[AuctionView] 找不到 bidder='{bidderId}' / name='{bidderName}' 的對話框（bidderSpawnPoints 未設定該位）。");
            return;
        }
        if (entry.Root == null || entry.Text == null)
            return;

        string format = ResolveBidBubbleFormat();
        entry.Text.text = NormalizeAuctionText(string.Format(format, FormatAmountForTemplate(format, bidAmount)));
        entry.Root.SetActive(true);

        if (entry.HideRoutine != null)
            StopCoroutine(entry.HideRoutine);

        entry.HideRoutine = StartCoroutine(HideBubbleAfterDelay(entry, Mathf.Max(0.1f, bubbleVisibleSeconds)));
    }

    /// <summary>關閉所有 NPC 對話框（含尚未到期的）。</summary>
    public void HideAllBidBubbles()
    {
        foreach (var pair in spawnedBubbles)
            HideBubbleEntry(pair.Value);
    }

    private void HideBubbleEntry(BubbleEntry entry)
    {
        if (entry == null)
            return;

        if (entry.HideRoutine != null)
        {
            StopCoroutine(entry.HideRoutine);
            entry.HideRoutine = null;
        }

        if (entry.Root != null)
            entry.Root.SetActive(false);
    }

    private IEnumerator HideBubbleAfterDelay(BubbleEntry entry, float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (entry.Root != null)
            entry.Root.SetActive(false);

        entry.HideRoutine = null;
    }

    private BubbleEntry FindBubble(string bidderId, string bidderName)
    {
        if (!string.IsNullOrEmpty(bidderId)
            && spawnedBubbles.TryGetValue(bidderId, out BubbleEntry byId))
            return byId;

        if (!string.IsNullOrEmpty(bidderName))
        {
            if (spawnedBubbles.TryGetValue(bidderName, out BubbleEntry byName))
                return byName;

            if (bidderSpawnPoints != null)
            {
                foreach (AuctionBidderSpawnInfo info in bidderSpawnPoints)
                {
                    if (info != null && info.DisplayName == bidderName
                        && !string.IsNullOrEmpty(info.BidderId)
                        && spawnedBubbles.TryGetValue(info.BidderId, out BubbleEntry match))
                        return match;
                }
            }
        }

        return null;
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
            label.text = $"+{FormatMoney(amount)}";
    }

    private static string FormatMoney(int amount)
    {
        return $"{amount:N0} 元";
    }

    private static string FormatAmountForTemplate(string template, int amount)
    {
        if (!string.IsNullOrEmpty(template)
            && (template.Contains("元") || template.Contains("金幣")))
        {
            return amount.ToString("N0");
        }

        return FormatMoney(amount);
    }

    private string ResolveBidBubbleFormat()
    {
        if (string.IsNullOrWhiteSpace(bidBubbleFormat) || bidBubbleFormat.Contains("{1}"))
            return "{0}";

        return bidBubbleFormat;
    }

    private static string NormalizeAuctionText(string text)
    {
        return string.IsNullOrEmpty(text)
            ? string.Empty
            : text.Replace("金幣", "元");
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
// AuctionBidderSpawnInfo：拍賣會單一出價者 NPC 的生成設定（Inspector 拖設定）
// -----------------------------------------------------------------------------
// 使用方式：
//   - BidderId 與 AuctionController 的 bidder ID 對齊，是 ShowBidBubble 主要查找 key。
//   - DisplayName 是當 BidderId 找不到時的備援比對 key（也可用作預設顯示名稱）。
//   - Sprite 是該 NPC 的角色圖，會被傳給 AuctionBidderNpc.ApplySprite。
//   - SpawnPoint 指定 NPC 生成位置（場景中的 Transform）。
//   - BubbleSpawnPoint 指定對話框生成的「UI 層」點位（建議放在 Canvas 之下的 RectTransform）。
//     留空時 fallback 到 SpawnPoint，再不行則掛在 NPC 之下。
// =============================================================================
[Serializable]
public class AuctionBidderSpawnInfo
{
    public string BidderId;              // 出價者唯一識別字串（與 AuctionController 對齊）
    public string DisplayName;           // 顯示名稱（兼作備援查找 key）
    public Sprite Sprite;                // 角色 Sprite
    public Transform SpawnPoint;         // NPC 生成位置；NPC 會 parent 到此 Transform 之下（HumanMissionGeneator 模式）
    public Transform BubbleSpawnPoint;   // 對話框生成位置（UI 層 RectTransform；建議在 Canvas 之下）
    public Transform HighestMarkerPoint; // 「目前最高出價者」標示停靠位置（UI 層 RectTransform；建議在 Canvas 之下）
}

// =============================================================================
// AuctionPlayerSpawnInfo：Player 專用的「對話框 + 最高出價標示停靠點」設定
// -----------------------------------------------------------------------------
// Player 不會被 Instantiate 成 AuctionBidderNpc（玩家本人不需要場上立繪），
// 只需要兩個 UI 點位讓 View 在 Player 出價時把對話框／marker 放上去。
// =============================================================================
[Serializable]
public class AuctionPlayerSpawnInfo
{
    public RectTransform BubbleSpawnPoint;   // 玩家出價時對話框停靠點（UI 層 RectTransform）
    public Transform HighestMarkerPoint; // 玩家為最高出價者時 marker 停靠點（UI 層 RectTransform）
}
