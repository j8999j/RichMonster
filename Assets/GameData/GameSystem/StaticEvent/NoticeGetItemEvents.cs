using System;
using System.Collections.Generic;
using UnityEngine;
public static class NoticeGetItemEvents
{
    /// <summary>
    /// 觸發顯示取得物品通知（source: 獎勵來源說明）
    /// </summary>
    public static event Action<string, List<NoticeItemEntry>> OnShowNotice;
    /// <summary>
    /// 觸發清除通知
    /// </summary>
    public static event Action OnClearNotice;
    /// <summary>
    /// 地圖指引設定，傳入目標ID
    /// </summary>
    public static event Action<string, Transform> OnSetMapGuide;
    /// <summary>
    /// 開始地圖指引
    /// </summary>
    public static event Action<string> OnStartMapGuide;
    /// <summary>
    /// 清空所有地圖指引
    /// </summary>
    public static event Action OnClearMapGuide;
    public static void InvokeShowNotice(string source, List<NoticeItemEntry> items)
    {
        OnShowNotice?.Invoke(source, items);
    }

    public static void InvokeClearNotice()
    {
        OnClearNotice?.Invoke();
    }
    public static void InvokeSetMapGuide(string targetId, Transform targetPos)
    {
        OnSetMapGuide?.Invoke(targetId, targetPos);
    }
    public static void InvokeStartMapGuide(string targetId)
    {
        OnStartMapGuide?.Invoke(targetId);
    }
    public static void InvokeClearMapGuide()
    {
        OnClearMapGuide?.Invoke();
    }
}
