using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/// <summary>
/// 可搜尋的彈出視窗 - 支援中英文快速搜尋
/// </summary>
public class SearchablePopupWindow : EditorWindow
{
    // 選項資料結構
    public struct OptionData
    {
        public string Value;       // 實際儲存的值 (例如: ItemId, TagId)
        public string DisplayName; // 顯示名稱 (例如: "礦泉水 (Mineralwater)")
        public string SearchText;  // 搜尋用文字 (中文名 + 英文ID，小寫)
    }

    private static SearchablePopupWindow _instance;
    private string _searchText = "";
    private Vector2 _scrollPos;
    private OptionData[] _allOptions;
    private List<OptionData> _filteredOptions = new List<OptionData>();
    private Action<string> _onSelected;
    private string _currentValue;
    private int _selectedIndex = -1;

    /// <summary>
    /// 顯示搜尋視窗
    /// </summary>
    public static void Show(Rect buttonRect, OptionData[] options, string currentValue, Action<string> onSelected)
    {
        if (_instance != null)
        {
            _instance.Close();
        }

        _instance = CreateInstance<SearchablePopupWindow>();
        _instance._allOptions = options;
        _instance._currentValue = currentValue;
        _instance._onSelected = onSelected;
        _instance._searchText = "";
        _instance.FilterOptions();

        // 計算視窗位置和大小
        float windowWidth = Mathf.Max(buttonRect.width, 250);
        float windowHeight = 300;
        
        // 將按鈕位置轉換為螢幕座標
        Vector2 screenPos = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.yMax));
        Rect windowRect = new Rect(screenPos.x, screenPos.y, windowWidth, windowHeight);

        _instance.ShowAsDropDown(buttonRect, new Vector2(windowWidth, windowHeight));
    }

    private void FilterOptions()
    {
        _filteredOptions.Clear();
        string search = _searchText.ToLower().Trim();

        if (string.IsNullOrEmpty(search))
        {
            _filteredOptions.AddRange(_allOptions);
        }
        else
        {
            foreach (var opt in _allOptions)
            {
                if (opt.SearchText.Contains(search))
                {
                    _filteredOptions.Add(opt);
                }
            }
        }

        // 找到當前選中項目的索引
        _selectedIndex = -1;
        for (int i = 0; i < _filteredOptions.Count; i++)
        {
            if (_filteredOptions[i].Value == _currentValue)
            {
                _selectedIndex = i;
                break;
            }
        }
    }

    private void OnGUI()
    {
        // 搜尋框
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.SetNextControlName("SearchField");
        string newSearch = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
        if (newSearch != _searchText)
        {
            _searchText = newSearch;
            FilterOptions();
        }
        EditorGUILayout.EndHorizontal();

        // 結果數量提示
        EditorGUILayout.LabelField($"搜尋結果: {_filteredOptions.Count} / {_allOptions.Length}", EditorStyles.miniLabel);

        // 選項列表
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        
        for (int i = 0; i < _filteredOptions.Count; i++)
        {
            var opt = _filteredOptions[i];
            bool isSelected = (opt.Value == _currentValue);

            // 繪製選項按鈕
            GUIStyle style = isSelected ? CreateSelectedStyle() : EditorStyles.label;
            
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            if (GUI.Button(rect, opt.DisplayName, style))
            {
                _onSelected?.Invoke(opt.Value);
                Close();
                return;
            }

            // 滑鼠懸停效果
            if (rect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.2f));
                Repaint();
            }
        }

        EditorGUILayout.EndScrollView();

        // 鍵盤事件處理
        HandleKeyboardInput();

        // 自動聚焦搜尋框
        if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(_searchText))
        {
            EditorGUI.FocusTextInControl("SearchField");
        }
    }

    private void HandleKeyboardInput()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;

        switch (e.keyCode)
        {
            case KeyCode.DownArrow:
                _selectedIndex = Mathf.Min(_selectedIndex + 1, _filteredOptions.Count - 1);
                if (_selectedIndex >= 0 && _selectedIndex < _filteredOptions.Count)
                {
                    _currentValue = _filteredOptions[_selectedIndex].Value;
                }
                e.Use();
                Repaint();
                break;

            case KeyCode.UpArrow:
                _selectedIndex = Mathf.Max(_selectedIndex - 1, 0);
                if (_selectedIndex >= 0 && _selectedIndex < _filteredOptions.Count)
                {
                    _currentValue = _filteredOptions[_selectedIndex].Value;
                }
                e.Use();
                Repaint();
                break;

            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (_selectedIndex >= 0 && _selectedIndex < _filteredOptions.Count)
                {
                    _onSelected?.Invoke(_filteredOptions[_selectedIndex].Value);
                    Close();
                }
                e.Use();
                break;

            case KeyCode.Escape:
                Close();
                e.Use();
                break;
        }
    }

    private GUIStyle CreateSelectedStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.background = MakeTexture(1, 1, new Color(0.24f, 0.48f, 0.9f, 0.6f));
        style.normal.textColor = Color.white;
        return style;
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        _instance = null;
    }
}
