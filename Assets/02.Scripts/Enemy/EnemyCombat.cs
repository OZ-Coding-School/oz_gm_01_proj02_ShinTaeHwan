using UnityEngine;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Combat;
using MiniExtractionShooter.Core;
using MiniExtractionShooter.Weapon;

namespace MiniExtractionShooter.Enemy
{
    /// <summary>
    /// 적 전투 시스템 (공격, 명중률)
    /// TDD 기준: 명중률 60%, 거리/이동에 따라 감소
    /// </summary>
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Fire Point")]
        [SerializeField] private Transform firePoint;

        [Header("State")]
        [SerializeField] private int currentAmmo;
        [SerializeField] private bool isReloading = false;

        private float nextAttackTime = 0f;
        private Enemy enemy;
        private EnemyData EnemyData => enemy != null ? enemy.Data : null;
        private EnemyAI enemyAI;

        // Events
        public event System.Action OnFired;
        public event System.Action OnReloadStart;
        public event System.Action OnReloadComplete;

        public int CurrentAmmo => currentAmmo;
        public bool IsReloading => isReloading;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            enemyAI = GetComponent<EnemyAI>();

            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        private void Start()
        {
            if (EnemyData != null && EnemyData.equippedWeapon != null)
            {
                // 탄약 랜덤 초기화
                currentAmmo = Random.Range(EnemyData.minAmmo, EnemyData.maxAmmo + 1);

                // Initialize pools
                if (EnemyData.equippedWeapon.muzzleFlashPrefab != null)
                    PoolManager.Instance.CreatePool(EnemyData.equippedWeapon.muzzleFlashPrefab, 10);

                if (EnemyData.equippedWeapon.bulletTrailPrefab != null)
                    PoolManager.Instance.CreatePool(EnemyData.equippedWeapon.bulletTrailPrefab, 15);
            }
        }

        private void OnDisable()
        {
            StopFiringLoop();
        }

        /// <summary>
        /// 공격 시도
        /// </summary>
        public bool TryAttack(Transform target)
        {
            if (EnemyData == null || EnemyData.equippedWeapon == null) return false;
            if (isReloading) return false;
            if (Time.time < nextAttackTime) return false;

            if (currentAmmo <= 0)
            {
                StartCoroutine(ReloadCoroutine());
                return false;
            }

            // 공격 실행
            Attack(target);
            return true;
        }

        /// <summary>
        /// 공격 실행
        /// </summary>
        private void Attack(Transform target)
        {
            StartFiringLoop(); // 연발 사운드 시작 (이미 재생 중이면 무시됨)

            currentAmmo--;
            nextAttackTime = Time.time + EnemyData.attackInterval;

            // 항상 레이캐스트 발사 (명중률은 확산으로 적용)
            PerformAttackRaycast(target);

            // 발사 이펙트
            PlayFireEffects();

            OnFired?.Invoke();

            // 탄약 소진 시 재장전
            if (currentAmmo <= 0)
            {
                StartCoroutine(ReloadCoroutine());
            }
        }



