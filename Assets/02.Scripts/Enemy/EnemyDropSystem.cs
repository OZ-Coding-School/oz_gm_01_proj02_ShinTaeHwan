using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Loot;

namespace MiniExtractionShooter.Enemy
{
    /// <summary>
    /// 적 사망 시 드랍 시스템
    /// EnemyData의 LootTableData 기반으로 아이템 결정
    /// </summary>
    public class EnemyDropSystem : MonoBehaviour
    {
        [Header("Loot Box")]
        [SerializeField] private LootBox lootBoxPrefab;
        [SerializeField] private Vector3 dropOffset = new Vector3(0, 0.15f, 0);

        private Enemy enemy;
        private EnemyData EnemyData => enemy != null ? enemy.Data : null;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
        }

        /// <summary>
        /// 사망 시 호출
        /// </summary>
        public void OnDeath()
        {
            if (EnemyData == null) return;

            List<LootEntry> drops = GenerateDrops();
            SpawnLootBox(drops);
        }

        /// <summary>
        /// LootTable에서 드랍 아이템 생성
        /// </summary>
        private List<LootEntry> GenerateDrops()
        {
            if (EnemyData.lootTable == null) return new List<LootEntry>();
            return EnemyData.lootTable.GenerateLoot();
        }

        /// <summary>
        /// LootBox 스폰 (PoolManager 사용)
        /// </summary>
        private void SpawnLootBox(List<LootEntry> drops)
        {
            if (lootBoxPrefab == null)
            {
                Debug.LogWarning($"[EnemyDropSystem] LootBox prefab is not assigned on {gameObject.name}");
                return;
            }

            if (drops == null || drops.Count == 0)
            {
                return;
            }

            LootBox box = PoolManager.Instance?.GetFromPool(lootBoxPrefab);

            if (box != null)
            {
                Vector3 spawnPosition = transform.position + dropOffset;
                box.Initialize(drops, spawnPosition);
            }
            else
            {
                Debug.LogWarning($"[EnemyDropSystem] Failed to get LootBox from pool, creating new instance");
                LootBox newBox = Instantiate(lootBoxPrefab, transform.position + dropOffset, Quaternion.identity);
                newBox.Initialize(drops, transform.position + dropOffset);
            }
        }



        /// <summary>
        /// LootBox 프리팹 설정
        /// </summary>
        public void SetLootBoxPrefab(LootBox prefab)
        {
            lootBoxPrefab = prefab;
        }
    }
}
