using UnityEngine;

namespace MiniExtractionShooter.Weapon
{
    /// <summary>
    /// 탄퍼짐(Spread) 시스템 - 확산 각도를 총알 방향 편차로 변환
    /// </summary>
    public class SpreadSystem : MonoBehaviour
    {
        /// <summary>
        /// 확산 각도를 적용하여 랜덤하게 편향된 방향 벡터 반환
        /// </summary>
        /// <param name="direction">원래 조준 방향 (정규화)</param>
        /// <param name="spreadAngle">확산 각도 (도)</param>
        /// <returns>편향된 방향 벡터 (정규화)</returns>
        public Vector3 ApplySpreadToDirection(Vector3 direction, float spreadAngle)
        {
            if (spreadAngle <= 0f)
                return direction.normalized;

            // 확산 각도를 라디안으로 변환 (반각)
            float halfAngleRad = spreadAngle * 0.5f * Mathf.Deg2Rad;

            // 원형 분포 내의 랜덤 포인트 생성
            Vector2 randomPoint = GetRandomPointInCircle(1f);

            // 최대 편차 계산
            float maxDeviation = Mathf.Tan(halfAngleRad);

            // X, Y 편차 계산
            float deviationX = randomPoint.x * maxDeviation;
            float deviationY = randomPoint.y * maxDeviation;

            // 원래 방향을 기준으로 로컬 좌표계 생성
            Vector3 forward = direction.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // forward가 거의 수직인 경우 처리
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(Vector3.forward, forward);
            }
            right.Normalize();

            Vector3 up = Vector3.Cross(forward, right).normalized;

            // 편차 적용
            Vector3 spreadDirection = forward + (right * deviationX) + (up * deviationY);

            return spreadDirection.normalized;
        }

        /// <summary>
        /// 특정 거리에서의 확산 반경 계산 (UI/디버그용)
        /// </summary>
        /// <param name="spreadAngle">확산 각도 (도)</param>
        /// <param name="distance">거리 (m)</param>
        /// <returns>해당 거리에서의 확산 원 반경</returns>
        public float GetSpreadRadius(float spreadAngle, float distance)
        {
            float halfAngleRad = spreadAngle * 0.5f * Mathf.Deg2Rad;
            return distance * Mathf.Tan(halfAngleRad);
        }

        /// <summary>
        /// 확산 각도를 스크린 픽셀로 변환 (크로스헤어용)
        /// </summary>
        /// <param name="spreadAngle">확산 각도 (도)</param>
        /// <param name="camera">현재 카메라</param>
        /// <param name="referenceDistance">기준 거리 (기본 10m)</param>
        /// <returns>스크린 상 확산 크기 (픽셀)</returns>
        public float GetSpreadInScreenPixels(float spreadAngle, Camera camera, float referenceDistance = 10f)
        {
            if (camera == null) return 0f;

            // 기준 거리에서의 월드 반경 계산
            float worldRadius = GetSpreadRadius(spreadAngle, referenceDistance);

            // 카메라 기준 스크린 크기로 변환
            Vector3 centerWorld = camera.transform.position + camera.transform.forward * referenceDistance;
            Vector3 edgeWorld = centerWorld + camera.transform.right * worldRadius;

            Vector3 centerScreen = camera.WorldToScreenPoint(centerWorld);
            Vector3 edgeScreen = camera.WorldToScreenPoint(edgeWorld);

            return Vector2.Distance(centerScreen, edgeScreen);
        }

        /// <summary>
        /// 원 내의 균등 분포 랜덤 포인트 생성
        /// </summary>
        private Vector2 GetRandomPointInCircle(float radius)
        {
            // 균등 분포를 위해 sqrt 사용
            float r = Mathf.Sqrt(Random.value) * radius;
            float theta = Random.value * 2f * Mathf.PI;

            return new Vector2(
                r * Mathf.Cos(theta),
                r * Mathf.Sin(theta)
            );
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 확산 시각화 (디버그용)
        /// </summary>
        public void DrawSpreadGizmo(Vector3 origin, Vector3 direction, float spreadAngle, float distance, Color color)
        {
            if (spreadAngle <= 0f) return;

            float radius = GetSpreadRadius(spreadAngle, distance);
            Vector3 endPoint = origin + direction.normalized * distance;

            Gizmos.color = color;

            // 확산 원뿔 라인
            int segments = 16;
            Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(Vector3.forward, direction.normalized);
            right.Normalize();

            Vector3 up = Vector3.Cross(direction.normalized, right).normalized;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * 2f * Mathf.PI;
                float angle2 = ((i + 1) / (float)segments) * 2f * Mathf.PI;

                Vector3 point1 = endPoint + (right * Mathf.Cos(angle1) + up * Mathf.Sin(angle1)) * radius;
                Vector3 point2 = endPoint + (right * Mathf.Cos(angle2) + up * Mathf.Sin(angle2)) * radius;

                Gizmos.DrawLine(point1, point2);
                Gizmos.DrawLine(origin, point1);
            }
        }
#endif
    }
}
