---
name: save-system
description: 本專案（ForTest / 紅盒子）存檔系統的完整架構與使用守則。當使用者提到 SaveManager、DataManager、GameDataLoader、存檔、讀檔、slot、save_slot、illustrated_book、圖鑑 (BookData、ItemBookData、MonsterBookData)、成就存檔 (AchievementData、IAchievementSave)、特殊紀念品 (SpecialSouvenirProgressData、ISpecialSouvenirSave、Sou_key、UnLockSpecialSouvenirID)、ISaveData、GameSaveFile、PlayerData、SaveSlot UI / SaveSlotPresenter、存檔管理面板 (SaveDataManagerPanel)、JSON 序列化、Newtonsoft.Json、TypeNameHandling、$type、persistentDataPath、AppData、LocalLow、清空/刪除存檔、重構存檔系統、存檔效能、存檔測試、存檔安全 / 加密 / 簽章，或要新增任何一種「需要跨單局持久化」的資料時載入此 skill。
---

# 存檔系統完整文件

## 0. TL;DR（先看這裡）

**一句話**：本系統採「Slot 單局 + Book 跨局」雙存檔管線，由 `SaveManager` 統一檔案 I/O，`DataManager` 持有活引用做 runtime 操作；兩者共享同一個 `GameSaveBook` 物件。

**決策樹（我要做 X → 直接跳到哪一節）**：

| 任務 | 跳到 |
|---|---|
| 新增單局內模組資料（每日重置 / 跨日累積） | §9.1 + §9.5 範例 1 |
| 新增成就 | §9.2 |
| 新增特殊紀念品 + 跨局進度 | §9.3 |
| 新增存檔管理面板按鈕 | §9.4 + §9.5 範例 5 |
| 查公開 API 簽章 | §7.5 |
| 碰到 JSON `Could not create instance` 或圖鑑讀不出 | §10.5 + §4 |
| 玩家回報「重啟後資料消失」 | §10 + §10.5 |
| 想手動備份 / 還原存檔 | §11.5 |
| 重構、效能、測試、加密 | 讀 `Docs/SaveSystem_Review.md`（見 §12） |
| 想看系統架構圖（Mermaid） | 讀 `Docs/SaveSystem_Architecture.md`（見 §12） |

**維護提示**：
- 本檔是 AI Agent 的檢索入口，**不要加 Mermaid 圖**（圖表請編輯 `Docs/SaveSystem_Architecture.md`）。
- 深度重構建議與 P0–P3 改進清單放在 `Docs/SaveSystem_Review.md`，不塞進本檔。

---

## 1. 兩條存檔管線（雙檔並行）

本專案把持久化資料切成兩條獨立管線，各自有檔案、各自有 API，**不可混用**。

| 管線 | 範圍 | 檔名格式 | Key 類別 |
|---|---|---|---|
| **Slot 存檔 (單局)** | 一局遊戲內的玩家資料 (Gold、Day、Inventory、交易紀錄…) | `save_slot_{N}.json` | `SaveFileData { PlayerData }` |
| **Book 存檔 (跨局)** | 圖鑑 + 成就 + 特殊紀念品進度 + 永久解鎖紀念品 ID | `illustrated_book.json` | `GameSaveBook` |

- 兩檔都放在 `Application.persistentDataPath`。
- Windows 位置：`%USERPROFILE%/AppData/LocalLow/<Company>/<Product>/`
- `SaveManager.OpenSaveFolder()` 會用 `explorer.exe` 打開該資料夾。

## 2. 關鍵檔案

