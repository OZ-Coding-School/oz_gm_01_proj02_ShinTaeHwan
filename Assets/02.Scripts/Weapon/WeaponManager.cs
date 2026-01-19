using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.Weapon
{
    /// <summary>
    /// 무기 관리 시스템 (교체, 슬롯)
    /// TDD 기준: 주무기 + 보조무기 2슬롯
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance { get; private set; }

        [Header("Weapon Slots")]
        [SerializeField] private WeaponData primaryWeapon;      // 슬롯 1
        [SerializeField] private WeaponData secondaryWeapon;    // 슬롯 2

        [Header("Current Weapon")]
        [SerializeField] private int currentSlot = 0;
        [SerializeField] private WeaponBase activeWeapon;

        [Header("Weapon Holder")]
        [SerializeField] private Transform weaponHolder;

        // Events
        public event System.Action<WeaponData> OnWeaponChanged;
        public event System.Action<int, int> OnAmmoChanged;     // current, magazine
        public event System.Action OnFired;
        public event System.Action OnReloadStart;
        public event System.Action OnReloadComplete;

        public WeaponData CurrentWeaponData => currentSlot == 0 ? primaryWeapon : secondaryWeapon;
        public WeaponBase ActiveWeapon => activeWeapon;
        public int CurrentSlot => currentSlot;

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
            // 시작 무기 장착
            if (primaryWeapon != null)
            {
                EquipWeapon(primaryWeapon);
            }
        }

        /// <summary>
        /// 발사
        /// </summary>
        public void Fire()
        {
            if (activeWeapon != null)
            {
                activeWeapon.TryFire();
            }
        }

        /// <summary>
        /// 재장전
        /// </summary>
        public void Reload()
        {
            if (activeWeapon != null)
            {
                activeWeapon.TryReload();
            }
        }

        /// <summary>
        /// 특정 슬롯의 무기로 교체
        /// </summary>
        public void SwitchToWeapon(int slot)
        {
            if (slot < 0 || slot > 1) return;

            WeaponData targetWeapon = slot == 0 ? primaryWeapon : secondaryWeapon;
            if (targetWeapon == null) return;

            currentSlot = slot;
            EquipWeapon(targetWeapon);
        }

        /// <summary>
        /// 무기 순환 교체
        /// </summary>
        public void CycleWeapon(int direction)
        {
            int nextSlot = (currentSlot + direction + 2) % 2;
            SwitchToWeapon(nextSlot);
        }

        /// <summary>
        /// 무기 장착
        /// </summary>
        private void EquipWeapon(WeaponData weaponData)
        {
            // 기존 무기 이벤트 해제
            if (activeWeapon != null)
            {
                activeWeapon.OnFired -= HandleFired;
                activeWeapon.OnAmmoChanged -= HandleAmmoChanged;
                activeWeapon.OnReloadStart -= HandleReloadStart;
                activeWeapon.OnReloadComplete -= HandleReloadComplete;
            }

            // 무기 오브젝트 생성 또는 업데이트
            if (activeWeapon == null)
            {
                GameObject weaponObj = new GameObject("ActiveWeapon");
                weaponObj.transform.SetParent(weaponHolder != null ? weaponHolder : transform);
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = Quaternion.identity;

                activeWeapon = weaponObj.AddComponent<WeaponBase>();
                weaponObj.AddComponent<RecoilSystem>();
            }

            activeWeapon.SetWeaponData(weaponData);

            // 이벤트 연결
            activeWeapon.OnFired += HandleFired;
            activeWeapon.OnAmmoChanged += HandleAmmoChanged;
            activeWeapon.OnReloadStart += HandleReloadStart;
            activeWeapon.OnReloadComplete += HandleReloadComplete;

            OnWeaponChanged?.Invoke(weaponData);
        }

        /// <summary>
        /// 새 무기 획득
        /// </summary>
        public void PickupWeapon(WeaponData newWeapon)
        {
            if (newWeapon == null) return;

            // 빈 슬롯 확인
            if (primaryWeapon == null)
            {
                primaryWeapon = newWeapon;
                SwitchToWeapon(0);
            }
            else if (secondaryWeapon == null)
            {
                secondaryWeapon = newWeapon;
                SwitchToWeapon(1);
            }
            else
            {
                // 현재 무기 교체 (기존 무기 드랍)
                DropCurrentWeapon();

                if (currentSlot == 0)
                {
                    primaryWeapon = newWeapon;
                }
                else
                {
                    secondaryWeapon = newWeapon;
                }

                EquipWeapon(newWeapon);
            }
        }

        /// <summary>
        /// 현재 무기 드랍
        /// </summary>
        public WeaponData DropCurrentWeapon()
        {
            WeaponData droppedWeapon = CurrentWeaponData;

            if (currentSlot == 0)
            {
                primaryWeapon = null;
            }
            else
            {
                secondaryWeapon = null;
            }

            // 드랍 아이템 생성 (선택적)
            // SpawnDroppedWeapon(droppedWeapon);

            return droppedWeapon;
        }

        /// <summary>
        /// 시작 무기 설정
        /// </summary>
        public void SetStartWeapon(WeaponData weapon)
        {
            primaryWeapon = weapon;
            secondaryWeapon = null;
            currentSlot = 0;
            EquipWeapon(weapon);
        }

        /// <summary>
        /// 무기 보유 확인
        /// </summary>
        public bool HasWeapon(WeaponType type)
        {
            return (primaryWeapon != null && primaryWeapon.weaponType == type) ||
                   (secondaryWeapon != null && secondaryWeapon.weaponType == type);
        }

        // Event handlers
        private void HandleFired() => OnFired?.Invoke();
        private void HandleAmmoChanged(int current, int max) => OnAmmoChanged?.Invoke(current, max);
        private void HandleReloadStart() => OnReloadStart?.Invoke();
        private void HandleReloadComplete() => OnReloadComplete?.Invoke();

        #region Save/Load Support

        [Header("Available Weapons (for Save/Load)")]
        [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();

        /// <summary>
        /// 주무기 이름 반환 (저장용)
        /// </summary>
        public string GetPrimaryWeaponName()
        {
            return primaryWeapon?.itemName ?? "";
        }

        /// <summary>
        /// 보조무기 이름 반환 (저장용)
        /// </summary>
        public string GetSecondaryWeaponName()
        {
            return secondaryWeapon?.itemName ?? "";
        }

        /// <summary>
        /// 이름으로 무기 데이터 찾기 (로드용)
        /// </summary>
        public WeaponData FindWeaponByName(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName)) return null;

            foreach (var weapon in availableWeapons)
            {
                if (weapon != null && weapon.itemName == weaponName)
                {
                    return weapon;
                }
            }

            // Resources 폴더에서 찾기 (fallback)
            WeaponData[] allWeapons = Resources.LoadAll<WeaponData>("");
            foreach (var weapon in allWeapons)
            {
                if (weapon.itemName == weaponName)
                {
                    return weapon;
                }
            }

            return null;
        }

        /// <summary>
        /// 주무기 직접 설정 (로드용)
        /// </summary>
        public void SetPrimaryWeapon(WeaponData weapon)
        {
            primaryWeapon = weapon;
        }

        /// <summary>
        /// 보조무기 직접 설정 (로드용)
        /// </summary>
        public void SetSecondaryWeapon(WeaponData weapon)
        {
            secondaryWeapon = weapon;
        }

        #endregion
    }
}
