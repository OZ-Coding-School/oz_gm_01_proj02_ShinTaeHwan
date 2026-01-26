using UnityEngine;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Combat;
using MiniExtractionShooter.Core;

namespace MiniExtractionShooter.Enemy
{
    /// <summary>
    /// 적 체력 시스템
    /// TDD 기준: Guard 체력 60
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private float currentHealth;

        [Header("Armor")]
        [SerializeField] private ArmorData equippedArmor;

        [Header("State")]
        [SerializeField] private bool isDead = false;

        // Events
        public event System.Action<float, float> OnHealthChanged;   // current, max
        public event System.Action<float> OnDamageTaken;
        public event System.Action OnDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsDead => isDead;

        private Enemy enemy;
        private EnemyData EnemyData => enemy != null ? enemy.Data : null;
        
        private HitboxManager hitboxManager;
        private EnemyDropSystem dropSystem;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            hitboxManager = GetComponent<HitboxManager>();
            dropSystem = GetComponent<EnemyDropSystem>();
        }

        private void Start()
        {
            if (EnemyData != null)
            {
                maxHealth = EnemyData.health;
            }
            currentHealth = maxHealth;

            // HitboxManager 이벤트 연결
            if (hitboxManager != null)
            {
                hitboxManager.OnDamageReceived += HandleDamageReceived;
            }
        }

        private void OnDestroy()
        {
            if (hitboxManager != null)
            {
                hitboxManager.OnDamageReceived -= HandleDamageReceived;
            }
        }

        /// <summary>
        /// HitboxManager에서 데미지 수신
        /// </summary>
        private void HandleDamageReceived(float damage, HitZoneType zone)
        {
            // 방어력 적용 (별도 처리 필요 시)
            if (equippedArmor != null && zone != HitZoneType.Head)
            {
                damage = equippedArmor.ApplyDefense(damage);
            }

            TakeDamage(damage);
        }

        /// <summary>
        /// IDamageable 구현 - 데미지 받기
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead) return;

            float actualDamage = Mathf.Max(0, damage);
            currentHealth -= actualDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            OnDamageTaken?.Invoke(actualDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // 피격 피드백 (선택적)
            PlayHitFeedback();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// 사망 처리
        /// </summary>
        private void Die()
        {
            if (isDead) return;

            isDead = true;

            OnDeath?.Invoke();

            // AI 비활성화
            EnemyAI ai = GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.enabled = false;
            }

            // 전투 비활성화
            EnemyCombat combat = GetComponent<EnemyCombat>();
            if (combat != null)
            {
                combat.enabled = false;
            }

            // 히트박스 비활성화
            if (hitboxManager != null)
            {
                hitboxManager.SetHitZonesActive(false);
            }

            // 드랍 시스템 활성화
            if (dropSystem != null)
            {
                dropSystem.OnDeath();
            }

            // 사망 이펙트
            PlayDeathEffect();

            string soundName = (EnemyData != null && !string.IsNullOrEmpty(EnemyData.deathSoundName)) 
                ? EnemyData.deathSoundName 
                : "EnemyDie";
            Managers.SoundManager.Instance?.PlaySFX(soundName, transform.position);

            Destroy(gameObject);

            // Debug.Log($"Enemy '{gameObject.name}' died!");
        }

        /// <summary>
        /// 피격 피드백
        /// </summary>
        private void PlayHitFeedback()
        {
            // TODO: 피격 애니메이션 등
            Managers.SoundManager.Instance?.PlaySFX("EnemyHit", transform.position);
        }

        /// <summary>
        /// 사망 이펙트
        /// </summary>
        private void PlayDeathEffect()
        {
            if (EnemyData != null && EnemyData.deathEffectPrefab != null)
            {
                // deathEffectPrefab is GameObject, need to get/create a component for pooling
                MonoBehaviour component = EnemyData.deathEffectPrefab.GetComponent<MonoBehaviour>();
                if (component == null)
                {
                    // If no component exists, instantiate directly (not pooled)
                    GameObject effect = Instantiate(EnemyData.deathEffectPrefab, transform.position, Quaternion.identity);
                    Destroy(effect, 3f);
                    return;
                }

                var effectInstance = PoolManager.Instance.GetFromPool(component);
                if (effectInstance != null)
                {
                    effectInstance.transform.position = transform.position;
                    effectInstance.transform.rotation = Quaternion.identity;
                    PoolManager.Instance.ReturnAfterDelay(effectInstance, 3f);
                }
            }

            // 레그돌 또는 사망 애니메이션
            // TODO: 구현
        }

        /// <summary>
        /// 방어구 장착
        /// </summary>
        public void EquipArmor(ArmorData armor)
        {
            equippedArmor = armor;
        }

        /// <summary>
        /// 현재 방어력 가져오기
        /// </summary>
        public float GetCurrentDefense()
        {
            return equippedArmor != null ? equippedArmor.defense : 0f;
        }
    }
}
