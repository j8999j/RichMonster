# 紀念品系統重構方案

## 目標

目前紀念品系統的效果分派集中在 `SouvenirEffectDispatcher`。它透過 `ForEachOwned<T>()` 與 `ForEachAllSpecial<T>()` 在每次效果觸發時掃描紀念品集合，再用 `is T` 判斷是否實作指定介面。

這個做法在紀念品數量少時很直覺，但如果未來紀念品增加，商店折扣、背包容量、刮刮樂免費、購買事件、妖怪交易事件等流程都會重複掃描持有紀念品。重構目標是把「執行時掃描」改成「持有狀態變更時建立索引」，並將紀念品效果拆成事件型與管線型兩種路徑。

重構後希望達成：

- 效果觸發時不再掃描所有持有紀念品。
- 商店、背包、刮刮樂等系統不直接知道具體紀念品。
- 紀念品效果仍可維持資料驅動與可擴充。
- 視覺資訊不綁定商店邏輯，而是由管線輸出 metadata，View 自行選擇是否顯示。
- 能逐步遷移，不需要一次重寫所有紀念品。

---

## 現況問題

目前 `SouvenirEffectDispatcher` 的核心分派方式如下：

```csharp
private void ForEachOwned<T>(Action<T> action) where T : class
{
    foreach (var souvenir in _getOwnedSouvenirs())
    {
        if (souvenir is T target)
        {
            action(target);
        }
    }
}
```

這代表每次呼叫以下功能時，都會掃描所有持有中的紀念品：

- `ApplyAllStartEffects()`
- `ApplyAllShopDiscounts()`
- `BuildShopVisualInfos()`
- `ApplyAllDailyEffects()`
- `IsScratchCardFree()`
- `GetExtraBagCapacity()`
- `OnItemPurchased()`
- `OnMonsterTradeCompleted()`

特殊紀念品也有類似問題：

```csharp
private void ForEachAllSpecial<T>(Action<T> action) where T : class
{
    foreach (var souvenir in _getAllSpecialSouvenirs())
    {
        if (souvenir is T target)
        {
            action(target);
        }
    }
}
```

這會讓 `IMonsterTradeWithRaceListener`、`IMonsterTradeFailedListener` 等特殊紀念品效果每次事件發生時掃描所有特殊紀念品。

目前架構還有幾個維護問題：

- 介面數量持續增加，Dispatcher 會越來越像大型分派表。
- 數值修正型效果與事件觸發型效果混在同一個 Dispatcher 中。
- 商店折扣與商店視覺資訊分成兩次掃描。
- `GetExtraBagCapacity()`、`IsScratchCardFree()` 這類查詢型效果每次查詢都重新計算。
- UI 視覺效果沒有明確與邏輯效果分層，未來如果增加折扣圖示、特殊框線、限時標籤，容易把 UI 細節帶進商店邏輯。

---

## 重構後整體架構

建議將紀念品效果系統拆成四個角色：

```text
SouvenirManager
負責建立紀念品、管理持有狀態、通知效果系統重建索引。

SouvenirEffectRegistry
負責根據持有中的紀念品建立事件 handler 與 pipeline step 索引。

SouvenirEffectDispatcher
負責訂閱 GameEventCenter，收到事件後從 Registry 取出對應 handler 執行。

SouvenirPipelineService
負責讓商店、背包、刮刮樂等系統主動呼叫管線，執行數值或視覺資料修正。
```

事件型效果走 Dispatcher：

```text
GameEventCenter.Publish(...)
-> SouvenirEffectDispatcher 收到事件
-> 從 SouvenirEffectRegistry 取得該事件的 handler list
-> 執行 handler
```

管線型效果走 Pipeline：

```text
系統需要計算結果
-> 建立 Context
-> SouvenirPipelineService.Run(context)
-> 依序執行已註冊的 pipeline step
-> 回傳被修改後的 Context
```

---

## 效果分類

