using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameSystem;

public class ContractView : MonoBehaviour, IPlayerInfoPage
{
    [Header("UI Components")]
    public GameObject ContractPanel;
    public Button ContractButton;
    public Button ClosePanelButton;
    [SerializeField] private Image guaranteeDepositPaidImage;
    [SerializeField] private Image auctionEntryFeePaidImage;

    [Header("Guarantee Deposit")]
    [SerializeField] private Button payGuaranteeDepositButton;
    [SerializeField] private GameObject guaranteeDepositConfirmPanel;
    [SerializeField] private TextMeshProUGUI guaranteeDepositConfirmText;
    [SerializeField] private TextMeshProUGUI guaranteeDepositStatusText;
    [SerializeField] private Button guaranteeDepositConfirmButton;
    [SerializeField] private Button guaranteeDepositCancelButton;
    [SerializeField] private string guaranteeDepositConfirmFormat = "是否繳交契約保證金 {0} 元？";
    [SerializeField] private string guaranteeDepositPaidText = "契約保證金已繳交";
    [SerializeField] private string guaranteeDepositNotEnoughGoldText = "金額不足，無法繳交契約保證金";

    [Header("Sound Effects")]
    [SerializeField] private AudioClip openPanelSfx;
    [SerializeField] private AudioClip closePanelSfx;
    [SerializeField] private AudioClip confirmPanelSfx;
    [SerializeField] private AudioClip payDepositSfx;
    [SerializeField] private AudioClip payDepositFailedSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolumeScale = 1f;

    private void Awake()
    {
        EnsureReferences();

        if (ContractButton != null)
        {
            ContractButton.onClick.AddListener(RequestCloseContractPanel);
        }

        if (ClosePanelButton != null)
        {
            ClosePanelButton.onClick.AddListener(RequestCloseContractPanel);
        }

        if (payGuaranteeDepositButton != null)
        {
            payGuaranteeDepositButton.onClick.AddListener(ShowGuaranteeDepositConfirmPanel);
        }

        if (guaranteeDepositConfirmButton != null)
        {
            guaranteeDepositConfirmButton.onClick.AddListener(ConfirmPayGuaranteeDeposit);
        }

        if (guaranteeDepositCancelButton != null)
        {
            guaranteeDepositCancelButton.onClick.AddListener(HideGuaranteeDepositConfirmPanel);
        }
    }

    private void Start()
    {
        SetContractPanelVisible(false, false);
        HideGuaranteeDepositConfirmPanel();
        RefreshPaymentState();
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

        if (payGuaranteeDepositButton != null)
        {
            payGuaranteeDepositButton.onClick.RemoveListener(ShowGuaranteeDepositConfirmPanel);
        }

        if (guaranteeDepositConfirmButton != null)
        {
            guaranteeDepositConfirmButton.onClick.RemoveListener(ConfirmPayGuaranteeDeposit);
        }

        if (guaranteeDepositCancelButton != null)
        {
            guaranteeDepositCancelButton.onClick.RemoveListener(HideGuaranteeDepositConfirmPanel);
        }
    }

    public void OpenContractPanel()
    {
        SetContractPanelVisible(true, true);
        RefreshPaymentState();
    }

    public void OpenPage() => OpenContractPanel();

    public void CloseContractPanel()
    {
        SetContractPanelVisible(false, true);
    }

    public void ClosePage() => CloseContractPanel();

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
        if (visible)
            RefreshPaymentState();
        else
            HideGuaranteeDepositConfirmPanel();

        if (!playSound || wasActive == visible)
            return;

