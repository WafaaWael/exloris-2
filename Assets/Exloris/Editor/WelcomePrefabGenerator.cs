using Exloris.Runtime.Welcome;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class WelcomePrefabGenerator
    {
        public const string WelcomePath = "Assets/Exloris/UI/Screens/Welcome.prefab";
        public const string SessionChoicePath = "Assets/Exloris/UI/Overlays/SessionChoice.prefab";

        [MenuItem("Exloris/Welcome/Generate Screen Prefabs")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens");
            EnsureFolder("Assets/Exloris/UI/Overlays");
            Save<WelcomeStageView>("Welcome", WelcomePath);
            Save<SessionChoiceView>("SessionChoice", SessionChoicePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Generated Welcome and SessionChoice prefabs.");
        }

        private static void Save<T>(string name, string path) where T : Component
        {
            var root = new GameObject(name, typeof(T));
            PrefabUtility.SaveAsPrefabAsset(root, path);
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
