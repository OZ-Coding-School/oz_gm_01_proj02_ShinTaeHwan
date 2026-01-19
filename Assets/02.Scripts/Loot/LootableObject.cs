using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Weapon;
using MiniExtractionShooter.UI.Inventory;

namespace MiniExtractionShooter.Loot
{
    /// <summary>
    /// 루팅 가능 오브젝트 (적 시체, 상자)
    /// TDD 기반 순차 공개 시스템
    /// </summary>
    public class LootableObject : MonoBehaviour
    {
        [Header("Loot Settings")]
        [SerializeField] private LootTableData lootTable;
        [SerializeField] private float revealInterval = 1f;

        [Header("State")]
        [SerializeField] private List<LootItem> lootItems = new List<LootItem>();
        [SerializeField] private bool isLooting = false;
        [SerializeField] private int revealedCount = 0;

        [Header("Interaction")]
        [SerializeField] private bool isEmpty = false;

        // Events
        public event System.Action OnLootingStarted;
        public event System.Action OnLootingStopped;
        public event System.Action<int, LootItem> OnItemRevealed;
        public event System.Action<LootItem> OnItemTaken;
        public event System.Action OnLootEmpty;

        public bool IsLooting => isLooting;
        public bool IsEmpty => isEmpty || lootItems.Count == 0;
        public int RevealedCount => revealedCount;
        public List<LootItem> Items => lootItems;

        private Coroutine revealCoroutine;

        private void Start()
        {
            // LootTable이 있으면 자동 생성
            if (lootTable != null && lootItems.Count == 0)
            {
                GenerateLootFromTable();
            }
        }

        /// <summary>
        /// LootTable에서 아이템 생성
        /// </summary>
        private void GenerateLootFromTable()
        {
            if (lootTable == null) return;

            List<LootEntry> entries = lootTable.GenerateLoot();
            lootItems.Clear();

            foreach (var entry in entries)
            {
                lootItems.Add(LootItem.FromLootEntry(entry));
            }

            revealInterval = lootTable.revealInterval;
        }

        /// <summary>
        /// LootEntry 리스트로 아이템 설정 (EnemyDropSystem용)
        /// </summary>
        public void SetLootItems(List<LootEntry> entries)
        {
            lootItems.Clear();
            foreach (var entry in entries)
            {
                lootItems.Add(LootItem.FromLootEntry(entry));
            }
            isEmpty = lootItems.Count == 0;
        }

        /// <summary>
        /// 아이템 직접 추가
        /// </summary>
        public void AddLootItem(LootEntry entry)
        {
            lootItems.Add(LootItem.FromLootEntry(entry));
            isEmpty = false;
        }

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

            // 플레이어 행동 제한
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetCanMove(false);
            }
            if (PlayerCombat.Instance != null)
            {
                PlayerCombat.Instance.SetCanShoot(false);
            }

            // UI 열기
            if (InventoryUI.Instance != null)
            {
                Debug.Log($"[LootableObject] Opening InventoryUI for {gameObject.name}");
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

            // 플레이어 행동 복구
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetCanMove(true);
            }
            if (PlayerCombat.Instance != null)
            {
                PlayerCombat.Instance.SetCanShoot(true);
            }

            // UI 닫기
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.Close();
            }

            OnLootingStopped?.Invoke();
        }

        /// <summary>
        /// 아이템 순차 공개 코루틴
        /// </summary>
        private IEnumerator RevealItemsSequentially()
        {
            for (int i = 0; i < lootItems.Count; i++)
            {
                yield return new WaitForSeconds(revealInterval);

                if (!isLooting) yield break;

                lootItems[i].isRevealed = true;
                revealedCount++;

                OnItemRevealed?.Invoke(i, lootItems[i]);

                // UI 업데이트
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.RevealItem(i, lootItems[i]);
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
            }

            return true;
        }

        /// <summary>
        /// 공개된 모든 아이템 획득
        /// </summary>
        public void TakeAll()
        {
            List<LootItem> itemsToTake = new List<LootItem>();

            // 공개된 아이템만 수집
            for (int i = 0; i < revealedCount && i < lootItems.Count; i++)
            {
                itemsToTake.Add(lootItems[i]);
            }

            // 인벤토리에 추가
            foreach (var item in itemsToTake)
            {
                AddItemToInventory(item);
                OnItemTaken?.Invoke(item);
            }

            // 공개된 아이템 제거
            lootItems.RemoveRange(0, Mathf.Min(revealedCount, lootItems.Count));
            revealedCount = 0;

            // 모든 아이템 획득 시
            if (lootItems.Count == 0)
            {
                isEmpty = true;
                StopLooting();
                OnLootEmpty?.Invoke();
            }
            else
            {
                // 아직 남은 아이템이 있으면 UI 업데이트
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.RefreshUI();
                }
            }
        }

        /// <summary>
        /// 인벤토리에 아이템 추가 - 통합 ItemData 방식
        /// </summary>
        private void AddItemToInventory(LootItem item)
        {
            if (item.itemData == null)
            {
                Debug.LogWarning($"[LootableObject] Cannot add item with null ItemData");
                return;
            }

            // 통합 방식: 모든 아이템을 동일하게 처리
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(item.itemData, item.amount);
                Debug.Log($"[LootableObject] Added {item.amount}x {item.ItemName} to inventory");
            }
        }
    }
}