### 事件型效果

事件型效果是「某件事發生後才觸發」的效果。它適合用 Trigger -> Handler 索引。

目前可歸類為事件型的效果：

- 商店購買後觸發：原 `IShopPurchaseListener`
- 妖怪交易成功後觸發：原 `IMonsterTradeListener`
- 妖怪交易成功且需要種族資料：原 `IMonsterTradeWithRaceListener`
- 妖怪交易失敗後觸發：原 `IMonsterTradeFailedListener`
- 完成訂單後觸發：未來可新增 `HumanOrderCompletedEvent` handler
- 成就解鎖後觸發：未來可新增 `AchievementUnlockedEvent` handler
- 紀念品購買後觸發：未來可新增 `SouvenirPurchasedEvent` handler

建議定義 Trigger enum：

```csharp
public enum SouvenirEffectTrigger
{
    ItemPurchased,
    MonsterTradeCompleted,
    MonsterTradeCompletedWithRace,
    MonsterTradeFailed,
    HumanOrderCompleted,
    AchievementUnlocked,
    SouvenirPurchased
}
```

Registry 內部可以維護：

```csharp
private readonly Dictionary<SouvenirEffectTrigger, List<ISouvenirEventHandler>> _eventHandlersByTrigger;
```

事件發生時不掃紀念品，而是直接取對應 List：

```csharp
foreach (var handler in registry.GetEventHandlers(SouvenirEffectTrigger.ItemPurchased))
{
    handler.Handle(context);
}
```

### 管線型效果

管線型效果是「某個系統需要計算結果」時主動呼叫的效果。它適合用 Pipeline Context。

目前可歸類為管線型的效果：

- 商店商品價格修正：原 `IShopDiscountProvider`
- 商店商品視覺資料修正：原 `IShopVisualModifier`
- 背包容量加成：原 `IBagCapacityProvider`
- 刮刮樂是否免費：原 `IFreeScratchCardProvider`
- 每日效果：原 `IDailyEffect`
- 開局效果：原 `IApplyStartEffect`
- 未來交易價格倍率、訂單價格倍率也可歸類為管線

建議定義 Pipeline enum：

```csharp
public enum SouvenirPipelineType
{
    ShopShelf,
    BagCapacity,
    ScratchCard,
    DailyEffect,
    StartEffect,
    MonsterTradePrice,
    HumanOrderPrice
}
```

其中 `ShopShelf` 可以同時處理價格與視覺 metadata，避免商店折扣掃一次、商店視覺又掃一次。

---

## 核心資料結構建議

### Pipeline Context

所有管線都用 Context 承載輸入與輸出資料。Context 是資料容器，不應直接依賴 Unity UI 元件。

商店貨架管線：

```csharp
public sealed class ShopShelfPipelineContext
{
    public string ShopId { get; }
    public IReadOnlyList<Shop.ShelfSlot> Items { get; }

    public ShopShelfPipelineContext(string shopId, IReadOnlyList<Shop.ShelfSlot> items)
    {
        ShopId = shopId;
        Items = items;
    }
}
```

背包容量管線：

```csharp
public sealed class BagCapacityPipelineContext
{
    public int BaseCapacity { get; }
    public int ExtraCapacity { get; set; }
    public int FinalCapacity => BaseCapacity + ExtraCapacity;
}
```

刮刮樂管線：

```csharp
public sealed class ScratchCardPipelineContext
{
    public bool IsFree { get; set; }
}
```

### 視覺資料

目前專案已有 `ShelfSlotVisualInfo`：

```csharp
public class ShelfSlotVisualInfo
{
    public int SlotIndex;
    public string DiscountLabel = "";
    public int OriginalPrice = -1;
    public bool IsDailySpecial = false;
}
```

建議保留這個方向，並讓商店管線負責填入視覺資料：

