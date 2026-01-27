using UnityEngine;
using Player;
using System;
using System.Collections;
public class Home : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private GameObject TradeCamera;

    private MonsterTradeView monsterTradeView;
    private bool CanInteract;
    void Start()
    {
        monsterTradeView = GetComponent<MonsterTradeView>();
        CanInteract = true;
    }
    public void ShowPrompt()
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (interactPrompt != null)
        {
            
            interactPrompt.SetActive(false);
        }
    }

    public void Interact()
    {
        if (!CanInteract)
            return;
        if (TradeCamera.activeSelf)
        {
            TradeCamera.SetActive(false);
            monsterTradeView.ExitShopUI();
            return;
        }
        StartCoroutine(InteractCoroutine());
    }
    private IEnumerator InteractCoroutine()
    {
        CanInteract = false;
        //TradeCamera.SetActive(!TradeCamera.activeSelf);
        yield return new WaitForSeconds(0.8f);
        Debug.Log("open");
        monsterTradeView.OpenShopUI();
        CanInteract = true;

    }
}

