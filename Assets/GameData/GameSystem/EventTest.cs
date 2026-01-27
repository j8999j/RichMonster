using UnityEngine;
using System.Linq;
public class EventTest : MonoBehaviour
{
    void Start()
    {
        // 確保 DataManager 已載入
        var manager = DataManager.Instance;

        var Traits = manager.GetTraits("Patience");
        Debug.Log($"耐心效果共有 {Traits.Count} 個");

        // 1. 找所有 "中期 (Mid)" 的事件
        var midEvents = manager.GetEventsByPeriod(EventTime.Mid);
        Debug.Log($"中期事件共有 {midEvents.Count} 個");
        foreach (var evt in midEvents) Debug.Log(" - " + evt.Name);
    }
}