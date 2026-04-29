using UnityEngine;

[CreateAssetMenu(fileName = "WanderingSO", menuName = "GameSet/WanderingSO", order = 2)]
public class WanderingSO : ScriptableObject
{
    [ShopIDSelector]
    public string ShopID;

    [DialogueIdSelect]
    public string GreetingDialogueId;
}
