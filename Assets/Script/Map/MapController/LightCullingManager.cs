using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightCullingManager : MonoBehaviour
{
    public Transform playerTarget;
    public float cullDistance = 50f;

    // 儲存所有需要被管理的點光源
    private List<Light> managedLights = new List<Light>();

    // 預先計算好距離的平方，避免執行期重複運算
    private float sqrCullDistance;

    void Start()
    {
        sqrCullDistance = cullDistance * cullDistance;
        // 啟動協程，每 0.2 秒檢查一次即可 (Tick Rate = 5次/秒)
        StartCoroutine(CullLightsRoutine());
    }

    // 提供給外部 (例如你的 Json 讀檔腳本) 註冊新光源的方法
    public void RegisterLight(Light newLight)
    {
        managedLights.Add(newLight);
    }

    private IEnumerator CullLightsRoutine()
    {
        // 使用 WaitForSeconds 緩存，避免產生 GC
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            if (playerTarget == null) yield return wait;

            Vector3 playerPos = playerTarget.position;

            for (int i = 0; i < managedLights.Count; i++)
            {
                Light light = managedLights[i];
                if (light == null) continue;

                // 核心優化：使用 sqrMagnitude (純加法與乘法)，避開開根號運算
                float sqrDistance = (light.transform.position - playerPos).sqrMagnitude;
                bool shouldBeEnabled = sqrDistance <= sqrCullDistance;

                // 只在狀態需要改變時才賦值，避免觸發底層不必要的更新
                if (light.enabled != shouldBeEnabled)
                {
                    light.enabled = shouldBeEnabled;
                }
            }

            yield return wait;
        }
    }
}