using UnityEngine;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Player;

namespace MiniExtractionShooter.Weapon
{
    /// <summary>
    /// 조준 시스템 - 힙파이어/ADS 상태 관리 및 확산 계산
    /// </summary>
    public class AimingSystem : MonoBehaviour
    {
        public static AimingSystem Instance { get; private set; }

        [Header("State")]
        [SerializeField] private AimState currentState = AimState.HipFire;
        [SerializeField] private float adsProgress = 0f; // 0 = 힙파이어, 1 = 완전 ADS

        [Header("Current Spread")]
        [SerializeField] private float currentSpread = 0f;
        [SerializeField] private float recoilSpreadContribution = 0f;

        [Header("Settings")]
        [SerializeField] private float spreadRecoveryMultiplier = 1f;

        // 현재 무기 데이터
        private WeaponData currentWeapon;

        // 연속 발사 카운터
        private int consecutiveShots = 0;
        private float lastShotTime = 0f;
        private const float CONSECUTIVE_SHOT_TIMEOUT = 0.3f;

        // Events
        public event System.Action<AimState> OnAimStateChanged;
        public event System.Action<float> OnADSProgressChanged;
        public event System.Action<float> OnSpreadChanged;
        public event System.Action OnWeaponFired;

        // Properties
        public AimState CurrentState => currentState;
        public float ADSProgress => adsProgress;
        public bool IsADS => currentState == AimState.ADS;
        public bool IsHipFire => currentState == AimState.HipFire;
        public bool IsTransitioning => currentState == AimState.TransitioningToADS ||
                                       currentState == AimState.TransitioningToHipFire;
        public float CurrentSpread => currentSpread;
        public int ConsecutiveShots => consecutiveShots;

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
        }

        private void Update()
        {
            UpdateADSTransition();
            UpdateSpreadRecovery();
            UpdateConsecutiveShotCounter();
            CalculateCurrentSpread();
        }

        /// <summary>
        /// 현재 무기 설정
        /// </summary>
        public void SetWeaponData(WeaponData weapon)
        {
            currentWeapon = weapon;

            // 무기 변경 시 상태 초기화
            adsProgress = 0f;
            currentState = AimState.HipFire;
            consecutiveShots = 0;
            recoilSpreadContribution = 0f;

            CalculateCurrentSpread();
        }

        /// <summary>
        /// ADS 시작 (우클릭 누름)
        /// </summary>
        public void StartADS()
        {
            if (currentWeapon == null) return;

            if (currentState == AimState.HipFire || currentState == AimState.TransitioningToHipFire)
            {
                SetState(AimState.TransitioningToADS);
            }
        }

        /// <summary>
        /// ADS 해제 (우클릭 뗌)
        /// </summary>
        public void StopADS()
        {
            if (currentState == AimState.ADS || currentState == AimState.TransitioningToADS)
            {
                SetState(AimState.TransitioningToHipFire);
            }
        }

        /// <summary>
        /// 발사 시 호출 - 반동으로 인한 확산 증가
        /// </summary>
        public void OnFired()
        {
            if (currentWeapon == null) return;

            consecutiveShots++;
            lastShotTime = Time.time;

            // 반동에 의한 확산 증가
            float recoilContribution = CalculateRecoilSpreadContribution();
            recoilSpreadContribution += recoilContribution;

            CalculateCurrentSpread();

            // 발사 이벤트 발송
            OnWeaponFired?.Invoke();
        }

        /// <summary>
        /// 현재 확산 각도 계산
        /// </summary>
        public float GetCurrentSpread()
        {
            return currentSpread;
        }

        /// <summary>
        /// 현재 이동 속도 계수 계산
        /// </summary>
        public float GetCurrentMoveModifier()
        {
            if (currentWeapon == null) return 1f;

            // 기본 무기 이동 속도 계수
            float baseModifier = currentWeapon.moveSpeedModifier;

            // ADS 시 추가 이동 속도 감소
            float adsModifier = Mathf.Lerp(1f, currentWeapon.adsMoveModifier, adsProgress);

            return baseModifier * adsModifier;
        }

