using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Talksystem;

public class TalkTest : MonoBehaviour
{
    [SerializeField] private TalkSystem talkSystem;
    [DialogueIdSelect]
    [SerializeField] private string dialogueId = "talk_sample";

    private async void Start()
    {
        string dialogueText = await GameDataLoader.LoadDialogueTextAsync(dialogueId);
        if (talkSystem != null && !string.IsNullOrEmpty(dialogueText))
        {
            talkSystem.StartDialogue(dialogueText);
        }
    }
}
