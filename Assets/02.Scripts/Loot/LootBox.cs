using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.UI.Inventory;

namespace MiniExtractionShooter.Loot
{
    /// <summary>
    /// 루트 상자 컴포넌트 - 아이템 관리 + 순차 공개 + 풀링 통합
    /// Dynamic: 적 사망 시 PoolManager로 생성
    /// Static: 맵에 미리 배치, Start()에서 아이템 결정
    /// </summary>
    public class LootBox : MonoBehaviour
    {
        public enum SpawnMode
        {
            Dynamic,    // 적 사망 시 동적 생성 (PoolManager 사용)
            Static      // 맵에 미리 배치 (Start에서 LootTable로 아이템 생성)
        }

        [Header("Spawn Mode")]
        [SerializeField] private SpawnMode spawnMode = SpawnMode.Dynamic;

        [Header("Static Mode - Loot Table")]
        [Tooltip("Static 모드에서 사용할 LootTable (확률 기반 아이템 생성)")]
        [SerializeField] private LootTableData lootTable;

        [Header("Loot Settings")]
        [SerializeField] private float revealInterval = 1f;

        [Header("Settings")]
        [SerializeField] private float despawnDelay = 0.5f;

        // Runtime state
        private List<LootItem> lootItems = new List<LootItem>();
        private bool isLooting = false;
        private int revealedCount = 0;
        private bool isEmpty = false;
        private bool isInitialized = false;
        private Coroutine revealCoroutine;

        // Events
        public event System.Action OnLootingStarted;
        public event System.Action OnLootingStopped;
        public event System.Action<int, LootItem> OnItemRevealed;
        public event System.Action<LootItem> OnItemTaken;
        public event System.Action OnLootEmpty;

        // Properties
        public SpawnMode Mode => spawnMode;
        public bool IsInitialized => isInitialized;
        public bool IsLooting => isLooting;
        public bool IsEmpty => isEmpty || lootItems.Count == 0;
        public int RevealedCount => revealedCount;
        public List<LootItem> Items => lootItems;

        private void Start()
        {
            // Static 모드: 게임 시작 시 LootTable에서 아이템 생성
            if (spawnMode == SpawnMode.Static && lootTable != null)
            {
                InitializeFromLootTable();
            }
        }

