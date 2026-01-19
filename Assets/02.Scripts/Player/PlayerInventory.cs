using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.Player
{
    /// <summary>
    /// 인벤토리 아이템 - ItemData 기반 통합 아이템
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData itemData;  // 통합 아이템 데이터 참조
        public int amount = 1;

        // 편의 프로퍼티
        public string ItemName => itemData?.itemName ?? "";
        public ItemType ItemType => itemData?.itemType ?? ItemType.Valuable;
        public Sprite Icon => itemData?.icon;
        public bool IsStackable => itemData?.isStackable ?? false;
        public int MaxStackSize => itemData?.maxStackSize ?? 1;

        // 타입 캐스팅 헬퍼
        public WeaponData WeaponData => itemData as WeaponData;
        public ArmorData ArmorData => itemData as ArmorData;
        public AmmoData AmmoData => itemData as AmmoData;

        public InventoryItem() { }

        public InventoryItem(ItemData data, int count = 1)
        {
            itemData = data;
            amount = count;
        }
    }

    /// <summary>
    /// 플레이어 인벤토리 시스템
    /// 모든 아이템을 Items 리스트에서 통합 관리
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Current Equipment")]
        [SerializeField] private ArmorData currentArmor;

        [Header("Items")]
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        [Header("Starting Items")]
        [SerializeField] private AmmoData pistolAmmoData;
        [SerializeField] private AmmoData rifleAmmoData;
        [SerializeField] private int startingPistolAmmo = 36;

        [Header("Limits")]
        [SerializeField] private int maxInventorySlots = 30;

        // Events
        public event System.Action<AmmoType, int> OnAmmoChanged;
        public event System.Action<ArmorData> OnArmorChanged;
        public event System.Action<InventoryItem> OnItemAdded;
        public event System.Action<InventoryItem> OnItemRemoved;
        public event System.Action OnInventoryChanged;

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

        private void Start()
        {
            // 시작 탄약 추가
            InitializeStartingItems();
        }

        /// <summary>
        /// 시작 아이템 초기화
        /// </summary>
        private void InitializeStartingItems()
        {
            if (pistolAmmoData != null && startingPistolAmmo > 0)
            {
                AddItem(pistolAmmoData, startingPistolAmmo);
            }
        }

        #region Item Management

        /// <summary>
        /// ItemData로 아이템 추가
        /// </summary>
        public bool AddItem(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0) return false;

            // 스택 가능한 아이템인 경우 기존 아이템에 추가 시도
            if (itemData.isStackable)
            {
                InventoryItem existing = FindItem(itemData);
                if (existing != null)
                {
                    int maxAdd = existing.MaxStackSize - existing.amount;
                    int toAdd = Mathf.Min(amount, maxAdd);
                    existing.amount += toAdd;
                    amount -= toAdd;

                    // 탄약의 경우 이벤트 발생
                    if (itemData is AmmoData ammoData)
                    {
                        OnAmmoChanged?.Invoke(ammoData.ammoType, GetAmmo(ammoData.ammoType));
                    }

                    OnInventoryChanged?.Invoke();

                    if (amount <= 0) return true;
                }
            }

            // 새 슬롯에 추가
            if (items.Count >= maxInventorySlots) return false;

            InventoryItem newItem = new InventoryItem(itemData, amount);
            items.Add(newItem);
            OnItemAdded?.Invoke(newItem);
            OnInventoryChanged?.Invoke();

            // 탄약의 경우 이벤트 발생
            if (itemData is AmmoData ammoData2)
            {
                OnAmmoChanged?.Invoke(ammoData2.ammoType, GetAmmo(ammoData2.ammoType));
            }

            return true;
        }

        /// <summary>
        /// LootEntry에서 아이템 추가 - 통합 방식
        /// </summary>
        public void AddItem(LootEntry lootEntry)
        {
            if (lootEntry == null) return;

            // 통합 방식: GetItemData()로 ItemData 가져오기
            ItemData itemData = lootEntry.GetItemData();
            if (itemData != null)
            {
                int amount = Random.Range(lootEntry.minAmount, lootEntry.maxAmount + 1);
                AddItem(itemData, amount);
            }
            else
            {
                Debug.LogWarning($"[PlayerInventory] LootEntry has no ItemData: {lootEntry.itemName}");
            }
        }

        /// <summary>
        /// 아이템 제거
        /// </summary>
        public bool RemoveItem(InventoryItem item)
        {
            if (items.Remove(item))
            {
                OnItemRemoved?.Invoke(item);
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 아이템 수량 감소
        /// </summary>
        public bool RemoveItemAmount(InventoryItem item, int amount)
        {
            if (item == null || !items.Contains(item)) return false;

            item.amount -= amount;
            if (item.amount <= 0)
            {
                items.Remove(item);
                OnItemRemoved?.Invoke(item);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// ItemData로 아이템 찾기
        /// </summary>
        public InventoryItem FindItem(ItemData itemData)
        {
            return items.FirstOrDefault(i => i.itemData == itemData);
        }

        /// <summary>
        /// 아이템 타입으로 찾기
        /// </summary>
        public InventoryItem FindItemByType(ItemType type)
        {
            return items.FirstOrDefault(i => i.ItemType == type);
        }

        /// <summary>
        /// 모든 아이템 비우기
        /// </summary>
        public void ClearItems()
        {
            items.Clear();
            OnInventoryChanged?.Invoke();
        }

        #endregion

        #region Ammo Management

        /// <summary>
        /// 특정 타입 탄약 수량 확인
        /// </summary>
        public int GetAmmo(AmmoType type)
        {
            int total = 0;
            foreach (var item in items)
            {
                if (item.AmmoData != null && item.AmmoData.ammoType == type)
                {
                    total += item.amount;
                }
            }
            return total;
        }

        /// <summary>
        /// 탄약 사용
        /// </summary>
        public bool UseAmmo(AmmoType type, int amount)
        {
            if (GetAmmo(type) < amount) return false;

            int remaining = amount;
            var ammoItems = items.Where(i => i.AmmoData != null && i.AmmoData.ammoType == type).ToList();

            foreach (var item in ammoItems)
            {
                if (remaining <= 0) break;

                int toUse = Mathf.Min(item.amount, remaining);
                item.amount -= toUse;
                remaining -= toUse;

                if (item.amount <= 0)
                {
                    items.Remove(item);
                    OnItemRemoved?.Invoke(item);
                }
            }

            OnAmmoChanged?.Invoke(type, GetAmmo(type));
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 탄약 추가 (AmmoType 기반 - 하위 호환)
        /// </summary>
        public void AddAmmo(AmmoType type, int amount)
        {
            AmmoData ammoData = FindAmmoDataByType(type);
            if (ammoData != null)
            {
                AddItem(ammoData, amount);
            }
        }

        /// <summary>
        /// 탄약 데이터 찾기
        /// </summary>
        private AmmoData FindAmmoDataByType(AmmoType type)
        {
            // 인벤토리에서 먼저 찾기
            var existing = items.FirstOrDefault(i => i.AmmoData != null && i.AmmoData.ammoType == type);
            if (existing != null) return existing.AmmoData;

            // 설정된 탄약 데이터 사용
            if (type == AmmoType.Pistol && pistolAmmoData != null)
            {
                return pistolAmmoData;
            }
            if (type == AmmoType.Rifle && rifleAmmoData != null)
            {
                return rifleAmmoData;
            }

            // Resources에서 찾기
            AmmoData[] allAmmo = Resources.LoadAll<AmmoData>("");
            var found = allAmmo.FirstOrDefault(a => a.ammoType == type);
            
            if (found == null)
            {
                Debug.LogWarning($"[PlayerInventory] AmmoData not found for type: {type}. Make sure to assign ammo data in Inspector or place in Resources folder.");
            }
            
            return found;
        }

        // 하위 호환용 프로퍼티
        public int PistolAmmo => GetAmmo(AmmoType.Pistol);
        public int RifleAmmo => GetAmmo(AmmoType.Rifle);

        #endregion

        #region Equipment

        /// <summary>
        /// 방어구 장착
        /// </summary>
        public void EquipArmor(ArmorData armor)
        {
            currentArmor = armor;

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

        #endregion

        #region Utility

        /// <summary>
        /// 아이템 사용
        /// </summary>
        public bool UseItem(InventoryItem item)
        {
            if (!items.Contains(item)) return false;

            switch (item.ItemType)
            {
                case ItemType.Health:
                    if (PlayerHealth.Instance != null)
                    {
                        PlayerHealth.Instance.Heal(item.amount);
                        RemoveItem(item);
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 회복 아이템 사용
        /// </summary>
        public bool UseHealthItem()
        {
            InventoryItem healthItem = FindItemByType(ItemType.Health);
            if (healthItem != null)
            {
                return UseItem(healthItem);
            }
            return false;
        }

        /// <summary>
        /// 인벤토리 초기화
        /// </summary>
        public void ResetInventory()
        {
            currentArmor = null;
            items.Clear();

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetArmorSpeedReduction(0f);
            }

            InitializeStartingItems();

            OnAmmoChanged?.Invoke(AmmoType.Pistol, GetAmmo(AmmoType.Pistol));
            OnAmmoChanged?.Invoke(AmmoType.Rifle, GetAmmo(AmmoType.Rifle));
            OnArmorChanged?.Invoke(null);
            OnInventoryChanged?.Invoke();
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// 탄약 직접 설정 (로드용)
        /// </summary>
        public void SetAmmo(AmmoType type, int amount)
        {
            // 기존 탄약 제거
            var existingAmmo = items.Where(i => i.AmmoData != null && i.AmmoData.ammoType == type).ToList();
            foreach (var item in existingAmmo)
            {
                items.Remove(item);
            }

            // 새로 추가
            if (amount > 0)
            {
                AddAmmo(type, amount);
            }
        }

        /// <summary>
        /// 저장 데이터에서 아이템 추가 (로드용)
        /// </summary>
        public void AddItemFromSaveData(ItemSaveData saveData)
        {
            // ItemData 찾기 (Resources에서)
            ItemData itemData = Resources.Load<ItemData>(saveData.itemName);

            if (itemData != null)
            {
                AddItem(itemData, saveData.amount);
            }
            else
            {
                Debug.LogWarning($"[PlayerInventory] Could not find ItemData: {saveData.itemName}");
            }
        }

        #endregion
    }
}
