using UnityEngine;
using Player;
using GameSystem;

public class ChangeSceneDoor : MonoBehaviour, IInteractable //切換場景
{
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private SceneScriptObj scene;
    public void Interact()
    {
        //傳送前往妖界
        ChangeScene();
    }
    public void ShowPrompt()
    {
        interactPrompt.SetActive(true);
    }
    public void HidePrompt()
    {

    }
    public void ChangeScene()
    {
        Debug.Log(scene.SceneID.AssetGUID.ToString());
        GameManager.Instance.LoadScene(scene.SceneID.AssetGUID.ToString());
    }
}
