using System;
using System.Collections.Generic;

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

    public static void InvokeShowNotice(string source, List<NoticeItemEntry> items)
    {
        OnShowNotice?.Invoke(source, items);
    }

    public static void InvokeClearNotice()
    {
        OnClearNotice?.Invoke();
    }
}
