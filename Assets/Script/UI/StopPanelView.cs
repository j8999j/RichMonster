using GameSystem;
using UnityEngine;
using UnityEngine.UI;

public class StopPanelView : MonoBehaviour
{
    public Button StopButton;
    public Button HomeButton;
    public Button NotionButton;
    public Button ClosePanelButton;
    public GameObject StopPanel;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip openPanelSfx;
    [SerializeField] private AudioClip closePanelSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private float sfxVolumeScale = 1f;

    private void Awake()
    {
        if (StopButton != null)
            StopButton.onClick.AddListener(OnStop);
        if (HomeButton != null)
            HomeButton.onClick.AddListener(OnHome);
        if (NotionButton != null)
            NotionButton.onClick.AddListener(OnContinue);
        if (ClosePanelButton != null)
            ClosePanelButton.onClick.AddListener(OnClosePanel);
    }

    private void OnDestroy()
    {
        if (StopButton != null)
            StopButton.onClick.RemoveListener(OnStop);
        if (HomeButton != null)
            HomeButton.onClick.RemoveListener(OnHome);
        if (NotionButton != null)
            NotionButton.onClick.RemoveListener(OnContinue);
        if (ClosePanelButton != null)
            ClosePanelButton.onClick.RemoveListener(OnClosePanel);
    }

    private void OnStop()
    {
        PlaySfx(buttonClickSfx);

        bool wasActive = StopPanel != null && StopPanel.activeSelf;
        if (StopPanel != null)
            StopPanel.SetActive(true);
        if (!wasActive)
            PlaySfx(openPanelSfx);
    }

    private async void OnHome()
    {
        PlaySfx(buttonClickSfx);

        await GameManager.Instance.gameFlow.SaveGameAsync();
        GameManager.Instance.GoToMainMenu();
    }

    private void OnContinue()
    {
        OnClosePanel();
    }

    private void OnClosePanel()
    {
        PlaySfx(buttonClickSfx);

        bool wasActive = StopPanel != null && StopPanel.activeSelf;
        if (StopPanel != null)
            StopPanel.SetActive(false);
        if (wasActive)
            PlaySfx(closePanelSfx);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
    }
}
