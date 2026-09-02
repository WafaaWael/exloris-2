using Exloris.Runtime.Adult;
using Exloris.Runtime.Completion;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class CompletionAdultReportPrefabGenerator
    {
        public const string CompletionPath = "Assets/Exloris/UI/Screens/Completion.prefab";
        public const string AdultGatePath = "Assets/Exloris/UI/Screens/AdultGate.prefab";
        public const string ParentReportPath = "Assets/Exloris/UI/Screens/ParentReport.prefab";

        [MenuItem("Exloris/Completion Adult Report/Generate Screen Prefabs")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens");
            Save<CompletionStageView>("Completion", CompletionPath);
            Save<AdultGateView>("AdultGate", AdultGatePath);
            Save<ParentReportView>("ParentReport", ParentReportPath);
            AssetDatabase.SaveAssets();
            Validate<CompletionStageView>(CompletionPath);
            Validate<AdultGateView>(AdultGatePath);
            Validate<ParentReportView>(ParentReportPath);
            Debug.Log("Generated Completion, Adult Gate, and Parent Report prefabs.");
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
                throw new System.InvalidOperationException("Closing-route prefab generation failed: " + path);
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
