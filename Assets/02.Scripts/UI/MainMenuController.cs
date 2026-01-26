using UnityEngine;
using UnityEngine.SceneManagement;
using MiniExtractionShooter.Core;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 메인 메뉴 UI 컨트롤러
    /// 게임 계속하기, 세이브 데이터 삭제, 게임 종료 기능
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("게임 씬 이름 (Continue 버튼 클릭 시 로드될 씬)")]
        [SerializeField] private string gameSceneName = "HomeScene";
        
        [Header("UI References (선택)")]
        [Tooltip("데이터 삭제 확인 패널")]
        [SerializeField] private GameObject deleteConfirmPanel;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        private void Start()
        {
            // 삭제 확인 패널 초기에 숨기기
            if (deleteConfirmPanel != null)
            {
                deleteConfirmPanel.SetActive(false);
            }
        }

        #region Public Button Methods
        
        /// <summary>
        /// 게임 계속하기 버튼
        /// - 세이브 파일 있으면 게임 씬 로드 (로드 시 SaveDataManager가 자동 복원)
        /// - 세이브 파일 없으면 새 게임 시작
        /// </summary>
        public void OnContinueGameClicked()
        {
            bool hasSave = SaveDataManager.Instance?.HasSaveFile() ?? false;
            Debug.Log($"[MainMenu] OnContinueGameClicked 호출됨. gameSceneName: {gameSceneName}, hasSave: {hasSave}");
            Debug.Log($"[MainMenu] 현재 Time.timeScale: {Time.timeScale}");

            // 게임 씬 로드 - SaveDataManager가 씬 로드 후 데이터를 복원
            Debug.Log($"[MainMenu] 씬 로드 시도: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// 세이브 데이터 삭제 버튼
        /// 확인 패널이 있으면 패널 표시, 없으면 즉시 삭제
        /// </summary>
        public void OnDeleteSaveDataClicked()
        {
            if (deleteConfirmPanel != null)
            {
                // 확인 패널 표시
                deleteConfirmPanel.SetActive(true);
            }
            else
            {
                // 확인 패널 없으면 즉시 삭제
                DeleteSaveData();
            }
        }

        /// <summary>
        /// 삭제 확인 - 예 버튼
        /// </summary>
        public void OnConfirmDeleteClicked()
        {
            DeleteSaveData();
            
            if (deleteConfirmPanel != null)
            {
                deleteConfirmPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 삭제 확인 - 아니오 버튼
        /// </summary>
        public void OnCancelDeleteClicked()
        {
            if (deleteConfirmPanel != null)
            {
                deleteConfirmPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 게임 종료 버튼
        /// </summary>
        public void OnQuitGameClicked()
        {
            if (debugMode)
            {
                Debug.Log("[MainMenu] Quit Game clicked.");
            }

#if UNITY_EDITOR
            // 에디터에서는 플레이 모드 종료
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 빌드된 게임에서는 어플리케이션 종료
            Application.Quit();
#endif
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 세이브 데이터 삭제 실행
        /// </summary>
        private void DeleteSaveData()
        {
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.DeleteSave();
                
                if (debugMode)
                {
                    Debug.Log("[MainMenu] Save data deleted.");
                }
            }
            else
            {
                Debug.LogWarning("[MainMenu] SaveDataManager not found. Cannot delete save data.");
            }
        }

        #endregion
    }
}
