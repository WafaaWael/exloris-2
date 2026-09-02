using System;
using System.IO;
using System.Linq;
using Exloris.Runtime.Overview;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Exloris.Editor
{
    public static class JourneyOverviewBuild
    {
        public const long MinimumFreeBytes = 15L * 1024L * 1024L * 1024L;
        public const long MaximumInstalledBytes = 200_000_000L;
        public const string MacEvidenceArchitecture = "ARM64";

        private static readonly string[] Scenes =
        {
            "Assets/Exloris/Scenes/Bootstrap.unity",
            "Assets/Exloris/Scenes/FoundationHarness.unity"
        };

        public static BuildPlayerOptions CreateMacOptions(string workspaceRoot)
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
                throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));

            return new BuildPlayerOptions
            {
                scenes = (string[])Scenes.Clone(),
                locationPathName = Path.Combine(
                    workspaceRoot, "builds", "journey-overview", "macos", "Exloris2.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };
        }

        public static void BuildMac()
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            EnsureMinimumFreeSpace(workspaceRoot);
            StartupIntroBuild.ValidateRuntimeMediaSet(Path.Combine(
                Application.dataPath, "StreamingAssets", "Exloris", "Intro"));
            ValidateJourneyOverviewContent();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(
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
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName) ??
                                          throw new InvalidOperationException(
                                              "Journey Overview output directory is unavailable."));
                if (Directory.Exists(options.locationPathName))
                    Directory.Delete(options.locationPathName, recursive: true);
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Journey Overview macOS build failed: {report.summary.result} " +
                        $"with {report.summary.totalErrors} error(s).");

                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);
                var installedBytes = Directory.GetFiles(
                        options.locationPathName, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                if (installedBytes >= MaximumInstalledBytes)
                    throw new InvalidOperationException(
                        $"Journey Overview build uses {installedBytes} bytes, " +
                        $"not below {MaximumInstalledBytes}.");
                Debug.Log(
                    $"Journey Overview macOS build succeeded at {options.locationPathName}; " +
                    $"bytes={installedBytes}.");
            }
            finally
            {
                EditorUserBuildSettings.SetPlatformSettings(
                    "Standalone", "Architecture", previousArchitecture);
                PlayerSettings.runInBackground = previousRunInBackground;
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
                    $"Journey Overview build requires at least {MinimumFreeBytes} free bytes; " +
                    $"only {drive.AvailableFreeSpace} are available.");
        }

        private static void ValidateJourneyOverviewContent()
        {
            var content = AssetDatabase.LoadAssetAtPath<JourneyOverviewContent>(
                "Assets/Exloris/Resources/Content/JourneyOverviewContent.asset");
            if (content == null || content.AllReferences.Length != 33 ||
                content.AllReferences.Any(item => item == null) ||
                content.AllReferences.Distinct().Count() != 33)
                throw new InvalidOperationException("Approved Journey Overview content is incomplete.");
        }
    }
}
