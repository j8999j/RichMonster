---
name: talk-system
description: 本專案（ForTest / 紅盒子）對話系統 (Talksystem) 完整指南。當使用者提到 TalkSystem、對話、talkSystem、StartDialogue、PlayDialogueAsync、OnDialogueEnd、autoLockPlayer、DialogueView、DialogueParser、DialogueCommand、DialogueCommandRegistry、DialogueNode、TalkText、TalkData、talk_sample、對話指令 ([w] [l] [r] [lr] [c] [wait] [speed] [color] [size] [b] [i] [fadein] [fadeout])、自訂指令 RegisterCommand、TMPro rich text、逐字顯示 typewriter、SkipTypewriter、DialogueEndListener、GameDataLoader.LoadDialogueTextAsync、GameDataLoader.PreloadDialoguesByLabelAsync、Dialogue Label、DialogueIdSelect、Addressables label 預載對話、對話 fade in/out、NPC 互動觸發對話、商店問候對話 (WanderingYokaiMerchant 等) 時載入此 skill。
---

# 對話系統 Talksystem

## 0. TL;DR

**全域入口**：`GameManager.Instance.talkSystem`（在 GameManager 上 SerializeField，全程不變）

**啟動對話一行**：`GameManager.Instance.talkSystem.StartDialogue(dialogueText);`

**推薦等待結束**：`await talkSystem.PlayDialogueAsync(dialogueText);`

**決策樹**：

| 任務 | 跳到 |
|---|---|
| 觸發一段對話 | §3.1 三種 StartDialogue 過載 |
| 等對話結束做事（如開商店、推進任務） | §4 `PlayDialogueAsync` 模式 + §6 範例 |
| 對話文本怎麼寫（指令） | §5 內建指令完整表 |
| 加入新的自訂指令 | §3.4 RegisterCommand |
| 中斷 / 暫停 / 恢復對話 | §3.3 控制 API |
| 從統一入口載對話 | §6.2 GameDataLoader 範例 |
| 對話卡住 / 不結束 | §8 Pitfalls |

**不要做的事**：
- 不要 `new TalkSystem()` — 它是場景中的 MonoBehaviour，由 GameManager Inspector 拖入
- 不要在 `OnDialogueEnd` callback 裡再 `StartDialogue`（會在 EndDialogue 還沒結束時重入）
- 不要訂閱 `OnDialogueEnd` 而不解訂閱 — 同一個 callback 會累積觸發
- 不要直接改 `DialogueView.dialogueText.text` — 走 `talkSystem` 的 API
- 不要在自訂指令裡用 `[w]` / `[l]` 等保留字當 keyword（`DialogueCommandRegistry` 會拒絕）

---

## 1. 關鍵檔案

| 路徑 | 角色 |
|---|---|
| [Assets/Script/TalkSystem/TalkSystem.cs](Assets/Script/TalkSystem/TalkSystem.cs) | 主控制器：狀態機、typewriter coroutine、事件、指令分派 |
| [Assets/Script/TalkSystem/DialogueParser.cs](Assets/Script/TalkSystem/DialogueParser.cs) | 純文字解析器：`Parse(string/TextAsset/List<string>) → List<DialogueNode>` |
| [Assets/Script/TalkSystem/DialogueCommand.cs](Assets/Script/TalkSystem/DialogueCommand.cs) | `DialogueCommandRegistry`、`CommandHandler` delegate、`DialogueNode` |
| [Assets/Script/TalkSystem/DialogueView.cs](Assets/Script/TalkSystem/DialogueView.cs) | View：TMP_Text 顯示、CanvasGroup fade、繼續指示器、說話者名稱 |
| [Assets/Script/TalkSystem/TalkTest.cs](Assets/Script/TalkSystem/TalkTest.cs) | 最小範例（用 `DialogueIdSelect` 選 ID，再交給 `GameDataLoader` 載入） |
| [Assets/GameData/GameSystem/GameDataLoader.cs](Assets/GameData/GameSystem/GameDataLoader.cs) | `PreloadDialoguesByLabelAsync` + `LoadDialogueTextAsync`：以 `Dialogue` label 預載所有對話，再由 ID 取文案 |
| [Assets/Resources/TalkData/talk_sample.txt](Assets/Resources/TalkData/talk_sample.txt) | 內建指令展示文本 |
| [Assets/Resources/TalkData/](Assets/Resources/TalkData/) | 對話文本資源資料夾（talk_*.txt） |
| [Assets/GameSet/TalkText/](Assets/GameSet/TalkText/) | 教學任務專用對話資料夾 |
| [Assets/GameData/GameSystem/Guide/GuideStep.cs](Assets/GameData/GameSystem/Guide/GuideStep.cs) | `ForceDialogueStep`：透過 `GameDataLoader.LoadDialogueTextAsync` 載入教學對話 |
| [Assets/GameData/GameSystem/Guide/GuideListener.cs](Assets/GameData/GameSystem/Guide/GuideListener.cs) | `DialogueEndListener`：舊版 event flow 的包裝器；新流程優先用 `PlayDialogueAsync` |
| [Assets/Script/Editor/EditorDialogueDataLoader.cs](Assets/Script/Editor/EditorDialogueDataLoader.cs) | Editor 端自動同步 Dialogue label，並提供對話 ID 清單 |
| [Assets/Script/Editor/DialogueIdDrawer.cs](Assets/Script/Editor/DialogueIdDrawer.cs) | `DialogueIdSelect` 專用可搜尋下拉框 |