| 路徑 | 角色 |
|---|---|
| [Assets/GameData/GameSystem/SaveManager.cs](Assets/GameData/GameSystem/SaveManager.cs) | Singleton。唯一的「檔案 I/O 入口」。持有 `_cachedBookData` / `_achievementDict` / `_specialSouvenirDict`。 |
| [Assets/GameData/GameSystem/DataManager.cs](Assets/GameData/GameSystem/DataManager.cs) | Singleton。Runtime 資料中樞。持有 `_currentPlayerData` / `_bookData` / `_achievementSaveDict` / `_specialSouvenirSaveDict` 的**活引用**（和 SaveManager 共用同一個物件）。 |
| [Assets/GameData/GameSystem/GameDataLoader.cs](Assets/GameData/GameSystem/GameDataLoader.cs) | 啟動時讀靜態設定 + Book 存檔的 Loader（被 DataManager.InitializeAsync 呼叫）。 |
| [Assets/GameData/GameSettingData/GameDataBase.cs](Assets/GameData/GameSettingData/GameDataBase.cs) | `PlayerData` / `GameSaveBook` / `ItemBookData` / `MonsterBookData` / `Inventory` 等 POCO。 |
| [Assets/GameData/GameSettingData/ISaveData.cs](Assets/GameData/GameSettingData/ISaveData.cs) | `GameSaveFile`（局內按 key 存取的 dict）+ `ISaveData` 契約 + 各種 `MissionSaveData`、`OrderHistoryData`、`ShopShelfData` 等實作。 |
| [Assets/Script/Achievement/AchievementEvents.cs](Assets/Script/Achievement/AchievementEvents.cs) | `IAchievementSave` / `IAchievementWithProgress` 介面。 |
| [Assets/Script/Souvenir/Souvenir.cs](Assets/Script/Souvenir/Souvenir.cs) | `ISpecialSouvenirSave` 介面（`extends IAchievementWithProgress` + 加 `SouvenirID`）。 |
| [Assets/Script/UI/SaveSlotUI.cs](Assets/Script/UI/SaveSlotUI.cs) / [SaveSlotPresenter.cs](Assets/Script/UI/SaveSlotPresenter.cs) | MVP 模式主選單存檔列表。View 發事件，Presenter 接 `SaveManager`。含 prefab 內建刪除按鈕（child 3）+ 刪除二次確認面板。 |
| [Assets/Script/UI/SaveDataManagerPanel.cs](Assets/Script/UI/SaveDataManagerPanel.cs) | 存檔管理面板（清空圖鑑 / 解鎖全部圖鑑 / 解鎖全部成就與特殊紀念品 / 清空所有存檔 / 開啟資料夾）。所有破壞性操作走 `ShowConfirm` 二次確認。 |

## 3. 資料模型總覽

### 3.1 Slot 存檔：`SaveFileData`
```csharp
SaveFileData
 └ Player : PlayerData
    ├ ID / MasterSeed / DaysPlayed / CustomerIndex
    ├ PlayingStatus : DayPhase / IsTrade : bool
    ├ Gold / MonsterGold : int
    ├ Inventory { List<Item> Items }
    ├ HoldAchievementSouvenirID : List<string>   // 當局持有的成就紀念品
    └ GameSaveFile { Dictionary<string, ISaveData> GameData }  // 局內按 key 寫入的任意模組資料
```

`GameSaveFile.GameData` 是模組化擴充點：任何類別實作 `ISaveData { UniqueID, LastUpdatedDay }` 就能透過 `DataManager.SetPlayerData<T>(key, data)` / `GetPlayerSaveData<T>(key)` / `GetPersistentSaveData<T>(key)` 存取。差別：
- `GetPlayerSaveData<T>`：跨日 (`LastUpdatedDay != DaysPlayed`) 會回傳 `new T()`，用於「每日重置」資料。
- `GetPersistentSaveData<T>`：不重置，用於「跨日累積」資料。

### 3.2 Book 存檔：`GameSaveBook`
```csharp
GameSaveBook
 ├ ItemBookData { List<ItemBookDatabase> ItemBooks }                // 物品圖鑑
 ├ MonsterBookData
 │   ├ UnlockMonsterInformationID   // 已解鎖妖怪情報
 │   ├ NewMonsterInformationID      // 紅點：新情報待確認
 │   └ NewMonsterStoryID            // 紅點：新故事待確認（每 2 個情報解鎖 1 個故事）
 ├ AchievementData : List<IAchievementSave>                          // 成就進度
 ├ UnLockAchievementSouvenirID : List<string>                        // 永久持有的成就紀念品（用成就點兌換）
 ├ UnLockSpecialSouvenirID : List<string>                            // 永久持有的特殊紀念品（含預設 "Sou_key"）
 └ SpecialSouvenirProgressData : List<ISpecialSouvenirSave>          // 特殊紀念品進度追蹤
```

## 4. JSON 序列化（**關鍵設定**）

因為 `AchievementData` 是 `List<IAchievementSave>`、`SpecialSouvenirProgressData` 是 `List<ISpecialSouvenirSave>`，序列化必須把**具體型別資訊**寫進 JSON 否則反序列化會失敗。所有讀寫都用同一組設定：

```csharp
private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.Auto,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
};
```

