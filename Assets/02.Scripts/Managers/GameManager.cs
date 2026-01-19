using UnityEngine;
using UnityEngine.SceneManagement;
using MiniExtractionShooter.Core;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Level;
using System.Collections.Generic;

namespace MiniExtractionShooter.Managers
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    /// <summary>
    /// 게임 매니저 - 게임 상태 및 흐름 관리
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        [Header("Spawn Points")]
        [SerializeField] private SpawnPoint playerSpawnPoint;
        [SerializeField] private List<SpawnPoint> enemySpawnPoints = new List<SpawnPoint>();

        [Header("Statistics")]
        [SerializeField] private int enemiesKilled = 0;
        [SerializeField] private int itemsLooted = 0;
        [SerializeField] private float playTime = 0f;

        [Header("Settings")]
        [SerializeField] private bool pauseOnFocusLost = true;

        // Events
        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action OnGameStarted;
        public event System.Action OnGamePaused;
        public event System.Action OnGameResumed;
        public event System.Action OnGameOver;
        public event System.Action OnVictory;

        public GameState CurrentState => currentState;
        public int EnemiesKilled => enemiesKilled;
        public int ItemsLooted => itemsLooted;
        public float PlayTime => playTime;

        protected override void Awake()
        {
            base.Awake();
            dontDestroyOnLoad = true;
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void Start()
        {
            // 플레이어 사망 이벤트 구독
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnDeath += HandlePlayerDeath;
            }
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                playTime += Time.deltaTime;

                // ESC 키로 일시정지
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGame();
                }
            }
            else if (currentState == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ResumeGame();
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && pauseOnFocusLost && currentState == GameState.Playing)
            {
                PauseGame();
            }
        }

        /// <summary>
        /// 게임 시작
        /// </summary>
        public void StartGame()
        {
            SetState(GameState.Playing);

            // 통계 초기화
            enemiesKilled = 0;
            itemsLooted = 0;
            playTime = 0f;

            // 플레이어 스폰
            SpawnPlayer();

            // 시간 정상화
            Time.timeScale = 1f;

            OnGameStarted?.Invoke();

            Debug.Log("Game Started!");
        }

        /// <summary>
        /// 플레이어 스폰
        /// </summary>
        private void SpawnPlayer()
        {
            if (playerSpawnPoint == null)
            {
                // 씬에서 플레이어 스폰 포인트 찾기
                SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
                foreach (var sp in spawnPoints)
                {
                    if (sp.Type == SpawnPointType.Player)
                    {
                        playerSpawnPoint = sp;
                        break;
                    }
                }
            }

            if (playerSpawnPoint != null && PlayerController.Instance != null)
            {
                PlayerController.Instance.Teleport(playerSpawnPoint.GetSpawnPosition());
                PlayerController.Instance.transform.rotation = playerSpawnPoint.GetSpawnRotation();
            }
        }

        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;

            SetState(GameState.Paused);
            Time.timeScale = 0f;

            OnGamePaused?.Invoke();

            // UI 표시
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPauseMenu();
            }

            Debug.Log("Game Paused!");
        }

        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;

            SetState(GameState.Playing);
            Time.timeScale = 1f;

            OnGameResumed?.Invoke();

            // UI 숨기기
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HidePauseMenu();
            }

            Debug.Log("Game Resumed!");
        }

        /// <summary>
        /// 게임 오버
        /// </summary>
        public void GameOver()
        {
            SetState(GameState.GameOver);
            Time.timeScale = 0f;

            OnGameOver?.Invoke();

            // UI 표시
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOverScreen(false);
            }

            Debug.Log("Game Over!");
        }

        /// <summary>
        /// 탈출 성공
        /// </summary>
        public void ExtractionSuccess()
        {
            SetState(GameState.Victory);
            Time.timeScale = 0f;

            // 성공 시 저장
            SaveDataManager.Instance?.SaveGame();

            OnVictory?.Invoke();

            // UI 표시
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOverScreen(true);
            }

            Debug.Log("Extraction Success!");
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// 메인 메뉴로 이동
        /// </summary>
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SetState(GameState.MainMenu);
            SceneManager.LoadScene(0); // 메인 메뉴 씬 인덱스
        }

        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 상태 변경
        /// </summary>
        private void SetState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// 플레이어 사망 핸들러
        /// </summary>
        private void HandlePlayerDeath()
        {
            GameOver();
        }

        /// <summary>
        /// 적 처치 카운트 증가
        /// </summary>
        public void AddEnemyKill()
        {
            enemiesKilled++;
        }

        /// <summary>
        /// 아이템 루팅 카운트 증가
        /// </summary>
        public void AddItemLooted()
        {
            itemsLooted++;
        }

        /// <summary>
        /// 게임 통계 가져오기
        /// </summary>
        public (int kills, int items, float time) GetStatistics()
        {
            return (enemiesKilled, itemsLooted, playTime);
        }

        #region Save/Load

        /// <summary>
        /// 씬 언로드 시 자동 저장
        /// </summary>
        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            // 게임 플레이 중이거나 승리 시에만 저장
            if (currentState == GameState.Playing || currentState == GameState.Victory)
            {
                SaveDataManager.Instance?.SaveGame();
            }
        }

        /// <summary>
        /// 저장된 게임 로드
        /// </summary>
        public bool LoadSavedGame()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.HasSaveFile())
            {
                return SaveDataManager.Instance.LoadGame();
            }
            return false;
        }

        /// <summary>
        /// 저장 파일 삭제 (새 게임 시작 시)
        /// </summary>
        public void DeleteSaveAndStartNew()
        {
            SaveDataManager.Instance?.DeleteSave();
            StartGame();
        }

        /// <summary>
        /// 저장 파일 존재 확인
        /// </summary>
        public bool HasSaveFile()
        {
            return SaveDataManager.Instance != null && SaveDataManager.Instance.HasSaveFile();
        }

        #endregion
    }
}
