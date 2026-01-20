using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.UI.Inventory
{
    public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [SerializeField] protected Image iconImage;
        [SerializeField] protected TextMeshProUGUI amountText;
        [SerializeField] protected Image backgroundImage;
        [SerializeField] protected GameObject highlightObj;
        [SerializeField] protected Image hiddenImage; // 공개 전 "?" 이미지

        [Header("State")]
        [SerializeField] protected int slotIndex;
        protected InventoryItem currentItem;
        protected InventoryUI inventoryUI;
        protected bool isRevealed = true; // 기본적으로 공개됨

        public InventoryItem CurrentItem => currentItem;
        public int SlotIndex => slotIndex;
        public bool IsRevealed => isRevealed;

        // 툴팁 이벤트
        public event System.Action<InventorySlot> OnSlotHoverEnter;
        public event System.Action<InventorySlot> OnSlotHoverExit;

        public virtual void Initialize(InventoryUI ui, int index)
        {
            inventoryUI = ui;
            slotIndex = index;
            isRevealed = true;
            ClearSlot();
        }

        public virtual void SetItem(InventoryItem item)
        {
            currentItem = item;
            
            if (currentItem != null)
            {
                // Icon
                if (iconImage != null)
                {
                    if (currentItem.Icon != null)
                    {
                        iconImage.sprite = currentItem.Icon;
                        iconImage.color = Color.white;
                        iconImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        iconImage.gameObject.SetActive(false);
                    }
                }

                // Amount - show for stackable items or always for ammo
                if (amountText != null)
                {
                    if (currentItem.amount > 1 || currentItem.ItemType == ItemType.Ammo)
                    {
                        amountText.text = currentItem.amount.ToString();
                        amountText.gameObject.SetActive(true);
                    }
                    else
                    {
                        amountText.gameObject.SetActive(false);
                    }
                }

                // Background Color based on rarity or type (Optional)
                if (backgroundImage != null)
                {
                    backgroundImage.color = GetColorByItemType(currentItem.ItemType);
                }
            }
            else
            {
                ClearSlot();
            }
        }

        public virtual void ClearSlot()
        {
            currentItem = null;
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = Color.clear;
                iconImage.gameObject.SetActive(false);
            }
            if (amountText != null) amountText.gameObject.SetActive(false);
            if (backgroundImage != null) backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }

        protected Color GetColorByItemType(ItemType type)
        {
            return type switch
            {
                ItemType.Weapon => new Color(0.6f, 0.2f, 0.2f, 0.5f),
                ItemType.Armor => new Color(0.2f, 0.4f, 0.6f, 0.5f),
                ItemType.Ammo => new Color(0.6f, 0.6f, 0.2f, 0.5f),
                ItemType.Health => new Color(0.2f, 0.6f, 0.2f, 0.5f),
                _ => new Color(0.2f, 0.2f, 0.2f, 0.5f)
            };
        }

        public void OnDrop(PointerEventData eventData)
        {
            // DragItem dropped on this slot
            DragItem draggedItem = eventData.pointerDrag?.GetComponent<DragItem>();
            if (draggedItem != null && inventoryUI != null)
            {
                inventoryUI.HandleItemDrop(draggedItem.OriginalSlot, this);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 공개되지 않은 아이템은 클릭 불가
            if (!isRevealed) return;

            if (inventoryUI != null && currentItem != null)
            {
                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    inventoryUI.ShowItemActions(this);
                }
                else
                {
                    inventoryUI.SelectItem(this);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 공개된 아이템만 툴팁 표시
            if (isRevealed && currentItem != null)
            {
                OnSlotHoverEnter?.Invoke(this);
                if (inventoryUI != null)
                {
                    inventoryUI.ShowTooltip(this);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnSlotHoverExit?.Invoke(this);
            if (inventoryUI != null)
            {
                inventoryUI.HideTooltip();
            }
        }

        /// <summary>
        /// 숨김 상태로 설정 (? 이미지 표시)
        /// </summary>
        public virtual void SetHidden()
        {
            isRevealed = false;
            
            // 아이콘 숨기기
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
            
            // 수량 숨기기
            if (amountText != null)
            {
                amountText.gameObject.SetActive(false);
            }
            
            // 숨김 이미지 표시
            if (hiddenImage != null)
            {
                hiddenImage.gameObject.SetActive(true);
            }
            
            // 배경색 어둡게
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            }
        }

        /// <summary>
        /// 공개 상태로 설정 (실제 아이템 표시)
        /// </summary>
        public virtual void SetRevealed()
        {
            isRevealed = true;
            
            // 숨김 이미지 숨기기
            if (hiddenImage != null)
            {
                hiddenImage.gameObject.SetActive(false);
            }
            
            // 아이템 다시 표시
            if (currentItem != null)
            {
                SetItem(currentItem);
            }
        }
    }
}
