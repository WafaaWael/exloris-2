using Exloris.Runtime.Boat;
using Exloris.Runtime.Recovery;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Boat3PrefabGenerator
    {
        public const string StagePath = "Assets/Exloris/UI/Screens/Boat3.prefab";
        public const string HelpPath = "Assets/Exloris/UI/Overlays/Boat3Help.prefab";
        [MenuItem("Exloris/Boat 3/Generate Screen Prefabs")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens"); EnsureFolder("Assets/Exloris/UI/Overlays");
            Save<Boat3StageView>("Boat3", StagePath); Save<Boat3HelpView>("Boat3Help", HelpPath);
            AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(StagePath, ImportAssetOptions.ForceSynchronousImport); AssetDatabase.ImportAsset(HelpPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Generated Boat 3 screen and Help prefabs.");
        }
        private static void Save<T>(string name, string path) where T : Component { var root = new GameObject(name, typeof(T)); PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); }
        private static void EnsureFolder(string path) { var parts = path.Split('/'); var current = parts[0]; for (var i = 1; i < parts.Length; i++) { var next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
    }
}
