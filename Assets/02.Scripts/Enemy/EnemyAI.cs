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
        Attack,
        Investigate
    }

    /// <summary>
    /// 적 AI 상태머신
    /// TDD 기준: Idle → Alert → Chase → Attack
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private EnemyState currentState = EnemyState.Idle;

        [Header("Patrol")]
        private Transform[] patrolPoints;
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
        private Enemy enemy;

        private Transform target;
        private Vector3 lastKnownPosition;
        private float stateTimer = 0f;
        private float patrolWaitTimer = 0f;
        private bool isWaitingAtPatrolPoint = false;
        private int lastPatrolDestinationIndex = -1;

        // Events
        public event System.Action<EnemyState> OnStateChanged;

        public EnemyState CurrentState => currentState;
        public EnemyData Data => enemy != null ? enemy.Data : null;
        public Transform Target => target;


        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            navAgent = GetComponent<NavMeshAgent>();
            enemyCombat = GetComponent<EnemyCombat>();
            enemyHealth = GetComponent<EnemyHealth>();

            // 장애물 마스크에 Ground 레이어 자동 추가
            obstacleMask |= 1 << LayerMask.NameToLayer("Ground");

            if (eyePoint == null)
            {
                eyePoint = transform;
            }
        }

        private void Start()
        {
            if (Data != null)
            {
                navAgent.speed = Data.moveSpeed;
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
                case EnemyState.Investigate:
                    UpdateInvestigate();
                    break;
            }

            stateTimer += Time.deltaTime;
        }

        /// <summary>
        /// 타겟 시야 확보 여부 체크
        /// </summary>
        private bool HasLineOfSight(Transform targetTransform)
        {
            if (targetTransform == null) return false;

            Vector3 directionToTarget = (targetTransform.position - eyePoint.position).normalized;
            float distance = Vector3.Distance(eyePoint.position, targetTransform.position);

            // 레이캐스트로 장애물 확인 (Ground 포함)
            if (!Physics.Raycast(eyePoint.position, directionToTarget, distance, obstacleMask))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 플레이어 감지
        /// </summary>
        private void DetectPlayer()
        {
            if (PlayerController.Instance == null) return;

            Transform player = PlayerController.Instance.transform;
            float distance = Vector3.Distance(transform.position, player.position);

            // 현재 상태에 따른 감지 범위 적용
            float detectionRange = Data.detectionRange;
            if (currentState == EnemyState.Investigate)
            {
                detectionRange *= Data.investigationDetectionMultiplier;
            }

            // 감지 범위 내
            if (distance <= detectionRange)
            {
                // 시야각 체크
                Vector3 directionToPlayer = (player.position - eyePoint.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToPlayer);

                // 시야각 체크 (조사 모드에서는 시야각도 조금 넓혀줄 수 있음, 일단은 거리만 확장)
                if (angle <= Data.detectionAngle * 0.5f)
                {
                    // 시야 확보 확인 (Ground 포함)
                    if (HasLineOfSight(player))
                    {
                        // 플레이어 발견
                        target = player;
                        lastKnownPosition = player.position;

                        if (currentState == EnemyState.Idle || currentState == EnemyState.Investigate)
                        {
                            // Debug.Log($"Enemy '{gameObject.name}' detected the player!");
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
                if (patrolWaitTimer >= Data.patrolWaitTime)
                {
                    isWaitingAtPatrolPoint = false;
                    patrolWaitTimer = 0f;
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                }
            }
            else
            {
                // 다음 순찰 포인트로 이동
                if (currentPatrolIndex != lastPatrolDestinationIndex)
                {
                    Vector3 targetPos = patrolPoints[currentPatrolIndex].position;
                    // Debug.Log($"[EnemyAI] Setting destination to Patrol Point {currentPatrolIndex}: {targetPos}");
                    navAgent.SetDestination(targetPos);
                    lastPatrolDestinationIndex = currentPatrolIndex;
                }

                // 도착 확인 (경로가 완전히 계산되었고, 목적지에 도착했는지 확인)
                // hasPath가 true이고, 남은 거리가 stoppingDistance(또는 0.2f 중 큰 값) 이하여야 도착으로 인정
                float arrivalThreshold = Mathf.Max(navAgent.stoppingDistance, 0.2f);
                if (!navAgent.pathPending && navAgent.hasPath && navAgent.remainingDistance <= arrivalThreshold)
                {
                    // Debug.Log($"[EnemyAI] Arrived at Patrol Point {currentPatrolIndex}. Waiting... (Dist: {navAgent.remainingDistance})");
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
            if (stateTimer >= Data.reactionTime)
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
                // 타겟을 완전히 잃음 - Idle 복귀
                SetState(EnemyState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, target.position);

            // 시야 확보 시 위치 갱신
            if (HasLineOfSight(target))
            {
                lastKnownPosition = target.position;
                navAgent.SetDestination(target.position);

                // 공격 사거리 내 진입
                if (distance <= Data.attackRange)
                {
                    SetState(EnemyState.Attack);
                }
            }
            else
            {
                // 시야 차단됨 - 마지막 위치로 이동
                navAgent.SetDestination(lastKnownPosition);

                // 마지막 위치 도착 여부 확인
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    // 마지막 위치에 왔는데도 없으면 추적 종료
                    target = null;
                    SetState(EnemyState.Idle);
                    return;
                }
            }

            // 타겟이 감지 범위를 너무 벗어나면 포기 (시야 확보와 무관하게 너무 멀어지면)
            if (distance > Data.detectionRange * 2.0f) // 추적 거리 여유 증가
            {
                target = null;
                SetState(EnemyState.Idle);
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
            if (distance > Data.attackRange)
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

            // 이전 상태 정리
            if (currentState == EnemyState.Attack)
            {
                enemyCombat?.StopFiringLoop();
            }

            currentState = newState;
            stateTimer = 0f;

            // 기본 속도로 복구 (Investigate 등에서 변경되었을 수 있음)
            if (Data != null)
            {
                navAgent.speed = Data.moveSpeed;
            }

            switch (newState)
            {
                case EnemyState.Idle:
                    navAgent.isStopped = false;
                    target = null;
                    lastPatrolDestinationIndex = -1; // 상태 진입 시 목적지 재설정 유도
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
                case EnemyState.Investigate:
                    navAgent.isStopped = false;
                    navAgent.speed = Data.investigationSpeed;
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

        private void OnEnable()
        {
            Managers.NoiseManager.OnNoiseGenerated += OnNoiseDetected;
        }

        private void OnDisable()
        {
            Managers.NoiseManager.OnNoiseGenerated -= OnNoiseDetected;
        }

        /// <summary>
        /// 소음 감지 처리
        /// </summary>
        private void OnNoiseDetected(Vector3 position, float range)
        {
            if (currentState == EnemyState.Chase || currentState == EnemyState.Attack || enemyHealth.IsDead) return;

            float distance = Vector3.Distance(transform.position, position);
            
            // 소음 범위 내에 있고, 감지 범위 내에 있으면 (청각 감지)
            // 청각 감지 범위는 시각 감지 범위보다 넓게 설정하거나 별도로 설정 가능하지만
            // 여기서는 소음 발생원의 범위(range)가 닿으면 듣는 것으로 처리
            if (distance <= range)
            {
                // 이미 조사 중이라면 더 가까운 소리나 새로운 소리에 반응
                if (currentState == EnemyState.Investigate)
                {
                    lastKnownPosition = position;
                    navAgent.SetDestination(lastKnownPosition);
                }
                else if (currentState == EnemyState.Idle || currentState == EnemyState.Alert)
                {
                    Managers.SoundManager.Instance?.PlaySFX("EnemySpot", transform.position);
                    lastKnownPosition = position;
                    SetState(EnemyState.Investigate);
                }
            }
        }

        private float investigationTimer = 0f;
        private bool isScanning = false;

        /// <summary>
        /// Investigate 상태 업데이트 (소음 발생 지점 조사)
        /// </summary>
        private void UpdateInvestigate()
        {
            navAgent.isStopped = false;

            // 목적지 이동
            if (!isScanning)
            {
                navAgent.SetDestination(lastKnownPosition);

                // 도착 확인
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    isScanning = true;
                    investigationTimer = 0f;
                }
            }
            else
            {
                // 주변 두리번거리기 (회전)
                transform.Rotate(Vector3.up, 30f * Time.deltaTime);

                investigationTimer += Time.deltaTime;
                if (investigationTimer >= Data.investigationWaitTime)
                {
                    // 조사 종료 -> 복귀
                    SetState(EnemyState.Idle);
                    isScanning = false;
                    
                    // 원래 속도로 복구
                    navAgent.speed = Data.moveSpeed;
                }
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
            if (!showDebugGizmos || Data == null) return;

            // 감지 범위
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, Data.detectionRange);

            // 공격 범위
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, Data.attackRange);

            // 시야각
            Gizmos.color = Color.yellow;
            Vector3 leftDir = Quaternion.Euler(0, -Data.detectionAngle * 0.5f, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, Data.detectionAngle * 0.5f, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, leftDir * Data.detectionRange);
            Gizmos.DrawRay(transform.position, rightDir * Data.detectionRange);

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
