using UnityEngine;
using MiniExtractionShooter.Managers;

namespace MiniExtractionShooter.Level
{
    /// <summary>
    /// 출발 구역 (HomeScene -> GameScene)
    /// 5초 대기, 구역 이탈 시 리셋
    /// </summary>
    public class DeploymentZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float deploymentTime = 5f;

        [Header("State")]
        [SerializeField] private float currentTime = 0f;
        [SerializeField] private bool playerInZone = false;
        [SerializeField] private bool isDeploying = false;

        [Header("Visual")]
        [SerializeField] private MeshRenderer zoneRenderer;
        [SerializeField] private Color inactiveColor = new Color(0f, 0.5f, 1f, 0.3f);
        [SerializeField] private Color activeColor = new Color(0f, 0.5f, 1f, 0.7f);

        // Events
        public event System.Action OnDeploymentStarted;
        public event System.Action OnDeploymentCancelled;
        public event System.Action<float> OnDeploymentProgress; // 0~1
        public event System.Action OnDeploymentComplete;

        public float DeploymentProgress => currentTime / deploymentTime;
        public bool IsDeploying => isDeploying;
        public float RemainingTime => Mathf.Max(0, deploymentTime - currentTime);

        private void Start()
        {
            UpdateVisual();
        }

        private void Update()
        {
            if (playerInZone && isDeploying)
            {
                currentTime += Time.deltaTime;

                // 진행률 이벤트
                OnDeploymentProgress?.Invoke(DeploymentProgress);

                // 출발 완료
                if (currentTime >= deploymentTime)
                {
                    CompleteDeployment();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInZone = true;
                StartDeployment();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInZone = false;
                CancelDeployment();
            }
        }

        /// <summary>
        /// 출발 시작
        /// </summary>
        private void StartDeployment()
        {
            if (isDeploying) return;

            isDeploying = true;
            currentTime = 0f;

            UpdateVisual();

            OnDeploymentStarted?.Invoke();

            // Debug.Log("Deployment started!");
        }

        /// <summary>
        /// 출발 취소 (구역 이탈)
        /// </summary>
        private void CancelDeployment()
        {
            if (!isDeploying) return;

            isDeploying = false;
            currentTime = 0f;

            UpdateVisual();

            OnDeploymentCancelled?.Invoke();

            // Debug.Log("Deployment cancelled!");
        }

        /// <summary>
        /// 출발 완료
        /// </summary>
        private void CompleteDeployment()
        {
            isDeploying = false;

            OnDeploymentComplete?.Invoke();

            // GameManager에 출발 알림
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DeployToGame();
            }

            // Debug.Log("Deployment complete!");
        }

        /// <summary>
        /// 비주얼 업데이트
        /// </summary>
        private void UpdateVisual()
        {
            if (zoneRenderer != null)
            {
                Material mat = zoneRenderer.material;
                mat.color = isDeploying ? activeColor : inactiveColor;
            }
        }

        /// <summary>
        /// 출발 시간 설정
        /// </summary>
        public void SetDeploymentTime(float time)
        {
            deploymentTime = time;
        }

        /// <summary>
        /// 강제 출발 완료 (치트/디버그용)
        /// </summary>
        public void ForceComplete()
        {
            if (playerInZone)
            {
                CompleteDeployment();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);

                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.center, box.size);
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                    Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
                }
            }

            // 라벨 표시
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "DEPLOYMENT ZONE");
        }
#endif
    }
}
