// ScratchCard.cs（修正版）
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ScratchCard : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [Header("刮除目標設定")]
    [Tooltip("指定要刮除的 RawImage 物件，若未指定則使用自身的 RawImage")]
    [SerializeField] private RawImage targetRawImage;

    [Header("貼圖設定")]
    public Texture2D coverTexture;
    public GameObject LeaveButton;
    public GameObject CardPanel;
    public GameObject BuyPanel;
    public GameObject ScratchPanel;
    public Button BuyScratchButton;
    public GameObject CompletePrizePanel;
    public GameObject BuyPanelRaycastImage;
    private bool PanelIsVisible;
    public bool CanClosePanel{private set; get;} = true;
    [SerializeField] private Button[] ScratchCardSelect;
    [Header("得獎設定")]
    public GameObject PrizePanel;
    public TextMeshProUGUI PrizeText;
    [Header("按鈕懸停效果設定")]
    [Tooltip("滑鼠懸停時按鈕朝自身方向移動的距離")]
    [SerializeField] private float hoverMoveDistance = 30f;
    [Tooltip("移動動畫持續時間（秒）")]
    [SerializeField] private float hoverMoveDuration = 0.15f;

    [Header("獎品圖片設定 (依照 ScratchCardPrizeType 順序特獎到銘謝)")]
    [SerializeField] private Sprite[] prizeSprites;
    [SerializeField] private Image[] prizeImages;

    /// <summary>
    /// 依照獎品索引設定獎品圖片
    /// </summary>
    public void SetPrize(ScratchCardPrizeType prizeIndex)
    {
        // 將獎項轉換為等級：GrandPrize(0) → 等級6(最高)，NoWin(6) → 等級0(最低)
        int prizeLevel = 6 - (int)prizeIndex;

        // 隨機選一個位置，保證至少有一張圖片為當前最高數字
        int guaranteedSlot = UnityEngine.Random.Range(0, prizeImages.Length);

        for (int i = 0; i < prizeImages.Length; i++)
        {
            int spriteIndex;

            if (i == guaranteedSlot || prizeLevel <= 0 || prizeLevel >= 6)
            {
                // 保證位置 或 最低/最高等級：直接設為 prizeLevel
                spriteIndex = Mathf.Clamp(prizeLevel, 0, 6);
            }
            else
            {
                // 其餘位置隨機抽選 0 ~ prizeLevel
                spriteIndex = UnityEngine.Random.Range(0, prizeLevel + 1);
            }

            prizeImages[i].sprite = prizeSprites[spriteIndex];
        }
    }
    public void ShowCompletePrize(ScratchCardPrizeType prize)
    {
        CompletePrizePanel.SetActive(true);
        PrizePanel.SetActive(true);
        switch (prize)
        {
            case ScratchCardPrizeType.GrandPrize:
                PrizeText.text = "恭喜獲得:10000";
                break;
            case ScratchCardPrizeType.FirstPrize:
                PrizeText.text = "恭喜獲得:5000";
                break;
            case ScratchCardPrizeType.SecondPrize:
                PrizeText.text = "恭喜獲得:2000";
                break;
            case ScratchCardPrizeType.ThirdPrize:
                PrizeText.text = "恭喜獲得:500";
                break;
            case ScratchCardPrizeType.FourthPrize:
                PrizeText.text = "恭喜獲得:300";
                break;
            case ScratchCardPrizeType.FifthPrize:
                PrizeText.text = "恭喜獲得:100";
                break;
            case ScratchCardPrizeType.NoWin:
                PrizeText.text = "恭喜獲得:0";
                break;
        }
    }
    public void ShowCardPanel(bool isScratched)
    {
        if (CanClosePanel)
        {
            PanelIsVisible = !PanelIsVisible;
            CardPanel.SetActive(PanelIsVisible);
            if (isScratched)
            {
                ShowScratchCard(true);
                ScratchPanel.SetActive(true);
                BuyPanel.SetActive(false);
                CompletePrizePanel.SetActive(true);
            }
            else
            {
                ShowBuyPanel();
            }
        }
    }
    public void ShowScratchPanel()
    {
        ScratchPanel.SetActive(true);
        BuyPanel.SetActive(false);
    }
    public void ShowBuyPanel()
    {
        ScratchPanel.SetActive(false);
        BuyPanel.SetActive(true);
    }
    public void HideBuyPanel()
    {
        ScratchPanel.SetActive(true);
        BuyPanel.SetActive(false);
    }
    /// <summary>
    /// 顯示刮刮卡漆面
    /// </summary>
    public void ShowScratchCard(bool isScratched)
    {
        if (isScratched)
        {
            // 已刮過：遮罩全黑，完整顯示獎品
            if (_maskTex != null)
            {
                Color32[] pixels = new Color32[_maskTex.width * _maskTex.height];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(0, 0, 0, 255);
                _maskTex.SetPixels32(pixels);
                _maskTex.Apply();
            }
            _completed = true;
        }
        else
        {
            // 未刮過：遮罩全白，顯示完整封面
            _completed = false;
            _lastUV = -Vector2.one;
            if (_maskTex != null)
            {
                ClearMask();
            }
        }
    }
    /// <summary>
    /// 購買刮刮卡
    /// </summary>
    public void BuyScratchCard()
    {
        if (DataManager.Instance.TrySpendGold(300))
        {
            LeaveButton.SetActive(false);
            BuyScratchButton.gameObject.SetActive(false);
            BuyPanelRaycastImage.SetActive(false);
            CanClosePanel = false;
        }
    }
    public void ConfirmRewardButton()
    {
        CanClosePanel = true;
    }
    [Header("筆刷設定")]
    [Range(10, 200)]
    public int brushSize = 60;

    [Header("結算設定")]
    [Range(0f, 1f)]
    public float revealThreshold = 0.80f;
    public float checkInterval = 0.3f;

    public event Action OnScratchComplete;

    // --- 私有 ---
    private Texture2D _maskTex;       // CPU 可寫的遮罩（黑=刮掉）
    private Material _mat;
    private RawImage _rawImage;
    private RectTransform _rectTransform;
    private float _nextCheckTime;
    private bool _completed;
    private Vector2 _lastUV = -Vector2.one;

    // 預先產生的圓形筆刷 alpha 值
    private float[] _brushAlpha;
    private int _brushDiameter;

    // 懸停效果：紀錄每個按鈕的原始位置與正在執行的 Coroutine
    private Dictionary<Button, Vector3> _buttonOriginalPos = new Dictionary<Button, Vector3>();
    private Dictionary<Button, Coroutine> _buttonCoroutines = new Dictionary<Button, Coroutine>();

    void Start()
    {
        BuyScratchButton.onClick.AddListener(BuyScratchCard);
        SetupHoverEffects();
        // 若未指定目標，則使用自身的 RawImage
        if (targetRawImage == null)
            targetRawImage = GetComponent<RawImage>();
        _rawImage = targetRawImage;
        _rectTransform = _rawImage.GetComponent<RectTransform>();

        int w = coverTexture.width;
        int h = coverTexture.height;

        // 建立全白遮罩（白=不透明封面，黑=已刮除）
        _maskTex = new Texture2D(w, h, TextureFormat.R8, false);
        ClearMask();

        // 材質套用 Shader
        _mat = new Material(Shader.Find("Custom/ScratchCard"));
        _mat.SetTexture("_MainTex", coverTexture);
        _mat.SetTexture("_MaskTex", _maskTex);
        _rawImage.material = _mat;
        _rawImage.texture = coverTexture;

        // 預建筆刷
        BuildBrush(brushSize);
    }

    // ── 建立圓形筆刷的 Alpha 遮罩 ─────────────────────
    void BuildBrush(int size)
    {
        _brushDiameter = size;
        _brushAlpha = new float[size * size];
        float r = size * 0.5f;
        float cx = r - 0.5f;
        float cy = r - 0.5f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // 平滑邊緣
                float alpha = 1f - Mathf.Clamp01((dist - r * 0.6f) / (r * 0.4f));
                _brushAlpha[y * size + x] = alpha;
            }
    }

    // ── 清除遮罩為全白 ────────────────────────────────
    void ClearMask()
    {
        Color32[] pixels = new Color32[_maskTex.width * _maskTex.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        _maskTex.SetPixels32(pixels);
        _maskTex.Apply();
    }

    // ── 輸入 ──────────────────────────────────────────
    public void OnPointerDown(PointerEventData e)
    {
        _lastUV = -Vector2.one; // 重置，避免跨越跳線
        Scratch(e);
    }

    public void OnDrag(PointerEventData e) => Scratch(e);

    void Scratch(PointerEventData e)
    {
        if (_completed) return;
        if (!GetUV(e, out Vector2 uv)) return;

        // 插值補點（滑動流暢）
        if (_lastUV.x >= 0)
        {
            float dist = Vector2.Distance(_lastUV, uv) * _maskTex.width;
            int steps = Mathf.Max(1, (int)(dist / (_brushDiameter * 0.3f)));
            for (int i = 1; i <= steps; i++)
                PaintMask(Vector2.Lerp(_lastUV, uv, (float)i / steps));
        }
        else
        {
            PaintMask(uv);
        }

        _lastUV = uv;
        _maskTex.Apply(); // 一次 Apply，不在迴圈裡

        if (Time.time >= _nextCheckTime)
        {
            _nextCheckTime = Time.time + checkInterval;
            CheckProgress();
        }
    }

    // ── 在遮罩 Texture 上刷黑色 ───────────────────────
    void PaintMask(Vector2 uv)
    {
        int w = _maskTex.width;
        int h = _maskTex.height;
        int cx = Mathf.RoundToInt(uv.x * w);
        int cy = Mathf.RoundToInt(uv.y * h);
        int r = _brushDiameter / 2;

        int x0 = Mathf.Clamp(cx - r, 0, w - 1);
        int x1 = Mathf.Clamp(cx + r, 0, w - 1);
        int y0 = Mathf.Clamp(cy - r, 0, h - 1);
        int y1 = Mathf.Clamp(cy + r, 0, h - 1);

        // 讀取目前像素
        Color32[] region = _maskTex.GetPixels32();

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int bx = x - (cx - r);
                int by = y - (cy - r);

                // 邊界保護
                if (bx < 0 || bx >= _brushDiameter || by < 0 || by >= _brushDiameter)
                    continue;

                float alpha = _brushAlpha[by * _brushDiameter + bx];
                int idx = y * w + x;
                byte current = region[idx].r;
                // 將遮罩值往 0（黑）靠近
                byte newVal = (byte)Mathf.Max(0, current - (int)(alpha * 255));
                region[idx] = new Color32(newVal, newVal, newVal, 255);
            }

        _maskTex.SetPixels32(region);
    }

    // ── 計算刮除比例 ──────────────────────────────────
    void CheckProgress()
    {
        Color32[] pixels = _maskTex.GetPixels32();
        int dark = 0;
        foreach (var p in pixels)
            if (p.r < 26) dark++; // < 10% 亮度視為已刮除

        float ratio = (float)dark / pixels.Length;

        if (ratio >= revealThreshold)
            RevealAll();
    }

    // ── 超過 80% → 全部顯示 ───────────────────────────
    void RevealAll()
    {
        if (_completed) return;
        _completed = true;

        // 遮罩全部清黑 → 封面完全消失
        Color32[] pixels = new Color32[_maskTex.width * _maskTex.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 255);
        _maskTex.SetPixels32(pixels);
        _maskTex.Apply();

        Debug.Log("刮除完成！");
        OnScratchComplete?.Invoke();
    }

    // ── 筆刷大小 Slider 回調 ──────────────────────────
    public void SetBrushSize(float size)
    {
        brushSize = Mathf.RoundToInt(size);
        BuildBrush(brushSize); // 重建筆刷
    }

    // ── UV 計算 ───────────────────────────────────────
    bool GetUV(PointerEventData e, out Vector2 uv)
    {
        uv = Vector2.zero;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, e.position, e.pressEventCamera, out Vector2 local))
            return false;

        Rect rect = _rectTransform.rect;
        uv = new Vector2(
            (local.x - rect.x) / rect.width,
            (local.y - rect.y) / rect.height);

        // 過濾超出範圍
        return uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1;
    }

    // ── 懸停效果設定 ──────────────────────────────────
    void SetupHoverEffects()
    {
        if (ScratchCardSelect == null) return;

        foreach (var btn in ScratchCardSelect)
        {
            if (btn == null) continue;

            // 紀錄原始位置
            _buttonOriginalPos[btn] = btn.transform.localPosition;

            // 取得或新增 EventTrigger
            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btn.gameObject.AddComponent<EventTrigger>();

            // PointerEnter
            var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            var captured = btn; // 避免閉包問題
            entryEnter.callback.AddListener(_ => OnButtonHoverEnter(captured));
            trigger.triggers.Add(entryEnter);

            // PointerExit
            var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener(_ => OnButtonHoverExit(captured));
            trigger.triggers.Add(entryExit);

            // Click → 選定卡片
            btn.onClick.AddListener(() => SelectCard(captured));
        }
    }

    void OnButtonHoverEnter(Button btn)
    {
        if (!_buttonOriginalPos.ContainsKey(btn)) return;

        // 目標位置：原始位置 + 物件自身朝向（localRotation 的 up）× 移動距離
        Vector3 origin = _buttonOriginalPos[btn];
        Vector3 direction = btn.transform.up; // 物件目前朝向
        Vector3 target = origin + direction * hoverMoveDistance;

        StartHoverCoroutine(btn, target);
    }

    void OnButtonHoverExit(Button btn)
    {
        if (!_buttonOriginalPos.ContainsKey(btn)) return;

        // 回到原始位置
        Vector3 origin = _buttonOriginalPos[btn];
        StartHoverCoroutine(btn, origin);
    }

    void StartHoverCoroutine(Button btn, Vector3 targetPos)
    {
        // 停止該按鈕先前的動畫
        if (_buttonCoroutines.TryGetValue(btn, out Coroutine prev) && prev != null)
            StopCoroutine(prev);

        _buttonCoroutines[btn] = StartCoroutine(SmoothMoveCoroutine(btn.transform, targetPos));
    }

    IEnumerator SmoothMoveCoroutine(Transform target, Vector3 endPos)
    {
        Vector3 startPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < hoverMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / hoverMoveDuration);
            target.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        target.localPosition = endPos;
    }

    // ── 選定卡片：隱藏其他選項，平滑移動至中央並放大 ─────
    void SelectCard(Button selectedBtn)
    {
        // 隱藏其他選項
        foreach (var btn in ScratchCardSelect)
        {
            if (btn == null) continue;
            if (btn == selectedBtn) continue;
            btn.gameObject.SetActive(false);
        }

        // 停止該按鈕的懸停動畫
        if (_buttonCoroutines.TryGetValue(selectedBtn, out Coroutine prev) && prev != null)
            StopCoroutine(prev);

        // 移除懸停與點擊事件，避免選定後繼續觸發
        var trigger = selectedBtn.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
            Destroy(trigger);
        selectedBtn.onClick.RemoveAllListeners();
        selectedBtn.enabled = false; // 取消按鈕組件

        // 開始平滑移動 + 調整大小
        RectTransform rt = selectedBtn.GetComponent<RectTransform>();
        StartCoroutine(SmoothSelectCoroutine(rt, Vector2.zero, new Vector2(500f, 760f), 1.5f));
    }

    IEnumerator SmoothSelectCoroutine(RectTransform rt, Vector2 targetPos, Vector2 targetSize, float duration)
    {
        Vector2 startPos = rt.anchoredPosition;
        Vector2 startSize = rt.sizeDelta;
        Quaternion startRot = rt.localRotation;
        Quaternion targetRot = Quaternion.identity; // 角度歸零
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            rt.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
            rt.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        rt.anchoredPosition = targetPos;
        rt.sizeDelta = targetSize;
        rt.localRotation = targetRot;

        ShowScratchPanel();
    }

    void OnDestroy()
    {
        if (_maskTex != null) Destroy(_maskTex);
        if (_mat != null) Destroy(_mat);
    }
}