using Exloris.Runtime.Stage2;
using Exloris.Runtime.Recovery;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Stage2PrefabGenerator
    {
        public const string StagePath = "Assets/Exloris/UI/Screens/Stage2.prefab";
        public const string HelpPath = "Assets/Exloris/UI/Overlays/Stage2Help.prefab";

        [MenuItem("Exloris/Stage 2/Generate Screen Prefab")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens");
            EnsureFolder("Assets/Exloris/UI/Overlays");
            var root = new GameObject("Stage2", typeof(Stage2StageView));
            PrefabUtility.SaveAsPrefabAsset(root, StagePath);
            Object.DestroyImmediate(root);
            var help = new GameObject("Stage2Help", typeof(Stage2HelpView));
            PrefabUtility.SaveAsPrefabAsset(help, HelpPath);
            Object.DestroyImmediate(help);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(StagePath, ImportAssetOptions.ForceSynchronousImport);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StagePath);
            if (prefab == null || prefab.GetComponent<Stage2StageView>() == null)
                throw new System.InvalidOperationException("Stage 2 prefab generation failed.");
            var helpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HelpPath);
            if (helpPrefab == null || helpPrefab.GetComponent<Stage2HelpView>() == null)
                throw new System.InvalidOperationException("Stage 2 Help prefab generation failed.");
            Debug.Log("Generated Stage 2 screen prefab.");
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
