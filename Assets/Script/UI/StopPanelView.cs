using System.Threading.Tasks;
using GameSystem;
using UnityEngine;
using UnityEngine.UI;

public class StopPanelView : MonoBehaviour
{
    public Button StopButton;
    public Button HomeButton;
    public Button NotionButton;
    public GameObject StopPanel;
    private void Awake()
    {
        if (StopButton != null)
            StopButton.onClick.AddListener(OnStop);
        if (HomeButton != null)
            HomeButton.onClick.AddListener(OnHome);
        if (NotionButton != null)
            NotionButton.onClick.AddListener(OnContinue);
    }

    private void OnStop()
    {
        StopPanel.SetActive(true);
    }

    private async void OnHome()
    {
        await GameManager.Instance.gameFlow.SaveGameAsync();
        GameManager.Instance.GoToMainMenu();
    }
    private void OnContinue()
    {
        //顯示提示
    }
}