using System;
using Exloris.Runtime.Welcome;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class WelcomeContentGenerator
    {
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/WelcomeContent.asset";
        private const string Imported = "Assets/Exloris/Content/Imported/";

        [MenuItem("Exloris/Welcome/Generate Approved Content Set")]
        public static void Generate()
        {
            EnsureFolder("Assets/Exloris/Resources/Content");
            var content = AssetDatabase.LoadAssetAtPath<WelcomeContent>(ContentAssetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<WelcomeContent>();
                AssetDatabase.CreateAsset(content, ContentAssetPath);
            }

            content.ConfigureApproved(
                Load<Sprite>("backgrounds/bg-01-launch-4x3.png"),
                Load<Sprite>("backgrounds/bg-01-launch-16x9.png"),
                Load<Sprite>("backgrounds/bg-01-launch-24x10.png"),
                Load<Sprite>("ui/ui-play.png"),
                Load<Sprite>("ui/ui-adult-hold.png"),
                Load<Sprite>("ui/ui-continue.png"),
                Load<Sprite>("ui/ui-restart.png"),
                Load<Sprite>("ui/ui-back.png"),
                Load<Sprite>("ui/ui-resume.png"),
                Load<Sprite>("ui/ui-retry.png"),
                Load<Sprite>("ui/ui-home.png"),
                Load<Sprite>("ui/ui-sound-on.png"),
                Load<Sprite>("ui/ui-sound-off.png"),
                Load<Sprite>("ui/ui-captions.png"),
                Load<Sprite>("ui/ui-captions-off.png"),
                Load<Sprite>("ui/ui-reduced-motion.png"),
                Load<Sprite>("ui/ui-reduced-motion-off.png"),
                Load<Sprite>("ui/ui-caption-backplate-opaque.png"),
                Load<Sprite>("ui/ui-pressed-overlay.png"),
                Load<AudioClip>("voice/en/vo-welcome-001-en.wav"),
                Load<AudioClip>("voice/en/vo-welcome-session-en.wav"),
                Load<Sprite>("visual-equivalents/ve-vo-welcome-001.png"),
                Load<Sprite>("visual-equivalents/ve-vo-welcome-session.png"),
                Load<AudioClip>("music/mus-main-journey-loop.wav"),
                Load<AudioClip>("ambience/amb-nile-day-loop.wav"));
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            Debug.Log("Generated approved Welcome content set.");
        }

        private static T Load<T>(string runtimePath) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Imported + runtimePath);
            if (asset == null) throw new InvalidOperationException("Missing approved Welcome asset: " + runtimePath);
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
