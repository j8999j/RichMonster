---
name: project-systems
description: "Use this skill in the ForTest Unity project when Codex needs to understand, modify, or call existing runtime systems and UI flows: GameManager, GameFlow, DataManager, SaveManager, TalkSystem dialogue commands, StoryPlaybackPanel, SpriteLoader, AudioManager, shops, missions, trades, orders, achievements, souvenirs, scene transitions, player locks, Addressables data, or common invocation patterns."
---

# ForTest Project Systems

Use this skill as the project-specific onboarding map before changing runtime systems, UI panels, save/data flow, dialogue scripts, or asset/audio loading.

## Workflow

1. Read `references/systems.md` for the relevant subsystem before editing code.
2. Prefer existing singletons, view patterns, and helper APIs over new infrastructure.
3. Keep changes scoped to the touched subsystem and preserve serialized Unity fields unless a migration is intentional.
4. For UI changes, check listener cleanup, null guards, audio hooks, save calls, and player-lock behavior.
5. For dialogue/story changes, route through `TalkSystem`, `GameDataLoader`, `StoryPlaybackPanel`, and `SpriteLoader` as documented.

## Reference Map

- `references/systems.md`: Core systems, common call patterns, dialogue commands, UI panel conventions, save/data APIs, SpriteLoader/AudioManager usage, shops, missions, achievements, souvenirs, and project pitfalls.

## Quick Patterns

Play dialogue from an Addressables text ID:

```csharp
string text = await GameDataLoader.LoadDialogueTextAsync(dialogueId);
await GameManager.Instance.talkSystem.PlayDialogueAsync(text);
```

Load a sprite by project ID:

```csharp
SpriteLoader.LoadSpriteAsync(imageId, sprite =>
{
    targetImage.sprite = sprite;
    SpriteLoader.AdjustImageScale(targetImage, sprite);
});
```

Play a UI SFX:

```csharp
if (AudioManager.Instance != null && clip != null)
{
    AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
}
```
