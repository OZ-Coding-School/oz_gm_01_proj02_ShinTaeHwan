using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniExtractionShooter.UI
{
    /// <summary>
    /// 공용 프로그레스 바 UI 시스템
    /// 재장전, 아이템 사용 등 진행형 동작에서 공유 사용
    /// </summary>
    public class ActionProgressUI : MonoBehaviour
    {
        public static ActionProgressUI Instance { get; private set; }

        [Header("Progress Bar UI")]
        [SerializeField] private GameObject progressBarContainer;
        [SerializeField] private Image progressBarFill; // Horizontal filled image
        [SerializeField] private TextMeshProUGUI actionNameText; // 동작 이름 표시 (선택)
        [SerializeField] private TextMeshProUGUI cancelPromptText;

        [Header("Settings")]
        [SerializeField] private string cancelPromptFormat = "<color=#FFCC00>X</color> 동작 취소";
        [SerializeField] private bool showActionName = false;

        private bool isActive = false;
        private string currentActionName;

        public bool IsActive => isActive;
        public string CurrentActionName => currentActionName;

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
            // 초기 상태 숨김
            Hide();
        }

        /// <summary>
        /// 프로그레스 바 표시 시작
        /// </summary>
        /// <param name="actionName">동작 이름 (예: "재장전", "치료")</param>
        /// <param name="showCancelPrompt">취소 안내 표시 여부</param>
        public void Show(string actionName, bool showCancelPrompt = true)
        {
            // 이미 다른 동작이 진행 중이면 차단
            if (isActive)
            {
                Debug.LogWarning($"[ActionProgressUI] Already active with '{currentActionName}'. Cannot start '{actionName}'.");
                return;
            }

            isActive = true;
            currentActionName = actionName;
            
            // fillAmount 초기화
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 0f;
            }

            if (progressBarContainer != null)
            {
                progressBarContainer.SetActive(true);
            }

            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 0f;
            }

            if (actionNameText != null && showActionName)
            {
                actionNameText.text = actionName;
                actionNameText.gameObject.SetActive(true);
            }

            SetCancelPromptVisible(showCancelPrompt);

            // Debug.Log($"[ActionProgressUI] Started: {actionName}");
        }

        /// <summary>
        /// 진행률 업데이트 (0 ~ 1)
        /// </summary>
        public void UpdateProgress(float progress)
        {
            if (!isActive) return;

            progress = Mathf.Clamp01(progress);

            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = progress;
            }
        }

        /// <summary>
        /// 프로그레스 바 숨기기
        /// </summary>
        public void Hide()
        {
            isActive = false;
            currentActionName = null;

            if (progressBarContainer != null)
            {
                progressBarContainer.SetActive(false);
            }

            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 0f;
            }

            if (actionNameText != null)
            {
                actionNameText.gameObject.SetActive(false);
            }

            if (cancelPromptText != null)
            {
                cancelPromptText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 취소 안내 문구 표시/숨김
        /// </summary>
        public void SetCancelPromptVisible(bool visible)
        {
            if (cancelPromptText != null)
            {
                cancelPromptText.text = cancelPromptFormat;
                cancelPromptText.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 현재 활성 동작인지 확인
        /// </summary>
        public bool IsActionActive(string actionName)
        {
            return isActive && currentActionName == actionName;
        }

        /// <summary>
        /// 강제로 현재 상태 초기화 (다른 시스템이 충돌 시 사용)
        /// </summary>
        public void ForceReset()
        {
            Debug.LogWarning($"[ActionProgressUI] Force reset called. Previous action: {currentActionName}");
            Hide();
        }
    }
}
