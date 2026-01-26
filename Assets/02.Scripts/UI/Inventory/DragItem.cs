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

        private Canvas mainCanvas;
        private InventorySlot originalSlot;
        
        // 고스트 이미지 (드래그 중 표시되는 복사본)
        private GameObject ghostObject;
        private Image ghostImage;
        private RectTransform ghostRectTransform;
        
        public InventorySlot OriginalSlot => originalSlot;

        private void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            
            // CanvasGroup이 없으면 자동으로 추가
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
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

            Debug.Log($"[DragItem] OnBeginDrag: slot={originalSlot.SlotIndex}, item={originalSlot.CurrentItem?.ItemName}");

            // 고스트 이미지 생성 (원본은 그대로 유지)
            CreateGhost();
            
            // 원본을 반투명하게 (드래그 중임을 표시)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.5f;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 고스트를 마우스 위치로 이동
            if (ghostRectTransform != null)
            {
                ghostRectTransform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"[DragItem] OnEndDrag: slot={originalSlot?.SlotIndex}");
            
            // 원본 투명도 복구
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            
            // 고스트 이미지 제거
            DestroyGhost();
        }

        /// <summary>
        /// 드래그용 고스트 이미지 생성
        /// </summary>
        private void CreateGhost()
        {
            if (mainCanvas == null || image == null || image.sprite == null) return;

            // 고스트 오브젝트 생성
            ghostObject = new GameObject("DragGhost");
            ghostObject.transform.SetParent(mainCanvas.transform, false);
            
            // RectTransform 설정
            ghostRectTransform = ghostObject.AddComponent<RectTransform>();
            ghostRectTransform.sizeDelta = ((RectTransform)transform).sizeDelta;
            ghostRectTransform.position = transform.position;
            
            // 이미지 복사
            ghostImage = ghostObject.AddComponent<Image>();
            ghostImage.sprite = image.sprite;
            ghostImage.color = new Color(1f, 1f, 1f, 0.7f);
            ghostImage.raycastTarget = false; // 드롭 이벤트 방해하지 않도록
            
            // CanvasGroup 추가 (레이캐스트 블록 방지)
            var ghostCanvasGroup = ghostObject.AddComponent<CanvasGroup>();
            ghostCanvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 고스트 이미지 제거
        /// </summary>
        private void DestroyGhost()
        {
            if (ghostObject != null)
            {
                Destroy(ghostObject);
                ghostObject = null;
                ghostImage = null;
                ghostRectTransform = null;
            }
        }

        private void OnDisable()
        {
            // 비활성화 시 고스트 정리
            DestroyGhost();
        }
    }
}
