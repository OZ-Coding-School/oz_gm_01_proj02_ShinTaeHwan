using UnityEngine;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Combat;
using MiniExtractionShooter.Core;

namespace MiniExtractionShooter.Weapon
{
    /// <summary>
    /// 무기 기본 클래스
    /// Escape from Duckov 스타일 무기 동작 구현
    /// </summary>
    public class WeaponBase : MonoBehaviour
    {
        [Header("Weapon Data")]
        [SerializeField] private WeaponData weaponData;

        [Header("Fire Point")]
        [SerializeField] private Transform firePoint;

        [Header("State")]
        [SerializeField] private int magazineAmmo;  // 탄창에 있는 탄약 (재장전 시점)
        [SerializeField] private bool isReloading = false;

        private float nextFireTime = 0f;
        private int consecutiveShots = 0;
        private float lastFireTime = 0f;
        private float consecutiveShotResetTime = 0.3f;

        private RecoilSystem recoilSystem;
        private SpreadSystem spreadSystem;
        private AimingSystem aimingSystem;

        // Events
        public event System.Action OnFired;
        public event System.Action OnReloadStart;
        public event System.Action OnReloadComplete;
        public event System.Action OnReloadCancelled;
        public event System.Action<int, int> OnAmmoChanged; // magazine, total reserve
        public event System.Action<float> OnReloadProgress; // 0-1 progress

        private Coroutine reloadCoroutine;

        public WeaponData Data => weaponData;
        public int MagazineAmmo => magazineAmmo;
        public int CurrentAmmo => magazineAmmo;  // 하위 호환용
        public int MagazineSize => weaponData?.magazineSize ?? 0; // HUD용
        public bool IsReloading => isReloading;
        public bool CanFire => !isReloading && magazineAmmo > 0 && Time.time >= nextFireTime;

        private void Awake()
        {
            recoilSystem = GetComponent<RecoilSystem>();
            spreadSystem = GetComponent<SpreadSystem>();

            if (spreadSystem == null)
            {
                spreadSystem = gameObject.AddComponent<SpreadSystem>();
            }
        }

        private void Start()
        {
            // AimingSystem 참조 (싱글톤)
            aimingSystem = AimingSystem.Instance;

            if (weaponData != null)
            {
                magazineAmmo = weaponData.magazineSize;
                NotifyAmmoChanged();

                // AimingSystem에 무기 데이터 전달
                aimingSystem?.SetWeaponData(weaponData);

                // Initialize pools
                if (weaponData.muzzleFlashPrefab != null)
                    PoolManager.Instance.CreatePool(weaponData.muzzleFlashPrefab, 100);

                Debug.Log($"Creating bullet trail pool {weaponData.bulletTrailPrefab}");
                if (weaponData.bulletTrailPrefab != null)
                    PoolManager.Instance.CreatePool(weaponData.bulletTrailPrefab, 100);
            }
        }

        private void Update()
        {
            // 연속 발사 카운터 리셋
            if (Time.time - lastFireTime > consecutiveShotResetTime)
            {
                consecutiveShots = 0;
            }
        }

        /// <summary>
        /// 무기 데이터 설정
        /// </summary>
        public void SetWeaponData(WeaponData data)
        {
            SetWeaponData(data, data.magazineSize);
        }

        /// <summary>
        /// 무기 데이터 설정 (탄창 수 지정)
        /// </summary>
        public void SetWeaponData(WeaponData data, int currentAmmo)
        {
            weaponData = data;
            magazineAmmo = Mathf.Clamp(currentAmmo, 0, data.magazineSize);
            isReloading = false;
            consecutiveShots = 0;

            NotifyAmmoChanged();

            // AimingSystem에 무기 데이터 전달
            if (aimingSystem == null)
            {
                aimingSystem = AimingSystem.Instance;
            }
            aimingSystem?.SetWeaponData(data);
        }

        /// <summary>
        /// 발사 시도
        /// </summary>
        public bool TryFire()
        {
            if (weaponData == null) return false;
            if (isReloading) return false;
            if (Time.time < nextFireTime) return false;

            if (magazineAmmo <= 0)
            {
                // 탄약 없음 - 재장전 필요
                PlayEmptySound();
                return false;
            }

            Fire();
            return true;
        }