```text
ShopShelfPipeline
-> 修改 ShelfSlot.Price
-> 寫入 ShelfSlot.VisualInfo.OriginalPrice
-> 寫入 ShelfSlot.VisualInfo.DiscountLabel
-> 寫入 ShelfSlot.VisualInfo.IsDailySpecial
```

商店 UI 不需要知道是哪個紀念品造成折扣，只要在 Slot 顯示時讀取 `ShelfSlot.VisualInfo`。

如果未來需要更多視覺效果，可以擴充 `ShelfSlotVisualInfo`：

```csharp
public string BadgeIconId;
public string FrameStyleId;
public bool Highlight;
public List<string> VisualTags;
```

但不建議讓商店邏輯實作折扣圖示介面。折扣圖示屬於 View 層能力，應該由支援的 SlotView 自行讀取 VisualInfo 顯示，不支援的 View 可以忽略。

---

## Registry 設計

`SouvenirEffectRegistry` 的責任是把持有中的紀念品轉換成可快速查詢的索引。

建議初期可以保留現有 interface，先只把掃描時機改掉：

```csharp
public sealed class SouvenirEffectRegistry
{
    private readonly List<IShopPurchaseListener> _shopPurchaseListeners = new();
    private readonly List<IMonsterTradeListener> _monsterTradeListeners = new();
    private readonly List<IMonsterTradeWithRaceListener> _monsterTradeWithRaceListeners = new();
    private readonly List<IMonsterTradeFailedListener> _monsterTradeFailedListeners = new();
    private readonly List<IShopDiscountProvider> _shopDiscountProviders = new();
    private readonly List<IShopVisualModifier> _shopVisualModifiers = new();
    private readonly List<IDailyEffect> _dailyEffects = new();
    private readonly List<IApplyStartEffect> _startEffects = new();
    private readonly List<IFreeScratchCardProvider> _scratchCardProviders = new();
    private readonly List<IBagCapacityProvider> _bagCapacityProviders = new();

    public void Rebuild(
        IEnumerable<SouvenirBase> ownedSouvenirs,
        IEnumerable<SpecialSouvenir> allSpecialSouvenirs)
    {
        Clear();

        foreach (var souvenir in ownedSouvenirs)
        {
            RegisterOwned(souvenir);
        }

        foreach (var souvenir in allSpecialSouvenirs)
        {
            RegisterAllSpecial(souvenir);
        }
    }
}
```

這個版本不需要立刻重寫所有紀念品類別，只是把 `is interface` 的檢查從「每次觸發」移到「持有狀態更新時」。

長期版本可以改成明確的 handler / pipeline step：

```csharp
public interface ISouvenirEventHandler<TEvent>
{
    void Handle(TEvent eventData);
}

public interface ISouvenirPipelineStep<TContext>
{
    int Order { get; }
    void Execute(TContext context);
}
```

這會比多個專用 interface 更通用，但改動較大，建議第二階段再做。

---

## Dispatcher 設計

重構後 `SouvenirEffectDispatcher` 不再掃描紀念品，只負責接事件並查 Registry。

購買事件流程：

```text
ShelfShopBase.tradeitem()
-> GameEventCenter.Publish(ItemPurchasedEvent)
-> SouvenirEffectDispatcher.OnItemPurchased
-> registry.ShopPurchaseListeners
-> 逐一執行購買後效果
```

範例：

```csharp
private void OnItemPurchased(ItemPurchasedEvent eventData)
{
    foreach (var listener in _registry.ShopPurchaseListeners)
    {
        listener.OnItemPurchased(eventData.ShopId, eventData.ItemId, eventData.Amount);
    }
}
```

妖怪交易成功：

```csharp
private void OnMonsterTradeCompleted(MonsterTradeCompletedEvent eventData)
{
    foreach (var listener in _registry.MonsterTradeListeners)
    {
        listener.OnTradeCompleted(eventData.Satisfaction);
    }

    foreach (var listener in _registry.MonsterTradeWithRaceListeners)
    {
        listener.OnTradeCompletedWithRace(eventData.Satisfaction, eventData.Race);
    }
}
```

