using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Loot;

namespace MiniExtractionShooter.Enemy
{
    /// <summary>
    /// 적 사망 시 드랍 시스템
    /// TDD 기준: 무기 + 잔여 탄약 고정 드랍
    /// </summary>
    public class EnemyDropSystem : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EnemyData enemyData;

        [Header("Drop Settings")]
        [SerializeField] private int minAmmoDrop = 6;
        [SerializeField] private int maxAmmoDrop = 18;

        [Header("Ammo Data References")]
        [SerializeField] private AmmoData pistolAmmoData;
        [SerializeField] private AmmoData rifleAmmoData;

        [Header("Loot Box")]
        [SerializeField] private LootBox lootBoxPrefab;
        [SerializeField] private Vector3 dropOffset = new Vector3(0, 0.15f, 0);

        private EnemyCombat enemyCombat;

        private void Awake()
        {
            enemyCombat = GetComponent<EnemyCombat>();
        }

        /// <summary>
        /// 사망 시 호출
        /// </summary>
        public void OnDeath()
        {
            if (enemyData == null) return;

            List<LootEntry> drops = GenerateDrops();

            // LootBox 스폰
            SpawnLootBox(drops);
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

            // PoolManager에서 LootBox 가져오기
            LootBox box = PoolManager.Instance?.GetFromPool(lootBoxPrefab);

            if (box != null)
            {
                Vector3 spawnPosition = transform.position + dropOffset;
                box.Initialize(drops, spawnPosition);
            }
            else
            {
                // Pool에서 가져오지 못한 경우 직접 생성 (fallback)
                Debug.LogWarning($"[EnemyDropSystem] Failed to get LootBox from pool, creating new instance");
                LootBox newBox = Instantiate(lootBoxPrefab, transform.position + dropOffset, Quaternion.identity);
                newBox.Initialize(drops, transform.position + dropOffset);
            }
        }

        /// <summary>
        /// 드랍 아이템 생성
        /// </summary>
        private List<LootEntry> GenerateDrops()
        {
            List<LootEntry> drops = new List<LootEntry>();

            // 1. 무기 드랍
            if (enemyData.equippedWeapon != null)
            {
                LootEntry weaponDrop = new LootEntry
                {
                    itemName = enemyData.equippedWeapon.itemName,
                    itemType = ItemType.Weapon,
                    weaponData = enemyData.equippedWeapon,
                    dropChance = 1f,
                    icon = enemyData.equippedWeapon.icon
                };
                drops.Add(weaponDrop);
            }

            // 2. 탄약 드랍
            int ammoAmount = CalculateAmmoDrop();
            if (ammoAmount > 0 && enemyData.equippedWeapon != null)
            {
                // 탄약 타입에 맞는 AmmoData 찾기
                AmmoData ammoData = GetAmmoDataForWeapon(enemyData.equippedWeapon);
                if (ammoData != null)
                {
                    LootEntry ammoDrop = new LootEntry
                    {
                        itemName = ammoData.itemName,
                        itemType = ItemType.Ammo,
                        ammoData = ammoData,
                        minAmount = ammoAmount,
                        maxAmount = ammoAmount,
                        dropChance = 1f,
                        icon = ammoData.icon
                    };
                    drops.Add(ammoDrop);
                }
            }

            return drops;
        }

        /// <summary>
        /// 탄약 드랍량 계산
        /// </summary>
        private int CalculateAmmoDrop()
        {
            // 적이 가지고 있던 잔여 탄약 기반
            int remainingAmmo = 0;
            if (enemyCombat != null)
            {
                remainingAmmo = enemyCombat.GetRemainingAmmo();
            }

            // 잔여 탄약 + 추가 랜덤 탄약
            int additionalAmmo = Random.Range(minAmmoDrop, maxAmmoDrop + 1);

            // 무기 타입에 따른 범위 조정
            if (enemyData.equippedWeapon != null)
            {
                switch (enemyData.equippedWeapon.weaponType)
                {
                    case WeaponType.Pistol:
                        // 권총: 6~18발
                        additionalAmmo = Random.Range(6, 19);
                        break;
                    case WeaponType.Rifle:
                        // 소총: 10~25발
                        additionalAmmo = Random.Range(10, 26);
                        break;
                }
            }

            return remainingAmmo + additionalAmmo;
        }

        /// <summary>
        /// EnemyData 설정
        /// </summary>
        public void SetEnemyData(EnemyData data)
        {
            enemyData = data;
        }

        /// <summary>
        /// LootBox 프리팹 설정
        /// </summary>
        public void SetLootBoxPrefab(LootBox prefab)
        {
            lootBoxPrefab = prefab;
        }

        /// <summary>
        /// 무기 타입에 맞는 AmmoData 가져오기
        /// </summary>
        private AmmoData GetAmmoDataForWeapon(WeaponData weapon)
        {
            if (weapon == null) return null;

            return weapon.ammoType switch
            {
                AmmoType.Pistol => pistolAmmoData,
                AmmoType.Rifle => rifleAmmoData,
                _ => null
            };
        }
    }
}
