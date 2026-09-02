using Exloris.Runtime.Boat;
using Exloris.Runtime.Recovery;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Boat1PrefabGenerator
    {
        public const string StagePath = "Assets/Exloris/UI/Screens/Boat1.prefab";
        public const string HelpPath = "Assets/Exloris/UI/Overlays/Boat1Help.prefab";
        [MenuItem("Exloris/Boat 1/Generate Screen Prefabs")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens"); EnsureFolder("Assets/Exloris/UI/Overlays");
            Save<Boat1StageView>("Boat1", StagePath); Save<Boat1HelpView>("Boat1Help", HelpPath);
            AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(StagePath, ImportAssetOptions.ForceSynchronousImport); AssetDatabase.ImportAsset(HelpPath, ImportAssetOptions.ForceSynchronousImport);
            Validate<Boat1StageView>(StagePath); Validate<Boat1HelpView>(HelpPath); Debug.Log("Generated Boat 1 screen and Help prefabs.");
        }
        private static void Save<T>(string name, string path) where T : Component { var root = new GameObject(name, typeof(T)); PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); }
        private static void Validate<T>(string path) where T : Component { var root = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (root == null || root.GetComponent<T>() == null) throw new System.InvalidOperationException("Boat 1 prefab generation failed: " + path); }
        private static void EnsureFolder(string path) { var parts = path.Split('/'); var current = parts[0]; for (var i = 1; i < parts.Length; i++) { var next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
    }
}
