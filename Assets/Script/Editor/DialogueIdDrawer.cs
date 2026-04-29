using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueIdSelectAttribute))]
public class DialogueIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [DialogueIdSelect] with string only.");
            return;
        }

        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        EditorGUI.LabelField(labelRect, label);

        Rect buttonRect = new Rect(
            position.x + EditorGUIUtility.labelWidth + 2,
            position.y,
            position.width - EditorGUIUtility.labelWidth - 2,
            position.height
        );

        string currentValue = property.stringValue;
        string displayText = GetDisplayName(currentValue);

        if (GUI.Button(buttonRect, displayText, EditorStyles.popup))
        {
            SearchablePopupWindow.Show(buttonRect, BuildOptionList(), currentValue, selected =>
            {
                property.serializedObject.Update();
                property.stringValue = selected;
                property.serializedObject.ApplyModifiedProperties();
            });
        }
    }

    private SearchablePopupWindow.OptionData[] BuildOptionList()
    {
        List<SearchablePopupWindow.OptionData> options = new List<SearchablePopupWindow.OptionData>
        {
            new SearchablePopupWindow.OptionData
            {
                Value = "",
                DisplayName = "(None)",
                SearchText = "none 無"
            }
        };

        foreach (EditorDialogueDataLoader.DialogueOption dialogue in EditorDialogueDataLoader.GetAllDialogues())
        {
            options.Add(new SearchablePopupWindow.OptionData
            {
                Value = dialogue.DialogueId,
                DisplayName = $"{dialogue.DialogueId} ({dialogue.AssetPath})",
                SearchText = $"{dialogue.DialogueId} {dialogue.AssetPath}".ToLower()
            });
        }

        return options.ToArray();
    }

    private string GetDisplayName(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            return "(None)";
        }

        EditorDialogueDataLoader.DialogueOption dialogue = EditorDialogueDataLoader.GetDialogueById(dialogueId);
        if (dialogue != null)
        {
            return $"{dialogue.DialogueId} ({dialogue.AssetPath})";
        }

        return $"{dialogueId} (missing)";
    }
}
