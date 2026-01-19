using UnityEngine;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.UI.Inventory
{
    public class EquipmentSlot : InventorySlot
    {
        [Header("Equipment Settings")]
        [SerializeField] private ItemType acceptedType;
        [SerializeField] private int equipmentIndex = 0; // 0: Primary, 1: Secondary for weapons

        public ItemType AcceptedType => acceptedType;
        public int EquipmentIndex => equipmentIndex;

        public override void SetItem(InventoryItem item)
        {
            if (item != null && item.ItemType != acceptedType)
            {
                Debug.LogWarning($"Trying to set {item.ItemType} in {acceptedType} slot");
                return;
            }
            base.SetItem(item);
        }

        public bool CanAccept(InventoryItem item)
        {
            return item != null && item.ItemType == acceptedType;
        }
    }
}
