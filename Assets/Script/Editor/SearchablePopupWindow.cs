using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class SearchablePopupWindow : EditorWindow
{
    public struct OptionData
    {
        public string Value;
        public string DisplayName;
        public string SearchText;
        public string TypeFilter;
        public string WorldFilter;
    }

    public struct FilterOption
    {
        public string Value;
        public string DisplayName;
    }

    private static SearchablePopupWindow _instance;
    private string _searchText = "";
    private Vector2 _scrollPos;
    private OptionData[] _allOptions;
    private readonly List<OptionData> _filteredOptions = new List<OptionData>();
    private FilterOption[] _typeFilters;
    private FilterOption[] _worldFilters;
    private string _selectedTypeFilter = "";
    private string _selectedWorldFilter = "";
    private Action<string> _onSelected;
    private string _currentValue;
    private int _selectedIndex = -1;

    public static void Show(Rect buttonRect, OptionData[] options, string currentValue, Action<string> onSelected)
    {
        Show(buttonRect, options, currentValue, onSelected, null, null);
    }

    public static void Show(
        Rect buttonRect,
        OptionData[] options,
        string currentValue,
        Action<string> onSelected,
        FilterOption[] typeFilters,
        FilterOption[] worldFilters)
    {
        if (_instance != null)
        {
            _instance.Close();
        }

        _instance = CreateInstance<SearchablePopupWindow>();
        _instance._allOptions = options ?? Array.Empty<OptionData>();
        _instance._currentValue = currentValue;
        _instance._onSelected = onSelected;
        _instance._typeFilters = typeFilters;
        _instance._worldFilters = worldFilters;
        _instance._selectedTypeFilter = "";
        _instance._selectedWorldFilter = "";
        _instance._searchText = "";
        _instance.FilterOptions();

        float windowWidth = Mathf.Max(buttonRect.width, 360);
        float windowHeight = 360;
        _instance.ShowAsDropDown(buttonRect, new Vector2(windowWidth, windowHeight));
    }

    private void FilterOptions()
    {
        _filteredOptions.Clear();
        string search = _searchText.ToLower().Trim();

        foreach (var opt in _allOptions)
        {
            if (!MatchesFilters(opt)) continue;
            if (!string.IsNullOrEmpty(search) && !opt.SearchText.Contains(search)) continue;
            _filteredOptions.Add(opt);
        }

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

    private bool MatchesFilters(OptionData option)
    {
        if (string.IsNullOrEmpty(option.Value))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(_selectedTypeFilter) && option.TypeFilter != _selectedTypeFilter)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(_selectedWorldFilter) && option.WorldFilter != _selectedWorldFilter)
        {
            return false;
        }

        return true;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.SetNextControlName("SearchField");
        string newSearch = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
        if (newSearch != _searchText)
        {
            _searchText = newSearch;
            FilterOptions();
        }
        EditorGUILayout.EndHorizontal();

        DrawFilterToolbar(_typeFilters, ref _selectedTypeFilter);
        DrawFilterToolbar(_worldFilters, ref _selectedWorldFilter);

        EditorGUILayout.LabelField($"Results: {_filteredOptions.Count} / {_allOptions.Length}", EditorStyles.miniLabel);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        for (int i = 0; i < _filteredOptions.Count; i++)
        {
            var opt = _filteredOptions[i];
            bool isSelected = opt.Value == _currentValue;
            GUIStyle style = isSelected ? CreateSelectedStyle() : EditorStyles.label;

            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            if (GUI.Button(rect, opt.DisplayName, style))
            {
                _onSelected?.Invoke(opt.Value);
                Close();
                return;
            }

            if (rect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.2f));
                Repaint();
            }
        }
        EditorGUILayout.EndScrollView();

        HandleKeyboardInput();

        if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(_searchText))
        {
            EditorGUI.FocusTextInControl("SearchField");
        }
    }

    private void DrawFilterToolbar(FilterOption[] filters, ref string selectedValue)
    {
        if (filters == null || filters.Length == 0) return;

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        foreach (var filter in filters)
        {
            bool selected = selectedValue == filter.Value;
            bool nextSelected = GUILayout.Toggle(selected, filter.DisplayName, EditorStyles.toolbarButton);
            if (nextSelected && !selected)
            {
                selectedValue = filter.Value;
                FilterOptions();
            }
        }
        EditorGUILayout.EndHorizontal();
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
