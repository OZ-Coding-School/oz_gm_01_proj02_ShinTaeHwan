using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MiniExtractionShooter.Player;

namespace MiniExtractionShooter.UI.Inventory
{
    public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image image;
        [SerializeField] private CanvasGroup canvasGroup;

        private Transform originalParent;
        private Canvas mainCanvas;
        private InventorySlot originalSlot;
        
        public InventorySlot OriginalSlot => originalSlot;

        private void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            mainCanvas = GetComponentInParent<Canvas>();
        }

        public void Initialize(InventorySlot slot)
        {
            originalSlot = slot;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (originalSlot == null || originalSlot.CurrentItem == null) 
            {
                eventData.pointerDrag = null;
                return;
            }

            originalParent = transform.parent;
            
            // Move to root to draw over everything
            if (mainCanvas != null)
            {
                transform.SetParent(mainCanvas.transform);
            }

            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mainCanvas != null)
            {
                transform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            
            // Check if dropped on nothing (optional: drop item to world)
            if (!eventData.pointerEnter)
            {
                // Dropped outside UI
            }
        }
    }
}
