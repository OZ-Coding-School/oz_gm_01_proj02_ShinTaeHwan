using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Core;

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
        public ConsumableData ConsumableData => itemData as ConsumableData;

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

        [Header("Inventory Slots")]
        [SerializeField] private int maxInventorySlots = 30;
        private InventoryItem[] slots; // 고정 크기 배열 (슬롯 인덱스 기반)

        [Header("Starting Items")]
        [SerializeField] private AmmoData pistolAmmoData;
        [SerializeField] private AmmoData rifleAmmoData;
        [SerializeField] private int startingPistolAmmo = 36;

        // Events
        public event System.Action<AmmoType, int> OnAmmoChanged;
        public event System.Action<ArmorData> OnArmorChanged;
        public event System.Action<InventoryItem> OnItemAdded;
        public event System.Action<InventoryItem> OnItemRemoved;
        public event System.Action OnInventoryChanged;

        public ArmorData CurrentArmor => currentArmor;
        
        /// <summary>
        /// 슬롯 배열 반환 (null 포함)
        /// </summary>
        public InventoryItem[] Slots => slots;
        
        /// <summary>
        /// 하위 호환을 위한 Items 리스트 (null 제외한 아이템만)
        /// </summary>
        public List<InventoryItem> Items
        {
            get
            {
                var list = new List<InventoryItem>();
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null)
                    {
                        list.Add(slots[i]);
                    }
                }
                return list;
            }
        }

        public int MaxSlots => maxInventorySlots;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // 슬롯 배열 초기화
                slots = new InventoryItem[maxInventorySlots];
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 저장 파일이 없을 때만 시작 아이템 추가
            if (SaveDataManager.Instance == null || !SaveDataManager.Instance.HasSaveFile())
            {
                InitializeStartingItems();
            }
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
        /// ItemData로 아이템 추가 (빈 슬롯에 자동 배치)
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

            // 빈 슬롯 찾기
            int emptySlot = FindFirstEmptySlot();
            if (emptySlot < 0) 
            {
                Debug.LogWarning("[PlayerInventory] No empty slots available!");
                return false; // 인벤토리 가득 참
            }
            InventoryItem newItem = new InventoryItem(itemData, amount);
            slots[emptySlot] = newItem;
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
        /// 특정 슬롯에 아이템 추가
        /// </summary>
        public bool AddItemToSlot(int slotIndex, ItemData itemData, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length) return false;
            if (itemData == null || amount <= 0) return false;
            if (slots[slotIndex] != null) return false; // 슬롯이 이미 사용 중

            InventoryItem newItem = new InventoryItem(itemData, amount);
            slots[slotIndex] = newItem;
            OnItemAdded?.Invoke(newItem);
            OnInventoryChanged?.Invoke();

            if (itemData is AmmoData ammoData)
            {
                OnAmmoChanged?.Invoke(ammoData.ammoType, GetAmmo(ammoData.ammoType));
            }

            return true;
        }

        /// <summary>
        /// 첫 번째 빈 슬롯 인덱스 찾기
        /// </summary>
        public int FindFirstEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    return i;
                }
            }
            return -1; // 빈 슬롯 없음
        }

        /// <summary>
        /// 두 슬롯의 아이템 교환
        /// </summary>
        public void SwapSlots(int fromIndex, int toIndex)
        {
            // Debug.Log($"[PlayerInventory] SwapSlots called: from={fromIndex}, to={toIndex}");
            
            if (fromIndex < 0 || fromIndex >= slots.Length)
            {
                Debug.LogWarning($"[PlayerInventory] Invalid fromIndex: {fromIndex}");
                return;
            }
            if (toIndex < 0 || toIndex >= slots.Length)
            {
                Debug.LogWarning($"[PlayerInventory] Invalid toIndex: {toIndex}");
                return;
            }
            if (fromIndex == toIndex) return;

            var fromItem = slots[fromIndex];
            var toItem = slots[toIndex];
            // Debug.Log($"[PlayerInventory] Swapping: [{fromIndex}]={fromItem?.ItemName ?? "null"} <-> [{toIndex}]={toItem?.ItemName ?? "null"}");

            slots[toIndex] = fromItem;
            slots[fromIndex] = toItem;
            
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 아이템 슬롯 인덱스 찾기
        /// </summary>
        public int GetSlotIndex(InventoryItem item)
        {
            if (item == null) return -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == item)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 특정 슬롯 비우기
        /// </summary>
        public void ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length) return;
            
            var item = slots[slotIndex];
            if (item != null)
            {
                slots[slotIndex] = null;
                OnItemRemoved?.Invoke(item);
                OnInventoryChanged?.Invoke();
            }
        }

        /// <summary>
        /// 특정 슬롯의 아이템 가져오기
        /// </summary>
        public InventoryItem GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length) return null;
            return slots[slotIndex];
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
                Debug.LogWarning($"[PlayerInventory] LootEntry has no ItemData: {lootEntry.ItemName}");
            }
        }

        /// <summary>
        /// 아이템 제거 (슬롯을 null로 설정)
        /// </summary>
        public bool RemoveItem(InventoryItem item)
        {
            int slotIndex = GetSlotIndex(item);
            if (slotIndex >= 0)
            {
                slots[slotIndex] = null;
                OnItemRemoved?.Invoke(item);
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 아이템 수량 감소 (슬롯 위치 유지)
        /// </summary>
        public bool RemoveItemAmount(InventoryItem item, int amount)
        {
            int slotIndex = GetSlotIndex(item);
            if (slotIndex < 0) return false;

            item.amount -= amount;
            if (item.amount <= 0)
            {
                slots[slotIndex] = null; // 슬롯은 null로 설정 (위치 영향 X)
                OnItemRemoved?.Invoke(item);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// ItemData로 아이템 찾기 (슬롯 배열에서 검색)
        /// </summary>
        public InventoryItem FindItem(ItemData itemData)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].itemData == itemData)
                {
                    return slots[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 아이템 타입으로 찾기
        /// </summary>
        public InventoryItem FindItemByType(ItemType type)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].ItemType == type)
                {
                    return slots[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 모든 아이템 비우기 (모든 슬롯 null로)
        /// </summary>
        public void ClearItems()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = null;
            }
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
            for (int i = 0; i < slots.Length; i++)
            {
                var item = slots[i];
                if (item != null && item.AmmoData != null && item.AmmoData.ammoType == type)
                {
                    total += item.amount;
                }
            }
            return total;
        }

        /// <summary>
        /// 탄약 사용 (슬롯 위치 유지)
        /// </summary>
        public bool UseAmmo(AmmoType type, int amount)
        {
            if (GetAmmo(type) < amount) return false;

            int remaining = amount;
            
            // 탄약 아이템이 있는 슬롯 찾기
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                var item = slots[i];
                if (item != null && item.AmmoData != null && item.AmmoData.ammoType == type)
                {
                    int toUse = Mathf.Min(item.amount, remaining);
                    item.amount -= toUse;
                    remaining -= toUse;

                    if (item.amount <= 0)
                    {
                        slots[i] = null; // 슬롯을 null로 설정 (위치 유지)
                        OnItemRemoved?.Invoke(item);
                    }
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
            for (int i = 0; i < slots.Length; i++)
            {
                var item = slots[i];
                if (item != null && item.AmmoData != null && item.AmmoData.ammoType == type)
                {
                    return item.AmmoData;
                }
            }

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
        /// 회복 아이템 사용 - PlayerConsumableSystem에 위임
        /// </summary>
        public bool UseHealthItem()
        {
            InventoryItem healthItem = FindItemByType(ItemType.Health);
            if (healthItem != null && PlayerConsumableSystem.Instance != null)
            {
                return PlayerConsumableSystem.Instance.UseItem(healthItem);
            }
            return false;
        }

        /// <summary>
        /// 인벤토리 초기화
        /// </summary>
        public void ResetInventory()
        {
            currentArmor = null;
            ClearItems(); // slots 배열 초기화

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
        /// 탄약 직접 설정 (로드용, 슬롯 위치 유지)
        /// </summary>
        public void SetAmmo(AmmoType type, int amount)
        {
            // 기존 탄약 제거 (슬롯 위치는 null로 설정)
            for (int i = 0; i < slots.Length; i++)
            {
                var item = slots[i];
                if (item != null && item.AmmoData != null && item.AmmoData.ammoType == type)
                {
                    slots[i] = null;
                }
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
            // Debug.Log($"[PlayerInventory] AddItemFromSaveData 호출: itemName={saveData.itemName}, amount={saveData.amount}");

            // ItemDatabase를 통해 아이템 찾기
            ItemData itemData = ItemDatabase.Instance.GetItemByName(saveData.itemName);

            if (itemData != null)
            {
                // Debug.Log($"[PlayerInventory] ItemData 로드 성공: {itemData.itemName}");
                AddItem(itemData, saveData.amount);
            }
            else
            {
                Debug.LogError($"[PlayerInventory] ItemDatabase에서 아이템을 찾을 수 없음: '{saveData.itemName}'. ItemDatabase 에셋이 Resources 폴더에 있는지, 그리고 아이템이 등록되었는지 확인하세요.");
            }
        }

        #endregion
    }
}
