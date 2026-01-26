using UnityEngine;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.Loot
{
    /// <summary>
    /// 루트 아이템 런타임 데이터
    /// ItemData 기반 통합 아이템 시스템
    /// </summary>
    [System.Serializable]
    public class LootItem
    {
        public ItemData itemData;   // 통합 ItemData 참조
        public int amount;
        public bool isRevealed;

        // 편의 프로퍼티
        public string ItemName => itemData?.itemName ?? "";
        public ItemType ItemType => itemData?.itemType ?? ItemType.Valuable;
        public Sprite Icon => itemData?.icon;
        public bool IsStackable => itemData?.isStackable ?? false;
        public int MaxStackSize => itemData?.maxStackSize ?? 1;

        // 타입 캐스팅 헬퍼
        public WeaponData WeaponData => itemData as WeaponData;
        public ArmorData ArmorData => itemData as ArmorData;
        public AmmoData AmmoData => itemData as AmmoData;

        public LootItem() { }

        public LootItem(ItemData data, int count = 1)
        {
            itemData = data;
            amount = count;
            isRevealed = false;
        }

        /// <summary>
        /// LootEntry에서 LootItem 생성
        /// </summary>
        public static LootItem FromLootEntry(LootEntry entry)
        {
            // LootEntry에서 ItemData 추출
            ItemData itemData = entry.GetItemData();

            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);

            return new LootItem(itemData, amount);
        }

        /// <summary>
        /// 아이템 표시 이름 생성
        /// </summary>
        public string GetDisplayName()
        {
            if (itemData == null) return "Unknown Item";

            if (IsStackable && amount > 1)
            {
                return $"{ItemName} x{amount}";
            }
            return ItemName;
        }

        /// <summary>
        /// 아이템 설명 생성
        /// </summary>
        public string GetDescription()
        {
            if (itemData == null) return "";

            string desc = itemData.description ?? "";

            // 타입별 추가 정보
            if (WeaponData != null)
            {
                desc = $"데미지: {WeaponData.baseDamage}\n" +
                       $"연사속도: {WeaponData.RPM:F0} RPM\n" +
                       $"장탄수: {WeaponData.magazineSize}";
            }
            else if (ArmorData != null)
            {
                desc = $"방어력: {ArmorData.defense}\n" +
                       $"이동속도 감소: {ArmorData.moveSpeedReduction * 100}%";
            }
            else if (AmmoData != null)
            {
                desc = $"{AmmoData.ammoType} 탄약 {amount}발";
            }

            return desc;
        }

        /// <summary>
        /// 아이콘 가져오기
        /// </summary>
        public Sprite GetIcon()
        {
            return itemData?.icon;
        }
    }
}
