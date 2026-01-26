using UnityEngine;
using System.Collections;
using MiniExtractionShooter.Data;
using MiniExtractionShooter.UI;
using MiniExtractionShooter.UI.Inventory;

namespace MiniExtractionShooter.Player
{
    /// <summary>
    /// 소모품 사용 시스템
    /// 사용 시간, 이동 속도 제어, 취소 로직 관리
    /// </summary>
    public class PlayerConsumableSystem : MonoBehaviour
    {
        public static PlayerConsumableSystem Instance { get; private set; }

        [Header("State")]
        [SerializeField] private bool isUsingItem = false;
        [SerializeField] private float currentUseTimer = 0f;
        
        // 현재 사용 중인 아이템 정보
        private InventoryItem currentItem;
        private ConsumableData currentConsumableData;
        private Coroutine useCoroutine;

        // Events
        public event System.Action<ConsumableData, float> OnUseStarted; // data, duration
        public event System.Action OnUseCompleted;
        public event System.Action OnUseCanceled;

        public bool IsUsingItem => isUsingItem;

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
        }

        private void Start()
        {
            // 구르기 시 취소를 위해 이벤트 구독
            if (PlayerController.Instance != null)
            {
                var animator = PlayerController.Instance.GetComponent<PlayerAnimator>();
                if (animator != null)
                {
                    animator.OnRollStart += CancelUsage;
                }
            }
        }

        private void OnDestroy()
        {
            if (PlayerController.Instance != null)
            {
                var animator = PlayerController.Instance.GetComponent<PlayerAnimator>();
                if (animator != null)
                {
                    animator.OnRollStart -= CancelUsage;
                }
            }
        }

        private void Update()
        {
            if (!isUsingItem) return;

            // X키로 취소
            if (Input.GetKeyDown(KeyCode.X))
            {
                CancelUsage();
            }
        }

        /// <summary>
        /// 아이템 사용 시작 - 모든 소모품 사용의 유일한 진입점
        /// </summary>
        public bool UseItem(InventoryItem item)
        {
            if (isUsingItem || item == null) return false;

            // 재장전 중에는 아이템 사용 불가
            if (Weapon.WeaponManager.Instance != null && 
                Weapon.WeaponManager.Instance.ActiveWeapon != null &&
                Weapon.WeaponManager.Instance.ActiveWeapon.IsReloading)
            {
                // Debug.Log("[PlayerConsumableSystem] Cannot use item while reloading.");
                return false;
            }

            // 인벤토리에 실제로 있는지 확인
            var inventoryItem = PlayerInventory.Instance?.FindItem(item.itemData);
            if (inventoryItem == null || inventoryItem.amount <= 0)
            {
                // Debug.LogWarning("[PlayerConsumableSystem] Item not found in inventory.");
                return false;
            }

            // ConsumableData인 경우 사용 시간이 있는 아이템
            if (item.itemData is ConsumableData consumableData)
            {
                StartCoroutine(UseItemRoutine(inventoryItem, consumableData));
                return true;
            }
            
            // 일반 아이템 (구형 Health/Food 등) 즉시 사용
            return UseItemInstantly(inventoryItem);
        }

        /// <summary>
        /// 구형 아이템 즉시 사용 (ConsumableData가 아닌 경우 호환성)
        /// </summary>
        private bool UseItemInstantly(InventoryItem item)
        {
            if (item.ItemType == ItemType.Health && PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.Heal(10f); // 기본 회복량
                PlayerInventory.Instance?.RemoveItemAmount(item, 1);
                // OnInventoryChanged 이벤트로 QuickSlotManager 자동 갱신
                // Debug.Log($"[PlayerConsumableSystem] Instantly used: {item.ItemName}");
                return true;
            }
            
            // Debug.LogWarning($"[PlayerConsumableSystem] Cannot use item: {item.ItemName}");
            return false;
        }

        private IEnumerator UseItemRoutine(InventoryItem item, ConsumableData data)
        {
            isUsingItem = true;
            currentItem = item;
            currentConsumableData = data;
            currentUseTimer = 0f;

            // 이동 속도 감소
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetConsumableSpeedModifier(data.speedMultiplierWhenUsing);
            }

