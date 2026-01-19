using UnityEngine;

namespace MiniExtractionShooter.Data
{
    public enum WeaponType
    {
        Pistol,
        Rifle
    }

    public enum AmmoType
    {
        Pistol,
        Rifle
    }

    /// <summary>
    /// Weapon ScriptableObject - Escape from Duckov 스타일 무기 데이터
    /// ItemData를 상속하여 통합 아이템 시스템 지원
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "MiniExtractionShooter/Weapon Data")]
    public class WeaponData : ItemData
    {
        [Header("Weapon Type")]
        public WeaponType weaponType = WeaponType.Pistol;
        public AmmoType ammoType = AmmoType.Pistol;
        public GameObject weaponPrefab;

        [Header("Physical Properties")]
        [Tooltip("무기 무게 (kg)")]
        public float weight = 2f;

        [Tooltip("구경 타입")]
        public string caliber = "9mm";

        [Header("Damage")]
        [Tooltip("기본 데미지")]
        public float baseDamage = 28.5f;

        [Tooltip("치명타 데미지 배율")]
        public float critMultiplier = 1.6f;

        [Header("Fire Rate")]
        [Tooltip("발사 속도 (초당 발사 수)")]
        public float fireRate = 2.5f;

        [Header("Magazine")]
        [Tooltip("탄창 용량")]
        public int magazineSize = 7;

        [Tooltip("장전 시간 (초)")]
        public float reloadTime = 3f;

        [Header("Ballistics")]
        [Tooltip("탄속 (m/s)")]
        public float muzzleVelocity = 93f;

        [Tooltip("사거리 (m)")]
        public float effectiveRange = 25.2f;

        [Tooltip("최대 사거리 (m) - 데미지 0이 되는 거리")]
        public float maxRange = 50f;

        [Header("Spread Settings")]
        [Tooltip("힙파이어 확산 (도)")]
        public float hipFireSpread = 16.6f;

        [Tooltip("조준 확산 (도)")]
        public float adsSpread = 10.14f;

        [Tooltip("확산 회복 속도 (도/초)")]
        public float spreadRecovery = 15f;

        [Header("Recoil")]
        [Tooltip("수직 반동")]
        public float verticalRecoil = 113.5f;

        [Tooltip("수평 반동")]
        public float horizontalRecoil = 94f;

        [Tooltip("반동 회복 속도 (도/초)")]
        public float recoilRecovery = 10f;

        [Header("ADS Settings")]
        [Tooltip("조준 전환 시간 (초)")]
        public float adsTime = 0.65f;

        [Tooltip("조준 시 이동 속도 계수 (0.0 ~ 1.0)")]
        [Range(0f, 1f)]
        public float adsMoveModifier = 0.55f;

        [Header("Movement")]
        [Tooltip("기본 이동 속도 계수 (무기 무게 영향)")]
        [Range(0f, 1f)]
        public float moveSpeedModifier = 0.92f;

        [Header("Detection")]
        [Tooltip("발사음 감지 범위 (m)")]
        public float soundRange = 32.4f;

        [Header("Special")]
        [Tooltip("폭발 데미지 계수")]
        public float explosionDamageMultiplier = 1f;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;

        [Header("Visual")]
        public ParticleSystem muzzleFlashPrefab;
        public MiniExtractionShooter.Weapon.BulletTrailController bulletTrailPrefab;

        /// <summary>
        /// 분당 발사 수 (RPM) 계산
        /// </summary>
        public float RPM => fireRate * 60f;

        /// <summary>
        /// 발사 간격 (초) 계산
        /// </summary>
        public float FireInterval => 1f / fireRate;

        /// <summary>
        /// 연속 발사 수에 따른 반동 배율 계산
        /// </summary>
        public float GetRecoilMultiplier(int consecutiveShots)
        {
            if (consecutiveShots <= 3) return 1.0f;
            if (consecutiveShots <= 6) return 1.3f;
            if (consecutiveShots <= 10) return 1.6f;
            return 2.0f;
        }

        /// <summary>
        /// 연속 발사 수에 따른 확산 배율 계산
        /// </summary>
        public float GetSpreadMultiplier(int consecutiveShots)
        {
            if (consecutiveShots <= 3) return 1.0f;
            if (consecutiveShots <= 6) return 1.2f;
            if (consecutiveShots <= 10) return 1.5f;
            return 2.0f;
        }
    }
}
