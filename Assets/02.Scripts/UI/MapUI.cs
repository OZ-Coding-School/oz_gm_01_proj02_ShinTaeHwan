using UnityEngine;
using UnityEngine.UI;
using MiniExtractionShooter.Player;
using MiniExtractionShooter.Level;
using MiniExtractionShooter.Managers;

namespace MiniExtractionShooter.UI
{
    public class MapUI : MonoBehaviour
    {
        public static MapUI Instance { get; private set; }

        [Header("Map Panel")]
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private RawImage mapImage;

        [Header("Map Camera")]
        [SerializeField] private Camera mapCamera;
        [SerializeField] private float cameraHeight = 100f;
        [SerializeField] private float orthographicSize = 50f;
        [SerializeField] private Vector2 mapCenter = Vector2.zero;

        [Header("RenderTexture Settings")]
        [SerializeField] private int renderTextureSize = 1024;

        [Header("3D Markers")]
        [SerializeField] private GameObject playerMarker3D;
        [SerializeField] private GameObject extractionMarker3D;
        [SerializeField] private float markerHeight = 50f;

        private RenderTexture mapRenderTexture;
        private ExtractionZone extractionZone;

        public bool IsMapOpen => mapPanel != null && mapPanel.activeSelf;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            SetupRenderTexture();
            SetupMapCamera();

            // 초기 상태: 모두 비활성화
            if (mapPanel != null)
                mapPanel.SetActive(false);

            SetMarkersActive(false);
            SetMapCameraActive(false);

            // UIStateManager에 닫기 콜백 등록
            UIStateManager.Instance?.RegisterCloseCallback("Map", () => {
                if (mapPanel != null && mapPanel.activeSelf)
                {
                    CloseMap();
                }
            });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMap();
            }

            if (IsMapOpen)
            {
                UpdateMarkerPositions();
            }
        }

        private void OnDestroy()
        {
            if (mapRenderTexture != null)
            {
                mapRenderTexture.Release();
                Destroy(mapRenderTexture);
            }
        }

        private void SetupRenderTexture()
        {
            mapRenderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16);
            mapRenderTexture.Create();

            if (mapImage != null)
            {
                mapImage.texture = mapRenderTexture;
            }
        }

        private void SetupMapCamera()
        {
            if (mapCamera == null)
            {
                // 맵 카메라가 없으면 생성
                GameObject camObj = new GameObject("MapCamera");
                camObj.transform.SetParent(transform);
                mapCamera = camObj.AddComponent<Camera>();
            }

            // 카메라 설정
            mapCamera.transform.position = new Vector3(mapCenter.x, cameraHeight, mapCenter.y);
            mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            mapCamera.orthographic = true;
            mapCamera.orthographicSize = orthographicSize;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            mapCamera.targetTexture = mapRenderTexture;
            mapCamera.depth = -10; // 메인 카메라보다 낮은 depth
        }

        public void ToggleMap()
        {
            if (mapPanel == null) return;

            bool willOpen = !mapPanel.activeSelf;

            if (willOpen)
            {
                OpenMap();
            }
            else
            {
                CloseMap();
            }
        }

        private void OpenMap()
        {
            // 마커 위치 업데이트
            UpdateMarkerPositions();

            // 마커 활성화
            SetMarkersActive(true);

            // 카메라 활성화
            SetMapCameraActive(true);

            // 패널 활성화
            mapPanel.SetActive(true);

            // UIStateManager로 플레이어 컨트롤 비활성화
            UIStateManager.Instance?.OpenUI("Map");
        }

        private void CloseMap()
        {
            // 패널 비활성화
            mapPanel.SetActive(false);

            // 마커 비활성화
            SetMarkersActive(false);

            // 카메라 비활성화
            SetMapCameraActive(false);

            // UIStateManager로 플레이어 컨트롤 활성화
            UIStateManager.Instance?.CloseUI("Map");
        }

        private void UpdateMarkerPositions()
        {
            // 플레이어 마커 위치 업데이트
            if (playerMarker3D != null && PlayerController.Instance != null)
            {
                Vector3 playerPos = PlayerController.Instance.transform.position;
                playerMarker3D.transform.position = new Vector3(playerPos.x, markerHeight, playerPos.z);

                // 플레이어 방향 반영
                float playerRotationY = PlayerController.Instance.transform.eulerAngles.y;
                playerMarker3D.transform.rotation = Quaternion.Euler(90f, playerRotationY, 0f);
            }

            // 탈출구 마커 위치 업데이트 (Lazy initialization - SpawnManager 타이밍 문제 해결)
            if (extractionZone == null)
            {
                extractionZone = FindObjectOfType<ExtractionZone>();
            }

            if (extractionMarker3D != null && extractionZone != null)
            {
                Vector3 extractionPos = extractionZone.transform.position;
                extractionMarker3D.transform.position = new Vector3(extractionPos.x, markerHeight, extractionPos.z);
            }
        }

        private void SetMarkersActive(bool active)
        {
            if (playerMarker3D != null)
                playerMarker3D.SetActive(active);

            if (extractionMarker3D != null)
                extractionMarker3D.SetActive(active);
        }

        private void SetMapCameraActive(bool active)
        {
            if (mapCamera != null)
                mapCamera.enabled = active;
        }
    }
}