- `TypeNameHandling.Auto` → JSON 中會出現 `$type` 欄位，反序列化時自動還原為具體類別。
- 若跨 Assembly 搬檔，`$type` 含命名空間；重構時小心破壞舊存檔。
- `LoadSlotInfo` 也必須帶入此設定（曾因遺漏拋 "Could not create instance..." 例外）。

## 5. 啟動流程（Init Path）

```
Scene 載入 → SaveManager.Awake()              ← _cachedBookData = LoadBookData() 直接讀 illustrated_book.json
            → DataManager.Awake() → InitializeAsync()
                 → GameDataLoader.LoadAllGameDataAsync()
                    - 載入所有靜態設定 (ItemDict, AchievementDict, SpecialSouvenirDict, …)
                    - 再讀一次 BookData（為了 PlayerData 預設值等）
                 → SaveManager.SetBookDataCache(_bookData)             ← 雙方指向同一個 GameSaveBook 實例
                 → _achievementSaveDict = SaveManager.GetAchievementDict()
                 → _specialSouvenirSaveDict = SaveManager.GetSpecialSouvenirDict()
                 → InitializeProgressManagers()
                    - AchievementManager.Initialize(configDict)       ← 用反射找 namespace=AchievementLibrary 的 AchievementBase 子類
                    - SouvenirManager.Initialize()
                 → _currentPlayerData = Clone(_initialPlayerData)
            → SaveManager.WhenInitialized() 對外可 await
```

**重點**：`SaveManager._cachedBookData` 和 `DataManager._bookData` 是**同一個物件**。任一側 mutate 都會互相看見。`DataManager.GetBookData()` 回傳的是活引用，直接改會立刻影響記憶體狀態，但要持久化**一定要走 `SaveManager.SaveBookData(bookData)`**。

## 6. 寫檔路徑

### 6.1 PlayerData（Slot 存檔）
- `DataManager.SaveCurrentPlayerAsync(slot)` → `SaveManager.SaveGameAsync(playerData, slot)` → `save_slot_{slot}.json`
- 內部用 `SemaphoreSlim _saveLock`（1,1）序列化寫檔；對外暴露 `IsSaving => _saveLock.CurrentCount == 0`。
- 透過 JSON clone 保存，所以存進去的是 snapshot 非活引用。

### 6.2 BookData（Book 存檔）
- 同步：`SaveManager.SaveBookData(bookData)`（多數流程走同步，因圖鑑變動頻繁且小）。
- 非同步：`SaveManager.SaveBookDataAsync(bookData)`（有 `IsSavingBook` 旗標）。
- `DataManager` 內每個 mutator（`UnlockMonsterInformation` / `UnlockRandomMonsterInformation` / `AddItemToBook` / `ConfirmSingleNewInfo` / `ConfirmSingleNewStory` / `ConfirmMonsterNewInfo` / `UpdateAchievementSaveData` / `UpdateAllAchievementSaveData` / `UpdateSpecialSouvenirSaveData` …）都會在改完之後 `SaveBookData(_bookData)` 立即落檔 + `OnBookDataChanged = true` + 觸發 `BookDataChanged` event。

### 6.3 成就 / 特殊紀念品的雙層字典
`SaveManager` 內部維護 `_achievementDict` / `_specialSouvenirDict`（Dictionary by ID），存檔時 `DictToList(...)` 寫回 `bookData.AchievementData / SpecialSouvenirProgressData`。`DataManager._achievementSaveDict` 引用同一個 Dictionary 物件，任一側新增 key 另一側也看得見。

## 7. 存檔管理 API（`SaveManager` 破壞性操作）

所有都在 `#region 存檔管理 API`：

| 方法 | 行為 | 備註 |
|---|---|---|
| `DeleteSaveSlot(slot)` | 刪 `save_slot_{slot}.json` | 不動 Book 存檔。 |
| `ClearBookData(bool reloadMainMenu = true)` | Book 存檔整檔重置為空（僅保留預設 `Sou_key`）；同步呼叫 `DataManager.ClearBookDataCache()` + `InitializeProgressManagers()` | 破壞性。預設會 `SceneTransitionManager.GoToMainMenu()`。 |
| `UnlockAllBookData(bool reloadMainMenu = true)` | 把 `ItemDict` / `MonsterInfoDict` 全解鎖 | 走 `SaveBookData` + `SetBookDataChanged(true)`。 |
| `UnlockAllAchievementsAndSpecialSouvenirs(bool reloadMainMenu = true)` | 透過 `AchievementManager.GetIncompleteAchievements()` 全設 `IsCompleted=true` + 填 `FinishDay`；透過 `SouvenirManager.GetAllSpecialSouvenirs()` 全設 `IsCompleted=true` + 加進 `UnLockSpecialSouvenirID` | **直接設 `IsCompleted=true` 而非 `CompletedAchievement()`，為了抑制 `OnUnlocked` 事件避免彈窗轟炸**。 |
| `ClearAllSaves()` | 刪所有 `save_slot_*.json` + `ClearBookData()` + 清 `_lastLoaded` | 大核彈。 |
| `OpenSaveFolder()` | OS 檔案總管打開存檔資料夾 | 非破壞，無需確認。 |

