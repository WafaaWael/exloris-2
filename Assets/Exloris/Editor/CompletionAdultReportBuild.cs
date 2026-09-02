using System;
using System.IO;
using System.Linq;
using Exloris.Editor.Build;
using Exloris.Runtime.Completion;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Exloris.Editor
{
    public static class CompletionAdultReportBuild
    {
        public const long MinimumFreeBytes = 15L * 1024L * 1024L * 1024L;
        public const long MaximumInstalledBytes = 200_000_000L;
        public const string MacEvidenceArchitecture = "ARM64";

        private static readonly string[] Scenes =
        {
            "Assets/Exloris/Scenes/Bootstrap.unity",
            "Assets/Exloris/Scenes/FoundationHarness.unity",
        };

        public static BuildPlayerOptions CreateMacOptions(string workspaceRoot)
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
                throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
            return new BuildPlayerOptions
            {
                scenes = (string[])Scenes.Clone(),
                locationPathName = Path.Combine(workspaceRoot, "builds", "completion-adult-report", "macos", "Exloris2.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CompressWithLz4HC,
            };
        }

        public static bool HasApprovedContent(CompletionAdultReportContent content)
        {
            if (content == null) return false;
            var references = content.AllReferences;
            return references.Length == 27 && references.All(item => item != null)
                && references.Distinct().Count() == 27;
        }

        public static void BuildMac()
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            Stage3Build.EnsureMinimumFreeSpace(workspaceRoot);
            StartupIntroBuild.ValidateRuntimeMediaSet(Path.Combine(
                Application.dataPath, "StreamingAssets", "Exloris", "Intro"));
            var content = AssetDatabase.LoadAssetAtPath<CompletionAdultReportContent>(
                CompletionAdultReportContentGenerator.ContentAssetPath);
            if (!HasApprovedContent(content))
                throw new InvalidOperationException("Approved Completion/Adult/Report content is incomplete.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
                throw new InvalidOperationException("Could not switch to the macOS build target.");

            FoundationBuild.ConfigurePlatformSettings();
            var previousRunInBackground = PlayerSettings.runInBackground;
            var previousArchitecture = EditorUserBuildSettings.GetPlatformSettings("Standalone", "Architecture");
            var previousStripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone);
            var previousStripEngineCode = PlayerSettings.stripEngineCode;
            try
            {
                PlayerSettings.runInBackground = true;
                EditorUserBuildSettings.SetPlatformSettings("Standalone", "Architecture", MacEvidenceArchitecture);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
                PlayerSettings.stripEngineCode = true;
                var options = CreateMacOptions(workspaceRoot);
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName)
                    ?? throw new InvalidOperationException("Closing route output directory is unavailable."));
                if (Directory.Exists(options.locationPathName)) Directory.Delete(options.locationPathName, true);
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Closing route macOS build failed: {report.summary.result} with {report.summary.totalErrors} error(s).");
                var removedDebugBytes = Stage3Build.RemoveManagedDebugSymbols(options.locationPathName);
                ReleaseEvidenceBuilder.ResealAdHoc(options.locationPathName);
                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);
                var installedBytes = Directory.GetFiles(options.locationPathName, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                if (installedBytes >= MaximumInstalledBytes)
                    throw new InvalidOperationException(
                        $"Closing route build uses {installedBytes} bytes, not below {MaximumInstalledBytes}.");
                Debug.Log($"Closing route macOS build succeeded at {options.locationPathName}; "
                    + $"bytes={installedBytes}; removed_managed_debug_bytes={removedDebugBytes}.");
            }
            finally
            {
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, previousStripping);
                PlayerSettings.stripEngineCode = previousStripEngineCode;
                EditorUserBuildSettings.SetPlatformSettings("Standalone", "Architecture", previousArchitecture);
                PlayerSettings.runInBackground = previousRunInBackground;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
