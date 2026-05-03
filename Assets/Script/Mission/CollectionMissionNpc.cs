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

    public string ID => $"CollectionMission_{race}";

    public CollectionMissionTracker Tracker => tracker;

    public CollectionMissionRace Race => race;

    private void Awake()
    {
        if (tracker == null)
            tracker = FindObjectOfType<CollectionMissionTracker>();
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
}
