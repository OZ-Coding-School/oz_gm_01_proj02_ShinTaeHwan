using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniExtractionShooter.Loot;
using MiniExtractionShooter.Player;

namespace MiniExtractionShooter.UI
{
    public class InteractionIndicatorUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LootInteraction lootInteraction;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera mainCamera;

        [Header("UI Elements")]
        [Tooltip("접근 시 표시할 원형 UI (비어있는 원)")]
        [SerializeField] private RectTransform circleIndicator;
        [Tooltip("상호작용 가능 시 표시할 원형 UI (채워진 원)")]
        [SerializeField] private RectTransform filledCircleIndicator;
        [Tooltip("상호작용 가능 시 표시할 텍스트 그룹 (배경 포함)")]
        [SerializeField] private GameObject interactionPromptGroup;
        [SerializeField] private TextMeshProUGUI interactionText;

        [Header("Settings")]
        [SerializeField] private float yOffset = 1.0f; // 대상 머리 위 오프셋
        [SerializeField] private float xOffset = 0.0f; // 대상 중심으로부터 가로 오프셋
        [Tooltip("채워진 원과 텍스트 그룹 간의 간격")]
        [SerializeField] private float promptSpacing = 60.0f;

        private LootBox currentTarget;

        private void Start()
        {
            if (lootInteraction == null)
            {
                var player = PlayerController.Instance;
                if (player != null)
                {
                    lootInteraction = player.GetComponent<LootInteraction>();
                }
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            HideAll();
        }

        private void Update()
        {
            if (lootInteraction == null) return;

            currentTarget = lootInteraction.NearestLootable;

            if (currentTarget == null || !currentTarget.isActiveAndEnabled || currentTarget.IsEmpty)
            {
                HideAll();
                return;
            }

            UpdateUIPositionAndState();
        }

        private void UpdateUIPositionAndState()
        {
            // 월드 좌표에서 카메라의 Right 벡터를 기준으로 xOffset 적용 (화면 상에서 좌우 이동 효과)
            Vector3 worldPos = currentTarget.transform.position + Vector3.up * yOffset;
            if (mainCamera != null)
            {
                worldPos += mainCamera.transform.right * xOffset;
            }
            
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            // 화면 뒤에 있는 경우 숨김
            if (screenPos.z < 0)
            {
                HideAll();
                return;
            }

            // 거리 계산
            float dist = Vector3.Distance(lootInteraction.transform.position, currentTarget.transform.position);
            
            // 상태에 따라 UI 전환
            if (dist <= lootInteraction.InteractionRange)
            {
                // 상호작용 가능 (Near) -> 채워진 원 + 텍스트
                if (circleIndicator != null) circleIndicator.gameObject.SetActive(false);
                
                if (filledCircleIndicator != null)
                {
                    filledCircleIndicator.gameObject.SetActive(true);
                    filledCircleIndicator.position = screenPos;
                }

                if (interactionPromptGroup != null)
                {
                    interactionPromptGroup.SetActive(true);
                    // 텍스트 그룹을 옆으로 이동
                    Vector3 promptPos = screenPos;
                    promptPos.x += promptSpacing;
                    interactionPromptGroup.transform.position = promptPos;
                }
            }
            else
            {
                // 접근 중 (Far) -> 비어있는 원
                if (circleIndicator != null)
                {
                    circleIndicator.gameObject.SetActive(true);
                    circleIndicator.position = screenPos;
                }

                if (filledCircleIndicator != null) filledCircleIndicator.gameObject.SetActive(false);
                if (interactionPromptGroup != null) interactionPromptGroup.SetActive(false);
            }
        }

        private void HideAll()
        {
            if (circleIndicator != null) circleIndicator.gameObject.SetActive(false);
            if (filledCircleIndicator != null) filledCircleIndicator.gameObject.SetActive(false);
            if (interactionPromptGroup != null) interactionPromptGroup.SetActive(false);
        }
    }
}
