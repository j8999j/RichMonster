using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using GameSystem;

namespace Souvenir
{
    public class SouvenirBagView : MonoBehaviour, IPlayerInfoPage
    {
        [Header("Panels")]
        public GameObject MainPanel;
        [Header("Slot Containers")]
        public Transform SlotContainer;
        public SouvenirBagSlot SlotPrefab;

        [Header("Pagination")]
        public Button PreviousPageButton;
        public Button NextPageButton;
        private const int ItemsPerPage = 8;
        private int _currentPage = 0;

        [Header("Detail Display")]
        public TextMeshProUGUI DetailNameText;
        public TextMeshProUGUI DetailDescriptionText;
        public TextMeshProUGUI DetailFunctionText; // 成就效果或特殊紀念品解鎖條件
        public Image DetailIcon;
        public GameObject SpecialSouvenirDisplay;
        public Button InteractButton; // 互動按鈕 (查看/使用)
        public TextMeshProUGUI InteractButtonText; // 互動按鈕文字
        public float TargetLongEdgeSize = 150f;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip switchPageSound;
        [SerializeField] private AudioClip selectItemSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip useSound;
        [SerializeField] private AudioClip souKeyUseSound;
        [SerializeField] private AudioClip useFailedSound;

        [Header("Settings")]
        /// <summary>
        /// true：局內模式，只顯示本局快照內已生效的紀念品，並顯示可用的互動按鈕。
        /// false：主選單模式，顯示 Book 存檔內所有已解鎖的紀念品，並隱藏互動按鈕。
        /// </summary>
        public bool ShowCurrentRunOnly = false;

        private List<SouvenirBagSlot> _spawnedSlots = new List<SouvenirBagSlot>();
        private List<SouvenirBagItemData> _bagItems = new List<SouvenirBagItemData>();
        private SouvenirBagSlot _currentSelectedSlot;
        private UnityEngine.Events.UnityAction _currentInteractListener;

        private void Awake()
        {
            if (PreviousPageButton != null) PreviousPageButton.onClick.AddListener(OnPreviousPage);
            if (NextPageButton != null) NextPageButton.onClick.AddListener(OnNextPage);
        }

        // 供 Unity 按鈕綁定，透過事件系統開啟或關閉背包，確保 UI 狀態互斥。
        public void RequestOpenBag() => PlayerInfoUIEvents.InvokeOpenSouvenirBag();
        public void RequestCloseBag() => PlayerInfoUIEvents.InvokeCloseAll();

        public void OpenPage() => OpenBag();

        public void OpenBag()
        {
            if (MainPanel != null) MainPanel.SetActive(true);
            PlaySound(openSound);
            _currentSelectedSlot = null;
            ResetDetailDisplay();

            LoadBagItems();

            _currentPage = 0;
            RefreshPage();
        }

        public void ClosePage() => CloseBag();

        public void CloseBag()
        {
            _currentSelectedSlot = null;
            ResetDetailDisplay();
            if (MainPanel != null)
            {
                bool wasActive = MainPanel.activeSelf;
                MainPanel.SetActive(false);
                if (wasActive)
                    PlaySound(closeSound);
            }
        }

