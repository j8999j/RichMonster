# ForTest Project Systems Reference

## Scope

Use this reference when modifying systems under `Assets/GameData/GameSystem`, `Assets/Script/TalkSystem`, `Assets/Script/UI`, `Assets/Script/Shop`, `Assets/Script/Trade`, `Assets/Script/Mission`, `Assets/Script/Achievement`, or `Assets/Script/Souvenir`.

## Core Runtime Singletons

### GameManager

Location: `Assets/GameData/GameSystem/GameManager.cs`

Primary role: global scene/game lifecycle coordinator. Use `GameManager.Instance`; do not instantiate it manually.

Common members and calls:

- `GameManager.Instance.talkSystem`: active `TalkSystem` used by project dialogue.
- `GameManager.Instance.gameFlow`: current `GameFlow`, created during game initialization.
- `InitializeGame(slot)`: initializes a save slot and runtime flow.
- `GoToMainMenu()`, `GoToHumanScene()`, `GoToMonsterScene()`, `GoToNextDay()`: high-level scene/day transitions.
- `LoadScene(sceneName)`: lower-level scene transition through the project loader.
- `LockPlayerMove(source)`, `UnlockPlayerMove(source)`, `LockPlayerInteract(source)`, `UnlockPlayerInteract(source)`: lock movement or interaction while UI/dialogue is active.

Use source constants from `Assets/GameData/GameSystem/PlayerLockSources.cs` instead of ad hoc string literals.

### GameFlow

Location: `Assets/GameData/GameSystem/GameFlow.cs`

Primary role: current day/stage flow and save orchestration.

Common calls:

- `SaveGameAsync()`: persist current game state.
- `NextDay()`: advance to next day.
- `SwitchGameStageAndSave(...)`: change stage and save.
- `StartTutorial(...)`: start guide/tutorial flow.

When UI actions mutate player state, call the relevant `DataManager` mutator first, then save through `GameManager.Instance.gameFlow.SaveGameAsync()` when the workflow expects persistence.

### DataManager

Location: `Assets/GameData/GameSystem/DataManager.cs`

Primary role: static game dictionaries plus current runtime player/book state.

Common data access:

- `GetItemById(id)`, `ItemDict`
- `GetMonsterInfoById(id)`, `MonsterProfessionDict`
- `GetMonsterStoriesByMonsterID(monsterId)`
- `GetPlayerSaveData()`, `GetPersistentSaveData()`

Common mutation:

- `ModifyGold(amount)`
- `TrySpendGold(amount)`
- `AddItem(...)`
- `SetPlayerData(...)`
- `UnlockMonsterInformation(...)`
- `ConfirmSingleNewInfo(...)`
- `ConfirmSingleNewStory(...)`
- `SaveCurrentPlayerAsync()`
- `SaveBookAsync()`

Prefer DataManager APIs to direct mutation of save objects. If no API exists, follow nearby code and ensure the modified save object is written back and saved.

### SaveManager

Location: `Assets/GameData/GameSystem/SaveManager.cs`

Primary role: save file I/O and slot operations.

Common calls:

- `SaveGameAsync(...)`
- `Load(...)`
- `DeleteSaveSlot(...)`
- `ClearBookData()`
- `UnlockAllBookData()`
- `OpenSaveFolder()`

Book data is stored separately from slot data, including the illustrated book file.

### Scene Loading

Locations:

- `Assets/GameData/GameSystem/SceneTransitionManager.cs`
- `Assets/GameData/GameSystem/AddressableSceneLoader.cs`

Use GameManager's high-level scene methods when possible. Use lower-level loaders only when a subsystem already owns scene transition details.

## Static Data and Addressables

### GameDataLoader

Location: `Assets/GameData/GameSystem/GameDataLoader.cs`

Primary role: load JSON/static data and dialogue text from Addressables.

Dialogue text pattern:

```csharp
string text = await GameDataLoader.LoadDialogueTextAsync(dialogueId);
await GameManager.Instance.talkSystem.PlayDialogueAsync(text);
```

Dialogue assets are cached by text asset name/address and use the project's dialogue Addressables label.

### SpriteLoader

Location: `Assets/Script/UI/SpriteLoader.cs`

Primary role: load sprites by project ID from the project atlas, with fallback handling.

Pattern:

```csharp
SpriteLoader.LoadSpriteAsync(spriteId, sprite =>
{
    targetImage.sprite = sprite;
    SpriteLoader.AdjustImageScale(targetImage, sprite);
});
```

