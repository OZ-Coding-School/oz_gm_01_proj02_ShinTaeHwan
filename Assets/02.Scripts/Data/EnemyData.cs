using UnityEngine;

namespace MiniExtractionShooter.Data
{
    /// <summary>
    /// Enemy ScriptableObject - TDD 기반 적 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "MiniExtractionShooter/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName = "Guard";

        [Header("Stats")]
        [Tooltip("체력 (Guard: 60)")]
        public float health = 60f;

        [Tooltip("이동 속도 (m/s, Guard: 3.5)")]
        public float moveSpeed = 3.5f;

        [Header("Detection")]
        [Tooltip("감지 범위 (m, Guard: 12)")]
        public float detectionRange = 12f;

        [Tooltip("감지 각도 (도, Guard: 120)")]
        public float detectionAngle = 120f;

        [Header("Combat")]
        [Tooltip("공격 사거리 (m, Guard: 10)")]
        public float attackRange = 10f;

        [Tooltip("기본 명중률 (0~1, Guard: 0.6)")]
        [Range(0f, 1f)]
        public float accuracy = 0.6f;

        [Tooltip("반응 시간 (초, Guard: 0.5)")]
        public float reactionTime = 0.5f;

        [Tooltip("공격 간격 (초)")]
        public float attackInterval = 0.5f;

        [Header("Weapon")]
        [Tooltip("장착 무기")]
        public WeaponData equippedWeapon;

        [Tooltip("최소 탄약 보유량")]
        public int minAmmo = 12;

        [Tooltip("최대 탄약 보유량")]
        public int maxAmmo = 24;

        [Header("Patrol")]
        [Tooltip("순찰 시 대기 시간")]
        public float patrolWaitTime = 2f;

        [Header("Loot")]
        [Tooltip("사망 시 드랍할 루트테이블")]
        public LootTableData lootTable;

        [Header("Investigation")]
        [Tooltip("조사 모드에서의 이동 속도 (Guard: 2.0)")]
        public float investigationSpeed = 2.0f;

        [Tooltip("조사 모드에서의 감지 범위 배율 (기본 감지 범위 * 배율)")]
        public float investigationDetectionMultiplier = 1.5f;

        [Tooltip("조사 지점 도착 후 대기 시간")]
        public float investigationWaitTime = 3.0f;

        [Header("Visual")]
        [Tooltip("사망 시 재생할 사운드 이름")]
        public string deathSoundName = "EnemyDie";

        public GameObject deathEffectPrefab;

        /// <summary>
        /// 거리와 플레이어 이동 상태에 따른 명중률 계산
        /// </summary>
        public float CalculateAccuracy(float distance, bool playerMoving)
        {
            float calculatedAccuracy = accuracy;

            // 5m 이후 m당 3% 감소
            if (distance > 5f)
            {
                calculatedAccuracy -= (distance - 5f) * 0.03f;
            }

            // 플레이어 이동 시 20% 감소
            if (playerMoving)
            {
                calculatedAccuracy -= 0.2f;
            }

            // 최소 10%, 최대 90%
            return Mathf.Clamp(calculatedAccuracy, 0.1f, 0.9f);
        }
    }
}
