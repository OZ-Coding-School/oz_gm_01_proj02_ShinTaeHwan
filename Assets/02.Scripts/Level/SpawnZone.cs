using UnityEngine;

namespace MiniExtractionShooter.Level
{
    /// <summary>
    /// 스폰 구역 데이터
    /// 여러 SpawnPoint를 묶어서 하나의 구역으로 관리
    /// </summary>
    [System.Serializable]
    public class SpawnZone
    {
        [Tooltip("구역 이름 (예: 시작 구역, 중앙 광장)")]
        public string zoneName = "New Zone";

        [Tooltip("이 구역에서 생성할 프리팹")]
        public GameObject prefab;

        [Tooltip("이 구역에 포함된 스폰 포인트들")]
        public SpawnPoint[] spawnPoints;

        [Tooltip("이 구역에서 생성할 수량")]
        [Min(0)]
        public int spawnCount = 1;

        /// <summary>
        /// 사용 가능한 스폰 포인트 중 랜덤으로 하나 반환
        /// </summary>
        public SpawnPoint GetRandomAvailableSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;

            // 사용 가능한 스폰 포인트 필터링
            var available = new System.Collections.Generic.List<SpawnPoint>();
            foreach (var sp in spawnPoints)
            {
                if (sp != null && !sp.IsUsed)
                {
                    available.Add(sp);
                }
            }

            if (available.Count == 0)
                return null;

            // 랜덤 선택
            int randomIndex = Random.Range(0, available.Count);
            return available[randomIndex];
        }

        /// <summary>
        /// 모든 스폰 포인트 사용 상태 초기화
        /// </summary>
        public void ResetAllSpawnPoints()
        {
            if (spawnPoints == null) return;

            foreach (var sp in spawnPoints)
            {
                if (sp != null)
                {
                    sp.Reset();
                }
            }
        }

        /// <summary>
        /// 유효한 스폰 포인트 개수
        /// </summary>
        public int ValidSpawnPointCount
        {
            get
            {
                if (spawnPoints == null) return 0;

                int count = 0;
                foreach (var sp in spawnPoints)
                {
                    if (sp != null) count++;
                }
                return count;
            }
        }
    }
}
