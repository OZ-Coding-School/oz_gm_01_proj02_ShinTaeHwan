using UnityEngine;
using System.IO;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Weapon;
using MiniExtractionShooter.Managers;

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
            if (!HasSaveFile())
            {
                if (debugMode)
                {
                    Debug.Log("[SaveDataManager] No save file found.");
                }
                return false;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

                if (saveData == null)
                {
                    Debug.LogWarning("[SaveDataManager] Failed to parse save data.");
                    return false;
                }

                // 인벤토리 복원
                if (PlayerInventory.Instance != null && saveData.inventory != null)
                {
                    LoadInventoryData(saveData.inventory);
                }

                // 무기 복원
                if (WeaponManager.Instance != null && saveData.weapons != null)
                {
                    LoadWeaponData(saveData.weapons);
                }

                // 통계는 GameManager에서 별도 관리

                if (debugMode)
                {
                    Debug.Log($"[SaveDataManager] Game loaded from: {SaveFilePath}");
                    Debug.Log($"[SaveDataManager] Save time: {saveData.saveTime}");
                }

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
            inv.ClearItems();
            foreach (var itemData in data.items)
            {
                inv.AddItemFromSaveData(itemData);
            }
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

        [Header("Resources")]
        [SerializeField] private ArmorData[] availableArmors;

        /// <summary>
        /// 이름으로 방어구 찾기
        /// </summary>
        private ArmorData FindArmorByName(string name)
        {
            if (availableArmors == null) return null;

            foreach (var armor in availableArmors)
            {
                if (armor != null && armor.itemName == name)
                {
                    return armor;
                }
            }
            return null;
        }

        #endregion
    }
}
