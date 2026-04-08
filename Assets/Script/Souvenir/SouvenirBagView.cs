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
        public float TargetLongEdgeSize = 150f;

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

            LoadBagItems();

            _currentPage = 0;
            RefreshPage();
        }

        public void CloseBag()
        {
            if (MainPanel != null) MainPanel.SetActive(false);
        }

        private void LoadBagItems()
        {
            _bagItems.Clear();

            var bookData = DataManager.Instance.GetBookData();
            if (bookData == null) return;

            var achieveDataDict = DataManager.Instance.AchievementSouvenirDict;
            var specialDataDict = DataManager.Instance.SpecialSouvenirDict;
            
            var unlockAchList = bookData.UnLockAchievementSouvenirID ?? new List<string>();
            var unlockSplList = bookData.UnLockSpecialSouvenirID ?? new List<string>();

            HashSet<string> addedIds = new HashSet<string>();

            // 1. 強制首位: Sou_key
            string keyId = "Sou_key";
            if (unlockAchList.Contains(keyId) || unlockSplList.Contains(keyId) || SouvenirManager.Instance.IsOwned(keyId))
            {
                if (specialDataDict != null && specialDataDict.TryGetValue(keyId, out var splData))
                {
                    _bagItems.Add(new SouvenirBagItemData
                    {
                        SouvenirID = splData.SouvenirID,
                        SouvenirName = splData.SouvenirName,
                        SouvenirDescription = splData.SouvenirDescription,
                        FunctionOrConditionDesc = splData.SouvenirCondition,
                        IsSpecial = true
                    });
                    addedIds.Add(keyId);
                }
            }

            // 2. 特殊紀念品前置
            foreach (var id in unlockSplList)
            {
                if (addedIds.Contains(id)) continue;
                
                if (specialDataDict != null && specialDataDict.TryGetValue(id, out var splData))
                {
                    _bagItems.Add(new SouvenirBagItemData
                    {
                        SouvenirID = splData.SouvenirID,
                        SouvenirName = splData.SouvenirName,
                        SouvenirDescription = splData.SouvenirDescription,
                        FunctionOrConditionDesc = splData.SouvenirCondition, // 使用解鎖條件作為功能描述
                        IsSpecial = true
                    });
                    addedIds.Add(id);
                }
            }

            // 3. 成就紀念品後置
            foreach (var id in unlockAchList)
            {
                if (addedIds.Contains(id)) continue;

                if (achieveDataDict != null && achieveDataDict.TryGetValue(id, out var achData))
                {
                    _bagItems.Add(new SouvenirBagItemData
                    {
                        SouvenirID = achData.SouvenirID,
                        SouvenirName = achData.SouvenirName,
                        SouvenirDescription = achData.SouvenirDescription,
                        FunctionOrConditionDesc = achData.SouvenirFunctionDescription,
                        IsSpecial = false
                    });
                    addedIds.Add(id);
                }
            }
        }

        private void RefreshPage()
        {
            if (_bagItems == null || _bagItems.Count == 0)
            {
                for (int i = 0; i < _spawnedSlots.Count; i++) _spawnedSlots[i].gameObject.SetActive(false);
                if (PreviousPageButton != null) PreviousPageButton.interactable = false;
                if (NextPageButton != null) NextPageButton.interactable = false;
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

            if (DetailNameText != null) DetailNameText.text = data.SouvenirName;
            if (DetailDescriptionText != null) DetailDescriptionText.text = data.SouvenirDescription;
            if (DetailFunctionText != null) DetailFunctionText.text = data.FunctionOrConditionDesc;

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
