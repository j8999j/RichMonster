using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MissionRequirement))]
public class MissionRequirementDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var typeProp = property.FindPropertyRelative("Type");
        var itemIDProp = property.FindPropertyRelative("TargetItemID");
        var tagProp = property.FindPropertyRelative("TargetTag");
        var itemTypeProp = property.FindPropertyRelative("TargetType");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        Rect line = new Rect(position.x, position.y, position.width, lineHeight);

        // 繪製 RequirementType
        EditorGUI.PropertyField(line, typeProp, new GUIContent("需求類型"));
        line.y += lineHeight + spacing;

        var reqType = (RequirementType)typeProp.enumValueIndex;

        switch (reqType)
        {
            case RequirementType.SpecificItem:
                EditorGUI.PropertyField(line, itemIDProp, new GUIContent("指定物品"));
                break;

            case RequirementType.SpecificTag:
                EditorGUI.PropertyField(line, tagProp, new GUIContent("指定標籤"));
                break;

            case RequirementType.SpecificType:
                EditorGUI.PropertyField(line, itemTypeProp, new GUIContent("指定類型"));
                break;

            case RequirementType.None:
                EditorGUI.HelpBox(line, "無需求限制", MessageType.Info);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // 類型選擇行 + 對應欄位行
        return (lineHeight + spacing) * 2;
    }
}