        private void OnDisable()
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }
        }

        #region Static Mode

        /// <summary>
        /// Static 모드: LootTable에서 확률 기반으로 아이템 생성
        /// </summary>
        private void InitializeFromLootTable()
        {
            if (lootTable == null)
            {
                Debug.LogWarning($"[LootBox] {gameObject.name}: Static 모드지만 LootTable이 없습니다.");
                return;
            }

            List<LootEntry> generatedLoot = lootTable.GenerateLoot();
            SetLootItems(generatedLoot);
            revealInterval = lootTable.revealInterval;
            isInitialized = true;
        }

        /// <summary>
        /// 런타임에 LootTable 변경 (Static 모드용)
        /// </summary>
        public void SetLootTable(LootTableData newLootTable)
        {
            lootTable = newLootTable;
        }

        /// <summary>
        /// 강제로 아이템 재생성 (Static 모드용)
        /// </summary>
        public void RegenerateLoot()
        {
            if (spawnMode == SpawnMode.Static && lootTable != null)
            {
                InitializeFromLootTable();
            }
        }

        #endregion

        #region Dynamic Mode

        /// <summary>
        /// Dynamic 모드: 상자 초기화 (풀에서 꺼낸 후 호출)
        /// </summary>
        public void Initialize(List<LootEntry> drops, Vector3 position)
        {
            spawnMode = SpawnMode.Dynamic;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(true);

            SetLootItems(drops);
            isInitialized = true;
        }

        /// <summary>
        /// 풀에서 꺼낼 때 호출 (Dynamic 모드)
        /// </summary>
        public void OnSpawn()
        {
            isInitialized = false;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 풀로 반환될 때 호출 (Dynamic 모드)
        /// </summary>
        public void OnDespawn()
        {
            // 이벤트 전체 해제 (풀링 시 구독 누출 방지)
            OnLootingStarted = null;
            OnLootingStopped = null;
            OnItemRevealed = null;
            OnItemTaken = null;
            OnLootEmpty = null;

            isInitialized = false;
            isLooting = false;
            revealedCount = 0;
            isEmpty = false;
            lootItems.Clear();

            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }

            gameObject.SetActive(false);
        }

        #endregion

        #region Loot Items

        /// <summary>
        /// LootEntry 리스트로 아이템 설정
        /// </summary>
        public void SetLootItems(List<LootEntry> entries)
        {
            lootItems.Clear();
            foreach (var entry in entries)
            {
                lootItems.Add(LootItem.FromLootEntry(entry));
            }
            isEmpty = lootItems.Count == 0;
            revealedCount = 0;
        }

        #endregion

        #region Looting

        /// <summary>
        /// 루팅 시작
        /// </summary>
        public void StartLooting()
        {
            if (isLooting || IsEmpty) return;

            isLooting = true;
            revealedCount = 0;

            // 모든 아이템 숨김 처리
            foreach (var item in lootItems)
            {
                item.isRevealed = false;
            }

            // UI 열기 (UIStateManager가 플레이어 컨트롤 처리)
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.OpenLoot(this);
            }

            // 아이템 순차 공개 시작
            revealCoroutine = StartCoroutine(RevealItemsSequentially());

            OnLootingStarted?.Invoke();
        }

        /// <summary>
        /// 루팅 중지
        /// </summary>
        public void StopLooting()
        {
            if (!isLooting) return;

            isLooting = false;

            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }

            // UI 닫기 (UIStateManager가 플레이어 컨트롤 처리)
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.Close();
            }

            OnLootingStopped?.Invoke();
        }

        /// <summary>
        /// 강제 루팅 중지 (UI에서 호출 - InventoryUI.Close() 호출 없이 상태만 정리)
        /// </summary>
        public void ForceStopLooting()
        {
            if (!isLooting) return;

            isLooting = false;

            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }

            OnLootingStopped?.Invoke();
        }

        /// <summary>
        /// 아이템 순차 공개 코루틴
        /// </summary>
        private IEnumerator RevealItemsSequentially()
        {
            while (revealedCount < lootItems.Count)
            {
                yield return new WaitForSeconds(revealInterval);

                if (!isLooting) yield break;

                if (revealedCount >= lootItems.Count) yield break;

                int currentIndex = revealedCount;
                LootItem itemToReveal = lootItems[currentIndex];

                itemToReveal.isRevealed = true;
                revealedCount++;

                OnItemRevealed?.Invoke(currentIndex, itemToReveal);

                // UI 업데이트
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.RevealItem(currentIndex, itemToReveal);
                }
            }
        }

        /// <summary>
        /// 특정 아이템 획득
        /// </summary>
        public bool TakeItem(int index)
        {
            if (index < 0 || index >= lootItems.Count) return false;
            if (index >= revealedCount) return false; // 아직 공개되지 않은 아이템

            LootItem item = lootItems[index];

            // 인벤토리에 추가
            AddItemToInventory(item);

            OnItemTaken?.Invoke(item);

            // 리스트에서 제거
            lootItems.RemoveAt(index);
            revealedCount--;

            // UI 업데이트
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.RefreshUI();
            }

            // 모든 아이템 획득 시
            if (lootItems.Count == 0)
            {
                isEmpty = true;
                StopLooting();
                OnLootEmpty?.Invoke();
                HandleLootEmpty();
            }

            return true;
        }

        /// <summary>
        /// 인벤토리에 아이템 추가
        /// </summary>
        private void AddItemToInventory(LootItem item)
        {
            if (item.itemData == null)
            {
                Debug.LogWarning($"[LootBox] Cannot add item with null ItemData");
                return;
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(item.itemData, item.amount);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 루팅 완료 시 처리
        /// </summary>
        private void HandleLootEmpty()
        {
            if (spawnMode == SpawnMode.Dynamic)
            {
                StartCoroutine(ReturnToPoolDelayed());
            }
            else
            {
                StartCoroutine(DeactivateDelayed());
            }
        }

        private IEnumerator ReturnToPoolDelayed()
        {
            yield return new WaitForSeconds(despawnDelay);

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.ReturnPool(this, false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator DeactivateDelayed()
        {
            yield return new WaitForSeconds(despawnDelay);
            gameObject.SetActive(false);
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (spawnMode == SpawnMode.Static)
            {
                Gizmos.color = isInitialized ? Color.green : Color.cyan;
            }
            else
            {
                Gizmos.color = isInitialized ? Color.yellow : Color.gray;
            }
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

            if (spawnMode == SpawnMode.Static)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawIcon(transform.position + Vector3.up * 0.5f, "d_Package Manager@2x", true);
            }
        }

        private void OnValidate()
        {
            if (spawnMode == SpawnMode.Static && lootTable == null)
            {
                Debug.LogWarning($"[LootBox] {gameObject.name}: Static 모드에서는 LootTable이 필요합니다.");
            }
        }
#endif
    }
}
