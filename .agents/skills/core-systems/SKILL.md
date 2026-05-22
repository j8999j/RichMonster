---
name: core-systems
description: 本專案（ForTest / 紅盒子）核心執行時三大 Singleton 的完整地圖。當使用者提到 GameManager、GameFlow、DataManager、Singleton、場景切換 (SceneTransitionManager、GoToHumanScene/GoToMonsterScene/GoToNextDay/GoToEndStoryScene、SCENE_END_STORY)、玩家鎖定 (LockPlayerMove、UnlockPlayerMove、LockPlayerInteract、_moveLockSources、PlayerLockSources)、天數切換 (NextDay、CurrentDay、DayPhase)、SwitchGameStageAndSave、TutorialFlow、StartTutorial、TutorialSaveData、GameRng (InitDailySeed、RangeKeyed、ValueKeyed、MasterSeed、固定式隨機)、結局系統 (EndingType、EndingConditionDetector、SetEndingReached、HasReachedEnding、ReachedEndingType、TryPayGuaranteeDeposit、TryPayAuctionEntryFee、HasPaidGuaranteeDeposit、HasPaidAuctionEntryFee、GuaranteeDepositGuide、AuctionEntryFeeGuide、AuctionDayGuide)、玩家靜態字典查詢 (ItemDict、ShopDict、MissionDict 等)、ModifyGold/ModifyMonsterGold/TrySpendGold/AddItem、背包容量 (BaseInventoryCapacity、GetInventoryCapacity、CanAddItemsToInventory、TrySpendGoldForItemPurchase)、ExchangeAllMonsterGoldToGold、OnPlayerDataChanged、BookDataChanged、SetPlayerData / GetPlayerSaveData / GetPersistentSaveData、GameFlowEvents、NoticeGetItemEvents、PlayerInfoUIEvents、Cinemachine 攝影機跟隨 (ClearCameraHorizontalBounds、CameraHorizontalBounds)、StartNewGame/InitializeGame 啟動流程等核心執行時議題時載入此 skill。與 save-system（磁碟 I/O 層）互補：core-systems 是 runtime 操作中樞，save-system 是 persistence 管線。
---

# 核心系統（GameManager / GameFlow / DataManager）

## 0. TL;DR

**三大 Singleton 的職責分工**：

| 類別 | 角色 | 生命期 | 繼承 |
|---|---|---|---|
| **GameManager** | 場景 / 玩家物件 / 鎖定 / 攝影機總控 | 常駐（DontDestroyOnLoad） | `Singleton<GameManager>` |
| **DataManager** | 靜態資料字典 + 玩家 runtime 資料 + 存檔代理 | 常駐 | `Singleton<DataManager>` |
| **GameFlow** | 單局日程推進（天數 / 階段 / 存檔觸發 / 教學起始）| 每局 new 一次 | 一般 class（非 Singleton） |
| **GameRng** | 固定式隨機（依 MasterSeed + Day） | 純 static 類別 | `static class` |

**決策樹（我要做 X → 直接跳到哪一節）**：

| 任務 | 跳到 |
|---|---|
| 鎖 / 解鎖玩家移動或互動 | §3.2 |
| 跨場景取得玩家物件 | §3.1 |
| 推進到下一天 / 切換日夜 | §4.1 / §4.2 |
| 讀 / 寫單局存檔 key | §4.4（與 save-system §9 互補） |
| 修改金幣、物品、背包 | §4.3 |
| 新 API 命名規範 | §4.2 動詞表 |
| 固定式隨機（抽商店、抽獎） | §6 |
| 接收日 / 階段變化事件 | §7 |
| 遊戲啟動流程 / 新遊戲 | §8 |
| 結局 / 拍賣保證金 / 入場費 | §10 |
| 重構、命名、效能議題 | §9 架構建議 |

**不要做的事**：
- 不要手動 `new GameManager()` / `new DataManager()`，都是 Singleton，場景內已掛 MonoBehaviour
- 不要直接改 `_currentPlayerData` 欄位，走 `ModifyXxx` / `SetPlayerData` API，才會設 `OnPlayerDataChanged` 旗標
- 不要在互動功能結束時忘了配對 `UnlockPlayerMove`，鎖源採 HashSet stack 疊加，漏一次就永遠卡住
- 不要在 `GameFlow` 建構子以外的地方呼叫 `GameRng.InitDailySeed`，會打亂整天的抽選結果

---

## 1. 關鍵檔案

