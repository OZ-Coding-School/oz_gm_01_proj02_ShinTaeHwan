using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniExtractionShooter.Level;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 탈출 UI - 탈출 진행률 표시
    /// </summary>
    public class ExtractionUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject extractionPanel;

        [Header("Progress")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color completeColor = Color.cyan;

        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.1f;

        private ExtractionZone currentZone;
        private bool isAnimating = false;

        private void Awake()
        {
            CreateUIElementsIfMissing();
        }

        private void Start()
        {
            Hide();
            FindExtractionZone();
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
        /// ExtractionZone 찾기 및 이벤트 연결
        /// </summary>
        private void FindExtractionZone()
        {
            ExtractionZone[] zones = FindObjectsOfType<ExtractionZone>();

            foreach (var zone in zones)
            {
                zone.OnExtractionStarted += OnExtractionStarted;
                zone.OnExtractionProgress += OnExtractionProgress;
                zone.OnExtractionCancelled += OnExtractionCancelled;
                zone.OnExtractionComplete += OnExtractionComplete;
            }
        }

        /// <summary>
        /// UI 표시
        /// </summary>
        public void Show()
        {
            if (extractionPanel != null)
            {
                extractionPanel.SetActive(true);
            }
            isAnimating = true;
        }

        /// <summary>
        /// UI 숨기기
        /// </summary>
        public void Hide()
        {
            if (extractionPanel != null)
            {
                extractionPanel.SetActive(false);
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

            if (progressText != null)
            {
                progressText.text = $"{progress * 100f:F0}%";
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

        private void OnExtractionStarted()
        {
            Show();
            SetStatus("탈출 중...");
            UpdateProgress(0f, 5f);
        }

        private void OnExtractionProgress(float progress)
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
                SetStatus("탈출 진행 중...");
            }
        }

        private void OnExtractionCancelled()
        {
            SetStatus("탈출 취소됨");

            // 잠시 후 숨기기
            Invoke(nameof(Hide), 1f);
        }

        private void OnExtractionComplete()
        {
            SetStatus("탈출 성공!");
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
            ExtractionZone[] zones = FindObjectsOfType<ExtractionZone>();
            foreach (var zone in zones)
            {
                zone.OnExtractionStarted -= OnExtractionStarted;
                zone.OnExtractionProgress -= OnExtractionProgress;
                zone.OnExtractionCancelled -= OnExtractionCancelled;
                zone.OnExtractionComplete -= OnExtractionComplete;
            }
        }
    }
}
