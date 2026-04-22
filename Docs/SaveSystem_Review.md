# Save System — 審查報告與改進建議

> 對應版本：2026-04-15
> 專案：RED / ForTest
> 事實來源：`Assets/GameData/GameSystem/SaveManager.cs` (797 行) / `DataManager.cs` (1162 行) / `GameDataLoader.cs` (817 行)
> 交叉參照：[SaveSystem_Architecture.md](SaveSystem_Architecture.md)（視覺架構圖）

---

## 0. 文件目的與閱讀指引

本文件**僅提出改進建議與重構骨架**，不包含已修復內容。適用情境：

- 排程重構、效能最佳化、安全性強化時優先閱讀。
- 為新進工程師提供「目前系統有哪些已知缺陷」的清單。
- Claude 在被問到「重構」「效能」「Bug 風險」「測試覆蓋」「加密/簽章」等話題時應主動讀取此檔。

### 優先度分類

| 層級 | 語意 | 行動時機 |
|---|---|---|
| **P0** | 資料正確性 / Crash 風險 | 下一個 Sprint 必做 |
| **P1** | 效能、耦合、維護性 | 本季度內處理 |
| **P2** | 最佳化、I/O、可讀性 | 條件性：觸發抱怨或重構順手做 |
| **P3** | 安全、測試、長期資產 | 納入年度 Roadmap |

### 每條條目統一欄位

- **症狀**：使用者或開發者會觀察到什麼
- **影響範圍**：牽連的模組 / 場景
- **根因**：精確的程式碼位置與設計問題
- **建議改法**：附 ≤15 行 C# 骨架
- **風險 / 向下相容**：改動是否會破壞既有存檔
- **預估工時**：給 Scrum 規劃參考

---

## 1. P0 — 關鍵（資料正確性 / Crash 風險）

### 1.1 SaveManager.Load() 同步路徑無鎖定機制

