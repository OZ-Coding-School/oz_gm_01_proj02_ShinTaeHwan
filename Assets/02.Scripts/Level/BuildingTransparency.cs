using UnityEngine;
using System.Collections.Generic;

namespace MiniExtractionShooter.Level
{
    /// <summary>
    /// 카메라와 플레이어 사이 건물 투명화 시스템
    /// 카메라 오브젝트에 부착
    /// </summary>
    public class BuildingTransparency : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private float sphereCastRadius = 0.5f;
        [SerializeField] private float checkInterval = 0.05f;

        [Header("Transparency Settings")]
        [SerializeField] private float transparentAlpha = 0.3f;
        [SerializeField] private float fadeSpeed = 5f;

        [Header("Mouse Transparency")]
        [SerializeField] private bool enableMouseTransparency = true;

        [Header("References")]
        [SerializeField] private Transform target;

        // 카메라-플레이어 간 건물 투명화용
        private HashSet<TransparentObject> currentOccluders = new HashSet<TransparentObject>();
        private HashSet<TransparentObject> previousOccluders = new HashSet<TransparentObject>();

        // 마우스 오버 건물 투명화용
        private HashSet<TransparentObject> mouseOccluders = new HashSet<TransparentObject>();
        private HashSet<TransparentObject> previousMouseOccluders = new HashSet<TransparentObject>();

        private Camera mainCamera;
        private float checkTimer = 0f;

        private void Start()
        {
            mainCamera = Camera.main;

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            checkTimer += Time.deltaTime;
            if (checkTimer >= checkInterval)
            {
                checkTimer = 0f;

                // 카메라-플레이어 간 건물 투명화
                if (target != null)
                {
                    DetectOccludingBuildings();
                }

                // 마우스 오버 건물 투명화
                if (enableMouseTransparency && mainCamera != null)
                {
                    DetectMouseOverBuilding();
                }
            }
        }

        private void DetectOccludingBuildings()
        {
            // 이전 프레임 목록 저장
            previousOccluders = new HashSet<TransparentObject>(currentOccluders);
            currentOccluders.Clear();

            // 카메라에서 플레이어 방향으로 SphereCast
            Vector3 direction = target.position - transform.position;
            float distance = direction.magnitude;

            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position,
                sphereCastRadius,
                direction.normalized,
                distance,
                buildingLayer
            );

            // 감지된 건물 처리
            foreach (RaycastHit hit in hits)
            {
                TransparentObject transparentObj = hit.collider.GetComponent<TransparentObject>();

                if (transparentObj == null)
                {
                    transparentObj = hit.collider.GetComponentInParent<TransparentObject>();
                }

                if (transparentObj != null)
                {
                    currentOccluders.Add(transparentObj);
                    transparentObj.SetTransparent(true, transparentAlpha, fadeSpeed);
                }
            }

            // 더 이상 가리지 않는 오브젝트 복구
            foreach (TransparentObject obj in previousOccluders)
            {
                if (!currentOccluders.Contains(obj) && !mouseOccluders.Contains(obj))
                {
                    obj.SetTransparent(false, 1f, fadeSpeed);
                }
            }
        }

        private void DetectMouseOverBuilding()
        {
            previousMouseOccluders = new HashSet<TransparentObject>(mouseOccluders);
            mouseOccluders.Clear();

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayer))
            {
                TransparentObject transparentObj = hit.collider.GetComponent<TransparentObject>();

                if (transparentObj == null)
                {
                    transparentObj = hit.collider.GetComponentInParent<TransparentObject>();
                }

                if (transparentObj != null)
                {
                    mouseOccluders.Add(transparentObj);
                    transparentObj.SetTransparent(true, transparentAlpha, fadeSpeed);
                }
            }

            // 더 이상 마우스가 올려져 있지 않은 오브젝트 복구
            foreach (TransparentObject obj in previousMouseOccluders)
            {
                if (!mouseOccluders.Contains(obj) && !currentOccluders.Contains(obj))
                {
                    obj.SetTransparent(false, 1f, fadeSpeed);
                }
            }
        }

        /// <summary>
        /// 런타임에 타겟 설정
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
