using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Exloris.Editor
{
    public static class FoundationFontGenerator
    {
        private const string ContentFolder = "Assets/Exloris/Content/Fonts";
        private const string ResourcesFolder = "Assets/Exloris/Resources/Fonts";
        private const string PrimarySource = ContentFolder + "/NotoSans-SemiBold.ttf";
        private const string MathSource = ContentFolder + "/NotoSansMath-Regular.ttf";
        private const string PrimaryAsset = ResourcesFolder + "/NotoSans-SemiBold SDF.asset";
        private const string MathAsset = ResourcesFolder + "/NotoSansMath-Regular SDF.asset";

        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Exloris/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/Exloris", "Resources");
            }
            EnsureTmpEssentialResources();
            EnsureTmpSettings();
            EnsureFolder("Assets/Exloris/Content", "Fonts");
            EnsureFolder("Assets/Exloris/Resources", "Fonts");

            CopyApprovedSource("NotoSans-SemiBold.ttf", PrimarySource);
            CopyApprovedSource("NotoSansMath-Regular.ttf", MathSource);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var primary = CreateFontAsset(PrimarySource, PrimaryAsset);
            var math = CreateFontAsset(MathSource, MathAsset);
            if (!math.TryAddCharacters("⟦⟧", out var missing) || !string.IsNullOrEmpty(missing))
            {
                throw new InvalidOperationException($"Noto Sans Math is missing pseudolocalization brackets: {missing}");
            }

            primary.fallbackFontAssetTable = new List<TMP_FontAsset> { math };
            EditorUtility.SetDirty(primary);
            EditorUtility.SetDirty(math);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureTmpEssentialResources()
        {
            if (Shader.Find("TextMeshPro/Distance Field") != null) return;

            const string localSettings = "Assets/Exloris/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(localSettings) != null)
            {
                AssetDatabase.DeleteAsset(localSettings);
            }

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
            if (package == null) throw new InvalidOperationException("The bundled TextMeshPro package is unavailable.");
            var essentials = Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(essentials, interactive: false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (Shader.Find("TextMeshPro/Distance Field") == null)
            {
                throw new InvalidOperationException("TextMeshPro essential shaders did not import.");
            }
        }

        private static void EnsureTmpSettings()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;
            const string settingsPath = "Assets/Exloris/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath) != null) return;

            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("assetVersion").stringValue = "2";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(settings, settingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static TMP_FontAsset CreateFontAsset(string sourcePath, string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null)
            {
                throw new FileNotFoundException("Approved font source did not import.", sourcePath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: false);
            if (fontAsset == null)
            {
                throw new InvalidOperationException($"Could not create TMP font asset from {sourcePath}.");
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.material != null) AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            foreach (var texture in fontAsset.atlasTextures)
            {
                if (texture != null) AssetDatabase.AddObjectToAsset(texture, fontAsset);
            }

            return fontAsset;
        }

        private static void CopyApprovedSource(string filename, string destinationAssetPath)
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var source = Path.Combine(workspaceRoot, "assets", "step-04-ui", "source", filename);
            if (!File.Exists(source)) throw new FileNotFoundException("Approved font source is missing.", source);
            var destination = Path.GetFullPath(Path.Combine(Application.dataPath, "..", destinationAssetPath));
            File.Copy(source, destination, overwrite: true);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