        /// <summary>
        /// 공격 레이캐스트 - WeaponBase와 동일한 방식
        /// 명중률을 확산으로 변환하여 적용
        /// </summary>
        private void PerformAttackRaycast(Transform target)
        {
            Vector3 origin = firePoint.position;

            // 플레이어 몸통 부근을 조준
            Vector3 targetPos = target.position + Vector3.up * 1.0f;
            Vector3 direction = (targetPos - origin).normalized;

            // 명중률 기반 확산 적용
            bool playerMoving = PlayerController.Instance?.IsMoving ?? false;
            float distance = Vector3.Distance(transform.position, target.position);
            float accuracy = EnemyData.CalculateAccuracy(distance, playerMoving);
            direction = ApplyAccuracySpread(direction, accuracy);

            // 레이캐스트 발사
            float maxRange = EnemyData.equippedWeapon.maxRange;
            Vector3 endPoint = origin + direction * maxRange;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange))
            {
                endPoint = hit.point;

                // HitZone 확인
                HitZone hitZone = hit.collider.GetComponent<HitZone>();
                if (hitZone != null)
                {
                    // 적 → 플레이어 데미지는 부위 배율 적용하지 않고 기본 데미지만 (밸런스)
                    float damage = EnemyData.equippedWeapon.baseDamage;

                    // 거리 감쇠 적용
                    float hitDistance = Vector3.Distance(origin, hit.point);
                    float falloff = DamageCalculator.GetDistanceFalloff(
                        hitDistance,
                        EnemyData.equippedWeapon.effectiveRange,
                        EnemyData.equippedWeapon.maxRange
                    );

                    hitZone.TakeDamage(damage * falloff);
                }
                else
                {
                    // PlayerHealth 직접 확인
                    PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                    if (playerHealth == null)
                    {
                        playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                    }

                    if (playerHealth != null)
                    {
                        float damage = EnemyData.equippedWeapon.baseDamage;
                        float hitDistance = Vector3.Distance(origin, hit.point);
                        float falloff = DamageCalculator.GetDistanceFalloff(
                            hitDistance,
                            EnemyData.equippedWeapon.effectiveRange,
                            EnemyData.equippedWeapon.maxRange
                        );

                        // 플레이어 방어력 적용
                        float armor = 0f;
                        if (PlayerInventory.Instance != null)
                        {
                            armor = PlayerInventory.Instance.GetCurrentDefense();
                        }

                        float finalDamage = DamageCalculator.ApplyArmor(damage * falloff, armor, HitZoneType.Body);
                        playerHealth.TakeDamage(finalDamage);
                    }
                }

                // 히트 이펙트
                SpawnHitEffect(hit.point, hit.normal);
            }

            // BulletTrail 생성 (명중 여부와 관계없이 항상 생성)
            SpawnBulletTrail(origin, endPoint);

            Debug.DrawRay(origin, direction * maxRange, Color.yellow, 0.5f);
        }

        /// <summary>
        /// 재장전 코루틴
        /// </summary>
        private System.Collections.IEnumerator ReloadCoroutine()
        {
            StopFiringLoop(); // 재장전 시 루프 중단

            if (EnemyData?.equippedWeapon == null) yield break;

            isReloading = true;
            OnReloadStart?.Invoke();

            yield return new WaitForSeconds(EnemyData.equippedWeapon.reloadTime);

            currentAmmo = EnemyData.equippedWeapon.magazineSize;
            isReloading = false;

            OnReloadComplete?.Invoke();
        }

        private AudioSource fireLoopSource;
        private float fireLoopStartTime;
        private Coroutine stopLoopCoroutine;

        public void StartFiringLoop()
        {
            if (EnemyData?.equippedWeapon != null && EnemyData.equippedWeapon.useLoopingFireSound)
            {
                // 탄약이 없거나 재장전 중이면 재생하지 않음
                if (currentAmmo <= 0 || isReloading) return;

                if (fireLoopSource == null && !string.IsNullOrEmpty(EnemyData.equippedWeapon.fireSoundName))
                {
                    if (stopLoopCoroutine != null)
                    {
                        StopCoroutine(stopLoopCoroutine);
                        stopLoopCoroutine = null;
                    }

                    fireLoopSource = Managers.SoundManager.Instance?.PlayLoopingSFX(EnemyData.equippedWeapon.fireSoundName, firePoint.position);
                    fireLoopStartTime = Time.time;
                }
            }
        }

        public void StopFiringLoop()
        {
            if (fireLoopSource != null)
            {
                if (EnemyData?.equippedWeapon == null)
                {
                    InternalStopLoop();
                    return;
                }

                float elapsedTime = Time.time - fireLoopStartTime;
                if (elapsedTime < EnemyData.equippedWeapon.minFireLoopDuration)
                {
                    float delay = EnemyData.equippedWeapon.minFireLoopDuration - elapsedTime;
                    if (stopLoopCoroutine == null)
                    {
                        stopLoopCoroutine = StartCoroutine(DelayedStopLoop(delay));
                    }
                }
                else
                {
                    InternalStopLoop();
                }
            }
        }

        private System.Collections.IEnumerator DelayedStopLoop(float delay)
        {
            yield return new WaitForSeconds(delay);
            InternalStopLoop();
            stopLoopCoroutine = null;
        }

        private void InternalStopLoop()
        {
            if (fireLoopSource != null)
            {
                Managers.SoundManager.Instance?.StopLoopingSFX(fireLoopSource);
                fireLoopSource = null;
            }
        }

        /// <summary>
        /// 발사 이펙트
        /// </summary>
        private void PlayFireEffects()
        {
            if (EnemyData?.equippedWeapon == null) return;

            // 발사 사운드 (루핑이 아닐 때만 원샷 재생)
            if (!EnemyData.equippedWeapon.useLoopingFireSound)
            {
                if (!string.IsNullOrEmpty(EnemyData.equippedWeapon.fireSoundName))
                {
                    Managers.SoundManager.Instance?.PlaySFX(EnemyData.equippedWeapon.fireSoundName, firePoint.position);
                }
                else if (EnemyData.equippedWeapon.fireSound != null)
                {
                    AudioSource.PlayClipAtPoint(EnemyData.equippedWeapon.fireSound, firePoint.position);
                }
            }

            // 머즐 플래시
            if (EnemyData.equippedWeapon.muzzleFlashPrefab != null)
            {
                var muzzleFlash = PoolManager.Instance.GetFromPool(EnemyData.equippedWeapon.muzzleFlashPrefab);
                if (muzzleFlash != null)
                {
                    muzzleFlash.transform.position = firePoint.position;
                    muzzleFlash.transform.rotation = firePoint.rotation;
                    PoolManager.Instance.ReturnAfterDelay(muzzleFlash, 0.1f);
                }
            }
        }

        /// <summary>
        /// 히트 이펙트 생성
        /// </summary>
        private void SpawnHitEffect(Vector3 position, Vector3 normal)
        {
            // TODO: 히트 이펙트 구현
        }

        /// <summary>
        /// 명중률을 확산 각도로 변환하여 방향에 적용
        /// accuracy 1.0 = 0도 확산 (완벽한 정확도)
        /// accuracy 0.0 = 최대 15도 확산
        /// </summary>
        private Vector3 ApplyAccuracySpread(Vector3 direction, float accuracy)
        {
            float maxSpreadAngle = 15f;
            float spreadAngle = maxSpreadAngle * (1f - accuracy);

            // 랜덤 확산 적용
            float randomAngleX = Random.Range(-spreadAngle, spreadAngle);
            float randomAngleY = Random.Range(-spreadAngle, spreadAngle);

            Quaternion spreadRotation = Quaternion.Euler(randomAngleX, randomAngleY, 0f);
            return spreadRotation * direction;
        }

        /// <summary>
        /// 총알 궤적 생성 (WeaponBase와 동일한 방식)
        /// </summary>
        private void SpawnBulletTrail(Vector3 start, Vector3 end)
        {
            if (EnemyData?.equippedWeapon?.bulletTrailPrefab == null) return;

            var trail = PoolManager.Instance.GetFromPool(EnemyData.equippedWeapon.bulletTrailPrefab);
            if (trail != null)
            {
                trail.transform.position = start;
                trail.transform.rotation = Quaternion.identity;
                trail.Initialize(start, end, EnemyData.equippedWeapon.muzzleVelocity);
            }
        }



        /// <summary>
        /// 잔여 탄약 반환 (드랍용)
        /// </summary>
        public int GetRemainingAmmo()
        {
            return currentAmmo;
        }
    }
}
