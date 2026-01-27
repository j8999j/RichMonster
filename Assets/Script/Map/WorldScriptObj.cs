using UnityEngine;
using UnityEngine.AddressableAssets;
[CreateAssetMenu(fileName = "New World", menuName = "ScriptableObjects/WorldScriptObj")]
public class SceneScriptObj : ScriptableObject
{
    public AssetReference SceneID;
    public string WorldName;
}