        /// <summary>
        /// ADS 전환 업데이트
        /// </summary>
        private void UpdateADSTransition()
        {
            if (currentWeapon == null) return;

            float adsTime = currentWeapon.adsTime;
            if (adsTime <= 0f) adsTime = 0.3f; // 최소값

            float transitionSpeed = 1f / adsTime;

            switch (currentState)
            {
                case AimState.TransitioningToADS:
                    adsProgress += transitionSpeed * Time.deltaTime;
                    if (adsProgress >= 1f)
                    {
                        adsProgress = 1f;
                        SetState(AimState.ADS);
                    }
                    OnADSProgressChanged?.Invoke(adsProgress);
                    break;

                case AimState.TransitioningToHipFire:
                    adsProgress -= transitionSpeed * Time.deltaTime;
                    if (adsProgress <= 0f)
                    {
                        adsProgress = 0f;
                        SetState(AimState.HipFire);
                    }
                    OnADSProgressChanged?.Invoke(adsProgress);
                    break;
            }

            // 이동 속도 계수 업데이트
            UpdatePlayerMoveSpeed();
        }

        /// <summary>
        /// 확산 회복 업데이트
        /// </summary>
        private void UpdateSpreadRecovery()
        {
            if (currentWeapon == null) return;

            // 반동 확산 회복
            if (recoilSpreadContribution > 0f)
            {
                float recovery = currentWeapon.spreadRecovery * spreadRecoveryMultiplier * Time.deltaTime;
                recoilSpreadContribution = Mathf.Max(0f, recoilSpreadContribution - recovery);
            }
        }

        /// <summary>
        /// 연속 발사 카운터 업데이트
        /// </summary>
        private void UpdateConsecutiveShotCounter()
        {
            if (consecutiveShots > 0 && Time.time - lastShotTime > CONSECUTIVE_SHOT_TIMEOUT)
            {
                consecutiveShots = 0;
            }
        }

        /// <summary>
        /// 현재 확산 계산
        /// </summary>
        private void CalculateCurrentSpread()
        {
            if (currentWeapon == null)
            {
                currentSpread = 0f;
                return;
            }

            // 기본 확산 (힙파이어 ↔ ADS 보간)
            float baseSpread = Mathf.Lerp(
                currentWeapon.hipFireSpread,
                currentWeapon.adsSpread,
                adsProgress
            );

            // 연속 발사 배율 적용
            float spreadMultiplier = currentWeapon.GetSpreadMultiplier(consecutiveShots);

            // 최종 확산 = 기본 확산 * 배율 + 반동 기여분
            currentSpread = (baseSpread * spreadMultiplier) + recoilSpreadContribution;

            OnSpreadChanged?.Invoke(currentSpread);
        }

        /// <summary>
        /// 반동으로 인한 확산 기여도 계산
        /// </summary>
        private float CalculateRecoilSpreadContribution()
        {
            if (currentWeapon == null) return 0f;

            // 반동 값을 확산 증가로 변환 (스케일링)
            float verticalContribution = currentWeapon.verticalRecoil * 0.01f;
            float horizontalContribution = currentWeapon.horizontalRecoil * 0.01f;

            return (verticalContribution + horizontalContribution) * 0.5f;
        }

        /// <summary>
        /// 플레이어 이동 속도 업데이트
        /// </summary>
        private void UpdatePlayerMoveSpeed()
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetWeaponSpeedModifier(GetCurrentMoveModifier());
            }
        }

        /// <summary>
        /// 상태 변경
        /// </summary>
        private void SetState(AimState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnAimStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// 강제 힙파이어 전환 (무기 교체 등)
        /// </summary>
        public void ForceHipFire()
        {
            adsProgress = 0f;
            currentState = AimState.HipFire;
            OnADSProgressChanged?.Invoke(adsProgress);
            OnAimStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// 연속 발사 카운터 리셋
        /// </summary>
        public void ResetConsecutiveShots()
        {
            consecutiveShots = 0;
            recoilSpreadContribution = 0f;
            CalculateCurrentSpread();
        }
    }
}
