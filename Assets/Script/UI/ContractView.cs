using UnityEngine;
using UnityEngine.UI;
using GameSystem;

public class ContractView : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject ContractPanel;
    public Button ContractButton;
    public Button ClosePanelButton;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip openPanelSfx;
    [SerializeField] private AudioClip closePanelSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolumeScale = 1f;

    private void Awake()
    {
        if (ContractButton != null)
        {
            ContractButton.onClick.AddListener(RequestCloseContractPanel);
        }

        if (ClosePanelButton != null)
        {
            ClosePanelButton.onClick.AddListener(RequestCloseContractPanel);
        }
    }

    private void Start()
    {
        SetContractPanelVisible(false, false);
    }

    private void OnDestroy()
    {
        if (ContractButton != null)
        {
            ContractButton.onClick.RemoveListener(RequestCloseContractPanel);
        }

        if (ClosePanelButton != null)
        {
            ClosePanelButton.onClick.RemoveListener(RequestCloseContractPanel);
        }
    }

    public void OpenContractPanel()
    {
        SetContractPanelVisible(true, true);
    }

    public void CloseContractPanel()
    {
        SetContractPanelVisible(false, true);
    }

    public void RequestCloseContractPanel()
    {
        PlayerInfoUIEvents.InvokeCloseAll();
    }

    public void ToggleContractPanel()
    {
        bool isActive = ContractPanel != null && ContractPanel.activeSelf;
        SetContractPanelVisible(!isActive, true);
    }

    private void SetContractPanelVisible(bool visible, bool playSound)
    {
        if (ContractPanel == null)
            return;

        bool wasActive = ContractPanel.activeSelf;
        ContractPanel.SetActive(visible);

        if (!playSound || wasActive == visible)
            return;

        PlaySfx(visible ? openPanelSfx : closePanelSfx);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
    }
}
