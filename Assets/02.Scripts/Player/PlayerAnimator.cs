using UnityEngine;

namespace MiniExtractionShooter.Player
{
    /// <summary>
    /// 플레이어 애니메이션 컨트롤러
    /// Animator 파라미터:
    /// - MoveSpeed (Float): 0=Idle, 1=Walk, 2=Run (BlendTree)
    /// - IsAiming (Bool): 조준 상태 (Strafe)
    /// - DoRoll (Trigger): 구르기
    /// - DoFire (Trigger): 발사 (Upper Body Layer)
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCombat playerCombat;
        
        [Header("Roll Settings")]
        [SerializeField] private float rollCooldown = 1f;
        [SerializeField] private float rollDuration = 0.6f;
        
        private Animator animator;
        
        // 파라미터 ID 캐싱 (성능 최적화)
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int DoRollHash = Animator.StringToHash("DoRoll");
        private static readonly int DoFireHash = Animator.StringToHash("DoFire");
        
        // 상태
        private float lastRollTime = -999f;
        private bool isRolling;
        
        // Events
        public event System.Action OnRollStart;
        public event System.Action OnRollEnd;
        
        public bool IsRolling => isRolling;
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
        }
        
        private void Start()
        {
            // 자동으로 같은 오브젝트의 컴포넌트 찾기
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
                if (playerController == null)
                {
                    playerController = PlayerController.Instance;
                }
            }
            
            if (playerCombat == null)
            {
                playerCombat = GetComponent<PlayerCombat>();
                if (playerCombat == null)
                {
                    playerCombat = PlayerCombat.Instance;
                }
            }
            
            // 이벤트 구독
            if (playerCombat != null)
            {
                playerCombat.OnFireAttempt += HandleFire;
                playerCombat.OnADSStart += HandleADSStart;
                playerCombat.OnADSEnd += HandleADSEnd;
            }
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (playerCombat != null)
            {
                playerCombat.OnFireAttempt -= HandleFire;
                playerCombat.OnADSStart -= HandleADSStart;
                playerCombat.OnADSEnd -= HandleADSEnd;
            }
        }
        
        private void Update()
        {
            UpdateMovementAnimation();
            HandleRollInput();
        }
        
        /// <summary>
        /// 이동 애니메이션 업데이트 (BlendTree)
        /// </summary>
        private void UpdateMovementAnimation()
        {
            if (playerController == null) return;
            
            // 구르기 중에는 이동 애니메이션 업데이트하지 않음
            if (isRolling) return;
            
            float targetMoveSpeed = 0f;
            
            if (playerController.IsMoving)
            {
                if (playerController.IsRunning)
                {
                    // 달리기: MoveSpeed = 2
                    targetMoveSpeed = 2f;
                }
                else
                {
                    // 걷기: MoveSpeed = 1
                    targetMoveSpeed = 1f;
                }
            }
            else
            {
                // 정지 (Idle): MoveSpeed = 0
                targetMoveSpeed = 0f;
            }
            
            // 부드러운 전환을 위해 Lerp 사용
            float currentMoveSpeed = animator.GetFloat(MoveSpeedHash);
            float smoothMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, Time.deltaTime * 10f);
            
            // 목표값에 충분히 가까우면 정확한 값으로 스냅
            if (Mathf.Abs(smoothMoveSpeed - targetMoveSpeed) < 0.05f)
            {
                smoothMoveSpeed = targetMoveSpeed;
            }
            
            animator.SetFloat(MoveSpeedHash, smoothMoveSpeed);
        }
        
        /// <summary>
        /// 구르기 입력 처리 (스페이스바)
        /// </summary>
        private void HandleRollInput()
        {
            // 구르기 중이거나 쿨다운 중이면 무시
            if (isRolling) return;
            if (Time.time - lastRollTime < rollCooldown) return;
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartRoll();
            }
        }
        
        /// <summary>
        /// 구르기 시작
        /// </summary>
        private void StartRoll()
        {
            isRolling = true;
            lastRollTime = Time.time;
            
            // 애니메이션 트리거
            animator.SetTrigger(DoRollHash);
            
            // 구르기 중 이동/사격 비활성화
            playerController?.SetCanMove(false);
            playerController?.SetCanRotate(false);
            playerCombat?.SetCanShoot(false);
            
            OnRollStart?.Invoke();
            
            // 구르기 종료 예약
            Invoke(nameof(EndRoll), rollDuration);
        }
        
        /// <summary>
        /// 구르기 종료
        /// </summary>
        private void EndRoll()
        {
            isRolling = false;
            
            // 이동/사격 재활성화
            playerController?.SetCanMove(true);
            playerController?.SetCanRotate(true);
            playerCombat?.SetCanShoot(true);
            
            OnRollEnd?.Invoke();
        }
        
        /// <summary>
        /// 발사 애니메이션 처리
        /// </summary>
        private void HandleFire()
        {
            // 구르기 중에는 발사 애니메이션 재생하지 않음
            if (isRolling) return;
            
            animator.SetTrigger(DoFireHash);
        }
        
        /// <summary>
        /// 조준(ADS) 시작 - Strafe 모드
        /// </summary>
        private void HandleADSStart()
        {
            animator.SetBool(IsAimingHash, true);
        }
        
        /// <summary>
        /// 조준(ADS) 종료 - 일반 이동 모드
        /// </summary>
        private void HandleADSEnd()
        {
            animator.SetBool(IsAimingHash, false);
        }
        
        /// <summary>
        /// 외부에서 구르기 강제 실행
        /// </summary>
        public void ForceRoll()
        {
            if (!isRolling && Time.time - lastRollTime >= rollCooldown)
            {
                StartRoll();
            }
        }
        
        /// <summary>
        /// 외부에서 발사 애니메이션 강제 실행
        /// </summary>
        public void ForceFire()
        {
            HandleFire();
        }
        
        /// <summary>
        /// 현재 조준 상태 반환
        /// </summary>
        public bool IsAiming => animator.GetBool(IsAimingHash);
    }
}
