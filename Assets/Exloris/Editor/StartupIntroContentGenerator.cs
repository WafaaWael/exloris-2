using System;
using Exloris.Runtime.Intro;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class StartupIntroContentGenerator
    {
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/StartupIntroContent.asset";
        private const string Imported = "Assets/Exloris/Content/Imported/";

        [MenuItem("Exloris/Startup Intro/Generate Approved Content Set")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/Resources/Content");
            var content = AssetDatabase.LoadAssetAtPath<StartupIntroContent>(ContentAssetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<StartupIntroContent>();
                AssetDatabase.CreateAsset(content, ContentAssetPath);
            }

            content.ConfigureApproved(
                Load<Sprite>("ui/ui-skip.png"),
                Load<Sprite>("ui/ui-resume.png"),
                Load<Sprite>("ui/ui-caption-backplate-opaque.png"),
                Load<Sprite>("gameplay/ibis-wings-open.png"),
                Load<Sprite>("gameplay/ibis-head-lift.png"),
                Load<Sprite>("gameplay/ibis-beak-right.png"),
                Load<Sprite>("gameplay/ibis-calm-settle.png"),
                Load<Sprite>("gameplay/ibis-static-right-reduced.png"),
                Load<Sprite>("visual-equivalents/ve-vo-intro-001.png"),
                Load<AudioClip>("voice/en/vo-intro-001-en.wav"));
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            Debug.Log("Generated approved Startup + Intro content set.");
        }

        private static T Load<T>(string runtimePath) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Imported + runtimePath);
            if (asset == null) throw new InvalidOperationException("Missing approved imported asset: " + runtimePath);
            return asset;
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
