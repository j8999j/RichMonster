using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Player;

using Cinemachine;
using Talksystem;

namespace GameSystem
{
    public class GameManager : Singleton<GameManager>
    {
        private DataManager dataManager;
        private SceneTransitionManager sceneTransitionManager;
        public SaveManager saveManager;
        public TalkSystem talkSystem;
        public GameFlow gameFlow { private set; get; }
        public GameObject PlayerPrefab;
        private PlayerController PlayerController;
        private GameObject Player;
        public Transform PlayerSpawnPoint;
        [SerializeField] private CinemachineVirtualCamera virtualCamera;

        // 透過 SceneTransitionManager 存取場景轉換功能
        public SceneTransitionManager SceneManager => sceneTransitionManager;

        protected override void Awake()
        {
            base.Awake();
            saveManager = SaveManager.Instance;
            dataManager = DataManager.Instance;
            sceneTransitionManager = GetComponent<SceneTransitionManager>();
            // 訂閱場景載入完成事件
            sceneTransitionManager.OnSceneLoadComplete += OnSceneLoadComplete;
        }

        protected override void OnDestroy()
        {
            if (sceneTransitionManager != null)
            {
                sceneTransitionManager.OnSceneLoadComplete -= OnSceneLoadComplete;
            }
            base.OnDestroy();
        }

        /// <summary>
        /// 場景載入完成後的回調
        /// </summary>
        private void OnSceneLoadComplete(string sceneName)
        {
            // MainMenu 不需要初始化玩家
            if (sceneName == SceneTransitionManager.SCENE_MAIN_MENU
                || sceneName == SceneTransitionManager.SCENE_END_STORY)
                return;
            InitializePlayerInScene(sceneName);
        }

        /// <summary>
        /// 在場景中初始化玩家
        /// </summary>
        private void InitializePlayerInScene(string sceneName)
        {
            SetPlayer();
            SetPlayerPosition(new Vector3(0, -2, 0));
            ClearAllLocks();
            SetCameraFollowPlayer();
            ClearCameraHorizontalBounds();
            if (sceneName == SceneTransitionManager.SCENE_MONSTER)
            {
                PlayerController.SetIsNight(true);
            }
        }

        private async void Start()
        {
            await dataManager.WhenInitialized();
            sceneTransitionManager.LoadScene(SceneTransitionManager.SCENE_MAIN_MENU);
        }

        public async void StartNewGame()
        {
            // 取得下一個可用的存檔欄位
            int slot = saveManager.GetNextAvailableSlot();

            // 取得初始玩家資料並生成新的隨機種子
            var newPlayerData = dataManager.InitialPlayerData;
            newPlayerData.MasterSeed = Random.Range(1, int.MaxValue);

            // 記錄目前持有的紀念品
            var bookData = dataManager.GetBookData();
            if (bookData != null)
            {
                newPlayerData.HoldAchievementSouvenirID = new List<string>(bookData.UnLockAchievementSouvenirID ?? new List<string>());
            }

            // 使用新種子的玩家資料創建新遊戲
            newPlayerData.HasReachedEnding = false;
            newPlayerData.ReachedEndingType = EndingType.None;
            newPlayerData.HasPaidGuaranteeDeposit = false;
            newPlayerData.HasPaidAuctionEntryFee = false;
            dataManager.SetCurrentPlayer(newPlayerData);
            await dataManager.SaveCurrentPlayerAsync(slot);
            Debug.Log($"[GameManager] 開始新遊戲，存檔欄位: {slot}, 種子: {newPlayerData.MasterSeed}");
            InitializeGame(slot);
        }

        /// <summary>
        /// 根據存檔欄位初始化遊戲
        /// </summary>
        public void InitializeGame(int slot)
        {
            var playerData = dataManager.CurrentPlayerData as PlayerData;
            if (playerData == null)
            {
                Debug.LogError("[GameManager] 無法取得玩家資料");
                return;
            }
            if (playerData.HasReachedEnding)
            {
                sceneTransitionManager.GoToEndStoryScene();
                return;
            }

            // 以本局 PlayerData.HoldAchievementSouvenirID 為準重新快照成就紀念品所有權
            Souvenir.SouvenirManager.Instance.ResnapshotForCurrentGame();
            gameFlow = new GameFlow(playerData, slot);
            sceneTransitionManager.GoToSceneByPhase(playerData.PlayingStatus, () =>
            {
                // 場景載入完成後才執行
                DataManager.Instance.ModifyCurrentDay(playerData.DaysPlayed);
                GameFlowEvents.InvokeDayPhaseChanged(playerData.PlayingStatus);
                GuaranteeDepositGuide.Refresh();
                AuctionEntryFeeGuide.Refresh();
                AuctionDayGuide.Refresh();
                Souvenir.SouvenirManager.Instance.ApplyAllStartEffects();
                gameFlow.StartTutorial();
                // 玩家初始化已由 OnSceneLoadComplete 事件處理
            });
        }

