using UnityEngine;
using UnityEngine.UI;
using MiniExtractionShooter.Weapon;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 동적 크로스헤어 UI (CS/Tarkov 스타일)
    /// 4개의 선이 확산에 따라 벌어지는 형태
    /// </summary>
    public class DynamicCrosshair : MonoBehaviour
    {
        public static DynamicCrosshair Instance { get; private set; }

        [Header("Crosshair Lines")]
        [SerializeField] private RectTransform topLine;
        [SerializeField] private RectTransform bottomLine;
        [SerializeField] private RectTransform leftLine;
        [SerializeField] private RectTransform rightLine;
        [SerializeField] private RectTransform centerDot;

        [Header("Line Settings")]
        [SerializeField] private float lineLength = 150f;
        [SerializeField] private float lineWidth = 20f;
        [SerializeField] private float baseGap = 50f;

        [Header("Spread Settings")]
        [SerializeField] private float spreadToPixelMultiplier = 30f;
        [SerializeField] private float smoothSpeed = 15f;
        [SerializeField] private float firePulseAmount = 100f;
        [SerializeField] private float firePulseRecovery = 300f;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color adsColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private Color firingColor = new Color(1f, 0.8f, 0.8f);

        [Header("Visibility")]
        [SerializeField] private bool showCenterDot = true;
        [SerializeField] private bool hideCursorInGame = true;

        // 현재 상태
        private float currentGap;
        private float targetGap;
        private float firePulse = 0f;
        private bool isVisible = true;
        private bool isADS = false;

        // 컴포넌트 캐시
        private RectTransform rectTransform;
        private Image[] lineImages;
        private Image centerDotImage;
        private Canvas parentCanvas;

        public float CurrentGap => currentGap;
        public bool IsVisible => isVisible;

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

            rectTransform = GetComponent<RectTransform>();
            CacheComponents();
        }

        private void Start()
        {
            // 부모 캔버스 찾기
            parentCanvas = GetComponentInParent<Canvas>();

            // 이벤트 구독
            SubscribeToEvents();

            // 초기 설정
            currentGap = baseGap;
            targetGap = baseGap;

            if (hideCursorInGame)
            {
                Cursor.visible = false;
            }

            SetupLines();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (hideCursorInGame)
            {
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            // 마우스 위치로 이동
            UpdatePosition(Input.mousePosition);

            // 확산 업데이트
            UpdateSpreadFromAimingSystem();

            // 발사 펄스 회복
            if (firePulse > 0)
            {
                firePulse -= firePulseRecovery * Time.deltaTime;
                firePulse = Mathf.Max(0, firePulse);
            }

            // 부드러운 갭 보간
            float finalTargetGap = targetGap + firePulse;
            currentGap = Mathf.Lerp(currentGap, finalTargetGap, smoothSpeed * Time.deltaTime);

            // 라인 위치 업데이트
            PositionLines(currentGap);
        }

        /// <summary>
        /// 컴포넌트 캐시
        /// </summary>
        private void CacheComponents()
        {
            lineImages = new Image[4];

            if (topLine != null) lineImages[0] = topLine.GetComponent<Image>();
            if (bottomLine != null) lineImages[1] = bottomLine.GetComponent<Image>();
            if (leftLine != null) lineImages[2] = leftLine.GetComponent<Image>();
            if (rightLine != null) lineImages[3] = rightLine.GetComponent<Image>();

            if (centerDot != null)
            {
                centerDotImage = centerDot.GetComponent<Image>();
            }
        }

        /// <summary>
        /// 라인 초기 설정
        /// </summary>
        private void SetupLines()
        {
            // 세로 라인 (상/하)
            if (topLine != null)
            {
                topLine.sizeDelta = new Vector2(lineWidth, lineLength);
                topLine.pivot = new Vector2(0.5f, 0f);
            }
            if (bottomLine != null)
            {
                bottomLine.sizeDelta = new Vector2(lineWidth, lineLength);
                bottomLine.pivot = new Vector2(0.5f, 1f);
            }

            // 가로 라인 (좌/우)
            if (leftLine != null)
            {
                leftLine.sizeDelta = new Vector2(lineLength, lineWidth);
                leftLine.pivot = new Vector2(1f, 0.5f);
            }
            if (rightLine != null)
            {
                rightLine.sizeDelta = new Vector2(lineLength, lineWidth);
                rightLine.pivot = new Vector2(0f, 0.5f);
            }

            // 중앙 점
            if (centerDot != null)
            {
                centerDot.sizeDelta = new Vector2(lineWidth, lineWidth);
                centerDot.gameObject.SetActive(showCenterDot);
            }

            SetColor(normalColor);
        }

        /// <summary>
        /// 크로스헤어 위치 업데이트 (스크린 좌표)
        /// </summary>
        public void UpdatePosition(Vector2 screenPosition)
        {
            if (rectTransform == null) return;

            // Canvas 스케일 모드에 따른 위치 변환
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rectTransform.position = screenPosition;
            }
            else if (parentCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    screenPosition,
                    parentCanvas.worldCamera,
                    out Vector2 localPoint
                );
                rectTransform.localPosition = localPoint;
            }
        }

        /// <summary>
        /// AimingSystem에서 확산 값 가져와 업데이트
        /// </summary>
        private void UpdateSpreadFromAimingSystem()
        {
            if (AimingSystem.Instance != null)
            {
                float spread = AimingSystem.Instance.GetCurrentSpread();
                UpdateSpread(spread, AimingSystem.Instance.IsADS);
            }
        }

        /// <summary>
        /// 확산에 따른 크로스헤어 크기 업데이트
        /// </summary>
        public void UpdateSpread(float spreadAngle, bool inADS)
        {
            isADS = inADS;

            // 확산 각도를 픽셀 갭으로 변환
            targetGap = baseGap + (spreadAngle * spreadToPixelMultiplier);

            // ADS 색상 변경
            SetColor(isADS ? adsColor : normalColor);
        }

        /// <summary>
        /// 발사 시 호출 - 확장 펄스
        /// </summary>
        public void OnFired()
        {
            firePulse += firePulseAmount;

            // 발사 색상 잠깐 표시
            SetColor(firingColor);

            // 잠시 후 원래 색상으로
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 0.05f);
        }

        /// <summary>
        /// 색상 리셋
        /// </summary>
        private void ResetColor()
        {
            SetColor(isADS ? adsColor : normalColor);
        }

        /// <summary>
        /// 라인 위치 설정
        /// </summary>
        private void PositionLines(float gap)
        {
            if (topLine != null)
                topLine.anchoredPosition = new Vector2(0, gap);

            if (bottomLine != null)
                bottomLine.anchoredPosition = new Vector2(0, -gap);

            if (leftLine != null)
                leftLine.anchoredPosition = new Vector2(-gap, 0);

            if (rightLine != null)
                rightLine.anchoredPosition = new Vector2(gap, 0);
        }

        /// <summary>
        /// 크로스헤어 색상 설정
        /// </summary>
        public void SetColor(Color color)
        {
            foreach (var img in lineImages)
            {
                if (img != null)
                    img.color = color;
            }

            if (centerDotImage != null)
                centerDotImage.color = color;
        }

        /// <summary>
        /// 크로스헤어 표시/숨김
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (AimingSystem.Instance != null)
            {
                AimingSystem.Instance.OnSpreadChanged += OnSpreadChanged;
                AimingSystem.Instance.OnAimStateChanged += OnAimStateChanged;
                AimingSystem.Instance.OnWeaponFired += OnFired;
            }
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (AimingSystem.Instance != null)
            {
                AimingSystem.Instance.OnSpreadChanged -= OnSpreadChanged;
                AimingSystem.Instance.OnAimStateChanged -= OnAimStateChanged;
                AimingSystem.Instance.OnWeaponFired -= OnFired;
            }
        }

        /// <summary>
        /// 확산 변경 이벤트 핸들러
        /// </summary>
        private void OnSpreadChanged(float spread)
        {
            targetGap = baseGap + (spread * spreadToPixelMultiplier);
        }

        /// <summary>
        /// 조준 상태 변경 이벤트 핸들러
        /// </summary>
        private void OnAimStateChanged(AimState state)
        {
            isADS = (state == AimState.ADS);
            SetColor(isADS ? adsColor : normalColor);
        }

        /// <summary>
        /// 강제 갭 설정 (디버그용)
        /// </summary>
        public void SetGap(float gap)
        {
            targetGap = gap;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 미리보기
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // 에디터에서 즉시 반영
            if (topLine != null && bottomLine != null && leftLine != null && rightLine != null)
            {
                SetupLines();
                PositionLines(baseGap);
            }
        }
#endif
    }
}