## 7.5 公開 API 速查表

**欄位說明**：✅=會直接落檔；⏳=同步；🔄=非同步；💥=重新載入場景；⚠️=有防連點旗標；🌐=需 `DataManager` 就緒。

### `SaveManager`

| 方法 | 同步性 | 落檔 | 備註 |
|---|:---:|:---:|---|
| `SaveGameAsync(PlayerData, int slot = 0)` | 🔄 | ✅ | ⚠️ `IsSaving` 防連點 |
| `Load(int slot = 0) → SaveFileData` | ⏳ | — | 檔案不存在時以 `DataManager.InitialPlayerData` 建立並回傳 clone |
| `LoadSlotInfo(int slot) → SaveSlotData` | ⏳ | — | 給 UI 顯示用；內部必須帶 `_jsonSettings`，否則拋 `Could not create instance` |
| `GetNextAvailableSlot(int maxSlots = 10) → int` | ⏳ | — | 全滿時回 `maxSlots`（越界，呼叫方需檢查） |
| `LastLoaded → SaveFileData` | ⏳ | — | 返回 clone；**若從未 `Load` 過會是 null** |
| `SaveBookDataAsync(GameSaveBook)` | 🔄 | ✅ | ⚠️ `IsSavingBook` 防連點 |
| `SaveBookData(GameSaveBook)` | ⏳ | ✅ | 沒有鎖定；I/O 頻繁時注意 |
| `SetBookDataCache(GameSaveBook)` | ⏳ | — | 同時重建 `_achievementDict` / `_specialSouvenirDict` |
| `GetBookDataCache() → GameSaveBook` | ⏳ | — | 活引用，直接 mutate 不會自動存檔 |
| `GetAchievementDict() → Dict<string, IAchievementSave>` | ⏳ | — | 活引用 |
| `SaveAchievementData(dict)` / `SaveAchievementDataAsync(dict)` | ⏳/🔄 | ✅ | 內部轉 List → `SaveBookData` |
| `GetSpecialSouvenirDict() → Dict<string, ISpecialSouvenirSave>` | ⏳ | — | 活引用 |
| `SaveSpecialSouvenirData(dict)` / `SaveSpecialSouvenirDataAsync(dict)` | ⏳/🔄 | ✅ | 同上 |
| `ClearBookData(bool reloadMainMenu = true)` | ⏳ | ✅ | 💥 + 重置 Manager |
| `UnlockAllBookData(bool reloadMainMenu = true)` | ⏳ | ✅ | 💥 🌐 |
| `UnlockAllAchievementsAndSpecialSouvenirs(bool reloadMainMenu = true)` | ⏳ | ✅ | 💥 🌐 直接 `IsCompleted=true` 抑制事件 |
| `DeleteSaveSlot(int slot) → bool` | ⏳ | ✅ | 不動 Book |
| `ClearAllSaves()` | ⏳ | ✅ | 刪所有 slot + `ClearBookData()` |
| `OpenSaveFolder()` | ⏳ | — | OS 檔案總管 |

### `DataManager`