| 路徑 | 角色 |
|---|---|
| [Assets/GameData/GameSystem/GameManager.cs](Assets/GameData/GameSystem/GameManager.cs) | 總指揮。持有 `dataManager` / `saveManager` / `talkSystem` / `gameFlow` / `sceneTransitionManager` / `PlayerController` / `virtualCamera` 引用 |
| [Assets/GameData/GameSystem/GameFlow.cs](Assets/GameData/GameSystem/GameFlow.cs) | 單局日程（CurrentDay / NextDay / SwitchGameStageAndSave / SaveGameAsync / StartTutorial） |
| [Assets/GameData/GameSystem/DataManager.cs](Assets/GameData/GameSystem/DataManager.cs) | 資料中樞：靜態字典 + `_currentPlayerData` + `_bookData` + Modify API + GetPlayerSaveData API |
| [Assets/GameData/GameSystem/GameRng.cs](Assets/GameData/GameSystem/GameRng.cs) | 固定式隨機：全域 (`Range` / `Value`) + Keyed (`RangeKeyed` / `ValueKeyed`) |
| [Assets/GameData/GameSystem/Singleton.cs](Assets/GameData/GameSystem/Singleton.cs) | MonoBehaviour Singleton 抽象基底 |
| [Assets/GameData/GameSystem/SceneTransitionManager.cs](Assets/GameData/GameSystem/SceneTransitionManager.cs) | 被 GameManager 代理呼叫的場景切換（`SCENE_MAIN_MENU` / `SCENE_HUMAN` / `SCENE_MONSTER` 常量） |
| [Assets/GameData/GameSystem/StaticEvent/GameFlowEvents.cs](Assets/GameData/GameSystem/StaticEvent/GameFlowEvents.cs) | `OnDayPhaseChanged(DayPhase)` / `OnDayChanged(int)` |
| [Assets/GameData/GameSystem/StaticEvent/NoticeGetItemEvents.cs](Assets/GameData/GameSystem/StaticEvent/NoticeGetItemEvents.cs) | `OnShowNotice` / `OnClearNotice` / Map guide 事件 |
| [Assets/GameData/GameSystem/StaticEvent/PlayerInfoUIEvents.cs](Assets/GameData/GameSystem/StaticEvent/PlayerInfoUIEvents.cs) | 背包 / 紀念品 / 成就 / 圖鑑 / 新聞面板開啟事件 |

---

## 2. GameManager：場景 / 玩家 / 鎖定 / 攝影機

### 2.1 繼承與初始化
```csharp
public class GameManager : Singleton<GameManager>
```
- `Awake()`：抓 `SaveManager.Instance` / `DataManager.Instance` / `GetComponent<SceneTransitionManager>()`，訂閱 `OnSceneLoadComplete`
- `Start()`（IEnumerator）：`while (!dataManager.IsInitialized) yield return null` → 載入 MainMenu
- `OnDestroy()`：解訂閱場景事件

### 2.2 玩家鎖定（★ 最常用的 API）

```csharp
private readonly HashSet<string> _moveLockSources = new HashSet<string>();
private readonly HashSet<string> _interactLockSources = new HashSet<string>();
```

**用法**（成對出現，source 必須唯一且對稱，★ 一律使用 `PlayerLockSources` 常數）：
```csharp
GameManager.Instance.LockPlayerMove(PlayerLockSources.GroceryStore);
// ...玩家被鎖定期間...
GameManager.Instance.UnlockPlayerMove(PlayerLockSources.GroceryStore);
```

**新增互動鎖時**：在 [Assets/GameData/GameSystem/PlayerLockSources.cs](Assets/GameData/GameSystem/PlayerLockSources.cs) 加一個常數，再於呼叫端引用。不要散佈 magic string。

| API | 行為 |
|---|---|
| `LockPlayerMove(source)` | 加入 HashSet；設 `PlayerController.SetCanMove(false)` |
| `UnlockPlayerMove(source)` | 移除；**只有 HashSet 空時才**真正 SetCanMove(true) |
| `IsPlayerMoveLocked(source)` | 查是否含 source（注意：不是「是否鎖定」） |
| `GetPlayerMove()` | HashSet 是否為空 |
| `LockPlayerInteract / UnlockPlayerInteract` | 同模式用於 E 鍵互動 |
| `ClearAllLocks()` | 清光兩個 HashSet；場景切換時自動呼叫 |

**常數清單**（全部定義於 [PlayerLockSources.cs](Assets/GameData/GameSystem/PlayerLockSources.cs)，目前共 24 個）：
- 通用：`Guide` / `TrashCan` / `ScratchCardShop` / `PlayerInfoUI` / `NoticeGetItem` / `MonsterTrade` / `HumanOrderView` / `NpcOnMap` / `AbyssShop` / `TalkSystem` / `MonsterGoldExchange`
- 傳送與拍賣：`TelePoint` / `TelePointAuctionGuide` / `Auction` / `AuctionNpc`
- 商店：`GroceryStore` / `YokaiStore` / `FurnituresShop` / `FoodShop` / `VendingMachine` / `YokaiEat` / `HumanShopEat` / `WanderingYokaiMerchant`
- 例外：AbyssShop 內部用 `ID` 屬性（`IMapGuideTarget` 介面契約）傳入，值與 `PlayerLockSources.AbyssShop` 一致
- 對話系統：`TalkSystem.autoLockPlayer = true` 時，由 TalkSystem 自動以 `PlayerLockSources.TalkSystem` 鎖定，呼叫端不需重複手動鎖

