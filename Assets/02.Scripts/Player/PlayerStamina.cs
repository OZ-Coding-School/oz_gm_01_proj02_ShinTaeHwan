using UnityEngine;
using System;

namespace MiniExtractionShooter.Player
{
    /// <summary>
    /// 플레이어 스태미나 시스템
    /// 달리기, 구르기 시 스태미나 소모
    /// 일정 시간 후 자동 회복
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        public static PlayerStamina Instance { get; private set; }

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;

        [Header("Consumption")]
        [SerializeField] private float runDrainRate = 15f;    // 달리기 초당 소모
        [SerializeField] private float rollCost = 25f;        // 구르기 1회 소모

        [Header("Recovery")]
        [SerializeField] private float recoveryDelay = 2f;    // 회복 시작 대기 시간
        [SerializeField] private float recoveryRate = 30f;    // 초당 회복량

        [Header("Debug")]
        [SerializeField] private bool infiniteStamina = false;

        // 내부 상태
        private float lastConsumeTime;
        private bool isConsuming;

        // Events
        public event Action<float, float> OnStaminaChanged;   // current, max
        public event Action OnStaminaDepleted;
        public event Action OnStaminaRecovered;

        // Properties
        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public float StaminaPercentage => currentStamina / maxStamina;
        public bool IsDepleted => currentStamina <= 0;
        public bool IsRecovering => !isConsuming && Time.time - lastConsumeTime >= recoveryDelay;

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

            currentStamina = maxStamina;
        }

        private void Start()
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        private void Update()
        {
            if (infiniteStamina)
            {
                currentStamina = maxStamina;
                return;
            }

            // 소모 중이 아니고, 딜레이가 지났으면 회복
            if (!isConsuming && Time.time - lastConsumeTime >= recoveryDelay)
            {
                RecoverStamina();
            }

            // 매 프레임 소모 상태 리셋 (다음 프레임에 다시 설정됨)
            isConsuming = false;
        }

        /// <summary>
        /// 달리기 스태미나 소모 (매 프레임 호출)
        /// </summary>
        public bool ConsumeRunStamina()
        {
            if (infiniteStamina) return true;
            if (currentStamina <= 0) return false;

            float consumption = runDrainRate * Time.deltaTime;
            
            // PlayerStats의 영향 적용
            if (PlayerStats.Instance != null)
            {
                // 탈수 상태면 회복 페널티가 있지만, 소모는 동일
            }

            currentStamina = Mathf.Max(0, currentStamina - consumption);
            lastConsumeTime = Time.time;
            isConsuming = true;

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            if (currentStamina <= 0)
            {
                OnStaminaDepleted?.Invoke();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 구르기 스태미나 소모 (1회성)
        /// </summary>
        public bool ConsumeRollStamina()
        {
            if (infiniteStamina) return true;
            if (currentStamina < rollCost) return false;

            currentStamina -= rollCost;
            lastConsumeTime = Time.time;
            isConsuming = true;

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            if (currentStamina <= 0)
            {
                OnStaminaDepleted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 구르기 가능 여부 확인
        /// </summary>
        public bool CanRoll()
        {
            return infiniteStamina || currentStamina >= rollCost;
        }

        /// <summary>
        /// 달리기 가능 여부 확인
        /// </summary>
        public bool CanRun()
        {
            return infiniteStamina || currentStamina > 0;
        }

        /// <summary>
        /// 스태미나 회복
        /// </summary>
        private void RecoverStamina()
        {
            if (currentStamina >= maxStamina) return;

            float recovery = recoveryRate * Time.deltaTime;

            // PlayerStats의 탈수 상태 영향 적용
            if (PlayerStats.Instance != null)
            {
                recovery *= PlayerStats.Instance.StaminaRegenMultiplier;
            }

            float previousStamina = currentStamina;
            currentStamina = Mathf.Min(maxStamina, currentStamina + recovery);

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            // 완전 회복 시 이벤트
            if (previousStamina < maxStamina && currentStamina >= maxStamina)
            {
                OnStaminaRecovered?.Invoke();
            }
        }

        /// <summary>
        /// 스태미나 즉시 회복 (아이템 사용)
        /// </summary>
        public void RestoreStamina(float amount)
        {
            if (amount <= 0) return;

            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            Debug.Log($"[PlayerStamina] Restored {amount:F0} stamina. Current: {currentStamina:F0}");
        }

        /// <summary>
        /// 스태미나 완전 회복
        /// </summary>
        public void FullRestore()
        {
            currentStamina = maxStamina;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        /// <summary>
        /// 리스폰 시 초기화
        /// </summary>
        public void ResetStamina()
        {
            currentStamina = maxStamina;
            lastConsumeTime = -999f;
            isConsuming = false;

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }
}