        PlaySfx(visible ? openPanelSfx : closePanelSfx);
    }

    private void RefreshPaymentState()
    {
        EnsureReferences();

        var playerData = DataManager.Instance?.CurrentPlayerData;
        bool hasPaidGuaranteeDeposit = playerData != null && playerData.HasPaidGuaranteeDeposit;
        bool hasPaidAuctionEntryFee = playerData != null && playerData.HasPaidAuctionEntryFee;
        int currentGold = playerData != null ? playerData.Gold : 0;

        if (guaranteeDepositPaidImage != null)
            guaranteeDepositPaidImage.gameObject.SetActive(hasPaidGuaranteeDeposit);

        if (auctionEntryFeePaidImage != null)
            auctionEntryFeePaidImage.gameObject.SetActive(hasPaidAuctionEntryFee);

        if (payGuaranteeDepositButton != null)
        {
            payGuaranteeDepositButton.gameObject.SetActive(!hasPaidGuaranteeDeposit);
            payGuaranteeDepositButton.interactable = playerData != null
                && !hasPaidGuaranteeDeposit;
        }

        if (guaranteeDepositConfirmButton != null)
        {
            guaranteeDepositConfirmButton.interactable = playerData != null
                && !hasPaidGuaranteeDeposit
                && currentGold >= EndingConditionDetector.GuaranteeDepositAmount;
        }

        if (guaranteeDepositStatusText != null)
        {
            guaranteeDepositStatusText.text = hasPaidGuaranteeDeposit
                ? guaranteeDepositPaidText
                : currentGold >= EndingConditionDetector.GuaranteeDepositAmount
                    ? string.Empty
                    : guaranteeDepositNotEnoughGoldText;
        }
    }

    private void ShowGuaranteeDepositConfirmPanel()
    {
        RefreshPaymentState();

        var playerData = DataManager.Instance?.CurrentPlayerData;
        if (playerData == null || playerData.HasPaidGuaranteeDeposit)
            return;

        if (guaranteeDepositConfirmText != null)
        {
            guaranteeDepositConfirmText.text = string.Format(
                guaranteeDepositConfirmFormat,
                EndingConditionDetector.GuaranteeDepositAmount.ToString("N0"));
        }

        if (guaranteeDepositConfirmPanel != null)
            guaranteeDepositConfirmPanel.SetActive(true);

        PlaySfx(confirmPanelSfx);
    }

    private void HideGuaranteeDepositConfirmPanel()
    {
        if (guaranteeDepositConfirmPanel != null)
            guaranteeDepositConfirmPanel.SetActive(false);
    }

    private async void ConfirmPayGuaranteeDeposit()
    {
        bool paid = DataManager.Instance != null && DataManager.Instance.TryPayGuaranteeDeposit();
        if (!paid)
        {
            PlaySfx(payDepositFailedSfx);
            RefreshPaymentState();
            return;
        }

        HideGuaranteeDepositConfirmPanel();
        GuaranteeDepositGuide.Hide();
        PlaySfx(payDepositSfx);
        RefreshPaymentState();

        if (GameManager.Instance?.gameFlow != null)
            await GameManager.Instance.gameFlow.SaveGameAsync();
    }

    private void EnsureReferences()
    {
        if (ContractPanel == null)
            return;

        if (guaranteeDepositPaidImage == null)
            guaranteeDepositPaidImage = FindImageInContractPanel("GuaranteeDepositPaidImage", "ContractDepositPaidImage", "PaidGuaranteeDepositImage");

        if (auctionEntryFeePaidImage == null)
            auctionEntryFeePaidImage = FindImageInContractPanel("AuctionEntryFeePaidImage", "AuctionPaidImage", "PaidAuctionEntryFeeImage");

        if (payGuaranteeDepositButton == null)
            payGuaranteeDepositButton = FindButtonInContractPanel("PayGuaranteeDepositButton", "PayContractDepositButton");

        if (guaranteeDepositConfirmPanel == null)
            guaranteeDepositConfirmPanel = FindObjectInContractPanel("GuaranteeDepositConfirmPanel", "ContractDepositConfirmPanel");

        if (guaranteeDepositConfirmButton == null && guaranteeDepositConfirmPanel != null)
            guaranteeDepositConfirmButton = FindButtonIn(guaranteeDepositConfirmPanel.transform, "ConfirmButton", "GuaranteeDepositConfirmButton");

        if (guaranteeDepositCancelButton == null && guaranteeDepositConfirmPanel != null)
            guaranteeDepositCancelButton = FindButtonIn(guaranteeDepositConfirmPanel.transform, "CancelButton", "GuaranteeDepositCancelButton");

        if (guaranteeDepositConfirmText == null && guaranteeDepositConfirmPanel != null)
            guaranteeDepositConfirmText = guaranteeDepositConfirmPanel.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private Image FindImageInContractPanel(params string[] names)
    {
        GameObject found = FindObjectInContractPanel(names);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private Button FindButtonInContractPanel(params string[] names)
    {
        GameObject found = FindObjectInContractPanel(names);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private GameObject FindObjectInContractPanel(params string[] names)
    {
        return FindObjectIn(ContractPanel.transform, names);
    }

    private Button FindButtonIn(Transform root, params string[] names)
    {
        GameObject found = FindObjectIn(root, names);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private GameObject FindObjectIn(Transform root, params string[] names)
    {
        if (root == null || names == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (string name in names)
            {
                if (child != null && child.name == name)
                    return child.gameObject;
            }
        }

        return null;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
    }
}
