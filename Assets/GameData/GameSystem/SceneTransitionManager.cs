using UnityEngine;
using System;
using DG.Tweening;
using UnityEngine.UI;
namespace GameSystem
{
    /// <summary>
    /// 場景轉換管理器
    /// 負責處理遊戲中的場景轉換邏輯
    /// </summary>
    public class SceneTransitionManager : Singleton<SceneTransitionManager>
    {
        private AddressableSceneLoader _sceneLoader;
        private DataManager _dataManager;

        // 場景名稱常數
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_HUMAN = "HumanScene";
        public const string SCENE_MONSTER = "MonsterWorldScene";
        public const string SCENE_TRADE = "TradeScene";

        // 場景轉換事件
        public event Action<string> OnSceneLoadStart;
        public event Action<string> OnSceneLoadComplete;

        // 當前場景
        public string CurrentScene { get; private set; }
        //場景視覺
        [SerializeField] private CanvasGroup fadeCanvasGroup; // 整體淡入淡出
        [SerializeField] private GameObject loadingContent;
        [SerializeField] private Image BlackImage;
        [SerializeField] private Image LogoImage;
        [SerializeField] private Image LoadingImage;
        [Header("設定")]
        [SerializeField] private float fadeDuration = 0.3f;
        protected override void Awake()
        {
            base.Awake();
            _sceneLoader = AddressableSceneLoader.Instance;
            _dataManager = DataManager.Instance;
        }

        /// <summary>
        /// 載入指定場景
        /// </summary>
        /// <param name="sceneName">場景名稱</param>
        /// <param name="onComplete">場景載入完成後的回調</param>
        public void LoadScene(string sceneName, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneTransitionManager] 場景名稱不能為空");
                return;
            }
            Sequence enterSeq = DOTween.Sequence();

            // A. 阻擋點擊
            enterSeq.AppendCallback(() => fadeCanvasGroup.blocksRaycasts = true);
            // B. 畫面變黑
            enterSeq.Append(BlackImage.DOFade(1f, fadeDuration));
            enterSeq.OnComplete(() =>
            {
                OnSceneLoadStart?.Invoke(sceneName);

                _sceneLoader.LoadScene(sceneName, (success) =>
                {
                    CurrentScene = sceneName;
                    OnSceneLoadComplete?.Invoke(sceneName);
                    onComplete?.Invoke();
                    Sequence EndSeq = DOTween.Sequence();
                    EndSeq.AppendInterval(0.3f);
                    // A. 阻擋點擊
                    EndSeq.AppendCallback(() => fadeCanvasGroup.blocksRaycasts = false);
                    // B. 畫面變黑
                    EndSeq.Append(BlackImage.DOFade(0f, fadeDuration));
                    Debug.Log($"場景載入成功: {sceneName}");
                });
            });
        }
        /// <summary>
        /// 返回主選單
        /// </summary>
        /// <param name="onComplete">場景載入完成後的回調</param>
        public void GoToMainMenu(Action onComplete = null)
        {
            LoadScene(SCENE_MAIN_MENU, onComplete);
        }

        /// <summary>
        /// 進入人類場景（白天）
        /// </summary>
        /// <param name="onComplete">場景載入完成後的回調</param>
        public void GoToHumanScene(Action onComplete = null)
        {
            _dataManager.ModifyCurrentDayPhase(DayPhase.HumanDay);
            LoadScene(SCENE_HUMAN, onComplete);
        }

        /// <summary>
        /// 進入妖怪場景（夜晚）
        /// </summary>
        /// <param name="onComplete">場景載入完成後的回調</param>
        public void GoToMonsterScene(Action onComplete = null)
        {
            _dataManager.ModifyCurrentDayPhase(DayPhase.Night);
            LoadScene(SCENE_MONSTER, onComplete);
        }

        /// <summary>
        /// 進入交易場景
        /// </summary>
        /// <param name="onComplete">場景載入完成後的回調</param>
        public void GoToTradeScene(Action onComplete = null)
        {
            _dataManager.ModifyCurrentDayPhase(DayPhase.NightTrade);
            LoadScene(SCENE_TRADE, onComplete);
        }

        /// <summary>
        /// 根據遊戲階段自動切換場景
        /// </summary>
        /// <param name="dayPhase">遊戲階段</param>
        /// <param name="onComplete">場景載入完成後的回調</param>
        public void GoToSceneByPhase(DayPhase dayPhase, Action onComplete = null)
        {
            switch (dayPhase)
            {
                case DayPhase.HumanDay:
                    LoadScene(SCENE_HUMAN, onComplete);
                    break;
                case DayPhase.Night:
                    LoadScene(SCENE_MONSTER, onComplete);
                    break;
                case DayPhase.NightTrade:
                    LoadScene(SCENE_MONSTER, onComplete);
                    break;
                default:
                    Debug.LogWarning($"[SceneTransitionManager] 未知的遊戲階段: {dayPhase}");
                    onComplete?.Invoke(); // 即使失敗也要呼叫回調避免卡住
                    break;
            }
        }
    }
}
