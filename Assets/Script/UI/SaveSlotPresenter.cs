using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystem;

/// <summary>
/// 存檔欄位 Presenter (MVP 模式)
/// 連接 SaveSlotUI (View) 與 SaveManager (Model)
/// </summary>
public class SaveSlotPresenter : MonoBehaviour
{
    [SerializeField] private SaveSlotUI saveSlotView;
    
    private void OnEnable()
    {
        // 訂閱 View 事件
        if (saveSlotView != null)
        {
            saveSlotView.OnSlotSelected += HandleSlotSelected;
            saveSlotView.OnRefreshRequested += HandleRefreshRequested;
        }
    }

    private void OnDisable()
    {
        // 取消訂閱 View 事件
        if (saveSlotView != null)
        {
            saveSlotView.OnSlotSelected -= HandleSlotSelected;
            saveSlotView.OnRefreshRequested -= HandleRefreshRequested;
        }
    }
    /// <summary>
    /// 載入並顯示所有存檔欄位 (只顯示非空存檔)
    /// </summary>
    public void LoadAndDisplaySaveSlots()
    {
        int maxSlots = saveSlotView.MaxSaveSlots;
        var slotDataList = new List<SaveSlotData>();

        for (int i = 0; i < maxSlots; i++)
        {
            var slotData = SaveManager.Instance.LoadSlotInfo(i);
            // 只加入非空存檔
            if (!slotData.IsEmpty)
            {
                slotDataList.Add(slotData);
            }
        }

        // 通知 View 更新顯示
        saveSlotView.DisplaySaveSlots(slotDataList);
    }

    /// <summary>
    /// 處理存檔欄位被選擇 (載入存檔)
    /// </summary>
    private void HandleSlotSelected(int slotIndex, bool isEmpty)
    {
        // 空存檔已不顯示，所以這裡只處理載入存檔
        Debug.Log($"[SaveSlotPresenter] 載入存檔 {slotIndex}");
        DataManager.Instance.LoadPlayerFromSave(slotIndex);
        GameManager.Instance.InitializeGame(slotIndex);
    }

    /// <summary>
    /// 處理刷新請求
    /// </summary>
    private void HandleRefreshRequested()
    {
        LoadAndDisplaySaveSlots();
    }
}
