using UnityEngine;
using UnityEngine.AI;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.Player;

namespace MiniExtractionShooter.Enemy
{
    public enum EnemyState
    {
        Idle,
        Alert,
        Chase,
        Attack
    }

    /// <summary>
    /// 적 AI 상태머신
    /// TDD 기준: Idle → Alert → Chase → Attack
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EnemyData enemyData;

        [Header("State")]
        [SerializeField] private EnemyState currentState = EnemyState.Idle;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private int currentPatrolIndex = 0;

        [Header("Detection")]
        [SerializeField] private Transform eyePoint;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private LayerMask playerMask;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private NavMeshAgent navAgent;
        private EnemyCombat enemyCombat;
        private EnemyHealth enemyHealth;

        private Transform target;
        private Vector3 lastKnownPosition;
        private float stateTimer = 0f;
        private float patrolWaitTimer = 0f;
        private bool isWaitingAtPatrolPoint = false;

        // Events
        public event System.Action<EnemyState> OnStateChanged;

        public EnemyState CurrentState => currentState;
        public EnemyData Data => enemyData;
        public Transform Target => target;

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            enemyCombat = GetComponent<EnemyCombat>();
            enemyHealth = GetComponent<EnemyHealth>();

            if (eyePoint == null)
            {
                eyePoint = transform;
            }
        }

        private void Start()
        {
            if (enemyData != null)
            {
                navAgent.speed = enemyData.moveSpeed;
            }

            SetState(EnemyState.Idle);
        }

        private void Update()
        {
            if (enemyHealth != null && enemyHealth.IsDead)
            {
                navAgent.isStopped = true;
                return;
            }

            // 플레이어 감지
            DetectPlayer();

            // 현재 상태별 업데이트
            switch (currentState)
            {
                case EnemyState.Idle:
                    UpdateIdle();
                    break;
                case EnemyState.Alert:
                    UpdateAlert();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.Attack:
                    UpdateAttack();
                    break;
            }

            stateTimer += Time.deltaTime;
        }

        /// <summary>
        /// 플레이어 감지
        /// </summary>
        private void DetectPlayer()
        {
            if (PlayerController.Instance == null) return;

            Transform player = PlayerController.Instance.transform;
            float distance = Vector3.Distance(transform.position, player.position);

            // 감지 범위 내
            if (distance <= enemyData.detectionRange)
            {
                // 시야각 체크
                Vector3 directionToPlayer = (player.position - eyePoint.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToPlayer);

                if (angle <= enemyData.detectionAngle * 0.5f)
                {
                    // 시야 차단 체크 (레이캐스트)
                    if (!Physics.Raycast(eyePoint.position, directionToPlayer, distance, obstacleMask))
                    {
                        // 플레이어 발견
                        target = player;
                        lastKnownPosition = player.position;

                        if (currentState == EnemyState.Idle)
                        {
                            Debug.Log($"Enemy '{gameObject.name}' detected the player!");
                            SetState(EnemyState.Alert);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Idle 상태 업데이트 (순찰)
        /// </summary>
        private void UpdateIdle()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                // 순찰 포인트 없으면 제자리 대기
                return;
            }

            if (isWaitingAtPatrolPoint)
            {
                patrolWaitTimer += Time.deltaTime;
                if (patrolWaitTimer >= enemyData.patrolWaitTime)
                {
                    isWaitingAtPatrolPoint = false;
                    patrolWaitTimer = 0f;
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                }
            }
            else
            {
                // 다음 순찰 포인트로 이동
                navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);

                // 도착 확인 (경로가 완전히 계산되었고, 목적지에 도착했는지 확인)
                if (!navAgent.pathPending && navAgent.hasPath && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    isWaitingAtPatrolPoint = true;
                }
            }
        }

        /// <summary>
        /// Alert 상태 업데이트 (감지 후 대기)
        /// </summary>
        private void UpdateAlert()
        {
            navAgent.isStopped = true;

            // 플레이어 방향 주시
            if (target != null)
            {
                Vector3 lookDir = (target.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
                }
            }

            // 반응 시간 후 Chase로 전환
            if (stateTimer >= enemyData.reactionTime)
            {
                SetState(EnemyState.Chase);
            }
        }

        /// <summary>
        /// Chase 상태 업데이트 (추적)
        /// </summary>
        private void UpdateChase()
        {
            navAgent.isStopped = false;

            if (target == null)
            {
                // 타겟 상실 - 마지막 위치로 이동
                navAgent.SetDestination(lastKnownPosition);

                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    // 마지막 위치 도착 - Idle로 복귀
                    SetState(EnemyState.Idle);
                }
                return;
            }

            // 타겟 추적
            navAgent.SetDestination(target.position);
            lastKnownPosition = target.position;

            // 공격 사거리 내 진입
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= enemyData.attackRange)
            {
                SetState(EnemyState.Attack);
            }

            // 타겟이 감지 범위를 벗어나면 상실
            if (distance > enemyData.detectionRange * 1.5f)
            {
                target = null;
            }
        }

        /// <summary>
        /// Attack 상태 업데이트 (공격)
        /// </summary>
        private void UpdateAttack()
        {
            if (target == null)
            {
                SetState(EnemyState.Chase);
                return;
            }

            navAgent.isStopped = true;

            // 플레이어 방향 주시
            Vector3 lookDir = (target.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
            }

            // 공격 실행 (EnemyCombat에서 처리)
            if (enemyCombat != null)
            {
                enemyCombat.TryAttack(target);
            }

            // 사거리 이탈 체크
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > enemyData.attackRange)
            {
                SetState(EnemyState.Chase);
            }
        }

        /// <summary>
        /// 상태 변경
        /// </summary>
        public void SetState(EnemyState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            stateTimer = 0f;

            switch (newState)
            {
                case EnemyState.Idle:
                    navAgent.isStopped = false;
                    target = null;
                    break;
                case EnemyState.Alert:
                    navAgent.isStopped = true;
                    break;
                case EnemyState.Chase:
                    navAgent.isStopped = false;
                    break;
                case EnemyState.Attack:
                    navAgent.isStopped = true;
                    break;
            }

            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// 외부에서 타겟 설정 (소리 등으로 인한 인식)
        /// </summary>
        public void AlertToPosition(Vector3 position)
        {
            if (currentState == EnemyState.Idle)
            {
                lastKnownPosition = position;
                SetState(EnemyState.Alert);
            }
        }

        /// <summary>
        /// 순찰 포인트 설정
        /// </summary>
        public void SetPatrolPoints(Transform[] points)
        {
            patrolPoints = points;
            currentPatrolIndex = 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || enemyData == null) return;

            // 감지 범위
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);

            // 공격 범위
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);

            // 시야각
            Gizmos.color = Color.yellow;
            Vector3 leftDir = Quaternion.Euler(0, -enemyData.detectionAngle * 0.5f, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, enemyData.detectionAngle * 0.5f, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, leftDir * enemyData.detectionRange);
            Gizmos.DrawRay(transform.position, rightDir * enemyData.detectionRange);

            // 순찰 경로
            if (patrolPoints != null && patrolPoints.Length > 1)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                        int nextIndex = (i + 1) % patrolPoints.Length;
                        if (patrolPoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                        }
                    }
                }
            }
        }
#endif
    }
}
