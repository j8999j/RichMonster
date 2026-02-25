// ScratchCard.cs（修正版）
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class ScratchCard : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [Header("貼圖設定")]
    public Texture2D coverTexture;
    [Header("筆刷設定")]
    [Range(10, 200)]
    public int brushSize = 60;

    [Header("結算設定")]
    [Range(0f, 1f)]
    public float revealThreshold = 0.80f;
    public float checkInterval   = 0.3f;

    public UnityEngine.Events.UnityEvent onScratchComplete;

    // --- 私有 ---
    private Texture2D     _maskTex;       // CPU 可寫的遮罩（黑=刮掉）
    private Material      _mat;
    private RawImage      _rawImage;
    private RectTransform _rectTransform;
    private float         _nextCheckTime;
    private bool          _completed;
    private Vector2       _lastUV = -Vector2.one;

    // 預先產生的圓形筆刷 alpha 值
    private float[] _brushAlpha;
    private int     _brushDiameter;

    void Start()
    {
        _rawImage      = GetComponent<RawImage>();
        _rectTransform = GetComponent<RectTransform>();

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
        _rawImage.texture  = coverTexture;

        // 預建筆刷
        BuildBrush(brushSize);
    }

    // ── 建立圓形筆刷的 Alpha 遮罩 ─────────────────────
    void BuildBrush(int size)
    {
        _brushDiameter = size;
        _brushAlpha    = new float[size * size];
        float r   = size * 0.5f;
        float cx  = r - 0.5f;
        float cy  = r - 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = x - cx;
            float dy   = y - cy;
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
            float dist  = Vector2.Distance(_lastUV, uv) * _maskTex.width;
            int   steps = Mathf.Max(1, (int)(dist / (_brushDiameter * 0.3f)));
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
        int w  = _maskTex.width;
        int h  = _maskTex.height;
        int cx = Mathf.RoundToInt(uv.x * w);
        int cy = Mathf.RoundToInt(uv.y * h);
        int r  = _brushDiameter / 2;

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

            float alpha    = _brushAlpha[by * _brushDiameter + bx];
            int   idx      = y * w + x;
            byte  current  = region[idx].r;
            // 將遮罩值往 0（黑）靠近
            byte  newVal   = (byte)Mathf.Max(0, current - (int)(alpha * 255));
            region[idx]    = new Color32(newVal, newVal, newVal, 255);
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
        Debug.Log($"刮除比例: {ratio:P1}");

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

        Debug.Log("🎉 刮除完成！");
        onScratchComplete?.Invoke();
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

    void OnDestroy()
    {
        if (_maskTex  != null) Destroy(_maskTex);
        if (_mat      != null) Destroy(_mat);
    }
}