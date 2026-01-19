using UnityEngine;
using System.Collections.Generic;

namespace MiniExtractionShooter.Data
{
    public enum ItemType
    {
        Weapon,
        Ammo,
        Armor,
        Health,
        Valuable
    }

    [System.Serializable]
    public class LootEntry
    {
        public string itemName;
        public ItemType itemType;

        [Header("Item Data Reference (하나만 선택)")]
        public WeaponData weaponData;
        public ArmorData armorData;
        public AmmoData ammoData;

        [Header("Amount")]
        public int minAmount = 1;
        public int maxAmount = 1;

        [Header("Loot Settings")]
        [Range(0f, 1f)]
        public float dropChance = 0.5f;

        public Sprite icon;

        /// <summary>
        /// ItemData 가져오기 (통합 접근)
        /// </summary>
        public ItemData GetItemData()
        {
            return weaponData as ItemData ?? armorData as ItemData ?? ammoData as ItemData;
        }
    }

    public enum LootContainerType
    {
        General,    // 일반 상자
        Medical,    // 의료 상자
        Military,   // 군수 상자
        Valuable    // 귀중품 상자
    }

    /// <summary>
    /// Loot Table ScriptableObject - TDD 기반 루팅 테이블 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "MiniExtractionShooter/Loot Table Data")]
    public class LootTableData : ScriptableObject
    {
        [Header("Container Info")]
        public string containerName = "Loot Container";
        public LootContainerType containerType = LootContainerType.General;

        [Header("Slot Settings")]
        [Tooltip("최소 아이템 슬롯 수")]
        public int minSlots = 2;

        [Tooltip("최대 아이템 슬롯 수")]
        public int maxSlots = 3;

        [Header("Loot Entries")]
        public List<LootEntry> lootEntries = new List<LootEntry>();

        [Header("Item Reveal")]
        [Tooltip("아이템 공개 간격 (초)")]
        public float revealInterval = 0.5f;

        /// <summary>
        /// 확률에 따라 랜덤 루트 아이템 리스트 생성
        /// </summary>
        public List<LootEntry> GenerateLoot()
        {
            List<LootEntry> generatedLoot = new List<LootEntry>();
            int slotCount = Random.Range(minSlots, maxSlots + 1);

            // 확률에 따라 아이템 선택
            List<LootEntry> availableEntries = new List<LootEntry>(lootEntries);
            ShuffleList(availableEntries);

            foreach (var entry in availableEntries)
            {
                if (generatedLoot.Count >= slotCount) break;

                if (Random.value <= entry.dropChance)
                {
                    // 수량 랜덤 설정
                    LootEntry newEntry = CloneEntry(entry);
                    generatedLoot.Add(newEntry);
                }
            }

            // 슬롯이 비어있으면 기본 아이템 추가 (빈 슬롯 방지)
            while (generatedLoot.Count < minSlots && lootEntries.Count > 0)
            {
                LootEntry fallback = lootEntries[Random.Range(0, lootEntries.Count)];
                generatedLoot.Add(CloneEntry(fallback));
            }

            return generatedLoot;
        }

        private LootEntry CloneEntry(LootEntry original)
        {
            LootEntry clone = new LootEntry
            {
                itemName = original.itemName,
                itemType = original.itemType,
                weaponData = original.weaponData,
                armorData = original.armorData,
                ammoData = original.ammoData,
                minAmount = original.minAmount,
                maxAmount = original.maxAmount,
                dropChance = original.dropChance,
                icon = original.icon
            };

            // 수량이 있는 아이템은 랜덤 수량 결정
            if (clone.itemType == ItemType.Ammo || clone.itemType == ItemType.Health || clone.itemType == ItemType.Valuable)
            {
                clone.minAmount = Random.Range(original.minAmount, original.maxAmount + 1);
                clone.maxAmount = clone.minAmount;
            }

            return clone;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
