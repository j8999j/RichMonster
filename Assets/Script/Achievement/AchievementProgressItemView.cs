using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
/// <summary>
/// 隱藏條件成就 View
/// Prefab：AchievementItem_Hidden
///
/// AchievementItem_Hidden
///  ├── NameText        (TMP_Text)
///  ├── ConditionText   (TMP_Text)   ← 未解鎖時顯示 ???，解鎖後顯示真實條件
///  ├── DescriptionText (TMP_Text)
///  └── CompletedMark   (GameObject)
/// </summary>
public class AchievementProgressItemView : MonoBehaviour, IAchievementProgressView
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject completedMark;
    [SerializeField] private Image LevelImage;
    [SerializeField] private List<Sprite> LevelImageList;
    [SerializeField] private Image PrgrassBar;
    [SerializeField] private TMP_Text Percentage;
    [SerializeField] private TMP_Text ProgressText;

    

    private AchievementBase _achievement;

    public void Bind(AchievementBase achievement)
    {
        _achievement = achievement;
    }

    public void Refresh()
    {
        if (_achievement == null) return;
        completedMark.SetActive(_achievement.IsCompleted);
        LevelImage.sprite = LevelImageList[(int)_achievement.Level];

        // 自動更新進度條
        if (_achievement is IAchievementWithProgress progress)
        {
            SetProgressText(progress.ProgressText);
            SetProgressFloat(progress.ProgressRatio);
        }
    }

    public void Unbind() => _achievement = null;
    private void OnDestroy() => Unbind();

    // --- IAchievementDisplayView ---
    public void SetNameText(string text)        => nameText.text = text;
    public void SetConditionText(string text)   => conditionText.text = text;
    public void SetDescriptionText(string text) => descriptionText.text = text;

    // --- IAchievementProgressView ---
    public void SetProgressText(string text) => ProgressText.text = text;

    /// <summary>
    /// 設定進度條填充量與百分比文字 (百分比為整數，最低為 1，除非進度為 0)
    /// </summary>
    public void SetProgressFloat(float progress)
    {
        progress = Mathf.Clamp01(progress);
        // 每 2% 為一階段 (共 50 階段)，無條件捨去
        int step = Mathf.FloorToInt(progress * 50f);
        // 除非為 0，否則最低顯示一階段
        if (progress > 0f && step < 1) step = 1;
        int percent = step * 2;
        PrgrassBar.fillAmount = step / 50f;
        Percentage.text = $"{Mathf.FloorToInt(progress * 100f)}%";
    }
}