using System;
using System.Threading.Tasks;
using GameSystem;
using Talksystem;
using UnityEngine;

public class EndStoryPlayer : MonoBehaviour
{
    [SerializeField]
    private TalkSystem talkSystem;

    [SerializeField]
    private bool returnToMainMenuOnComplete = true;

    [SerializeField]
    private EndingDialogueEntry[] endingDialogues =
    {
        new EndingDialogueEntry(EndingType.Type1, "EndStory_Type1_Dialogue"),
        new EndingDialogueEntry(EndingType.Type2, "EndStory_Type2_Dialogue"),
        new EndingDialogueEntry(EndingType.Type3, "EndStory_Type3_Dialogue"),
        new EndingDialogueEntry(EndingType.Type4, "EndStory_Type4_Dialogue"),
        new EndingDialogueEntry(EndingType.Type5, "EndStory_Type5_Dialogue")
    };

    private bool isPlaying;

    private async void Start()
    {
        await PlayCurrentEndingAsync();
    }

    public async Task PlayCurrentEndingAsync()
    {
        if (isPlaying)
            return;

        isPlaying = true;

        EndingType endingType = ResolveEndingType();
        string dialogueId = GetDialogueId(endingType);

        if (!string.IsNullOrWhiteSpace(dialogueId))
        {
            string dialogueText = await GameDataLoader.LoadDialogueTextAsync(dialogueId);
            TalkSystem player = ResolveTalkSystem();
            if (!string.IsNullOrWhiteSpace(dialogueText) && player != null)
                await player.PlayDialogueAsync(dialogueText);
        }
        else
        {
            Debug.LogWarning($"[{nameof(EndStoryPlayer)}] Missing ending dialogue for {endingType}.");
        }

        isPlaying = false;

        if (returnToMainMenuOnComplete)
            ReturnToMainMenu();
    }

    private EndingType ResolveEndingType()
    {
        var playerData = DataManager.Instance?.CurrentPlayerData;
        if (playerData != null && playerData.HasReachedEnding)
            return playerData.ReachedEndingType;

        return EndingType.None;
    }

    private TalkSystem ResolveTalkSystem()
    {
        if (talkSystem != null)
            return talkSystem;

        var gameManager = FindObjectOfType<GameManager>(true);
        if (gameManager != null && gameManager.talkSystem != null)
        {
            talkSystem = gameManager.talkSystem;
            return talkSystem;
        }

        talkSystem = FindObjectOfType<TalkSystem>(true);
        return talkSystem;
    }

    private string GetDialogueId(EndingType endingType)
    {
        if (endingDialogues != null)
        {
            foreach (var entry in endingDialogues)
            {
                if (entry.Ending == endingType)
                    return entry.DialogueId;
            }
        }

        return string.Empty;
    }

    private void ReturnToMainMenu()
    {
        var sceneTransitionManager = FindObjectOfType<SceneTransitionManager>(true);
        if (sceneTransitionManager != null)
        {
            sceneTransitionManager.GoToMainMenu();
            return;
        }

        Debug.LogError($"[{nameof(EndStoryPlayer)}] SceneTransitionManager not found.");
    }

    [Serializable]
    private struct EndingDialogueEntry
    {
        public EndingType Ending;

        [DialogueIdSelect]
        public string DialogueId;

        public EndingDialogueEntry(EndingType ending, string dialogueId)
        {
            Ending = ending;
            DialogueId = dialogueId;
        }
    }
}