**常見 bug**：`_moveLockSources` 用 HashSet 不支援重複計數，同一 source 鎖多次只存一次，**不要**當成 int stack 使用。

### 2.3 玩家物件 / 攝影機

```csharp
public GameObject PlayerPrefab;        // Inspector 指派
private GameObject Player;             // runtime Instantiate
private PlayerController PlayerController;
[SerializeField] private CinemachineVirtualCamera virtualCamera;
```

場景載入完成（`OnSceneLoadComplete`）會略過 `SCENE_MAIN_MENU` 與 `SCENE_END_STORY`，其餘場景自動：
1. `SetPlayer()` → Instantiate(PlayerPrefab)
2. `SetPlayerPosition(new Vector3(0, -2, 0))`（寫死；內部走 `PlayerController.TeleportTo(position)`）
3. `ClearAllLocks()`
4. `SetCameraFollowPlayer()`
5. `ClearCameraHorizontalBounds()` — 解除 TelePoint 可能留下的 `CameraHorizontalBounds` 限制並重置 `PreviousStateIsValid`
6. 如果是妖怪場景 → `PlayerController.SetIsNight(true)`

**手動切玩家位置**：`GameManager.Instance.SwitchPlayerPos(Vector3)`

### 2.4 場景切換捷徑

全部都是代理呼叫 `SceneTransitionManager`：
```csharp
GameManager.Instance.GoToHumanScene();   // 人間場景
GameManager.Instance.GoToMonsterScene(); // 妖怪場景
GameManager.Instance.GoToMainMenu();     // 主選單
GameManager.Instance.GoToNextDay();      // 呼 gameFlow.NextDay() + GoToHumanScene
```

`SceneTransitionManager` 常量：
- `SCENE_MAIN_MENU` / `SCENE_HUMAN` / `SCENE_MONSTER`
- `GoToSceneByPhase(DayPhase, onComplete)` — 根據 PlayingStatus 決定進哪個場景

---

## 3. GameFlow：單局日程

### 3.1 生命期

```csharp
// 建構：InitializeGame(slot) 內部 new 一次
gameFlow = new GameFlow(playerData, slot);
// 銷毀：遊戲結束 / 回主選單時（實際未顯式銷毀，由 GameManager 重指派覆蓋）
```

建構子會**立刻**呼叫 `GameRng.InitDailySeed(MasterSeed, CurrentDay)` + `new TutorialFlow()`，所以 `new GameFlow` 之後當天所有 Keyed 隨機已就緒、教學流程也已就位。

### 3.2 核心 API

| 方法 | 何時呼叫 | 做什麼 |
|---|---|---|
| `CurrentDay` | 讀取 | 單局當前天數（以 `PlayerData.DaysPlayed` 為 source of truth） |
| `NextDay()` | 夜晚結束 | `CurrentDay++` → `DataManager.ModifyCurrentDay` → 重設 RNG 種子 → `SwitchGameStageAndSave(Night)` |
| `SwitchGameStageAndSave(DayPhase)` | 切換日夜階段 | 1) 非 AfterNoon 清訂單進度 2) `ModifyCurrentDayPhase` + 發 `OnDayPhaseChanged` 3) 刷新 `GuaranteeDepositGuide` / `AuctionEntryFeeGuide` / `AuctionDayGuide` 4) Night/HumanDay 呼叫 `EndingConditionDetector.EvaluateForNewMonsterDay/HumanDay`，若有結局則 `SetEndingReached` + 存檔 + 提前 return 5) HumanDay 發 `OnDayChanged` 與 `AchievementEvents.DayEndGold` 6) `SaveGameAsync` |
| `StartTutorial()` | InitializeGame | 讀跨局 `GetPersistentSaveData<TutorialSaveData>("TutorialSaveData")`，**未完成 (`!IsComplete`) 且 `DaysPlayed <= 1`** 時 `_tutorialFlow.Start()`。實際教學流程封裝在 `TutorialFlow` class，不再內聯 |
| `SaveGameAsync()` | 存檔點 | 檢查 `OnPlayerDataChanged`，先清旗標再 `await SaveManager.SaveGameAsync` + `DataManager.SaveAchievementAsync` |

**日夜階段**（`DayPhase` enum）：`HumanDay` → `AfterNoon` → `Night` → ...

**存檔旗標模式**（重要）：
```csharp
if (!DataManager.Instance.OnPlayerDataChanged) return;
DataManager.Instance.SetPlayerDataChanged(false);   // 先清再 await
await SaveManager.Instance.SaveGameAsync(...);
```
在 await 期間若有新變更會再次把旗標標 dirty，下一次 save 就會補寫。

