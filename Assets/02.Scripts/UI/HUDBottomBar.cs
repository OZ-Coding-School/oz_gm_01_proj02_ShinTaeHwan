using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Weapon;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// HUD 하단 바 - HP, 수분, 에너지, 무기 슬롯, 퀵슬롯 표시
    /// </summary>
    public class HUDBottomBar : MonoBehaviour
    {
        public static HUDBottomBar Instance { get; private set; }

        [Header("HP Display")]
        [SerializeField] private Image hpBarFill;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Hydration Display")]
        [SerializeField] private Image hydrationFill;     // 아이콘 위 덮개 (반전 Fill)
        [SerializeField] private Image hydrationIcon;

        [Header("Energy Display")]
        [SerializeField] private Image energyFill;        // 아이콘 위 덮개 (반전 Fill)
        [SerializeField] private Image energyIcon;

        [Header("Weapon Slots")]
        [SerializeField] private WeaponSlotUI weaponSlot1; // 주무기
        [SerializeField] private WeaponSlotUI weaponSlot2; // 보조무기

        [Header("Colors")]
        [SerializeField] private Color normalHydrationColor = new Color(0.24f, 0.63f, 0.86f, 1f);
        [SerializeField] private Color lowHydrationColor = new Color(0.4f, 0.4f, 0.5f, 1f);
        [SerializeField] private Color normalEnergyColor = new Color(1f, 0.78f, 0.2f, 1f);
        [SerializeField] private Color lowEnergyColor = new Color(0.4f, 0.4f, 0.5f, 1f);

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
            SubscribeToEvents();
            InitializeUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            // HP 이벤트
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnHealthChanged += UpdateHPDisplay;
            }

            // 수분/에너지 이벤트
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnHydrationChanged += UpdateHydrationDisplay;
                PlayerStats.Instance.OnEnergyChanged += UpdateEnergyDisplay;
            }

            // 무기 이벤트
            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.OnWeaponChanged += OnWeaponChanged;
                WeaponManager.Instance.OnAmmoChanged += OnAmmoChanged;
                WeaponManager.Instance.OnReloadProgress += OnReloadProgress;
            }

            // 인벤토리 탄약 변경 이벤트 (루팅 시 업데이트)
            if (Player.PlayerInventory.Instance != null)
            {
                Player.PlayerInventory.Instance.OnAmmoChanged += OnInventoryAmmoChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnHealthChanged -= UpdateHPDisplay;
            }

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnHydrationChanged -= UpdateHydrationDisplay;
                PlayerStats.Instance.OnEnergyChanged -= UpdateEnergyDisplay;
            }

            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.OnWeaponChanged -= OnWeaponChanged;
                WeaponManager.Instance.OnAmmoChanged -= OnAmmoChanged;
                WeaponManager.Instance.OnReloadProgress -= OnReloadProgress;
            }

            if (Player.PlayerInventory.Instance != null)
            {
                Player.PlayerInventory.Instance.OnAmmoChanged -= OnInventoryAmmoChanged;
            }
        }

        private void InitializeUI()
        {
            // HP 초기화
            if (PlayerHealth.Instance != null)
            {
                UpdateHPDisplay(PlayerHealth.Instance.CurrentHealth, PlayerHealth.Instance.MaxHealth);
            }

            // 수분/에너지 초기화
            if (PlayerStats.Instance != null)
            {
                UpdateHydrationDisplay(PlayerStats.Instance.CurrentHydration, PlayerStats.Instance.MaxHydration);
                UpdateEnergyDisplay(PlayerStats.Instance.CurrentEnergy, PlayerStats.Instance.MaxEnergy);
            }

            // 무기 슬롯 초기화
            UpdateWeaponSlots();
        }

        #region HP Display

        private void UpdateHPDisplay(float current, float max)
        {
            if (hpBarFill != null)
            {
                hpBarFill.fillAmount = current / max;
            }

            if (hpText != null)
            {
                hpText.text = $"{current:F1} / {max:F0}";
            }
        }

        #endregion

        #region Hydration/Energy Display

        private void UpdateHydrationDisplay(float current, float max)
        {
            float percentage = current / max;

            // 덮개 방식: 가득 찼을 때 Fill = 0, 비었을 때 Fill = 1
            if (hydrationFill != null)
            {
                hydrationFill.fillAmount = 1f - percentage;
            }

            // 아이콘 색상 변경 (낮을 때 회색)
            if (hydrationIcon != null)
            {
                hydrationIcon.color = percentage > 0.2f ? normalHydrationColor : lowHydrationColor;
            }
        }

        private void UpdateEnergyDisplay(float current, float max)
        {
            float percentage = current / max;

            // 덮개 방식: 가득 찼을 때 Fill = 0, 비었을 때 Fill = 1
            if (energyFill != null)
            {
                energyFill.fillAmount = 1f - percentage;
            }

            // 아이콘 색상 변경 (낮을 때 회색)
            if (energyIcon != null)
            {
                energyIcon.color = percentage > 0.2f ? normalEnergyColor : lowEnergyColor;
            }
        }

        #endregion

        #region Weapon Slots

        private void OnWeaponChanged(Data.WeaponData weapon)
        {
            UpdateWeaponSlots();
        }

        private void OnAmmoChanged(int current, int magazine)
        {
            // 현재 선택된 무기 슬롯의 탄약 정보 업데이트
            int currentSlot = WeaponManager.Instance?.CurrentSlot ?? 0;
            
            if (currentSlot == 0 && weaponSlot1 != null)
            {
                weaponSlot1.UpdateAmmo(current, magazine);
            }
            else if (currentSlot == 1 && weaponSlot2 != null)
            {
                weaponSlot2.UpdateAmmo(current, magazine);
            }
        }

        /// <summary>
        /// 인벤토리 탄약 변경 시 호출 (루팅으로 탄약 획득 등)
        /// </summary>
        private void OnInventoryAmmoChanged(Data.AmmoType ammoType, int newAmount)
        {
            // 현재 무기의 탄약 타입인 경우에만 업데이트
            var currentWeapon = WeaponManager.Instance?.CurrentWeaponData;
            if (currentWeapon != null && currentWeapon.ammoType == ammoType)
            {
                UpdateWeaponSlots();
            }
        }

        /// <summary>
        /// 재장전 진행률 업데이트
        /// </summary>
        private void OnReloadProgress(float progress)
        {
            int currentSlot = WeaponManager.Instance?.CurrentSlot ?? 0;
            
            if (currentSlot == 0 && weaponSlot1 != null)
            {
                weaponSlot1.UpdateReloadProgress(progress);
            }
            else if (currentSlot == 1 && weaponSlot2 != null)
            {
                weaponSlot2.UpdateReloadProgress(progress);
            }
        }

        private void UpdateWeaponSlots()
        {
            if (WeaponManager.Instance == null) return;

            int currentSlot = WeaponManager.Instance.CurrentSlot;

            // 주무기 슬롯
            if (weaponSlot1 != null)
            {
                Data.WeaponData primary = WeaponManager.Instance.GetPrimaryWeapon();
                
                weaponSlot1.SetWeapon(primary);
                weaponSlot1.SetSelected(currentSlot == 0);

                // 선택된 슬롯만 탄약 정보 표시
                if (currentSlot == 0 && WeaponManager.Instance.ActiveWeapon != null && primary != null)
                {
                    int reserveAmmo = Player.PlayerInventory.Instance?.GetAmmo(primary.ammoType) ?? 0;
                    weaponSlot1.UpdateAmmo(
                        WeaponManager.Instance.ActiveWeapon.CurrentAmmo,
                        reserveAmmo
                    );
                }
            }

            // 보조무기 슬롯
            if (weaponSlot2 != null)
            {
                Data.WeaponData secondary = WeaponManager.Instance.GetSecondaryWeapon();
                
                weaponSlot2.SetWeapon(secondary);
                weaponSlot2.SetSelected(currentSlot == 1);

                // 선택된 슬롯만 탄약 정보 표시
                if (currentSlot == 1 && WeaponManager.Instance.ActiveWeapon != null && secondary != null)
                {
                    int reserveAmmo = Player.PlayerInventory.Instance?.GetAmmo(secondary.ammoType) ?? 0;
                    weaponSlot2.UpdateAmmo(
                        WeaponManager.Instance.ActiveWeapon.CurrentAmmo,
                        reserveAmmo
                    );
                }
            }
        }

        #endregion
    }
}