        /// <summary>
        /// 실제 발사 처리
        /// </summary>
        private void Fire()
        {
            magazineAmmo--;
            nextFireTime = Time.time + weaponData.FireInterval; // 발사 간격 사용
            consecutiveShots++;
            lastFireTime = Time.time;

            // 레이캐스트 발사
            PerformRaycast();

            // 반동 적용
            ApplyRecoil();

            // AimingSystem에 발사 알림 (확산 증가)
            aimingSystem?.OnFired();

            // 이펙트
            PlayFireEffects();

            OnFired?.Invoke();
            NotifyAmmoChanged();

            // 탄약 소진 시 자동 재장전
            if (magazineAmmo <= 0)
            {
                TryReload();
            }
        }

        /// <summary>
        /// 레이캐스트로 피격 처리
        /// </summary>
        private void PerformRaycast()
        {

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector3 direction = firePoint != null ? firePoint.forward : transform.forward;

            // 1. 확산(Spread) 적용 - AimingSystem에서 현재 확산 값 가져옴
            if (spreadSystem != null && aimingSystem != null)
            {
                float currentSpread = aimingSystem.GetCurrentSpread();
                direction = spreadSystem.ApplySpreadToDirection(direction, currentSpread);
            }

            // 2. 반동에 의한 방향 편차 적용
            if (recoilSystem != null)
            {
                direction = recoilSystem.ApplyRecoilToDirection(direction);
            }

            Vector3 endPoint = origin + direction * weaponData.maxRange;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, weaponData.maxRange))
            {
                endPoint = hit.point;

                // 거리 계산
                float distance = Vector3.Distance(origin, hit.point);

                // HitZone 확인
                HitZone hitZone = hit.collider.GetComponent<HitZone>();
                if (hitZone != null)
                {
                    // 데미지 계산 및 적용
                    float damage = DamageCalculator.CalculateDamage(
                        weaponData.baseDamage,
                        hitZone.Zone,
                        0f, // 방어력은 대상에서 처리
                        distance,
                        weaponData.effectiveRange,
                        weaponData.maxRange
                    );

                    hitZone.TakeDamage(damage);
                }

                // 히트 이펙트 생성
                SpawnHitEffect(hit.point, hit.normal);
            }

            // 총알 궤적 이펙트
            SpawnBulletTrail(origin, endPoint);

