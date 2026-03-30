// ============================================================
// GuideLookupRegistry.cs - 統一登記中心
// ============================================================
using System.Collections.Generic;
using UnityEngine;
public class GuideLookupRegistry : MonoBehaviour
{
    public static GuideLookupRegistry Instance { get; private set; }

    private readonly Dictionary<string, IGuideInteractable> interactables
        = new Dictionary<string, IGuideInteractable>();
    private readonly Dictionary<string, IGuideButton> buttons
        = new Dictionary<string, IGuideButton>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public void RegisterInteractable(IGuideInteractable target)
        => interactables[target.GuideInteractableId] = target;

    public void UnregisterInteractable(IGuideInteractable target)
        => interactables.Remove(target.GuideInteractableId);

    public void RegisterButton(IGuideButton button)
        => buttons[button.ButtonId] = button;

    public void UnregisterButton(IGuideButton button)
        => buttons.Remove(button.ButtonId);

    public bool TryGetInteractable(string id, out IGuideInteractable result)
        => interactables.TryGetValue(id, out result);

    public bool TryGetButton(string id, out IGuideButton result)
        => buttons.TryGetValue(id, out result);
}