| 方法 | 同步性 | 落檔 | 備註 |
|---|:---:|:---:|---|
| `InitializeAsync()` / `WhenInitialized() → Task` | 🔄 | — | 對外 await 點**在 DataManager 上**（不在 SaveManager） |
| `IsInitialized → bool` | ⏳ | — | 啟動守衛 |
| `LoadCurrentPlayerFromSlot(int slot = 0)` | ⏳ | — | 由 Presenter 呼叫；內部走 `SaveManager.Load(slot)` + clone 後覆蓋 `_currentPlayerData`，不落檔 |
| `SaveCurrentPlayerAsync(int slot)` | 🔄 | ✅ | 包住 `SaveManager.SaveGameAsync` |
| `GetBookData() → GameSaveBook` | ⏳ | — | 活引用（與 SaveManager 同一物件） |
| `SetBookDataChanged(bool)` | ⏳ | — | 供 UI 紅點 / 存檔判斷用 |
| `ClearBookDataCache()` | ⏳ | — | 由 `SaveManager.ClearBookData` 呼叫 |
| `InitializeProgressManagers()` | ⏳ | — | 重建 `AchievementManager` / `SouvenirManager` 狀態 |
| `SetPlayerData<T>(string key, T data) where T : ISaveData` | ⏳ | — | 設 `OnPlayerDataChanged=true`；**不自動落檔**，需呼叫 `SaveCurrentPlayerAsync` |
| `GetPlayerSaveData<T>(string key) → T` | ⏳ | — | **跨日回新實例**（依 `LastUpdatedDay`） |
| `GetPersistentSaveData<T>(string key) → T` | ⏳ | — | **跨日保留** |
| `UnlockMonsterInformation(string)` / `UnlockRandomMonsterInformation()` / `AddItemToBook(string)` | ⏳ | ✅ | 內部呼叫 `SaveManager.SaveBookData`；前者自動處理「每 2 個情報→新故事」門檻並寫紅點 |
| `ConfirmSingleNewInfo(id)` / `ConfirmSingleNewStory(id)` / `ConfirmMonsterNewInfo(monsterId)` | ⏳ | ✅ | 清紅點；有實際移除才落檔 |
| `IsMonsterInfoUnlocked(id)` / `HasAnyNewMonsterInfo()` / `HasNewMonsterInfo(monsterId)` | ⏳ | — | 圖鑑紅點查詢 |
| `UpdateAchievementSaveData(IAchievementSave)` | ⏳ | ✅ | 寫入 `_achievementDict` 並落檔 |
| `UpdateAllAchievementSaveData()` | ⏳ | ✅ | 從 `AchievementManager` 撈所有已完成 + 未完成成就，批次寫 dict 並落檔 |
| `UpdateSpecialSouvenirSaveData(ISpecialSouvenirSave)` | ⏳ | ✅ | 寫入 `_specialSouvenirDict` 並落檔 |
| `BookDataChanged` event | ⏳ | — | 圖鑑/成就/紀念品任一筆改動會 fire，UI 紅點訂閱此事件 |
| `ModifyGold(int)` / `ModifyMonsterGold(int)` / `TrySpendGold(int) → bool` | ⏳ | — | 僅改記憶體；觸發 `AchievementEvents.OnGoldChanged` |

---

## 8. UI 層

### 8.1 `SaveSlotUI` + `SaveSlotPresenter` (MVP)
- Prefab 子物件順序（硬編碼）：`child0`=slotText、`child1`=dayText、`child2`=goldText、`child3`=刪除按鈕。
- View 事件：`OnSlotSelected(slotIndex, isEmpty)`、`OnSlotDeleteRequested(slotIndex)`、`OnRefreshRequested`。
- Presenter 處理選取 → `DataManager.LoadCurrentPlayerFromSlot` + `GameManager.InitializeGame`；刪除 → `SaveManager.DeleteSaveSlot` + 刷新。
- **刪除二次確認**：View 內建 `deleteConfirmPanel` + `_pendingDeleteSlotIndex`；按刪除先彈面板，確認才觸發 `OnSlotDeleteRequested`。
- `LoadAndDisplaySaveSlots()` 只把非空 slot 加入 list（空 slot 不顯示）。

### 8.2 `SaveDataManagerPanel`
所有破壞性按鈕（`clearBookButton` / `unlockAllBookButton` / `unlockAllAchievementsButton` / `clearAllSavesButton`）走同一 `ShowConfirm(message, Action)` 管線：把 `_pendingAction` 存起來，按確認時才 `action?.Invoke()`。`openSaveFolderButton` 直接執行不確認。

## 9. 擴充配方

### 9.1 加一種「每日重置」的局內資料
1. 新類別 `implements ISaveData { UniqueID; LastUpdatedDay; }`，加上 `[System.Serializable]`。
2. 寫入：`DataManager.SetPlayerData("MyKey", data)`（會自動設 `OnPlayerDataChanged = true`）。
3. 讀取：`DataManager.GetPlayerSaveData<MyData>("MyKey")`（跨日回新實例）。
4. 不需動 `SaveManager`；會隨 `PlayerData.GameSaveFile.GameData` 自動被序列化。
5. JSON 會有 `$type` — 沒問題。

