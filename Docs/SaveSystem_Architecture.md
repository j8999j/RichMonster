# 存檔系統架構圖

> 對應版本：2026-04-15  
> 專案：RED / ForTest

---

## 1. 整體元件關係

```mermaid
graph TB
    subgraph UI["UI 層"]
        SSU["SaveSlotUI\n(View — MVP)"]
        SSP["SaveSlotPresenter\n(Presenter — MVP)"]
        SDMP["SaveDataManagerPanel\n(管理面板)"]
    end

    subgraph Managers["系統層 Singletons"]
        SM["SaveManager\n★ 唯一 I/O 入口\n────────────────\n_cachedBookData\n_achievementDict\n_specialSouvenirDict\nIsSaving / IsSavingBook"]
        DM["DataManager\n★ Runtime 資料中樞\n────────────────\n_currentPlayerData\n_bookData (= SM._cachedBookData)\n_achievementSaveDict (= SM._achievementDict)\n_specialSouvenirSaveDict (= SM._specialSouvenirDict)"]
        GDL["GameDataLoader\n靜態設定 Loader"]
        AM["AchievementManager\n成就生命週期"]
        SVM["SouvenirManager\n特殊紀念品生命週期"]
        STM["SceneTransitionManager\n場景切換"]
    end

    subgraph Disk["磁碟 (persistentDataPath)"]
        SLOT["save_slot_{N}.json\n(Slot 存檔，最多 10 個)"]
        BOOK["illustrated_book.json\n(Book 存檔，唯一)"]
    end

    SSU -->|"事件: OnSlotSelected\nOnSlotDeleteRequested\nOnRefreshRequested"| SSP
    SSP -->|"LoadPlayerFromSave\nDeleteSaveSlot\nLoadSlotInfo"| SM
    SSP -->|"InitializeGame"| DM
    SDMP -->|"ClearBookData\nUnlockAllBookData\nUnlockAllAchievements…\nClearAllSaves\nOpenSaveFolder"| SM

    DM -->|"SaveCurrentPlayerAsync(slot)"| SM
    DM -->|"SaveBookData / SaveBookDataAsync"| SM
    DM -->|"InitializeAsync → LoadAllGameDataAsync"| GDL
    DM -->|"Initialize(configDict)"| AM
    DM -->|"Initialize()"| SVM
    SM -->|破壞性操作後| STM

    GDL -->|"SetBookDataCache()"| SM

    SM <-->|"共用同一個物件引用"| DM

    SM -->|"async / sync 讀寫"| SLOT
    SM -->|"async / sync 讀寫"| BOOK
```

---

## 2. 雙管線存檔結構

```mermaid
graph LR
    subgraph SlotPipeline["Slot 管線（單局）"]
        SF["SaveFileData"]
        PD["PlayerData\n──────────────\nID / MasterSeed\nDaysPlayed / CustomerIndex\nPlayingStatus : DayPhase\nIsTrade : bool\nGold / MonsterGold\nHoldAchievementSouvenirID"]
        INV["Inventory\n  List&lt;Item&gt;"]
        GSF["GameSaveFile\n  Dict&lt;string, ISaveData&gt;\n  ─────────────────────\n  MissionSaveData\n  OrderHistoryData\n  ShopShelfData\n  … 任意 ISaveData"]
        SF --> PD
        PD --> INV
        PD --> GSF
    end

    subgraph BookPipeline["Book 管線（跨局）"]
        GB["GameSaveBook"]
        IB["ItemBookData\n  List&lt;ItemBookDatabase&gt;"]
        MB["MonsterBookData\n  UnlockMonsterInformationID\n  NewMonsterInformationID\n  NewMonsterStoryID"]
        AD["AchievementData\n  List&lt;IAchievementSave&gt;"]
        UAS["UnLockAchievementSouvenirID\n  List&lt;string&gt;"]
        USS["UnLockSpecialSouvenirID\n  List&lt;string&gt;\n  (含預設 Sou_key)"]
        SPD["SpecialSouvenirProgressData\n  List&lt;ISpecialSouvenirSave&gt;"]
        GB --> IB
        GB --> MB
        GB --> AD
        GB --> UAS
        GB --> USS
        GB --> SPD
    end

    SlotPipeline -->|"save_slot_{N}.json"| D1[("磁碟")]
    BookPipeline -->|"illustrated_book.json"| D1
```

