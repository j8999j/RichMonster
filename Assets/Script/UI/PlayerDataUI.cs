using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class PlayerDataUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI daysPlayedText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Image statusIconImage;
    [SerializeField] private GameObject HumanIcon;
    [SerializeField] private GameObject MonsterIcon;
    [Header("Status Sprites")]
    [SerializeField] private Sprite morningSprite;
    [SerializeField] private Sprite noonSprite;
    [SerializeField] private Sprite nightSprite;
    private void OnEnable()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.PlayerMainViewUpdate += UpdateUI;
        }
    }

    private void OnDisable()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.PlayerMainViewUpdate -= UpdateUI;
        }
    }
    private void Start()
    {
        DataManager.Instance.ShowPlayerMainData();
    }
    /// <summary>
    /// Updates the UI with current player data from DataManager.
    /// </summary>
    public void UpdateUI(int daysPlayed, int gold, DayPhase playingStatus)
    {
        UpdatePlayingStatus(daysPlayed, playingStatus);
        UpdateGold(gold);
    }
    private void UpdatePlayingStatus(int newStatus, DayPhase newPhase)
    {
        statusIconImage.sprite = GetStatusSprite(newPhase);
        daysPlayedText.text = newStatus.ToString();
        if (newPhase == DayPhase.HumanDay)
        {
            HumanIcon.SetActive(true);
            MonsterIcon.SetActive(false);
        }
        else
        {
            HumanIcon.SetActive(false);
            MonsterIcon.SetActive(true);
        }
    }
    private void UpdateGold(int newGold)
    {
        goldText.text = newGold.ToString();
    }
    private Sprite GetStatusSprite(DayPhase status)
    {
        return status switch
        {
            DayPhase.HumanDay => morningSprite,
            DayPhase.Night => nightSprite,
            DayPhase.NightTrade => nightSprite,
            _ => null
        };
    }
}
