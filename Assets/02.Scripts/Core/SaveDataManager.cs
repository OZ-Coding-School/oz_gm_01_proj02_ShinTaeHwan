using UnityEngine;
using System.IO;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Weapon;
using MiniExtractionShooter.Managers;
using MiniExtractionShooter.UI.Inventory;

namespace MiniExtractionShooter.Core
{
    /// <summary>
    /// 게임 데이터 저장/로드 매니저
    /// JSON 파일로 인벤토리, 무기, 통계 저장
    /// </summary>
    public class SaveDataManager : Singleton<SaveDataManager>
    {
        [Header("Settings")]
        [SerializeField] private string saveFileName = "save_data.json";
        [SerializeField] private bool autoSaveOnQuit = true;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        /// <summary>
        /// 저장 파일 경로
        /// </summary>
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

        /// <summary>
        /// 저장 파일 존재 확인
        /// </summary>
        public bool HasSaveFile()
        {
            return File.Exists(SaveFilePath);
        }

        protected override void Awake()
        {
            base.Awake();
            dontDestroyOnLoad = true;
        }

        protected override void OnApplicationQuit()
        {
            if (autoSaveOnQuit)
            {
                SaveGame();
            }
            base.OnApplicationQuit();
        }

        /// <summary>
        /// 게임 저장
        /// </summary>
        public void SaveGame()
        {
            try
            {
                GameSaveData saveData = new GameSaveData();

                // 인벤토리 데이터 수집
                if (PlayerInventory.Instance != null)
                {
                    saveData.inventory = GetInventorySaveData();
                }

                // 무기 데이터 수집
                if (WeaponManager.Instance != null)
                {
                    saveData.weapons = GetWeaponSaveData();
                }

                // 통계 데이터 수집
                if (GameManager.Instance != null)
                {
                    saveData.statistics = GetStatisticsSaveData();
                }

                // 퀵슬롯 데이터 수집
                if (QuickSlotManager.Instance != null)
                {
                    saveData.quickSlots.slotItemNames = QuickSlotManager.Instance.GetSaveData();
                }

                // 저장 시간
                saveData.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // JSON 변환 및 저장
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SaveFilePath, json);

                if (debugMode)
                {
                    Debug.Log($"[SaveDataManager] Game saved to: {SaveFilePath}");
                    Debug.Log($"[SaveDataManager] Data: {json}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveDataManager] Failed to save game: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 로드
        /// </summary>
        public bool LoadGame()
        {
            // Debug.Log($"[SaveDataManager] LoadGame 호출됨. 저장 파일 경로: {SaveFilePath}");

            if (!HasSaveFile())
            {
                // Debug.Log("[SaveDataManager] 저장 파일 없음.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                // Debug.Log($"[SaveDataManager] JSON 로드됨: {json}");

                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

                if (saveData == null)
                {
                    Debug.LogWarning("[SaveDataManager] Failed to parse save data.");
                    return false;
                }

                // Debug.Log($"[SaveDataManager] 저장된 아이템 수: {saveData.inventory?.items?.Count ?? 0}");
                // Debug.Log($"[SaveDataManager] 저장된 퀵슬롯: {string.Join(", ", saveData.quickSlots?.slotItemNames ?? new System.Collections.Generic.List<string>())}");

                // 인벤토리 복원
                if (PlayerInventory.Instance != null && saveData.inventory != null)
                {
                    // Debug.Log("[SaveDataManager] 인벤토리 복원 시작");
                    LoadInventoryData(saveData.inventory);
                }

                // 무기 복원
                if (WeaponManager.Instance != null && saveData.weapons != null)
                {
                    LoadWeaponData(saveData.weapons);
                }

                // 퀵슬롯 복원
                if (QuickSlotManager.Instance != null && saveData.quickSlots != null)
                {
                    // Debug.Log("[SaveDataManager] 퀵슬롯 복원 시작");
                    QuickSlotManager.Instance.LoadData(saveData.quickSlots.slotItemNames);
                }

                // Debug.Log($"[SaveDataManager] 게임 로드 완료. 저장 시간: {saveData.saveTime}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveDataManager] Failed to load game: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 저장 파일 삭제
        /// </summary>
        public void DeleteSave()
        {
            if (HasSaveFile())
            {
                File.Delete(SaveFilePath);
                if (debugMode)
                {
                    Debug.Log("[SaveDataManager] Save file deleted.");
                }
            }
        }

        #region Data Collection

        private InventorySaveData GetInventorySaveData()
        {
            PlayerInventory inv = PlayerInventory.Instance;
            InventorySaveData data = new InventorySaveData
            {
                pistolAmmo = inv.PistolAmmo,
                rifleAmmo = inv.RifleAmmo,
                armorName = inv.CurrentArmor?.itemName ?? ""
            };

            // 아이템 저장
            foreach (var item in inv.Items)
            {
                data.items.Add(ItemSaveData.FromInventoryItem(item));
            }

            return data;
        }

        private WeaponSaveData GetWeaponSaveData()
        {
            WeaponManager wm = WeaponManager.Instance;
            return new WeaponSaveData
            {
                primaryWeaponName = wm.GetPrimaryWeaponName(),
                secondaryWeaponName = wm.GetSecondaryWeaponName(),
                currentSlot = wm.CurrentSlot
            };
        }

        private StatisticsSaveData GetStatisticsSaveData()
        {
            var stats = GameManager.Instance.GetStatistics();
            return new StatisticsSaveData
            {
                enemiesKilled = stats.kills,
                itemsLooted = stats.items,
                playTime = stats.time
            };
        }

        #endregion

        #region Data Loading

        private void LoadInventoryData(InventorySaveData data)
        {
            // Debug.Log($"[SaveDataManager] LoadInventoryData 시작 - 탄약: Pistol={data.pistolAmmo}, Rifle={data.rifleAmmo}, 아이템 수={data.items?.Count ?? 0}");
            PlayerInventory inv = PlayerInventory.Instance;

            // 탄약 설정
            inv.SetAmmo(AmmoType.Pistol, data.pistolAmmo);
            inv.SetAmmo(AmmoType.Rifle, data.rifleAmmo);

            // 방어구 설정
            if (!string.IsNullOrEmpty(data.armorName))
            {
                ArmorData armor = FindArmorByName(data.armorName);
                if (armor != null)
                {
                    inv.EquipArmor(armor);
                }
            }

            // 아이템 복원
            // Debug.Log($"[SaveDataManager] 인벤토리 클리어 후 아이템 복원 시작. 복원할 아이템 수: {data.items?.Count ?? 0}");
            inv.ClearItems();
            foreach (var itemData in data.items)
            {
                // Debug.Log($"[SaveDataManager] 아이템 복원 시도: {itemData.itemName} x{itemData.amount}");
                inv.AddItemFromSaveData(itemData);
            }
            // Debug.Log($"[SaveDataManager] LoadInventoryData 완료. 현재 인벤토리 아이템 수: {inv.Items.Count}");
        }

        private void LoadWeaponData(WeaponSaveData data)
        {
            WeaponManager wm = WeaponManager.Instance;

            // 무기 복원
            if (!string.IsNullOrEmpty(data.primaryWeaponName))
            {
                WeaponData primary = WeaponDatabase.Instance?.FindByName(data.primaryWeaponName);
                if (primary != null)
                {
                    wm.SetPrimaryWeapon(primary);
                }
            }

            if (!string.IsNullOrEmpty(data.secondaryWeaponName))
            {
                WeaponData secondary = WeaponDatabase.Instance?.FindByName(data.secondaryWeaponName);
                if (secondary != null)
                {
                    wm.SetSecondaryWeapon(secondary);
                }
            }

            // 현재 슬롯으로 전환
            wm.SwitchToWeapon(data.currentSlot);
        }

        #endregion

        #region Resource Lookup

        /// <summary>
        /// 이름으로 방어구 찾기 (ItemDatabase 사용)
        /// </summary>
        private ArmorData FindArmorByName(string name)
        {
            return ItemDatabase.Instance.GetItemByName(name) as ArmorData;
        }

        #endregion
    }
}
