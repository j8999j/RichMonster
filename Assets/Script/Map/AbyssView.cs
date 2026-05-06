using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using TMPro;

public class AbyssView : MonoBehaviour
{
    [SerializeField] private GameObject BlackView;
    [SerializeField] private GameObject StartPanel;
    [SerializeField] private CanvasGroup LossPanel;
    [SerializeField] private CanvasGroup LeavePanel;
    [SerializeField] private GameObject GamePanel;
    [SerializeField] private GameObject IsPlayPanel;
    [SerializeField] private GameObject PlayerObj;
    [SerializeField] private GameObject EatHole;
    [SerializeField] private GameObject TreasureBox;
    [SerializeField] private GameObject NowLevel;
    [SerializeField] private Button LeaveButton;
    [SerializeField] private Button ContinueButton;
    [SerializeField] private Button MoveRightButton;
    [SerializeField] private Button MoveLeftButton;
    [SerializeField] private Button CloseButton;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip dropItemSfx;
    [SerializeField] private AudioClip openTreasureSfx;
    [SerializeField] private AudioClip nextStepSfx;
    [SerializeField] private AudioClip exploreFailedSfx;
    [SerializeField] private AudioClip continueExploreSfx;
    [SerializeField] private AudioClip leaveWithRewardSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolumeScale = 1f;

    public event Action OnContinueClicked;
    public event Action OnLeaveClicked;
    public event Action OnFail;
    public event Action OnCloseClicked;

    [Header("點位設定（由上到下各4個，Index 0~3）")]
    [SerializeField] private RectTransform[] LeftPoints;   // L0~L3
    [SerializeField] private RectTransform[] RightPoints;  // R0~R3
    [SerializeField] private RectTransform   StartPoint;   // 頂部中央起始點
    [SerializeField] private RectTransform   EndRightPoint; // 底部右側結束點
    [SerializeField] private RectTransform   EndLeftPoint; // 底部左側結束點
    [SerializeField] private RectTransform[] NowLevelPoints; // 目前層數顯示點
    [Header("已獲得獎勵顯示")]
    [SerializeField] private AbyssSlot RewardItemPrefab;
    [SerializeField] private Transform  RewardContainer;
    [SerializeField] private Sprite GoldSprite;

    [Header("入場背包顯示")]
    [SerializeField] private GameObject NotionTitle;
    [SerializeField] private TradeSlot TradeSlotPrefab;
    [SerializeField] private Transform BagContainer;
    [SerializeField] private RectTransform DropZone;
    [SerializeField] private Image CenterItemImage; // 暫存拖放圖片做動畫用

    public event Action<Item> OnItemDroppedToStart;
    private System.Collections.Generic.List<TradeSlot> _activeSlots = new System.Collections.Generic.List<TradeSlot>();

    [Header("Tooltip 設定")]
    [SerializeField] private GameObject TooltipPanel;
    [SerializeField] private TextMeshProUGUI TooltipText;
    [SerializeField] private Vector2 TooltipOffset = new Vector2(15f, -15f);

    [Header("跳躍設定")]
    [SerializeField] private float jumpHeight   = 160f;
    [SerializeField] private float jumpDuration = 0.65f;
    [SerializeField] private int   arcSteps     = 40;      // 拋物線取樣點數

    // ── 狀態 ─────────────────────────────────────────
    private enum Side { Center, Left, Right }
    private Side _currentSide  = Side.Center;
    private int  _currentIndex = -1;   // -1 = 起始點，0~3 = 各排
    private bool _isMoving     = false;

    private RectTransform _playerRect;
    private Vector3       _originalScale;
    
    private bool _isTooltipActive = false;

    // 將收到的獎勵先暫存，直到抵達結束點寶箱動畫後才顯示
    private struct PendingReward
    {
        public GameSystem.AbyssRewardType Type;
        public string ItemId;
        public int GoldAmount;
    }
    private System.Collections.Generic.List<PendingReward> _pendingRewards = new System.Collections.Generic.List<PendingReward>();
    
    // 是否為必定安全（無吃洞）的樓層
    private bool _isSafeLayer = true;
    private bool _isFinalLayer = false;

    // ─────────────────────────────────────────────────
    private int _testLayer = 1; // 獨立測試用層數

    private void Update()
    {
        if (_isTooltipActive && TooltipPanel != null)
        {
            Vector2 mousePos = Input.mousePosition;
            TooltipPanel.transform.position = mousePos + TooltipOffset;
        }
    }

