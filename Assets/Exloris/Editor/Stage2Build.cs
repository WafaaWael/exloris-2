using System;
using System.IO;
using System.Linq;
using Exloris.Runtime.Stage2;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Stage2Build
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
                locationPathName = Path.Combine(
                    workspaceRoot, "builds", "stage-2", "macos", "Exloris2.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CompressWithLz4HC,
            };
        }

        public static bool HasApprovedContent(Stage2Content content)
        {
            if (content == null) return false;
            var references = content.AllReferences;
            return references.Length == 46
                && references.All(item => item != null)
                && references.Distinct().Count() == 46;
        }

        public static void BuildMac()
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            EnsureMinimumFreeSpace(workspaceRoot);
            StartupIntroBuild.ValidateRuntimeMediaSet(Path.Combine(
                Application.dataPath, "StreamingAssets", "Exloris", "Intro"));
            var content = AssetDatabase.LoadAssetAtPath<Stage2Content>(
                Stage2ContentGenerator.ContentAssetPath);
            if (!HasApprovedContent(content))
                throw new InvalidOperationException("Approved Stage 2 content is incomplete.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
                throw new InvalidOperationException("Could not switch to the macOS build target.");

            FoundationBuild.ConfigurePlatformSettings();
            var previousRunInBackground = PlayerSettings.runInBackground;
            var previousArchitecture = EditorUserBuildSettings.GetPlatformSettings(
                "Standalone", "Architecture");
            try
            {
                PlayerSettings.runInBackground = true;
                EditorUserBuildSettings.SetPlatformSettings(
                    "Standalone", "Architecture", MacEvidenceArchitecture);
                var options = CreateMacOptions(workspaceRoot);
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName)
                    ?? throw new InvalidOperationException("Stage 2 output directory is unavailable."));
                if (Directory.Exists(options.locationPathName))
                    Directory.Delete(options.locationPathName, true);
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Stage 2 macOS build failed: {report.summary.result} "
                        + $"with {report.summary.totalErrors} error(s).");

                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);
                var installedBytes = Directory.GetFiles(
                        options.locationPathName, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                if (installedBytes >= MaximumInstalledBytes)
                    throw new InvalidOperationException(
                        $"Stage 2 build uses {installedBytes} bytes, not below {MaximumInstalledBytes}.");
                Debug.Log($"Stage 2 macOS build succeeded at {options.locationPathName}; bytes={installedBytes}.");
            }
            finally
            {
                EditorUserBuildSettings.SetPlatformSettings(
                    "Standalone", "Architecture", previousArchitecture);
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
                .Where(candidate => fullPath.StartsWith(
                    candidate.RootDirectory.FullName, StringComparison.Ordinal))
                .OrderByDescending(candidate => candidate.RootDirectory.FullName.Length)
                .FirstOrDefault() ?? new DriveInfo(Path.GetPathRoot(fullPath) ?? fullPath);
            if (drive.AvailableFreeSpace < MinimumFreeBytes)
                throw new InvalidOperationException(
                    $"Stage 2 build requires at least {MinimumFreeBytes} free bytes; "
                    + $"only {drive.AvailableFreeSpace} are available.");
        }
    }
}