### 3.3 天數常數

```csharp
private const int DAY_THRESHOLD_MID = 6;    // 進入中期
private const int DAY_THRESHOLD_LATE = 14;  // 進入後期
```
（目前未使用，為未來遊戲進程階段預留）

---

## 4. DataManager：靜態字典 + 玩家資料中樞

### 4.1 區塊總覽

```csharp
#region Game Static Data    — 16 個 Dictionary，由 GameDataLoader 一次載入
#region Mission Caches       — 任務分類快取（人間/妖界 × Info/NonInfo）
#region Save & Runtime Data  — _initialPlayerData / _currentPlayerData / _bookData / 成就 dict / 特殊紀念品 dict
#region State Flags          — OnPlayerDataChanged / OnBookDataChanged / IsInitialized
#region Read-only Accessors  — IReadOnlyDictionary / IReadOnlyPlayerData 外露
#region Events               — PlayerMainViewUpdate / OnItemPurchased
#region Player Save/Load     — SaveCurrentPlayerAsync / LoadPlayerFromSave / SetCurrentPlayer
#region ModifyPlayerAPI      — ModifyGold / TrySpendGold / AddItem / AddShopShelfData ...
#region GetPlayerSaveDataAPI — GetPlayerSaveData<T> / GetPersistentSaveData<T>
```

### 4.2 動詞命名規範（★ 新增功能時遵守）

| 動詞 | 語意 | 範例 |
|---|---|---|
| `Modify*` | 改值 + 設 dirty flag + 可能發事件 | `ModifyGold` / `ModifyMonsterGold` / `ModifyCurrentDay` / `ModifyCurrentDayPhase` |
| `Add*` / `Remove*` | 集合 CRUD | `AddItem` / `RemoveItem` / `AddShopShelfData` / `AddOrderProgress` |
| `Clear*` | 集合清空 | `ClearOrderProgress` / `ClearBookDataCache` |
| `TrySpend*` | 足額才扣，回 bool | `TrySpendGold` / `TrySpendMonsterGold` |
| `Set*` | 覆寫純量 / 記憶體 dict（不一定落檔）| `SetCurrentPlayer` / `SetIsTrade` / `SetPlayerData<T>` / `SetPlayerDataChanged` |
| `Get*` | 只讀 | `GetItemById` / `GetPlayerSaveData<T>` / `GetMonsterTradeHistory` |
| `Is*` / `Has*` | bool 查詢 | `IsAchievementCompleted` / `HasAnyNewMonsterInfo` |
| `Refresh*` | 手動觸發 UI 事件（不改資料） | `RefreshPlayerMainView` |
| `Save*Async` | 非同步落檔 | `SaveCurrentPlayerAsync(slot)` / `SaveBookAsync` / `SaveAchievementAsync` |
| `Load*` | 同步或非同步從磁碟讀 | `LoadGameDataAsync` / `LoadCurrentPlayerFromSlot(slot)` |
| `Update*SaveData` | **寫 dict + 同步落檔**（與 `Set*` 不同處：會立刻 sync save） | `UpdateAchievementSaveData` / `UpdateSpecialSouvenirSaveData` |
| `Unlock*` / `Confirm*` | 業務動作 | `UnlockMonsterInformation` / `ConfirmSingleNewInfo` |

**規則**：
- 需要立即落檔 → 用 `Update*SaveData` 族
- 只改記憶體、等下次 SaveGameAsync → 用 `SetPlayerData` / `Modify*`
- 新增任何 API 時先對照上表，不要發明新動詞

### 4.3 常用 Modify API（會自動設 `OnPlayerDataChanged = true`）

| API | 用途 |
|---|---|
| `ModifyGold(int)` / `ModifyMonsterGold(int)` | 加減金幣（自動 clamp 0） |
| `TrySpendGold(int)` / `TrySpendMonsterGold(int)` | 足額才扣；Gold 版本會觸發 `OnItemPurchased` 事件 |
| `TrySpendGoldForItemPurchase(amount, itemAmount=1)` / `TrySpendMonsterGoldForItemPurchase(...)` | 先 `CanAddItemsToInventory` 檢查背包再扣金；背包滿時走 `SystemInfoEvent.Show("背包已滿")` |
| `ExchangeAllMonsterGoldToGold(out spent, out gained)` | 妖怪金幣按 3/4 (向上取整) 兌成人界金幣，會 clamp 至 `int.MaxValue` |
| `AddItem(itemId, costPrice)` | 加入背包；自動 `AchievementEvents.GetItem` + `AddItemToBook` |
| `RemoveItem(Item)` | 需 id + costPrice 同時符合 |
| `SetIsTrade(bool)` | 白天開店狀態旗標 |
| `ModifyCurrentDay(int)` | 改天數（通常由 GameFlow 呼叫）；跨日會自動把 `IsTrade=false` |
| `ModifyCurrentDayPhase(DayPhase)` | 改階段；會發 `GameFlowEvents.InvokeDayPhaseChanged` |
| `SetEndingReached(EndingType)` | 標記本局已達結局（同時設定 `HasReachedEnding`），詳見 §10 |
| `TryPayGuaranteeDeposit()` / `TryPayAuctionEntryFee()` | 一次性支付保證金 / 拍賣入場費；已支付過直接回 true，金幣不足回 false |
| `RefreshPlayerMainView()` | 手動觸發 `PlayerMainViewUpdate` 事件（UI 初始化時呼叫） |
| `GetMonsterTradeHistory()` | 從 GameSaveFile 讀 `"MonsterTradeHistory"`，無則回傳 new |