### 9.2 加一種新的成就
1. 在 `namespace AchievementLibrary` 下寫一個 `: AchievementBase` 的非 abstract class，`AchievementID` 對應 `AchievementConfig` 的 key。
2. `AchievementManager.Initialize` 用反射自動撈（namespace 必須精確是 `AchievementLibrary`）。
3. 無須手動註冊存檔欄位。

### 9.3 加一種特殊紀念品 + 跨局進度
1. `: SpecialSouvenir` 並 implement `ISpecialSouvenirSave`（回傳 `SouvenirID`、顯示欄位、`IsCompleted`）。
2. 由 `SouvenirManager` 管理生命週期。
3. 要進度寫檔時走 `DataManager.UpdateSpecialSouvenirSaveData(saveData)`。

### 9.4 加一個存檔管理按鈕
1. 在 `SaveManager` 實作業務邏輯（參考 `UnlockAllBookData` 樣板）：`GetBookData()` → mutate → `SaveBookData` → `SetBookDataChanged(true)` → 視情況 `SceneTransitionManager.GoToMainMenu()`。
2. `SaveDataManagerPanel`：加 `[SerializeField] Button xxx` 欄位、Awake/OnDestroy 加 AddListener/RemoveListener、寫 `OnClickXxx` 用 `ShowConfirm` 包裝呼叫。

## 9.5 常用程式碼範例

### 範例 1：新增一個「每日重置」的局內模組資料
```csharp
// 1. 宣告（同檔或另開檔）
[System.Serializable]
public class CustomerFavorData : ISaveData
{
    public string UniqueID => "CustomerFavorData";
    public int LastUpdatedDay { get; set; }
    public int Favor;
}

// 2. 寫入（修改後呼叫，自動 OnPlayerDataChanged=true）
var favor = DataManager.Instance.GetPlayerSaveData<CustomerFavorData>("CustomerFavorData"); // 跨日回新實例
favor.Favor += 5;
favor.LastUpdatedDay = DataManager.Instance.CurrentPlayerData.DaysPlayed;
DataManager.Instance.SetPlayerData("CustomerFavorData", favor);
// 注意：SetPlayerData 只改記憶體；最後仍要 DataManager.SaveCurrentPlayerAsync(slot) 才落檔
```
若需跨日累積改 `GetPersistentSaveData<T>("Key")`。

### 範例 2：解鎖一個物品圖鑑項目
```csharp
// AddItemToBook 內部會 SaveBookData 立刻落檔
DataManager.Instance.AddItemToBook("Item_Herb01");
// 驗證：
bool booked = DataManager.Instance.GetBookData()
    .ItemBookData.ItemBooks.Exists(x => x.ItemID == "Item_Herb01" && x.IsBooked);
```

### 範例 3：更新一個成就進度
```csharp
// 若成就繼承 IAchievementWithProgress，由 AchievementLibrary 的具體類別負責 SaveData
public class SellItemAchievement : AchievementBase, IAchievementWithProgress
{
    public int CurrentProgress { get; set; }
    public int TargetProgress => 100;
    protected override void SubscribeEvents() => AchievementEvents.OnTransactionCompleted += OnSold;
    private void OnSold(string itemId, string buyerId)
    {
        if (IsCompleted) return;
        CurrentProgress++;
        if (CurrentProgress >= TargetProgress) CompletedAchievement();
        SaveData();
    }
    protected override void SaveData() => DataManager.Instance.UpdateAchievementSaveData(this);
    protected override void UnsubscribeEvents() => AchievementEvents.OnTransactionCompleted -= OnSold;
}
```

### 範例 4：更新特殊紀念品進度
```csharp
public class TradeMilestoneSouvenir : SpecialSouvenir, ISpecialSouvenirSave
{
    public string SouvenirID => "Sou_TradeMilestone";
    public int CurrentProgress { get; set; }
    public int TargetProgress => 50;
    public override void Register() => AchievementEvents.OnTransactionCompleted += OnTrade;
    private void OnTrade(string a, string b)
    {
        if (IsCompleted) return;
        CurrentProgress++;
        if (CurrentProgress >= TargetProgress) TryCollect();
        DataManager.Instance.UpdateSpecialSouvenirSaveData(this); // 落檔
    }
}
```

