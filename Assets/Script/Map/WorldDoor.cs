using GameSystem;
using Player;
using UnityEngine;
public class WorldDoor : MonoBehaviour, IInteractable, IMapGuideTarget
{
    [SerializeField] private GameObject interactPrompt;
    public string ID => "WorldDoor";
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
    void NextDayWorldDoor()
    {
        GameManager.Instance.gameFlow.NextDay();
        GameManager.Instance.GoToMonsterScene();
    }

}