    private void Awake()
    {
        _playerRect    = PlayerObj.GetComponent<RectTransform>();
        _originalScale = PlayerObj.transform.localScale;

        if (CloseButton != null)
        {
            CloseButton.onClick.AddListener(() => 
            {
                OnCloseClicked?.Invoke();
                if (OnCloseClicked == null)
                {
                    Close();
                }
            });
        }

        ContinueButton .onClick.AddListener(() => 
        {
            PlaySfx(continueExploreSfx);
            OnContinueClicked?.Invoke();
            
            // 測試用：如果沒有外部綁定，直接播放進入下一層的視覺表現
            if (OnContinueClicked == null)
            {
                _testLayer++;
                if (_testLayer > 5) _testLayer = 1;
                ProceedToNextLayer(_testLayer, true);
            }
        });

        LeaveButton    .onClick.AddListener(() => 
        {
            PlaySfx(leaveWithRewardSfx);
            OnLeaveClicked?.Invoke();
            
            if (OnLeaveClicked == null)
            {
                Close();
            }
        });

        MoveRightButton.onClick.AddListener(OnMoveRight);
        MoveLeftButton .onClick.AddListener(OnMoveLeft);
    }

    // ── 公開介面 ──────────────────────────────────────
    public void Open(bool alreadyPlayedToday = false)
    {
        IsPlayPanel.SetActive(false);
        StartPanel.SetActive(true);
        GamePanel.SetActive(false);
        if (CloseButton != null) CloseButton.gameObject.SetActive(true);
        
        if (alreadyPlayedToday)
        {
            ClearBagItems();
        }
        else
        {
            RefreshBagItems();
        }
    }

    public void ClearBagItems()
    {
        if (_activeSlots != null)
        {
            foreach (var slot in _activeSlots)
            {
                if (slot != null && slot.gameObject != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _activeSlots.Clear();
        }
    }
    public void IsPlayView()
    {
        NotionTitle.SetActive(false);
        IsPlayPanel.SetActive(true);
    }

    public void RefreshBagItems()
    {
        if (DataManager.Instance == null || BagContainer == null || TradeSlotPrefab == null) return;

        var allItems = DataManager.Instance.CurrentPlayerData?.InventoryItems;
        if (allItems == null) return;

        // 篩選僅限人界物品
        var items = new System.Collections.Generic.List<Item>();
        foreach (var item in allItems)
        {
            var def = DataManager.Instance.GetItemById(item.ItemId);
            if (def != null && def.World == ItemWorld.Human)
            {
                items.Add(item);
            }
        }

        // 生成或重複使用 UI
        while (_activeSlots.Count < items.Count)
        {
            TradeSlot newSlot = Instantiate(TradeSlotPrefab, BagContainer);
            newSlot.OnDragEnded += OnEndDrag;
            _activeSlots.Add(newSlot);
        }

        // 填寫資料
        for (int i = 0; i < items.Count; i++)
        {
            _activeSlots[i].Setup(items[i], null);
            _activeSlots[i].gameObject.SetActive(true);
        }

        // 隱藏多餘
        for (int i = items.Count; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].gameObject.SetActive(false);
        }
    }

    private void OnEndDrag(TradeSlot slot, UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (slot == null || slot._currentData == null) return;

        if (DropZone != null && RectTransformUtility.RectangleContainsScreenPoint(DropZone, eventData.position, eventData.pressEventCamera))
        {
            PlaySfx(dropItemSfx);

            // 開始播放吸入動畫
            if (CenterItemImage != null && slot._targetImage != null)
            {
                slot.gameObject.SetActive(false); // 隱藏原本 UI

                CenterItemImage.sprite = slot._targetImage.sprite;
                CenterItemImage.rectTransform.sizeDelta = slot._targetImage.rectTransform.sizeDelta;
                CenterItemImage.rectTransform.position = eventData.position; // 從滑鼠游標位置開始
                CenterItemImage.rectTransform.localScale = Vector3.one;
                CenterItemImage.rectTransform.localRotation = Quaternion.identity;
                CenterItemImage.color = Color.white;
                CenterItemImage.gameObject.SetActive(true);

                // 播放旋轉並進入洞內的設定
                CenterItemImage.rectTransform.DOMove(DropZone.position, 0.5f); // 吸向洞口居中
                CenterItemImage.rectTransform.DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360);
                CenterItemImage.rectTransform.DOScale(Vector3.zero, 0.5f);
                CenterItemImage.DOFade(0, 0.5f).OnComplete(() =>
                {
                     CenterItemImage.gameObject.SetActive(false);
                     PlayTransitionToGameplay(slot._currentData);
                });
            }
            else
            {
                PlayTransitionToGameplay(slot._currentData);
            }
        }
    }