            OnUseStarted?.Invoke(data, data.useDuration);
            
            // 사용 사운드 재생
            if (!string.IsNullOrEmpty(data.useSoundName))
            {
                Managers.SoundManager.Instance?.PlaySFX(data.useSoundName, PlayerController.Instance.transform.position);
            }
            else if (data.useSound != null)
            {
                AudioSource.PlayClipAtPoint(data.useSound, PlayerController.Instance.transform.position);
            }

            // 공용 프로그레스 바 표시
            ActionProgressUI.Instance?.Show(item.ItemName, true);

            // 진행
            while (currentUseTimer < data.useDuration)
            {
                currentUseTimer += Time.deltaTime;
                
                // 프로그레스 업데이트
                ActionProgressUI.Instance?.UpdateProgress(currentUseTimer / data.useDuration);
                
                yield return null;
            }

            // 완료
            CompleteUsage();
        }

        private void CompleteUsage()
        {
            if (!isUsingItem) return;

            // Debug.Log($"[PlayerConsumableSystem] Completed using {currentItem.ItemName}");

            // 효과 적용
            ApplyEffects(currentConsumableData);

            // 아이템 소모 (OnInventoryChanged 이벤트로 QuickSlotManager 자동 갱신)
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.RemoveItemAmount(currentItem, 1);
            }

            Cleanup();
            OnUseCompleted?.Invoke();
            
            // 공용 프로그레스 바 숨기기
            ActionProgressUI.Instance?.Hide();
        }

        public void CancelUsage()
        {
            if (!isUsingItem) return;

            // Debug.Log("[PlayerConsumableSystem] Usage Canceled");

            if (useCoroutine != null)
            {
                StopCoroutine(useCoroutine);
            }

            Cleanup();
            OnUseCanceled?.Invoke();
            
            // 공용 프로그레스 바 숨기기
            ActionProgressUI.Instance?.Hide();
        }

        private void Cleanup()
        {
            isUsingItem = false;
            currentItem = null;
            currentConsumableData = null;

            // 이동 속도 복구
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetConsumableSpeedModifier(1f);
            }
        }

        private void ApplyEffects(ConsumableData data)
        {
            if (data == null)
            {
                // Debug.LogWarning("[PlayerConsumableSystem] ApplyEffects: ConsumableData is null!");
                return;
            }

            // Debug.Log($"[PlayerConsumableSystem] ApplyEffects - Health: {data.restoreHealth}, Energy: {data.restoreEnergy}, Hydration: {data.restoreHydration}");

            // HP 회복
            if (data.restoreHealth > 0)
            {
                if (PlayerHealth.Instance != null)
                {
                    float beforeHP = PlayerHealth.Instance.CurrentHealth;
                    PlayerHealth.Instance.Heal(data.restoreHealth);
                    float afterHP = PlayerHealth.Instance.CurrentHealth;
                    // Debug.Log($"[PlayerConsumableSystem] HP: {beforeHP} -> {afterHP} (+{data.restoreHealth})");
                }
            }

            // 에너지/수분 회복 (음식)
            if (PlayerStats.Instance != null)
            {
                if (data.restoreEnergy > 0)
                {
                    float beforeEnergy = PlayerStats.Instance.CurrentEnergy;
                    PlayerStats.Instance.RestoreEnergy(data.restoreEnergy);
                    float afterEnergy = PlayerStats.Instance.CurrentEnergy;
                    // Debug.Log($"[PlayerConsumableSystem] Energy: {beforeEnergy} -> {afterEnergy} (+{data.restoreEnergy})");
                }
                if (data.restoreHydration > 0)
                {
                    float beforeHydration = PlayerStats.Instance.CurrentHydration;
                    PlayerStats.Instance.RestoreHydration(data.restoreHydration);
                    float afterHydration = PlayerStats.Instance.CurrentHydration;
                    // Debug.Log($"[PlayerConsumableSystem] Hydration: {beforeHydration} -> {afterHydration} (+{data.restoreHydration})");
                }
            }

            // 추가 효과 (사운드 등) - 필요시 구현
        }
    }
}
