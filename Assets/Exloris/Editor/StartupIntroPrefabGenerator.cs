using Exloris.Runtime.Intro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Editor
{
    public static class StartupIntroPrefabGenerator
    {
        public const string StartupPath = "Assets/Exloris/UI/Screens/StartupLoading.prefab";
        public const string IntroPath = "Assets/Exloris/UI/Screens/IntroVideo.prefab";

        [MenuItem("Exloris/Startup Intro/Generate Screen Prefabs")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens");
            GenerateStartup();
            GenerateIntro();
            AssetDatabase.SaveAssets();
            Debug.Log("Generated StartupLoading and IntroVideo prefabs.");
        }

        private static void GenerateStartup()
        {
            var root = new GameObject("StartupLoading");
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var background = new GameObject("TechnicalLoadingBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            var image = background.GetComponent<Image>();
            image.color = new Color32(12, 28, 42, 255);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            PrefabUtility.SaveAsPrefabAsset(root, StartupPath);
            Object.DestroyImmediate(root);
        }

        private static void GenerateIntro()
        {
            var root = new GameObject("IntroVideo", typeof(IntroStageView));
            PrefabUtility.SaveAsPrefabAsset(root, IntroPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
