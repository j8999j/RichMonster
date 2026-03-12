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
       UIEvents.OnAchievementUnlocked += Setup;
    }
    public void OnDisable()
    {
        UIEvents.OnAchievementUnlocked -= Setup;
    }
    public void Setup(AchievementBase achievement)
    {
        
        if (achievement == null) return;
        // 顯示成就名稱
        if (NameText != null) NameText.text = achievement.AchievementName;

        // 顯示成就描述
        if (DescriptionText != null) DescriptionText.text = achievement.Description;
        Panel.SetActive(true);
    }
}