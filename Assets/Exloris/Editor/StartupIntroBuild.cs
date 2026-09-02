using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Exloris.Editor
{
    public static class StartupIntroBuild
    {
        public const long MinimumFreeBytes = 15L * 1024L * 1024L * 1024L;
        public const long MaximumInstalledBytes = 200_000_000L;
        public const long MaximumIntroMediaBytes = 45_000_000L;

        private static readonly string[] ApprovedVideos =
        {
            "intro-16x9-low.mp4",
            "intro-16x9.mp4",
            "intro-24x10.mp4",
            "intro-4x3.mp4",
            "intro-reduced-16x9-low.mp4",
            "intro-reduced-16x9.mp4",
            "intro-reduced-24x10.mp4",
            "intro-reduced-4x3.mp4"
        };

        public static IReadOnlyList<string> ApprovedRuntimeVideoFileNames => ApprovedVideos;

        private static readonly string[] Scenes =
        {
            "Assets/Exloris/Scenes/Bootstrap.unity",
            "Assets/Exloris/Scenes/FoundationHarness.unity"
        };

        public static BuildPlayerOptions CreateMacOptions(string workspaceRoot)
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
            {
                throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
            }

            return new BuildPlayerOptions
            {
                scenes = (string[])Scenes.Clone(),
                locationPathName = Path.Combine(
                    workspaceRoot,
                    "builds",
                    "startup-intro",
                    "macos",
                    "Exloris2.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };
        }

        public static void BuildMac()
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            EnsureMinimumFreeSpace(workspaceRoot);
            ValidateRuntimeMediaSet(Path.Combine(
                Application.dataPath,
                "StreamingAssets",
                "Exloris",
                "Intro"));

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneOSX))
            {
                throw new InvalidOperationException("Could not switch to the macOS build target.");
            }

            FoundationBuild.ConfigurePlatformSettings();
            var previousRunInBackground = PlayerSettings.runInBackground;
            try
            {
                PlayerSettings.runInBackground = true;
                var options = CreateMacOptions(workspaceRoot);
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName) ??
                                          throw new InvalidOperationException("macOS output directory is unavailable."));
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Startup + Intro macOS build failed: {report.summary.result} with {report.summary.totalErrors} error(s).");
                }

                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);
                EnsureInstalledSizeBelowCeiling(CalculateArtifactBytes(options.locationPathName));
                Debug.Log("Startup + Intro macOS build succeeded at " + options.locationPathName + ".");
            }
            finally
            {
                PlayerSettings.runInBackground = previousRunInBackground;
            }
        }

        public static void EnsureMinimumFreeSpace(string workspaceRoot)
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
            {
                throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
            }

            var fullPath = Path.GetFullPath(workspaceRoot);
            var drive = DriveInfo.GetDrives()
                .Where(candidate =>
                    fullPath.StartsWith(
                        candidate.RootDirectory.FullName,
                        StringComparison.Ordinal))
                .OrderByDescending(candidate => candidate.RootDirectory.FullName.Length)
                .FirstOrDefault() ?? new DriveInfo(Path.GetPathRoot(fullPath) ?? fullPath);
            if (drive.AvailableFreeSpace < MinimumFreeBytes)
            {
                throw new InvalidOperationException(
                    $"Startup + Intro build requires at least {MinimumFreeBytes} free bytes on the project volume; " +
                    $"only {drive.AvailableFreeSpace} are available.");
            }
        }

        public static void ValidateRuntimeMediaSet(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                throw new InvalidOperationException("The runtime intro media directory is missing.");
            }

            var files = Directory.GetFiles(directory)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var names = files
                .Select(Path.GetFileName)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (!names.SequenceEqual(ApprovedVideos, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Runtime intro media must contain exactly the eight approved MP4 files and no previews or PNG frames.");
            }

            var totalBytes = files.Sum(path => new FileInfo(path).Length);
            if (totalBytes > MaximumIntroMediaBytes)
            {
                throw new InvalidOperationException(
                    $"Runtime intro media uses {totalBytes} bytes, above the {MaximumIntroMediaBytes}-byte ceiling.");
            }
        }

        public static void ValidateMotionCadence(
            bool reducedMotion,
            int frameRate,
            long frameCount,
            long uniqueFrameCount,
            int longestIdenticalRun)
        {
            var expectedFrameCount = reducedMotion
                ? Exloris.Runtime.Intro.IntroVariantSelector.ReducedFrameCount
                : Exloris.Runtime.Intro.IntroVariantSelector.FullFrameCount;
            var invalidBaseContract =
                frameRate != Exloris.Runtime.Intro.IntroVariantSelector.FrameRate ||
                frameCount != expectedFrameCount ||
                uniqueFrameCount <= 0 ||
                uniqueFrameCount > frameCount ||
                longestIdenticalRun <= 0;
            var invalidFullMotionCadence =
                !reducedMotion &&
                (uniqueFrameCount != frameCount || longestIdenticalRun != 1);
            if (invalidBaseContract || invalidFullMotionCadence)
            {
                throw new InvalidOperationException(
                    reducedMotion
                        ? "Reduced intro media must be the approved 120-frame static/cut/short-fade sequence at 30 fps."
                        : "Full-motion intro media must be 30 fps with every decoded frame unique; five-fps frame holding is forbidden.");
            }
        }

        public static void EnsureInstalledSizeBelowCeiling(long installedBytes)
        {
            if (installedBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(installedBytes));
            }

            if (installedBytes >= MaximumInstalledBytes)
            {
                throw new InvalidOperationException(
                    $"Built player uses {installedBytes} bytes, which is not below the {MaximumInstalledBytes}-byte ceiling.");
            }
        }

        private static long CalculateArtifactBytes(string path)
        {
            if (File.Exists(path)) return new FileInfo(path).Length;
            if (!Directory.Exists(path))
            {
                throw new InvalidOperationException("Built player artifact is missing: " + path);
            }

            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);
        }
    }
}
