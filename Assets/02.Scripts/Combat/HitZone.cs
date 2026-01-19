using UnityEngine;

namespace MiniExtractionShooter.Combat
{
    /// <summary>
    /// 피격 부위 컴포넌트
    /// 각 콜라이더에 부착하여 부위별 데미지 처리
    /// </summary>
    public class HitZone : MonoBehaviour
    {
        [Header("Hit Zone Settings")]
        [SerializeField] private HitZoneType zone = HitZoneType.Body;

        [Header("Owner Reference")]
        [SerializeField] private HitboxManager owner;

        public HitZoneType Zone => zone;
        public HitboxManager Owner => owner;

        private void Awake()
        {
            // 부모에서 HitboxManager 찾기
            if (owner == null)
            {
                owner = GetComponentInParent<HitboxManager>();
            }
        }

        /// <summary>
        /// 데미지 받기 (방어력 미적용 상태)
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (owner != null)
            {
                owner.ReceiveDamage(damage, zone);
            }
            else
            {
                Debug.LogWarning($"HitZone '{gameObject.name}' has no owner HitboxManager!");
            }
        }

        /// <summary>
        /// 데미지 받기 (원본 데미지 + 부위 정보)
        /// </summary>
        public void TakeDamageRaw(float baseDamage, float armor)
        {
            float finalDamage = DamageCalculator.CalculateSimpleDamage(baseDamage, zone, armor);
            TakeDamage(finalDamage);
        }

        /// <summary>
        /// Owner 설정
        /// </summary>
        public void SetOwner(HitboxManager newOwner)
        {
            owner = newOwner;
        }

        /// <summary>
        /// Zone 타입 설정
        /// </summary>
        public void SetZone(HitZoneType newZone)
        {
            zone = newZone;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Zone별 색상으로 기즈모 표시
            Color gizmoColor = zone switch
            {
                HitZoneType.Head => Color.red,
                HitZoneType.Body => Color.yellow,
                HitZoneType.Arms => Color.blue,
                HitZoneType.Legs => Color.green,
                _ => Color.white
            };

            gizmoColor.a = 0.3f;
            Gizmos.color = gizmoColor;

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;

                if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
                else if (col is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is CapsuleCollider capsule)
                {
                    // 간단한 캡슐 표현
                    Gizmos.DrawSphere(capsule.center + Vector3.up * (capsule.height * 0.5f - capsule.radius), capsule.radius);
                    Gizmos.DrawSphere(capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius), capsule.radius);
                }
            }
        }
#endif
    }
}