Use SpriteLoader for item icons, monster icons, story images, rarity icons, tags, NPC heads, event images, and souvenir icons. Do not bypass it with direct Addressables sprite loads unless the existing local subsystem already does that.

### AudioManager

Location: `Assets/GameData/GameSystem/AudioManager.cs`

Common SFX pattern:

```csharp
if (AudioManager.Instance != null && clip != null)
{
    AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
}
```

Typical UI SFX fields are `openPanelSfx`, `closePanelSfx`, `buttonClickSfx`, `itemClickSfx`, `buySuccessSfx`, `buyFailedSfx`, and `sfxVolumeScale`.

## Dialogue System

Locations:

- `Assets/Script/TalkSystem/TalkSystem.cs`
- `Assets/Script/TalkSystem/DialogueView.cs`
- `Assets/Script/TalkSystem/DialogueParser.cs`
- `Assets/Script/TalkSystem/DialogueCommand.cs`
- `Assets/Script/TalkSystem/TalkTest.cs`

Preferred API:

```csharp
string dialogueText = await GameDataLoader.LoadDialogueTextAsync(dialogueId);
bool completed = await GameManager.Instance.talkSystem.PlayDialogueAsync(dialogueText);
```

Use `PlayDialogueAsync` when caller logic must wait for dialogue completion. `StartDialogue` is lower-level fire-and-forget style.

Common text commands include:

- `[w]`: wait for player advance.
- `[l]`, `[lr]`, `[r]`, `[c]`: dialogue view layout/position commands.
- `[wait,ms]`: wait fixed milliseconds.
- `[speed,value]`: change typing speed.
- `[fadein,seconds]`, `[fadeout,seconds]`: dialogue fade.
- Rich-text commands are parsed by `DialogueParser`.

### StoryPlaybackPanel Commands

Locations:

- `Assets/Script/UI/Book/StoryPlaybackPanel.cs`
- `Assets/Script/TalkSystem/TalkSystem.cs`
- `Assets/Script/TalkSystem/DialogueCommand.cs`

Story panel commands are part of TalkSystem command handling. The panel uses the active TalkSystem dialogue flow and does not require its own serialized `DialogueView`.

Supported forms:

```text
[storypanel,show]
[storypanel,show,0.5]
[storypanel,image,Story_0_0]
[storypanel,close]
[storypanel,close,0.3]

[storyopen,0.5]
[storyimage,Story_0_0]
[storyclose,0.3]
```

Behavior:

- `show` / `storyopen`: open the story panel, optionally using fade duration.
- `image` / `storyimage`: load the image ID through `SpriteLoader.LoadSpriteAsync`.
- `close` / `storyclose`: close the panel, optionally using fade duration.
- `StoryPlaybackPanel` supports fade-in/fade-out settings and should be assigned on TalkSystem or found in scene by existing fallback behavior.

Example dialogue:

```text
[storyopen,0.2]
[storyimage,Story_0_0]
First line of story.[w]
[storyimage,Story_0_1]
Second line of story.[w]
[storyclose,0.2]
```

## UI View Conventions

General pattern:

- Register button listeners in `Awake` or initialization.
- Remove listeners in `OnDestroy`.
- Null-check serialized refs before use when a missing assignment should not crash the entire scene.
- Use `SetActive`, `CanvasGroup`, or existing panel visibility fields according to the local view.
- Play open/close/click SFX through `AudioManager.Instance.PlaySfx`.
- Save after meaningful player-state changes.
- Lock player movement/interaction while modal UI blocks gameplay, if nearby views do so.

### StopPanelView

Location: `Assets/Script/UI/StopPanelView.cs`

Current pattern:

- `StopButton`: click SFX, then opens panel.
- `HomeButton`: click SFX, save, then go to main menu.
- `NotionButton`: click SFX, then close panel.
- `ClosePanelButton`: click SFX, then close panel.
- `openPanelSfx`, `closePanelSfx`, `buttonClickSfx`, `sfxVolumeScale`: panel audio hooks.

Use the exposed close method for both notion/back buttons and explicit close UI.

### GameBookView

Location: `Assets/Script/UI/Book/GameBookView.cs`

Important current requirement: monster story buttons should only display text content in the book UI. Do not route GameBookView story buttons through StoryPlaybackPanel unless explicitly requested again.

Story text data comes from `DataManager.GetMonsterStoriesByMonsterID(monsterId)`, and new-story badges/state use `ConfirmSingleNewStory(...)` style APIs.

