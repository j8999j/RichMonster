using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Souvenir
{
    public class SouvenirBagView : MonoBehaviour
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
        public TextMeshProUGUI DetailFunctionText; // 成就功能 or 特殊解鎖條件
        public Image DetailIcon;
        public Button InteractButton;           // 互動按鈕 (查看/使用)
        public TextMeshProUGUI InteractButtonText; // 互動按鈕文字
        public float TargetLongEdgeSize = 150f;

        [Header("Settings")]
        public bool ShowInteractionButton = true;

        private List<SouvenirBagSlot> _spawnedSlots = new List<SouvenirBagSlot>();
        private List<SouvenirBagItemData> _bagItems = new List<SouvenirBagItemData>();
        private SouvenirBagSlot _currentSelectedSlot;

        private void Awake()
        {
            if (PreviousPageButton != null) PreviousPageButton.onClick.AddListener(OnPreviousPage);
            if (NextPageButton != null) NextPageButton.onClick.AddListener(OnNextPage);
        }

        public void OpenBag()
        {
            if (MainPanel != null) MainPanel.SetActive(true);
            _currentSelectedSlot = null;
            ResetDetailDisplay();

            LoadBagItems();

            _currentPage = 0;
            RefreshPage();
        }

        /// <summary>
        /// 設定能否顯示互動按鈕
        /// </summary>
        public void SetInteractionButtonVisible(bool visible)
        {
            ShowInteractionButton = visible;
            if (MainPanel != null && MainPanel.activeSelf && _currentSelectedSlot != null)
            {
                OnSlotClicked(_currentSelectedSlot);
            }
        }

        public void CloseBag()
        {
            _currentSelectedSlot = null;
            ResetDetailDisplay();
            if (MainPanel != null) MainPanel.SetActive(false);
        }

        private void LoadBagItems()
        {
            _bagItems.Clear();

            var bookData = DataManager.Instance.GetBookData();
            if (bookData == null) return;

            var unlockAchList = bookData.UnLockAchievementSouvenirID ?? new List<string>();
            var unlockSplList = bookData.UnLockSpecialSouvenirID ?? new List<string>();

            HashSet<string> addedIds = new HashSet<string>();

            // 1. 強制首位: Sou_key
            string keyId = "Sou_key";
            if (unlockAchList.Contains(keyId) || unlockSplList.Contains(keyId) || SouvenirManager.Instance.IsOwned(keyId))
            {
                _bagItems.Add(new SouvenirBagItemData { SouvenirID = keyId, IsSpecial = true });
                addedIds.Add(keyId);
            }

            // 2. 特殊紀念品前置
            foreach (var id in unlockSplList)
            {
                if (addedIds.Contains(id)) continue;
                _bagItems.Add(new SouvenirBagItemData { SouvenirID = id, IsSpecial = true });
                addedIds.Add(id);
            }

            // 3. 成就紀念品後置
            foreach (var id in unlockAchList)
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
                OnSlotClicked(_spawnedSlots[0]);
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

            if (InteractButton != null)
            {
                InteractButton.onClick.RemoveAllListeners();
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
            _currentSelectedSlot = slot;

            var data = slot.CurrentData;
            if (data == null) return;

            // 從 SouvenirManager 取得實例，透過 ISouvenirBagView 讀取顯示資訊
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

            // 處理 ISouvenirInteractive 互動按鈕
            if (InteractButton != null)
            {
                InteractButton.gameObject.SetActive(false); // 預設隱藏
                InteractButton.onClick.RemoveAllListeners();

                if (!ShowInteractionButton) return;

                // 從 Manager 取得這項紀念品的實例來確認是否實作介面
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
                    InteractButton.gameObject.SetActive(true);
                    if (InteractButtonText != null)
                    {
                        InteractButtonText.text = interactiveSouvenir.InteractionButtonText;
                    }

                    InteractButton.onClick.AddListener(() =>
                    {
                        interactiveSouvenir.OnInteraction();
                    });
                }
            }
        }

        private void OnPreviousPage()
        {
            _currentPage--;
            RefreshPage();
        }

        private void OnNextPage()
        {
            _currentPage++;
            RefreshPage();
        }
    }
}
