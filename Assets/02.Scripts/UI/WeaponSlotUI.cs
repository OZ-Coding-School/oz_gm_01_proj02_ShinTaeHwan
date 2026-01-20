using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 개별 무기 슬롯 UI
    /// </summary>
    public class WeaponSlotUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TextMeshProUGUI slotNumberText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private GameObject highlight;
        [SerializeField] private Image backgroundImage;

        [Header("Reload Progress")]
        [SerializeField] private Image reloadFillImage; // Fill type image, bottom to top

        [Header("Settings")]
        [SerializeField] private int slotIndex = 0; // 0: 주무기, 1: 보조무기

        [Header("Colors")]
        [SerializeField] private Color normalBackgroundColor = new Color(0.16f, 0.16f, 0.2f, 0.8f);
        [SerializeField] private Color selectedBackgroundColor = new Color(0.3f, 0.3f, 0.4f, 0.9f);
        [SerializeField] private Color reloadFillColor = new Color(0.3f, 0.7f, 1f, 0.5f);

        private WeaponData currentWeapon;
        private bool isSelected = false;

        public int SlotIndex => slotIndex;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            // 슬롯 번호 표시 (1 또는 2)
            if (slotNumberText != null)
            {
                slotNumberText.text = (slotIndex + 1).ToString();
            }

            // 기본 상태
            ClearSlot();
        }

        /// <summary>
        /// 무기 설정
        /// </summary>
        public void SetWeapon(WeaponData weapon)
        {
            currentWeapon = weapon;

            if (weapon != null)
            {
                // 아이콘 표시
                if (weaponIcon != null)
                {
                    if (weapon.icon != null)
                    {
                        weaponIcon.sprite = weapon.icon;
                        weaponIcon.color = Color.white;
                        weaponIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        weaponIcon.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                ClearSlot();
            }
        }

        /// <summary>
        /// 슬롯 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            // 하이라이트 표시
            if (highlight != null)
            {
                highlight.SetActive(selected);
            }

            // 배경색 변경
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;
            }

            // 선택된 슬롯만 탄약 정보 표시
            if (ammoText != null)
            {
                ammoText.gameObject.SetActive(selected && currentWeapon != null);
            }
        }

        /// <summary>
        /// 탄약 정보 업데이트
        /// </summary>
        public void UpdateAmmo(int currentMagazine, int reserveAmmo)
        {
            if (ammoText != null && isSelected)
            {
                ammoText.text = $"{currentMagazine} / {reserveAmmo}";
                ammoText.gameObject.SetActive(true);

                // 탄창 탄약 부족 경고 (탄창 비었거나 예비 탄약 없음)
                if (currentMagazine == 0 || (currentMagazine <= 2 && reserveAmmo == 0))
                {
                    ammoText.color = new Color(1f, 0.4f, 0.4f, 1f); // 빨간색
                }
                else
                {
                    ammoText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 슬롯 비우기
        /// </summary>
        public void ClearSlot()
        {
            currentWeapon = null;

            if (weaponIcon != null)
            {
                weaponIcon.sprite = null;
                weaponIcon.color = Color.clear;
                weaponIcon.gameObject.SetActive(false);
            }

            if (ammoText != null)
            {
                ammoText.gameObject.SetActive(false);
            }

            if (highlight != null)
            {
                highlight.SetActive(false);
            }

            if (reloadFillImage != null)
            {
                reloadFillImage.fillAmount = 0f;
                reloadFillImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 재장전 진행률 업데이트
        /// </summary>
        public void UpdateReloadProgress(float progress)
        {
            if (reloadFillImage == null) return;

            if (progress > 0f)
            {
                reloadFillImage.gameObject.SetActive(true);
                reloadFillImage.fillAmount = progress;
                reloadFillImage.color = reloadFillColor;
            }
            else
            {
                reloadFillImage.fillAmount = 0f;
                reloadFillImage.gameObject.SetActive(false);
            }
        }
    }
}
