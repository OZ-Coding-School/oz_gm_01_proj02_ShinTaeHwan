using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Player;

namespace MiniExtractionShooter.UI.Inventory
{
    /// <summary>
    /// 퀵슬롯 전체 관리 (6개 슬롯, 키 3~8)
    /// </summary>
    public class QuickSlotManager : MonoBehaviour
    {
        public static QuickSlotManager Instance { get; private set; }

        [Header("Quick Slots")]
        [SerializeField] private List<QuickSlot> quickSlots = new List<QuickSlot>();

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
            // 인벤토리 변경 이벤트 구독
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnItemAdded += OnInventoryChanged;
                PlayerInventory.Instance.OnItemRemoved += OnInventoryChanged;
            }
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnItemAdded -= OnInventoryChanged;
                PlayerInventory.Instance.OnItemRemoved -= OnInventoryChanged;
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
            if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.activeInHierarchy)
            {
                return;
            }

            // 키 3~8 처리
            for (int i = 0; i < quickSlots.Count && i < 6; i++)
            {
                KeyCode key = KeyCode.Alpha3 + i;
                if (Input.GetKeyDown(key))
                {
                    UseSlot(i);
                }
            }
        }

        /// <summary>
        /// 특정 슬롯 아이템 사용
        /// </summary>
        public void UseSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= quickSlots.Count)
            {
                Debug.LogWarning($"[QuickSlotManager] Invalid slot index: {slotIndex}");
                return;
            }

            QuickSlot slot = quickSlots[slotIndex];
            if (slot != null)
            {
                // 하이라이트 피드백
                slot.ShowHighlight();
                StartCoroutine(HideHighlightAfterDelay(slot));

                // 아이템 사용
                slot.UseItem();
            }
        }

        private System.Collections.IEnumerator HideHighlightAfterDelay(QuickSlot slot)
        {
            yield return new WaitForSeconds(highlightDuration);
            slot?.HideHighlight();
        }

        /// <summary>
        /// 인벤토리 변경 시 모든 퀵슬롯 업데이트
        /// </summary>
        private void OnInventoryChanged(InventoryItem item)
        {
            RefreshAllSlots();
        }

        /// <summary>
        /// 모든 퀵슬롯 수량 갱신
        /// </summary>
        public void RefreshAllSlots()
        {
            foreach (var slot in quickSlots)
            {
                if (slot != null)
                {
                    slot.UpdateAmount();
                }
            }
        }

        /// <summary>
        /// 모든 퀵슬롯 비우기
        /// </summary>
        public void ClearAllSlots()
        {
            foreach (var slot in quickSlots)
            {
                if (slot != null)
                {
                    slot.ClearSlot();
                }
            }
        }

        /// <summary>
        /// 특정 슬롯 가져오기
        /// </summary>
        public QuickSlot GetSlot(int index)
        {
            if (index >= 0 && index < quickSlots.Count)
            {
                return quickSlots[index];
            }
            return null;
        }

        #region Save/Load Support

        /// <summary>
        /// 퀵슬롯 데이터 저장용 (아이템 이름 리스트)
        /// </summary>
        public List<string> GetSaveData()
        {
            List<string> data = new List<string>();
            foreach (var slot in quickSlots)
            {
                if (slot != null && slot.HasItem)
                {
                    data.Add(slot.LinkedItem.ItemName);
                }
                else
                {
                    data.Add("");
                }
            }
            return data;
        }

        /// <summary>
        /// 저장된 퀵슬롯 데이터 로드
        /// </summary>
        public void LoadData(List<string> itemNames)
        {
            if (itemNames == null) return;

            for (int i = 0; i < quickSlots.Count && i < itemNames.Count; i++)
            {
                if (!string.IsNullOrEmpty(itemNames[i]) && PlayerInventory.Instance != null)
                {
                    // 인벤토리에서 해당 아이템 찾기
                    var item = PlayerInventory.Instance.Items.Find(x => x.ItemName == itemNames[i]);
                    if (item != null)
                    {
                        quickSlots[i].SetLinkedItem(item);
                    }
                }
            }
        }

        #endregion
    }
}
