using UnityEngine;
using Player;
using GameSystem;
using System;
using System.Collections;
public class Home : MonoBehaviour, IGuideInteractable
{
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private GameObject TradeCamera;
    [SerializeField] private Transform GuideTransform;
    private MonsterTradeMode monsterTradeMode;
    private bool CanInteract;
    public string ID => GuideIDs.Interactable.GuideGroceryStore;
    public event Action<string> OnInteracted;
    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID, GuideTransform);
    }
    void Awake()
    {
        SetMapGuide();
    }
    void OnEnable()
    {
        GuideLookupRegistry.Instance.RegisterInteractable(this);
    }
    void OnDisable()
    {
        GuideLookupRegistry.Instance.UnregisterInteractable(this);
    }
    void Start()
    {
        monsterTradeMode = GetComponent<MonsterTradeMode>();
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
        OnInteracted?.Invoke(ID);
        // 鎖定玩家移動與互動，防止透過再次點擊互動關閉頁面
        GameManager.Instance.LockPlayerMove(PlayerLockSources.MonsterTrade);
        GameManager.Instance.LockPlayerInteract(PlayerLockSources.MonsterTrade);
        StartCoroutine(InteractCoroutine());
    }
    private IEnumerator InteractCoroutine()
    {
        CanInteract = false;
        yield return new WaitForSeconds(0.01f);
        monsterTradeMode.InteractShopUI();
        CanInteract = true;
    }
}