            Debug.DrawRay(origin, direction * weaponData.maxRange, Color.red, 0.5f);
        }

        /// <summary>
        /// 현재 반동 누적량 반환 (UI 등에서 사용)
        /// </summary>
        public float GetRecoilAccumulation()
        {
            if (recoilSystem == null) return 0f;
            return recoilSystem.GetSpreadContribution();
        }

        /// <summary>
        /// 반동 적용
        /// </summary>
        private void ApplyRecoil()
        {
            if (recoilSystem == null) return;

            float multiplier = weaponData.GetRecoilMultiplier(consecutiveShots);
            recoilSystem.ApplyRecoil(
                weaponData.verticalRecoil * multiplier,
                weaponData.horizontalRecoil * multiplier
            );
        }

        /// <summary>
        /// 재장전 시도
        /// </summary>
        public bool TryReload()
        {
            if (weaponData == null) return false;
            if (isReloading) return false;
            if (magazineAmmo >= weaponData.magazineSize) return false;

            // 인벤토리에서 탄약 확인
            if (PlayerInventory.Instance != null)
            {
                int availableAmmo = PlayerInventory.Instance.GetAmmo(weaponData.ammoType);
                if (availableAmmo <= 0) return false;
            }

            reloadCoroutine = StartCoroutine(ReloadCoroutine());
            return true;
        }

        /// <summary>
        /// 재장전 취소
        /// </summary>
        public void CancelReload()
        {
            if (!isReloading) return;

            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }

            isReloading = false;
            OnReloadCancelled?.Invoke();
            OnReloadProgress?.Invoke(0f);
        }

        /// <summary>
        /// 재장전 코루틴
        /// </summary>
        private System.Collections.IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            OnReloadStart?.Invoke();

            // 재장전 사운드
            if (weaponData.reloadSound != null)
            {
                AudioSource.PlayClipAtPoint(weaponData.reloadSound, transform.position);
            }

            float reloadTime = weaponData.reloadTime;
            float elapsedTime = 0f;

            // 재장전 진행률 업데이트
            while (elapsedTime < reloadTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / reloadTime);
                OnReloadProgress?.Invoke(progress);
                yield return null;
            }

            // 필요한 탄약 계산
            int neededAmmo = weaponData.magazineSize - magazineAmmo;

            if (PlayerInventory.Instance != null)
            {
                int availableAmmo = PlayerInventory.Instance.GetAmmo(weaponData.ammoType);
                int ammoToLoad = Mathf.Min(neededAmmo, availableAmmo);

                if (PlayerInventory.Instance.UseAmmo(weaponData.ammoType, ammoToLoad))
                {
                    magazineAmmo += ammoToLoad;
                }
            }
            else
            {
                // 테스트용 - 무한 탄약
                magazineAmmo = weaponData.magazineSize;
            }

            isReloading = false;
            reloadCoroutine = null;
            consecutiveShots = 0;

            OnReloadProgress?.Invoke(0f);
            OnReloadComplete?.Invoke();
            NotifyAmmoChanged();
        }

        /// <summary>
        /// 발사 이펙트
        /// </summary>
        private void PlayFireEffects()
        {
            // 발사 사운드
            if (weaponData.fireSound != null)
            {
                AudioSource.PlayClipAtPoint(weaponData.fireSound, transform.position);
            }

            // 머즐 플래시
            if (weaponData.muzzleFlashPrefab != null && firePoint != null)
            {
                var muzzleFlash = PoolManager.Instance.GetFromPool(weaponData.muzzleFlashPrefab);
                if (muzzleFlash != null)
                {
                    muzzleFlash.transform.position = firePoint.position;
                    muzzleFlash.transform.rotation = firePoint.rotation;
                    PoolManager.Instance.ReturnAfterDelay(muzzleFlash, 0.1f);
                }
            }
        }

        /// <summary>
        /// 탄약 없음 사운드
        /// </summary>
        private void PlayEmptySound()
        {
            if (weaponData.emptySound != null)
            {
                AudioSource.PlayClipAtPoint(weaponData.emptySound, transform.position);
            }
        }

        /// <summary>
        /// 히트 이펙트 생성
        /// </summary>
        private void SpawnHitEffect(Vector3 position, Vector3 normal)
        {
            // TODO: Hit effect 구현
        }

        /// <summary>
        /// 총알 궤적 생성
        /// </summary>
        private void SpawnBulletTrail(Vector3 start, Vector3 end)
        {
            if (weaponData.bulletTrailPrefab == null) return;

            var trail = PoolManager.Instance.GetFromPool(weaponData.bulletTrailPrefab);
            if (trail != null)
            {
                trail.transform.position = start;
                trail.transform.rotation = Quaternion.identity;
                // 무기의 탄속(muzzleVelocity)을 전달
                trail.Initialize(start, end, weaponData.muzzleVelocity);
            }
        }

        /// <summary>
        /// 현재 탄약 직접 설정 (적 드랍 등)
        /// </summary>
        public void SetCurrentAmmo(int ammo)
        {
            if (weaponData == null) return;
            magazineAmmo = Mathf.Clamp(ammo, 0, weaponData.magazineSize);
            NotifyAmmoChanged();
        }

        /// <summary>
        /// 탄약 변경 알림
        /// </summary>
        private void NotifyAmmoChanged()
        {
            int reserveAmmo = PlayerInventory.Instance?.GetAmmo(weaponData?.ammoType ?? AmmoType.Pistol) ?? 0;
            OnAmmoChanged?.Invoke(magazineAmmo, reserveAmmo);
        }
    }
}
