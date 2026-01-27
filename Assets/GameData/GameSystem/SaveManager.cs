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
        private const string SaveFilePattern = "save_slot_{0}.json";
        private SaveFileData _lastLoaded;

        /// <summary>
        /// 是否正在存檔中，用於防止連點導致的檔案寫入衝突
        /// </summary>
        public bool IsSaving { get; private set; }

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
                string json = JsonConvert.SerializeObject(payload, Formatting.Indented);
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
                _lastLoaded = JsonConvert.DeserializeObject<SaveFileData>(json) ?? new SaveFileData();

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
                var saveData = JsonConvert.DeserializeObject<SaveFileData>(json);

                if (saveData?.Player != null)
                {
                    slotData.IsEmpty = false;
                    slotData.DaysPlayed = saveData.Player.DaysPlayed;
                    slotData.Gold = saveData.Player.Gold;
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
            return JsonConvert.DeserializeObject<SaveFileData>(JsonConvert.SerializeObject(source));
        }

        private PlayerData ClonePlayer(PlayerData source)
        {
            if (source == null) return new PlayerData();
            var clone = JsonConvert.DeserializeObject<PlayerData>(JsonConvert.SerializeObject(source));
            EnsureLists(clone);
            return clone;
        }

        private void EnsureLists(PlayerData player)
        {
            if (player == null) return;
            player.Inventory ??= new Inventory();
            player.Inventory.Items ??= new List<Item>();
            player.ShopShelves ??= new List<ShopShelfData>();
        }
    }

    [System.Serializable]
    public class SaveFileData
    {
        public PlayerData Player = new PlayerData();
    }
}
