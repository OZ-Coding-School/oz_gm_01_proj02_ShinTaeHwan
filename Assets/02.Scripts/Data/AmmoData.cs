using UnityEngine;

namespace MiniExtractionShooter.Data
{
    /// <summary>
    /// Ammo ScriptableObject - 탄약 데이터
    /// ItemData를 상속하여 통합 아이템 시스템 지원
    /// </summary>
    [CreateAssetMenu(fileName = "NewAmmo", menuName = "MiniExtractionShooter/Ammo Data")]
    public class AmmoData : ItemData
    {
        [Header("Ammo Type")]
        public AmmoType ammoType = AmmoType.Pistol;

        [Header("Loot")]
        [Tooltip("한 번에 드랍되는 기본 수량")]
        public int defaultDropAmount = 12;

        private void OnEnable()
        {
            // AmmoData는 기본적으로 스택 가능
            itemType = ItemType.Ammo;
            isStackable = true;
            if (maxStackSize <= 1) maxStackSize = 60;
        }
    }
}
