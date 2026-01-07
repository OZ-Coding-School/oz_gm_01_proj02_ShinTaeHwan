using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.Player
{
    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public ItemType itemType;
        public int amount;
        public Sprite icon;

        // 참조 데이터
        public WeaponData weaponData;
        public ArmorData armorData;
        public AmmoType ammoType;
    }

    /// <summary>
    /// 플레이어 인벤토리 시스템
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Ammo")]
        [SerializeField] private int pistolAmmo = 36;   // 시작 탄약 (3탄창)
        [SerializeField] private int rifleAmmo = 0;

        [Header("Current Equipment")]
        [SerializeField] private ArmorData currentArmor;

        [Header("Items")]
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        [Header("Limits")]
        [SerializeField] private int maxPistolAmmo = 120;
        [SerializeField] private int maxRifleAmmo = 180;
        [SerializeField] private int maxHealthItems = 5;

        // Events
        public event System.Action<AmmoType, int> OnAmmoChanged;
        public event System.Action<ArmorData> OnArmorChanged;
        public event System.Action<InventoryItem> OnItemAdded;
        public event System.Action<InventoryItem> OnItemRemoved;

        public int PistolAmmo => pistolAmmo;
        public int RifleAmmo => rifleAmmo;
        public ArmorData CurrentArmor => currentArmor;
        public List<InventoryItem> Items => items;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// 탄약 추가
        /// </summary>
        public void AddAmmo(AmmoType type, int amount)
        {
            switch (type)
            {
                case AmmoType.Pistol:
                    pistolAmmo = Mathf.Min(pistolAmmo + amount, maxPistolAmmo);
                    OnAmmoChanged?.Invoke(type, pistolAmmo);
                    break;
                case AmmoType.Rifle:
                    rifleAmmo = Mathf.Min(rifleAmmo + amount, maxRifleAmmo);
                    OnAmmoChanged?.Invoke(type, rifleAmmo);
                    break;
            }
        }

        /// <summary>
        /// 탄약 사용
        /// </summary>
        public bool UseAmmo(AmmoType type, int amount)
        {
            switch (type)
            {
                case AmmoType.Pistol:
                    if (pistolAmmo >= amount)
                    {
                        pistolAmmo -= amount;
                        OnAmmoChanged?.Invoke(type, pistolAmmo);
                        return true;
                    }
                    break;
                case AmmoType.Rifle:
                    if (rifleAmmo >= amount)
                    {
                        rifleAmmo -= amount;
                        OnAmmoChanged?.Invoke(type, rifleAmmo);
                        return true;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 특정 타입 탄약 수량 확인
        /// </summary>
        public int GetAmmo(AmmoType type)
        {
            return type switch
            {
                AmmoType.Pistol => pistolAmmo,
                AmmoType.Rifle => rifleAmmo,
                _ => 0
            };
        }

        /// <summary>
        /// 방어구 장착
        /// </summary>
        public void EquipArmor(ArmorData armor)
        {
            currentArmor = armor;

            // 이동 속도 감소 적용
            if (PlayerController.Instance != null && armor != null)
            {
                PlayerController.Instance.SetArmorSpeedReduction(armor.moveSpeedReduction);
            }
            else if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetArmorSpeedReduction(0f);
            }

            OnArmorChanged?.Invoke(armor);
        }

        /// <summary>
        /// 현재 방어력 가져오기
        /// </summary>
        public float GetCurrentDefense()
        {
            return currentArmor != null ? currentArmor.defense : 0f;
        }

        /// <summary>
        /// LootEntry에서 아이템 추가
        /// </summary>
        public void AddItem(LootEntry lootEntry)
        {
            InventoryItem newItem = new InventoryItem
            {
                itemName = lootEntry.itemName,
                itemType = lootEntry.itemType,
                amount = lootEntry.minAmount,
                icon = lootEntry.icon,
                weaponData = lootEntry.weaponData,
                armorData = lootEntry.armorData,
                ammoType = lootEntry.ammoType
            };

            // 아이템 타입별 처리
            switch (lootEntry.itemType)
            {
                case ItemType.Ammo:
                    AddAmmo(lootEntry.ammoType, lootEntry.minAmount);
                    break;

                case ItemType.Armor:
                    // 더 좋은 방어구면 자동 장착
                    if (currentArmor == null ||
                        (lootEntry.armorData != null && lootEntry.armorData.defense > currentArmor.defense))
                    {
                        EquipArmor(lootEntry.armorData);
                    }
                    break;

                case ItemType.Weapon:
                    // WeaponManager에서 처리
                    break;

                case ItemType.Health:
                case ItemType.Valuable:
                    items.Add(newItem);
                    OnItemAdded?.Invoke(newItem);
                    break;
            }
        }

        /// <summary>
        /// 아이템 사용
        /// </summary>
        public bool UseItem(InventoryItem item)
        {
            if (!items.Contains(item)) return false;

            switch (item.itemType)
            {
                case ItemType.Health:
                    if (PlayerHealth.Instance != null)
                    {
                        PlayerHealth.Instance.Heal(item.amount);
                        items.Remove(item);
                        OnItemRemoved?.Invoke(item);
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 회복 아이템 사용 (가장 작은 것부터)
        /// </summary>
        public bool UseHealthItem()
        {
            InventoryItem healthItem = items.Find(i => i.itemType == ItemType.Health);
            if (healthItem != null)
            {
                return UseItem(healthItem);
            }
            return false;
        }

        /// <summary>
        /// 인벤토리 초기화 (게임 시작 시)
        /// </summary>
        public void ResetInventory()
        {
            pistolAmmo = 36;
            rifleAmmo = 0;
            currentArmor = null;
            items.Clear();

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetArmorSpeedReduction(0f);
            }

            OnAmmoChanged?.Invoke(AmmoType.Pistol, pistolAmmo);
            OnAmmoChanged?.Invoke(AmmoType.Rifle, rifleAmmo);
            OnArmorChanged?.Invoke(null);
        }
    }
}
