using Player;
using UnityEngine;

public class OrderShopGuide : MonoBehaviour, IGuideInteractable
{
    public string ID => GuideIDs.Interactable.GuideOrderShop;
    void Start()
    {
        SetMapGuide();
    }
    public event System.Action<string> OnInteracted;
    public void OnEnable()
    {
        GuideLookupRegistry.Instance.RegisterInteractable(this);
    }
    public void OnDisable()
    {
        GuideLookupRegistry.Instance.UnregisterInteractable(this);
    }
    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID,transform);
        
    }
    public void Interact()
    {
       OnInteracted?.Invoke(ID);
    }
    public void ShowPrompt()
    {
        
    }
    public void HidePrompt()
    {
        
    }
}
