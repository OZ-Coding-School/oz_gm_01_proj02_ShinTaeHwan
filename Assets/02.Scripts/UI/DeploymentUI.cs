using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniExtractionShooter.Level;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 출발 UI - 출발 진행률 표시
    /// </summary>
    public class DeploymentUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject deploymentPanel;

        [Header("Progress")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0f, 0.5f, 1f);
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color completeColor = Color.cyan;

        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.1f;

        private DeploymentZone currentZone;
        private bool isAnimating = false;

        private void Start()
        {
            Hide();
            FindDeploymentZone();
        }

        private void Update()
        {
            if (isAnimating && progressSlider != null)
            {
                // 진행 중 펄스 애니메이션
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
                progressSlider.transform.localScale = Vector3.one * pulse;
            }
        }

        /// <summary>
        /// DeploymentZone 찾기 및 이벤트 연결
        /// </summary>
        private void FindDeploymentZone()
        {
            DeploymentZone[] zones = FindObjectsOfType<DeploymentZone>();

            foreach (var zone in zones)
            {
                zone.OnDeploymentStarted += OnDeploymentStarted;
                zone.OnDeploymentProgress += OnDeploymentProgress;
                zone.OnDeploymentCancelled += OnDeploymentCancelled;
                zone.OnDeploymentComplete += OnDeploymentComplete;
            }
        }

        /// <summary>
        /// UI 표시
        /// </summary>
        public void Show()
        {
            if (deploymentPanel != null)
            {
                deploymentPanel.SetActive(true);
            }
            isAnimating = true;
        }

        /// <summary>
        /// UI 숨기기
        /// </summary>
        public void Hide()
        {
            if (deploymentPanel != null)
            {
                deploymentPanel.SetActive(false);
            }
            isAnimating = false;

            if (progressSlider != null)
            {
                progressSlider.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 진행률 업데이트
        /// </summary>
        public void UpdateProgress(float progress, float remainingTime)
        {
            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }

            if (timerText != null)
            {
                timerText.text = $"{remainingTime:F1}초";
            }

            // 진행률에 따른 색상 변경
            if (progressFill != null)
            {
                if (progress >= 0.9f)
                {
                    progressFill.color = completeColor;
                }
                else if (progress >= 0.5f)
                {
                    progressFill.color = warningColor;
                }
                else
                {
                    progressFill.color = normalColor;
                }
            }
        }

        /// <summary>
        /// 상태 텍스트 설정
        /// </summary>
        public void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        // Event handlers

        private void OnDeploymentStarted()
        {
            Show();
            SetStatus("출발 준비 중...");
            UpdateProgress(0f, 5f);
        }

        private void OnDeploymentProgress(float progress)
        {
            float remainingTime = (1f - progress) * 5f; // 5초 기준
            UpdateProgress(progress, remainingTime);

            // 진행률에 따른 상태 메시지
            if (progress >= 0.8f)
            {
                SetStatus("거의 완료!");
            }
            else if (progress >= 0.5f)
            {
                SetStatus("출발 진행 중...");
            }
        }

        private void OnDeploymentCancelled()
        {
            SetStatus("출발 취소됨");

            // 잠시 후 숨기기
            Invoke(nameof(Hide), 1f);
        }

        private void OnDeploymentComplete()
        {
            SetStatus("출발!");
            UpdateProgress(1f, 0f);

            if (progressFill != null)
            {
                progressFill.color = completeColor;
            }

            isAnimating = false;
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            DeploymentZone[] zones = FindObjectsOfType<DeploymentZone>();
            foreach (var zone in zones)
            {
                zone.OnDeploymentStarted -= OnDeploymentStarted;
                zone.OnDeploymentProgress -= OnDeploymentProgress;
                zone.OnDeploymentCancelled -= OnDeploymentCancelled;
                zone.OnDeploymentComplete -= OnDeploymentComplete;
            }
        }
    }
}