### 範例 5：手動觸發完整存檔（Day End 結算、離開前）
```csharp
// 結算流程呼叫；Book 資料改過就順帶落檔
await DataManager.Instance.SaveCurrentPlayerAsync(slot); // Slot 存檔
if (DataManager.Instance.OnBookDataChanged)
{
    SaveManager.Instance.SaveBookData(DataManager.Instance.GetBookData()); // Book 存檔
    DataManager.Instance.SetBookDataChanged(false);
}
```

---

## 10. 常見陷阱

- **介面集合序列化**：任何 `List<I某某>` 一定要用 `_jsonSettings`（含 `TypeNameHandling.Auto`）序列化，否則反序列化會回傳 null 或拋例外。
- **Book 存檔的快取同步**：不要自己 `new GameSaveBook()` 覆蓋 `_bookData`；要走 `SaveManager.SetBookDataCache(newBook)` 讓兩個 Manager 共享引用。
- **bulk 解鎖不要用事件**：`UnlockAllAchievementsAndSpecialSouvenirs` 直接 `IsCompleted = true` 而不是 `CompletedAchievement()`，避免 `OnUnlocked` 累計 N 次造成 UI 彈窗轟炸 / 存檔 I/O 爆炸。
- **`ClearAllSaves` 一定連 Book 一起清**：若只想清 Slot 請用迴圈呼叫 `DeleteSaveSlot`，不要改 `ClearAllSaves` 的語意。
- **`Sou_key` 預設紀念品**：`ClearBookData` 重置後仍會保留 `"Sou_key"` 於 `UnLockSpecialSouvenirID` 中，對應的是 `DefaultOwnedSouvenirBase` 子類。對應邏輯：`DefaultOwnedSouvenirBase` 不 implement `ISpecialSouvenirSave`，**不會**被寫進 `SpecialSouvenirProgressData`，所以 bulk 解鎖時只會進到 `UnLockSpecialSouvenirID`。
- **`LoadSlotInfo` 強制轉型 `(DayPhase)saveData.Player.PlayingStatus`**：Enum 定義不能變，否則舊存檔錯位。
- **`GetNextAvailableSlot(maxSlots = 10)`**：若所有 slot 都用掉，回傳 `maxSlots`（越界值，呼叫方要自己檢查邊界）。

## 10.5 除錯與故障排除

優先查這張表；若都不符，再去讀 `Docs/SaveSystem_Review.md` 看是否命中已知 P0/P1 清單。

| 症狀 / Log 訊息 | 根因 | 定位點 / 修法 |
|---|---|---|
| `Could not create instance of type ...` | 反序列化沒帶 `_jsonSettings`（缺 `TypeNameHandling.Auto`） | 檢查該次 `JsonConvert.DeserializeObject` 是否帶入 `_jsonSettings`；典型發生於新寫的 `LoadSlotInfo` 類工具 |
| `LastLoaded` 取回 null / `LastLoaded.Player` NullRef | 從未 `Load()` 過就直接讀 | 先 `Load(slot)` 或改用 `DataManager.CurrentPlayerData`；也見 Review §1.2 |
| Book mutate 後重啟資料丟失 | 只改了 `_bookData` 但沒呼叫 `SaveBookData` | DataManager 新寫的 mutator 結尾要 `SaveManager.Instance.SaveBookData(_bookData)` |
| DataManager 與 SaveManager 看到不同 BookData | 某處自己 `new GameSaveBook()` 覆蓋了引用 | 改走 `SaveManager.SetBookDataCache(newBook)` 保持共享 |
| Bulk 解鎖成就 / 紀念品時 UI 彈窗連環炸 | 呼叫了 `CompletedAchievement()` 觸發 `OnUnlocked` | 改直接 `IsCompleted = true`；範本見 `UnlockAllAchievementsAndSpecialSouvenirs` |
| `GetNextAvailableSlot()` 回 10（越界） | 全滿 | 呼叫端要檢查 `>= maxSlots` 並顯示「滿了」提示 |
| `AchievementManager` 掃不到你新寫的成就 | namespace 不是精確 `AchievementLibrary` | 確認 `namespace AchievementLibrary { ... }` 完全一致（不是 `AchievementLib` / `Achievement.Library`） |
| 改 `DayPhase` enum 順序後舊存檔「剩餘天數」錯亂 | `LoadSlotInfo` 以 `(DayPhase)int` 強制轉型 | Enum 只能 append 新值、不可重編號；或跑遷移 |
| `Sou_key` 在 `ClearBookData` 後不見了 | `ClearBookData` 被錯改，未保留 `UnLockSpecialSouvenirID = new List<string> { "Sou_key" }` | 比對 SaveManager.cs 中 `ClearBookData` 的初始化片段 |
| `$type` 看起來對但仍拋 JsonSerializationException | 類別被搬 namespace / 換 assembly 名 | 舊存檔的 `$type` 字串沒跟上；寫一次性遷移腳本或 `SerializationBinder` |
| 圖鑑存檔頻繁造成卡頓 | 每個 mutator 即時同步寫檔 | 參考 `Docs/SaveSystem_Review.md` §3.3 批量延遲方案 |
| `Load()` 讀到腐敗 JSON 後，玩家繼續玩覆蓋原檔 | `catch` 僅 LogError + fallback，未備份 | Review §3.4；暫解：先複製 `persistentDataPath` 的 `.json` 再玩 |