**背包容量類**：
- 常數 `DataManager.BaseInventoryCapacity = 25`
- `GetInventoryCapacity()` = Base + `SouvenirManager.GetExtraBagCapacity()`
- `GetInventoryItemCount()` / `CanAddItemsToInventory(amount=1)` 用於 UI / 商店扣款前檢查

**查詢類**：`GetItemCountByRarity` / `GetDistinctItemCountByTypeAndWorld` / `GetHumanItemCount` / `GetMonsterItemCount`

**圖鑑 mutator（會立刻 `SaveBookData` 落檔 + 觸發 `BookDataChanged` 事件）**：
- `UnlockMonsterInformation(string)` / `UnlockRandomMonsterInformation()` — 解鎖單筆 / 隨機一筆未解鎖情報，自動處理「每 2 個情報解鎖 1 個故事」門檻並寫入 `NewMonsterInformationID / NewMonsterStoryID` 紅點
- `ConfirmSingleNewInfo(id)` / `ConfirmSingleNewStory(id)` / `ConfirmMonsterNewInfo(monsterId)` — 清單筆 / 整個妖怪的紅點
- `IsMonsterInfoUnlocked(id)` / `HasAnyNewMonsterInfo()` / `HasNewMonsterInfo(monsterId)` — 圖鑑紅點查詢
- `UpdateAchievementSaveData(IAchievementSave)` / `UpdateAllAchievementSaveData()` — 單筆 / 批次寫成就 dict 並同步落檔
- `UpdateSpecialSouvenirSaveData(ISpecialSouvenirSave)` — 同上但用於特殊紀念品
- `BookDataChanged` event：圖鑑/成就/紀念品任一筆改動都會 fire，UI 紅點訂閱此事件

### 4.4 單局按 key 存取（ISaveData 模式）

```csharp
// 寫入（自動設 OnPlayerDataChanged）
DataManager.Instance.SetPlayerData("MyKey", mySaveData);

// 跨日自動回傳新實例（適合「每日重置」的資料）
var data = DataManager.Instance.GetPlayerSaveData<MyData>("MyKey");

// 跨日保留（適合「單局累積」的資料，如教學進度）
var data = DataManager.Instance.GetPersistentSaveData<MyData>("MyKey");
```

判斷邏輯（`GetPlayerSaveData<T>`）：
```
if (data.LastUpdatedDay != _currentPlayerData.DaysPlayed) return new T();
```

**要跨局持久化**（主選單讀檔後還在）：用 save-system skill 的 `Book` 管線，不是這裡。

### 4.5 PlayerMainViewUpdate（主畫面 HUD）

```csharp
public event Action<int, int, DayPhase> PlayerMainViewUpdate;
```
由 `AdjustUpdateView()` 根據 `PlayingStatus` + `IsTrade` 決定顯示 Gold 或 MonsterGold，顯示的天數在 Night 階段會顯示 `DaysPlayed - 1`。

### 4.6 初始化（啟動流程）

```csharp
protected override void Awake() {
    base.Awake();
    _initTask = InitializeAsync();   // fire-and-forget
}
public Task WhenInitialized() => _initTask;
```
`GameManager.Start()` 會 `while (!dataManager.IsInitialized) yield return null` 等待字典載入完才進主選單。

---

## 5. GameRng：固定式隨機

### 5.1 兩種呼叫模式

| 模式 | API | 何時用 |
|---|---|---|
| **全域 Daily Rng** | `Range(min,max)` / `Value()` | 當日的一次性抽選，呼叫順序會影響結果 |
| **Keyed Rng** | `RangeKeyed(min,max,key)` / `ValueKeyed(key)` | 依 `key` 決定，與呼叫順序無關（推薦） |

### 5.2 初始化
```csharp
GameRng.InitDailySeed(masterSeed, currentDay);
// 僅由 GameFlow 建構子與 NextDay 呼叫
```
種子公式：`masterSeed + (currentDay * 9973)`（9973 為大質數分散分佈）。

