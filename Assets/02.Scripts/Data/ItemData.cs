using UnityEngine;

namespace MiniExtractionShooter.Data
{
    /// <summary>
    /// 모든 아이템의 기본 ScriptableObject
    /// WeaponData, ArmorData, AmmoData가 이 클래스를 상속
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName = "New Item";
        public ItemType itemType;
        public Sprite icon;

        [Header("Stack Settings")]
        public bool isStackable = false;
        public int maxStackSize = 1;

        [Header("Description")]
        [TextArea(2, 4)]
        public string description;

        /// <summary>
        /// 아이템이 스택 가능한지 확인
        /// </summary>
        public bool CanStack(ItemData other)
        {
            if (!isStackable || other == null) return false;
            return this == other; // 같은 ScriptableObject 인스턴스인 경우만 스택 가능
        }
    }
}
