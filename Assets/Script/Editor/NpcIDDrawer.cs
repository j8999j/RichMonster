using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(NpcIDSelectAttribute))]
public class NpcIDDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [NpcIDSelect] with string only.");
            return;
        }

        // 繪製標籤
        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        EditorGUI.LabelField(labelRect, label);

        // 繪製按鈕
        Rect buttonRect = new Rect(
            position.x + EditorGUIUtility.labelWidth + 2,
            position.y,
            position.width - EditorGUIUtility.labelWidth - 2,
            position.height
        );

        // 取得當前值的顯示名稱
        string currentValue = property.stringValue;
        string displayText = GetDisplayName(currentValue);

        if (GUI.Button(buttonRect, displayText, EditorStyles.popup))
        {
            // 建立選項列表
            var options = BuildOptionList();

            // 顯示搜尋視窗
            SearchablePopupWindow.Show(buttonRect, options, currentValue, (selected) =>
            {
                property.serializedObject.Update();
                property.stringValue = selected;
                property.serializedObject.ApplyModifiedProperties();
            });
        }
    }

    private SearchablePopupWindow.OptionData[] BuildOptionList()
    {
        var monsters = EditorMissionDataLoader.GetAllMonsters();
        var npcs = EditorMissionDataLoader.GetAllNPCs();
        var options = new List<SearchablePopupWindow.OptionData>();

        // 加入空選項
        options.Add(new SearchablePopupWindow.OptionData
        {
            Value = "",
            DisplayName = "(None)",
            SearchText = "none 無"
        });

        // 妖怪選項
        foreach (var monster in monsters)
        {
            options.Add(new SearchablePopupWindow.OptionData
            {
                Value = monster.Id,
                DisplayName = $"[妖怪] {monster.ProfessionName} ({monster.Id})",
                SearchText = $"妖怪 {monster.ProfessionName} {monster.Id} {monster.Race}".ToLower()
            });
        }

        // NPC 選項
        foreach (var npc in npcs)
        {
            options.Add(new SearchablePopupWindow.OptionData
            {
                Value = npc.NpcID,
                DisplayName = $"[NPC] {npc.NpcName} ({npc.NpcID})",
                SearchText = $"npc {npc.NpcName} {npc.NpcID}".ToLower()
            });
        }

        return options.ToArray();
    }

    private string GetDisplayName(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
            return "(None)";

        // 先查妖怪
        var monster = EditorMissionDataLoader.GetMonsterById(npcId);
        if (monster != null)
            return $"[妖怪] {monster.ProfessionName} ({monster.Id})";

        // 再查 NPC
        var npc = EditorMissionDataLoader.GetNPCById(npcId);
        if (npc != null)
            return $"[NPC] {npc.NpcName} ({npc.NpcID})";

        return npcId;
    }
}
