using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class AchievementComplete : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject Panel;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI DescriptionText;
    public void OnEnable()
    {
       GameEventCenter.Subscribe<AchievementUnlockedEvent>(Setup);
    }
    public void OnDisable()
    {
        GameEventCenter.Unsubscribe<AchievementUnlockedEvent>(Setup);
    }
    public void Setup(AchievementUnlockedEvent eventData)
    {
        if (eventData == null) return;
        // 顯示成就名稱
        if (NameText != null) NameText.text = eventData.AchievementName;

        // 顯示成就描述
        if (DescriptionText != null) DescriptionText.text = eventData.Description;
        Panel.SetActive(true);
    }
}
