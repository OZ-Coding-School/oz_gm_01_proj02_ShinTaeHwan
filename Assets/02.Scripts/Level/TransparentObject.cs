using UnityEngine;
using System.Collections.Generic;

namespace MiniExtractionShooter.Level
{
    /// <summary>
    /// 투명화 가능한 건물 오브젝트
    /// 건물 루트 오브젝트에 부착
    /// </summary>
    public class TransparentObject : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool includeChildren = true;

        private struct MaterialInfo
        {
            public Renderer renderer;
            public Material[] originalMaterials;
            public Material[] transparentMaterials;
            public float[] originalAlphas;
        }

        private List<MaterialInfo> materialInfos = new List<MaterialInfo>();
        private float targetAlpha = 1f;
        private float currentAlpha = 1f;
        private float fadeSpeed = 5f;
        private bool isTransparent = false;
        private bool isInitialized = false;

        private void Awake()
        {
            InitializeMaterials();
        }

        /// <summary>
        /// Material 초기화 및 투명 버전 생성
        /// </summary>
        private void InitializeMaterials()
        {
            if (isInitialized) return;

            Renderer[] renderers = includeChildren
                ? GetComponentsInChildren<Renderer>()
                : new Renderer[] { GetComponent<Renderer>() };

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                MaterialInfo info = new MaterialInfo
                {
                    renderer = renderer,
                    originalMaterials = renderer.sharedMaterials,
                    transparentMaterials = new Material[renderer.sharedMaterials.Length],
                    originalAlphas = new float[renderer.sharedMaterials.Length]
                };

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material original = renderer.sharedMaterials[i];
                    if (original == null) continue;

                    // 원본 Material 인스턴스 복제
                    Material transparentMat = new Material(original);
                    SetMaterialTransparent(transparentMat);

                    info.transparentMaterials[i] = transparentMat;
                    info.originalAlphas[i] = GetMaterialAlpha(original);
                }

                materialInfos.Add(info);
            }

            isInitialized = true;
        }

        /// <summary>
        /// URP Material을 투명 모드로 설정
        /// </summary>
        private void SetMaterialTransparent(Material mat)
        {
            // URP Lit Shader 투명 설정
            mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0);   // 0 = Alpha

            // Render Queue 설정
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Keywords 설정
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");

            // ZWrite Off for proper transparency
            mat.SetInt("_ZWrite", 0);

            // Alpha 블렌딩 설정
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        /// <summary>
        /// Material에서 알파값 추출
        /// </summary>
        private float GetMaterialAlpha(Material mat)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                return mat.GetColor("_BaseColor").a;
            }
            else if (mat.HasProperty("_Color"))
            {
                return mat.GetColor("_Color").a;
            }
            return 1f;
        }

        /// <summary>
        /// 투명화 상태 설정
        /// </summary>
        public void SetTransparent(bool transparent, float alpha, float speed)
        {
            isTransparent = transparent;
            targetAlpha = transparent ? alpha : 1f;
            fadeSpeed = speed;

            // 투명 Material로 교체 (처음 투명화될 때)
            if (transparent && Mathf.Approximately(currentAlpha, 1f))
            {
                ApplyTransparentMaterials();
            }
        }

        private void Update()
        {
            if (Mathf.Approximately(currentAlpha, targetAlpha)) return;

            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            UpdateMaterialAlpha(currentAlpha);

            // 완전 불투명으로 복구 완료 시 원본 Material로 교체
            if (!isTransparent && Mathf.Approximately(currentAlpha, 1f))
            {
                ApplyOriginalMaterials();
            }
        }

        /// <summary>
        /// Material 알파값 업데이트
        /// </summary>
        private void UpdateMaterialAlpha(float alpha)
        {
            foreach (var info in materialInfos)
            {
                if (info.renderer == null) continue;

                Material[] currentMats = info.renderer.materials;

                for (int i = 0; i < currentMats.Length; i++)
                {
                    if (currentMats[i] == null) continue;

                    if (currentMats[i].HasProperty("_BaseColor"))
                    {
                        Color color = currentMats[i].GetColor("_BaseColor");
                        color.a = info.originalAlphas[i] * alpha;
                        currentMats[i].SetColor("_BaseColor", color);
                    }
                    else if (currentMats[i].HasProperty("_Color"))
                    {
                        Color color = currentMats[i].GetColor("_Color");
                        color.a = info.originalAlphas[i] * alpha;
                        currentMats[i].SetColor("_Color", color);
                    }
                }
            }
        }

        /// <summary>
        /// 투명 Material 적용
        /// </summary>
        private void ApplyTransparentMaterials()
        {
            foreach (var info in materialInfos)
            {
                if (info.renderer == null) continue;
                info.renderer.materials = info.transparentMaterials;
            }
        }

        /// <summary>
        /// 원본 Material 복구
        /// </summary>
        private void ApplyOriginalMaterials()
        {
            foreach (var info in materialInfos)
            {
                if (info.renderer == null) continue;
                info.renderer.sharedMaterials = info.originalMaterials;
            }
        }

        private void OnDestroy()
        {
            // 생성된 투명 Material 정리
            foreach (var info in materialInfos)
            {
                if (info.transparentMaterials != null)
                {
                    foreach (var mat in info.transparentMaterials)
                    {
                        if (mat != null)
                        {
                            Destroy(mat);
                        }
                    }
                }
            }
        }
    }
}