### 5.3 Key 命名慣例（全專案掃出的範本）

| 來源 | Key 範本 |
|---|---|
| AbyssShop | `"AbyssReward_Day{day}_Layer{layer}_Draw{index}"` / `"AbyssRate_Day{day}_Layer{layer}"` |
| GroceryStore / Furnitures / Food 等 | `ShopID + index.ToString()`（抽商品池） |
| YokaiPackageSpawner | `"YokaiPackage_Day{day}_Pos{i}"` / `"YokaiPackage_Day{day}_Reward{index}"` |

**原則**：Key 至少要包含「系統名 + 區分點」，讓同天同位置每次結果一樣，但不同位置/不同天不衝突。

### 5.4 技術細節
- 用 FNV-1a 32-bit hash + Mix avalanche + XOR with dailySeed
- `ValueKeyed` 取前 24 bit 除 16777216 映射 0~1
- 未 `InitDailySeed` 前呼叫會 fallback `UnityEngine.Random`（警告：非固定）

---

## 6. StaticEvent（橫切事件）

三組 static 事件，**發布者**在 DataManager / GameFlow / UI 按鈕，**訂閱者**在 UI、成就、存檔觸發等處。

### 6.1 GameFlowEvents
```csharp
GameFlowEvents.OnDayPhaseChanged += (DayPhase p) => ...;  // 階段切換
GameFlowEvents.OnDayChanged      += (int day)    => ...;  // 日期遞增（HumanDay 時才發）
GameFlowEvents.InvokeDayPhaseChanged(phase);
GameFlowEvents.InvokeDayChanged(day);
```

### 6.2 NoticeGetItemEvents
```csharp
// 獲得物品通知
NoticeGetItemEvents.InvokeShowNotice("來源名稱", noticeItems);
NoticeGetItemEvents.OnShowNotice  += (source, items) => ...;
NoticeGetItemEvents.OnClearNotice += () => ...;

// 地圖導引
NoticeGetItemEvents.InvokeSetMapGuide(id, transform);
NoticeGetItemEvents.OnStartMapGuide += (id) => ...;
NoticeGetItemEvents.OnClearMapGuide += () => ...;
```

建立 NoticeItemEntry：`NoticeItemEntry.MonsterGold(n)` / `.Gold(n)` / `.ItemEntry(id, n)` / `.Other(name, sprite, n)`

### 6.3 PlayerInfoUIEvents
```csharp
PlayerInfoUIEvents.InvokeOpenBag();           // 背包
PlayerInfoUIEvents.InvokeOpenSouvenirBag();   // 紀念品
PlayerInfoUIEvents.InvokeOpenAchievement();   // 成就
PlayerInfoUIEvents.InvokeOpenBook();          // 圖鑑
PlayerInfoUIEvents.InvokeOpenNews();          // 新聞
PlayerInfoUIEvents.InvokeCloseAll();          // 關閉全部
```

---

## 7. 啟動流程（時序）

```
1. Unity Awake    → GameManager.Awake / DataManager.Awake（後者 fire _initTask）
2. Unity Start    → GameManager.Start → await dataManager.IsInitialized
3. IsInitialized  → LoadScene(MainMenu)
4. 按「新遊戲」   → StartNewGame:
                     - GetNextAvailableSlot
                     - new MasterSeed = Random.Range
                     - HoldAchievementSouvenirID 從 Book 複製一份
                     - 重置結局旗標：HasReachedEnding=false / ReachedEndingType=None
                     - 重置一次性費用：HasPaidGuaranteeDeposit=false / HasPaidAuctionEntryFee=false
                     - SetCurrentPlayer + SaveCurrentPlayerAsync
                     - InitializeGame(slot)
5. 按「讀取存檔」 → DataManager.LoadCurrentPlayerFromSlot(slot) + InitializeGame(slot)
6. InitializeGame →
    - 若 playerData.HasReachedEnding == true → GoToEndStoryScene() 直接結束
    - Souvenir.ResnapshotForCurrentGame
    - new GameFlow(playerData, slot)          // 此時呼叫 GameRng.InitDailySeed + new TutorialFlow
    - sceneTransitionManager.GoToSceneByPhase →
        - 場景載入完成（OnSceneLoadComplete，略過 MAIN_MENU 與 END_STORY）
        - ModifyCurrentDay(DaysPlayed)
        - InvokeDayPhaseChanged(PlayingStatus)
        - GuaranteeDepositGuide.Refresh / AuctionEntryFeeGuide.Refresh / AuctionDayGuide.Refresh
        - ApplyAllStartEffects（紀念品常駐效果）
        - gameFlow.StartTutorial
        - InitializePlayerInScene（SetPlayer / TeleportTo / ClearAllLocks / SetCamera / ClearCameraHorizontalBounds）
```

---

## 8. 實務 Checklist

