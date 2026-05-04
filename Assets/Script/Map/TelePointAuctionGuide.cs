using Player;

public class TelePointAuctionGuide : TelePoint, IMapGuideTarget
{
    public string ID => GuideIDs.Interactable.TelePointAuctionGuide;

    private void OnEnable()
    {
        SetMapGuide();
        AuctionDayGuide.Refresh();
    }

    private void Start()
    {
        SetMapGuide();
        AuctionDayGuide.Refresh();
    }

    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID, transform);
    }

    public override void Interact()
    {
        AuctionDayGuide.CompleteAuctionStartGuide();
        base.Interact();
    }
}
