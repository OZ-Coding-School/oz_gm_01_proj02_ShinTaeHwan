using UnityEngine;
using UnityEngine.UI;
using MiniExtractionShooter.Player;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 플레이어 주변에 표시되는 World Space UI
    /// - 스태미나: 원형 Radial Fill 이미지 (플레이어 옆)
    /// - 체력바: 가로 Fill 이미지 (플레이어 머리 위)
    /// </summary>
    public class PlayerWorldUI : MonoBehaviour
    {
        public static PlayerWorldUI Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Camera mainCamera;

        [Header("Stamina UI")]
        [SerializeField] private GameObject staminaRoot;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private Vector3 staminaOffset = new Vector3(0.8f, 0.5f, 0f);
        [SerializeField] private float staminaUISize = 50f;
        [SerializeField] private Color staminaFullColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color staminaLowColor = new Color(1f, 0.4f, 0.2f);
        [SerializeField] private float lowStaminaThreshold = 0.3f;
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private float fadeSpeed = 3f;

        [Header("Health UI")]
        [SerializeField] private GameObject healthRoot;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthBackgroundImage;
        [SerializeField] private Vector3 healthOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private float healthBarWidth = 80f;
        [SerializeField] private float healthBarHeight = 10f;
        [SerializeField] private Color healthFullColor = new Color(0.2f, 1f, 0.3f);
        [SerializeField] private Color healthLowColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private float lowHealthThreshold = 0.3f;

        // 내부 상태
        private float currentStaminaDisplay;
        private float currentHealthDisplay;
        private float staminaUIAlpha = 0f;
        private CanvasGroup staminaCanvasGroup;

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
            // 자동으로 참조 찾기
            if (playerTransform == null && PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // 스태미나 UI 캔버스 그룹 추가 (페이드용)
            if (staminaRoot != null && staminaCanvasGroup == null)
            {
                staminaCanvasGroup = staminaRoot.GetComponent<CanvasGroup>();
                if (staminaCanvasGroup == null)
                {
                    staminaCanvasGroup = staminaRoot.AddComponent<CanvasGroup>();
                }
            }

            // 이벤트 구독
            if (PlayerStamina.Instance != null)
            {
                PlayerStamina.Instance.OnStaminaChanged += OnStaminaChanged;
                currentStaminaDisplay = PlayerStamina.Instance.StaminaPercentage;
            }

            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnHealthChanged += OnHealthChanged;
                currentHealthDisplay = PlayerHealth.Instance.HealthPercentage;
            }

            // 초기 표시 업데이트
            UpdateStaminaDisplay(currentStaminaDisplay);
            UpdateHealthDisplay(currentHealthDisplay);
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (PlayerStamina.Instance != null)
            {
                PlayerStamina.Instance.OnStaminaChanged -= OnStaminaChanged;
            }

            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnHealthChanged -= OnHealthChanged;
            }
        }

        private void LateUpdate()
        {
            if (playerTransform == null) return;

            // 빌보드 처리 + 위치 업데이트
            UpdatePositionAndRotation();

            // 스태미나 UI 페이드
            UpdateStaminaFade();
        }

        /// <summary>
        /// UI 위치와 회전 업데이트 (빌보드)
        /// </summary>
        private void UpdatePositionAndRotation()
        {
            if (mainCamera == null) return;

            // 스태미나 UI 위치 (플레이어 옆)
            if (staminaRoot != null)
            {
                Vector3 staminaWorldPos = playerTransform.position + staminaOffset;
                staminaRoot.transform.position = staminaWorldPos;
                staminaRoot.transform.LookAt(staminaRoot.transform.position + mainCamera.transform.forward);
            }

            // 체력바 UI 위치 (플레이어 머리 위)
            if (healthRoot != null)
            {
                Vector3 healthWorldPos = playerTransform.position + healthOffset;
                healthRoot.transform.position = healthWorldPos;
                healthRoot.transform.LookAt(healthRoot.transform.position + mainCamera.transform.forward);
            }
        }

        /// <summary>
        /// 스태미나 UI 페이드 효과
        /// </summary>
        private void UpdateStaminaFade()
        {
            if (staminaCanvasGroup == null) return;

            float targetAlpha = 1f;

            // 가득 찼을 때 숨기기 옵션
            if (hideWhenFull && currentStaminaDisplay >= 0.99f)
            {
                targetAlpha = 0f;
            }

            staminaUIAlpha = Mathf.Lerp(staminaUIAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            staminaCanvasGroup.alpha = staminaUIAlpha;
        }

        /// <summary>
        /// 스태미나 변경 이벤트 핸들러
        /// </summary>
        private void OnStaminaChanged(float current, float max)
        {
            currentStaminaDisplay = current / max;
            UpdateStaminaDisplay(currentStaminaDisplay);
        }

        /// <summary>
        /// 체력 변경 이벤트 핸들러
        /// </summary>
        private void OnHealthChanged(float current, float max)
        {
            currentHealthDisplay = current / max;
            UpdateHealthDisplay(currentHealthDisplay);
        }

        /// <summary>
        /// 스태미나 UI 표시 업데이트
        /// </summary>
        private void UpdateStaminaDisplay(float percentage)
        {
            if (staminaFillImage == null) return;

            // Fill Amount 업데이트 (Radial Fill)
            staminaFillImage.fillAmount = percentage;

            // 색상 업데이트
            Color targetColor = percentage <= lowStaminaThreshold
                ? staminaLowColor
                : Color.Lerp(staminaLowColor, staminaFullColor, (percentage - lowStaminaThreshold) / (1f - lowStaminaThreshold));

            staminaFillImage.color = targetColor;
        }

        /// <summary>
        /// 체력바 UI 표시 업데이트
        /// </summary>
        private void UpdateHealthDisplay(float percentage)
        {
            if (healthFillImage == null) return;

            // Fill Amount 업데이트 (Horizontal Fill)
            healthFillImage.fillAmount = percentage;

            // 색상 업데이트
            Color targetColor = percentage <= lowHealthThreshold
                ? healthLowColor
                : Color.Lerp(healthLowColor, healthFullColor, (percentage - lowHealthThreshold) / (1f - lowHealthThreshold));

            healthFillImage.color = targetColor;
        }

        /// <summary>
        /// 플레이어 Transform 설정
        /// </summary>
        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }

        /// <summary>
        /// UI 표시/숨김
        /// </summary>
        public void SetUIVisible(bool visible)
        {
            if (staminaRoot != null) staminaRoot.SetActive(visible);
            if (healthRoot != null) healthRoot.SetActive(visible);
        }
    }
}