這樣事件觸發成本只跟「真正關心該事件的紀念品數量」有關。

---

## Pipeline Service 設計

重構後商店、背包、刮刮樂等系統主動呼叫管線取得結果。

商店貨架流程：

```text
ShelfShopBase.OpenShop()
-> 生成今日貨架
-> SyncPurchaseState
-> 設定基礎價格
-> SouvenirPipelineService.ApplyShopShelf(shopId, items)
-> ShowItems
```

建議把目前兩個方法：

```csharp
ApplyAllShopDiscounts(shopId, items)
BuildShopVisualInfos(shopId, items)
```

整合成：

```csharp
ApplyShopShelfEffects(shopId, items)
```

內部一次建立 VisualInfo，並一次跑完價格與視覺修正：

```csharp
public void ApplyShopShelfEffects(string shopId, List<Shop.ShelfSlot> items)
{
    if (items == null || items.Count == 0) return;

    foreach (var slot in items)
    {
        slot.VisualInfo ??= new ShelfSlotVisualInfo { SlotIndex = slot.SlotIndex };
    }

    var context = new ShopShelfPipelineContext(shopId, items);

    foreach (var discount in _registry.ShopDiscountProviders)
    {
        discount.ApplyShopDiscount(shopId, items);
    }

    foreach (var visual in _registry.ShopVisualModifiers)
    {
        visual.ModifyVisual(shopId, items.Select(x => x.VisualInfo).ToList());
    }
}
```

第一階段可以先這樣包裝現有介面；第二階段再把 discount 與 visual 都改成 `ISouvenirPipelineStep<ShopShelfPipelineContext>`。

背包容量流程：

```text
DataManager 或 PlayerView 需要背包容量
-> SouvenirPipelineService.GetExtraBagCapacity()
-> 直接回傳快取值或跑 BagCapacityPipeline
```

刮刮樂流程：

```text
ScratchCardShop 判斷是否免費
-> SouvenirPipelineService.IsScratchCardFree()
-> 直接回傳快取值或跑 ScratchCardPipeline
```

---

## 快取策略

### 效果列表快取

只在以下時機重建：

- `SouvenirManager.Initialize()`
- `SouvenirManager.SnapshotOwnedSouvenirs()`
- `SouvenirManager.ResnapshotForCurrentGame()`
- 玩家持有紀念品變動
- 特殊紀念品解鎖狀態變動

不在每次事件觸發時掃描。

### Aggregate 結果快取

以下結果建議直接快取：

- 額外背包容量
- 刮刮樂是否免費
- 是否允許世界切換

例如：

```csharp
private int _cachedExtraBagCapacity;
private bool _cachedScratchCardFree;
private bool _cacheDirty;
```

在持有狀態變更時重算：

```csharp
public void RecalculateAggregateCache()
{
    _cachedExtraBagCapacity = 0;
    foreach (var provider in _registry.BagCapacityProviders)
    {
        _cachedExtraBagCapacity += provider.GetExtraCapacity();
    }

    _cachedScratchCardFree = false;
    foreach (var provider in _registry.FreeScratchCardProviders)
    {
        if (provider.IsScratchCardFree())
        {
            _cachedScratchCardFree = true;
            break;
        }
    }
}
```

這類資料通常不需要每次 UI 查詢都重新跑所有效果。

---

## 與現有系統的對接

### SouvenirManager

`SouvenirManager` 保留目前職責：

- 建立成就紀念品與特殊紀念品。
- 管理 `_ownedSouvenirIds`。
- 提供 `GetAchievementSouvenir`、`GetSpecialSouvenir`、`IsOwned` 等查詢。

新增職責：