### ✅ 新增一個「互動鎖玩家」功能
1. `Interact()` 入口先檢查 `GameManager.Instance.IsPlayerMoveLocked("MyID")`（避免重入）
2. 鎖：`GameManager.Instance.LockPlayerMove("MyID")`
3. 結束：`GameManager.Instance.UnlockPlayerMove("MyID")`
4. 若面板可能因場景切換被關掉：靠 `ClearAllLocks()` 兜底

### ✅ 新增一個「每日重置」的存檔資料
1. 建類別實作 `ISaveData`：`UniqueID` 回傳固定字串、`int LastUpdatedDay { get; set; }`
2. 寫：`DataManager.Instance.SetPlayerData("MyKey", data)`
3. 讀：`DataManager.Instance.GetPlayerSaveData<MyData>("MyKey")`（跨日自動回新實例）
4. **若要跨日保留**：改用 `GetPersistentSaveData<T>`

### ✅ 新增一個「固定式隨機」抽選
1. 確保此功能在 `GameFlow` 建構完之後才觸發（InitDailySeed 已呼過）
2. Key 命名：`"{系統}_Day{day}_{區分點}"`
3. 用 `RangeKeyed` 取整數、`ValueKeyed` 取 0~1

### ✅ 接收「進入新的一天」事件
```csharp
void OnEnable()  { GameFlowEvents.OnDayChanged += HandleDay; }
void OnDisable() { GameFlowEvents.OnDayChanged -= HandleDay; }
void HandleDay(int day) { ... }
```

---

## 9. 架構觀察與改進建議

### 9.1 現況的強項
1. **清楚的三層切分**：場景/玩家（GameManager）、日程（GameFlow）、資料（DataManager）邊界明確
2. **存檔旗標模式**（`OnPlayerDataChanged` 先清再 await）正確處理 save-during-save 競態
3. **鎖源 HashSet 模式**可讓多個系統同時鎖玩家移動，彼此不互相 unlock 錯
4. **GameRng Keyed** 設計讓隨機結果與呼叫順序解耦，利於除錯與重現

### 9.2 建議改進項（依優先度）

#### ✅ P0（已完成）：Lock Source 常數化
已新增 [PlayerLockSources.cs](Assets/GameData/GameSystem/PlayerLockSources.cs)，全專案 16 個 Lock source 常數集中管理。詳見 §2.2 用法。

#### 🟠 P1：GameManager 職責過載
**問題**：GameManager 現在同時管：Singleton 初始化、場景切換代理、玩家 Instantiate、Lock 系統、攝影機跟隨、GoToHumanScene/Monster/NextDay 捷徑。單一檔 260+ 行且會長更多。

**建議**：拆 3 個專責 Component，由 GameManager 組合：
- `PlayerLockController`（HashSet 鎖源、SetCanMove/Interact）
- `PlayerSpawnController`（Instantiate Prefab、SetPosition、Camera Follow）
- `SceneShortcuts`（GoToHumanScene / GoToMonsterScene / GoToNextDay）

GameManager 只保留「Singleton 入口 + 組合這些 Component」。

#### 🟠 P1：DataManager 過 God Object
**問題**：1289 行、15 個靜態字典、40+ public API、多個職責交疊（Game Data Registry + Player Save Proxy + ModifyAPI + Book 代理 + 結局/拍賣狀態）。

**建議**：拆分為
- `GameDataRegistry`（16 個 Dictionary + 唯讀存取）
- `PlayerStateManager`（_currentPlayerData + Modify API + GetPlayerSaveData）
- `BookDataProxy`（_bookData + SaveBookAsync，與 SaveManager 通訊）

DataManager 保留 Singleton 入口與三者組合。這項工程較大，但能讓 save-system 與 core-systems 兩份 skill 各自專注自己的層。

#### 🟡 P2：GameRng 未 Init 時 fallback 到 UnityEngine.Random
**問題**：若 `InitDailySeed` 未呼叫就使用 `RangeKeyed`，會退化成非固定隨機且不會明顯報錯，除錯困難。

**建議**：改為 `Debug.LogError` + 回傳固定 min 值（或拋例外，讓呼叫端察覺）；另外加 `IsInitialized` property 讓系統在啟動流程中顯式等待。

#### 🟡 P2：GameFlow 無法被測試
**問題**：GameFlow 直接呼叫 `GameRng.InitDailySeed` 靜態 + `DataManager.Instance.ModifyCurrentDay` + `SaveManager.Instance.SaveGameAsync`，無法 mock。

**建議**：抽介面 `IGameRngService` / `ISaveService`，GameFlow 建構子注入，方便寫單元測試驗證跨日邏輯。（若專案不打算寫測試可略過此項）

