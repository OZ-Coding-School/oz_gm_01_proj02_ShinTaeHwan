using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.UI.Inventory
{
    /// <summary>
    /// 퀵슬롯 중앙 관리자
    /// 데이터(ItemData)를 보관하고 양쪽 UI(Inventory, HUD)를 동시에 업데이트
    /// </summary>
    public class QuickSlotManager : MonoBehaviour
    {
        public static QuickSlotManager Instance { get; private set; }

        [Header("Slot Data")]
        [Tooltip("등록된 아이템 데이터 (6개 슬롯)")]
        [SerializeField] private ItemData[] slotData = new ItemData[6];

        [Header("UI References")]
        [Tooltip("인벤토리 캔버스의 퀵슬롯 6개")]
        [SerializeField] private QuickSlot[] inventorySlots;
        
        [Tooltip("HUD의 퀵슬롯 6개")]
        [SerializeField] private QuickSlot[] hudSlots;

        [Header("Settings")]
        [SerializeField] private float highlightDuration = 0.2f;

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
            // 슬롯 인덱스 초기화
            InitializeSlotIndices();
            
            // 인벤토리 변경 이벤트 구독
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += RefreshAllSlots;
            }
            
            Debug.Log($"[QuickSlotManager] Initialized - Inventory: {inventorySlots?.Length ?? 0}, HUD: {hudSlots?.Length ?? 0}");
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshAllSlots;
            }
        }

        /// <summary>
        /// 슬롯 인덱스 자동 설정
        /// </summary>
        private void InitializeSlotIndices()
        {
            for (int i = 0; i < 6; i++)
            {
                if (i < inventorySlots?.Length && inventorySlots[i] != null)
                {
                    inventorySlots[i].SetSlotIndex(i);
                }
                if (i < hudSlots?.Length && hudSlots[i] != null)
                {
                    hudSlots[i].SetSlotIndex(i);
                }
            }
        }

        private void Update()
        {
            HandleInput();
        }

        /// <summary>
        /// 숫자키 입력 처리 (3~8)
        /// </summary>
        private void HandleInput()
        {
            // UI가 열려있으면 입력 무시
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            {
                return;
            }

            // 키 3~8 처리
            for (int i = 0; i < 6; i++)
            {
                KeyCode key = KeyCode.Alpha3 + i;
                if (Input.GetKeyDown(key))
                {
                    Debug.Log($"[QuickSlotManager] Key {key} pressed, using slot {i}");
                    UseSlot(i);
                }
            }
        }

        #region Public API

        /// <summary>
        /// 아이템을 퀵슬롯에 등록
        /// </summary>
        public void RegisterItem(int slotIndex, ItemData itemData)
        {
            if (slotIndex < 0 || slotIndex >= 6) return;

            // 소모품만 허용
            if (itemData != null)
            {
                var itemType = itemData.itemType;
                if (itemType != ItemType.Health && 
                    itemType != ItemType.Food && 
                    itemType != ItemType.Valuable &&
                    itemType != ItemType.Ammo)
                {
                    Debug.Log($"[QuickSlotManager] Cannot register {itemType}. Only consumables allowed.");
                    return;
                }
            }

            slotData[slotIndex] = itemData;
            RefreshSlotUI(slotIndex);
            Debug.Log($"[QuickSlotManager] Registered {itemData?.itemName ?? "null"} to slot {slotIndex}");
        }

        /// <summary>
        /// 퀵슬롯 비우기
        /// </summary>
        public void ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 6) return;

            slotData[slotIndex] = null;
            RefreshSlotUI(slotIndex);
        }

        /// <summary>
        /// 특정 슬롯 아이템 사용
        /// </summary>
        public void UseSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 6) return;

            // HUD 하이라이트
            if (slotIndex < hudSlots?.Length && hudSlots[slotIndex] != null)
            {
                hudSlots[slotIndex].ShowHighlight();
                StartCoroutine(HideHighlightAfterDelay(hudSlots[slotIndex]));
            }

            // 등록된 아이템 확인
            var itemData = slotData[slotIndex];
            if (itemData == null)
            {
                Debug.Log($"[QuickSlotManager] Slot {slotIndex} is empty.");
                return;
            }

            // 인벤토리에서 해당 아이템 찾기
            var inventoryItem = PlayerInventory.Instance?.FindItem(itemData);
            if (inventoryItem == null || inventoryItem.amount <= 0)
            {
                Debug.Log($"[QuickSlotManager] Item not in inventory, clearing slot {slotIndex}");
                ClearSlot(slotIndex);
                return;
            }

            // 아이템 사용
            if (PlayerConsumableSystem.Instance != null)
            {
                PlayerConsumableSystem.Instance.UseItem(inventoryItem);
            }
        }

        /// <summary>
        /// 모든 슬롯 UI 갱신
        /// </summary>
        public void RefreshAllSlots()
        {
            for (int i = 0; i < 6; i++)
            {
                RefreshSlotUI(i);
            }
        }

        /// <summary>
        /// 모든 슬롯 비우기
        /// </summary>
        public void ClearAllSlots()
        {
            for (int i = 0; i < 6; i++)
            {
                slotData[i] = null;
            }
            RefreshAllSlots();
        }

        /// <summary>
        /// 슬롯 데이터 가져오기
        /// </summary>
        public ItemData GetSlotData(int index)
        {
            if (index >= 0 && index < 6)
            {
                return slotData[index];
            }
            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 특정 슬롯 UI 갱신 (양쪽 동시)
        /// </summary>
        private void RefreshSlotUI(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 6) return;

            var itemData = slotData[slotIndex];
            InventoryItem item = null;

            // 등록된 아이템이 있으면 인벤토리에서 찾기
            if (itemData != null && PlayerInventory.Instance != null)
            {
                item = PlayerInventory.Instance.FindItem(itemData);
                
                // 인벤토리에 없으면 슬롯 데이터도 비우기
                if (item == null || item.amount <= 0)
                {
                    slotData[slotIndex] = null;
                    item = null;
                }
            }

            // 양쪽 UI 업데이트
            if (slotIndex < inventorySlots?.Length && inventorySlots[slotIndex] != null)
            {
                inventorySlots[slotIndex].UpdateUI(item);
            }
            if (slotIndex < hudSlots?.Length && hudSlots[slotIndex] != null)
            {
                hudSlots[slotIndex].UpdateUI(item);
            }
        }

        private System.Collections.IEnumerator HideHighlightAfterDelay(QuickSlot slot)
        {
            yield return new WaitForSeconds(highlightDuration);
            slot?.HideHighlight();
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// 퀵슬롯 데이터 저장용
        /// </summary>
        public List<string> GetSaveData()
        {
            List<string> data = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                data.Add(slotData[i]?.itemName ?? "");
            }
            return data;
        }

        /// <summary>
        /// 저장된 퀵슬롯 데이터 로드
        /// </summary>
        public void LoadData(List<string> itemNames)
        {
            Debug.Log($"[QuickSlotManager] LoadData 호출 - 아이템 수: {itemNames?.Count ?? 0}");
            if (itemNames == null) return;

            for (int i = 0; i < 6 && i < itemNames.Count; i++)
            {
                Debug.Log($"[QuickSlotManager] 슬롯 {i} 복원 시도: '{itemNames[i]}'");
                if (!string.IsNullOrEmpty(itemNames[i]) && PlayerInventory.Instance != null)
                {
                    var item = PlayerInventory.Instance.Items.Find(x => x.ItemName == itemNames[i]);
                    if (item != null)
                    {
                        slotData[i] = item.itemData;
                        Debug.Log($"[QuickSlotManager] 슬롯 {i}에 '{itemNames[i]}' 등록 성공");
                    }
                    else
                    {
                        Debug.LogWarning($"[QuickSlotManager] 슬롯 {i}: 인벤토리에서 '{itemNames[i]}' 아이템을 찾을 수 없음");
                    }
                }
            }
            RefreshAllSlots();
            Debug.Log("[QuickSlotManager] LoadData 완료");
        }

        #endregion
    }
}
