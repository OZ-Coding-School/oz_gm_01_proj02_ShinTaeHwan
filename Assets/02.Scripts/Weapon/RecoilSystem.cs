using UnityEngine;

namespace MiniExtractionShooter.Weapon
{
    /// <summary>
    /// 반동 시스템
    /// TDD 기반 반동 적용 및 회복
    /// </summary>
    public class RecoilSystem : MonoBehaviour
    {
        [Header("Current Recoil State")]
        [SerializeField] private float currentVerticalRecoil = 0f;
        [SerializeField] private float currentHorizontalRecoil = 0f;

        [Header("Settings")]
        [SerializeField] private float recoilRecoverySpeed = 10f;
        [SerializeField] private float maxRecoil = 15f;

        [Header("Camera/Aim Reference")]
        [SerializeField] private Transform aimTransform;

        // 초기 회전값 저장
        private Quaternion initialRotation;
        private bool hasInitialRotation = false;

        public float CurrentVerticalRecoil => currentVerticalRecoil;
        public float CurrentHorizontalRecoil => currentHorizontalRecoil;

        private void Start()
        {
            if (aimTransform == null)
            {
                aimTransform = Camera.main?.transform;
            }

            if (aimTransform != null)
            {
                initialRotation = aimTransform.localRotation;
                hasInitialRotation = true;
            }
        }

        private void Update()
        {
            RecoverRecoil();
            ApplyRecoilToAim();
        }

        /// <summary>
        /// 반동 적용
        /// </summary>
        public void ApplyRecoil(float vertical, float horizontal)
        {
            // 수직 반동 누적
            currentVerticalRecoil += vertical;
            currentVerticalRecoil = Mathf.Clamp(currentVerticalRecoil, 0f, maxRecoil);

            // 수평 반동 (랜덤)
            currentHorizontalRecoil += Random.Range(-horizontal, horizontal);
            currentHorizontalRecoil = Mathf.Clamp(currentHorizontalRecoil, -maxRecoil, maxRecoil);
        }

        /// <summary>
        /// 반동 회복 (시간에 따라)
        /// </summary>
        private void RecoverRecoil()
        {
            if (currentVerticalRecoil > 0)
            {
                currentVerticalRecoil -= recoilRecoverySpeed * Time.deltaTime;
                currentVerticalRecoil = Mathf.Max(0f, currentVerticalRecoil);
            }

            if (currentHorizontalRecoil != 0)
            {
                float horizontalRecovery = recoilRecoverySpeed * Time.deltaTime;
                if (Mathf.Abs(currentHorizontalRecoil) < horizontalRecovery)
                {
                    currentHorizontalRecoil = 0f;
                }
                else
                {
                    currentHorizontalRecoil -= Mathf.Sign(currentHorizontalRecoil) * horizontalRecovery;
                }
            }
        }

        /// <summary>
        /// 반동을 조준점/카메라에 적용
        /// </summary>
        private void ApplyRecoilToAim()
        {
            if (aimTransform == null || !hasInitialRotation) return;

            // Top-Down 게임에서는 반동이 조준 정확도에만 영향
            // 카메라 회전 대신 발사 방향 편차로 처리
        }

        /// <summary>
        /// 반동을 발사 방향에 적용
        /// </summary>
        public Vector3 ApplyRecoilToDirection(Vector3 originalDirection)
        {
            if (currentVerticalRecoil == 0 && currentHorizontalRecoil == 0)
            {
                return originalDirection;
            }

            // 반동에 의한 방향 편차 계산
            Quaternion verticalRotation = Quaternion.AngleAxis(-currentVerticalRecoil * 0.1f, Vector3.right);
            Quaternion horizontalRotation = Quaternion.AngleAxis(currentHorizontalRecoil * 0.1f, Vector3.up);

            Vector3 recoilDirection = horizontalRotation * verticalRotation * originalDirection;
            return recoilDirection.normalized;
        }

        /// <summary>
        /// 반동 회복 속도 설정
        /// </summary>
        public void SetRecoilRecoverySpeed(float speed)
        {
            recoilRecoverySpeed = speed;
        }

        /// <summary>
        /// 반동 초기화
        /// </summary>
        public void ResetRecoil()
        {
            currentVerticalRecoil = 0f;
            currentHorizontalRecoil = 0f;
        }

        /// <summary>
        /// 조준 Transform 설정
        /// </summary>
        public void SetAimTransform(Transform aim)
        {
            aimTransform = aim;
            if (aimTransform != null)
            {
                initialRotation = aimTransform.localRotation;
                hasInitialRotation = true;
            }
        }

        /// <summary>
        /// 반동이 확산에 기여하는 정도 반환 (0-1)
        /// </summary>
        public float GetSpreadContribution()
        {
            float totalRecoil = currentVerticalRecoil + Mathf.Abs(currentHorizontalRecoil);
            return Mathf.Clamp01(totalRecoil / (maxRecoil * 2f));
        }

        /// <summary>
        /// 현재 총 반동량 반환 (수직 + 수평)
        /// </summary>
        public float GetTotalRecoil()
        {
            return currentVerticalRecoil + Mathf.Abs(currentHorizontalRecoil);
        }
    }
}
