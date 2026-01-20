using UnityEngine;
using System.Collections.Generic;

namespace MiniExtractionShooter.Data
{
    /// <summary>
    /// 게임 내 모든 무기 데이터를 관리하는 데이터베이스
    /// ScriptableObject로 에디터에서 무기 목록 관리
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Data/Weapon Database")]
    public class WeaponDatabase : ScriptableObject
    {
        private static WeaponDatabase _instance;
        public static WeaponDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<WeaponDatabase>("WeaponDatabase");
                    if (_instance == null)
                    {
                        Debug.LogError("[WeaponDatabase] WeaponDatabase not found in Resources folder!");
                    }
                }
                return _instance;
            }
        }

        [Header("All Weapons")]
        [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>();

        public IReadOnlyList<WeaponData> AllWeapons => allWeapons;

        /// <summary>
        /// 이름으로 무기 찾기
        /// </summary>
        public WeaponData FindByName(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName)) return null;

            foreach (var weapon in allWeapons)
            {
                if (weapon != null && weapon.itemName == weaponName)
                {
                    return weapon;
                }
            }

            Debug.LogWarning($"[WeaponDatabase] Weapon not found: {weaponName}");
            return null;
        }

        /// <summary>
        /// 무기 타입으로 무기 목록 찾기
        /// </summary>
        public List<WeaponData> FindByType(WeaponType type)
        {
            List<WeaponData> result = new List<WeaponData>();

            foreach (var weapon in allWeapons)
            {
                if (weapon != null && weapon.weaponType == type)
                {
                    result.Add(weapon);
                }
            }

            return result;
        }

        /// <summary>
        /// 무기가 데이터베이스에 있는지 확인
        /// </summary>
        public bool Contains(WeaponData weapon)
        {
            return weapon != null && allWeapons.Contains(weapon);
        }

        /// <summary>
        /// 무기 개수
        /// </summary>
        public int Count => allWeapons.Count;
    }
}
