using UnityEngine;

namespace MiniExtractionShooter.Data
{
    public enum ArmorTier
    {
        None = 0,
        Light = 1,      // 경량 조끼
        Tactical = 2,   // 전술 조끼
        Heavy = 3       // 중장갑
    }

    /// <summary>
    /// Armor ScriptableObject - TDD 기반 방어구 데이터
    /// ItemData를 상속하여 통합 아이템 시스템 지원
    /// </summary>
    [CreateAssetMenu(fileName = "NewArmor", menuName = "MiniExtractionShooter/Armor Data")]
    public class ArmorData : ItemData
    {
        [Header("Armor Type")]
        public ArmorTier tier = ArmorTier.Light;

        [Header("Stats")]
        [Tooltip("방어력 (경량: 10, 전술: 20, 중장갑: 35)")]
        public float defense = 10f;

        [Tooltip("이동 속도 감소율 (0~1, 경량: 0, 전술: 0.05, 중장갑: 0.15)")]
        [Range(0f, 0.5f)]
        public float moveSpeedReduction = 0f;

        [Header("Loot")]
        [Tooltip("상자에서 획득 확률 (경량: 0.30, 전술: 0.15, 중장갑: 0.05)")]
        [Range(0f, 1f)]
        public float dropChance = 0.30f;

        /// <summary>
        /// 방어력 적용 후 데미지 계산 (머리 제외)
        /// </summary>
        public float ApplyDefense(float rawDamage)
        {
            float reducedDamage = rawDamage - defense;
            float minimumDamage = rawDamage * 0.2f; // 최소 20% 데미지 보장
            return Mathf.Max(reducedDamage, minimumDamage);
        }
    }
}
