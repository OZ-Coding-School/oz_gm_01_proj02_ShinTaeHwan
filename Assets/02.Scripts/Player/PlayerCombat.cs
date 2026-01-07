using UnityEngine;
using MiniExtractionShooter.Weapon;

namespace MiniExtractionShooter.Player
{
    /// <summary>
    /// 플레이어 전투 시스템 (사격, 재장전)
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        public static PlayerCombat Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private WeaponManager weaponManager;

        [Header("State")]
        [SerializeField] private bool canShoot = true;

        // Events
        public event System.Action OnFireAttempt;
        public event System.Action OnReloadAttempt;

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

        private void Start()
        {
            if (weaponManager == null)
            {
                weaponManager = GetComponent<WeaponManager>();
            }
        }

        private void Update()
        {
            if (!canShoot) return;

            HandleFireInput();
            HandleReloadInput();
            HandleWeaponSwitch();
        }

        private void HandleFireInput()
        {
            // 좌클릭 - 발사
            if (Input.GetMouseButton(0))
            {
                OnFireAttempt?.Invoke();
                weaponManager?.Fire();
            }
        }

        private void HandleReloadInput()
        {
            // R - 재장전
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnReloadAttempt?.Invoke();
                weaponManager?.Reload();
            }
        }

        private void HandleWeaponSwitch()
        {
            // 1, 2 - 무기 교체
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                weaponManager?.SwitchToWeapon(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                weaponManager?.SwitchToWeapon(1);
            }

            // 마우스 휠 - 무기 교체
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                weaponManager?.CycleWeapon(scroll > 0 ? 1 : -1);
            }
        }

        /// <summary>
        /// 사격 가능 여부 설정 (루팅 중 비활성화)
        /// </summary>
        public void SetCanShoot(bool value)
        {
            canShoot = value;
        }

        public bool CanShoot => canShoot;
        public Transform FirePoint => firePoint;
    }
}
