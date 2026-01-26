using UnityEngine;
using MiniExtractionShooter.Managers;

namespace MiniExtractionShooter.Level
{
    /// <summary>
    /// 탈출 구역
    /// TDD 기준: 5초 대기, 구역 이탈 시 리셋, 피격 시 유지
    /// </summary>
    public class ExtractionZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float extractionTime = 5f;

        [Header("State")]
        [SerializeField] private float currentTime = 0f;
        [SerializeField] private bool playerInZone = false;
        [SerializeField] private bool isExtracting = false;

        [Header("Visual")]
        [SerializeField] private MeshRenderer zoneRenderer;
        [SerializeField] private Color inactiveColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color activeColor = new Color(0f, 1f, 0f, 0.7f);

        // Events
        public event System.Action OnExtractionStarted;
        public event System.Action OnExtractionCancelled;
        public event System.Action<float> OnExtractionProgress; // 0~1
        public event System.Action OnExtractionComplete;

        public float ExtractionProgress => currentTime / extractionTime;
        public bool IsExtracting => isExtracting;
        public float RemainingTime => Mathf.Max(0, extractionTime - currentTime);

        private void Start()
        {
            UpdateVisual();
            
            // UI에 등록
            MiniExtractionShooter.UI.ExtractionUI.Instance?.RegisterZone(this);
        }

        private void Update()
        {
            if (playerInZone && isExtracting)
            {
                currentTime += Time.deltaTime;

                // 진행률 이벤트
                OnExtractionProgress?.Invoke(ExtractionProgress);

                // 탈출 완료
                if (currentTime >= extractionTime)
                {
                    CompleteExtraction();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInZone = true;
                StartExtraction();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInZone = false;
                CancelExtraction();
            }
        }

        /// <summary>
        /// 탈출 시작
        /// </summary>
        private void StartExtraction()
        {
            if (isExtracting) return;

            isExtracting = true;
            currentTime = 0f;

            UpdateVisual();

            OnExtractionStarted?.Invoke();

            // Debug.Log("Extraction started!");
        }

        /// <summary>
        /// 탈출 취소 (구역 이탈)
        /// </summary>
        private void CancelExtraction()
        {
            if (!isExtracting) return;

            isExtracting = false;
            currentTime = 0f;

            UpdateVisual();

            OnExtractionCancelled?.Invoke();

            // Debug.Log("Extraction cancelled!");
        }

        /// <summary>
        /// 탈출 완료
        /// </summary>
        private void CompleteExtraction()
        {
            isExtracting = false;

            OnExtractionComplete?.Invoke();

            // GameManager에 탈출 성공 알림
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ExtractionSuccess();
            }

            // Debug.Log("Extraction complete!");
        }

        /// <summary>
        /// 비주얼 업데이트
        /// </summary>
        private void UpdateVisual()
        {
            if (zoneRenderer != null)
            {
                Material mat = zoneRenderer.material;
                mat.color = isExtracting ? activeColor : inactiveColor;
            }
        }

        /// <summary>
        /// 탈출 시간 설정 (난이도 조절용)
        /// </summary>
        public void SetExtractionTime(float time)
        {
            extractionTime = time;
        }

        /// <summary>
        /// 강제 탈출 완료 (치트/디버그용)
        /// </summary>
        public void ForceComplete()
        {
            if (playerInZone)
            {
                CompleteExtraction();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

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
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "EXTRACTION ZONE");
        }
#endif
    }
}
