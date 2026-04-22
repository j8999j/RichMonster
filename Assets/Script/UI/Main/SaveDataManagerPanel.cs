using System;
using GameSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 存檔管理面板：使用 SaveManager 提供的 API
/// (清空圖鑑 / 刪除指定 slot / 清空全部存檔 / 開啟存檔資料夾)
/// 所有破壞性操作會先彈出二次確認 UI。
/// </summary>
public class SaveDataManagerPanel : MonoBehaviour
{
    [Header("功能按鈕")]
    [SerializeField] private Button clearBookButton;        // 清空圖鑑存檔
    [SerializeField] private Button unlockAllBookButton;    // 解鎖全部圖鑑資訊
    [SerializeField] private Button unlockAllAchievementsButton; // 解鎖全部成就與特殊紀念品
    [SerializeField] private Button clearAllSavesButton;    // 清空所有存檔
    [SerializeField] private Button openSaveFolderButton;   // 開啟存檔位置 (無需確認)

    [Header("二次確認 UI")]
    [SerializeField] private GameObject confirmPanel;       // 確認面板根物件
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField] private Button confirmButton;          // 是 / 確認
    [SerializeField] private Button cancelButton;           // 否 / 取消

    // 當前等待執行的動作 (按下確認後執行)
    private Action _pendingAction;

    private void Awake()
    {
        if (clearBookButton != null)
            clearBookButton.onClick.AddListener(OnClickClearBook);

        if (unlockAllBookButton != null)
            unlockAllBookButton.onClick.AddListener(OnClickUnlockAllBook);

        if (unlockAllAchievementsButton != null)
            unlockAllAchievementsButton.onClick.AddListener(OnClickUnlockAllAchievements);

        if (clearAllSavesButton != null)
            clearAllSavesButton.onClick.AddListener(OnClickClearAllSaves);

        if (openSaveFolderButton != null)
            openSaveFolderButton.onClick.AddListener(OnClickOpenSaveFolder);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (clearBookButton != null) clearBookButton.onClick.RemoveListener(OnClickClearBook);
        if (unlockAllBookButton != null) unlockAllBookButton.onClick.RemoveListener(OnClickUnlockAllBook);
        if (unlockAllAchievementsButton != null) unlockAllAchievementsButton.onClick.RemoveListener(OnClickUnlockAllAchievements);
        if (clearAllSavesButton != null) clearAllSavesButton.onClick.RemoveListener(OnClickClearAllSaves);
        if (openSaveFolderButton != null) openSaveFolderButton.onClick.RemoveListener(OnClickOpenSaveFolder);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancel);
    }

    #region Button Click Handlers
    private void OnClickClearBook()
    {
        ShowConfirm(
            "確定要<color=red>清空圖鑑存檔</color>嗎？\n物品圖鑑、妖怪圖鑑、成就與紀念品進度將全部重置，且無法復原。",
            () => SaveManager.Instance.ClearBookData()
        );
    }

    private void OnClickUnlockAllBook()
    {
        ShowConfirm(
            "測試用功能:<color=red>解鎖全部圖鑑資訊</color>嗎？\n所有物品圖鑑與妖怪情報將立即解鎖。",
            () => SaveManager.Instance.UnlockAllBookData()
        );
    }
    private void OnClickUnlockAllAchievements()
    {
        ShowConfirm(
            "測試用功能:<color=red>解鎖全部成就與特殊紀念品</color>嗎？\n所有成就將立即完成、所有特殊紀念品將立即收集。",
            () => SaveManager.Instance.UnlockAllAchievementsAndSpecialSouvenirs()
        );
    }

    private void OnClickClearAllSaves()
    {
        ShowConfirm(
            "確定要<color=red>清空所有存檔</color>嗎？\n所有存檔位置與圖鑑進度將全部刪除，且無法復原。",
            () => SaveManager.Instance.ClearAllSaves()
        );
    }

    private void OnClickOpenSaveFolder()
    {
        // 開啟資料夾屬於非破壞性操作，不需要二次確認
        SaveManager.Instance.OpenSaveFolder();
    }
    #endregion

    #region Confirm Dialog
    /// <summary>
    /// 顯示二次確認面板，按下確認後執行 onConfirmed
    /// </summary>
    private void ShowConfirm(string message, Action onConfirmed)
    {
        _pendingAction = onConfirmed;

        if (confirmMessageText != null)
            confirmMessageText.text = message;

        if (confirmPanel != null)
            confirmPanel.SetActive(true);
    }

    private void OnConfirm()
    {
        var action = _pendingAction;
        _pendingAction = null;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        action?.Invoke();
    }

    private void OnCancel()
    {
        _pendingAction = null;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }
    #endregion
}
