using GameSystem;
using Player;
using UnityEngine;
public class HumanDoor : MonoBehaviour, IInteractable, IMapGuideTarget
{
    [SerializeField] private GameObject interactPrompt;
    public string ID => GuideIDs.Interactable.HumanDoor;
    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID,transform);
    }
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
    async void NextDayWorldDoor()
    {
        await GameManager.Instance.gameFlow.SwitchGameStageAndSaveAsync(DayPhase.HumanDay);
        GameManager.Instance.GoToHumanScene();
    }

}
