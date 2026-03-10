using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MissionReward))]
public class MissionRewardDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var rewardTypeProp = property.FindPropertyRelative("RewardType");
        var goldAmountProp = property.FindPropertyRelative("GoldAmount");
        var itemIDProp     = property.FindPropertyRelative("ItemID");
        var itemAmountProp = property.FindPropertyRelative("ItemAmount");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing    = EditorGUIUtility.standardVerticalSpacing;
        Rect line        = new Rect(position.x, position.y, position.width, lineHeight);

        int previousType = rewardTypeProp.enumValueIndex;

        EditorGUI.PropertyField(line, rewardTypeProp);
        line.y += lineHeight + spacing;

        // 切換類型時重置所有欄位
        if (rewardTypeProp.enumValueIndex != previousType)
        {
            goldAmountProp.intValue = 0;
            itemIDProp.stringValue  = string.Empty;
            itemAmountProp.intValue = 0;
            property.serializedObject.ApplyModifiedProperties();
        }

        var rewardType = (RewardType)rewardTypeProp.enumValueIndex;

        switch (rewardType)
        {
            case RewardType.Gold:
                EditorGUI.PropertyField(line, goldAmountProp, new GUIContent("金幣數量"));
                break;

            case RewardType.Item:
                EditorGUI.PropertyField(line, itemIDProp, new GUIContent("物品 ID"));
                line.y += lineHeight + spacing;
                EditorGUI.PropertyField(line, itemAmountProp, new GUIContent("物品數量"));
                break;

            case RewardType.Information:
                // 無額外欄位，僅顯示提示文字
                EditorGUI.HelpBox(line, "解鎖隨機妖怪情報", MessageType.Info);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var rewardTypeProp = property.FindPropertyRelative("RewardType");
        var rewardType     = (RewardType)rewardTypeProp.enumValueIndex;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing    = EditorGUIUtility.standardVerticalSpacing;

        return rewardType switch
        {
            RewardType.Item        => (lineHeight + spacing) * 3,
            RewardType.Information => (lineHeight + spacing) * 2, // 類型 + HelpBox
            _                     => (lineHeight + spacing) * 2  // Gold：類型 + 數量
        };
    }
}