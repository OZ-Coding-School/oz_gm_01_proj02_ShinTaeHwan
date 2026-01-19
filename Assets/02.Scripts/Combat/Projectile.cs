using UnityEngine;
using MiniExtractionShooter.Core;

namespace MiniExtractionShooter.Combat
{
    /// <summary>
    /// 투사체 클래스 (물리 기반 탄환용)
    /// 기본적으로는 레이캐스트 사용, 느린 투사체가 필요할 때 사용
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 100f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private float effectiveRange = 15f;
        [SerializeField] private float maxRange = 30f;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private TrailRenderer trailRenderer;

        private Vector3 startPosition;
        private float spawnTime;
        private bool hasHit = false;

        // 발사자 정보 (아군 피격 방지용)
        private GameObject owner;
        private string ownerTag;

        public void Initialize(float projectileDamage, float projectileSpeed, float effective, float max, GameObject projectileOwner)
        {
            damage = projectileDamage;
            speed = projectileSpeed;
            effectiveRange = effective;
            maxRange = max;
            owner = projectileOwner;
            ownerTag = projectileOwner?.tag ?? "";

            startPosition = transform.position;
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (hasHit) return;

            // 이동
            transform.position += transform.forward * speed * Time.deltaTime;

            // 거리 체크
            float distance = Vector3.Distance(startPosition, transform.position);
            if (distance >= maxRange)
            {
                Destroy(gameObject);
                return;
            }

            // 수명 체크
            if (Time.time - spawnTime >= maxLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // 발사자 무시
            if (other.gameObject == owner) return;
            if (!string.IsNullOrEmpty(ownerTag) && other.CompareTag(ownerTag)) return;

            hasHit = true;

            // 거리에 따른 데미지 감쇠
            float distance = Vector3.Distance(startPosition, transform.position);
            float falloff = DamageCalculator.GetDistanceFalloff(distance, effectiveRange, maxRange);
            float finalDamage = damage * falloff;

            // 데미지 적용
            HitZone hitZone = other.GetComponent<HitZone>();
            if (hitZone != null)
            {
                hitZone.TakeDamage(finalDamage);
            }

            // 히트 이펙트
            SpawnHitEffect(transform.position, -transform.forward);

            // 투사체 제거
            Destroy(gameObject);
        }

        private void SpawnHitEffect(Vector3 position, Vector3 normal)
        {
            if (hitEffectPrefab == null) return;

            // hitEffectPrefab is GameObject, need to get/create a component for pooling
            // Try to get any MonoBehaviour component or add a generic one
            MonoBehaviour component = hitEffectPrefab.GetComponent<MonoBehaviour>();
            if (component == null)
            {
                // If no component exists, instantiate directly (not pooled)
                GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
                Destroy(effect, 2f);
                return;
            }

            var effectInstance = PoolManager.Instance.GetFromPool(component);
            if (effectInstance != null)
            {
                effectInstance.transform.position = position;
                effectInstance.transform.rotation = Quaternion.LookRotation(normal);
                PoolManager.Instance.ReturnAfterDelay(effectInstance, 2f);
            }
        }

        /// <summary>
        /// 투사체 생성 헬퍼 메서드
        /// </summary>
        public static Projectile Create(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            float damage,
            float speed,
            float effectiveRange,
            float maxRange,
            GameObject owner)
        {
            GameObject projectileObj = Instantiate(prefab, position, rotation);
            Projectile projectile = projectileObj.GetComponent<Projectile>();

            if (projectile == null)
            {
                projectile = projectileObj.AddComponent<Projectile>();
            }

            projectile.Initialize(damage, speed, effectiveRange, maxRange, owner);
            return projectile;
        }
    }
}
