using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Loot;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Weapon;
using MiniExtractionShooter.Level;
using MiniExtractionShooter.Managers;

namespace MiniExtractionShooter.UI.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        /// <summary>
        /// 인벤토리 UI가 열려있는지 여부 (mainPanel 기준)
        /// </summary>
        public bool IsOpen => mainPanel != null && mainPanel.activeSelf;

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
        private LootBox currentLootTarget;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeSlots();
        }

        private void Start()
        {
            if (mainPanel != null) mainPanel.SetActive(false);

            // UIStateManager에 닫기 콜백 등록 (Map 열기 시 자동 닫힘)
            UIStateManager.Instance?.RegisterCloseCallback("Inventory", () => {
                if (mainPanel != null && mainPanel.activeSelf)
                {
                    Close();
                }
            });

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
                // UIStateManager로 플레이어 컨트롤 비활성화
                UIStateManager.Instance?.OpenUI("Inventory");
            }
            else
            {
                // 루팅 중이면 루팅 중지
                StopCurrentLooting();
                // UIStateManager로 플레이어 컨트롤 활성화
                UIStateManager.Instance?.CloseUI("Inventory");
            }
        }

        public void OpenLoot(LootBox lootTarget)
        {
            mainPanel.SetActive(true);
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            if (lootPanel != null) lootPanel.SetActive(true);

            currentLootTarget = lootTarget;
            RefreshInventory();
            RefreshLoot();

            // UIStateManager로 플레이어 컨트롤 비활성화
            UIStateManager.Instance?.OpenUI("Inventory");
        }

        public void CloseLoot()
        {
            currentLootTarget = null;
            if (lootPanel != null) lootPanel.SetActive(false);
        }

        /// <summary>
        /// 전체 UI 닫기 (LootBox에서 호출)
        /// </summary>
        public void Close()
        {
            // 루팅 중이면 루팅 중지
            StopCurrentLooting();
            
            if (mainPanel != null) mainPanel.SetActive(false);
            // UIStateManager로 플레이어 컨트롤 활성화
            UIStateManager.Instance?.CloseUI("Inventory");
        }

        /// <summary>
        /// 현재 루팅 중이면 중지 (내부 사용)
        /// </summary>
        private void StopCurrentLooting()
        {
            if (currentLootTarget != null)
            {
                // LootBox의 루팅 상태 종료 (isLooting = false)
                if (currentLootTarget.IsLooting)
                {
                    // 코루틴 정지 및 상태 정리는 LootBox가 처리
                    currentLootTarget.ForceStopLooting();
                }
                currentLootTarget = null;
                if (lootPanel != null) lootPanel.SetActive(false);
            }
        }



        /// <summary>
        /// 아이템 순차 공개 시 호출 (LootBox에서 호출)
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
        /// UI 새로고침 (LootBox에서 호출)
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

            // Update Grid - 슬롯 배열 기반으로 접근
            var slots = PlayerInventory.Instance.Slots;
            int maxSlots = Mathf.Min(gridSlots.Count, slots?.Length ?? 0);

            for (int i = 0; i < gridSlots.Count; i++)
            {
                if (i < maxSlots && slots[i] != null)
                {
                    gridSlots[i].SetItem(slots[i]);
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

            return slot == 0 
                ? WeaponManager.Instance.GetPrimaryWeapon() 
                : WeaponManager.Instance.GetSecondaryWeapon();
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
            bool sourceIsEquipment = source is EquipmentSlot;
            bool destIsLoot = lootSlots.Contains(destination);

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
                        if (currentLootTarget != null)
                        {
                            currentLootTarget.TakeItem(lootIndex);
                            OpenLoot(currentLootTarget);
                        }
                    }
                }
                // If dropping to Grid
                else if (!destIsLoot)
                {
                    if (currentLootTarget != null)
                    {
                        currentLootTarget.TakeItem(lootIndex);
                        OpenLoot(currentLootTarget);
                    }
                }
            }
            // 2. Inventory Grid -> Equipment
            else if (!sourceIsLoot && !sourceIsEquipment && destIsEquipment)
            {
                EquipmentSlot eqSlot = (EquipmentSlot)destination;
                InventoryItem item = source.CurrentItem;

                if (item != null && eqSlot.CanAccept(item))
                {
                    if (item.ItemType == ItemType.Armor)
                    {
                        // 1. 기존 장착 방어구 백업
                        ArmorData oldArmor = PlayerInventory.Instance.CurrentArmor;

                        // 2. 인벤토리에서 새 아이템 제거
                        PlayerInventory.Instance.RemoveItem(item);

                        // 3. 새 방어구 장착
                        PlayerInventory.Instance.EquipArmor(item.ArmorData);

                        // 4. 기존 방어구 반환 (교체)
                        if (oldArmor != null)
                        {
                            // 원래 위치에 넣기 시도
                            if (!PlayerInventory.Instance.AddItemToSlot(source.SlotIndex, oldArmor, 1))
                            {
                                // 원래 위치가 차있으면(거의 없겠지만) 빈 슬롯에 추가
                                if (!PlayerInventory.Instance.AddItem(oldArmor, 1))
                                {
                                    // 인벤토리 꽉 참 - 바닥에 드랍하거나 경고 (여기선 로그만)
                                    Debug.LogWarning("[InventoryUI] Inventory full. Swapped armor lost.");
                                }
                            }
                        }

                        RefreshInventory();
                    }
                    else if (item.ItemType == ItemType.Weapon)
                    {
                        // 1. 기존 장착 무기 백업
                        WeaponData oldWeapon = null;
                        if (eqSlot.EquipmentIndex == 0) oldWeapon = WeaponManager.Instance.GetPrimaryWeapon();
                        else if (eqSlot.EquipmentIndex == 1) oldWeapon = WeaponManager.Instance.GetSecondaryWeapon();

                        // 2. 인벤토리에서 새 아이템 제거
                        PlayerInventory.Instance.RemoveItem(item);

                        // 3. 새 무기 장착
                        if (eqSlot.EquipmentIndex == 0) 
                        {
                            WeaponManager.Instance.SetPrimaryWeapon(item.WeaponData);
                        }
                        else if (eqSlot.EquipmentIndex == 1) 
                        {
                            WeaponManager.Instance.SetSecondaryWeapon(item.WeaponData);
                        }

                        // 4. 기존 무기 반환 (교체)
                        if (oldWeapon != null)
                        {
                            // 원래 위치에 넣기 시도
                            if (!PlayerInventory.Instance.AddItemToSlot(source.SlotIndex, oldWeapon, 1))
                            {
                                // 원래 위치가 차있으면 빈 슬롯에 추가
                                if (!PlayerInventory.Instance.AddItem(oldWeapon, 1))
                                {
                                    Debug.LogWarning("[InventoryUI] Inventory full. Swapped weapon lost.");
                                }
                            }
                        }

                        RefreshInventory();
                        WeaponManager.Instance.SwitchToWeapon(WeaponManager.Instance.CurrentSlot);
                    }
                }
            }
            // 3. Equipment -> Inventory (Unequip)
            else if (sourceIsEquipment && !destIsEquipment && !destIsLoot)
            {
                EquipmentSlot eqSource = (EquipmentSlot)source;
                InventoryItem item = source.CurrentItem;

                if (item != null)
                {
                    // Add to inventory at destination slot if empty, otherwise find empty slot
                    int targetSlotIndex = destination.SlotIndex;
                    
                    // 목표 슬롯이 비어있으면 바로 이동 (단순 해제)
                    if (PlayerInventory.Instance.GetSlot(targetSlotIndex) == null)
                    {
                        PlayerInventory.Instance.AddItemToSlot(targetSlotIndex, item.itemData, item.amount);
                        UnequipSourceSlot(eqSource);
                        RefreshInventory();
                    }
                    // 목표 슬롯에 아이템이 있으면? (스왑)
                    else
                    {
                        InventoryItem destItem = PlayerInventory.Instance.GetSlot(targetSlotIndex);
                        
                        // 목표 아이템이 장착 가능한 타입인지 확인
                        if (eqSource.CanAccept(destItem))
                        {
                            // 1. 장착 해제할 아이템(A) 백업 (item 변수)
                            // 2. 인벤토리의 아이템(B)을 장착 슬롯으로 이동
                            // 3. A를 인벤토리 슬롯으로 이동

                            // 인벤토리에서 B 제거
                            PlayerInventory.Instance.RemoveItem(destItem);

                            // B 장착
                            EquipItemToSlot(eqSource, destItem);

                            // A를 인벤토리 슬롯에 추가
                             PlayerInventory.Instance.AddItemToSlot(targetSlotIndex, item.itemData, item.amount);

                            RefreshInventory();
                        }
                        else
                        {
                            // 교체 불가 - 빈 슬롯 찾아서 단순 해제
                            int emptySlot = PlayerInventory.Instance.FindFirstEmptySlot();
                            if (emptySlot >= 0)
                            {
                                PlayerInventory.Instance.AddItemToSlot(emptySlot, item.itemData, item.amount);
                                UnequipSourceSlot(eqSource);
                                RefreshInventory();
                            }
                            else
                            {
                                Debug.LogWarning("[InventoryUI] Inventory full. Cannot unequip.");
                            }
                        }
                    }
                }
            }
            // 4. Inventory Grid <-> Grid (슬롯 교환)
            else if (!sourceIsLoot && !sourceIsEquipment && !destIsEquipment && !destIsLoot)
            {
                int fromIndex = source.SlotIndex;
                int toIndex = destination.SlotIndex;
                
                Debug.Log($"[InventoryUI] HandleItemDrop Grid<->Grid: source.SlotIndex={fromIndex}, dest.SlotIndex={toIndex}");
                
                // 슬롯 교환 (비어있어도 교환, 아이템이 있어도 교환)
                PlayerInventory.Instance.SwapSlots(fromIndex, toIndex);
                RefreshInventory();
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
        /// <summary>
        /// 장비 슬롯 아이템 해제 (데이터 처리)
        /// </summary>
        private void UnequipSourceSlot(EquipmentSlot eqSource)
        {
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
        }

        /// <summary>
        /// 아이템을 장비 슬롯에 장착 (데이터 처리)
        /// </summary>
        private void EquipItemToSlot(EquipmentSlot eqSlot, InventoryItem item)
        {
            if (item.ItemType == ItemType.Armor)
            {
                PlayerInventory.Instance.EquipArmor(item.ArmorData);
            }
            else if (item.ItemType == ItemType.Weapon)
            {
                if (eqSlot.EquipmentIndex == 0) WeaponManager.Instance.SetPrimaryWeapon(item.WeaponData);
                else WeaponManager.Instance.SetSecondaryWeapon(item.WeaponData);
                
                WeaponManager.Instance.SwitchToWeapon(WeaponManager.Instance.CurrentSlot);
            }
        }
    }
}
