using UnityEngine;
using UnityEditor;
using GameSystem;

[CustomPropertyDrawer(typeof(AbyssRewardItem))]
public class AbyssRewardItemDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty rewardTypeProp = property.FindPropertyRelative("RewardType");
        SerializedProperty goldAmountProp = property.FindPropertyRelative("GoldAmount");
        SerializedProperty itemIDProp = property.FindPropertyRelative("ItemID");
        SerializedProperty itemAmountProp = property.FindPropertyRelative("ItemAmount");
        SerializedProperty weightProp = property.FindPropertyRelative("Weight");

        var rect = new Rect(position.x, position.y, position.width, lineHeight);
        
        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
        rect.y += lineHeight + spacing;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // RewardType
            rect.height = lineHeight;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, rewardTypeProp);
            if (EditorGUI.EndChangeCheck())
            {
                // 切換時將所有數值清空為 0 或空字串
                goldAmountProp.intValue = 0;
                itemAmountProp.intValue = 0;
                weightProp.intValue = 0;
                itemIDProp.stringValue = string.Empty;
            }
            rect.y += lineHeight + spacing;

            AbyssRewardType type = (AbyssRewardType)rewardTypeProp.enumValueIndex;

            if (type == AbyssRewardType.MonsterGold)
            {
                rect.height = lineHeight;
                EditorGUI.PropertyField(rect, goldAmountProp, new GUIContent("妖怪幣數量"));
                rect.y += lineHeight + spacing;
            }
            else if (type == AbyssRewardType.Item)
            {
                float idHeight = EditorGUI.GetPropertyHeight(itemIDProp);
                rect.height = idHeight;
                EditorGUI.PropertyField(rect, itemIDProp, new GUIContent("物品ID"));
                rect.y += idHeight + spacing;
                
                rect.height = lineHeight;
                EditorGUI.PropertyField(rect, itemAmountProp, new GUIContent("物品數量"));
                rect.y += lineHeight + spacing;
            }

            // Weight
            rect.height = lineHeight;
            EditorGUI.PropertyField(rect, weightProp, new GUIContent("權重"));
            
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float height = lineHeight; 

        if (property.isExpanded)
        {
            height += spacing;
            height += lineHeight + spacing; // RewardType

            SerializedProperty rewardTypeProp = property.FindPropertyRelative("RewardType");
            AbyssRewardType type = (AbyssRewardType)rewardTypeProp.enumValueIndex;

            if (type == AbyssRewardType.MonsterGold)
            {
                height += lineHeight + spacing; // GoldAmount
            }
            else if (type == AbyssRewardType.Item)
            {
                SerializedProperty itemIDProp = property.FindPropertyRelative("ItemID");
                height += EditorGUI.GetPropertyHeight(itemIDProp) + spacing; // ItemID
                height += lineHeight + spacing; // ItemAmount
            }

            height += lineHeight + spacing; // Weight
        }

        return height;
    }
}
