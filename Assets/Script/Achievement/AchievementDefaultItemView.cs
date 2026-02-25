// ============================================================
// AchievementViews.cs
// View 層：不同類型對應不同 Prefab，刷新由外部 Manager 統一驅動
// ============================================================

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#region View 實作

/// <summary>
/// 預設成就 View
/// Prefab：AchievementItem_Default
///
/// AchievementItem_Default
///  ├── NameText        (TMP_Text)
///  ├── ConditionText   (TMP_Text)
///  ├── DescriptionText (TMP_Text)
///  └── CompletedMark   (GameObject)
/// </summary>
public class AchievementDefaultItemView : MonoBehaviour, IAchievementDisplayView
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject completedMark;
    [SerializeField] private Image LevelImage;
    [SerializeField] private List<Sprite> LevelImageList;

    private AchievementBase _achievement;

    public void Bind(AchievementBase achievement)
    {
        _achievement = achievement;
    }

    // Manager 呼叫此方法刷新 CompletedMark 等非文字狀態
    public void Refresh()
    {
        if (_achievement == null) return;
        completedMark.SetActive(_achievement.IsCompleted);
        LevelImage.sprite = LevelImageList[(int)_achievement.Level];
    }

    public void Unbind() => _achievement = null;
    private void OnDestroy() => Unbind();

    // --- IAchievementDisplayView ---
    public void SetNameText(string text)        => nameText.text = text;
    public void SetConditionText(string text)   => conditionText.text = text;
    public void SetDescriptionText(string text) => descriptionText.text = text;
}
#endregion