        #region ControllerPlayer
        public void SetPlayer()
        {
            Player = Instantiate(PlayerPrefab);
            PlayerController = Player.GetComponent<PlayerController>();
        }
        public void SetPlayerPosition(Vector3 position)
        {
            if (PlayerController != null)
            {
                PlayerController.TeleportTo(position);
                return;
            }

            if (Player != null)
            {
                Player.transform.position = position;
            }
        }
        private readonly HashSet<string> _moveLockSources = new HashSet<string>();
        private readonly HashSet<string> _interactLockSources = new HashSet<string>();

        public bool GetPlayerMove()
        {
            return _moveLockSources.Count == 0;
        }

        public bool IsPlayerMoveLocked(string source)
        {
            return _moveLockSources.Contains(source);
        }

        public void LockPlayerMove(string source)
        {
            if (PlayerController == null) return;
            _moveLockSources.Add(source);
            PlayerController.SetCanMove(false);
        }

        public void UnlockPlayerMove(string source)
        {
            if (PlayerController == null) return;
            _moveLockSources.Remove(source);
            if (_moveLockSources.Count == 0)
            {
                PlayerController.SetCanMove(true);
            }
        }

        public void LockPlayerInteract(string source)
        {
            if (PlayerController == null) return;
            _interactLockSources.Add(source);
            PlayerController.SetCanInteract(false);
        }

        public void UnlockPlayerInteract(string source)
        {
            if (PlayerController == null) return;
            _interactLockSources.Remove(source);
            if (_interactLockSources.Count == 0)
            {
                PlayerController.SetCanInteract(true);
            }
        }

        public void ClearAllLocks()
        {
            _moveLockSources.Clear();
            _interactLockSources.Clear();
            if (PlayerController != null)
            {
                PlayerController.SetCanMove(true);
                PlayerController.SetCanInteract(true);
            }
        }

        /// <summary>
        /// 設定攝影機跟隨玩家
        /// </summary>
        public void SetCameraFollowPlayer()
        {
            if (virtualCamera == null)
            {
                virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            }

            if (virtualCamera != null && Player != null)
            {
                virtualCamera.Follow = Player.transform;
                virtualCamera.LookAt = Player.transform;
            }
            else
            {
                Debug.LogWarning("[GameManager] 無法設定攝影機跟隨：" +
                    (virtualCamera == null ? "找不到 VirtualCamera" : "找不到 Player"));
            }
        }

        /// <summary>
        /// 場景載入後解除 TelePoint 可能留下的攝影機水平邊界限制。
        /// </summary>
        public void ClearCameraHorizontalBounds()
        {
            CameraHorizontalBounds[] boundsList = FindObjectsOfType<CameraHorizontalBounds>(true);
            for (int i = 0; i < boundsList.Length; i++)
            {
                if (boundsList[i] == null)
                    continue;

                boundsList[i].ClearBounds();
            }

            CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                {
                    cameras[i].PreviousStateIsValid = false;
                }
            }
        }
        /// <summary>
        /// 切換玩家位置
        /// </summary>
        public void SwitchPlayerPos(Vector3 position)
        {
            SetPlayerPosition(position);
        }
        #endregion
        #region 場景轉換捷徑方法（透過 SceneTransitionManager）
        private void BlackView()
        {

        }
        public void LoadImage()
        {

        }
        /// <summary>
        /// 載入指定場景
        /// </summary>
        public void LoadScene(string sceneName) => sceneTransitionManager.LoadScene(sceneName);

        /// <summary>
        /// 返回主選單
        /// </summary>
        public void GoToMainMenu() => sceneTransitionManager.GoToMainMenu();

        /// <summary>
        /// 進入人類場景（白天）
        /// </summary>
        public void GoToHumanScene() => sceneTransitionManager.GoToHumanScene();

        /// <summary>
        /// 進入妖怪場景（夜晚）
        /// </summary>
        public void GoToMonsterScene() => sceneTransitionManager.GoToMonsterScene();
        /// <summary>
        /// 進入下一天（從夜晚結束進入新的白天）
        /// </summary>
        public void GoToNextDay()
        {
            gameFlow?.NextDay();
            sceneTransitionManager.GoToHumanScene();
        }
        #endregion
    }
}
