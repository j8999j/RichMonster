using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

[CustomPropertyDrawer(typeof(ShopIDSelectorAttribute))]
public class ShopIDSelectorDrawer : PropertyDrawer
{
    private List<ShopDefinition> _shops;
    private string[] _displayOptions;

    private void LoadData()
    {
        if (_shops != null) return;

        string path = Path.Combine(Application.dataPath, "GameResources", "shops.json");
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                var database = JsonConvert.DeserializeObject<ShopDatabase>(json);
                if (database != null && database.Shops != null)
                {
                    _shops = database.Shops;
                    _displayOptions = new string[_shops.Count];
                    for (int i = 0; i < _shops.Count; i++)
                    {
                        // 依照需求顯示商店ID中文名稱，與貨架數量
                        _displayOptions[i] = $"{_shops[i].ShopID} ({_shops[i].ShopName} - 貨架: {_shops[i].ShelfCount})";
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to parse shops.json in ShopIDSelectorDrawer: " + e.Message);
            }
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            LoadData();

            if (_shops != null && _shops.Count > 0)
            {
                int selectedIndex = 0;
                for (int i = 0; i < _shops.Count; i++)
                {
                    if (_shops[i].ShopID == property.stringValue)
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, _displayOptions);
                if (newIndex != selectedIndex || string.IsNullOrEmpty(property.stringValue))
                {
                    property.stringValue = _shops[newIndex].ShopID;
                    
                    // 自動帶入 ShopName，讓企劃不用手動輸入
                    SerializedProperty nameProperty = property.serializedObject.FindProperty("ShopName");
                    if (nameProperty != null && nameProperty.propertyType == SerializedPropertyType.String)
                    {
                        nameProperty.stringValue = _shops[newIndex].ShopName;
                    }
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [ShopIDSelector] on a string field.");
        }
    }
}
