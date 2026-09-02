using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Runtime.Completion;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class CompletionAdultReportContentGenerator
    {
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/CompletionAdultReportContent.asset";
        private const string ImportedRoot = "Assets/Exloris/Content/Imported/CompletionAdultReport";
        private const string SharedStage3Root = "Assets/Exloris/Content/Imported/Stage3";

        public static IReadOnlyList<string> ApprovedRuntimeNames => Rows
            .Select(row => row.Name).ToArray();

        [MenuItem("Exloris/Completion Adult Report/Import and Generate Approved Content")]
        public static void ImportAndGenerate()
        {
            EnsureFolder(ImportedRoot);
            ValidateSources();
            CopySources();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateImportedFileSet(Directory.GetFiles(ImportedRoot)
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                .Select(Path.GetFileName));
            ConfigureImporters();
            GenerateContentAsset();
            AssetDatabase.SaveAssets();
            Debug.Log("Imported and generated the approved Completion, Adult Gate, and Parent Report content.");
        }

        public static void ValidateImportedFileSet(IEnumerable<string> names)
        {
            var actual = names.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var expected = Rows.Select(row => row.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("Closing-route import folder must contain exactly the approved 27 files.");
        }

        public static void ValidateFrozenImportedBytes()
        {
            foreach (var row in Rows)
            {
                var path = PathFor(row.Name);
                if (!File.Exists(path) || !string.Equals(Hash(path), row.Hash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Closing-route imported bytes changed: " + row.Name);
            }
        }

        private static void ValidateSources()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in Rows)
            {
                var path = Path.Combine(workspace, row.Source);
                if (!File.Exists(path)) throw new InvalidOperationException("Missing closing-route source: " + row.Source);
                if (!string.Equals(Hash(path), row.Hash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Closing-route source hash changed: " + row.Source);
            }
        }

        private static void CopySources()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in Rows)
                File.Copy(Path.Combine(workspace, row.Source), Path.Combine(ImportedRoot, row.Name), true);
        }

        private static void ConfigureImporters()
        {
            foreach (var row in Rows)
            {
                var path = ImportedRoot + "/" + row.Name;
                if (row.Name.EndsWith(".png", StringComparison.Ordinal)) ConfigureTexture(path, row);
                else if (row.Name.EndsWith(".wav", StringComparison.Ordinal)) ConfigureAudio(path, row.Profile);
            }
        }

        private static void ConfigureTexture(string path, Row row)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Missing imported texture: " + row.Name);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spritePivot = row.Profile == "CHR" ? new Vector2(0.5f, 0.08f) : new Vector2(0.5f, 0.5f);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = importer.spritePivot;
            importer.SetTextureSettings(settings);
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = row.Profile != "BG3";
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            if (row.Profile == "UI-9") importer.spriteBorder = new Vector4(48f, 48f, 48f, 48f);
            var defaults = importer.GetDefaultPlatformTextureSettings();
            defaults.maxTextureSize = row.Profile == "BG3" ? 4096
                : row.Profile == "CHR" || row.Profile == "UI-9" ? 2048 : 1024;
            importer.SetPlatformTextureSettings(defaults);
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone",
                overridden = true,
                maxTextureSize = defaults.maxTextureSize,
                format = importer.alphaIsTransparency
                    ? TextureImporterFormat.DXT5Crunched
                    : TextureImporterFormat.DXT1Crunched,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100,
                crunchedCompression = true,
            });
            ConfigurePlatform(importer, "iPhone", row.Profile);
            ConfigurePlatform(importer, "Android", row.Profile);
            importer.SaveAndReimport();
        }

        private static void ConfigurePlatform(TextureImporter importer, string platform, string profile)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            settings.name = platform;
            settings.overridden = true;
            settings.maxTextureSize = profile == "BG3" ? 4096
                : profile == "CHR" || profile == "UI-9" ? 2048 : 1024;
            settings.format = profile == "BG3" || profile == "CHR" || profile == "VISUAL-EQUIVALENT"
                ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
            importer.SetPlatformTextureSettings(settings);
        }

        private static void ConfigureAudio(string path, string profile)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Missing imported audio: " + path);
            var voice = profile == "AUD-VO";
            importer.forceToMono = voice;
            importer.loadInBackground = !voice;
            var settings = importer.defaultSampleSettings;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.loadType = voice ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.Streaming;
            settings.preloadAudioData = voice;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void GenerateContentAsset()
        {
            EnsureFolder("Assets/Exloris/Resources/Content");
            var content = AssetDatabase.LoadAssetAtPath<CompletionAdultReportContent>(ContentAssetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<CompletionAdultReportContent>();
                AssetDatabase.CreateAsset(content, ContentAssetPath);
            }
            content.ConfigureApproved(
                SharedStage3Sprites("bg-15-village-after-4x3.png", "bg-15-village-after-16x9.png", "bg-15-village-after-24x10.png"),
                Sprites("bg-16-parent-4x3.png", "bg-16-parent-16x9.png", "bg-16-parent-24x10.png"),
                Sprites("chr-boy-completion-neutral.png", "chr-girl-completion-neutral.png"),
                Sprites("frame-card.png", "frame-slot-empty.png"),
                Sprite("ui-adult-hold.png"),
                Sprites("ui-adult-hold-progress-000.png", "ui-adult-hold-progress-025.png",
                    "ui-adult-hold-progress-050.png", "ui-adult-hold-progress-075.png",
                    "ui-adult-hold-progress-100.png"),
                Sprites("ui-back.png", "ui-close.png", "ui-confirm.png", "ui-home.png", "ui-restart.png", "ui-stay.png"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(PathFor("anim-completion.motion.json")),
                AssetDatabase.LoadAssetAtPath<AudioClip>(PathFor("vo-completion-en.wav")),
                Sprite("ve-vo-completion.png"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(PathFor("mus-completion-loop.wav")),
                AssetDatabase.LoadAssetAtPath<AudioClip>(SharedStage3Root + "/amb-village-loop.wav"));
            EditorUtility.SetDirty(content);
        }

        private static Sprite[] Sprites(params string[] names) => names.Select(Sprite).ToArray();
        private static Sprite[] SharedStage3Sprites(params string[] names) => names
            .Select(name => AssetDatabase.LoadAssetAtPath<Sprite>(SharedStage3Root + "/" + name)).ToArray();
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PathFor(name));
        private static string PathFor(string name) => ImportedRoot + "/" + name;

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
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

        private readonly struct Row
        {
            public Row(string name, string source, string hash, string profile)
            { Name = name; Source = source; Hash = hash; Profile = profile; }
            public string Name { get; }
            public string Source { get; }
            public string Hash { get; }
            public string Profile { get; }
        }

        private static readonly Row[] Rows =
        {
            new Row("frame-card.png", "assets/step-04-ui/runtime/frame-card.png", "0b65d482ba6c4456e06eb0bba0a6b8dbf8cacc34e5cf60d2a5076e1c8e523787", "UI-9"),
            new Row("frame-slot-empty.png", "assets/step-04-ui/runtime/frame-slot-empty.png", "f16eb64be6b00adc21fb78b34c774968e29329f8a112f9d32758fe06738089eb", "UI-9"),
            new Row("ui-adult-hold-progress-000.png", "assets/step-04-ui/runtime/ui-adult-hold-progress-000.png", "39d0dd22bd23e09917b4e4a6d8941edfe8a0784c27a49d1e9f03a156aa10f05f", "UI-OVERLAY"),
            new Row("ui-adult-hold-progress-025.png", "assets/step-04-ui/runtime/ui-adult-hold-progress-025.png", "e43edf97c90447ab797fe4431b0586c0c14c567cdcb7fa492606a2bf9e0534f4", "UI-OVERLAY"),
            new Row("ui-adult-hold-progress-050.png", "assets/step-04-ui/runtime/ui-adult-hold-progress-050.png", "161e89225b4e69945cb09e4ed2f07957fdb5adcd5fd5f3a2d5d1c692cf1d88fc", "UI-OVERLAY"),
            new Row("ui-adult-hold-progress-075.png", "assets/step-04-ui/runtime/ui-adult-hold-progress-075.png", "e7f3c7eb93e76ef48dbd43f3bc2fd225cbe6ff53e8e872a47aab533ae584d6a1", "UI-OVERLAY"),
            new Row("ui-adult-hold-progress-100.png", "assets/step-04-ui/runtime/ui-adult-hold-progress-100.png", "9a004304a7a190503a18dba4eed4c8b57ddd73a73180c7bb63407b5c697b68d7", "UI-OVERLAY"),
            new Row("ui-adult-hold.png", "assets/step-04-ui/runtime/ui-adult-hold.png", "e88ab4e7fc0c3b74f7dd28a47ac91532b0fb1b7c96ca820cb65c6dc605058991", "UI-I"),
            new Row("ui-back.png", "assets/step-04-ui/runtime/ui-back.png", "ce7d9212c18ed0b72501f5b867483d4afcb201f88536455c7a897454e115eac7", "UI-I"),
            new Row("ui-close.png", "assets/step-04-ui/runtime/ui-close.png", "6b85ce266c40667e5f2c3b1dd947491153b20bb53e5113fde5a38acca566eac9", "UI-I"),
            new Row("ui-confirm.png", "assets/step-04-ui/runtime/ui-confirm.png", "cea2f5f945c515dcdccf4cd4f3601e0a2f2ce6dc6d6c66d5eb156821b8384d1f", "UI-I"),
            new Row("ui-home.png", "assets/step-04-ui/runtime/ui-home.png", "8616a22329052c902a8d39699fc1f967e4288374f40b456f58638e1b60109892", "UI-I"),
            new Row("ui-restart.png", "assets/step-04-ui/runtime/ui-restart.png", "90afd9d454eb7012e81f42ded578bbad2d32ca9ffd57bea4b5dd364536a04c42", "UI-I"),
            new Row("ui-stay.png", "assets/step-04-ui/runtime/ui-stay.png", "7796c74a745e9d4475ea8747a9f81c296de7a72fe1a4b262df1fdac24ce9026b", "UI-I"),
            new Row("chr-boy-completion-neutral.png", "assets/step-05-characters/runtime/chr-boy-completion-neutral.png", "b63a5c765f2f4c0463dbbf92c602f53674e63d5a6c4dd3bb0c3d56359fa70cf2", "CHR"),
            new Row("chr-girl-completion-neutral.png", "assets/step-05-characters/runtime/chr-girl-completion-neutral.png", "b5708b78cc0e351dfda6070189f8b3879e0464ca65979b0f5c23a196d51e7ffb", "CHR"),
            new Row("bg-15-village-after-4x3.png", "assets/step-06-environments/runtime/bg-15-village-after-4x3.png", "031c9750353d1ae58548e0a1c5c09f330dbb765f5d1eff40326754cc94696d12", "BG3"),
            new Row("bg-15-village-after-16x9.png", "assets/step-06-environments/runtime/bg-15-village-after-16x9.png", "4ccfac190334047d19173b67654ea076d8acbdcc720583887ccde117671b57a6", "BG3"),
            new Row("bg-15-village-after-24x10.png", "assets/step-06-environments/runtime/bg-15-village-after-24x10.png", "8c435ea3e59e193f5bf9a333c20d882d6b8007483a7f5c2b810297dd20fd8591", "BG3"),
            new Row("bg-16-parent-4x3.png", "assets/step-06-environments/runtime/bg-16-parent-4x3.png", "001fd34dc19e5387337d43e2ddab253eece850630cbfad4267fe93f2424c5af2", "BG3"),
            new Row("bg-16-parent-16x9.png", "assets/step-06-environments/runtime/bg-16-parent-16x9.png", "a4407204ec7aacba438f902753f0fe4ab850bdf2e6b2840507eb1a79044e22ba", "BG3"),
            new Row("bg-16-parent-24x10.png", "assets/step-06-environments/runtime/bg-16-parent-24x10.png", "de2ef58200408308aef7c774b40dfe175437106998bc48268fc1e4ebc741f92b", "BG3"),
            new Row("anim-completion.motion.json", "production/step-09-animation/manifests/anim-completion.motion.json", "4b82062ef3712de5541f0d288dd3ba084257fa0976f04d2aaa741ceed0298b46", "MOT"),
            new Row("vo-completion-en.wav", "production/step-10-voice-captions/voice/en/vo-completion-en.wav", "888661837e49c129b4d5762fc2f3aa0215a9fc0663a343795e9fe6469563ec03", "AUD-VO"),
            new Row("ve-vo-completion.png", "production/step-10-voice-captions/visual-equivalents/ve-vo-completion.png", "99ad026ff1a6c771499beb032604debfe721b4fbc241198ac7e7506e833dbbc5", "VISUAL-EQUIVALENT"),
            new Row("mus-completion-loop.wav", "production/step-11-audio/music/mus-completion-loop.wav", "4061975114467af7f68abd7b1fae1517cd6fdbce726a30a9f5d8b187040203b1", "AUD-MUS"),
            new Row("amb-village-loop.wav", "production/step-11-audio/ambience/amb-village-loop.wav", "a69ea1382639dd93543de903a9e6f27af61953efcc2b69bc78ac1266fbb667e6", "AUD-AMB"),
        };
    }
}
