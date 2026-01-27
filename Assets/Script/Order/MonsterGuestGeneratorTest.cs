using System.Collections.Generic;
using UnityEngine;
using GameSystem;

/// <summary>
/// 妖怪客人生成測試腳本
/// 在 Inspector 中設定天數和種子，點擊按鈕或進入 Play Mode 生成客人清單
/// </summary>
public class MonsterGuestGeneratorTest : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("主種子 (MasterSeed)")]
    public int MasterSeed = 12345;

    [Tooltip("要測試的天數")]
    public int DayNumber = 1;

    [Tooltip("強制指定客人數量 (0 表示使用隨機 6-10)")]
    public int ForceGuestCount = 0;

    [Header("測試")]
    [Tooltip("勾選後在 Start 時自動生成")]
    public bool GenerateOnStart = false;

    private MonsterGuestGenerator _generator;
    private List<MonsterGuest> _lastGeneratedGuests;

    void Start()
    {
        if (GenerateOnStart)
        {
            GenerateGuests();
        }
    }

    /// <summary>
    /// 初始化種子並生成客人清單
    /// </summary>
    [ContextMenu("Generate Guests")]
    public void GenerateGuests()
    {
        // 確保 DataManager 已初始化
        if (DataManager.Instance == null || !DataManager.Instance.IsInitialized)
        {
            Debug.LogError("[MonsterGuestGeneratorTest] DataManager 尚未初始化！");
            return;
        }

        // 建立生成器
        _generator = new MonsterGuestGenerator(
            new Dictionary<string, MonsterProfessionDefinition>(DataManager.Instance.MonsterProfessionDict),
            new Dictionary<string, MonsterTraitDefinition>(DataManager.Instance.MonsterTraitDict),
            new Dictionary<string, ItemTags>(DataManager.Instance.ItemTagsDict)
        );

        // 生成客人 (使用天數生成，數量按種子隨機 6-10 或強制指定)
        int? explicitCount = ForceGuestCount > 0 ? ForceGuestCount : null;
        _lastGeneratedGuests = _generator.GenerateGuestsForDay(DayNumber, explicitCount);

        // 輸出結果
        LogGeneratedGuests();
    }

    /// <summary>
    /// 輸出生成的客人清單到 Console
    /// </summary>
    private void LogGeneratedGuests()
    {
        Debug.Log($"========== Day {DayNumber} | Seed: {MasterSeed} | 客人數: {_lastGeneratedGuests.Count} ==========");

        for (int i = 0; i < _lastGeneratedGuests.Count; i++)
        {
            var guest = _lastGeneratedGuests[i];
            var customer = guest.monsterCustomer;
            var request = guest.monsterRequest;

            string traits = customer.Traits.Count > 0 
                ? string.Join(", ", customer.TraitNames) 
                : "無";

            string tags = request.RequestTags.Count > 0 
                ? string.Join(", ", request.RequestTags) 
                : "無";

            string preferredTags = customer.PreferredTags.Count > 0
                ? string.Join(", ", customer.PreferredTags)
                : "無";

            Debug.Log($"[客人 {i + 1}] " +
                $"職業: {customer.ProfessionName} ({customer.Type}) | " +
                $"種族: {customer.Race} | " +
                $"預算乘數: {customer.BudgetMultiplier:F2}");

            Debug.Log($"    特質: {traits}");
            Debug.Log($"    偏好標籤: {preferredTags}");
            Debug.Log($"    請求類型: {request.itemType} | 請求標籤: {tags}");
        }

        Debug.Log("==========================================================");
    }

    /// <summary>
    /// 測試多天生成，驗證種子重現性
    /// </summary>
    [ContextMenu("Test Multiple Days")]
    public void TestMultipleDays()
    {
        Debug.Log($"===== 測試多天生成 (Seed: {MasterSeed}) =====");

        for (int day = 1; day <= 3; day++)
        {
            DayNumber = day;
            GenerateGuests();
        }
    }

    /// <summary>
    /// 驗證同一天同種子會產生相同結果
    /// </summary>
    [ContextMenu("Verify Reproducibility")]
    public void VerifyReproducibility()
    {
        Debug.Log($"===== 驗證重現性 (Day: {DayNumber}, Seed: {MasterSeed}) =====");

        // 第一次生成
        GenerateGuests();
        var firstRun = new List<string>();
        foreach (var guest in _lastGeneratedGuests)
        {
            firstRun.Add($"{guest.monsterCustomer.Profession}|{string.Join(",", guest.monsterRequest.RequestTags)}");
        }

        // 第二次生成 (應該相同)
        GenerateGuests();
        var secondRun = new List<string>();
        foreach (var guest in _lastGeneratedGuests)
        {
            secondRun.Add($"{guest.monsterCustomer.Profession}|{string.Join(",", guest.monsterRequest.RequestTags)}");
        }

        // 比較
        bool identical = true;
        for (int i = 0; i < firstRun.Count; i++)
        {
            if (firstRun[i] != secondRun[i])
            {
                Debug.LogError($"[客人 {i}] 不一致！ First: {firstRun[i]} | Second: {secondRun[i]}");
                identical = false;
            }
        }

        if (identical)
        {
            Debug.Log("<color=green>✓ 重現性驗證通過！兩次生成結果完全相同。</color>");
        }
        else
        {
            Debug.LogError("✗ 重現性驗證失敗！請檢查 RNG 邏輯。");
        }
    }
}
