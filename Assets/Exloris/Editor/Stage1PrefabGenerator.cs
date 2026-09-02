using Exloris.Runtime.Stage1;
using Exloris.Runtime.Recovery;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Stage1PrefabGenerator
    {
        public const string StagePath = "Assets/Exloris/UI/Screens/Stage1.prefab";
        public const string HelpPath = "Assets/Exloris/UI/Overlays/Stage1Help.prefab";

        [MenuItem("Exloris/Stage 1/Generate Screen Prefab")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens");
            EnsureFolder("Assets/Exloris/UI/Overlays");
            Save<Stage1StageView>("Stage1", StagePath);
            Save<Stage1HelpView>("Stage1Help", HelpPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(StagePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(HelpPath, ImportAssetOptions.ForceSynchronousImport);
            Validate<Stage1StageView>(StagePath);
            Validate<Stage1HelpView>(HelpPath);
            Debug.Log("Generated Stage 1 screen and Help prefabs.");
        }

        private static void Save<T>(string name, string path) where T : Component
        {
            var root = new GameObject(name, typeof(T));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void Validate<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<T>() == null)
                throw new System.InvalidOperationException("Stage 1 prefab generation failed: " + path);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/'); var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
