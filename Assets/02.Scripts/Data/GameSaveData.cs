using System.Collections.Generic;

namespace MiniExtractionShooter.Data
{
    /// <summary>
    /// 전체 게임 저장 데이터
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        public InventorySaveData inventory = new InventorySaveData();
        public WeaponSaveData weapons = new WeaponSaveData();
        public StatisticsSaveData statistics = new StatisticsSaveData();
        public QuickSlotSaveData quickSlots = new QuickSlotSaveData();
        public string saveTime;
    }

    /// <summary>
    /// 인벤토리 저장 데이터
    /// </summary>
    [System.Serializable]
    public class InventorySaveData
    {
        public int pistolAmmo;
        public int rifleAmmo;
        public string armorName;  // ArmorData.armorName
        public List<ItemSaveData> items = new List<ItemSaveData>();
    }

    /// <summary>
    /// 무기 저장 데이터
    /// </summary>
    [System.Serializable]
    public class WeaponSaveData
    {
        public string primaryWeaponName;
        public string secondaryWeaponName;
        public int currentSlot;
    }

    /// <summary>
    /// 아이템 저장 데이터
    /// </summary>
    [System.Serializable]
    public class ItemSaveData
    {
        public string itemName;
        public int itemType;  // ItemType enum as int
        public int amount;
        public string ammoTypeName;  // AmmoType enum as string
        public string weaponDataName;
        public string armorDataName;

        /// <summary>
        /// InventoryItem에서 ItemSaveData 생성
        /// </summary>
        public static ItemSaveData FromInventoryItem(MiniExtractionShooter.Player.InventoryItem item)
        {
            return new ItemSaveData
            {
                itemName = item.ItemName,
                itemType = (int)item.ItemType,
                amount = item.amount,
                ammoTypeName = item.AmmoData?.ammoType.ToString() ?? "",
                weaponDataName = item.WeaponData?.itemName ?? "",
                armorDataName = item.ArmorData?.itemName ?? ""
            };
        }
    }

    /// <summary>
    /// 통계 저장 데이터
    /// </summary>
    [System.Serializable]
    public class StatisticsSaveData
    {
        public int enemiesKilled;
        public int itemsLooted;
        public float playTime;
    }

    /// <summary>
    /// 퀵슬롯 저장 데이터
    /// </summary>
    [System.Serializable]
    public class QuickSlotSaveData
    {
        public List<string> slotItemNames = new List<string>();
    }
}
