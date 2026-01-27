using GameSystem;
using Player;
using UnityEngine;
public class WorldDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactPrompt;
    public void Interact()
    {
        //傳送前往妖界
        NextDayWorldDoor();
    }
    public void ShowPrompt()
    {
        interactPrompt.SetActive(true);
    }
    public void HidePrompt()
    {

    }
    void NextDayWorldDoor()
    {
        GameManager.Instance.gameFlow.NextDay();
        GameManager.Instance.GoToMonsterScene();
    }

}