using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage3;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Stage3PrefabGenerator
    {
        public const string StagePath = "Assets/Exloris/UI/Screens/Stage3.prefab";
        public const string HelpPath = "Assets/Exloris/UI/Overlays/Stage3Help.prefab";

        [MenuItem("Exloris/Stage 3/Generate Screen Prefabs")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/UI/Screens"); EnsureFolder("Assets/Exloris/UI/Overlays");
            var stage = new GameObject("Stage3", typeof(Stage3StageView));
            PrefabUtility.SaveAsPrefabAsset(stage, StagePath); Object.DestroyImmediate(stage);
            var help = new GameObject("Stage3Help", typeof(Stage3HelpView));
            PrefabUtility.SaveAsPrefabAsset(help, HelpPath); Object.DestroyImmediate(help);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(StagePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(HelpPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(StagePath)?.GetComponent<Stage3StageView>() == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(HelpPath)?.GetComponent<Stage3HelpView>() == null)
                throw new System.InvalidOperationException("Stage 3 prefab generation failed.");
            Debug.Log("Generated Stage 3 screen and Help prefabs.");
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
