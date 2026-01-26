using UnityEngine;
using System;

namespace MiniExtractionShooter.Player
{
    /// <summary>
    /// 플레이어 추가 스탯 시스템 (수분, 에너지)
    /// 수분 0: 스태미나 회복 속도 감소
    /// 에너지 0: HP 지속 감소
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Hydration (수분)")]
        [SerializeField] private float maxHydration = 100f;
        [SerializeField] private float currentHydration;
        [SerializeField] private float hydrationDecayRate = 1f; // 분당 감소량
        [SerializeField] private float staminaRegenPenalty = 0.5f; // 수분 0일 때 스태미나 회복 50% 감소

        [Header("Energy (에너지)")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float currentEnergy;
        [SerializeField] private float energyDecayRate = 0.8f; // 분당 감소량
        [SerializeField] private float energyDepletionDamage = 2f; // 에너지 0일 때 초당 HP 감소량

        [Header("Debug")]
        [SerializeField] private bool enableDecay = true;

        private float decayTimer = 0f;
        private const float DECAY_INTERVAL = 1f; // 1초마다 감소 체크

        // Events
        public event Action<float, float> OnHydrationChanged; // current, max
        public event Action<float, float> OnEnergyChanged;    // current, max
        public event Action OnHydrationDepleted;
        public event Action OnEnergyDepleted;

        // Properties
        public float MaxHydration => maxHydration;
        public float CurrentHydration => currentHydration;
        public float HydrationPercentage => currentHydration / maxHydration;
        public bool IsDehydrated => currentHydration <= 0;

        public float MaxEnergy => maxEnergy;
        public float CurrentEnergy => currentEnergy;
        public float EnergyPercentage => currentEnergy / maxEnergy;
        public bool IsExhausted => currentEnergy <= 0;

        /// <summary>
        /// 스태미나 회복 속도 배율 (수분 영향)
        /// </summary>
        public float StaminaRegenMultiplier => IsDehydrated ? staminaRegenPenalty : 1f;

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

            // 초기화
            currentHydration = maxHydration;
            currentEnergy = maxEnergy;
        }

        private void Start()
        {
            // 초기 상태 알림
            OnHydrationChanged?.Invoke(currentHydration, maxHydration);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        private void Update()
        {
            if (!enableDecay) return;
            if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead) return;

            decayTimer += Time.deltaTime;
            if (decayTimer >= DECAY_INTERVAL)
            {
                decayTimer = 0f;
                ApplyDecay();
            }

            // 에너지 고갈 시 HP 감소
            if (IsExhausted)
            {
                ApplyExhaustionDamage();
            }
        }

        /// <summary>
        /// 수분과 에너지 감소 적용
        /// </summary>
        private void ApplyDecay()
        {
            // 분당 감소량을 초당으로 변환
            float hydrationDecay = hydrationDecayRate / 60f;
            float energyDecay = energyDecayRate / 60f;

            // 수분 감소
            if (currentHydration > 0)
            {
                currentHydration = Mathf.Max(0, currentHydration - hydrationDecay);
                OnHydrationChanged?.Invoke(currentHydration, maxHydration);

                if (currentHydration <= 0)
                {
                    OnHydrationDepleted?.Invoke();
                    // Debug.Log("[PlayerStats] Hydration depleted! Stamina regen reduced.");
                }
            }

            // 에너지 감소
            if (currentEnergy > 0)
            {
                currentEnergy = Mathf.Max(0, currentEnergy - energyDecay);
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

                if (currentEnergy <= 0)
                {
                    OnEnergyDepleted?.Invoke();
                    // Debug.Log("[PlayerStats] Energy depleted! HP will decrease.");
                }
            }
        }

        /// <summary>
        /// 에너지 고갈 시 HP 지속 감소
        /// </summary>
        private void ApplyExhaustionDamage()
        {
            if (PlayerHealth.Instance != null)
            {
                float damage = energyDepletionDamage * Time.deltaTime;
                PlayerHealth.Instance.TakeDamage(damage);
            }
        }

        /// <summary>
        /// 수분 회복 (물 아이템 사용)
        /// </summary>
        public void RestoreHydration(float amount)
        {
            if (amount <= 0) return;

            float previousHydration = currentHydration;
            currentHydration = Mathf.Min(currentHydration + amount, maxHydration);

            OnHydrationChanged?.Invoke(currentHydration, maxHydration);

            // Debug.Log($"[PlayerStats] Hydration restored: {previousHydration:F1} -> {currentHydration:F1}");
        }

        /// <summary>
        /// 에너지 회복 (음식 아이템 사용)
        /// </summary>
        public void RestoreEnergy(float amount)
        {
            if (amount <= 0) return;

            float previousEnergy = currentEnergy;
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);

            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

            // Debug.Log($"[PlayerStats] Energy restored: {previousEnergy:F1} -> {currentEnergy:F1}");
        }

        /// <summary>
        /// 수분과 에너지 모두 완전 회복
        /// </summary>
        public void FullRestore()
        {
            currentHydration = maxHydration;
            currentEnergy = maxEnergy;

            OnHydrationChanged?.Invoke(currentHydration, maxHydration);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        /// <summary>
        /// 리스폰 시 초기화
        /// </summary>
        public void ResetStats()
        {
            currentHydration = maxHydration;
            currentEnergy = maxEnergy;
            decayTimer = 0f;

            OnHydrationChanged?.Invoke(currentHydration, maxHydration);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        #region Save/Load Support

        /// <summary>
        /// 현재 상태 저장용 데이터 반환
        /// </summary>
        public (float hydration, float energy) GetSaveData()
        {
            return (currentHydration, currentEnergy);
        }

        /// <summary>
        /// 저장된 데이터 로드
        /// </summary>
        public void LoadData(float hydration, float energy)
        {
            currentHydration = Mathf.Clamp(hydration, 0, maxHydration);
            currentEnergy = Mathf.Clamp(energy, 0, maxEnergy);

            OnHydrationChanged?.Invoke(currentHydration, maxHydration);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        #endregion
    }
}