- **症狀**：玩家快速點擊 Slot 按鈕連續觸發讀檔，或場景載入時讀檔與其他系統存檔重疊，可能得到不一致快照或檔案殘缺。
- **影響範圍**：`SaveSlotPresenter` → `DataManager.LoadPlayerFromSave` → `SaveManager.Load()`；`SaveSlotUI` 的多次 click spam。
- **根因**：[SaveManager.cs:85-121](../Assets/GameData/GameSystem/SaveManager.cs#L85-L121) 的 `Load(int slot)` 是純同步 `File.ReadAllText`，沒有 `IsLoading` 或 lock 物件；與 `SaveGameAsync` 的 `IsSaving` 保護**不對稱**。若同 slot 正在非同步寫入 (`IsSaving == true`) 而 UI 觸發 `Load()` 時仍會讀到半寫檔。
- **建議改法**：
  ```csharp
  private readonly object _ioLock = new object();

  public SaveFileData Load(int slot = 0)
  {
      lock (_ioLock)
      {
          // 既有讀檔邏輯
      }
  }
  // SaveGameAsync 內的 File.WriteAllTextAsync 前後包
  lock (_ioLock) { /* 序列化 */ }
  await File.WriteAllTextAsync(filePath, json); // 移到 lock 外 (async 無法在 lock 內 await)
  ```
  或採 `SemaphoreSlim` 支援非同步：
  ```csharp
  private readonly SemaphoreSlim _ioSemaphore = new SemaphoreSlim(1, 1);
  ```
- **風險 / 向下相容**：無存檔格式變動，純內部保護。Semaphore 方案要確保 `Release` 被 `finally` 正確執行。
- **預估工時**：2 小時（含手動測試連點場景）。

### 1.2 LastLoaded 可能返回 null 造成 NullReferenceException

- **症狀**：呼叫 `SaveManager.Instance.LastLoaded.Player` 時丟 NullRef。
- **影響範圍**：任何從 SaveManager 取資料的模組（尤其是在 `Awake` 階段、`Load()` 尚未執行時）。
- **根因**：[SaveManager.cs:49](../Assets/GameData/GameSystem/SaveManager.cs#L49) `public SaveFileData LastLoaded => CloneData(_lastLoaded);`，但 `_lastLoaded` 只在 `Load()` / 讀檔失敗時賦值；`Awake` 階段尚未呼叫 `Load()` → 返回 null。`CloneData` 看到 null 直接回 null。
- **建議改法**：
  ```csharp
  public SaveFileData LastLoaded
  {
      get
      {
          if (_lastLoaded == null)
          {
              _lastLoaded = new SaveFileData
              {
                  Player = DataManager.Instance?.InitialPlayerData ?? new PlayerData()
              };
              EnsureLists(_lastLoaded.Player);
          }
          return CloneData(_lastLoaded);
      }
  }
  ```
- **風險 / 向下相容**：無存檔格式變動。唯一需留意：若 `DataManager.Instance` 也尚未初始化（場景尚早），會落到 `new PlayerData()` 空物件，呼叫方仍需防禦 `Player.Inventory` 存取。
- **預估工時**：1 小時。

---

## 2. P1 — 高（效能、耦合、維護性）

### 2.1 `_jsonSettings` 在三處重複定義

- **症狀**：修改 JSON 序列化策略（如加 `Converters`、換 `TypeNameHandling`）需同時改三個檔案，容易漏改造成 `$type` 缺失 → 反序列化失敗。
- **影響範圍**：SaveManager、DataManager、GameDataLoader 三處各自有一份。
- **根因**：
  - [SaveManager.cs:33-37](../Assets/GameData/GameSystem/SaveManager.cs#L33-L37)
  - `DataManager.cs` 內的 `_jsonSettings`
  - `GameDataLoader.cs` 內的 `_jsonSettings`
- **建議改法**：新增 `Assets/GameData/GameSystem/JsonSettingsProvider.cs`：
  ```csharp
  namespace GameSystem
  {
      public static class JsonSettingsProvider
      {
          public static readonly JsonSerializerSettings Default = new JsonSerializerSettings
          {
              TypeNameHandling = TypeNameHandling.Auto,
              ReferenceLoopHandling = ReferenceLoopHandling.Ignore
          };
      }
  }
  ```
  三處改為 `JsonSettingsProvider.Default`。
- **風險 / 向下相容**：零風險，純 refactor，JSON 輸出位元相同。
- **預估工時**：30 分鐘。

### 2.2 CloneData / ClonePlayer 使用 JSON 序列化做深複製 → 大背包幀卡

- **症狀**：`SaveManager.SaveGameAsync` 呼叫 `ClonePlayer` 再寫檔；每次 `LastLoaded` 存取也跑一遍 `CloneData`。玩家累積 100+ 背包物品或長時間交易後，每次存檔可量測到 **10~50ms** 幀停頓。
- **影響範圍**：結算、跨日、任何 UI 觸發 `LastLoaded` 的查詢。
- **根因**：[SaveManager.cs:199-219](../Assets/GameData/GameSystem/SaveManager.cs#L199-L219) 用 `JsonConvert.SerializeObject` → `DeserializeObject` 完成深複製。本意是避免活引用被呼叫方 mutate，但 JSON 走 reflection + string allocation，效能極差。
- **建議改法**：
  1. `LastLoaded` 改為返回**唯讀 wrapper** 或活引用（若呼叫方保證不 mutate）。
  2. `ClonePlayer` 改用手寫深拷貝：
     ```csharp
     private PlayerData ClonePlayer(PlayerData s)
     {
         if (s == null) return new PlayerData();
         return new PlayerData
         {
             ID = s.ID,
             MasterSeed = s.MasterSeed,
             DaysPlayed = s.DaysPlayed,
             Gold = s.Gold,
             MonsterGold = s.MonsterGold,
             PlayingStatus = s.PlayingStatus,
             IsTrade = s.IsTrade,
             CustomerIndex = s.CustomerIndex,
             HoldAchievementSouvenirID = new List<string>(s.HoldAchievementSouvenirID ?? new()),
             Inventory = CloneInventory(s.Inventory),
             GameSaveFile = CloneGameSaveFile(s.GameSaveFile) // ISaveData dict 仍需 JSON
         };
     }
     ```
  3. 只對 `GameSaveFile.GameData`（介面集合）保留 JSON 深拷貝，其他欄位純手寫。
- **風險 / 向下相容**：中等。新增 `PlayerData` 欄位時需記得同步 `ClonePlayer`；建議加單元測試（見 §4.1）鎖定行為。
- **預估工時**：3 小時（含 `Inventory` / `GameSaveFile` helper）。

### 2.3 DataManager 直接呼叫 SaveManager.Instance → 跨層耦合

- **症狀**：任何 `DataManager` 的 book mutator（如 `UnlockMonsterInformation` / `AddItemToBook`）都內含 `SaveManager.Instance.SaveBookData(_bookData)`；測試 DataManager 時必須連帶初始化 SaveManager，無法單獨替換。
- **影響範圍**：DataManager 十數處方法；未來若要做「批量編輯後一次存檔」難以抽離。
- **根因**：DataManager 直呼 Singleton，沒有存檔策略介面。
- **建議改法**：引入 `ISaveSink`：
  ```csharp
  public interface ISaveSink
  {
      void PersistBook(GameSaveBook book);
      Task PersistBookAsync(GameSaveBook book);
  }
  // DataManager 持有 ISaveSink 欄位；預設在 Initialize 時注入 SaveManager.Instance。
  // 測試時可注入 mock sink。
  ```
- **風險 / 向下相容**：中等。改動面廣（十幾個 mutator 呼叫點），但行為不變。建議配合 §2.1 `JsonSettingsProvider` 一起做，減少 PR 次數。
- **預估工時**：4 小時。

---

## 3. P2 — 中（最佳化、I/O、可讀性）

### 3.1 AchievementManager / SouvenirManager 反射掃描無快取

- **症狀**：首次進入遊戲時 `Initialize()` 會執行 `AppDomain.CurrentDomain.GetAssemblies()` → 過濾 `AchievementLibrary` namespace → 實例化。在 Editor 下首次大約 50–200ms，Mobile 可能更久。
- **根因**：每次 `Initialize` 都重新掃一次，無靜態快取。
- **建議改法**：
  ```csharp
  private static List<Type> _cachedAchievementTypes;
  private static List<Type> GetAchievementTypes()
  {
      if (_cachedAchievementTypes != null) return _cachedAchievementTypes;
      _cachedAchievementTypes = AppDomain.CurrentDomain.GetAssemblies()
          .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
          .Where(t => !t.IsAbstract && t.Namespace == "AchievementLibrary"
                      && typeof(AchievementBase).IsAssignableFrom(t))
          .ToList();
      return _cachedAchievementTypes;
  }
  ```
  SouvenirManager 同樣套用。
- **風險 / 向下相容**：零風險。Editor Domain Reload 會自然重置靜態快取。
- **預估工時**：1 小時。

### 3.2 `AdjustUpdateView` 連串 if-elseif 可重構為 switch

- **症狀**：可讀性問題，`SaveSlotUI.CreateSlotUI` 內一串 `if (phase == HumanDay) ... else if (phase == AfterNoon) ...` 處理天數顯示。
- **根因**：[SaveSlotUI.cs:183-194](../Assets/Script/UI/SaveSlotUI.cs#L183-L194)。
- **建議改法**：
  ```csharp
  dayText.text = slotData.CurrentPhase switch
  {
      DayPhase.HumanDay or DayPhase.AfterNoon => $"剩餘 {21 - slotData.DaysPlayed} 天",
      DayPhase.Night => $"剩餘 {21 - (slotData.DaysPlayed - 1)} 天",
      _ => ""
  };
  ```
- **風險 / 向下相容**：零。
- **預估工時**：15 分鐘。

### 3.3 圖鑑 mutator 即時落檔 → I/O 頻繁

- **症狀**：玩家一次交易可能同時：加一個新物品到圖鑑 + 解鎖一個妖怪情報 + 更新一個成就 → 觸發 3 次 `SaveBookData` 同步寫檔。檔案愈大（5+ MB）卡頓愈明顯。
- **根因**：每個 mutator 結尾都呼叫 `SaveBookData(_bookData)`，缺少批量延遲。
- **建議改法**：引入 `SaveBatcher`：
  ```csharp
  public class SaveBatcher
  {
      private bool _bookDirty;
      public void MarkBookDirty() => _bookDirty = true;
      public async Task FlushAsync()
      {
          if (_bookDirty)
          {
              await SaveManager.Instance.SaveBookDataAsync(DataManager.Instance.GetBookData());
              _bookDirty = false;
          }
      }
  }
  ```
  DataManager mutator 只 `MarkBookDirty`；在 DayEnd / 場景切換前 `FlushAsync`。
- **風險 / 向下相容**：中。Crash 未 Flush 會遺失該次 session 變更；需配合 auto-save timer（例如每 30 秒 Flush 一次）。
- **預估工時**：4 小時。

### 3.4 反序列化失敗僅 LogError → 建議備份腐敗檔

- **症狀**：玩家存檔 JSON 損壞（例如 disk full 寫了半截），遊戲 `Load()` 直接返回預設空資料，**原檔案被覆蓋** → 玩家進度永久丟失。
- **根因**：[SaveManager.cs:111-120](../Assets/GameData/GameSystem/SaveManager.cs#L111-L120) catch 區塊只 LogError + 建立新 PlayerData，隨後玩家若繼續玩並存檔就會覆蓋腐敗原檔。
- **建議改法**：
  ```csharp
  catch (Exception ex)
  {
      string backupPath = filePath + $".corrupt.{DateTime.Now:yyyyMMddHHmmss}.bak";
      try { File.Move(filePath, backupPath); } catch {}
      Debug.LogError($"[SaveManager] 讀檔失敗已備份到 {backupPath}: {ex.Message}");
      // ... 後續建立新 PlayerData
  }
  ```
- **風險 / 向下相容**：低。備份檔不干擾正常流程。
- **預估工時**：1 小時。

### 3.5 `GroupBy(...).ToDictionary(...)` 遇重複 ID 靜默忽略

- **症狀**：新增靜態資料時不小心打重複 ID（例如兩個 `ItemDefinition` 都是 `"Item_Key"`），GameDataLoader 會靜默只保留第一個，QA 可能花數小時才發現。
- **根因**：GameDataLoader 的多處 `.GroupBy(x => x.ID).ToDictionary(g => g.Key, g => g.First())` 模式。
- **建議改法**：
  ```csharp
  foreach (var g in list.GroupBy(x => x.ID))
  {
      if (g.Count() > 1)
          Debug.LogWarning($"[GameDataLoader] 重複 ID: {g.Key} (x{g.Count()})，只保留第一筆");
  }
  return list.GroupBy(x => x.ID).ToDictionary(g => g.Key, g => g.First());
  ```
  或封裝成 helper：
  ```csharp
  static Dictionary<string, T> SafeGroupToDict<T>(IEnumerable<T> list, Func<T, string> keyFn, string label) { ... }
  ```
- **風險 / 向下相容**：零。僅新增 Warning log。
- **預估工時**：1 小時（若寫 helper 並替換所有 Loader 約 2 小時）。

---

## 4. P3 — 低 / 安全與測試

### 4.1 完全無單元 / 整合測試

- **症狀**：專案內 `*Test*.cs` 檔案只有 `MonsterGuestGeneratorTest.cs` 與 `TalkTest.cs`，存檔相關程式碼零覆蓋 → 任何重構都要手動 QA。
- **建議改法**：最小測試矩陣
  | 類別 | 測試項目 |
  |---|---|
  | SaveManager | `Save → Load` round-trip、`DeleteSaveSlot` 刪除存在/不存在、`GetNextAvailableSlot` 越界、`CloneData` 深度 |
  | DataManager | `GetPlayerSaveData<T>` 跨日重置、`GetPersistentSaveData<T>` 保留、`AddItem` 同步到 Book |
  | GameDataLoader | 正常 JSON、空 JSON、壞 JSON、重複 ID |
  | Integration | 完整開局 → 存檔 → Load → 驗證欄位一致 |
- **建議骨架**：
  ```csharp
  [Test]
  public void CloneData_ModifyingCloneDoesNotAffectSource()
  {
      var src = new SaveFileData { Player = new PlayerData { Gold = 100 } };
      // 透過 reflection 或 InternalsVisibleTo 呼叫 CloneData
      var clone = SaveManager.CloneDataForTest(src);
      clone.Player.Gold = 999;
      Assert.AreEqual(100, src.Player.Gold);
  }
  ```
- **風險 / 向下相容**：零（新增測試不影響 production）。
- **預估工時**：8 小時（初版 15–20 個測試 + 測試 harness）。

### 4.2 TypeNameHandling.Auto 反序列化安全風險

- **症狀**：`$type` 欄位含 `Namespace.Class, Assembly` 字串。攻擊者修改玩家存檔把 `$type` 指向危險類型（例如 `System.Diagnostics.Process`）可觸發任意類型實例化。
- **風險程度**：本專案為單機遊戲，攻擊者改自己的存檔只能攻擊自己；但若未來接雲端、多人，風險放大。
- **建議改法**：用 `ISerializationBinder` 白名單限制可反序列化的 namespace：
  ```csharp
  public class SafeSerializationBinder : ISerializationBinder
  {
      private static readonly HashSet<string> AllowedNamespaces = new()
      {
          "AchievementLibrary", "SouvenirLibrary", "GameSystem", "GameData"
      };
      public Type BindToType(string assemblyName, string typeName)
      {
          string ns = typeName.Substring(0, typeName.LastIndexOf('.'));
          if (!AllowedNamespaces.Contains(ns))
              throw new JsonSerializationException($"Type {typeName} is not allowed.");
          return Type.GetType($"{typeName}, {assemblyName}");
      }
      public void BindToName(Type t, out string aName, out string tName)
      { aName = t.Assembly.FullName; tName = t.FullName; }
  }
  // JsonSettingsProvider.Default.SerializationBinder = new SafeSerializationBinder();
  ```
- **風險 / 向下相容**：**高**。舊存檔中 `$type` 若用了非白名單 namespace 會讀不出來。建議先上線 warning 模式（記錄不在白名單但仍允許），收集 1–2 版後再切 enforcing。
- **預估工時**：4 小時（含 binder 寫作 + 舊檔遷移測試）。

### 4.3 存檔明文可被竄改

- **症狀**：玩家用文字編輯器改 `save_slot_0.json` 把 `Gold` 改成 999999 即可作弊。
- **建議改法**：存檔寫入時追加 HMAC 簽章：
  ```csharp
  private string ComputeHmac(string json)
  {
      using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SECRET_KEY));
      return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(json)));
  }
  // 寫檔：{ "data": <json>, "hmac": <base64> }
  // 讀檔：比對 hmac，不符則警告或拒讀
  ```
- **風險 / 向下相容**：**高**。舊存檔無 hmac；需版本化 schema。`SECRET_KEY` 存在 client 程式碼中只能擋低手玩家，高手 decompile 仍可偽造 → 若非必要不建議做。
- **預估工時**：6 小時。
- **建議**：若沒有競技 / 成就排行榜需求，**不值得做**（單機遊戲作弊是玩家自由）。

### 4.4 破壞性 API 無二次驗證

- **症狀**：`SaveDataManagerPanel` 的 `ClearAllSaves` 只要點兩次按鈕就刪光，無輸入驗證。家長模式、主機共用場景下誤觸風險。
- **建議改法**：在 `ShowConfirm` 額外要求輸入確認字串：
  ```csharp
  // UI 加一個 InputField，要求輸入 "DELETE" 才啟用確認按鈕
  deleteConfirmButton.interactable = confirmInput.text == "DELETE";
  ```
- **風險 / 向下相容**：零。
- **預估工時**：1 小時。
- **建議**：非關鍵優先，若 UX 研究顯示有誤觸回報再做。

### 4.5 字典 Loader 格式不對稱（List vs Wrapper）

- **症狀**：GameDataLoader 中某些 Loader（如 `LoadItemsAsync`）支援 JSON 檔以 `[{...}]` 或 `{ "Items": [...] }` 兩種格式開頭；某些只支援其中一種。新人加 JSON 檔時易踩雷。
- **建議改法**：抽 `JsonLoadHelper.LoadListOrWrapper<T, TWrapper>(string json, Func<TWrapper, List<T>> getter)` 統一處理。
- **風險 / 向下相容**：零。
- **預估工時**：2 小時。

---

## 5. 改進路線圖（Roadmap）

### 短期（1 週內）
- **§1.1** SaveManager.Load 鎖定
- **§1.2** LastLoaded null-safe
- **§2.1** 抽 JsonSettingsProvider
- **§3.2** DayPhase switch 重構

### 中期（1 個月內）
- **§2.2** 手寫 ClonePlayer
- **§2.3** ISaveSink 解耦
- **§3.1** 反射快取
- **§3.4** 腐敗檔備份
- **§3.5** 重複 ID 警告

### 長期（季度）
- **§4.1** 建立單元測試矩陣
- **§3.3** SaveBatcher 延遲批量存檔
- **§4.2** SerializationBinder 白名單
- **§4.5** Loader 格式統一 helper
- 視需求：§4.3 HMAC / §4.4 二次驗證強化

---

## 6. 變更時的迴歸測試 Checklist

**每次動到 SaveManager / DataManager 的公開 API 後，手動跑一次：**

- [ ] 全新開局 → 玩 1 日 → 結算 → 重啟 → 正確讀回
- [ ] 解鎖 1 個物品圖鑑 → 重啟 → 圖鑑顯示
- [ ] 完成 1 個成就 → 重啟 → 成就點數與完成狀態正確
- [ ] 收集 1 個特殊紀念品 → 重啟 → `UnLockSpecialSouvenirID` 包含該 ID
- [ ] `ClearBookData` → 圖鑑清空但 `Sou_key` 仍在
- [ ] `ClearAllSaves` → 全部 slot + 圖鑑清空，預設值正確
- [ ] `UnlockAllBookData` → 圖鑑全解鎖，UI 無彈窗轟炸
- [ ] `UnlockAllAchievementsAndSpecialSouvenirs` → 成就 / 紀念品全解鎖，UI 無彈窗轟炸
- [ ] 跨日 `GetPlayerSaveData<T>` → 回傳新實例
- [ ] 跨日 `GetPersistentSaveData<T>` → 保留舊資料
- [ ] 快速點擊 Slot 按鈕 × 10 → 無異常、無檔案損壞（驗 §1.1）
- [ ] 手動刪除 JSON 其中 `}` 造成 JSON 損壞 → 啟動遊戲應 fallback 而不 crash（驗 §3.4）
- [ ] 在 ProductInfo 切換 Company 名後啟動 → 舊 slot 不會消失（persistentDataPath 變動檢查）

---

## 附錄：快速查詢表

### A. 改進項目優先度矩陣

| # | 標題 | 優先度 | 工時 | 風險 |
|---|---|---|---|---|
| 1.1 | Load() 鎖定 | P0 | 2h | 低 |
| 1.2 | LastLoaded null-safe | P0 | 1h | 低 |
| 2.1 | JsonSettingsProvider | P1 | 0.5h | 零 |
| 2.2 | 手寫 ClonePlayer | P1 | 3h | 中 |
| 2.3 | ISaveSink 解耦 | P1 | 4h | 中 |
| 3.1 | 反射快取 | P2 | 1h | 零 |
| 3.2 | DayPhase switch | P2 | 0.25h | 零 |
| 3.3 | SaveBatcher | P2 | 4h | 中 |
| 3.4 | 腐敗檔備份 | P2 | 1h | 低 |
| 3.5 | 重複 ID Warning | P2 | 1h | 零 |
| 4.1 | 單元測試矩陣 | P3 | 8h | 零 |
| 4.2 | SerializationBinder | P3 | 4h | 高 |
| 4.3 | HMAC 簽章 | P3 | 6h | 高 |
| 4.4 | 破壞性 API 二次驗證 | P3 | 1h | 零 |
| 4.5 | Loader 格式統一 | P3 | 2h | 零 |

**總工時**：約 38.75 小時（不含 §4.2 / §4.3 若選擇不做）。

### B. 本文件維護規則

- 條目完成實作後**不要刪除**，改成「已修復（見 commit 〈hash〉）」移到附錄 C。
- 新發現問題按 P0–P3 插入對應節，維持編號連續。
- 大型重構（例如真的抽了 ISaveSink）應同步更新 [SaveSystem_Architecture.md](SaveSystem_Architecture.md)。
