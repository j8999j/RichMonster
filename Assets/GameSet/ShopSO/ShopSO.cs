using UnityEngine;

public class ShopIDSelectorAttribute : PropertyAttribute { }

[CreateAssetMenu(fileName = "ShopSO", menuName = "GameSet/ShopSO", order = 1)]
public class ShopSO : ScriptableObject
{
    [ShopIDSelector]
    public string ShopID;
}
