using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class WaypointIndicator : MonoBehaviour
{
    [Header("目標設定")]
    public Transform target;          // 要指向的目標
    public RectTransform iconRect;    // UI 圖標的 RectTransform
    public Image iconImage;           // 圖標的 Image 元件

    [Header("設定")]
    public float edgePadding = 50f;   // 圖標距離螢幕邊緣的距離
    public Sprite onScreenSprite;     // 目標在畫面內時的圖標
    public Sprite offScreenSprite;    // 目標在畫面外時的圖標

    private Camera mainCam;
    private Canvas canvas;
    Dictionary<string, Transform> mapGuide = new Dictionary<string, Transform>();

    private void OnEnable()
    {
        NoticeGetItemEvents.OnSetMapGuide += SetMapGuide;
        NoticeGetItemEvents.OnStartMapGuide += StartMapGuide;
        NoticeGetItemEvents.OnClearMapGuide += ClearMapGuide;
    }
    private void OnDisable()
    {
        NoticeGetItemEvents.OnSetMapGuide -= SetMapGuide;
        NoticeGetItemEvents.OnStartMapGuide -= StartMapGuide;
        NoticeGetItemEvents.OnClearMapGuide -= ClearMapGuide;
    }
    void SetMapGuide(string id,Transform pos)
    {
        mapGuide[id] = pos;
    }
    void StartMapGuide(string id)
    {
        iconImage.gameObject.SetActive(true);
        target = mapGuide[id];
    }
    void ClearMapGuide()
    {
        mapGuide.Clear();
        target = null;
        iconImage.gameObject.SetActive(false);
    }
    void Start()
    {
        mainCam = Camera.main;
        canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (target == null) return;

        Vector3 screenPos = mainCam.WorldToScreenPoint(target.position);
        bool isOnScreen = IsOnScreen(screenPos);

        if (isOnScreen)
        {
            // 目標在畫面內：直接顯示在目標位置
            iconImage.sprite = onScreenSprite;
            iconRect.position = screenPos;
            iconRect.rotation = Quaternion.identity;
        }
        else
        {
            // 目標在畫面外：將圖標限制在螢幕邊緣
            iconImage.sprite = offScreenSprite;
            iconRect.position = ClampToScreenEdge(screenPos);
            
            // 確保圖標不旋轉
            iconRect.rotation = Quaternion.identity;
        }
    }

    bool IsOnScreen(Vector3 screenPos)
    {
        return screenPos.z > 0 &&
               screenPos.x > 0 && screenPos.x < Screen.width &&
               screenPos.y > 0 && screenPos.y < Screen.height;
    }

    Vector3 ClampToScreenEdge(Vector3 screenPos)
    {
        // 若目標在攝影機後方，翻轉座標
        if (screenPos.z < 0)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Vector3 dir = (screenPos - screenCenter).normalized;

        float slope = dir.y / dir.x;
        float halfW = Screen.width / 2f - edgePadding;
        float halfH = Screen.height / 2f - edgePadding;

        // 計算與螢幕邊緣的交點
        if (Mathf.Abs(slope) * halfW < halfH)
        {
            // 左右邊
            float sign = Mathf.Sign(dir.x);
            screenPos = new Vector3(screenCenter.x + halfW * sign,
                                    screenCenter.y + slope * halfW * sign, 0);
        }
        else
        {
            // 上下邊
            float sign = Mathf.Sign(dir.y);
            screenPos = new Vector3(screenCenter.x + halfH / slope * sign,
                                    screenCenter.y + halfH * sign, 0);
        }

        return screenPos;
    }

}