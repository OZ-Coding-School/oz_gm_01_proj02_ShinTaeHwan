using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MiniExtractionShooter.Player;

namespace MiniExtractionShooter.UI.Inventory
{
    /// <summary>
    /// 퀵슬롯 UI 컴포넌트
    /// UI 표시만 담당, 데이터는 QuickSlotManager가 관리
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

        public int SlotIndex => slotIndex;
        public int KeyNumber => slotIndex + 3; // 슬롯 0 = 키 3

        private void Awake()
        {
            UpdateSlotNumber();
            ClearUI();
        }

        /// <summary>
        /// 슬롯 번호 텍스트 업데이트
        /// </summary>
        public void UpdateSlotNumber()
        {
            if (slotNumberText != null)
            {
                slotNumberText.text = KeyNumber.ToString();
            }
        }

        /// <summary>
        /// 슬롯 인덱스 설정 (외부에서 호출)
        /// </summary>
        public void SetSlotIndex(int index)
        {
            slotIndex = index;
            UpdateSlotNumber();
        }

        /// <summary>
        /// UI 업데이트 (QuickSlotManager에서 호출)
        /// </summary>
        public void UpdateUI(InventoryItem item)
        {
            if (item == null || item.itemData == null)
            {
                ClearUI();
                return;
            }

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
            if (amountText != null)
            {
                amountText.text = item.amount.ToString();
                amountText.gameObject.SetActive(true);
            }

            // 배경색 변경
            if (backgroundImage != null)
            {
                backgroundImage.color = filledBackgroundColor;
            }
        }

        /// <summary>
        /// UI 초기화 (빈 슬롯 상태)
        /// </summary>
        public void ClearUI()
        {
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

        /// <summary>
        /// 인벤토리에서 드래그된 아이템 처리
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            DragItem dragItem = eventData.pointerDrag?.GetComponent<DragItem>();
            if (dragItem != null && dragItem.OriginalSlot?.CurrentItem != null)
            {
                var item = dragItem.OriginalSlot.CurrentItem;
                
                // Manager에 등록 요청
                if (QuickSlotManager.Instance != null)
                {
                    QuickSlotManager.Instance.RegisterItem(slotIndex, item.itemData);
                }
            }
        }

        /// <summary>
        /// 마우스 클릭 처리
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (QuickSlotManager.Instance == null) return;

            // 우클릭: 슬롯 비우기
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                QuickSlotManager.Instance.ClearSlot(slotIndex);
            }
            // 좌클릭: 아이템 사용
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                QuickSlotManager.Instance.UseSlot(slotIndex);
            }
        }

        #endregion
    }
}
