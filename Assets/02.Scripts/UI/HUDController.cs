using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Weapon;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// HUD 컨트롤러 - 체력, 탄약, 무기 정보, 크로스헤어 관리
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health UI")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image vignetteOverlay;

        [Header("Health Colors")]
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color injuredColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        [Header("Weapon UI")]
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI reserveAmmoText;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private GameObject reloadingIndicator;

        [Header("Armor UI")]
        [SerializeField] private Image armorIcon;
        [SerializeField] private TextMeshProUGUI armorText;

        [Header("Crosshair")]
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private DynamicCrosshair dynamicCrosshair;

        private void Start()
        {
            SubscribeToEvents();
            InitializeUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            // Health events
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnHealthChanged += UpdateHealthUI;
                PlayerHealth.Instance.OnHealthStateChanged += UpdateHealthState;
            }

            // Weapon events
            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.OnWeaponChanged += UpdateWeaponUI;
                WeaponManager.Instance.OnAmmoChanged += UpdateAmmoUI;
                WeaponManager.Instance.OnReloadStart += ShowReloadingIndicator;
                WeaponManager.Instance.OnReloadComplete += HideReloadingIndicator;
            }

            // Inventory events
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnAmmoChanged += UpdateReserveAmmo;
                PlayerInventory.Instance.OnArmorChanged += UpdateArmorUI;
            }
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnHealthChanged -= UpdateHealthUI;
                PlayerHealth.Instance.OnHealthStateChanged -= UpdateHealthState;
            }

            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.OnWeaponChanged -= UpdateWeaponUI;
                WeaponManager.Instance.OnAmmoChanged -= UpdateAmmoUI;
                WeaponManager.Instance.OnReloadStart -= ShowReloadingIndicator;
                WeaponManager.Instance.OnReloadComplete -= HideReloadingIndicator;
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnAmmoChanged -= UpdateReserveAmmo;
                PlayerInventory.Instance.OnArmorChanged -= UpdateArmorUI;
            }
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // Health
            if (PlayerHealth.Instance != null)
            {
                UpdateHealthUI(PlayerHealth.Instance.CurrentHealth, PlayerHealth.Instance.MaxHealth);
                UpdateHealthState(PlayerHealth.Instance.CurrentState);
            }

            // Weapon
            if (WeaponManager.Instance != null && WeaponManager.Instance.CurrentWeaponData != null)
            {
                UpdateWeaponUI(WeaponManager.Instance.CurrentWeaponData);
            }

            // Armor
            if (PlayerInventory.Instance != null)
            {
                UpdateArmorUI(PlayerInventory.Instance.CurrentArmor);
            }

            // Reloading indicator
            if (reloadingIndicator != null)
            {
                reloadingIndicator.SetActive(false);
            }

            // Vignette
            if (vignetteOverlay != null)
            {
                vignetteOverlay.color = new Color(1, 0, 0, 0);
            }
        }

        /// <summary>
        /// 체력 UI 업데이트
        /// </summary>
        private void UpdateHealthUI(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        /// <summary>
        /// 체력 상태에 따른 UI 업데이트
        /// </summary>
        private void UpdateHealthState(HealthState state)
        {
            Color barColor = normalColor;
            float vignetteAlpha = 0f;

            switch (state)
            {
                case HealthState.Normal:
                    barColor = normalColor;
                    vignetteAlpha = 0f;
                    break;
                case HealthState.Injured:
                    barColor = injuredColor;
                    vignetteAlpha = 0.2f;
                    break;
                case HealthState.Critical:
                    barColor = criticalColor;
                    vignetteAlpha = 0.4f;
                    break;
                case HealthState.Dead:
                    barColor = criticalColor;
                    vignetteAlpha = 0.6f;
                    break;
            }

            if (healthBarFill != null)
            {
                healthBarFill.color = barColor;
            }

            if (vignetteOverlay != null)
            {
                vignetteOverlay.color = new Color(1, 0, 0, vignetteAlpha);
            }
        }

        /// <summary>
        /// 무기 UI 업데이트
        /// </summary>
        private void UpdateWeaponUI(WeaponData weapon)
        {
            if (weapon == null) return;

            if (weaponNameText != null)
            {
                weaponNameText.text = weapon.itemName;
            }

            if (weaponIcon != null && weapon.icon != null)
            {
                weaponIcon.sprite = weapon.icon;
                weaponIcon.gameObject.SetActive(true);
            }
            else if (weaponIcon != null)
            {
                weaponIcon.gameObject.SetActive(false);
            }

            // 예비 탄약 업데이트
            UpdateReserveAmmo(weapon.ammoType, PlayerInventory.Instance?.GetAmmo(weapon.ammoType) ?? 0);
        }

        /// <summary>
        /// 탄약 UI 업데이트
        /// </summary>
        private void UpdateAmmoUI(int current, int magazine)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{current} / {magazine}";

                // 탄약 부족 경고
                if (current <= magazine * 0.2f)
                {
                    ammoText.color = criticalColor;
                }
                else
                {
                    ammoText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 예비 탄약 업데이트
        /// </summary>
        private void UpdateReserveAmmo(AmmoType type, int amount)
        {
            // 현재 무기의 탄약 타입만 표시
            if (WeaponManager.Instance?.CurrentWeaponData?.ammoType != type) return;

            if (reserveAmmoText != null)
            {
                reserveAmmoText.text = amount.ToString();

                // 탄약 부족 경고
                if (amount <= 0)
                {
                    reserveAmmoText.color = criticalColor;
                }
                else
                {
                    reserveAmmoText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 방어구 UI 업데이트
        /// </summary>
        private void UpdateArmorUI(ArmorData armor)
        {
            if (armorText != null)
            {
                if (armor != null)
                {
                    armorText.text = $"방어력: {armor.defense}";
                }
                else
                {
                    armorText.text = "방어력: 0";
                }
            }

            if (armorIcon != null)
            {
                if (armor != null && armor.icon != null)
                {
                    armorIcon.sprite = armor.icon;
                    armorIcon.gameObject.SetActive(true);
                }
                else
                {
                    armorIcon.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 재장전 인디케이터 표시
        /// </summary>
        private void ShowReloadingIndicator()
        {
            if (reloadingIndicator != null)
            {
                reloadingIndicator.SetActive(true);
            }
        }

        /// <summary>
        /// 재장전 인디케이터 숨기기
        /// </summary>
        private void HideReloadingIndicator()
        {
            if (reloadingIndicator != null)
            {
                reloadingIndicator.SetActive(false);
            }
        }
    }
}
