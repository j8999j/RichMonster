using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSystem;
using UnityEngine.UI;
public class MainMenu : MonoBehaviour
{
    public GameObject SavedGamePanel;
    public SaveSlotPresenter SaveSlotPresenter;
    [SerializeField] private Button NewGamesButton;
    [SerializeField] private Button SavedGamesButton;
    private void Start()
    {
        NewGamesButton.onClick.AddListener(StartNewGames);
        SavedGamesButton.onClick.AddListener(ShowSavedGames);
    }

    public void StartNewGames()
    {
        GameManager.Instance.StartNewGame();
    }
    public void ShowSavedGames()
    {
        SavedGamePanel.SetActive(true);
        SaveSlotPresenter.LoadAndDisplaySaveSlots();
    }

}