## 11. 路徑快速導覽

```
SaveManager       → Assets/GameData/GameSystem/SaveManager.cs
DataManager       → Assets/GameData/GameSystem/DataManager.cs
GameDataLoader    → Assets/GameData/GameSystem/GameDataLoader.cs
Singleton<T>      → Assets/GameData/GameSystem/Singleton.cs
SceneTransitionManager → Assets/GameData/GameSystem/SceneTransitionManager.cs

PlayerData / GameSaveBook / Inventory / Item / *BookData*  → Assets/GameData/GameSettingData/GameDataBase.cs
ISaveData / GameSaveFile / MissionSaveData / OrderHistoryData / ShopShelfData → Assets/GameData/GameSettingData/ISaveData.cs

IAchievementSave / IAchievementWithProgress / AchievementBase → Assets/Script/Achievement/
SpecialSouvenir / ISpecialSouvenirSave / SouvenirManager     → Assets/Script/Souvenir/

SaveSlotUI / SaveSlotPresenter       → Assets/Script/UI/
SaveDataManagerPanel                 → Assets/Script/UI/SaveDataManagerPanel.cs
```

## 11.5 OS 層級檔案位置

Unity 的 `Application.persistentDataPath` 在三大平台對應：

| 平台 | 路徑 |
|---|---|
| Windows | `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\` |
| macOS | `~/Library/Application Support/<Company>/<Product>/` |
| Linux | `~/.config/unity3d/<Company>/<Product>/` |

`<Company>` / `<Product>` 來自 `Edit → Project Settings → Player`。

資料夾內預期檔案：

```
save_slot_0.json
save_slot_1.json
...
save_slot_9.json            # 最多 10 個 slot（GetNextAvailableSlot 上限）
illustrated_book.json       # Book 存檔（唯一）
```

**手動備份 / 還原流程**：
1. 關閉遊戲（避免寫檔中）。
2. 執行時點 `SaveDataManagerPanel → OpenSaveFolder`，或手動開上表路徑。
3. 整包複製整個資料夾作為備份。
4. 還原時把備份整包覆蓋回去，重啟遊戲。**不要只還原單一 JSON**，因為 Slot 和 Book 相互參照（例如 `HoldAchievementSouvenirID` 指向 Book 內資料）。

## 12. 相關文件索引

兩份配套文件，按需載入：

| 檔案 | 內容 | 何時讀 |
|---|---|---|
| [Docs/SaveSystem_Architecture.md](../../../Docs/SaveSystem_Architecture.md) | 9 張 Mermaid 圖（元件關係 / 雙管線結構 / 資料介面 / 啟動流程 / 寫檔路徑 / MVP 互動 / JSON 策略 / 破壞性操作 / 擴充指引） | 要視覺化架構、向他人解釋、做 onboarding 時 |
| [Docs/SaveSystem_Review.md](../../../Docs/SaveSystem_Review.md) | 13 項 P0~P3 改進建議（含 C# 骨架）+ Roadmap + 迴歸測試 Checklist | 使用者問「重構 / 效能 / 測試 / 存檔安全 / 腐敗檔備份 / JsonSettings 重複 / 反射快取 / 批量延遲寫檔」等深度議題時必讀 |

> **維護規則**：本 SKILL.md 是 AI Agent 首選入口，**不放 Mermaid**（圖表一律寫到 Architecture.md）。深度重構建議、P0~P3 清單一律寫到 Review.md 保持 SKILL.md 行數 ≤ 500。
