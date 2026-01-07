using UnityEngine;
using System;

namespace MiniExtractionShooter.Player
{
    public enum HealthState
    {
        Normal,     // 100~70
        Injured,    // 69~30
        Critical,   // 29~1
        Dead        // 0
    }

    /// <summary>
    /// 플레이어 체력 시스템
    /// TDD 기준: 최대 체력 100
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("State Thresholds")]
        [SerializeField] private float injuredThreshold = 70f;
        [SerializeField] private float criticalThreshold = 30f;

        [Header("Debug")]
        [SerializeField] private bool invincible = false;

        // Events
        public event Action<float, float> OnHealthChanged;          // current, max
        public event Action<HealthState> OnHealthStateChanged;
        public event Action<float> OnDamageTaken;                   // damage amount
        public event Action<float> OnHealed;                        // heal amount
        public event Action OnDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsDead => currentHealth <= 0;

        private HealthState currentState = HealthState.Normal;
        public HealthState CurrentState => currentState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            currentHealth = maxHealth;
        }

        private void Start()
        {
            UpdateHealthState();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// 데미지 적용
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (IsDead || invincible) return;

            float actualDamage = Mathf.Max(0, damage);
            currentHealth -= actualDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            OnDamageTaken?.Invoke(actualDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            UpdateHealthState();

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
            if (IsDead) return;

            float actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
            currentHealth += actualHeal;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            OnHealed?.Invoke(actualHeal);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            UpdateHealthState();
        }

        /// <summary>
        /// 체력 완전 회복
        /// </summary>
        public void FullHeal()
        {
            Heal(maxHealth);
        }

        /// <summary>
        /// 체력 상태 업데이트
        /// </summary>
        private void UpdateHealthState()
        {
            HealthState newState;

            if (currentHealth <= 0)
            {
                newState = HealthState.Dead;
            }
            else if (currentHealth < criticalThreshold)
            {
                newState = HealthState.Critical;
            }
            else if (currentHealth < injuredThreshold)
            {
                newState = HealthState.Injured;
            }
            else
            {
                newState = HealthState.Normal;
            }

            if (newState != currentState)
            {
                currentState = newState;
                OnHealthStateChanged?.Invoke(currentState);
            }
        }

        /// <summary>
        /// 사망 처리
        /// </summary>
        private void Die()
        {
            OnDeath?.Invoke();

            // 플레이어 행동 비활성화
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetCanMove(false);
                PlayerController.Instance.SetCanRotate(false);
            }

            Debug.Log("Player Died!");
        }

        /// <summary>
        /// 리스폰 (체력 초기화)
        /// </summary>
        public void Respawn()
        {
            currentHealth = maxHealth;
            currentState = HealthState.Normal;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHealthStateChanged?.Invoke(currentState);

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetCanMove(true);
                PlayerController.Instance.SetCanRotate(true);
            }
        }

        /// <summary>
        /// 최대 체력 설정 (난이도 조절용)
        /// </summary>
        public void SetMaxHealth(float newMaxHealth)
        {
            float healthPercent = currentHealth / maxHealth;
            maxHealth = newMaxHealth;
            currentHealth = maxHealth * healthPercent;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateHealthState();
        }
    }
}
