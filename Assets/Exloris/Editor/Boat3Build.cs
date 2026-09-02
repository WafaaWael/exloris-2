using System;
using System.IO;
using System.Linq;
using Exloris.Runtime.Boat;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Boat3Build
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
                    workspaceRoot, "builds", "boat-3", "macos", "Exloris2.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CompressWithLz4HC
            };
        }

        public static void BuildMac()
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            EnsureMinimumFreeSpace(workspaceRoot);
            StartupIntroBuild.ValidateRuntimeMediaSet(Path.Combine(
                Application.dataPath, "StreamingAssets", "Exloris", "Intro"));
            ValidateBoat3Content();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
                throw new InvalidOperationException("Could not switch to the macOS build target.");

            FoundationBuild.ConfigurePlatformSettings();
            var previousRunInBackground = PlayerSettings.runInBackground;
            var previousArchitecture = EditorUserBuildSettings.GetPlatformSettings(
                "Standalone", "Architecture");
            var previousStripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone);
            var previousStripEngineCode = PlayerSettings.stripEngineCode;
            try
            {
                PlayerSettings.runInBackground = true;
                EditorUserBuildSettings.SetPlatformSettings(
                    "Standalone", "Architecture", MacEvidenceArchitecture);
                PlayerSettings.SetManagedStrippingLevel(
                    NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
                PlayerSettings.stripEngineCode = true;
                var options = CreateMacOptions(workspaceRoot);
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName) ??
                                          throw new InvalidOperationException(
                                              "Boat 3 output directory is unavailable."));
                if (Directory.Exists(options.locationPathName))
                    Directory.Delete(options.locationPathName, recursive: true);
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Boat 3 macOS build failed: {report.summary.result} " +
                        $"with {report.summary.totalErrors} error(s).");

                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);
                var installedBytes = Directory.GetFiles(
                        options.locationPathName, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                if (installedBytes >= MaximumInstalledBytes)
                    throw new InvalidOperationException(
                        $"Boat 3 build uses {installedBytes} bytes, not below {MaximumInstalledBytes}.");
                Debug.Log($"Boat 3 macOS build succeeded at {options.locationPathName}; bytes={installedBytes}.");
            }
            finally
            {
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, previousStripping);
                PlayerSettings.stripEngineCode = previousStripEngineCode;
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
                    $"Boat 3 build requires at least {MinimumFreeBytes} free bytes; " +
                    $"only {drive.AvailableFreeSpace} are available.");
        }

        private static void ValidateBoat3Content()
        {
            var content = AssetDatabase.LoadAssetAtPath<Boat3Content>(
                "Assets/Exloris/Resources/Content/Boat3Content.asset");
            if (content == null || content.AllReferences.Length != 50 ||
                content.AllReferences.Any(item => item == null) ||
                content.AllReferences.Distinct().Count() != 50)
                throw new InvalidOperationException("Approved Boat 3 content is incomplete.");
        }
    }
}
