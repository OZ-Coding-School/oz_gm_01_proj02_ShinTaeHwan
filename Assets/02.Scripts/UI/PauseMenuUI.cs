using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MiniExtractionShooter.Managers;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 일시정지 메뉴 UI 컨트롤러
    /// ESC 키로 열리며, 게임으로 돌아가기/메인 메뉴/게임 종료 버튼 포함
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance { get; private set; }

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;       // 게임으로 돌아가기
        [SerializeField] private Button mainMenuButton;     // 메인 메뉴로 돌아가기
        [SerializeField] private Button quitButton;         // 게임 종료

        [Header("Scene")]
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            // 버튼 이벤트 등록
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            // 초기에는 숨김
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 일시정지 메뉴 표시
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            // UIStateManager로 플레이어 컨트롤 비활성화
            UIStateManager.Instance?.OpenUI("PauseMenu");
        }

        /// <summary>
        /// 일시정지 메뉴 숨기기
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            // UIStateManager로 플레이어 컨트롤 활성화
            UIStateManager.Instance?.CloseUI("PauseMenu");
        }

        /// <summary>
        /// 게임으로 돌아가기 버튼 클릭
        /// </summary>
        private void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        /// <summary>
        /// 메인 메뉴로 돌아가기 버튼 클릭
        /// </summary>
        private void OnMainMenuClicked()
        {
            Debug.Log($"[PauseMenuUI] OnMainMenuClicked 호출됨. mainMenuSceneName: {mainMenuSceneName}");
            Debug.Log($"[PauseMenuUI] 현재 Time.timeScale: {Time.timeScale}");

            Time.timeScale = 1f;
            Debug.Log($"[PauseMenuUI] Time.timeScale을 1f로 설정 완료. 씬 로드 시도: {mainMenuSceneName}");

            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// 게임 종료 버튼 클릭
        /// </summary>
        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }
    }
}
