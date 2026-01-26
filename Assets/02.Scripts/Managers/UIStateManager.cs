using UnityEngine;
using System.Collections.Generic;
using System;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.UI;
using MiniExtractionShooter.Level;

namespace MiniExtractionShooter.Managers
{
    /// <summary>
    /// UI 상태를 중앙에서 관리하는 매니저
    /// HUD를 제외한 UI가 열릴 때 플레이어 컨트롤을 비활성화
    /// UI 간 배타적 열기 지원 (한 UI 열면 다른 UI 닫힘)
    /// </summary>
    public class UIStateManager : MonoBehaviour
    {
        public static UIStateManager Instance { get; private set; }

        // 현재 열린 UI 목록
        private HashSet<string> openUIs = new HashSet<string>();

        // UI 닫기 콜백 등록 (UI 이름 → 닫기 액션)
        private Dictionary<string, Action> closeCallbacks = new Dictionary<string, Action>();

        // 배타적 UI 그룹 (이 그룹에 속한 UI는 서로 동시에 열리지 않음)
        // Note: Loot는 Inventory를 함께 여는 구조이므로 Inventory 대신 Loot를 사용
        private HashSet<string> exclusiveUIGroup = new HashSet<string>
        {
            "Inventory", "Map"
        };

        // 플레이어 컨트롤이 비활성화되었는지 여부
        public bool IsPlayerControlDisabled => openUIs.Count > 0;

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

        /// <summary>
        /// UI 닫기 콜백 등록
        /// </summary>
        /// <param name="uiName">UI 식별자</param>
        /// <param name="closeCallback">UI 닫기 함수</param>
        public void RegisterCloseCallback(string uiName, Action closeCallback)
        {
            if (string.IsNullOrEmpty(uiName) || closeCallback == null) return;
            closeCallbacks[uiName] = closeCallback;
        }

        /// <summary>
        /// UI 닫기 콜백 해제
        /// </summary>
        public void UnregisterCloseCallback(string uiName)
        {
            if (string.IsNullOrEmpty(uiName)) return;
            closeCallbacks.Remove(uiName);
        }

        /// <summary>
        /// UI가 열릴 때 호출
        /// </summary>
        /// <param name="uiName">UI 식별자</param>
        public void OpenUI(string uiName)
        {
            if (string.IsNullOrEmpty(uiName)) return;

            // 배타적 그룹에 속한 UI면 다른 배타적 UI 닫기
            if (exclusiveUIGroup.Contains(uiName))
            {
                CloseExclusiveUIs(uiName);
            }

            bool wasEmpty = openUIs.Count == 0;
            openUIs.Add(uiName);

            // Debug.Log($"[UIStateManager] OpenUI: {uiName}, Total open: {openUIs.Count}");

            // 첫 번째 UI가 열리면 플레이어 컨트롤 비활성화
            if (wasEmpty && openUIs.Count > 0)
            {
                SetPlayerControlEnabled(false);
            }
        }

        /// <summary>
        /// 배타적 UI 그룹에서 다른 UI 닫기
        /// </summary>
        private void CloseExclusiveUIs(string exceptUI)
        {
            List<string> uisToClose = new List<string>();

            foreach (string uiName in openUIs)
            {
                if (exclusiveUIGroup.Contains(uiName) && uiName != exceptUI)
                {
                    uisToClose.Add(uiName);
                }
            }

            foreach (string uiName in uisToClose)
            {
                // Debug.Log($"[UIStateManager] Auto-closing {uiName} because {exceptUI} is opening");
                
                // 콜백으로 실제 UI 닫기
                if (closeCallbacks.TryGetValue(uiName, out Action closeCallback))
                {
                    closeCallback.Invoke();
                }
                else
                {
                    // 콜백이 없으면 목록에서만 제거
                    openUIs.Remove(uiName);
                }
            }
        }

        /// <summary>
        /// UI가 닫힐 때 호출
        /// </summary>
        /// <param name="uiName">UI 식별자</param>
        public void CloseUI(string uiName)
        {
            if (string.IsNullOrEmpty(uiName)) return;

            openUIs.Remove(uiName);

            // Debug.Log($"[UIStateManager] CloseUI: {uiName}, Total open: {openUIs.Count}");

            // 모든 UI가 닫히면 플레이어 컨트롤 활성화
            if (openUIs.Count == 0)
            {
                SetPlayerControlEnabled(true);
            }
        }

        /// <summary>
        /// 특정 UI가 열려있는지 확인
        /// </summary>
        public bool IsUIOpen(string uiName)
        {
            return openUIs.Contains(uiName);
        }

        /// <summary>
        /// 모든 UI 강제 닫기 (씬 전환 등에서 사용)
        /// </summary>
        public void CloseAllUIs()
        {
            openUIs.Clear();
            SetPlayerControlEnabled(true);
        }

        /// <summary>
        /// 플레이어 컨트롤 활성화/비활성화
        /// </summary>
        private void SetPlayerControlEnabled(bool enabled)
        {
            // Debug.Log($"[UIStateManager] SetPlayerControlEnabled: {enabled}");

            // 크로스헤어
            if (DynamicCrosshair.Instance != null)
            {
                DynamicCrosshair.Instance.SetVisible(enabled);
            }

            // 플레이어 이동
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetCanMove(enabled);
                PlayerController.Instance.SetCanRotate(enabled);
            }

            // 공격
            if (PlayerCombat.Instance != null)
            {
                PlayerCombat.Instance.SetCanShoot(enabled);
            }

            // 카메라 마우스 오프셋
            if (Camera.main != null)
            {
                CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.SetMouseOffsetEnabled(enabled);
                }
            }

            // 마우스 커서
            Cursor.visible = !enabled;
            Cursor.lockState = enabled ? CursorLockMode.Confined : CursorLockMode.None;
        }
    }
}

