using UnityEngine;

namespace MiniExtractionShooter.Data
{
    /// <summary>
    /// 소모품 아이템 데이터 (회복, 음식 등)
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Scriptable Objects/Consumable Data")]
    public class ConsumableData : ItemData
    {
        [Header("Restoration")]
        public float restoreHealth = 0f;
        public float restoreEnergy = 0f;
        public float restoreHydration = 0f;

        [Header("Usage Settings")]
        [Tooltip("사용하는데 걸리는 시간 (초)")]
        public float useDuration = 2f;

        [Tooltip("사용 중 이동 속도 배율 (1 = 정상, 0 = 정지)")]
        [Range(0f, 1f)]
        public float speedMultiplierWhenUsing = 0.5f;

        [Header("Effects")]
        public GameObject useEffect; // 사용 시 이펙트 (옵션)
        public string useSoundName;  // 사용 시 사운드 이름
        public AudioClip useSound;   // 레거시: 사용 시 사운드 (옵션)

        private void OnEnable()
        {
            // 타입에 따라 자동 설정 (에디터 편의용)
            if (restoreHealth > 0 && restoreEnergy == 0 && restoreHydration == 0)
            {
                itemType = ItemType.Health;
            }
            else if (restoreEnergy > 0 || restoreHydration > 0)
            {
                itemType = ItemType.Food;
            }
        }
    }
}
