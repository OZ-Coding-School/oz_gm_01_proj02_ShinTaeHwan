using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniExtractionShooter.Weapon;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 재장전 UI 시스템 - 크로스헤어 원형 프로그레스 + 하단 바
    /// </summary>
    public class ReloadUI : MonoBehaviour
    {
        public static ReloadUI Instance { get; private set; }

        [Header("Crosshair Reload Circle")]
        [SerializeField] private Image crosshairReloadCircle; // Radial filled image
        [SerializeField] private CanvasGroup crosshairCircleGroup;

        [Header("Bottom Reload Bar")]
        [SerializeField] private GameObject reloadBarContainer;
        [SerializeField] private Image reloadBarFill; // Horizontal filled image
        [SerializeField] private TextMeshProUGUI cancelPromptText;

        [Header("Settings")]
        [SerializeField] private string cancelPromptFormat = "<color=#FFCC00>X</color> 동작 취소";
        [SerializeField] private float circleStartAngle = -90f; // Top = -90

        private bool isReloading = false;
        private RectTransform circleRectTransform;
        private Canvas parentCanvas;

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
            // 이벤트 구독
            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.OnReloadStart += OnReloadStart;
                WeaponManager.Instance.OnReloadComplete += OnReloadEnd;
                WeaponManager.Instance.OnReloadProgress += OnReloadProgress;
            }

            // WeaponBase에서 직접 취소 이벤트도 구독
            if (WeaponManager.Instance?.ActiveWeapon != null)
            {
                WeaponManager.Instance.ActiveWeapon.OnReloadCancelled += OnReloadEnd;
            }

            // RectTransform 및 Canvas 캐시 - 그룹 전체를 옮기기 위해 그룹의 RectTransform 사용
            if (crosshairCircleGroup != null)
            {
                circleRectTransform = crosshairCircleGroup.GetComponent<RectTransform>();
            }
            parentCanvas = GetComponentInParent<Canvas>();

            // 초기 상태 숨김
            HideReloadUI();
        }

        private void OnDestroy()
        {
            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.OnReloadStart -= OnReloadStart;
                WeaponManager.Instance.OnReloadComplete -= OnReloadEnd;
                WeaponManager.Instance.OnReloadProgress -= OnReloadProgress;
            }

            if (WeaponManager.Instance?.ActiveWeapon != null)
            {
                WeaponManager.Instance.ActiveWeapon.OnReloadCancelled -= OnReloadEnd;
            }
        }

        private void Update()
        {
            // X키로 재장전 취소
            if (isReloading && Input.GetKeyDown(KeyCode.X))
            {
                WeaponManager.Instance?.CancelReload();
            }

            // 마우스 위치에 원형 프로그레스 동기화
            if (isReloading && circleRectTransform != null)
            {
                UpdateCirclePosition(Input.mousePosition);
            }
        }

        /// <summary>
        /// 원형 프로그레스 위치 업데이트 (마우스 따라가기)
        /// </summary>
        private void UpdateCirclePosition(Vector2 screenPosition)
        {
            if (circleRectTransform == null) return;

            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                circleRectTransform.position = screenPosition;
            }
            else if (parentCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    screenPosition,
                    parentCanvas.worldCamera,
                    out Vector2 localPoint
                );
                circleRectTransform.localPosition = localPoint;
            }
        }

        /// <summary>
        /// 재장전 시작
        /// </summary>
        private void OnReloadStart()
        {
            isReloading = true;
            ShowReloadUI();

            // 크로스헤어 숨기기
            if (DynamicCrosshair.Instance != null)
            {
                DynamicCrosshair.Instance.SetVisible(false);
            }
        }

        /// <summary>
        /// 재장전 종료/취소
        /// </summary>
        private void OnReloadEnd()
        {
            isReloading = false;
            HideReloadUI();

            // 크로스헤어 복원
            if (DynamicCrosshair.Instance != null)
            {
                DynamicCrosshair.Instance.SetVisible(true);
            }
        }

        /// <summary>
        /// 재장전 진행률 업데이트
        /// </summary>
        private void OnReloadProgress(float progress)
        {
            if (progress <= 0f)
            {
                OnReloadEnd();
                return;
            }

            // 원형 프로그레스 업데이트
            if (crosshairReloadCircle != null)
            {
                crosshairReloadCircle.fillAmount = progress;
            }

            // 수평 바 업데이트
            if (reloadBarFill != null)
            {
                reloadBarFill.fillAmount = progress;
            }
        }

        /// <summary>
        /// 재장전 UI 표시
        /// </summary>
        private void ShowReloadUI()
        {
            if (crosshairCircleGroup != null)
            {
                crosshairCircleGroup.alpha = 1f;
            }

            if (crosshairReloadCircle != null)
            {
                crosshairReloadCircle.fillAmount = 0f;
                crosshairReloadCircle.gameObject.SetActive(true);
            }

            if (reloadBarContainer != null)
            {
                reloadBarContainer.SetActive(true);
            }

            if (reloadBarFill != null)
            {
                reloadBarFill.fillAmount = 0f;
            }

            if (cancelPromptText != null)
            {
                cancelPromptText.text = cancelPromptFormat;
                cancelPromptText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 재장전 UI 숨기기
        /// </summary>
        private void HideReloadUI()
        {
            if (crosshairCircleGroup != null)
            {
                crosshairCircleGroup.alpha = 0f;
            }

            if (crosshairReloadCircle != null)
            {
                crosshairReloadCircle.gameObject.SetActive(false);
            }

            if (reloadBarContainer != null)
            {
                reloadBarContainer.SetActive(false);
            }

            if (cancelPromptText != null)
            {
                cancelPromptText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 재장전 중인지 확인
        /// </summary>
        public bool IsReloading => isReloading;
    }
}
