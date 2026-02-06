using UnityEngine;
using System.Collections;
using Player;
using Cinemachine;

namespace GameSystem
{
    public class GameManager : Singleton<GameManager>
    {
        private DataManager dataManager;
        private SceneTransitionManager sceneTransitionManager;
        public SaveManager saveManager;
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
            dataManager = DataManager.Instance;
            saveManager = SaveManager.Instance;
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
            if (sceneName == SceneTransitionManager.SCENE_MAIN_MENU)
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
            SetPlayerMove(true);
            SetPlayerInteract(true);
            SetCameraFollowPlayer();
            if (sceneName == SceneTransitionManager.SCENE_MONSTER)
            {
                PlayerController.SetIsNight(true);
            }
        }

        private IEnumerator Start()
        {
            while (!dataManager.IsInitialized)
                yield return null;
            sceneTransitionManager.LoadScene(SceneTransitionManager.SCENE_MAIN_MENU);
        }

        public async void StartNewGame()
        {
            // 取得下一個可用的存檔欄位
            int slot = saveManager.GetNextAvailableSlot();

            // 取得初始玩家資料並生成新的隨機種子
            var newPlayerData = dataManager.InitialPlayerData;
            newPlayerData.MasterSeed = UnityEngine.Random.Range(1, int.MaxValue);
            // 使用新種子的玩家資料創建新遊戲
            dataManager.SetCurrentPlayer(newPlayerData);
            gameFlow = new GameFlow(newPlayerData, slot);
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
            gameFlow = new GameFlow(playerData, slot);
            sceneTransitionManager.GoToSceneByPhase(playerData.PlayingStatus, () =>
            {
                // 場景載入完成後才執行
                DataManager.Instance.ModifyCurrentDay(playerData.DaysPlayed);
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
            Player.transform.position = position;
        }
        public void SetPlayerMove(bool CanMove)
        {
            PlayerController.SetCanMove(CanMove);
        }
        public void SetPlayerInteract(bool CanInteract)
        {
            PlayerController.SetCanInteract(CanInteract);
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
        /// 進入交易場景
        /// </summary>
        public void GoToTradeScene() => sceneTransitionManager.GoToTradeScene();

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
