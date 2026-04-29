using UnityEngine;
using Player;

/// <summary>
/// 地圖上的單一遺落妖怪包裹。被 YokaiPackageSpawner 生成，玩家互動後
/// 通知 Spawner 給獎並隱藏。
/// </summary>
public class YokaiPackage : MonoBehaviour, IInteractable
{
    [Tooltip("玩家靠近時顯示的按鍵提示（如 E 鍵 icon）")]
    public GameObject Prompt_E;

    private YokaiPackageSpawner _spawner;
    private int _index;

    public void Initialize(YokaiPackageSpawner spawner, int index)
    {
        _spawner = spawner;
        _index = index;
    }

    public void Interact()
    {
        if (_spawner == null) return;
        HidePrompt();
        _spawner.OnPackagePicked(_index);
    }

    public void ShowPrompt()
    {
        if (Prompt_E != null) Prompt_E.SetActive(true);
    }

    public void HidePrompt()
    {
        if (Prompt_E != null) Prompt_E.SetActive(false);
    }
}