#### 🟢 P3：SaveCurrentPlayerAsync 與 GameFlow.SaveGameAsync 邏輯重複
兩處都走 `SaveManager.SaveGameAsync`，但 `GameFlow.SaveGameAsync` 多了 `OnPlayerDataChanged` 檢查與成就非同步存檔。建議統一由 DataManager 提供 `SaveAllAsync`，GameFlow 直接呼叫。

#### 🟢 P3：GameManager.InitializePlayerInScene 寫死 `new Vector3(0, -2, 0)`
建議改成由 scene-specific 的 spawn point 提供，或從 `PlayerSpawnPoint` Transform 讀。目前 `PlayerSpawnPoint` 欄位已宣告但未使用。

#### ✅ P3（已完成）：DataManager 命名統一
- ✅ `ShowPlayerMainData` → `RefreshPlayerMainView`（實際是 refresh view，非 show）
- ✅ `LoadMonsterTradeHistory` → `GetMonsterTradeHistory`（只從 dict 讀，沒落盤）
- ✅ `LoadPlayerFromSave` → `LoadCurrentPlayerFromSlot`（與 `SaveCurrentPlayerAsync(slot)` 對稱命名）
- ℹ `Update*SaveData`（3 個方法）保留，語意為「寫 dict + 同步 sync 落檔」，與 `SetPlayerData`（memory-only）不同。詳見下方動詞表

### 9.3 跨系統互動地圖

```
  GameRng.InitDailySeed
         ▲
         │ new GameFlow 時
         │
[GameManager] ──owns──▶ [GameFlow] ──reads──▶ [DataManager._currentPlayerData]
      │                     │                        │
      │                     │ SaveAsync              │ SetPlayerData / Modify*
      │                     ▼                        ▼
      │                 [SaveManager] ◀──writes── [_currentPlayerData]
      │                                                │
      │ LockPlayerMove / Spawn                         │ Fire events
      ▼                                                ▼
  [PlayerController]                          [GameFlowEvents / PlayerInfoUIEvents]
                                                       │
                                                       ▼
                                        [UI / Shop / Souvenir 等訂閱者]
```

### 9.4 與 save-system skill 的分工
- **core-systems（本 skill）**：runtime 操作 API、事件、生命期、鎖、RNG
- **save-system**：Slot/Book 雙管線、ISaveData 契約、磁碟 I/O、JSON 細節、SaveSlot UI

兩份 skill 會互相引用彼此的章節，遇到「怎麼新增一類存檔」要同時看 core-systems §4.3（API）+ save-system §9（磁碟面）。

---

## 10. 結局與拍賣保證金

本局是否抵達結局是「在 `GameFlow.SwitchGameStageAndSave` 切換到 Night 或 HumanDay 時」由 `EndingConditionDetector` 判定，並寫回 `PlayerData` 並立刻存檔。

### 10.1 PlayerData 旗標

| 欄位 | 型別 | 寫入點 |
|---|---|---|
| `HasReachedEnding` | bool | `DataManager.SetEndingReached(EndingType)` 在 GameFlow 偵測到結局時呼叫；`StartNewGame` 會清回 false |
| `ReachedEndingType` | `EndingType` | 同上；用 `EndingType.None` 表示尚未結局 |
| `HasPaidGuaranteeDeposit` | bool | `TryPayGuaranteeDeposit()` 成功支付後設 true |
| `HasPaidAuctionEntryFee` | bool | `TryPayAuctionEntryFee()` 成功支付後設 true |

### 10.2 流程
1. `GameFlow.SwitchGameStageAndSave(Night)` → `EndingConditionDetector.EvaluateForNewMonsterDay(player)`
2. `GameFlow.SwitchGameStageAndSave(HumanDay)` → `EndingConditionDetector.EvaluateForHumanDay(player)`
3. 任一回傳非 `EndingType.None` → `DataManager.SetEndingReached(endingType)` → `SaveGameAsync` → 提早 return
4. 下次 `GameManager.InitializeGame(slot)` 偵測到 `HasReachedEnding` → `GoToEndStoryScene()`

### 10.3 保證金 / 入場費
- `EndingConditionDetector.GuaranteeDepositAmount` / `EndingConditionDetector.AuctionEntryFeeAmount` 為靜態金額常數
- `DataManager.TryPayGuaranteeDeposit()` / `TryPayAuctionEntryFee()` 是冪等支付：已支付直接回 true、金幣不足回 false
- UI 顯示由 `GuaranteeDepositGuide` / `AuctionEntryFeeGuide` / `AuctionDayGuide` 三個 static class 的 `Refresh()` 驅動，GameFlow 切換階段時呼叫
- 對應 PlayerLockSources：`Auction` / `AuctionNpc` / `TelePointAuctionGuide`

### 10.4 重置點
- `GameManager.StartNewGame()` 才會清回 false / None；讀檔不會重置
- 進入結局後，舊存檔再讀取會直接 `GoToEndStoryScene()`，**不能**回頭繼續玩同一槽位 — 要重開新局