- 在 `SnapshotOwnedSouvenirs()` 後通知 Registry 重建。
- 在 `TryPurchaseSouvenir()` 成功後，如果該紀念品會立即被持有，也要重建 Registry。
- 對外公開管線 API，例如 `ApplyShopShelfEffects`、`GetExtraBagCapacity`、`IsScratchCardFree`。

### SouvenirEffectDispatcher

保留事件訂閱職責：

- 訂閱 `ItemPurchasedEvent`
- 訂閱 `MonsterTradeCompletedEvent`
- 訂閱 `MonsterTradeFailedEvent`

移除或逐步淘汰：

- `ForEachOwned<T>()`
- `ForEachAllSpecial<T>()`

### ShopBase / ShelfShopBase

商店目前在 `ShopBase.ApplyPriceFactor` 裡呼叫：

```csharp
SouvenirManager.Instance.ApplyAllShopDiscounts(ShopID, shelves);
```

以及部分商店呼叫：

```csharp
SouvenirManager.Instance.BuildShopVisualInfos(ShopID, items);
```

建議整合為：

```csharp
SouvenirManager.Instance.ApplyShopShelfEffects(ShopID, shelves);
```

這樣商店只呼叫一次紀念品貨架管線，價格與視覺資料都在同一條管線完成。

### ShopView / ShopSlot

商店 UI 不需要知道紀念品。Slot 顯示時只讀取：

```csharp
shelfSlot.VisualInfo
```

建議讓支援視覺資訊的 Slot 實作可選 View 介面：

```csharp
public interface IShelfSlotVisualInfoView
{
    void ApplyVisualInfo(ShelfSlotVisualInfo info);
}
```

`ShopSlotBase.Setup` 可以做：

```csharp
if (this is IShelfSlotVisualInfoView visualView)
{
    visualView.ApplyVisualInfo(data.VisualInfo);
}
```

沒有折扣圖示需求的商店 Slot 不需要實作這個介面。

---

## 遷移步驟

### 第一階段：建立 Registry，但保留現有介面

目標是降低風險，不重寫所有紀念品。

1. 新增 `SouvenirEffectRegistry`。
2. Registry 內建立各介面的 List 快取。
3. 在 `SouvenirManager.SnapshotOwnedSouvenirs()` 後呼叫 `registry.Rebuild(...)`。
4. `SouvenirEffectDispatcher` 改成使用 Registry 的 List，不再呼叫 `ForEachOwned<T>()`。
5. `GetExtraBagCapacity()` 與 `IsScratchCardFree()` 改用快取結果。

這一階段完成後，效能問題會大幅改善，但現有紀念品類別幾乎不用動。

### 第二階段：整合商店價格與視覺管線

目標是把商店折扣與視覺資訊整合成一次管線呼叫。

1. 新增 `ShopShelfPipelineContext`。
2. 新增 `ApplyShopShelfEffects(shopId, items)`。
3. 在此方法內初始化 `ShelfSlotVisualInfo`。
4. 依序執行原本的 `IShopDiscountProvider` 與 `IShopVisualModifier`。
5. `ShopBase.ApplyPriceFactor` 改呼叫 `ApplyShopShelfEffects`。
6. 移除商店端額外呼叫 `BuildShopVisualInfos` 的需求。

### 第三階段：導入通用 Handler / Pipeline Step

目標是讓未來效果新增時不再新增大量專用 interface。

1. 新增 `ISouvenirEventHandler<TEvent>`。
2. 新增 `ISouvenirPipelineStep<TContext>`。
3. 新紀念品效果優先使用 handler 或 pipeline step。
4. 舊介面用 Adapter 包起來，逐步淘汰。

### 第四階段：商店專屬索引

如果未來商店折扣紀念品非常多，可以再增加 shopId 索引。

```csharp
Dictionary<string, List<ISouvenirPipelineStep<ShopShelfPipelineContext>>> _shopShelfStepsByShopId;
```

通用效果放在 `All`，指定商店效果放在對應 shopId：

```text
ShopShelfPipeline
-> 先跑 All steps
-> 再跑 shopId steps
```

