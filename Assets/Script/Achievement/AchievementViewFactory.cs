// ============================================================
// AchievementViewFactory.cs
// 工廠：根據成就類型選擇對應 Prefab 生成 View
// 支援四個分類頁籤切換與上下捲動瀏覽
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Inspector 設定：
///  ├── defaultPrefab  → AchievementItem_Default（掛 AchievementDefaultItemView）
///  ├── hiddenPrefab   → AchievementItem_Hidden （掛 AchievementHiddenItemView）
///  ├── container      → ScrollView 的 Content Transform
///  ├── scrollRect     → ScrollRect 元件（用於垂直捲動）
///  └── categoryButtons → 四個分類按鈕 (依序: Item, Transaction, Record, Others)
/// </summary>
public class AchievementViewFactory : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private AchievementDefaultItemView defaultPrefab;
    [SerializeField] private AchievementProgressItemView progressPrefab;

    [Header("Scroll View")]
    [SerializeField] private Transform container;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Dependencies")]
    [SerializeField] private Souvenir.SouvenirManager _souvenirProvider;

    [Header("Category Buttons (依序: Item, Transaction, Record, Others, SpecialSouvenir)")]
    [SerializeField] private Button btnItem;
    [SerializeField] private Button btnTransaction;
    [SerializeField] private Button btnRecord;
    [SerializeField] private Button btnOthers;
    [SerializeField] private Button btnSpecialSouvenir;

    // Binder 清單：順序即優先權，DefaultBinder 永遠放最後
    private static readonly List<IAchievementViewBinder> Binders = new()
    {
        new HiddenConditionBinder(),
        new ProgressBinder(),
        new DefaultBinder(),
    };

    // 紀錄所有生成的 View 與對應 Binder，供 Manager 統一 Refresh 使用
    private readonly List<(IAchievementDisplayData data, IAchievementDisplayView view, IAchievementViewBinder binder)> _entries = new();

    // 目前選取的分類
    private AchievementCategory _currentCategory = AchievementCategory.Item;

    private void Start()
    {
        // 綁定按鈕事件
        btnItem?.onClick.AddListener(() => SwitchCategory(AchievementCategory.Item));
        btnTransaction?.onClick.AddListener(() => SwitchCategory(AchievementCategory.Transaction));
        btnRecord?.onClick.AddListener(() => SwitchCategory(AchievementCategory.Record));
        btnOthers?.onClick.AddListener(() => SwitchCategory(AchievementCategory.Others));
        btnSpecialSouvenir?.onClick.AddListener(() => SwitchCategory(AchievementCategory.SpecialSouvenir));
        _souvenirProvider = Souvenir.SouvenirManager.Instance;
    }

    private void OnEnable()
    {
        // 開啟時預設選取第一個分類
        SwitchCategory(AchievementCategory.Item);
    }

    /// <summary>切換分類頁籤，重新生成該分類的成就 View</summary>
    public void SwitchCategory(AchievementCategory category)
    {
        _currentCategory = category;

        UpdateButtonVisuals();

        if (category == AchievementCategory.SpecialSouvenir)
        {
            ISpecialSouvenirProvider provider = _souvenirProvider;
            var displayDataList = provider.GetAllSpecialSouvenirSaves()
                .Cast<IAchievementDisplayData>()
                .ToList();
            BuildAll(displayDataList);
        }
        else
        {
            var achievements = AchievementManager.Instance.GetAchievementsByCategory(category);
            var displayDataList = new List<IAchievementDisplayData>();
            foreach (var achievement in achievements)
            {
                displayDataList.Add(achievement);
            }
            BuildAll(displayDataList);
        }

        // 切換分類後將捲動位置重置到最上方
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void UpdateButtonVisuals()
    {
        SetButtonActive(btnItem, _currentCategory == AchievementCategory.Item);
        SetButtonActive(btnTransaction, _currentCategory == AchievementCategory.Transaction);
        SetButtonActive(btnRecord, _currentCategory == AchievementCategory.Record);
        SetButtonActive(btnOthers, _currentCategory == AchievementCategory.Others);
        SetButtonActive(btnSpecialSouvenir, _currentCategory == AchievementCategory.SpecialSouvenir);
    }

    private void SetButtonActive(Button btn, bool isActive)
    {
        if (btn != null && btn.transform.childCount > 0)
        {
            btn.transform.GetChild(0).gameObject.SetActive(isActive);
        }
    }

    /// <summary>一次生成所有成就 View（初始化時呼叫）</summary>
    public void BuildAll(List<IAchievementDisplayData> dataList)
    {
        ClearAll();
        foreach (var data in dataList)
            CreateOne(data);
    }

    /// <summary>生成單一成就 View</summary>
    public IAchievementDisplayView CreateOne(IAchievementDisplayData data)
    {
        var view = InstantiateView(data);
        var binder = Binders.First(b => b.CanBind(data));

        // 初次顯示
        binder.Refresh(data, view);
        view.Refresh();

        _entries.Add((data, view, binder));
        return view;
    }

    /// <summary>Manager 呼叫：刷新所有 View（例如開啟成就面板時）</summary>
    public void RefreshAll()
    {
        foreach (var (data, view, binder) in _entries)
        {
            binder.Refresh(data, view);
            view.Refresh();
        }
    }

    /// <summary>Manager 呼叫：刷新特定成就 View（例如某成就剛解鎖時）</summary>
    public void RefreshOne(IAchievementDisplayData data)
    {
        var entry = _entries.FirstOrDefault(e => e.data == data);
        if (entry == default) return;

        entry.binder.Refresh(entry.data, entry.view);
        entry.view.Refresh();
    }

    /// <summary>清除所有已生成的 View</summary>
    public void ClearAll()
    {
        foreach (var (_, view, _) in _entries)
        {
            view.Unbind();
            Destroy(((MonoBehaviour)view).gameObject);
        }
        _entries.Clear();
    }

    /// <summary>取得目前選取的分類</summary>
    public AchievementCategory CurrentCategory => _currentCategory;

    // --- 私有輔助 ---

    /// <summary>根據顯示資料類型選擇對應 Prefab 並實例化</summary>
    private IAchievementDisplayView InstantiateView(IAchievementDisplayData data)
    {
        switch (data)
        {
            case IAchievementWithProgress:
                var progressView = Instantiate(progressPrefab, container);
                progressView.Bind(data);
                return progressView;
            case IAchievementHiddenCondition:
                var hiddenView = Instantiate(defaultPrefab, container);
                hiddenView.Bind(data);
                return hiddenView;

            default:
                var defaultView = Instantiate(defaultPrefab, container);
                defaultView.Bind(data);
                return defaultView;
        }
    }

    private void OnDestroy()
    {
        btnItem?.onClick.RemoveAllListeners();
        btnTransaction?.onClick.RemoveAllListeners();
        btnRecord?.onClick.RemoveAllListeners();
        btnOthers?.onClick.RemoveAllListeners();
        btnSpecialSouvenir?.onClick.RemoveAllListeners();
    }
}