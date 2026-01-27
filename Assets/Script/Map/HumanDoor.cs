using GameSystem;
using Player;
using UnityEngine;
public class HumanDoor : MonoBehaviour, IInteractable
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
        GameManager.Instance.gameFlow.SwitchGameStageAndSave(DayPhase.HumanDay);
        GameManager.Instance.GoToHumanScene();
    }

}