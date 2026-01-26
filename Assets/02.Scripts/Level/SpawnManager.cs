using UnityEngine;
using System.Collections.Generic;

namespace MiniExtractionShooter.Level
{
    /// <summary>
    /// 스폰 매니저
    /// 구역별로 적, 루트, 탈출구를 랜덤 스폰
    /// 각 SpawnZone에 프리팹이 설정되어 있음
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [Header("Spawn Zones")]
        [Tooltip("스폰 구역 리스트 (각 구역에 프리팹, 스폰포인트, 스폰수량 설정)")]
        [SerializeField] private List<SpawnZone> spawnZones = new List<SpawnZone>();

        [Header("Settings")]
        [Tooltip("게임 시작 시 자동 스폰")]
        [SerializeField] private bool spawnOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // 생성된 오브젝트 추적 (Zone별로 관리)
        private Dictionary<SpawnZone, List<GameObject>> spawnedObjects = new Dictionary<SpawnZone, List<GameObject>>();

        public List<SpawnZone> SpawnZones => spawnZones;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAll();
            }
        }

        /// <summary>
        /// 모든 구역에서 스폰
        /// </summary>
        public void SpawnAll()
        {
            foreach (var zone in spawnZones)
            {
                SpawnInZone(zone);
            }

            if (debugMode)
                Debug.Log($"[SpawnManager] Total spawned objects: {GetTotalSpawnedCount()}");
        }

        /// <summary>
        /// 특정 구역에서 스폰
        /// </summary>
        public void SpawnInZone(SpawnZone zone)
        {
            if (zone == null)
            {
                if (debugMode)
                    Debug.LogWarning("[SpawnManager] Zone is null!");
                return;
            }

            if (zone.prefab == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[SpawnManager] Zone '{zone.zoneName}' has no prefab assigned!");
                return;
            }

            if (zone.spawnPoints == null || zone.spawnPoints.Length == 0)
            {
                if (debugMode)
                    Debug.LogWarning($"[SpawnManager] Zone '{zone.zoneName}' has no spawn points!");
                return;
            }

            // 구역의 모든 스폰 포인트 초기화
            zone.ResetAllSpawnPoints();

            // 이 구역의 스폰된 오브젝트 리스트 초기화
            if (!spawnedObjects.ContainsKey(zone))
            {
                spawnedObjects[zone] = new List<GameObject>();
            }

            int spawned = 0;
            int attempts = 0;
            int maxAttempts = zone.spawnCount * 3; // 무한 루프 방지

            while (spawned < zone.spawnCount && attempts < maxAttempts)
            {
                attempts++;

                SpawnPoint spawnPoint = zone.GetRandomAvailableSpawnPoint();
                if (spawnPoint == null)
                {
                    if (debugMode)
                        Debug.LogWarning($"[SpawnManager] No available spawn points in zone '{zone.zoneName}'!");
                    break;
                }

                // 오브젝트 생성
                Vector3 position = spawnPoint.GetSpawnPosition();
                Quaternion rotation = spawnPoint.GetSpawnRotation();

                GameObject spawnedObject = Instantiate(zone.prefab, position, rotation);
                spawnedObject.name = $"{zone.prefab.name}_{zone.zoneName}_{spawned}";

                // 적인 경우 순찰 포인트 설정
                if (spawnPoint.PatrolPoints != null && spawnPoint.PatrolPoints.Length > 0)
                {
                    var enemyAI = spawnedObject.GetComponent<MiniExtractionShooter.Enemy.EnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.SetPatrolPoints(spawnPoint.PatrolPoints);
                    }
                }

                // 스폰 포인트 사용됨 표시
                spawnPoint.MarkAsUsed();

                spawnedObjects[zone].Add(spawnedObject);
                spawned++;

                if (debugMode)
                    Debug.Log($"[SpawnManager] Spawned '{zone.prefab.name}' at {position} in zone '{zone.zoneName}'");
            }

            if (debugMode)
                Debug.Log($"[SpawnManager] Zone '{zone.zoneName}': Spawned {spawned}/{zone.spawnCount}");
        }

        /// <summary>
        /// 모든 스폰된 오브젝트 제거
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in spawnedObjects)
            {
                foreach (var obj in kvp.Value)
                {
                    if (obj != null)
                        Destroy(obj);
                }
                kvp.Value.Clear();
            }
            spawnedObjects.Clear();
        }

        /// <summary>
        /// 특정 구역의 스폰된 오브젝트 제거
        /// </summary>
        public void ClearZone(SpawnZone zone)
        {
            if (zone == null || !spawnedObjects.ContainsKey(zone)) return;

            foreach (var obj in spawnedObjects[zone])
            {
                if (obj != null)
                    Destroy(obj);
            }
            spawnedObjects[zone].Clear();
        }

        /// <summary>
        /// 리스폰 (제거 후 재생성)
        /// </summary>
        public void Respawn()
        {
            ClearAll();
            SpawnAll();
        }

        /// <summary>
        /// 특정 구역만 리스폰
        /// </summary>
        public void RespawnZone(SpawnZone zone)
        {
            ClearZone(zone);
            SpawnInZone(zone);
        }

        /// <summary>
        /// 총 스폰된 오브젝트 수
        /// </summary>
        public int GetTotalSpawnedCount()
        {
            int count = 0;
            foreach (var kvp in spawnedObjects)
            {
                count += kvp.Value.Count;
            }
            return count;
        }

        /// <summary>
        /// 특정 구역의 스폰된 오브젝트 리스트 반환
        /// </summary>
        public List<GameObject> GetSpawnedObjects(SpawnZone zone)
        {
            if (zone != null && spawnedObjects.ContainsKey(zone))
            {
                return new List<GameObject>(spawnedObjects[zone]);
            }
            return new List<GameObject>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (spawnZones == null) return;

            Color[] colors = { Color.red, Color.yellow, Color.green, Color.cyan, Color.magenta };

            for (int i = 0; i < spawnZones.Count; i++)
            {
                var zone = spawnZones[i];
                if (zone?.spawnPoints == null) continue;

                Color color = colors[i % colors.Length];
                DrawZoneGizmos(zone, color);
            }
        }

        private void DrawZoneGizmos(SpawnZone zone, Color color)
        {
            if (zone?.spawnPoints == null) return;

            Vector3 center = Vector3.zero;
            int validCount = 0;

            foreach (var sp in zone.spawnPoints)
            {
                if (sp != null)
                {
                    center += sp.transform.position;
                    validCount++;
                }
            }

            if (validCount > 0)
            {
                center /= validCount;
                UnityEditor.Handles.color = color;
                string prefabName = zone.prefab != null ? zone.prefab.name : "(No Prefab)";
                UnityEditor.Handles.Label(center + Vector3.up * 2f,
                    $"{zone.zoneName}\n[{prefabName}]\n(Count: {zone.spawnCount})");
            }
        }
#endif
    }
}