## Shops and Purchases

Locations:

- `Assets/Script/Shop/ShopViewBase.cs`
- `Assets/Script/Shop/*ShopView.cs`
- `Assets/Script/Shop/WanderingYokaiMerchant.cs`

Patterns:

- Views commonly use `PanelisVisible` to toggle open/close state.
- Panel open/close and item actions play SFX.
- Items, tags, rarity icons, and shop images load through `SpriteLoader`.
- Purchases use DataManager spending/mutation APIs.
- Successful state changes should save through `GameManager.Instance.gameFlow.SaveGameAsync()` when the existing view does so.

Wandering merchant greeting pattern:

```csharp
string dialogueText = await GameDataLoader.LoadDialogueTextAsync(_greetingDialogueId);
bool completed = await talk.PlayDialogueAsync(dialogueText);
```

Open shop UI only after required dialogue completes.

## Trade, Orders, and Missions

Locations:

- `Assets/Script/Trade/HumanOrderView.cs`
- `Assets/Script/Trade/MonsterTradeView.cs`
- `Assets/Script/Order`
- `Assets/Script/Mission/NPCMissionView.cs`
- `Assets/Script/Mission`

Patterns:

- UI icons use `SpriteLoader`.
- Progress/state should flow through DataManager or the existing save data model.
- Reward item slots and requirement slots should mirror nearby slot setup code.
- Save after accepting, completing, or mutating mission/order/trade progress when existing workflows do.

## Achievements and Souvenirs

Achievement locations:

- `Assets/Script/Achievement/AchievementManager.cs`
- `Assets/Script/Achievement/AchievementEvents.cs`
- `Assets/Script/Achievement/AchievementViewFactory.cs`
- `Assets/GameSet/AchievementSet`

Patterns:

- Achievement classes live under `Assets/GameSet/AchievementSet`.
- Use project achievement events instead of polling when adding new triggers.
- Persist through DataManager achievement save helpers already used nearby.

Souvenir locations:

- `Assets/Script/Souvenir/SouvenirManager.cs`
- `Assets/Script/Souvenir`
- `Assets/GameSet/Souvenir/SouvenirEffect`

Patterns:

- Souvenir icons use `SpriteLoader`.
- Special souvenir effects live under `Assets/GameSet/Souvenir/SouvenirEffect`.
- Persist through the existing DataManager/SouvenirManager save helpers.

## Common Implementation Checklists

### Add a UI panel action

1. Add serialized fields for button/panel/audio only if the view does not already expose them.
2. Register listeners in `Awake`.
3. Remove listeners in `OnDestroy`.
4. Add one public method for the action if other UI needs to call it.
5. Play button/open/close SFX with null checks.
6. Save through GameFlow only if player state changed.

### Play project dialogue

1. Load text with `GameDataLoader.LoadDialogueTextAsync(dialogueId)`.
2. Await `GameManager.Instance.talkSystem.PlayDialogueAsync(text)`.
3. Gate follow-up UI or state changes on the returned completion value when needed.

### Add story panel playback to dialogue

1. Ensure TalkSystem has or can find a `StoryPlaybackPanel`.
2. Use `[storyopen,duration]`.
3. Use `[storyimage,imageId]` for each image change.
4. Write normal dialogue lines and `[w]` waits between image changes.
5. End with `[storyclose,duration]`.

### Load an image/icon

1. Use `SpriteLoader.LoadSpriteAsync(id, callback)`.
2. Assign sprite inside callback.
3. Call `SpriteLoader.AdjustImageScale(image, sprite)` when the UI expects fit scaling.

### Save custom data

1. Prefer an existing DataManager mutator.
2. If adding new save fields, update the save data model and serialization users.
3. Save with `GameManager.Instance.gameFlow.SaveGameAsync()` for slot data or DataManager book/persistent save APIs for those domains.

## Pitfalls

- Do not instantiate project singletons manually.
- Do not mutate current player/book save objects directly when a DataManager API exists.
- Do not use raw strings for player lock sources; use `PlayerLockSources`.
- Do not make GameBookView story buttons launch story playback unless the user asks for that behavior.
- Do not add a separate `DialogueView` field to `StoryPlaybackPanel`; it uses the dialogue system's active flow.
- Do not bypass `SpriteLoader` for normal project sprites.
- Some terminal output may show mojibake for existing Chinese comments/text. Inspect source behavior, not terminal encoding artifacts.
