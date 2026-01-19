using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Loot;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Weapon;
using MiniExtractionShooter.Level;

namespace MiniExtractionShooter.UI.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject inventoryPanel; // Left
        [SerializeField] private GameObject lootPanel;      // Right

        [Header("Equipment Slots")]
        [SerializeField] private EquipmentSlot armorSlot;
        [SerializeField] private EquipmentSlot primaryWeaponSlot;
        [SerializeField] private EquipmentSlot secondaryWeaponSlot;

        [Header("Grid Inventory")]
        [SerializeField] private Transform gridContent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private int gridSlotCount = 30;

        [Header("Loot Grid")]
        [SerializeField] private Transform lootGridContent;
        [SerializeField] private GameObject lootSlotPrefab;

        [Header("Quick Slots")]
        [SerializeField] private Transform quickSlotContent; // Slots 1-6

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TMPro.TextMeshProUGUI tooltipTitle;
        [SerializeField] private TMPro.TextMeshProUGUI tooltipDescription;


        private List<InventorySlot> gridSlots = new List<InventorySlot>();
        private List<InventorySlot> lootSlots = new List<InventorySlot>();
        private LootableObject currentLootTarget;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeSlots();
        }

        private void Start()
        {
            if (mainPanel != null) mainPanel.SetActive(false);

            // Subscribe to events
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnItemAdded += HandleItemChanged;
                PlayerInventory.Instance.OnItemRemoved += HandleItemChanged;
                PlayerInventory.Instance.OnArmorChanged += HandleArmorChanged;
            }
        }

        private void InitializeSlots()
        {
            // Initialize Grid Slots
            for (int i = 0; i < gridSlotCount; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, gridContent);
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();
                if (slot != null)
                {
                    slot.Initialize(this, i);
                    gridSlots.Add(slot);
                }
            }

            // Initialize Equipment Slots
            if (armorSlot != null) armorSlot.Initialize(this, -1);
            if (primaryWeaponSlot != null) primaryWeaponSlot.Initialize(this, -2);
            if (secondaryWeaponSlot != null) secondaryWeaponSlot.Initialize(this, -3);
        }

        public void ToggleInventory()
        {
            bool isActive = !mainPanel.activeSelf;
            mainPanel.SetActive(isActive);

            if (isActive)
            {
                RefreshInventory();
                // Hide loot panel if just opening inventory
                if (lootPanel != null) lootPanel.SetActive(false);
                // 크로스헤어 숨기고 카메라 마우스 오프셋 비활성화
                SetUIMode(true);
            }
            else
            {
                // 크로스헤어 복원하고 카메라 마우스 오프셋 활성화
                SetUIMode(false);
            }
        }

        public void OpenLoot(LootableObject lootTarget)
        {
            mainPanel.SetActive(true);
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            if (lootPanel != null) lootPanel.SetActive(true);

            currentLootTarget = lootTarget;
            RefreshInventory();
            RefreshLoot();

            // 크로스헤어 숨기고 카메라 마우스 오프셋 비활성화
            SetUIMode(true);
        }

        public void CloseLoot()
        {
            currentLootTarget = null;
            if (lootPanel != null) lootPanel.SetActive(false);
        }

        /// <summary>
        /// 전체 UI 닫기 (LootableObject에서 호출)
        /// </summary>
        public void Close()
        {
            currentLootTarget = null;
            if (mainPanel != null) mainPanel.SetActive(false);
            // 크로스헤어 복원하고 카메라 마우스 오프셋 활성화
            SetUIMode(false);
        }

        /// <summary>
        /// UI 모드 설정 (크로스헤어, 카메라 마우스 오프셋, 마우스 커서)
        /// </summary>
        private void SetUIMode(bool uiOpen)
        {
            // 크로스헤어
            if (DynamicCrosshair.Instance != null)
            {
                DynamicCrosshair.Instance.SetVisible(!uiOpen);
            }

            // 카메라 마우스 오프셋
            CameraFollow cameraFollow = Camera.main?.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetMouseOffsetEnabled(!uiOpen);
            }

            // 마우스 커서 표시
            Cursor.visible = uiOpen;
            Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Confined;

            // 플레이어 컨트롤러
            PlayerController.Instance.SetCanRotate(!uiOpen);
            PlayerCombat.Instance.SetCanShoot(!uiOpen);
        }

        /// <summary>
        /// 아이템 순차 공개 시 호출 (LootableObject에서 호출)
        /// </summary>
        public void RevealItem(int index, LootItem item)
        {
            // 해당 인덱스의 슬롯 공개
            if (index < lootSlots.Count)
            {
                lootSlots[index].SetRevealed();
                Debug.Log($"[InventoryUI] Revealed item at index {index}: {item.ItemName}");
            }
        }

        /// <summary>
        /// UI 새로고침 (LootableObject에서 호출)
        /// </summary>
        public void RefreshUI()
        {
            RefreshInventory();
            RefreshLoot();
        }

        public void RefreshInventory()
        {
            if (PlayerInventory.Instance == null) return;

            // Update Armor
            if (armorSlot != null && PlayerInventory.Instance.CurrentArmor != null)
            {
                ArmorData armor = PlayerInventory.Instance.CurrentArmor;
                InventoryItem armorItem = new InventoryItem(armor, 1);
                armorSlot.SetItem(armorItem);
            }
            else if (armorSlot != null)
            {
                armorSlot.ClearSlot();
            }

            // Update Weapons from WeaponManager
            UpdateWeaponSlots();

            // Update Grid - directly from PlayerInventory.Items
            List<InventoryItem> displayItems = PlayerInventory.Instance.Items;

            for (int i = 0; i < gridSlots.Count; i++)
            {
                if (i < displayItems.Count)
                {
                    gridSlots[i].SetItem(displayItems[i]);
                }
                else
                {
                    gridSlots[i].ClearSlot();
                }
            }
        }

        /// <summary>
        /// WeaponManager에서 무기 정보를 가져와 장비 슬롯 업데이트
        /// </summary>
        private void UpdateWeaponSlots()
        {
            if (WeaponManager.Instance == null) return;

            // Primary Weapon (Slot 0)
            if (primaryWeaponSlot != null)
            {
                WeaponData primaryWeapon = GetWeaponAtSlot(0);
                if (primaryWeapon != null)
                {
                    InventoryItem weaponItem = new InventoryItem(primaryWeapon, 1);
                    primaryWeaponSlot.SetItem(weaponItem);
                }
                else
                {
                    primaryWeaponSlot.ClearSlot();
                }
            }

            // Secondary Weapon (Slot 1)
            if (secondaryWeaponSlot != null)
            {
                WeaponData secondaryWeapon = GetWeaponAtSlot(1);
                if (secondaryWeapon != null)
                {
                    InventoryItem weaponItem = new InventoryItem(secondaryWeapon, 1);
                    secondaryWeaponSlot.SetItem(weaponItem);
                }
                else
                {
                    secondaryWeaponSlot.ClearSlot();
                }
            }
        }

        /// <summary>
        /// WeaponManager에서 특정 슬롯의 무기 가져오기
        /// </summary>
        private WeaponData GetWeaponAtSlot(int slot)
        {
            if (WeaponManager.Instance == null) return null;

            string weaponName = slot == 0 
                ? WeaponManager.Instance.GetPrimaryWeaponName() 
                : WeaponManager.Instance.GetSecondaryWeaponName();

            if (string.IsNullOrEmpty(weaponName)) return null;

            return WeaponManager.Instance.FindWeaponByName(weaponName);
        }


        public void RefreshLoot()
        {
            if (currentLootTarget == null) return;

            // Clear existing loot slots
            foreach (Transform child in lootGridContent)
            {
                Destroy(child.gameObject);
            }
            lootSlots.Clear();

            // Create new slots
            List<LootItem> lootItems = currentLootTarget.Items;
            int revealedCount = currentLootTarget.RevealedCount;

            for (int i = 0; i < lootItems.Count; i++)
            {
                LootItem lItem = lootItems[i];
                
                // LootItem에서 직접 InventoryItem 생성
                InventoryItem iItem;
                
                if (lItem.itemData != null)
                {
                    iItem = new InventoryItem(lItem.itemData, lItem.amount);
                }
                else
                {
                    // ItemData가 없는 경우 - 빈 InventoryItem
                    iItem = new InventoryItem() { amount = lItem.amount };
                }

                GameObject slotObj = Instantiate(lootSlotPrefab, lootGridContent);
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();
                if (slot != null)
                {
                    slot.Initialize(this, 1000 + i);
                    slot.SetItem(iItem);
                    
                    // 아직 공개되지 않은 아이템은 숨김 처리
                    if (i >= revealedCount)
                    {
                        slot.SetHidden();
                    }
                    
                    lootSlots.Add(slot);
                }
            }
        }

        public void HandleItemDrop(InventorySlot source, InventorySlot destination)
        {
            if (source == destination) return;

            bool sourceIsLoot = lootSlots.Contains(source);
            bool destIsEquipment = destination is EquipmentSlot;

            // 1. Loot -> Inventory/Equipment
            if (sourceIsLoot)
            {
                int lootIndex = source.SlotIndex - 1000;

                // If dropping to Equipment Slot
                if (destIsEquipment)
                {
                    EquipmentSlot eqSlot = (EquipmentSlot)destination;
                    if (eqSlot.CanAccept(source.CurrentItem))
                    {
                        // Take item from loot
                        if (currentLootTarget != null)
                        {
                            // This logic assumes we can take specific item and it goes to inventory automatically
                            // We might need to intervene to equip it immediately if it goes to inventory first.
                            // But LootableObject.TakeItem adds to Inventory. 
                            // So we let it add to inventory, then try to equip it from inventory?
                            // Simpler: Just TakeItem. If it's Armor/Weapon, LootableObject logic might auto-equip if better/empty.
                            // Let's rely on TakeItem for now.
                            currentLootTarget.TakeItem(lootIndex);
                            OpenLoot(currentLootTarget);
                        }
                    }
                }
                // If dropping to Grid
                else
                {
                    if (currentLootTarget != null)
                    {
                        currentLootTarget.TakeItem(lootIndex);
                        OpenLoot(currentLootTarget);
                    }
                }
            }
            // 2. Inventory -> Equipment
            else if (!sourceIsLoot && destIsEquipment)
            {
                EquipmentSlot eqSlot = (EquipmentSlot)destination;
                InventoryItem item = source.CurrentItem;

                if (item != null && eqSlot.CanAccept(item))
                {
                    if (item.ItemType == ItemType.Armor)
                    {
                        PlayerInventory.Instance.EquipArmor(item.ArmorData);
                        // Remove from inventory list if it was in list? 
                        // PlayerInventory.EquipArmor doesn't remove from list. 
                        // We need to manage the list. 
                        // Usually Equipment is NOT in the list.
                        // So we remove from list, set to equipment.
                        PlayerInventory.Instance.RemoveItem(item);
                        RefreshInventory();
                    }
                    else if (item.ItemType == ItemType.Weapon)
                    {
                        if (eqSlot.EquipmentIndex == 0) // Primary
                        {
                            WeaponManager.Instance.SetPrimaryWeapon(item.WeaponData);
                            // Handle old weapon? WeaponManager.PickupWeapon logic is complex.
                            // For simplicity, just set. Real game needs swap logic.
                        }
                        else if (eqSlot.EquipmentIndex == 1) // Secondary
                        {
                            WeaponManager.Instance.SetSecondaryWeapon(item.WeaponData);
                        }

                        PlayerInventory.Instance.Items.Remove(item);
                        RefreshInventory();
                        // Also notify WeaponManager to equip/refresh
                        WeaponManager.Instance.SwitchToWeapon(WeaponManager.Instance.CurrentSlot);
                    }
                }
            }
            // 3. Equipment -> Inventory (Unequip)
            else if (source is EquipmentSlot && !destIsEquipment)
            {
                EquipmentSlot eqSource = (EquipmentSlot)source;
                InventoryItem item = source.CurrentItem;

                if (item != null)
                {
                    // Add to inventory
                    PlayerInventory.Instance.Items.Add(item);

                    // Clear Equipment
                    if (eqSource.AcceptedType == ItemType.Armor)
                    {
                        PlayerInventory.Instance.EquipArmor(null);
                    }
                    else if (eqSource.AcceptedType == ItemType.Weapon)
                    {
                        if (eqSource.EquipmentIndex == 0) WeaponManager.Instance.SetPrimaryWeapon(null);
                        else WeaponManager.Instance.SetSecondaryWeapon(null);
                        WeaponManager.Instance.SwitchToWeapon(WeaponManager.Instance.CurrentSlot);
                    }

                    RefreshInventory();
                }
            }
        }

        public void SelectItem(InventorySlot slot)
        {
            // 루팅 슬롯인 경우 클릭으로 아이템 획득
            if (lootSlots.Contains(slot) && currentLootTarget != null)
            {
                // 공개되지 않은 아이템은 획득 불가
                if (!slot.IsRevealed)
                {
                    Debug.Log("[InventoryUI] Cannot take unrevealed item");
                    return;
                }

                int lootIndex = slot.SlotIndex - 1000;
                if (lootIndex >= 0)
                {
                    bool success = currentLootTarget.TakeItem(lootIndex);
                    Debug.Log($"[InventoryUI] TakeItem result: {success}");
                }
            }
            else
            {
                // 인벤토리 슬롯 - 상세 정보 표시 (향후 구현)
                Debug.Log($"Selected item: {slot.CurrentItem?.ItemName}");
            }
        }

        public void ShowItemActions(InventorySlot slot)
        {
            // 우클릭 컨텍스트 메뉴 (사용, 버리기 등) - 향후 구현
            if (slot.CurrentItem != null)
            {
                Debug.Log($"Show actions for: {slot.CurrentItem.ItemName}");
                
                // 루팅 슬롯이면 우클릭도 획득으로 처리
                if (lootSlots.Contains(slot) && currentLootTarget != null)
                {
                    // 공개되지 않은 아이템은 획득 불가
                    if (!slot.IsRevealed)
                    {
                        Debug.Log("[InventoryUI] Cannot take unrevealed item");
                        return;
                    }

                    int lootIndex = slot.SlotIndex - 1000;
                    if (lootIndex >= 0)
                    {
                        currentLootTarget.TakeItem(lootIndex);
                    }
                }
            }
        }

        private void HandleItemChanged(InventoryItem item) => RefreshInventory();
        private void HandleArmorChanged(ArmorData armor) => RefreshInventory();

        /// <summary>
        /// 툴팁 표시
        /// </summary>
        public void ShowTooltip(InventorySlot slot)
        {
            if (tooltipPanel == null || slot.CurrentItem == null) return;

            tooltipPanel.SetActive(true);

            if (tooltipTitle != null)
            {
                tooltipTitle.text = slot.CurrentItem.ItemName;
            }

            if (tooltipDescription != null)
            {
                string desc = $"타입: {slot.CurrentItem.ItemType}";
                if (slot.CurrentItem.amount > 1)
                {
                    desc += $"\n수량: {slot.CurrentItem.amount}";
                }
                tooltipDescription.text = desc;
            }

            // 마우스 위치에 툴팁 표시
            if (tooltipPanel.TryGetComponent<RectTransform>(out var tooltipRect))
            {
                tooltipRect.position = Input.mousePosition + new Vector3(15, -15, 0);
            }
        }

        /// <summary>
        /// 툴팁 숨기기
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
    }
}