    private void PlayTransitionToGameplay(Item targetItem)
    {
        if (BlackView != null)
        {
            FadeIn(BlackView, 0.5f, () => 
            {
                OnItemDroppedToStart?.Invoke(targetItem);
                if (OnItemDroppedToStart == null) StartGameplay();
                
                FadeOut(BlackView, 0.5f);
            });
        }
        else
        {
            OnItemDroppedToStart?.Invoke(targetItem);
            if (OnItemDroppedToStart == null) StartGameplay();
        }
    }

    public void Close()
    {
        StartPanel.SetActive(false);
        GamePanel .SetActive(false);
    }

    public void StartGameplay()
    {
        StartPanel.SetActive(false);
        GamePanel .SetActive(true);
        _isFinalLayer = false;
        SetLayer(_testLayer); // 給純視覺測試用的初始化
        ResetPlayer();
    }

    private void ResetPlayer()
    {
        DOTween.Kill(_playerRect);
        _isMoving     = false;
        _currentSide  = Side.Center;
        _currentIndex = -1;
        _playerRect.anchoredPosition   = StartPoint.anchoredPosition;
        PlayerObj.transform.localScale = _originalScale;
        PlayerObj.transform.localRotation = Quaternion.identity;
        
        CanvasGroup cg = PlayerObj.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        // 重新顯示所有點位
        foreach (var p in LeftPoints)  if (p != null) p.gameObject.SetActive(true);
        foreach (var p in RightPoints) if (p != null) p.gameObject.SetActive(true);
        
        MoveRightButton.gameObject.SetActive(true);
        MoveLeftButton.gameObject.SetActive(true);
        ContinueButton.gameObject.SetActive(false);
        LeaveButton.gameObject.SetActive(false);
        
        RefreshButtons();
    }

    /// <summary>
    /// 更新指標位置
    /// </summary>
    public void SetLayer(int layer)
    {
        int index = Mathf.Clamp(layer - 1, 0, NowLevelPoints.Length - 1);
        if (NowLevel != null && NowLevelPoints != null && NowLevelPoints.Length > 0 && NowLevelPoints[index] != null)
        {
            RectTransform nowLevelRect = NowLevel.GetComponent<RectTransform>();
            nowLevelRect.anchoredPosition = new Vector2(nowLevelRect.anchoredPosition.x, NowLevelPoints[index].anchoredPosition.y);
        }
    }

    /// <summary>
    /// 開始切換至下一層的動畫 (黑畫面過場)
    /// </summary>
    public void ProceedToNextLayer(int newLayer, bool isSafe = true)
    {
        _isSafeLayer = isSafe;
        if (BlackView != null)
        {
            FadeIn(BlackView, 0.5f, () => 
            {
                SetLayer(newLayer);
                if (TreasureBox != null) TreasureBox.SetActive(false);
                ResetPlayer();
                FadeOut(BlackView, 0.5f);
            });
        }
        else
        {
            SetLayer(newLayer);
            if (TreasureBox != null) TreasureBox.SetActive(false);
            ResetPlayer();
        }
    }

    /// <summary>
    /// 新增獲得的獎勵顯示 (先暫存)
    /// </summary>
    public void AddRewardDisplay(GameSystem.AbyssRewardType type, string itemId, int goldAmount)
    {
        _pendingRewards.Add(new PendingReward { Type = type, ItemId = itemId, GoldAmount = goldAmount });
    }

    public void SetFinalLayer(bool isFinalLayer)
    {
        _isFinalLayer = isFinalLayer;
    }

    /// <summary>
    /// 抵達底部後實際把暫存的獎勵產生到畫面上
    /// </summary>
    private void ShowPendingRewards()
    {
        if (RewardItemPrefab == null || RewardContainer == null) return;
        
        foreach (var reward in _pendingRewards)
        {
            AbyssSlot slot = Instantiate(RewardItemPrefab, RewardContainer);
            if (reward.Type == GameSystem.AbyssRewardType.MonsterGold)
            {
                slot.SetupGold(reward.GoldAmount, GoldSprite, ShowTooltip, HideTooltip);
            }
            else
            {
                slot.SetupItem(reward.ItemId, ShowTooltip, HideTooltip);
            }
        }
        
        _pendingRewards.Clear();
    }