這樣打開某一間商店時，只跑通用效果與該商店相關效果。

---

## 建議資料流範例

### 打開商店

```text
ShelfShopBase.OpenShop()
-> GenerateTodayShopItems
-> SyncPurchaseState
-> PriceCalculationResult 設定基本價格
-> SouvenirManager.ApplyShopShelfEffects
   -> 確保每個 ShelfSlot 都有 VisualInfo
   -> 執行商店價格管線
   -> 執行商店視覺管線
-> ShopViewBase.ShowItems
-> ShopSlot 讀取 ShelfSlot.VisualInfo 顯示折扣圖示
```

### 購買商品

```text
ShelfShopBase.tradeitem()
-> 扣款
-> 加入物品
-> 更新 ShopShelfData
-> GameEventCenter.Publish(ItemPurchasedEvent)
-> SouvenirEffectDispatcher.OnItemPurchased
-> registry.ShopPurchaseListeners
-> 執行購買後紀念品效果
```

### 妖怪交易成功

```text
MonsterTradeMode
-> GameEventCenter.Publish(MonsterTradeCompletedEvent)
-> SouvenirEffectDispatcher.OnMonsterTradeCompleted
-> registry.MonsterTradeListeners
-> registry.MonsterTradeWithRaceListeners
-> 執行交易成功效果
```

### 查詢背包容量

```text
DataManager / PlayerView
-> SouvenirManager.GetExtraBagCapacity()
-> 回傳 Registry 重建時算好的 cached value
```

---

## 風險與注意事項

### 持有狀態更新時機

Registry 是否正確，取決於 `_ownedSouvenirIds` 是否即時更新。所有會改變持有狀態的流程都必須呼叫重新快取，例如：

- 讀檔完成
- 開新局
- 購買成就紀念品後
- 解鎖特殊紀念品後
- 裝備或持有紀念品清單變化後

### 效果執行順序

管線效果可能會互相影響，例如先打折再設原價標籤，或先改價格再加成。建議每個 pipeline step 都有 `Order`：

```csharp
int Order { get; }
```

例如：

- 100：基礎折扣
- 200：特殊活動價格
- 300：視覺標籤
- 900：除錯或統計

### 多個折扣疊加規則

需要先決定折扣是乘算、加算、最低價覆蓋，還是只能取最大折扣。這個規則應該放在 `ShopShelfPipelineContext` 或統一的價格工具裡，不要讓每個紀念品各自決定。

### 特殊紀念品與持有紀念品差異

目前有些特殊紀念品即使不是一般持有狀態，也可能需要監聽全域事件。這類效果應該明確分成：

- Owned effects：只有玩家持有時才生效。
- Global special effects：特殊紀念品全域監聽，用於解鎖條件或進度統計。

Registry 應該分開保存這兩種索引，避免未持有紀念品意外生效。

### UI 不應被管線直接控制

管線只輸出資料，例如 `ShelfSlotVisualInfo`。不要在管線內直接操作 `Image`、`Button`、`GameObject`。這樣才能保持邏輯層與 UI 層分離。

---

## 推薦最終架構摘要

```text
SouvenirManager
  - 建立紀念品
  - 管理持有狀態
  - 對外提供效果 API

SouvenirEffectRegistry
  - 根據持有狀態建立 handler / pipeline 快取
  - 保存 aggregate cache

SouvenirEffectDispatcher
  - 訂閱 GameEventCenter
  - 事件發生時查 Registry 執行 handler

SouvenirPipelineService
  - 被商店、背包、刮刮樂等系統主動呼叫
  - 執行管線型效果

ShopView / SlotView
  - 只讀取 VisualInfo 顯示 UI
  - 不知道具體紀念品
```

最優先建議先做第一階段與第二階段。這兩階段能解決目前最實際的效能與重複掃描問題，而且不需要一次把所有紀念品效果重新設計。

