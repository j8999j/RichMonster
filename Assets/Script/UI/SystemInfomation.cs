using System.Collections.Generic;
using UnityEngine;

public class SystemInfomation : MonoBehaviour
{
    public GameObject TextPrefab;
    public Transform spawnRoot;
    private Queue<SystemInfoItem> pool = new Queue<SystemInfoItem>();
    void OnEnable()
    {
        SystemInfoEvent.OnShow += ShowNewText;
    }

    void OnDisable()
    {
        SystemInfoEvent.OnShow -= ShowNewText;
    }

    public void ShowNewText(string text)
    {
        SystemInfoItem item;

        // 從池取
        if (pool.Count > 0)
        {
            item = pool.Dequeue();
            item.gameObject.SetActive(true);
        }
        else
        {
            GameObject obj = Instantiate(TextPrefab, spawnRoot);
            item = obj.GetComponent<SystemInfoItem>();
        }

        item.transform.SetAsLastSibling(); // UI層級
        item.transform.localPosition = Vector3.zero; // 重置局部位置以確保固定生成位置

        item.Init(text, Recycle);
    }

    private void Recycle(SystemInfoItem item)
    {
        item.gameObject.SetActive(false);
        pool.Enqueue(item);
    }
}
public static class SystemInfoEvent
{
    public static System.Action<string> OnShow;

    public static void Show(string msg)
    {
        OnShow?.Invoke(msg);
    }
}