    public void ClearRewards()
    {
        _pendingRewards.Clear();
        if (RewardContainer == null) return;
        foreach (Transform child in RewardContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void ShowTooltip(string content)
    {
        if (TooltipPanel != null && TooltipText != null)
        {
            TooltipText.text = content;
            TooltipPanel.SetActive(true);
            _isTooltipActive = true;
            TooltipPanel.transform.position = (Vector2)Input.mousePosition + TooltipOffset;
        }
    }

    public void HideTooltip()
    {
        if (TooltipPanel != null) TooltipPanel.SetActive(false);
        _isTooltipActive = false;
    }

    // ── 移動邏輯（index 只能遞增）────────────────────
    private void OnMoveRight()
    {
        if (_isMoving) return;

        // 下一格 index 必須 > currentIndex
        int nextIndex = _currentIndex + 1;
        if (nextIndex >= RightPoints.Length) return;

        PlaySfx(nextStepSfx);

        // 跳躍時面向右；落地後面向左（等待往左跳）
        SetFacing(facingRight: true);

        JumpTo(RightPoints[nextIndex].anchoredPosition, () =>
        {
            _currentSide  = Side.Right;
            _currentIndex = nextIndex;
            SetFacing(facingRight: false);  // 落地後轉左
            
            HandleMoveResult();
        });
    }

    private void OnMoveLeft()
    {
        if (_isMoving) return;

        int nextIndex = _currentIndex + 1;
        if (nextIndex >= LeftPoints.Length) return;

        PlaySfx(nextStepSfx);

        // 跳躍時面向左；落地後面向右（等待往右跳）
        SetFacing(facingRight: false);

        JumpTo(LeftPoints[nextIndex].anchoredPosition, () =>
        {
            _currentSide  = Side.Left;
            _currentIndex = nextIndex;
            SetFacing(facingRight: true);  // 落地後轉右
            
            HandleMoveResult();
        });
    }

    private void HandleMoveResult()
    {
        // 取得當前走過的點
        RectTransform currentPoint = null;
        if (_currentIndex >= 0)
        {
            if (_currentSide == Side.Left && _currentIndex < LeftPoints.Length)
                currentPoint = LeftPoints[_currentIndex];
            else if (_currentSide == Side.Right && _currentIndex < RightPoints.Length)
                currentPoint = RightPoints[_currentIndex];
        }

        // 決定是否吃洞
        bool triggerEatHole = false;
        
        if (!_isSafeLayer)
        {
            if (_currentIndex == 3)
            {
                // 到達最底部仍為危險層，則必定吃洞
                triggerEatHole = true;
            }
            else
            {
                // 每次移動 50% 機率
                if (UnityEngine.Random.value <= 0.5f)
                {
                    triggerEatHole = true;
                }
            }
        }

        if (triggerEatHole)
        {
            PlaySfx(exploreFailedSfx);

            _isMoving = true; // 鎖死按鈕
            EatHole.SetActive(true);
            
            if (currentPoint != null) currentPoint.gameObject.SetActive(false); // 觸發吃洞時隱藏該點位
            
            RectTransform holeRect = EatHole.GetComponent<RectTransform>();
            if (holeRect != null) holeRect.position = _playerRect.position;

            // 動畫延遲 0.5 秒後持續 1 秒
            DOVirtual.DelayedCall(0.5f, () => 
            {
                _playerRect.DORotate(new Vector3(0, 0, 180), 1.0f);
                _playerRect.DOScale(Vector3.zero, 1.0f);
                
                CanvasGroup cg = PlayerObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = PlayerObj.AddComponent<CanvasGroup>();
                cg.DOFade(0f, 1.0f);
            });

            DOVirtual.DelayedCall(1.5f, () => // 0.5 + 1.0
            {
                if (LossPanel != null)
                {
                    FadeIn(LossPanel.gameObject, 0.5f, () =>
                    {
                        Close(); // 淡入完直接調用關閉

                        // 停留 1.5 秒後淡出 LossPanel
                        DOVirtual.DelayedCall(1.5f, () =>
                        {
                            FadeOut(LossPanel.gameObject, 0.5f);
                        });
                    });
                }
                OnFail?.Invoke(); // 通知外部失敗
            });
            return;
        }

        // 2. 抵達 L3 / R3 (Index 3)，觸發自動往結束點跳躍
        if (_currentIndex == 3)
        {
            RectTransform endPoint = (_currentSide == Side.Left) ? EndLeftPoint : EndRightPoint;
            
            JumpTo(endPoint.anchoredPosition, () =>
            {
                _isMoving = true; // 鎖死
                if (TreasureBox != null) TreasureBox.SetActive(true);
                
                DOVirtual.DelayedCall(1.0f, () =>
                {
                    // 在寶箱動畫結束後，才將暫存的獎勵真正顯示到格子上
                    PlaySfx(openTreasureSfx);
                    ShowPendingRewards();

                    // 顯示 Continue 與 Leave，隱藏 Move 按鈕
                    MoveRightButton.gameObject.SetActive(false);
                    MoveLeftButton.gameObject.SetActive(false);
                    ContinueButton.gameObject.SetActive(!_isFinalLayer);
                    LeaveButton.gameObject.SetActive(true);
                    if (CloseButton != null) CloseButton.gameObject.SetActive(!_isFinalLayer);
                });
            });
            return;
        }

        // 3. 繼續探索
        RefreshButtons();
    }

    // ── 淡入淡出效果 ───────────────────────────────────
    private void FadeIn(GameObject view, float duration, System.Action onComplete = null)
    {
        if (view == null) return;
        view.SetActive(true);
        CanvasGroup cg = view.GetComponent<CanvasGroup>();
        if (cg == null) cg = view.AddComponent<CanvasGroup>();
        
        cg.alpha = 0f;
        cg.DOFade(1f, duration).OnComplete(() => onComplete?.Invoke());
    }

    private void FadeOut(GameObject view, float duration, System.Action onComplete = null)
    {
        if (view == null) return;
        CanvasGroup cg = view.GetComponent<CanvasGroup>();
        if (cg == null) cg = view.AddComponent<CanvasGroup>();
        
        cg.alpha = 1f;
        cg.DOFade(0f, duration).OnComplete(() => 
        {
            view.SetActive(false);
            onComplete?.Invoke();
        });
    }

    // ── 真實拋物線路徑（DOPath 多點）──────────────────
    /// <summary>
    /// 用貝塞爾公式取樣 arcSteps 個點形成拋物線，
    /// 避免 X / Y 分拆造成的非物理弧形
    /// </summary>
    private void JumpTo(Vector2 target, System.Action onComplete)
    {
        _isMoving = true;
        DOTween.Kill(_playerRect);

        Vector2 origin = _playerRect.anchoredPosition;

        // 控制點：兩點中間水平位置、高於兩端的頂點
        Vector2 control = new Vector2(
            (origin.x + target.x) * 0.5f,
            Mathf.Max(origin.y, target.y) + jumpHeight
        );

        // 取樣拋物線點（二次貝塞爾）
        Vector3[] waypoints = new Vector3[arcSteps];
        for (int i = 0; i < arcSteps; i++)
        {
            float t = (i + 1f) / arcSteps;          // 不含起點，含終點
            Vector2 pt = Mathf.Pow(1 - t, 2) * origin
                       + 2 * (1 - t) * t * control
                       + Mathf.Pow(t, 2) * target;
            waypoints[i] = new Vector3(pt.x, pt.y, 0f);
        }

        _playerRect
            .DOLocalPath(waypoints, jumpDuration, PathType.Linear)
            .SetEase(Ease.InOutSine)               // 起點與終點稍慢，中段稍快
            .SetOptions(false)
            .OnComplete(() =>
            {
                // 強制對齊終點（消除浮點誤差）
                _playerRect.anchoredPosition = target;
                _isMoving = false;
                onComplete?.Invoke();
            });
    }

    // ── 角色面向（水平翻轉）─────────────────────────────
    /// <summary>
    /// 若原始 Sprite 面向左請將下方正負對調
    /// </summary>
    private void SetFacing(bool facingRight)
    {
        Vector3 s = _originalScale;
        PlayerObj.transform.localScale = new Vector3(
            facingRight ? -Mathf.Abs(s.x) : Mathf.Abs(s.x),  // 翻轉修正（原始朝左）
            s.y, s.z
        );
    }

    // ── 按鈕狀態 ─────────────────────────────────────
    private void RefreshButtons()
    {
        int next = _currentIndex + 1;
        MoveRightButton.interactable = next < RightPoints.Length;
        MoveLeftButton .interactable = next < LeftPoints.Length;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || GameSystem.AudioManager.Instance == null)
        {
            return;
        }

        GameSystem.AudioManager.Instance.PlaySfx(clip, sfxVolumeScale);
    }
}