        private void LoadBagItems()
        {
            _bagItems.Clear();

            var bookData = DataManager.Instance.GetBookData();
            if (bookData == null) return;

            IReadOnlyList<string> achList;
            IReadOnlyList<string> splList;

            if (ShowCurrentRunOnly)
            {
                // 局內模式：成就紀念品使用 PlayerData.HoldAchievementSouvenirID 的本局快照。
                // 特殊紀念品仍使用 Book 存檔，因為特殊紀念品是跨局持有。
                var playerData = DataManager.Instance.CurrentPlayerData;
                achList = playerData?.HoldAchievementSouvenirID ?? (IReadOnlyList<string>)new List<string>();
                splList = bookData.UnLockSpecialSouvenirID ?? (IReadOnlyList<string>)new List<string>();
            }
            else
            {
                // 主選單模式：直接讀取 Book 存檔中的所有解鎖紀念品。
                achList = bookData.UnLockAchievementSouvenirID ?? (IReadOnlyList<string>)new List<string>();
                splList = bookData.UnLockSpecialSouvenirID ?? (IReadOnlyList<string>)new List<string>();
            }

            HashSet<string> addedIds = new HashSet<string>();

            // 1. 鑰匙紀念品固定優先顯示。
            string keyId = "Sou_key";
            if (achList.Contains(keyId) || splList.Contains(keyId) || SouvenirManager.Instance.IsOwned(keyId))
            {
                _bagItems.Add(new SouvenirBagItemData { SouvenirID = keyId, IsSpecial = true });
                addedIds.Add(keyId);
            }

            // 2. 特殊紀念品排在成就紀念品前面。
            foreach (var id in splList)
            {
                if (addedIds.Contains(id)) continue;
                _bagItems.Add(new SouvenirBagItemData { SouvenirID = id, IsSpecial = true });
                addedIds.Add(id);
            }

            // 3. 成就紀念品排在特殊紀念品後面。
            foreach (var id in achList)
            {
                if (addedIds.Contains(id)) continue;
                _bagItems.Add(new SouvenirBagItemData { SouvenirID = id, IsSpecial = false });
                addedIds.Add(id);
            }
        }

        private void RefreshPage()
        {
            if (_bagItems == null || _bagItems.Count == 0)
            {
                _currentSelectedSlot = null;
                for (int i = 0; i < _spawnedSlots.Count; i++) _spawnedSlots[i].gameObject.SetActive(false);
                if (PreviousPageButton != null) PreviousPageButton.interactable = false;
                if (NextPageButton != null) NextPageButton.interactable = false;
                ResetDetailDisplay();
                return;
            }

            int totalPages = Mathf.CeilToInt((float)_bagItems.Count / ItemsPerPage);

            if (_currentPage < 0) _currentPage = 0;
            if (_currentPage >= totalPages) _currentPage = totalPages - 1;

            int startIndex = _currentPage * ItemsPerPage;
            int endIndex = Mathf.Min(startIndex + ItemsPerPage, _bagItems.Count);
            int displayCount = endIndex - startIndex;

            AdjustSlotCount(displayCount);

            for (int i = 0; i < displayCount; i++)
            {
                var data = _bagItems[startIndex + i];

                _spawnedSlots[i].Setup(data, OnSlotClicked);
                _spawnedSlots[i].gameObject.SetActive(true);
            }

            for (int i = displayCount; i < _spawnedSlots.Count; i++)
            {
                _spawnedSlots[i].gameObject.SetActive(false);
            }

            if (displayCount > 0)
            {
                SelectSlot(_spawnedSlots[0], false);
            }

            if (PreviousPageButton != null) PreviousPageButton.interactable = _currentPage > 0;
            if (NextPageButton != null) NextPageButton.interactable = _currentPage < totalPages - 1;
        }

        private void ResetDetailDisplay()
        {
            if (DetailNameText != null) DetailNameText.text = string.Empty;
            if (DetailDescriptionText != null) DetailDescriptionText.text = string.Empty;
            if (DetailFunctionText != null) DetailFunctionText.text = string.Empty;

            if (DetailIcon != null)
            {
                DetailIcon.sprite = null;
                DetailIcon.color = Color.clear;
            }

            if (SpecialSouvenirDisplay != null)
            {
                SpecialSouvenirDisplay.SetActive(false);
            }

            if (InteractButton != null)
            {
                if (_currentInteractListener != null)
                {
                    InteractButton.onClick.RemoveListener(_currentInteractListener);
                    _currentInteractListener = null;
                }
                InteractButton.gameObject.SetActive(false);
            }

            if (InteractButtonText != null)
            {
                InteractButtonText.text = string.Empty;
            }
        }

        private void AdjustSlotCount(int targetCount)
        {
            while (_spawnedSlots.Count < targetCount)
            {
                if (SlotPrefab != null && SlotContainer != null)
                {
                    SouvenirBagSlot newSlot = Instantiate(SlotPrefab, SlotContainer);
                    _spawnedSlots.Add(newSlot);
                }
                else
                {
                    Debug.LogWarning("[SouvenirBagView] 未設定 SlotPrefab 或 SlotContainer。");
                    break;
                }
            }
        }

