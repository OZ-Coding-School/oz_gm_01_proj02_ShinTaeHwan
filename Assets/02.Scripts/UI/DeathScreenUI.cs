using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 사망 화면 UI 컨트롤러
    /// 붉은색 "YOU DIED" 텍스트와 계속하기 버튼
    /// </summary>
    public class DeathScreenUI : MonoBehaviour
    {
        public static DeathScreenUI Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button continueButton;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 1f;

        [Header("Scene")]
        [SerializeField] private string homeSceneName = "HomeScene";

        private bool isAnimating = false;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            // 초기에는 숨김
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);

            // 버튼 이벤트 등록
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        /// <summary>
        /// 사망 화면 표시
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// 페이드인 애니메이션
        /// </summary>
        private System.Collections.IEnumerator FadeIn()
        {
            isAnimating = true;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                float elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    // Time.timeScale이 0이어도 동작하도록 unscaledDeltaTime 사용
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                    yield return null;
                }

                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            isAnimating = false;
        }

        /// <summary>
        /// 계속하기 버튼 클릭 시 HomeScene으로 이동
        /// </summary>
        private void OnContinueClicked()
        {
            if (isAnimating) return;

            Time.timeScale = 1f;
            SceneManager.LoadScene(homeSceneName);
        }

        /// <summary>
        /// 사망 화면 숨기기
        /// </summary>
        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }
    }
}
