using GameSystem;
using Player;
using UnityEngine;

public class CollectionMissionNpc : MonoBehaviour, IInteractable, IMapGuideTarget
{
    [SerializeField]
    private CollectionMissionTracker tracker;

    [SerializeField]
    private CollectionMissionRace race;

    [SerializeField]
    private GameObject prompt;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    public string ID => GuideIDs.Interactable.CollectionMission(race);

    public CollectionMissionTracker Tracker => tracker;

    public CollectionMissionRace Race => race;

    private void Awake()
    {
        if (tracker == null)
            tracker = FindObjectOfType<CollectionMissionTracker>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID, transform);
    }

    public void ShowPrompt()
    {
        if (prompt != null)
            prompt.SetActive(true);
    }

    public void HidePrompt()
    {
        if (prompt != null)
            prompt.SetActive(false);
    }

    public void Interact()
    {
        if (GameManager.Instance.IsPlayerMoveLocked(PlayerLockSources.NpcOnMap))
        {
            CloseView();
            return;
        }

        if (tracker == null)
        {
            Debug.LogWarning($"[{nameof(CollectionMissionNpc)}] Missing collection mission tracker reference on {name}.");
            return;
        }

        tracker.OpenMission(race);
    }

    public void CloseView()
    {
        if (tracker != null)
            tracker.CloseMission();
    }

    public void Setup(CollectionMissionTracker collectionTracker)
    {
        tracker = collectionTracker;
    }

    public void Setup(CollectionMissionTracker collectionTracker, CollectionMissionRace collectionRace)
    {
        tracker = collectionTracker;
        race = collectionRace;
    }

    public void ApplySprite(Sprite sprite)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && sprite != null)
            spriteRenderer.sprite = sprite;
    }
}
