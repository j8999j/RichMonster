using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Talksystem;

public class TalkTest : MonoBehaviour
{
    [SerializeField]private TalkSystem talkSystem;
    [SerializeField]private TextAsset textAsset;
    void Start()
    {
        talkSystem.StartDialogue(textAsset);
    }
}
