using Player;
using UnityEngine;

public class OrderShopGuide : MonoBehaviour, IGuideInteractable
{
    public string ID => "OrderShop";
    void Start()
    {
        SetGuideID();
        SetMapGuide();
    }
    public event System.Action<string> OnInteracted;
    public void SetGuideID()
    {
        
    }
    public void SetMapGuide()
    {
        
    }
    public void Interact()
    {
       
    }
    public void ShowPrompt()
    {
        
    }
    public void HidePrompt()
    {
        
    }
}
