using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Exloris.Editor
{
    public static class FoundationBuild
    {
        public const string ApplicationIdentifier = "com.exloris.exloris2";

        private static readonly string[] FoundationScenes =
        {
            "Assets/Exloris/Scenes/Bootstrap.unity",
            "Assets/Exloris/Scenes/FoundationHarness.unity"
        };

        public static BuildPlayerOptions CreateOptions(BuildTarget target, string workspaceRoot)
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
            {
                throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
            }

            string relativeOutput;
            switch (target)
            {
                case BuildTarget.iOS:
                    relativeOutput = "ios";
                    break;
                case BuildTarget.Android:
                    relativeOutput = Path.Combine("android", "ExlorisFoundation.apk");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "Only Foundation mobile targets are supported.");
            }

            return new BuildPlayerOptions
            {
                scenes = (string[])FoundationScenes.Clone(),
                locationPathName = Path.Combine(
                    workspaceRoot,
                    "builds",
                    "foundation-stage-0",
                    relativeOutput),
                target = target,
                options = BuildOptions.CleanBuildCache
            };
        }

        public static BuildPlayerOptions CreateMacOptions(string workspaceRoot)
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
            {
                throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
            }

            return new BuildPlayerOptions
            {
                scenes = (string[])FoundationScenes.Clone(),
                locationPathName = Path.Combine(
                    workspaceRoot,
                    "builds",
                    "foundation-stage-0",
                    "macos",
                    "ExlorisFoundation.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CleanBuildCache
            };
        }

        public static void BuildIOS()
        {
            Build(BuildTarget.iOS);
        }

        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android);
        }

        public static void BuildMac()
        {
            EnsureActiveBuildTarget(BuildTarget.StandaloneOSX);
            ConfigurePlatformSettings();
            var previousRunInBackground = PlayerSettings.runInBackground;
            try
            {
                PlayerSettings.runInBackground = true;
                var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                var options = CreateMacOptions(workspaceRoot);
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName) ??
                                          throw new InvalidOperationException("macOS output directory is unavailable."));
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Foundation macOS build failed: {report.summary.result} with {report.summary.totalErrors} error(s).");
                }

                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);

                Debug.Log($"Foundation macOS build succeeded at {options.locationPathName}.");
            }
            finally
            {
                PlayerSettings.runInBackground = previousRunInBackground;
            }
        }

        private static void Build(BuildTarget target)
        {
            EnsureActiveBuildTarget(target);
            ConfigurePlatformSettings();
            ConfigurePlatformIdentity(target);

            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var options = CreateOptions(target, workspaceRoot);
            var outputDirectory = target == BuildTarget.iOS
                ? options.locationPathName
                : Path.GetDirectoryName(options.locationPathName);
            Directory.CreateDirectory(outputDirectory ?? throw new InvalidOperationException("Build output directory is unavailable."));

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Foundation {target} build failed: {report.summary.result} with {report.summary.totalErrors} error(s).");
            }

            if (target == BuildTarget.iOS)
            {
                FoundationIOSExportValidator.Validate(options.locationPathName);
            }

            FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);

            Debug.Log($"Foundation {target} build succeeded at {options.locationPathName}.");
        }

        public static void ConfigurePlatformSettings()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, ApplicationIdentifier);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, ApplicationIdentifier);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, ApplicationIdentifier);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.Android,
                new[] { GraphicsDeviceType.OpenGLES3 });
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            FoundationPrivacyValidator.ValidateProjectSettings(projectRoot);
        }

        private static void ConfigurePlatformIdentity(BuildTarget target)
        {
            BuildTargetGroup group;
            switch (target)
            {
                case BuildTarget.iOS:
                    group = BuildTargetGroup.iOS;
                    break;
                case BuildTarget.Android:
                    group = BuildTargetGroup.Android;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "Only Foundation mobile targets are supported.");
            }

            PlayerSettings.SetApplicationIdentifier(group, ApplicationIdentifier);
        }

        private static void EnsureActiveBuildTarget(BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target) return;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
            {
                throw new InvalidOperationException("Could not switch active build target to " + target + ".");
            }
        }
    }
}
