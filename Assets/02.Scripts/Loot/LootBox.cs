using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Data;

namespace MiniExtractionShooter.Loot
{
    /// <summary>
    /// PoolManager로 관리되는 루트 상자
    /// Enemy 사망 시 스폰되어 플레이어가 루팅할 수 있음
    /// </summary>
    public class LootBox : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LootableObject lootableObject;

        [Header("Settings")]
        [SerializeField] private float despawnDelay = 0.5f;

        private bool isInitialized = false;

        private void Awake()
        {
            // LootableObject 참조 확인/추가
            if (lootableObject == null)
            {
                lootableObject = GetComponent<LootableObject>();
            }
            if (lootableObject == null)
            {
                lootableObject = gameObject.AddComponent<LootableObject>();
            }
        }

        /// <summary>
        /// 상자 초기화 (풀에서 꺼낸 후 호출)
        /// </summary>
        public void Initialize(List<LootEntry> drops, Vector3 position)
        {
            // 위치 설정
            transform.position = position;
            transform.rotation = Quaternion.identity;

            // 활성화
            gameObject.SetActive(true);

            // LootableObject 설정
            if (lootableObject != null)
            {
                lootableObject.enabled = true;
                lootableObject.SetLootItems(drops);

                // 루팅 완료 이벤트 구독
                lootableObject.OnLootEmpty -= HandleLootEmpty;
                lootableObject.OnLootEmpty += HandleLootEmpty;
            }

            isInitialized = true;
        }

        /// <summary>
        /// 풀에서 꺼낼 때 호출
        /// </summary>
        public void OnSpawn()
        {
            isInitialized = false;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 풀로 반환될 때 호출
        /// </summary>
        public void OnDespawn()
        {
            // 이벤트 해제
            if (lootableObject != null)
            {
                lootableObject.OnLootEmpty -= HandleLootEmpty;
                lootableObject.enabled = false;
            }

            isInitialized = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 루팅 완료 시 풀로 반환
        /// </summary>
        private void HandleLootEmpty()
        {
            // 이벤트 해제
            if (lootableObject != null)
            {
                lootableObject.OnLootEmpty -= HandleLootEmpty;
            }

            // 약간의 딜레이 후 풀로 반환
            StartCoroutine(ReturnToPoolDelayed());
        }

        private System.Collections.IEnumerator ReturnToPoolDelayed()
        {
            yield return new WaitForSeconds(despawnDelay);

            // PoolManager로 반환
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.ReturnPool(this, false);
            }
            else
            {
                // PoolManager가 없으면 비활성화
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// LootableObject 참조 반환
        /// </summary>
        public LootableObject GetLootableObject()
        {
            return lootableObject;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (isInitialized)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }
        }
#endif
    }
}