---

## 2. 系統架構

```
你的腳本
  └─→ GameManager.Instance.talkSystem
                  │
                  │ StartDialogue(text)
                  ▼
        ┌─────────────────┐
        │  DialogueParser │  純 static，把 raw text → List<DialogueNode>
        └─────────────────┘
                  │
                  ▼
        ┌─────────────────────────────────┐
        │  TalkSystem (MonoBehaviour)     │
        │  - 狀態機 (active/typing/wait)   │
        │  - typewriter coroutine         │
        │  - OnDialogueEnd 事件           │
        │  - DialogueCommandRegistry      │
        └─────────────────────────────────┘
                  │
                  │ 呼叫 View
                  ▼
        ┌──────────────────┐
        │  DialogueView    │  TMP_Text + CanvasGroup
        └──────────────────┘
```

**節點類型**（`DialogueNodeType`）：
- `Text`：純文字（含 TMPro rich text tag，由 typewriter 逐字顯示）
- `Command`：流程控制指令（暫停 typewriter、執行動作）

**轉換規則**（DialogueParser）：
- 格式指令 `[color] [size] [b] [i]` → 直接轉成 TMPro rich text tag 嵌入文字節點
- 流程指令 `[w] [l] [r] [lr] [c] [wait] [speed] [fadein] [fadeout]` + 自訂指令 → 留為 `Command` 節點
- 轉義：`[[` → 顯示為 `[`

---

## 3. TalkSystem 公開 API

### 3.1 開始對話（推薦走對話 ID）

```csharp
// 來源 A：對話 ID（推薦）
[DialogueIdSelect]
[SerializeField] private string dialogueId = "talk_sample";
string dialogueText = await GameDataLoader.LoadDialogueTextAsync(dialogueId);
GameManager.Instance.talkSystem.StartDialogue(dialogueText);

// 來源 B：runtime 字串（適合動態組合）
GameManager.Instance.talkSystem.StartDialogue("你好[w]再見");

// 來源 C：多行字串列表
GameManager.Instance.talkSystem.StartDialogue(new List<string> {
    "你好[w]",
    "再見"
});
```

### 3.2 事件（進階 / 舊流程）

| 事件 | 簽章 | 觸發時機 |
|---|---|---|
| `OnDialogueEnd` | `Action` | 所有節點處理完、進入 `EndDialogue()` |
| `OnTextUpdated` | `Action<string>` | typewriter 每顯示一字觸發一次（高頻，少用） |
| `OnWaitingForInput` | `Action` | 進入 `[w]` / `[l]` / `[lr]` 等待玩家按鍵 |

**建議**：新流程優先用 `PlayDialogueAsync(...)`，只有在你真的需要廣播式事件或觀察型 UI 時才直接訂 `OnDialogueEnd`。

### 3.3 流程控制 API

| 方法 | 用途 |
|---|---|
| `PlayDialogueAsync(string/TextAsset/List<string>)` | 啟動對話並等待自然結束；若被中斷則回傳 `false` |
| `Next()` | 玩家按下繼續鍵時呼叫（若 `enableKeyInput=false`） |
| `StopDialogue()` | 強制中斷（停 coroutine、清狀態） |
| `Pause()` / `Resume()` | 暫停／恢復 typewriter 進度 |
| `SetDialogueView(view)` | runtime 換 View 引用 |
| `IsDialogueActive` / `IsWaitingForInput` / `IsTyping` | 狀態查詢 |

### 3.4 自訂指令 RegisterCommand

