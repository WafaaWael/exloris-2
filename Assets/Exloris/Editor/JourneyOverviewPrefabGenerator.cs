using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class JourneyOverviewPrefabGenerator
    {
        public const string OverviewPath = "Assets/Exloris/UI/Screens/JourneyOverview.prefab";
        public const string PausePath = "Assets/Exloris/UI/Overlays/PauseOverlay.prefab";
        public const string HelpPath = "Assets/Exloris/UI/Overlays/JourneyOverviewPauseHelp.prefab";

        [MenuItem("Exloris/Journey Overview/Generate Screen Prefab")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens");
            EnsureFolder("Assets/Exloris/UI/Overlays");
            Save<JourneyOverviewStageView>("JourneyOverview", OverviewPath);
            Save<PauseOverlayView>("PauseOverlay", PausePath);
            Save<JourneyOverviewPauseHelpView>("JourneyOverviewPauseHelp", HelpPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(OverviewPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(PausePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(HelpPath, ImportAssetOptions.ForceSynchronousImport);
            Validate<JourneyOverviewStageView>(OverviewPath);
            Validate<PauseOverlayView>(PausePath);
            Validate<JourneyOverviewPauseHelpView>(HelpPath);
            Debug.Log("Generated Journey Overview, Pause, and Pause Help prefabs.");
        }

        private static void Save<T>(string name, string path) where T : Component
        {
            var root = new GameObject(name, typeof(T));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void Validate<T>(string path) where T : Component
        {
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (saved == null || saved.GetComponent<T>() == null)
                throw new System.InvalidOperationException("Journey prefab generation failed: " + path);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
