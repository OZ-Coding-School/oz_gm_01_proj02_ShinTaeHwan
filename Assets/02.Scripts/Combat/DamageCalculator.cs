using UnityEngine;

namespace MiniExtractionShooter.Combat
{
    public enum HitZoneType
    {
        Head,
        Body,
        Arms,
        Legs
    }

    /// <summary>
    /// 데미지 계산 유틸리티
    /// TDD 공식: 최종 데미지 = (기본 데미지 × 부위 배율 - 방어력) × 거리 감쇠
    /// </summary>
    public static class DamageCalculator
    {
        // 부위별 데미지 배율 (TDD 기준)
        private static readonly float HEAD_MULTIPLIER = 2.5f;
        private static readonly float BODY_MULTIPLIER = 1.0f;
        private static readonly float ARMS_MULTIPLIER = 0.7f;
        private static readonly float LEGS_MULTIPLIER = 0.8f;

        // 최소 데미지 비율 (방어력 적용 시)
        private static readonly float MINIMUM_DAMAGE_RATIO = 0.2f;

        /// <summary>
        /// 부위별 데미지 배율 반환
        /// </summary>
        public static float GetZoneMultiplier(HitZoneType zone)
        {
            return zone switch
            {
                HitZoneType.Head => HEAD_MULTIPLIER,
                HitZoneType.Body => BODY_MULTIPLIER,
                HitZoneType.Arms => ARMS_MULTIPLIER,
                HitZoneType.Legs => LEGS_MULTIPLIER,
                _ => BODY_MULTIPLIER
            };
        }

        /// <summary>
        /// 거리 감쇠 계산
        /// 유효 사거리 내: 100%
        /// 유효 ~ 최대 사거리: 선형 감쇠
        /// 최대 사거리 초과: 0%
        /// </summary>
        public static float GetDistanceFalloff(float distance, float effectiveRange, float maxRange)
        {
            if (distance <= effectiveRange)
            {
                return 1.0f;
            }

            if (distance >= maxRange)
            {
                return 0f;
            }

            // 선형 감쇠
            float falloffRange = maxRange - effectiveRange;
            float distanceBeyondEffective = distance - effectiveRange;
            float falloffPercent = 1f - (distanceBeyondEffective / falloffRange);

            return falloffPercent;
        }

        /// <summary>
        /// 방어력 적용 (머리 제외)
        /// </summary>
        public static float ApplyArmor(float damage, float armor, HitZoneType zone)
        {
            // 머리는 방어구 적용 안 됨
            if (zone == HitZoneType.Head)
            {
                return damage;
            }

            float reducedDamage = damage - armor;
            float minimumDamage = damage * MINIMUM_DAMAGE_RATIO;

            return Mathf.Max(reducedDamage, minimumDamage);
        }

        /// <summary>
        /// 최종 데미지 계산 (전체 공식)
        /// </summary>
        public static float CalculateDamage(
            float baseDamage,
            HitZoneType hitZone,
            float armor,
            float distance,
            float effectiveRange,
            float maxRange)
        {
            // 1. 부위 배율 적용
            float zoneMultiplier = GetZoneMultiplier(hitZone);
            float zoneDamage = baseDamage * zoneMultiplier;

            // 2. 방어력 적용 (머리 제외)
            float armoredDamage = ApplyArmor(zoneDamage, armor, hitZone);

            // 3. 거리 감쇠 적용
            float falloff = GetDistanceFalloff(distance, effectiveRange, maxRange);
            float finalDamage = armoredDamage * falloff;

            return Mathf.Max(0f, finalDamage);
        }

        /// <summary>
        /// 간단한 데미지 계산 (거리 감쇠 없음)
        /// </summary>
        public static float CalculateSimpleDamage(float baseDamage, HitZoneType hitZone, float armor)
        {
            float zoneMultiplier = GetZoneMultiplier(hitZone);
            float zoneDamage = baseDamage * zoneMultiplier;
            return ApplyArmor(zoneDamage, armor, hitZone);
        }

        /// <summary>
        /// 예상 TTK (Time To Kill) 계산
        /// </summary>
        public static float CalculateTTK(
            float targetHealth,
            float baseDamage,
            float fireRate,
            HitZoneType hitZone = HitZoneType.Body,
            float armor = 0f)
        {
            float damagePerShot = CalculateSimpleDamage(baseDamage, hitZone, armor);
            if (damagePerShot <= 0) return float.MaxValue;

            int shotsToKill = Mathf.CeilToInt(targetHealth / damagePerShot);
            float timeToKill = (shotsToKill - 1) * fireRate; // 첫 발은 즉시

            return timeToKill;
        }

        /// <summary>
        /// 데미지 정보 문자열 (디버그용)
        /// </summary>
        public static string GetDamageInfo(
            float baseDamage,
            HitZoneType hitZone,
            float armor,
            float distance,
            float effectiveRange,
            float maxRange)
        {
            float zoneMult = GetZoneMultiplier(hitZone);
            float zoneDamage = baseDamage * zoneMult;
            float armoredDamage = ApplyArmor(zoneDamage, armor, hitZone);
            float falloff = GetDistanceFalloff(distance, effectiveRange, maxRange);
            float finalDamage = armoredDamage * falloff;

            return $"Base: {baseDamage} × Zone({hitZone}): {zoneMult} = {zoneDamage}\n" +
                   $"- Armor: {armor} = {armoredDamage}\n" +
                   $"× Falloff({distance}m): {falloff:P0} = {finalDamage:F1}";
        }
    }
}
