using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(ItemIDSelectAttribute))]
public class ItemIDDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [ItemIDSelect] with string only.");
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
        var items = EditorMissionDataLoader.GetAllItems();
        var options = new List<SearchablePopupWindow.OptionData>();

        // 加入空選項
        options.Add(new SearchablePopupWindow.OptionData
        {
            Value = "",
            DisplayName = "(None)",
            SearchText = "none 無"
        });

        foreach (var item in items)
        {
            options.Add(new SearchablePopupWindow.OptionData
            {
                Value = item.Id,
                DisplayName = $"{item.Name} ({item.Id})",
                SearchText = $"{item.Name} {item.Id}".ToLower()
            });
        }

        return options.ToArray();
    }

    private string GetDisplayName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return "(None)";

        var item = EditorMissionDataLoader.GetItemById(itemId);
        if (item != null)
            return $"{item.Name} ({item.Id})";

        return itemId;
    }
}
