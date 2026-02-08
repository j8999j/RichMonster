using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class TradeSlider : MonoBehaviour, IDragHandler
{
    [Header("Slider 元件")]
    [SerializeField] private RectTransform bar;           // Bar 背景
    [SerializeField] private RectTransform handle;        // 可拖拉的 Handle

    [Header("階段設定")]
    [SerializeField] private int stageCount = 5;          // 階段數量（預設5個階段）
    [SerializeField] private int startStage = 0;          // 起始階段（預設從0開始）

    [Header("邊界設定")]
    [SerializeField] private float leftPadding = 0f;      // 左邊界縮減距離
    [SerializeField] private float rightPadding = 0f;     // 右邊界縮減距離

    [Header("視覺回饋（選用）")]
    [SerializeField] private bool showStageMarkers = true; // 是否顯示階段標記
    [SerializeField] private GameObject markerPrefab;      // 階段標記預製物

    // 事件：當階段改變時觸發
    public event Action<int> OnStageChanged;

    private int currentStage = 0;                         // 目前階段（0 到 stageCount-1）
    private Canvas canvas;
    private RectTransform handleRect;
    private float barWidth;
    private float effectiveBarWidth;                      // 扣除邊界後的有效寬度
    private float minX, maxX;                             // 實際移動範圍

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        handleRect = handle.GetComponent<RectTransform>();

        // 計算 Bar 寬度和有效範圍
        barWidth = bar.rect.width;
        CalculateEffectiveRange();

        // 設定起始階段
        startStage = Mathf.Clamp(startStage, 0, stageCount - 1);
        currentStage = startStage;

        // 初始化位置
        UpdateHandlePosition();

        // 生成階段標記
        if (showStageMarkers && markerPrefab != null)
        {
            GenerateStageMarkers();
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        // 轉換滑鼠位置到 Bar 的本地座標
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bar,
            eventData.position,
            eventData.pressEventCamera ?? canvas.worldCamera,
            out localPoint
        );

        // 限制在有效範圍內（考慮邊界縮減）
        float clampedX = Mathf.Clamp(localPoint.x, minX, maxX);

        // 計算最接近的階段（拖動時即時吸附）
        int nearestStage = GetNearestStage(clampedX);

        // 如果階段改變，立即定位並觸發事件
        if (nearestStage != currentStage)
        {
            currentStage = nearestStage;
            UpdateHandlePosition();
            OnStageChanged?.Invoke(currentStage);
        }
    }
    /// <summary>
    /// 計算扣除邊界後的有效範圍
    /// </summary>
    private void CalculateEffectiveRange()
    {
        minX = -barWidth / 2 + leftPadding;
        maxX = barWidth / 2 - rightPadding;
        effectiveBarWidth = maxX - minX;
    }

    /// <summary>
    /// 取得指定階段的 X 座標位置
    /// </summary>
    private float GetStagePosition(int stage)
    {
        if (stageCount <= 1) return (minX + maxX) / 2;

        float normalizedPosition = (float)stage / (stageCount - 1);
        return Mathf.Lerp(minX, maxX, normalizedPosition);
    }

    /// <summary>
    /// 根據 X 座標取得最接近的階段
    /// </summary>
    private int GetNearestStage(float xPosition)
    {
        if (stageCount <= 1) return 0;

        // 正規化位置 (0 到 1)，使用有效範圍
        float normalizedPos = (xPosition - minX) / effectiveBarWidth;
        normalizedPos = Mathf.Clamp01(normalizedPos);

        // 計算最近的階段
        int nearestStage = Mathf.RoundToInt(normalizedPos * (stageCount - 1));
        return Mathf.Clamp(nearestStage, 0, stageCount - 1);
    }

    /// <summary>
    /// 立即更新 Handle 位置到目前階段
    /// </summary>
    private void UpdateHandlePosition()
    {
        float targetX = GetStagePosition(currentStage);
        handleRect.anchoredPosition = new Vector2(targetX, handleRect.anchoredPosition.y);
    }

    /// <summary>
    /// 生成階段標記（視覺化用）
    /// </summary>
    private void GenerateStageMarkers()
    {
        for (int i = 0; i < stageCount; i++)
        {
            GameObject marker = Instantiate(markerPrefab, bar);
            RectTransform markerRect = marker.GetComponent<RectTransform>();

            float xPos = GetStagePosition(i);
            markerRect.anchoredPosition = new Vector2(xPos, 0);
        }
    }

    // ===== 公開方法 =====

    /// <summary>
    /// 設定到指定階段
    /// </summary>
    public void SetStage(int stage)
    {
        stage = Mathf.Clamp(stage, 0, stageCount - 1);

        if (currentStage != stage)
        {
            currentStage = stage;
            UpdateHandlePosition();
            OnStageChanged?.Invoke(currentStage);
        }
    }

    /// <summary>
    /// 取得目前階段
    /// </summary>
    public int GetCurrentStage()
    {
        return currentStage;
    }

    /// <summary>
    /// 設定階段數量（執行時期也可修改）
    /// </summary>
    public void SetStageCount(int count)
    {
        stageCount = Mathf.Max(2, count);
        startStage = Mathf.Clamp(startStage, 0, stageCount - 1);
        currentStage = Mathf.Clamp(currentStage, 0, stageCount - 1);
        UpdateHandlePosition();
    }

    /// <summary>
    /// 設定起始階段
    /// </summary>
    public void SetStartStage(int stage)
    {
        startStage = Mathf.Clamp(stage, 0, stageCount - 1);
    }

    /// <summary>
    /// 重置到起始階段
    /// </summary>
    public void ResetToStart()
    {
        currentStage = startStage;
        UpdateHandlePosition();
        OnStageChanged?.Invoke(currentStage);
    }

    /// <summary>
    /// 取得起始階段
    /// </summary>
    public int GetStartStage()
    {
        return startStage;
    }

    /// <summary>
    /// 設定左邊界縮減距離
    /// </summary>
    public void SetLeftPadding(float padding)
    {
        leftPadding = Mathf.Max(0, padding);
        CalculateEffectiveRange();
        UpdateHandlePosition();
    }

    /// <summary>
    /// 設定右邊界縮減距離
    /// </summary>
    public void SetRightPadding(float padding)
    {
        rightPadding = Mathf.Max(0, padding);
        CalculateEffectiveRange();
        UpdateHandlePosition();
    }

    /// <summary>
    /// 同時設定左右邊界縮減距離
    /// </summary>
    public void SetPadding(float left, float right)
    {
        leftPadding = Mathf.Max(0, left);
        rightPadding = Mathf.Max(0, right);
        CalculateEffectiveRange();
        UpdateHandlePosition();
    }
}