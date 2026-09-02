using Exloris.Runtime.Avatar;
using Exloris.Runtime.Recovery;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class AvatarPrefabGenerator
    {
        public const string SelectPath = "Assets/Exloris/UI/Screens/AvatarSelect.prefab";
        public const string PreviewPath = "Assets/Exloris/UI/Screens/AvatarPreview.prefab";
        public const string LeavePath = "Assets/Exloris/UI/Overlays/LeaveConfirmation.prefab";

        [MenuItem("Exloris/Avatar/Generate Screen Prefabs")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens");
            EnsureFolder("Assets/Exloris/UI/Overlays");
            Save<AvatarSelectView>("AvatarSelect", SelectPath);
            Save<AvatarPreviewView>("AvatarPreview", PreviewPath);
            Save<LeaveConfirmationView>("LeaveConfirmation", LeavePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Generated Avatar Select, Preview, and Leave prefabs.");
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
