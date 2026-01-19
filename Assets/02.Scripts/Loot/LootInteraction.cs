using UnityEngine;
using System.Collections.Generic;
using MiniExtractionShooter.Managers;

namespace MiniExtractionShooter.Loot
{
    /// <summary>
    /// 플레이어의 루팅 인터랙션 처리
    /// TDD 기준: F키 루팅, Space 모두 획득, ESC 취소
    /// </summary>
    public class LootInteraction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float lootRange = 2f;
        [SerializeField] private LayerMask lootableLayerMask = -1;

        [Header("State")]
        [SerializeField] private LootableObject nearestLootable;
        [SerializeField] private LootableObject currentLootTarget;

        [Header("UI")]
        [SerializeField] private string interactionHintText = "[F] 루팅";

        // Events
        public event System.Action<LootableObject> OnLootableFound;
        public event System.Action OnLootableLost;

        public LootableObject NearestLootable => nearestLootable;
        public bool IsLooting => currentLootTarget != null && currentLootTarget.IsLooting;

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

            LootableObject previousNearest = nearestLootable;
            nearestLootable = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                LootableObject lootable = col.GetComponent<LootableObject>();
                if (lootable == null)
                {
                    lootable = col.GetComponentInParent<LootableObject>();
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
                    StartLooting(nearestLootable);
                }
            }

            // ESC - 루팅 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsLooting)
                {
                    StopLooting();
                }
            }

            // Space - 모두 획득
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (IsLooting && currentLootTarget != null)
                {
                    currentLootTarget.TakeAll();
                }
            }

            // 숫자키 - 개별 아이템 획득
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    if (IsLooting && currentLootTarget != null)
                    {
                        currentLootTarget.TakeItem(i);
                    }
                }
            }
        }

        /// <summary>
        /// 루팅 시작
        /// </summary>
        public void StartLooting(LootableObject target)
        {
            if (target == null || target.IsEmpty) return;

            currentLootTarget = target;
            target.StartLooting();

            // 루팅 종료 시 이벤트 구독
            target.OnLootingStopped += HandleLootingStopped;
            target.OnLootEmpty += HandleLootEmpty;

            HideInteractionHint();
        }

        /// <summary>
        /// 루팅 중지
        /// </summary>
        public void StopLooting()
        {
            if (currentLootTarget != null)
            {
                currentLootTarget.OnLootingStopped -= HandleLootingStopped;
                currentLootTarget.OnLootEmpty -= HandleLootEmpty;
                currentLootTarget.StopLooting();
                currentLootTarget = null;
            }
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
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowInteractionHint(interactionHintText);
            }
        }

        /// <summary>
        /// 인터랙션 힌트 숨기기
        /// </summary>
        private void HideInteractionHint()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideInteractionHint();
            }
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
            // 루팅 범위 표시
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, lootRange);

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
