using UnityEngine;
using MiniExtractionShooter.Player;

namespace MiniExtractionShooter.Level
{
    /// <summary>
    /// Top-Down 카메라 팔로우 (마우스 오프셋 지원)
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Offset")]
        [SerializeField] private Vector3 offset = new Vector3(0, 15, -10);

        [Header("Mouse Offset")]
        [SerializeField] private bool useMouseOffset = true;
        [SerializeField] private float maxMouseOffset = 3f;
        [SerializeField] private float mouseOffsetSmooth = 5f;
        [SerializeField] [Range(0f, 1f)] private float mouseInfluence = 0.3f;

        [Header("Smoothing")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool useSmoothDamp = false;
        [SerializeField] private float dampTime = 0.3f;

        [Header("Bounds (Optional)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);

        [Header("Screen Shake")]
        [SerializeField] private float shakeIntensity = 0.1f;
        [SerializeField] private float shakeDuration = 0.1f;

        [Header("Top-Down Toggle")]
        [SerializeField] private KeyCode topDownToggleKey = KeyCode.Y;
        [SerializeField] private Vector3 topDownOffset = new Vector3(0, 15, 0);
        [SerializeField] private Vector3 topDownRotation = new Vector3(90, 0, 0);
        [SerializeField] private float viewTransitionSpeed = 3f;

        private Vector3 velocity = Vector3.zero;
        private float currentShakeDuration = 0f;
        private Vector3 currentMouseOffset = Vector3.zero;
        private Camera cam;
        private bool isTopDownMode = false;
        private Vector3 originalOffset;
        private Quaternion originalRotation;
        private Quaternion targetRotation;

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            // 타겟이 없으면 플레이어 찾기
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            // 시작 시 즉시 타겟 위치로
            if (target != null)
            {
                transform.position = target.position + offset;
            }

            // 원래 오프셋과 회전값 저장
            originalOffset = offset;
            originalRotation = transform.rotation;
            targetRotation = originalRotation;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Y키 토글 입력 처리
            if (Input.GetKeyDown(topDownToggleKey))
            {
                ToggleTopDownView();
            }

            // 회전 보간 적용
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, viewTransitionSpeed * Time.deltaTime);

            // 마우스 오프셋 계산 및 적용
            Vector3 targetMouseOffset = CalculateMouseOffset();
            currentMouseOffset = Vector3.Lerp(
                currentMouseOffset,
                targetMouseOffset,
                mouseOffsetSmooth * Time.deltaTime
            );

            // 최종 위치 = 타겟 + 기본 오프셋 + 마우스 오프셋
            Vector3 desiredPosition = target.position + offset + currentMouseOffset;

            // 경계 제한
            desiredPosition = ClampPosition(desiredPosition);

            // 스무딩 적용
            if (useSmoothDamp)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref velocity,
                    dampTime
                );
            }
            else
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    desiredPosition,
                    smoothSpeed * Time.deltaTime
                );
            }

            // 화면 흔들림 적용
            ApplyShake();
        }

        /// <summary>
        /// 마우스 위치 기반 오프셋 계산
        /// </summary>
        private Vector3 CalculateMouseOffset()
        {
            if (!useMouseOffset || target == null || cam == null)
                return Vector3.zero;

            // PlayerController에서 마우스 월드 위치 가져오기
            Vector3 mouseWorldPos;
            if (PlayerController.Instance != null)
            {
                mouseWorldPos = PlayerController.Instance.CurrentAimPoint;
            }
            else
            {
                // 폴백: 직접 계산
                Plane groundPlane = new Plane(Vector3.up, target.position);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);

                if (groundPlane.Raycast(ray, out float distance))
                {
                    mouseWorldPos = ray.GetPoint(distance);
                }
                else
                {
                    return Vector3.zero;
                }
            }

            // 플레이어에서 마우스 방향 계산
            Vector3 directionToMouse = mouseWorldPos - target.position;
            directionToMouse.y = 0; // XZ 평면에서만

            // 오프셋 계산 (최대치 제한)
            Vector3 mouseOffset = Vector3.ClampMagnitude(directionToMouse * mouseInfluence, maxMouseOffset);

            return mouseOffset;
        }

        /// <summary>
        /// 위치 경계 제한
        /// </summary>
        private Vector3 ClampPosition(Vector3 position)
        {
            if (!useBounds) return position;

            position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
            position.z = Mathf.Clamp(position.z, minBounds.y, maxBounds.y);

            return position;
        }

        /// <summary>
        /// 화면 흔들림 적용
        /// </summary>
        private void ApplyShake()
        {
            if (currentShakeDuration > 0)
            {
                Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
                shakeOffset.y = 0; // Top-Down이므로 Y축 흔들림 최소화
                transform.position += shakeOffset;
                currentShakeDuration -= Time.deltaTime;
            }
        }

        /// <summary>
        /// 화면 흔들림 실행
        /// </summary>
        public void Shake(float intensity = -1f, float duration = -1f)
        {
            if (intensity > 0) shakeIntensity = intensity;
            currentShakeDuration = duration > 0 ? duration : shakeDuration;
        }

        /// <summary>
        /// 런타임에 타겟 설정
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// 즉시 타겟 위치로 이동
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        /// <summary>
        /// 오프셋 설정
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        /// <summary>
        /// 마우스 오프셋 활성화/비활성화
        /// </summary>
        public void SetMouseOffsetEnabled(bool enabled)
        {
            useMouseOffset = enabled;
            if (!enabled)
            {
                currentMouseOffset = Vector3.zero;
            }
        }

        /// <summary>
        /// 탑다운 뷰 토글
        /// </summary>
        public void ToggleTopDownView()
        {
            isTopDownMode = !isTopDownMode;

            if (isTopDownMode)
            {
                offset = topDownOffset;
                targetRotation = Quaternion.Euler(topDownRotation);
            }
            else
            {
                offset = originalOffset;
                targetRotation = originalRotation;
            }
        }
    }
}
