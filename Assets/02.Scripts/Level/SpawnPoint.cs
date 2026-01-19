using UnityEngine;

namespace MiniExtractionShooter.Level
{
    public enum SpawnPointType
    {
        Player,
        Enemy,
        Loot
    }

    /// <summary>
    /// 스폰 포인트
    /// 플레이어, 적, 아이템 스폰 위치 지정
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private SpawnPointType spawnType = SpawnPointType.Player;
        [SerializeField] private float spawnRadius = 0.5f;

        [Header("Enemy Spawn Settings")]
        [SerializeField] private Transform[] patrolPoints;

        [Header("State")]
        [SerializeField] private bool isUsed = false;

        public SpawnPointType Type => spawnType;
        public bool IsUsed => isUsed;
        public Transform[] PatrolPoints => patrolPoints;

        /// <summary>
        /// 스폰 위치 가져오기 (랜덤 오프셋 적용)
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            if (spawnRadius <= 0)
            {
                return transform.position;
            }

            // 반경 내 랜덤 위치
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);
        }

        /// <summary>
        /// 스폰 회전값 가져오기
        /// </summary>
        public Quaternion GetSpawnRotation()
        {
            return transform.rotation;
        }

        /// <summary>
        /// 사용됨 표시
        /// </summary>
        public void MarkAsUsed()
        {
            isUsed = true;
        }

        /// <summary>
        /// 사용 가능 상태로 초기화
        /// </summary>
        public void Reset()
        {
            isUsed = false;
        }

        /// <summary>
        /// 순찰 포인트 설정
        /// </summary>
        public void SetPatrolPoints(Transform[] points)
        {
            patrolPoints = points;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 타입별 색상
            Color gizmoColor = spawnType switch
            {
                SpawnPointType.Player => Color.blue,
                SpawnPointType.Enemy => Color.red,
                SpawnPointType.Loot => Color.yellow,
                _ => Color.white
            };

            Gizmos.color = gizmoColor;

            // 위치 표시
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // 방향 표시
            Gizmos.DrawRay(transform.position, transform.forward * 1f);

            // 스폰 반경
            if (spawnRadius > 0)
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
                DrawWireCircle(transform.position, spawnRadius, 32);
            }

            // 순찰 포인트 연결선
            if (spawnType == SpawnPointType.Enemy && patrolPoints != null && patrolPoints.Length > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(patrolPoints[i].position, 0.2f);

                        if (i == 0)
                        {
                            Gizmos.DrawLine(transform.position, patrolPoints[i].position);
                        }

                        int nextIndex = (i + 1) % patrolPoints.Length;
                        if (patrolPoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                        }
                    }
                }
            }

            // 라벨
            string label = spawnType switch
            {
                SpawnPointType.Player => "PLAYER SPAWN",
                SpawnPointType.Enemy => "ENEMY SPAWN",
                SpawnPointType.Loot => "LOOT SPAWN",
                _ => "SPAWN"
            };

            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
        }

        private void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
#endif
    }
}