```csharp
GameManager.Instance.talkSystem.RegisterCommand("playsfx", parameters => {
    if (parameters.Count > 0) {
        AudioManager.Instance.Play(parameters[0]);
    }
});
```
- `keyword` 不可與內建衝突（`w/l/r/lr/c/wait/speed/fadein/fadeout/color/size/b/i` 等）
- 執行時的 keyword 寫法：`[playsfx,bell]`
- 例外會被 `DialogueCommandRegistry.ExecuteCommand` 捕獲並 LogError

---

## 4. `PlayDialogueAsync` 模式（最常見用法）

```csharp
private async void StartGreetingDialogue() {
    var talk = GameManager.Instance.talkSystem;
    if (talk == null) {
        OnGreetingFinished();
        return;
    }
    string dialogueText = await GameDataLoader.LoadDialogueTextAsync(_greetingDialogueId);
    bool completed = await talk.PlayDialogueAsync(dialogueText);
    if (completed) OnGreetingFinished();
}
```

**優點**：
- 不需要自己訂閱 / 解訂閱 `OnDialogueEnd`
- 對話被 `StopDialogue()` 或新對話蓋掉時，會回傳 `false`
- `autoLockPlayer` 會在 `TalkSystem` 內自動處理，不必每個呼叫端重寫一次 lock / unlock

**舊版 event flow** — 只有在你真的需要事件模式時才用 [DialogueEndListener](Assets/GameData/GameSystem/Guide/GuideListener.cs)：
```csharp
var listener = new DialogueEndListener();
listener.StartListen(() => {
    listener.StopListen();   // 自動解訂閱
    DoSomething();
});
GameManager.Instance.talkSystem.StartDialogue(text);
```

---

## 5. 內建指令完整表

### 5.1 流程控制（暫停 typewriter）

| 指令 | 行為 |
|---|---|
| `[w]` | 等待按鍵 → 清除文字 → 繼續 |
| `[l]` | 等待按鍵 → 直接追加後續文字 |
| `[lr]` | 等待按鍵 → 換行 → 繼續 |
| `[r]` | 立即換行（不等待） |
| `[c]` | 立即清除文字 |
| `[wait,毫秒]` | 自動等待 N 毫秒（`[wait,1000]` = 1 秒） |
| `[speed,秒/字]` | 修改 typewriter 速度（`[speed,0.1]` 較慢、`[speed,0.05]` 預設） |

### 5.2 面板淡入淡出

| 指令 | 行為 |
|---|---|
| `[fadein,秒]` | 對話面板淡入（`[fadein,0.5]` = 0.5 秒）通常放開頭 |
| `[fadeout,秒]` | 對話面板淡出，通常放結尾 |

### 5.3 格式指令（轉成 TMPro rich text）

| 指令 | 對應 |
|---|---|
| `[color,#RRGGBB]...[/color]` | `<color=#RRGGBB>...</color>` |
| `[size,N%]...[/size]` | `<size=N%>...</size>` |
| `[b]...[/b]` | `<b>...</b>` |
| `[i]...[/i]` | `<i>...</i>` |

格式指令可嵌套，與流程指令穿插：
```
[color,#FF5555]注意！[/color]這裡有[b]重要[/b]訊息。[w]
```

---

## 6. 範例

### 6.1 NPC / 商人觸發對話 → 結束後開商店

見 [Assets/Script/Shop/WanderingYokaiMerchant.cs](Assets/Script/Shop/WanderingYokaiMerchant.cs) 的完整流程：

```csharp
public class WanderingSO : ScriptableObject {
    [ShopIDSelector] public string ShopID;
    [DialogueIdSelect] public string GreetingDialogueId;
}

private string _greetingDialogueId;
private bool _hasGreetedToday;
private bool _inDialogue;

public void Initialize(WanderingSO config) {
    ShopID = config.ShopID;
    _greetingDialogueId = config.GreetingDialogueId;
    GetShopData();
}

protected override void OnInteract() {
    if (_inDialogue) return;   // 對話中不重入
    if (_hasGreetedToday) OpenShop();
    else                  StartGreetingDialogue();
}

private async void StartGreetingDialogue() {
    var talk = GameManager.Instance.talkSystem;
    if (talk == null) {
        _hasGreetedToday = true;
        OpenShop();
        return;
    }
    _inDialogue = true;
string dialogueText = await GameDataLoader.LoadDialogueTextAsync(_greetingDialogueId);
    bool completed = await talk.PlayDialogueAsync(dialogueText);
    _inDialogue = false;
    if (!completed) return;
    _hasGreetedToday = true;
    OpenShop();
}
```

