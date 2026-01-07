using UnityEngine;

namespace MiniExtractionShooter.Level
{
    /// <summary>
    /// Top-Down 카메라 팔로우
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Offset")]
        [SerializeField] private Vector3 offset = new Vector3(0, 15, -10);

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

        private Vector3 velocity = Vector3.zero;
        private float currentShakeDuration = 0f;

        private void Start()
        {
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
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;

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
    }
}
