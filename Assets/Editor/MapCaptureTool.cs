#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace MiniExtractionShooter.Editor
{
    public class MapCaptureTool : EditorWindow
    {
        private float cameraHeight = 100f;
        private float orthographicSize = 50f;
        private int resolution = 1024;
        private Vector2 mapCenter = Vector2.zero;
        private Color backgroundColor = Color.black;
        private LayerMask cullingMask = -1;

        [MenuItem("Tools/Map Capture Tool")]
        public static void ShowWindow()
        {
            GetWindow<MapCaptureTool>("Map Capture Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("맵 스크린샷 촬영 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("카메라 설정", EditorStyles.boldLabel);
            mapCenter = EditorGUILayout.Vector2Field("맵 중심 (X, Z)", mapCenter);
            cameraHeight = EditorGUILayout.FloatField("카메라 높이 (Y)", cameraHeight);
            orthographicSize = EditorGUILayout.FloatField("Orthographic Size", orthographicSize);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("출력 설정", EditorStyles.boldLabel);
            resolution = EditorGUILayout.IntPopup("해상도", resolution,
                new string[] { "512x512", "1024x1024", "2048x2048", "4096x4096" },
                new int[] { 512, 1024, 2048, 4096 });
            backgroundColor = EditorGUILayout.ColorField("배경색", backgroundColor);

            EditorGUILayout.Space(5);
            cullingMask = EditorGUILayoutExtensions.LayerMaskField("Culling Mask", cullingMask);

            EditorGUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "1. 맵 중심과 카메라 높이를 설정하세요\n" +
                "2. Orthographic Size로 보이는 범위를 조절하세요\n" +
                "   (맵이 -50~50 범위라면 Size = 50)\n" +
                "3. Culling Mask로 맵 레이어만 선택하세요\n" +
                "4. '맵 촬영' 버튼을 클릭하세요",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("미리보기 카메라 생성", GUILayout.Height(30)))
            {
                CreatePreviewCamera();
            }

            if (GUILayout.Button("미리보기 카메라 삭제", GUILayout.Height(30)))
            {
                DestroyPreviewCamera();
            }

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("맵 촬영", GUILayout.Height(40)))
            {
                CaptureMap();
            }
            GUI.backgroundColor = Color.white;
        }

        private void CreatePreviewCamera()
        {
            DestroyPreviewCamera();

            GameObject camObj = new GameObject("__MapPreviewCamera__");
            Camera cam = camObj.AddComponent<Camera>();

            camObj.transform.position = new Vector3(mapCenter.x, cameraHeight, mapCenter.y);
            camObj.transform.rotation = Quaternion.Euler(90, 0, 0);

            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.cullingMask = cullingMask;

            Selection.activeGameObject = camObj;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("미리보기 카메라가 생성되었습니다. Scene 뷰에서 확인하세요.");
        }

        private void DestroyPreviewCamera()
        {
            GameObject existing = GameObject.Find("__MapPreviewCamera__");
            if (existing != null)
            {
                DestroyImmediate(existing);
                Debug.Log("미리보기 카메라가 삭제되었습니다.");
            }
        }

        private void CaptureMap()
        {
            string folderPath = "Assets/09.UI";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = $"{folderPath}/MapImage.png";

            GameObject camObj = new GameObject("__TempMapCamera__");
            Camera cam = camObj.AddComponent<Camera>();

            camObj.transform.position = new Vector3(mapCenter.x, cameraHeight, mapCenter.y);
            camObj.transform.rotation = Quaternion.Euler(90, 0, 0);

            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.cullingMask = cullingMask;

            RenderTexture rt = new RenderTexture(resolution, resolution, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            RenderTexture.active = null;
            cam.targetTexture = null;
            DestroyImmediate(rt);
            DestroyImmediate(tex);
            DestroyImmediate(camObj);

            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.maxTextureSize = resolution;
                importer.SaveAndReimport();
            }

            Debug.Log($"맵 이미지가 저장되었습니다: {filePath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(filePath));
        }
    }

    public static class EditorGUILayoutExtensions
    {
        public static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            var layerNumbers = new int[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                layerNumbers[i] = LayerMask.NameToLayer(layers[i]);
            }

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Length; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) != 0)
                {
                    maskWithoutEmpty |= (1 << i);
                }
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layerNumbers.Length; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) != 0)
                {
                    mask |= (1 << layerNumbers[i]);
                }
            }

            layerMask.value = mask;
            return layerMask;
        }
    }
}
#endif