        private void OnSlotClicked(SouvenirBagSlot slot)
        {
            SelectSlot(slot, true);
        }

        private void SelectSlot(SouvenirBagSlot slot, bool playSound)
        {
            _currentSelectedSlot = slot;

            var data = slot.CurrentData;
            if (data == null) return;
            if (playSound)
                PlaySound(selectItemSound);

            if (SpecialSouvenirDisplay != null)
            {
                SpecialSouvenirDisplay.SetActive(data.IsSpecial);
            }

            ISouvenirBagView bagView = data.IsSpecial
                ? SouvenirManager.Instance.GetSpecialSouvenir(data.SouvenirID)
                : SouvenirManager.Instance.GetAchievementSouvenir(data.SouvenirID);

            if (bagView != null)
            {
                if (DetailNameText != null) DetailNameText.text = bagView.SouvenirName;
                if (DetailDescriptionText != null) DetailDescriptionText.text = bagView.SouvenirDescription;
                if (DetailFunctionText != null) DetailFunctionText.text = bagView.EffectName;
            }

            if (DetailIcon != null)
            {
                DetailIcon.color = Color.white;
                SpriteLoader.LoadSpriteAsync(data.SouvenirID, sprite =>
                {
                    if (DetailIcon != null && _currentSelectedSlot == slot)
                    {
                        DetailIcon.sprite = sprite != null ? sprite : slot.DefaultSprite;
                        SpriteLoader.AdjustImageScale(DetailIcon, TargetLongEdgeSize);
                    }
                });
            }

            // 處理 ISouvenirInteractive 互動按鈕。
            if (InteractButton != null)
            {
                InteractButton.gameObject.SetActive(false); // 預設隱藏。
                if (_currentInteractListener != null)
                {
                    InteractButton.onClick.RemoveListener(_currentInteractListener);
                    _currentInteractListener = null;
                }

                if (!ShowCurrentRunOnly) return;

                // 從 Manager 取得紀念品實例，確認是否支援互動功能。
                ISouvenirInteractive interactiveSouvenir = null;
                if (data.IsSpecial)
                {
                    interactiveSouvenir = SouvenirManager.Instance.GetSpecialSouvenir(data.SouvenirID) as ISouvenirInteractive;
                }
                else
                {
                    interactiveSouvenir = SouvenirManager.Instance.GetAchievementSouvenir(data.SouvenirID) as ISouvenirInteractive;
                }

                if (interactiveSouvenir != null
                    && interactiveSouvenir.HasInteraction
                    && interactiveSouvenir.CanShowInteractionButton())
                {
                    if (AuctionDayGuide.ShouldHideSouvenirUseButton(DataManager.Instance?.CurrentPlayerData))
                        return;

                    InteractButton.gameObject.SetActive(true);
                    if (InteractButtonText != null)
                    {
                        InteractButtonText.text = interactiveSouvenir.InteractionButtonText;
                    }

                    _currentInteractListener = () =>
                    {
                        bool success = interactiveSouvenir.OnInteraction();
                        PlaySound(success ? GetUseSuccessSound(data.SouvenirID) : useFailedSound);
                    };
                    InteractButton.onClick.AddListener(_currentInteractListener);
                }
            }
        }

        private void OnPreviousPage()
        {
            int previousPage = _currentPage;
            _currentPage--;
            RefreshPage();
            if (_currentPage != previousPage)
                PlaySound(switchPageSound);
        }

        private void OnNextPage()
        {
            int previousPage = _currentPage;
            _currentPage++;
            RefreshPage();
            if (_currentPage != previousPage)
                PlaySound(switchPageSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySfx(clip);
        }

        private AudioClip GetUseSuccessSound(string souvenirId)
        {
            if (souvenirId == "Sou_key" && souKeyUseSound != null)
            {
                return souKeyUseSound;
            }

            return useSound;
        }
    }
}
