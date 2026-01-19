using UnityEngine;
using MiniExtractionShooter.Core;

namespace MiniExtractionShooter.Managers
{
    /// <summary>
    /// UI 매니저 - 모든 UI 요소 관리
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject lootPanel;

        [Header("UI Elements")]
        [SerializeField] private TMPro.TextMeshProUGUI messageText;
        [SerializeField] private TMPro.TextMeshProUGUI interactionHintText;
        [SerializeField] private GameObject extractionProgressPanel;
        [SerializeField] private UnityEngine.UI.Slider extractionProgressSlider;
        [SerializeField] private TMPro.TextMeshProUGUI extractionTimeText;

        [Header("Game Over")]
        [SerializeField] private TMPro.TextMeshProUGUI gameOverTitleText;
        [SerializeField] private TMPro.TextMeshProUGUI statisticsText;

        [Header("Settings")]
        [SerializeField] private float messageDisplayTime = 2f;

        private float messageTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            // 초기 UI 상태 설정
            HideAllPanels();

            if (hudPanel != null)
            {
                hudPanel.SetActive(true);
            }
        }

        private void Update()
        {
            // 메시지 타이머
            if (messageTimer > 0)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0)
                {
                    HideMessage();
                }
            }
        }

        /// <summary>
        /// 모든 패널 숨기기
        /// </summary>
        private void HideAllPanels()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (lootPanel != null) lootPanel.SetActive(false);
            if (extractionProgressPanel != null) extractionProgressPanel.SetActive(false);
        }

        /// <summary>
        /// 메시지 표시
        /// </summary>
        public void ShowMessage(string message, float duration = -1f)
        {
            if (messageText != null)
            {
                messageText.text = message;
                messageText.gameObject.SetActive(true);
                messageTimer = duration > 0 ? duration : messageDisplayTime;
            }

            Debug.Log($"[UI Message] {message}");
        }

        /// <summary>
        /// 메시지 숨기기
        /// </summary>
        public void HideMessage()
        {
            if (messageText != null)
            {
                messageText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 인터랙션 힌트 표시
        /// </summary>
        public void ShowInteractionHint(string hint)
        {
            if (interactionHintText != null)
            {
                interactionHintText.text = hint;
                interactionHintText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 인터랙션 힌트 숨기기
        /// </summary>
        public void HideInteractionHint()
        {
            if (interactionHintText != null)
            {
                interactionHintText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 탈출 진행률 표시
        /// </summary>
        public void ShowExtractionProgress(float progress, float remainingTime)
        {
            if (extractionProgressPanel != null)
            {
                extractionProgressPanel.SetActive(true);
            }

            if (extractionProgressSlider != null)
            {
                extractionProgressSlider.value = progress;
            }

            if (extractionTimeText != null)
            {
                extractionTimeText.text = $"{remainingTime:F1}초";
            }
        }

        /// <summary>
        /// 탈출 진행률 숨기기
        /// </summary>
        public void HideExtractionProgress()
        {
            if (extractionProgressPanel != null)
            {
                extractionProgressPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 일시정지 메뉴 표시
        /// </summary>
        public void ShowPauseMenu()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 일시정지 메뉴 숨기기
        /// </summary>
        public void HidePauseMenu()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 게임 오버 화면 표시
        /// </summary>
        public void ShowGameOverScreen(bool isVictory)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = isVictory ? "탈출 성공!" : "게임 오버";
                gameOverTitleText.color = isVictory ? Color.green : Color.red;
            }

            // 통계 표시
            if (statisticsText != null && GameManager.Instance != null)
            {
                var stats = GameManager.Instance.GetStatistics();
                statisticsText.text = $"처치한 적: {stats.kills}\n" +
                                     $"획득한 아이템: {stats.items}\n" +
                                     $"플레이 시간: {FormatTime(stats.time)}";
            }
        }

        /// <summary>
        /// 게임 오버 화면 숨기기
        /// </summary>
        public void HideGameOverScreen()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 루팅 패널 표시
        /// </summary>
        public void ShowLootPanel()
        {
            if (lootPanel != null)
            {
                lootPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 루팅 패널 숨기기
        /// </summary>
        public void HideLootPanel()
        {
            if (lootPanel != null)
            {
                lootPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 시간 포맷팅
        /// </summary>
        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }

        // UI 버튼 콜백들

        /// <summary>
        /// 재개 버튼
        /// </summary>
        public void OnResumeButtonClicked()
        {
            GameManager.Instance?.ResumeGame();
        }

        /// <summary>
        /// 재시작 버튼
        /// </summary>
        public void OnRestartButtonClicked()
        {
            GameManager.Instance?.RestartGame();
        }

        /// <summary>
        /// 메인 메뉴 버튼
        /// </summary>
        public void OnMainMenuButtonClicked()
        {
            GameManager.Instance?.GoToMainMenu();
        }

        /// <summary>
        /// 종료 버튼
        /// </summary>
        public void OnQuitButtonClicked()
        {
            GameManager.Instance?.QuitGame();
        }
    }
}
