using UnityEngine;
using MiniExtractionShooter.UI.Inventory;

namespace MiniExtractionShooter.Player
{
    /// <summary>
    /// 플레이어 이동 및 조준 컨트롤러 (Top-Down)
    /// TDD 기준: 이동 5m/s, 달리기 8m/s
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Aim Settings (Top-Down)")]
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private float rotationSpeed = 15f;

        [Header("State")]
        [SerializeField] private bool canMove = true;
        [SerializeField] private bool canRotate = true;

        private CharacterController characterController;
        private Vector3 velocity;
        private Vector3 moveDirection;
        private bool isRunning;

        // 방어구에 의한 이동 속도 감소
        private float armorSpeedReduction = 0f;
        // 무기에 의한 이동 속도 계수
        private float weaponSpeedModifier = 1f;
        private Camera mainCamera;

        // 현재 마우스가 가리키는 월드 위치
        private Vector3 currentAimPoint;

        // Events
        public System.Action<bool> OnMovementStateChanged;
        public System.Action<Vector3> OnAimDirectionChanged;

        public bool IsMoving => moveDirection.magnitude > 0.1f;
        public bool IsRunning => isRunning && IsMoving;
        public float CurrentSpeed => IsRunning ? runSpeed : walkSpeed;
        public Vector3 MoveDirection => moveDirection;
        public Vector3 CurrentAimPoint => currentAimPoint;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            characterController = GetComponent<CharacterController>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (canMove)
            {
                HandleMovementInput();
                HandleRunInput();
            }
            else
            {
                moveDirection = Vector3.zero;
            }

            ApplyMovement();

            if (canRotate)
            {
                HandleAiming();
            }

            // Tab 또는 I 키로 인벤토리 열기
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
            {
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.ToggleInventory();
                }
            }

            // 무기 교체 (1, 2 키)
            if (canMove) // UI가 열려있지 않을 때만
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    Weapon.WeaponManager.Instance?.SwitchToWeapon(0);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Weapon.WeaponManager.Instance?.SwitchToWeapon(1);
                }
            }
        }

        private void HandleMovementInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // Top-Down에서는 X-Z 평면 이동
            moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        }

        private void HandleRunInput()
        {
            // 달리기 입력 + 스태미나 체크
            bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
            
            if (wantsToRun && PlayerStamina.Instance != null)
            {
                isRunning = PlayerStamina.Instance.CanRun();
            }
            else
            {
                isRunning = wantsToRun;
            }
        }

        private void ApplyMovement()
        {
            // 중력 적용
            if (characterController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;

            // 달리기 중이면 스태미나 소모
            if (IsRunning && IsMoving && PlayerStamina.Instance != null)
            {
                if (!PlayerStamina.Instance.ConsumeRunStamina())
                {
                    // 스태미나 부족 - 달리기 중단
                    isRunning = false;
                }
            }

            // 이동 속도 계산 (방어구 감소 + 무기 계수 적용)
            float currentSpeed = IsRunning ? runSpeed : walkSpeed;
            currentSpeed *= (1f - armorSpeedReduction);
            currentSpeed *= weaponSpeedModifier; // 무기/ADS 속도 계수 적용

            // 이동 적용
            Vector3 move = moveDirection * currentSpeed * Time.deltaTime;
            move.y = velocity.y * Time.deltaTime;

            characterController.Move(move);

            // 이동 상태 변경 이벤트
            OnMovementStateChanged?.Invoke(IsMoving);
        }

        private void HandleAiming()
        {
            if (mainCamera == null) return;

            // 플레이어 높이의 평면 생성
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // 평면과 레이의 교차점 계산
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 targetPosition = ray.GetPoint(distance);
                currentAimPoint = targetPosition; // 마우스 월드 위치 저장

                Vector3 direction = (targetPosition - transform.position).normalized;

                if (direction.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                    OnAimDirectionChanged?.Invoke(direction);
                }
            }
        }

        /// <summary>
        /// 이동 가능 여부 설정 (루팅 중 비활성화)
        /// </summary>
        public void SetCanMove(bool value)
        {
            Debug.Log($"[PlayerController] SetCanMove called: {value}");
            canMove = value;
            if (!canMove)
            {
                moveDirection = Vector3.zero;
            }
        }

        /// <summary>
        /// 회전 가능 여부 설정
        /// </summary>
        public void SetCanRotate(bool value)
        {
            canRotate = value;
        }

        /// <summary>
        /// 방어구에 의한 속도 감소 설정
        /// </summary>
        public void SetArmorSpeedReduction(float reduction)
        {
            armorSpeedReduction = Mathf.Clamp01(reduction);
        }

        /// <summary>
        /// 무기에 의한 속도 계수 설정 (무기 무게, ADS 등)
        /// </summary>
        public void SetWeaponSpeedModifier(float modifier)
        {
            weaponSpeedModifier = Mathf.Clamp(modifier, 0.1f, 1f);
        }

        /// <summary>
        /// 강제 위치 이동 (스폰 등)
        /// </summary>
        public void Teleport(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }

        public bool CanMove => canMove;
        public bool CanRotate => canRotate;
    }
}