---

## 3. 資料介面層級

```mermaid
classDiagram
    class ISaveData {
        <<interface>>
        +string UniqueID
        +int LastUpdatedDay
    }
    class MissionSaveData { +UniqueID +LastUpdatedDay }
    class OrderHistoryData { +UniqueID +LastUpdatedDay }
    class ShopShelfData   { +UniqueID +LastUpdatedDay }
    ISaveData <|.. MissionSaveData
    ISaveData <|.. OrderHistoryData
    ISaveData <|.. ShopShelfData

    class IAchievementSave {
        <<interface>>
        +string AchievementID
        +bool IsCompleted
        +int FinishDay
    }
    class IAchievementWithProgress {
        <<interface>>
        +int CurrentProgress
        +int TargetProgress
    }
    class AchievementBase {
        <<abstract>>
        (namespace AchievementLibrary)
    }
    IAchievementSave <|-- IAchievementWithProgress
    IAchievementWithProgress <|.. AchievementBase

    class ISpecialSouvenirSave {
        <<interface>>
        +string SouvenirID
        +bool IsCompleted
        +int CurrentProgress
        +int TargetProgress
    }
    IAchievementWithProgress <|-- ISpecialSouvenirSave
    ISpecialSouvenirSave <|.. SpecialSouvenir
```

---

## 4. 啟動初始化流程

```mermaid
sequenceDiagram
    participant Scene as Scene 載入
    participant SM as SaveManager
    participant DM as DataManager
    participant GDL as GameDataLoader
    participant AM as AchievementManager
    participant SVM as SouvenirManager

    Scene->>SM: Awake()
    SM->>SM: _cachedBookData = LoadBookData()<br/>(illustrated_book.json)
    SM->>SM: _achievementDict = ListToDict(…)
    SM->>SM: _specialSouvenirDict = ListToSpecialSouvenirDict(…)

    Scene->>DM: Awake() → InitializeAsync()
    DM->>GDL: LoadAllGameDataAsync()
    GDL->>GDL: 載入所有靜態設定<br/>(ItemDict, AchievementDict, …)
    GDL->>GDL: 再讀 BookData (for 預設值)
    GDL->>SM: SetBookDataCache(_bookData)
    Note over SM,DM: 兩者現在指向同一個 GameSaveBook 物件

    DM->>SM: GetAchievementDict()
    DM->>SM: GetSpecialSouvenirDict()
    Note over SM,DM: Dict 也是共用引用

    DM->>AM: Initialize(configDict)
    Note over AM: 反射掃描 namespace=AchievementLibrary<br/>下所有 AchievementBase 子類
    DM->>SVM: Initialize()
    DM->>DM: _currentPlayerData = Clone(_initialPlayerData)
    DM->>SM: WhenInitialized() ← 對外 await 點
```

---

## 5. 存檔寫入路徑

```mermaid
flowchart TD
    subgraph PlayerSave["Slot 存檔路徑"]
        A1["DataManager.SaveCurrentPlayerAsync(slot)"]
        A2{"IsSaving?"}
        A3["SM.SaveGameAsync(playerData, slot)"]
        A4["JSON Clone → snapshot"]
        A5["save_slot_{slot}.json"]
        A1 --> A2
        A2 -->|"Yes → 跳過"| END1["略過 (警告 Log)"]
        A2 -->|"No"| A3
        A3 --> A4 --> A5
    end

    subgraph BookSave["Book 存檔路徑"]
        B1["DM mutator 呼叫<br/>(UnlockMonsterInfo / AddItemToBook / …)"]
        B2["mutate _bookData (共用物件)"]
        B3["SM.SaveBookData(_bookData) 同步"]
        B4["illustrated_book.json"]
        B5["DictToList(_achievementDict)<br/>→ bookData.AchievementData"]
        B6["DictToList(_specialSouvenirDict)<br/>→ bookData.SpecialSouvenirProgressData"]
        B1 --> B2 --> B5 --> B6 --> B3 --> B4
    end

    subgraph AsyncBookSave["Book 非同步路徑（少用）"]
        C1["SM.SaveBookDataAsync(bookData)"]
        C2{"IsSavingBook?"}
        C3["await WriteAllTextAsync"]
        C1 --> C2
        C2 -->|"Yes → 跳過"| END2["略過"]
        C2 -->|"No"| C3
    end
```

