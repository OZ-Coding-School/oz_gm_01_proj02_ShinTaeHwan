using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.UI.Inventory
{
    /// <summary>
    /// 퀵슬롯 - 인벤토리 아이템을 등록하고 숫자키로 빠르게 사용
    /// </summary>
    public class QuickSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private TextMeshProUGUI slotNumberText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject highlight;

        [Header("Settings")]
        [SerializeField] private int slotIndex = 0; // 0~5 (키 3~8)

        [Header("Colors")]
        [SerializeField] private Color emptyBackgroundColor = new Color(0.2f, 0.2f, 0.24f, 0.7f);
        [SerializeField] private Color filledBackgroundColor = new Color(0.2f, 0.25f, 0.3f, 0.85f);

        private InventoryItem linkedItem; // 연결된 인벤토리 아이템

        public int SlotIndex => slotIndex;
        public int KeyNumber => slotIndex + 3; // 슬롯 0 = 키 3
        public InventoryItem LinkedItem => linkedItem;
        public bool HasItem => linkedItem != null && linkedItem.itemData != null;

        private void Awake()
        {
            // 슬롯 번호 표시 (3~8)
            if (slotNumberText != null)
            {
                slotNumberText.text = KeyNumber.ToString();
            }

            ClearSlot();
        }

        /// <summary>
        /// 아이템 연결 (인벤토리에서 드래그)
        /// </summary>
        public void SetLinkedItem(InventoryItem item)
        {
            if (item == null || item.itemData == null)
            {
                ClearSlot();
                return;
            }

            // 소모품/회복 아이템만 허용
            if (item.ItemType != ItemType.Health && 
                item.ItemType != ItemType.Valuable &&
                item.ItemType != ItemType.Ammo)
            {
                Debug.Log($"[QuickSlot] Cannot add {item.ItemType} to quick slot. Only consumables allowed.");
                return;
            }

            linkedItem = item;

            // 아이콘 표시
            if (iconImage != null)
            {
                if (item.Icon != null)
                {
                    iconImage.sprite = item.Icon;
                    iconImage.color = Color.white;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }

            // 수량 표시
            UpdateAmount();

            // 배경색 변경
            if (backgroundImage != null)
            {
                backgroundImage.color = filledBackgroundColor;
            }
        }

        /// <summary>
        /// 수량 업데이트 (인벤토리 변경 시 호출)
        /// </summary>
        public void UpdateAmount()
        {
            if (linkedItem == null || linkedItem.itemData == null)
            {
                ClearSlot();
                return;
            }

            // 인벤토리에서 실제 수량 확인
            if (PlayerInventory.Instance != null)
            {
                var inventoryItem = PlayerInventory.Instance.FindItem(linkedItem.itemData);
                if (inventoryItem != null)
                {
                    linkedItem.amount = inventoryItem.amount;

                    if (amountText != null)
                    {
                        amountText.text = linkedItem.amount.ToString();
                        amountText.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // 인벤토리에 아이템이 없으면 슬롯 비우기
                    ClearSlot();
                }
            }
        }

        /// <summary>
        /// 슬롯 비우기
        /// </summary>
        public void ClearSlot()
        {
            linkedItem = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = Color.clear;
                iconImage.gameObject.SetActive(false);
            }

            if (amountText != null)
            {
                amountText.gameObject.SetActive(false);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = emptyBackgroundColor;
            }
        }

        /// <summary>
        /// 아이템 사용
        /// </summary>
        public bool UseItem()
        {
            if (!HasItem)
            {
                Debug.Log($"[QuickSlot] Slot {KeyNumber} is empty.");
                return false;
            }

            // 인벤토리에서 아이템 사용
            if (PlayerInventory.Instance != null)
            {
                var inventoryItem = PlayerInventory.Instance.FindItem(linkedItem.itemData);
                if (inventoryItem != null)
                {
                    bool success = PlayerInventory.Instance.UseItem(inventoryItem);
                    if (success)
                    {
                        Debug.Log($"[QuickSlot] Used item: {linkedItem.ItemName}");
                        UpdateAmount();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 하이라이트 표시 (키 입력 피드백)
        /// </summary>
        public void ShowHighlight()
        {
            if (highlight != null)
            {
                highlight.SetActive(true);
            }
        }

        /// <summary>
        /// 하이라이트 숨기기
        /// </summary>
        public void HideHighlight()
        {
            if (highlight != null)
            {
                highlight.SetActive(false);
            }
        }

        #region Event Handlers

        public void OnDrop(PointerEventData eventData)
        {
            // 인벤토리에서 드래그된 아이템 처리
            DragItem dragItem = eventData.pointerDrag?.GetComponent<DragItem>();
            if (dragItem != null && dragItem.OriginalSlot?.CurrentItem != null)
            {
                SetLinkedItem(dragItem.OriginalSlot.CurrentItem);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 우클릭으로 슬롯 비우기
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ClearSlot();
            }
            // 좌클릭으로 아이템 사용
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                UseItem();
            }
        }

        #endregion
    }
}
