using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Managers;

namespace MiniExtractionShooter.Loot
{
    /// <summary>
    /// 플레이어의 루팅 인터랙션 처리
    /// F키 루팅, ESC 취소
    /// </summary>
    public class LootInteraction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float lootRange = 4f; // 감지 범위 (동그라미 표시)
        [SerializeField] private float interactionRange = 1.5f; // 상호작용 가능 범위 (F키 표시)
        [SerializeField] private LayerMask lootableLayerMask = -1;

        [Header("State")]
        [SerializeField] private LootBox nearestLootable;
        [SerializeField] private LootBox currentLootTarget;

        [Header("UI")]
        [SerializeField] private string interactionHintText = "[F] 루팅";

        // Events
        public event System.Action<LootBox> OnLootableFound;
        public event System.Action OnLootableLost;

        public LootBox NearestLootable => nearestLootable;
        public bool IsLooting => currentLootTarget != null && currentLootTarget.IsLooting;
        public float InteractionRange => interactionRange;

        private void Start()
        {
            // 닫기 콜백은 필요 없음 - InventoryUI.Close()가 처리
        }

        private void Update()
        {
            // 루팅 중이 아닐 때만 주변 탐색
            if (!IsLooting)
            {
                FindNearestLootable();
            }

            HandleInput();
        }

        /// <summary>
        /// 가장 가까운 루팅 대상 찾기
        /// </summary>
        private void FindNearestLootable()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, lootRange, lootableLayerMask);

            LootBox previousNearest = nearestLootable;
            nearestLootable = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                LootBox lootable = col.GetComponent<LootBox>();
                if (lootable == null)
                {
                    lootable = col.GetComponentInParent<LootBox>();
                }

                if (lootable != null && !lootable.IsEmpty && lootable.enabled)
                {
                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestLootable = lootable;
                    }
                }
            }

            // 이벤트 발생
            if (nearestLootable != previousNearest)
            {
                if (nearestLootable != null)
                {
                    OnLootableFound?.Invoke(nearestLootable);
                    ShowInteractionHint();
                }
                else
                {
                    OnLootableLost?.Invoke();
                    HideInteractionHint();
                }
            }
        }

        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            // F키 - 루팅 시작/아이템 선택
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (!IsLooting && nearestLootable != null)
                {
                    // 상호작용 범위 내에 있는지 확인
                    float dist = Vector3.Distance(transform.position, nearestLootable.transform.position);
                    if (dist <= interactionRange)
                    {
                        StartLooting(nearestLootable);
                    }
                }
            }
        }

        /// <summary>
        /// 루팅 시작
        /// </summary>
        public void StartLooting(LootBox target)
        {
            if (target == null || target.IsEmpty) return;

            currentLootTarget = target;
            target.StartLooting(); // LootBox -> InventoryUI.OpenLoot() -> UIStateManager

            // 루팅 종료 시 이벤트 구독
            target.OnLootingStopped += HandleLootingStopped;
            target.OnLootEmpty += HandleLootEmpty;

            target.OnLootEmpty += HandleLootEmpty;

            Managers.SoundManager.Instance?.PlaySFX("ItemPickup", transform.position);

            HideInteractionHint();
        }


        /// <summary>
        /// 루팅 종료 핸들러
        /// </summary>
        private void HandleLootingStopped()
        {
            if (currentLootTarget != null)
            {
                currentLootTarget.OnLootingStopped -= HandleLootingStopped;
                currentLootTarget.OnLootEmpty -= HandleLootEmpty;
                currentLootTarget = null;
            }
        }

        /// <summary>
        /// 루팅 대상 비움 핸들러
        /// </summary>
        private void HandleLootEmpty()
        {
            HandleLootingStopped();
        }

        /// <summary>
        /// 인터랙션 힌트 표시
        /// </summary>
        private void ShowInteractionHint()
        {
            // InteractionIndicatorUI에서 처리
        }

        /// <summary>
        /// 인터랙션 힌트 숨기기
        /// </summary>
        private void HideInteractionHint()
        {
            // InteractionIndicatorUI에서 처리
        }

        /// <summary>
        /// 루팅 범위 설정
        /// </summary>
        public void SetLootRange(float range)
        {
            lootRange = range;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 감지 범위 표시 (노란색)
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, lootRange);

            // 상호작용 범위 표시 (초록색)
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            // 가장 가까운 루팅 대상 하이라이트
            if (nearestLootable != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, nearestLootable.transform.position);
            }
        }
#endif
    }
}