---

## 6. UI 層 MVP 互動

```mermaid
sequenceDiagram
    participant User as 玩家操作
    participant SSU as SaveSlotUI (View)
    participant SSP as SaveSlotPresenter
    participant SM as SaveManager
    participant DM as DataManager

    User->>SSU: 點擊 Slot 卡片
    SSU->>SSP: OnSlotSelected(slotIndex, isEmpty)
    alt isEmpty
        SSP->>DM: 使用預設 PlayerData 開新局
    else 有存檔
        SSP->>SM: LoadSlotInfo(slotIndex)
        SM-->>SSP: SaveFileData
        SSP->>DM: LoadPlayerFromSave(playerData)
        SSP->>DM: InitializeGame()
    end

    User->>SSU: 點擊刪除按鈕 (child 3)
    SSU->>SSU: 顯示 deleteConfirmPanel<br/>_pendingDeleteSlotIndex = index
    User->>SSU: 按確認
    SSU->>SSP: OnSlotDeleteRequested(slotIndex)
    SSP->>SM: DeleteSaveSlot(slotIndex)
    SSP->>SSU: OnRefreshRequested → LoadAndDisplaySaveSlots()

    User->>SDMP: 點擊破壞性按鈕
    SDMP->>SDMP: ShowConfirm(message, action)<br/>存入 _pendingAction
    User->>SDMP: 按確認
    SDMP->>SM: _pendingAction.Invoke()
    SM->>STM: GoToMainMenu() (若 reloadMainMenu=true)
```

---

## 7. JSON 序列化策略

```mermaid
graph LR
    subgraph Problem["問題"]
        P["List&lt;IAchievementSave&gt;\nList&lt;ISpecialSouvenirSave&gt;\n是介面集合，\n反序列化需知道具體型別"]
    end
    subgraph Solution["解法"]
        S["JsonSerializerSettings\n────────────────────────\nTypeNameHandling = Auto\nReferenceLoopHandling = Ignore"]
        J["JSON 中自動附加 $type 欄位\n→ 反序列化時還原為具體類別"]
    end
    subgraph Warning["注意事項"]
        W1["$type 含 Assembly + Namespace\n重構搬移類別 → 舊存檔失效"]
        W2["LoadSlotInfo 也必須帶入 _jsonSettings\n否則 'Could not create instance' 例外"]
    end
    Problem --> Solution --> Warning
```

---

## 8. 破壞性操作一覽

| 方法 | 影響 Slot | 影響 Book | 重新載入場景 |
|---|:---:|:---:|:---:|
| `DeleteSaveSlot(slot)` | ✅ 單個 | ✗ | ✗ |
| `ClearBookData()` | ✗ | ✅ 全清 (保留 Sou_key) | ✅ |
| `UnlockAllBookData()` | ✗ | ✅ 全解鎖圖鑑 | ✅ |
| `UnlockAllAchievementsAndSpecialSouvenirs()` | ✗ | ✅ 全解鎖成就+紀念品 | ✅ |
| `ClearAllSaves()` | ✅ 全部 | ✅ 全清 | ✅ |
| `OpenSaveFolder()` | ✗ | ✗ | ✗ |

> 所有破壞性操作在 `SaveDataManagerPanel` 都經過 `ShowConfirm()` 二次確認才執行。

---

## 9. 擴充指引速查

```mermaid
flowchart TD
    Q{要加什麼？}
    Q -->|"每日重置的局內資料"| E1["1. 實作 ISaveData\n2. SetPlayerData / GetPlayerSaveData"]
    Q -->|"跨日累積的局內資料"| E2["1. 實作 ISaveData\n2. SetPlayerData / GetPersistentSaveData"]
    Q -->|"新成就"| E3["namespace AchievementLibrary\n繼承 AchievementBase\n(反射自動撈)"]
    Q -->|"新特殊紀念品"| E4["繼承 SpecialSouvenir\nimplements ISpecialSouvenirSave\n走 SouvenirManager"]
    Q -->|"新存檔管理按鈕"| E5["1. SaveManager 加業務邏輯\n2. SaveDataManagerPanel 加 Button\n3. 用 ShowConfirm 包裝"]
```
