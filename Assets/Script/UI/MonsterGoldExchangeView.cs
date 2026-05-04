using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterGoldExchangeView : MonoBehaviour
{
    [Header("Exchange Panel")]
    [SerializeField] private GameObject exchangePanel;
    [SerializeField] private TextMeshProUGUI monsterGoldText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI rateText;
    [SerializeField] private Button exchangeButton;
    [SerializeField] private Button closeButton;

    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Messages")]
    [SerializeField] private string rateFormat = "\u514C\u63DB\u6BD4\u4F8B 4 : 3";
    [SerializeField] private string monsterGoldFormat = "\u5996\u754C\u5E63: {0}";
    [SerializeField] private string goldFormat = "\u53EF\u7372\u5F97\u91D1\u5E63: {0}";
    [SerializeField] private string confirmMessageFormat = "\u78BA\u5B9A\u8981\u5C07 <color=red>{0}</color> \u5996\u754C\u5E63\u514C\u63DB\u70BA <color=yellow>{1}</color> \u91D1\u5E63\u55CE\uFF1F";

    public event Action OnCloseRequested;
    public event Action OnExchangeConfirmed;

    private int currentMonsterGold;
    private int currentExchangeGold;

    private void Awake()
    {
        if (exchangeButton != null) exchangeButton.onClick.AddListener(ShowConfirmPanel);
        if (closeButton != null) closeButton.onClick.AddListener(HandleCloseClicked);
        if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(HideConfirmPanel);

        Close();
    }

    private void OnDestroy()
    {
        if (exchangeButton != null) exchangeButton.onClick.RemoveListener(ShowConfirmPanel);
        if (closeButton != null) closeButton.onClick.RemoveListener(HandleCloseClicked);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(HideConfirmPanel);
    }

    public void Open(int monsterGold, int exchangeGold)
    {
        if (exchangePanel != null) exchangePanel.SetActive(true);
        HideConfirmPanel();
        Refresh(monsterGold, exchangeGold);
    }

    public void Close()
    {
        if (exchangePanel != null) exchangePanel.SetActive(false);
        HideConfirmPanel();
    }

    public void Refresh(int monsterGold, int exchangeGold)
    {
        currentMonsterGold = monsterGold;
        currentExchangeGold = exchangeGold;

        if (rateText != null) rateText.text = rateFormat;
        if (monsterGoldText != null) monsterGoldText.text = string.Format(monsterGoldFormat, currentMonsterGold);
        if (goldText != null) goldText.text = string.Format(goldFormat, currentExchangeGold);
        if (exchangeButton != null) exchangeButton.interactable = currentMonsterGold > 0 && currentExchangeGold > 0;

        if (confirmPanel != null && confirmPanel.activeSelf)
            UpdateConfirmMessage();
    }

    public void HideConfirmPanel()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void ShowConfirmPanel()
    {
        if (currentMonsterGold <= 0 || currentExchangeGold <= 0)
            return;

        if (confirmPanel != null) confirmPanel.SetActive(true);
        UpdateConfirmMessage();
    }

    private void UpdateConfirmMessage()
    {
        if (confirmMessageText != null)
        {
            confirmMessageText.text = string.Format(confirmMessageFormat, currentMonsterGold, currentExchangeGold);
        }
    }

    private void HandleCloseClicked()
    {
        OnCloseRequested?.Invoke();
    }

    private void HandleConfirmClicked()
    {
        OnExchangeConfirmed?.Invoke();
    }
}
