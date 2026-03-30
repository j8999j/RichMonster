// ============================================================
// Interfaces.cs - 物件自我宣告能力
// ============================================================
using Player;

public interface IGuideInteractable : IInteractable, IMapGuideTarget
{
    string GuideInteractableId => ID;
    event System.Action<string> OnInteracted;
    void SetGuideID();
}
public interface IGuideButton
{
    string ButtonId { get; }
    event System.Action<string> OnClicked;
}
