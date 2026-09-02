using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Exloris.Editor
{
    public static class WelcomeBuild
    {
        public const long MinimumFreeBytes = 15L * 1024L * 1024L * 1024L;
        public const long MaximumInstalledBytes = 200_000_000L;

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
                locationPathName = Path.Combine(workspaceRoot, "builds", "welcome", "macos", "Exloris2.app"),
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
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
                throw new InvalidOperationException("Could not switch to the macOS build target.");

            FoundationBuild.ConfigurePlatformSettings();
            var previousRunInBackground = PlayerSettings.runInBackground;
            try
            {
                PlayerSettings.runInBackground = true;
                var options = CreateMacOptions(workspaceRoot);
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName) ??
                                          throw new InvalidOperationException("Welcome output directory is unavailable."));
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Welcome macOS build failed: {report.summary.result} with {report.summary.totalErrors} error(s).");

                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);
                var installedBytes = Directory.GetFiles(options.locationPathName, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                StartupIntroBuild.EnsureInstalledSizeBelowCeiling(installedBytes);
                Debug.Log($"Welcome macOS build succeeded at {options.locationPathName}; bytes={installedBytes}.");
            }
            finally
            {
                PlayerSettings.runInBackground = previousRunInBackground;
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
                    $"Welcome build requires at least {MinimumFreeBytes} free bytes; " +
                    $"only {drive.AvailableFreeSpace} are available.");
        }
    }
}