**重點**：
- `_inDialogue` 旗標守門（雙保險）
- `GreetingDialogueId` 由 `WanderingSO` 持有，並用 `DialogueIdSelect` 選取
- 對話期間的移動 / 互動鎖交給 `TalkSystem.autoLockPlayer`
- 商店自己的 `PlayerLockSources.WanderingYokaiMerchant` 只保留給開店 UI 期間，不再重複用於對話期

### 6.2 從統一入口載對話（教學 / 商店共用模式）

見 [Assets/GameData/GameSystem/Guide/GuideStep.cs](Assets/GameData/GameSystem/Guide/GuideStep.cs#L26):

```csharp
public override void Execute(System.Action onComplete) {
    ExecuteAsync(onComplete);
}

private async void ExecuteAsync(System.Action onComplete) {
    string dialogueText = await GameDataLoader.LoadDialogueTextAsync(dialogueId);
    if (!string.IsNullOrEmpty(dialogueText)) {
        bool completed = await GameManager.Instance.talkSystem.PlayDialogueAsync(dialogueText);
        if (completed) onComplete?.Invoke();
    } else {
        Debug.LogError($"[ForceDialogueStep] 找不到對話: {dialogueId}");
        onComplete?.Invoke();   // 容錯：避免步驟卡死
    }
}
```

**重點**：
- `ForceDialogueStep` 直接 `await PlayDialogueAsync`，不再自己接 `OnDialogueEnd`
- `GameDataLoader` 啟動時先用 `Dialogue` label 預載所有對話，再由 ID 取文本
- 載入失敗一定要 `onComplete` 兜底；對話若被外部中斷則不自動推進步驟

### 6.3 自訂指令範例

```csharp
void Awake() {
    GameManager.Instance.talkSystem.RegisterCommand("giveitem", parameters => {
        if (parameters.Count >= 2 && int.TryParse(parameters[1], out int amount)) {
            for (int i = 0; i < amount; i++)
                DataManager.Instance.AddItem(parameters[0], 0);
        }
    });
}
```

對話文本：
```
獎勵給你！[giveitem,potion_red,3][w]
```

---

## 7. 對話文本撰寫範例

[Assets/Resources/TalkData/talk_sample.txt](Assets/Resources/TalkData/talk_sample.txt) 是完整展示：

```
[fadein,0.5]你好！歡迎來到這個世界。[w]
這裡是一個奇妙的地方...[w]
[color,#FF5555]注意！[/color]前方可能有危險！[l]
讓我[b]認真[/b]告訴你一些事情。[w]
[speed,0.1]慢...慢...地...說...[speed,0.05][w]
好的，速度恢復正常了。[r]
[size,150%]這段文字比較大！[/size][w]
[i]這是斜體文字。[/i][lr]
[color,#55FF55]綠色的文字[/color]搭配[color,#5555FF]藍色的文字[/color]。[w]
感謝你的傾聽，再見！[fadeout,0.8]
```

**寫法慣例**：
- 換行只是編輯方便，DialogueParser 會忽略單一 `\n`（要明確換行用 `[r]`）
- 每段話的結尾用 `[w]` 等待玩家按鍵後清空，類似換頁
- 開頭 `[fadein]`、結尾 `[fadeout]` 提供平滑切換

---

## 8. Common Pitfalls（常見陷阱）

| 症狀 | 原因 | 解法 |
|---|---|---|
| 「對話結束後事件跑了 N 次」 | 直接訂了 `OnDialogueEnd` 但沒解 | 優先改成 `await PlayDialogueAsync(...)`；若一定要事件模式，callback 第一行就 `-=` |
| 「對話結束玩家還是動不了」 | 呼叫端自己手動加了鎖，卻沒配對解鎖 | 對話期的鎖優先交給 `TalkSystem.autoLockPlayer`，不要重複手動鎖 |
| 「按 E 在對話中又觸發 Interact」 | 關閉了 `autoLockPlayer` 或在其他腳本搶著解鎖 | 保持 `autoLockPlayer=true`；檢查是否有其他 source 在對話中錯誤解鎖 |
| 「對話沒出現任何文字」 | DialogueView SerializeField 沒指派 | TalkSystem GameObject 上把 dialogueView 拖好 |
| 「指令 `[playsfx,bell]` 沒效」 | 沒呼叫 RegisterCommand 或在 RegisterCommand 之前 StartDialogue | 在 Awake 註冊；確保 keyword 不衝突 |
| 「換行沒生效」 | 預期 `\n` 自動換行 | DialogueParser 忽略單純 `\n`，要用 `[r]` 或 `[lr]` |
| 「`[` 顯示不出來」 | 被當指令解析 | 用 `[[` 轉義 |
| 「`Could not parse fadein duration`」 | `[fadein,0.5]` 的 `0.5` 用了中文逗號或全形數字 | 改半形 |

---

## 9. 架構觀察與改進建議

### 9.1 強項
- **指令 / 文字 / View 三層解耦**：DialogueParser 純 static、無狀態，TalkSystem 持狀態，DialogueView 純 UI
- **格式 vs 流程指令的雙軌處理**：格式指令直接內聯成 TMPro tag（讓 typewriter 不用懂 rich text），流程指令保留為節點（精準控制 pause）
- **轉義 `[[` 設計**：避免文本中 `[` 字面量被誤判
- **CanvasGroup fadein/fadeout coroutine** 使用 elapsed lerp，不依賴 DOTween，相依少

### 9.2 建議改進

#### ✅ 已落地：對話資源統一走 `GameDataLoader.LoadDialogueTextAsync`
- `GuideStep.cs` 不再直接碰 Addressables
- `WanderingYokaiMerchant` 優先使用 `WanderingSO.GreetingDialogueId`，並透過 `GameDataLoader` 統一取得文本
- `GameDataLoader` 啟動時以 `Dialogue` label 預載所有 `TextAsset`，並用對話 ID 快取
- Editor 端有 `DialogueIdSelect` 下拉與 `EditorDialogueDataLoader` 自動同步 Addressables label

**目前規則**：新增對話呼叫點時，用 `string dialogueId` + `[DialogueIdSelect]` 存 ID，再走 `GameDataLoader.LoadDialogueTextAsync(dialogueId)`；不要在功能腳本內直接寫 `Addressables.LoadAssetAsync<TextAsset>`，也不要再留舊版 `TextAsset` 欄位。

#### ✅ 已落地：`PlayDialogueAsync` 成為標準等待方式
- `TalkSystem` 現在提供 `PlayDialogueAsync(string/TextAsset/List<string>)`
- 對話自然結束回傳 `true`，被 `StopDialogue()` 或新對話中斷時回傳 `false`
- `ForceDialogueStep` 與 `WanderingYokaiMerchant` 已遷移到這條路徑

#### ✅ 已落地：`autoLockPlayer` 統一管理對話期鎖定
- `TalkSystem` 內建 `[SerializeField] bool autoLockPlayer = true`
- 開始對話時自動用 `PlayerLockSources.TalkSystem` 鎖住移動與互動
- 結束或中斷時自動解除
- 已盤點現有對話呼叫點，移除流浪商人對話期的重複手動鎖；商店 UI 期仍保留自己的 `WanderingYokaiMerchant` lock source

#### 🟢 P3：`enableKeyInput` 寫死 nextKey/skipKey
目前 `Space` / `Return` 是 SerializeField，但全專案統一鎖在 TalkSystem 元件 Inspector，不能 runtime 換。

**建議**：改透過 InputAction（Unity 新 Input System）或 KeyBindings 設定取得。

#### 🟢 P3：缺對話狀態存檔（restartable dialogue）
若玩家在對話中存檔退出，重開遊戲不會回到對話。對長對話可能造成體驗斷層。

**建議**：以對話 ID + 當前 `_currentNodeIndex` 寫入 GameSaveFile（用 `SetPlayerData` ISaveData 機制），讀檔時自動續跑。但要注意自訂指令副作用的冪等性。

---

## 10. 與其他系統的關聯

| 系統 | 互動點 |
|---|---|
| **GameManager** | 持有 `talkSystem` 引用（`public TalkSystem talkSystem`），所有對話呼叫的入口 |
| **Guide / Tutorial** | `GuideStep.ForceDialogueStep` 用 `GameDataLoader.LoadDialogueTextAsync` 載對話，再 `await PlayDialogueAsync` 等結束 |
| **Shop（商店）** | `WanderingYokaiMerchant` 用 `WanderingSO` 管商店 ID 與 `GreetingDialogueId`，再透過 `GameDataLoader.LoadDialogueTextAsync` 載問候對話，對話期鎖交給 `TalkSystem` |
| **NPC** | `NpcOnMap` 也可在互動時觸發對話（pattern 同 §6.1） |
| **PlayerController** | 對話期間預設由 `TalkSystem.autoLockPlayer` 經 `PlayerLockSources.TalkSystem` 鎖玩家 |
| **Achievement** | 自訂指令可觸發成就解鎖 |
