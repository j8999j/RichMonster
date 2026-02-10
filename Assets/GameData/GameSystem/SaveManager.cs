using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace GameSystem
{
    /// <summary>
    /// 負責遊戲存檔與讀檔，直接保存完整的 PlayerData（含 Inventory 與商店變更）。
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private const string BookSaveFilePattern = "illustrated_book.json";
        private const string SaveFilePattern = "save_slot_{0}.json";
        private SaveFileData _lastLoaded;
        private GameSaveBook _cachedBookData;

        /// <summary>
        /// 共用的 JSON 設定，確保介面類型可以正確序列化/反序列化
        /// </summary>
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        /// <summary>
        /// 是否正在存檔中，用於防止連點導致的檔案寫入衝突
        /// </summary>
        public bool IsSaving { get; private set; }

        /// <summary>
        /// 是否正在儲存圖鑑中，獨立於一般存檔
        /// </summary>
        public bool IsSavingBook { get; private set; }

        public SaveFileData LastLoaded => CloneData(_lastLoaded);

        public async Task SaveGameAsync(PlayerData playerData, int slot = 0)
        {
            // 如果正在存檔中，跳過本次請求
            if (IsSaving)
            {
                Debug.LogWarning("[SaveManager] 正在存檔中，跳過本次存檔請求");
                return;
            }

            IsSaving = true;
            string filePath = GetFilePath(slot);

            var payload = new SaveFileData
            {
                Player = ClonePlayer(playerData)
            };

            try
            {
                string json = JsonConvert.SerializeObject(payload, Formatting.Indented, _jsonSettings);
                await File.WriteAllTextAsync(filePath, json);
                Debug.Log($"[SaveManager] 存檔完成: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] 存檔失敗: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }
        

        public SaveFileData Load(int slot = 0)
        {
            string filePath = GetFilePath(slot);

            if (!File.Exists(filePath))
            {
                Debug.Log($"[SaveManager] 找不到存檔，建立新的資料: {filePath}");
                _lastLoaded = new SaveFileData
                {
                    Player = DataManager.Instance?.InitialPlayerData ?? new PlayerData()
                };
                EnsureLists(_lastLoaded.Player);
                return CloneData(_lastLoaded);
            }

            try
            {
                string json = File.ReadAllText(filePath);
                _lastLoaded = JsonConvert.DeserializeObject<SaveFileData>(json, _jsonSettings) ?? new SaveFileData();

                if (_lastLoaded.Player == null) _lastLoaded.Player = new PlayerData();
                EnsureLists(_lastLoaded.Player);

                Debug.Log($"[SaveManager] 讀檔成功: {filePath}");
                return CloneData(_lastLoaded);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] 讀檔失敗: {ex.Message}");
                _lastLoaded = new SaveFileData
                {
                    Player = new PlayerData()
                };
                EnsureLists(_lastLoaded.Player);
                return CloneData(_lastLoaded);
            }
        }

        /// <summary>
        /// 載入存檔欄位的摘要資訊 (給 MVP Presenter 使用)
        /// </summary>
        public SaveSlotData LoadSlotInfo(int slot)
        {
            string filePath = GetFilePath(slot);
            var slotData = new SaveSlotData { SlotIndex = slot };

            if (!File.Exists(filePath))
            {
                slotData.IsEmpty = true;
                return slotData;
            }

            try
            {
                string json = File.ReadAllText(filePath);

                // ---------------------------------------------------------
                // 修正點：必須建立與存檔時一模一樣的設定
                // ---------------------------------------------------------
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                // 帶入 settings 進行反序列化
                var saveData = JsonConvert.DeserializeObject<SaveFileData>(json, settings);

                if (saveData?.Player != null)
                {
                    slotData.IsEmpty = false;
                    slotData.DaysPlayed = saveData.Player.DaysPlayed;
                    slotData.Gold = saveData.Player.Gold;
                    // 注意這裡強制轉型可能會有隱藏風險，確保 Enum 定義一致
                    slotData.CurrentPhase = (DayPhase)saveData.Player.PlayingStatus;

                    var fileInfo = new FileInfo(filePath);
                    slotData.SaveTime = fileInfo.LastWriteTime;
                }
                else
                {
                    slotData.IsEmpty = true;
                }
            }
            catch (System.Exception ex)
            {
                // 這裡會捕捉到 "Could not create instance..." 如果設定沒加上的話
                Debug.LogError($"[SaveManager] 讀取存檔資訊 {slot} 失敗: {ex.Message}");
                slotData.IsEmpty = true;
            }

            return slotData;
        }

        /// <summary>
        /// 取得下一個可用的存檔欄位 (按序列新增)
        /// </summary>
        public int GetNextAvailableSlot(int maxSlots = 10)
        {
            for (int i = 0; i < maxSlots; i++)
            {
                string filePath = GetFilePath(i);
                if (!File.Exists(filePath))
                {
                    return i;
                }
            }
            // 如果所有存檔都已使用，返回下一個序號
            return maxSlots;
        }

        private string GetFilePath(int slot)
        {
            if (slot < 0) slot = 0;
            string fileName = string.Format(SaveFilePattern, slot);
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        private static SaveFileData CloneData(SaveFileData source)
        {
            if (source == null) return null;
            var json = JsonConvert.SerializeObject(source, _jsonSettings);
            return JsonConvert.DeserializeObject<SaveFileData>(json, _jsonSettings);
        }

        private PlayerData ClonePlayer(PlayerData source)
        {
            if (source == null) return new PlayerData();
            // 這裡我們告訴 JsonConvert：「請把類別名稱 (Type Name) 也存進去！」
            var settings = new JsonSerializerSettings
            {
                // Auto 代表：如果型別是介面或繼承類別，自動寫入 "$type" 屬性
                TypeNameHandling = TypeNameHandling.Auto,

                // (選用建議) 防止物件 A 參照 B，B 又參照 A 造成的無窮迴圈錯誤
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // 3. 序列化 (轉成字串，包含 $type 資訊)
            var json = JsonConvert.SerializeObject(source, settings);

            // 4. 反序列化 (讀回物件，根據 $type 資訊還原正確的 Class)
            var clone = JsonConvert.DeserializeObject<PlayerData>(json, settings);
            EnsureLists(clone);
            return clone;
        }

        private void EnsureLists(PlayerData player)
        {
            if (player == null) return;
            player.Inventory ??= new Inventory();
            player.Inventory.Items ??= new List<Item>();
        }

        #region Book Save/Load (跨單局圖鑑資料)
        /// <summary>
        /// 非同步儲存圖鑑資料 (物品圖鑑與妖怪圖鑑)
        /// </summary>
        public async Task SaveBookDataAsync(GameSaveBook bookData)
        {
            if (IsSavingBook)
            {
                Debug.LogWarning("[SaveManager] 正在儲存圖鑑中，跳過本次圖鑑存檔請求");
                return;
            }

            IsSavingBook = true;
            string filePath = GetBookFilePath();

            try
            {
                string json = JsonConvert.SerializeObject(bookData, Formatting.Indented, _jsonSettings);
                await File.WriteAllTextAsync(filePath, json);
                _cachedBookData = bookData;
                Debug.Log($"[SaveManager] 圖鑑存檔完成: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] 圖鑑存檔失敗: {ex.Message}");
            }
            finally
            {
                IsSavingBook = false;
            }
        }

        /// <summary>
        /// 同步儲存圖鑑資料 (物品圖鑑與妖怪圖鑑)
        /// </summary>
        public void SaveBookData(GameSaveBook bookData)
        {
            string filePath = GetBookFilePath();

            try
            {
                string json = JsonConvert.SerializeObject(bookData, Formatting.Indented, _jsonSettings);
                File.WriteAllText(filePath, json);
                _cachedBookData = bookData;
                Debug.Log($"[SaveManager] 圖鑑存檔完成: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] 圖鑑存檔失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定圖鑑快取資料 (由 GameDataLoader 載入後設定)
        /// </summary>
        public void SetBookDataCache(GameSaveBook bookData)
        {
            _cachedBookData = bookData;
        }

        /// <summary>
        /// 取得圖鑑快取資料
        /// </summary>
        public GameSaveBook GetBookDataCache()
        {
            return _cachedBookData;
        }

        private string GetBookFilePath()
        {
            return Path.Combine(Application.persistentDataPath, BookSaveFilePattern);
        }
        #endregion
    }

    [System.Serializable]
    public class SaveFileData
    {
        public PlayerData Player = new PlayerData();
    }
}
