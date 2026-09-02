using System;
using System.IO;
using System.Linq;
using Exloris.Editor.Build;
using Exloris.Runtime.Stage3;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Stage3Build
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
                locationPathName = Path.Combine(workspaceRoot, "builds", "stage-3", "macos", "Exloris2.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CompressWithLz4HC,
            };
        }

        public static bool HasApprovedContent(Stage3Content content)
        {
            if (content == null) return false;
            var references = content.AllReferences;
            return references.Length == 39
                && references.All(item => item != null)
                && references.Distinct().Count() == 39;
        }

        public static void BuildMac()
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            EnsureMinimumFreeSpace(workspaceRoot);
            StartupIntroBuild.ValidateRuntimeMediaSet(Path.Combine(
                Application.dataPath, "StreamingAssets", "Exloris", "Intro"));
            var content = AssetDatabase.LoadAssetAtPath<Stage3Content>(Stage3ContentGenerator.ContentAssetPath);
            if (!HasApprovedContent(content))
                throw new InvalidOperationException("Approved Stage 3 content is incomplete.");
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
                PlayerSettings.SetManagedStrippingLevel(
                    NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
                PlayerSettings.stripEngineCode = true;
                var options = CreateMacOptions(workspaceRoot);
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName)
                    ?? throw new InvalidOperationException("Stage 3 output directory is unavailable."));
                if (Directory.Exists(options.locationPathName)) Directory.Delete(options.locationPathName, true);
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Stage 3 macOS build failed: {report.summary.result} with {report.summary.totalErrors} error(s).");

                var removedDebugBytes = RemoveManagedDebugSymbols(options.locationPathName);
                ReleaseEvidenceBuilder.ResealAdHoc(options.locationPathName);
                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);
                var installedBytes = Directory.GetFiles(options.locationPathName, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                if (installedBytes >= MaximumInstalledBytes)
                    throw new InvalidOperationException(
                        $"Stage 3 build uses {installedBytes} bytes, not below {MaximumInstalledBytes}.");
                Debug.Log($"Stage 3 macOS build succeeded at {options.locationPathName}; "
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

        public static void EnsureMinimumFreeSpace(string workspaceRoot)
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
                throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
            var fullPath = Path.GetFullPath(workspaceRoot);
            var drive = DriveInfo.GetDrives()
                .Where(candidate => fullPath.StartsWith(candidate.RootDirectory.FullName, StringComparison.Ordinal))
                .OrderByDescending(candidate => candidate.RootDirectory.FullName.Length)
                .FirstOrDefault() ?? new DriveInfo(Path.GetPathRoot(fullPath) ?? fullPath);
            if (drive.AvailableFreeSpace < MinimumFreeBytes)
                throw new InvalidOperationException(
                    $"Stage 3 build requires at least {MinimumFreeBytes} free bytes; only {drive.AvailableFreeSpace} are available.");
        }

        public static long RemoveManagedDebugSymbols(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath))
                throw new ArgumentException("App path is required.", nameof(appPath));
            var managedPath = Path.Combine(appPath, "Contents", "Resources", "Data", "Managed");
            if (!Directory.Exists(managedPath)) return 0L;
            long removed = 0L;
            foreach (var path in Directory.GetFiles(managedPath, "*.pdb", SearchOption.AllDirectories))
            {
                removed += new FileInfo(path).Length;
                File.Delete(path);
            }
            return removed;
        }
    }
}
