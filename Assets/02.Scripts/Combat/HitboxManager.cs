using UnityEngine;
using System.Collections.Generic;

namespace MiniExtractionShooter.Combat
{
    /// <summary>
    /// 히트박스 매니저
    /// 캐릭터의 모든 HitZone을 관리하고 데미지를 전달
    /// </summary>
    public class HitboxManager : MonoBehaviour
    {
        [Header("Hit Zones")]
        [SerializeField] private List<HitZone> hitZones = new List<HitZone>();

        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupHitZones = true;

        // Events
        public event System.Action<float, HitZoneType> OnDamageReceived;

        // 데미지 수신 인터페이스 (PlayerHealth 또는 EnemyHealth)
        private IDamageable damageReceiver;

        private void Awake()
        {
            damageReceiver = GetComponent<IDamageable>();

            if (autoSetupHitZones)
            {
                SetupHitZones();
            }
        }

        /// <summary>
        /// 자식 오브젝트에서 HitZone 자동 찾기
        /// </summary>
        private void SetupHitZones()
        {
            hitZones.Clear();
            HitZone[] zones = GetComponentsInChildren<HitZone>();

            foreach (var zone in zones)
            {
                zone.SetOwner(this);
                hitZones.Add(zone);
            }
        }

        /// <summary>
        /// HitZone 수동 등록
        /// </summary>
        public void RegisterHitZone(HitZone zone)
        {
            if (!hitZones.Contains(zone))
            {
                zone.SetOwner(this);
                hitZones.Add(zone);
            }
        }

        /// <summary>
        /// 데미지 수신 (HitZone에서 호출)
        /// </summary>
        public void ReceiveDamage(float damage, HitZoneType zone)
        {
            OnDamageReceived?.Invoke(damage, zone);

            // IDamageable 인터페이스로 데미지 전달
            if (damageReceiver != null)
            {
                damageReceiver.TakeDamage(damage);
            }
        }

        /// <summary>
        /// 특정 부위의 HitZone 가져오기
        /// </summary>
        public HitZone GetHitZone(HitZoneType zoneType)
        {
            return hitZones.Find(z => z.Zone == zoneType);
        }

        /// <summary>
        /// 모든 HitZone 활성화/비활성화
        /// </summary>
        public void SetHitZonesActive(bool active)
        {
            foreach (var zone in hitZones)
            {
                Collider col = zone.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = active;
                }
            }
        }

        /// <summary>
        /// 히트박스 자동 생성 (캐릭터 크기 기준)
        /// TDD 기준: 캐릭터 높이 1.8m
        /// </summary>
        public void CreateDefaultHitboxes()
        {
            // 기존 HitZone 제거
            foreach (var zone in hitZones)
            {
                if (zone != null)
                {
                    DestroyImmediate(zone.gameObject);
                }
            }
            hitZones.Clear();

            // 머리 (Sphere, 반지름 0.12m)
            CreateHitZone("Head", HitZoneType.Head, new Vector3(0, 1.65f, 0),
                CreateSphereCollider, 0.12f);

            // 몸통 (Capsule, 높이 0.5m, 반지름 0.2m)
            CreateHitZone("Body", HitZoneType.Body, new Vector3(0, 1.15f, 0),
                CreateCapsuleCollider, 0.2f, 0.5f);

            // 왼팔 (Capsule, 높이 0.5m, 반지름 0.08m)
            CreateHitZone("LeftArm", HitZoneType.Arms, new Vector3(-0.3f, 1.15f, 0),
                CreateCapsuleCollider, 0.08f, 0.5f);

            // 오른팔 (Capsule, 높이 0.5m, 반지름 0.08m)
            CreateHitZone("RightArm", HitZoneType.Arms, new Vector3(0.3f, 1.15f, 0),
                CreateCapsuleCollider, 0.08f, 0.5f);

            // 왼다리 (Capsule, 높이 0.7m, 반지름 0.1m)
            CreateHitZone("LeftLeg", HitZoneType.Legs, new Vector3(-0.1f, 0.35f, 0),
                CreateCapsuleCollider, 0.1f, 0.7f);

            // 오른다리 (Capsule, 높이 0.7m, 반지름 0.1m)
            CreateHitZone("RightLeg", HitZoneType.Legs, new Vector3(0.1f, 0.35f, 0),
                CreateCapsuleCollider, 0.1f, 0.7f);
        }

        private void CreateHitZone(string name, HitZoneType zoneType, Vector3 localPosition,
            System.Action<GameObject, float, float> colliderCreator, float radius, float height = 0f)
        {
            GameObject hitZoneObj = new GameObject($"HitZone_{name}");
            hitZoneObj.transform.SetParent(transform);
            hitZoneObj.transform.localPosition = localPosition;
            hitZoneObj.transform.localRotation = Quaternion.identity;
            hitZoneObj.layer = gameObject.layer;

            colliderCreator(hitZoneObj, radius, height);

            HitZone hitZone = hitZoneObj.AddComponent<HitZone>();
            hitZone.SetZone(zoneType);
            hitZone.SetOwner(this);

            hitZones.Add(hitZone);
        }

        private void CreateSphereCollider(GameObject obj, float radius, float height)
        {
            SphereCollider col = obj.AddComponent<SphereCollider>();
            col.radius = radius;
            col.isTrigger = true;
        }

        private void CreateCapsuleCollider(GameObject obj, float radius, float height)
        {
            CapsuleCollider col = obj.AddComponent<CapsuleCollider>();
            col.radius = radius;
            col.height = height;
            col.isTrigger = true;
        }
    }

    /// <summary>
    /// 데미지 수신 인터페이스
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
}
