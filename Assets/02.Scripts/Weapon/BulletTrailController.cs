using UnityEngine;
using MiniExtractionShooter.Core;

namespace MiniExtractionShooter.Weapon
{
    /// <summary>
    /// 총알 궤적(Trail) 컨트롤러
    /// Trail Renderer를 사용하여 짧은 총알이 날아가는 모습을 표현
    /// 무기의 탄속(muzzleVelocity)에 따라 속도가 결정됨
    /// </summary>
    public class BulletTrailController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float bulletLength = 1f;  // 총알 길이 (미터)
        [SerializeField] private float destroyDelay = 0.05f;

        private float speed = 50f;  // 무기 데이터에서 전달받음
        private Vector3 targetPosition;
        private bool isInitialized = false;
        private TrailRenderer trailRenderer;

        private void Awake()
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }

        /// <summary>
        /// 총알 궤적 초기화 (무기 탄속 사용)
        /// </summary>
        /// <param name="start">발사 지점</param>
        /// <param name="end">도착 지점 (피격 지점 또는 최대 사거리)</param>
        /// <param name="muzzleVelocity">무기 탄속 (m/s)</param>
        public void Initialize(Vector3 start, Vector3 end, float muzzleVelocity)
        {
            speed = muzzleVelocity;
            targetPosition = end;
            transform.position = start;

            // Trail Time을 총알 길이에 맞게 계산
            // Time = 길이 / 속도
            if (trailRenderer != null && speed > 0)
            {
                trailRenderer.time = bulletLength / speed;
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized) return;

            // 목표 지점을 향해 이동
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

            // 목표 지점 도달 시 풀로 반환
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                // Trail이 사라질 시간을 주고 풀로 반환
                PoolManager.Instance.ReturnAfterDelay(this, destroyDelay);

                isInitialized = false;
            }
        }
    }
}
