using System;
using System.IO;
using System.Linq;
using Exloris.Editor.Validation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Exloris.Editor.Build
{
    public static class ReleaseEvidenceBuilder
    {
        public const long MaximumInstalledBytes = 200_000_000L;
        public const string MacArchitecture = "ARM64";

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
                    workspaceRoot, "builds", "release-candidate", "macos", "Exloris2.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CompressWithLz4HC,
            };
        }

        public static void BuildMac()
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            AcceptanceMatrixValidator.ValidateWorkspace(workspaceRoot, false);
            Stage3Build.EnsureMinimumFreeSpace(workspaceRoot);
            StartupIntroBuild.ValidateRuntimeMediaSet(Path.Combine(
                Application.dataPath, "StreamingAssets", "Exloris", "Intro"));
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
                throw new InvalidOperationException("Could not switch to the macOS build target.");

            FoundationBuild.ConfigurePlatformSettings();
            var previousRunInBackground = PlayerSettings.runInBackground;
            var previousArchitecture = EditorUserBuildSettings.GetPlatformSettings("Standalone", "Architecture");
            var previousStripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone);
            var previousStripEngine = PlayerSettings.stripEngineCode;
            var previousDevelopment = EditorUserBuildSettings.development;
            var previousDebugging = EditorUserBuildSettings.allowDebugging;
            var previousProfiler = EditorUserBuildSettings.connectProfiler;
            try
            {
                PlayerSettings.runInBackground = true;
                EditorUserBuildSettings.SetPlatformSettings("Standalone", "Architecture", MacArchitecture);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
                PlayerSettings.stripEngineCode = true;
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.connectProfiler = false;

                var options = CreateMacOptions(workspaceRoot);
                var parent = Path.GetDirectoryName(options.locationPathName)
                    ?? throw new InvalidOperationException("Release output directory is unavailable.");
                Directory.CreateDirectory(parent);
                if (Directory.Exists(options.locationPathName)) Directory.Delete(options.locationPathName, true);
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Release candidate build failed: {report.summary.result} with "
                        + $"{report.summary.totalErrors} error(s).");

                var removedDebugBytes = Stage3Build.RemoveManagedDebugSymbols(options.locationPathName);
                ResealAdHoc(options.locationPathName);
                FoundationPrivacyValidator.ValidateBuildArtifact(options.locationPathName);
                var installedBytes = Directory.GetFiles(
                        options.locationPathName, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                if (installedBytes >= MaximumInstalledBytes)
                    throw new InvalidOperationException(
                        $"Release candidate uses {installedBytes} bytes, not below {MaximumInstalledBytes}.");
                AcceptanceMatrixValidator.ValidateWorkspace(workspaceRoot, false);
                Debug.Log("RELEASE_BUILD_PASS path=" + options.locationPathName
                    + " bytes=" + installedBytes
                    + " removed_managed_debug_bytes=" + removedDebugBytes);
            }
            finally
            {
                EditorUserBuildSettings.connectProfiler = previousProfiler;
                EditorUserBuildSettings.allowDebugging = previousDebugging;
                EditorUserBuildSettings.development = previousDevelopment;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, previousStripping);
                PlayerSettings.stripEngineCode = previousStripEngine;
                EditorUserBuildSettings.SetPlatformSettings("Standalone", "Architecture", previousArchitecture);
                PlayerSettings.runInBackground = previousRunInBackground;
                AssetDatabase.SaveAssets();
            }
        }

        public static void ResealAdHoc(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath))
                throw new ArgumentException("App path is required.", nameof(appPath));
            if (!Directory.Exists(appPath))
                throw new DirectoryNotFoundException(appPath);

            RunCodesign("--force --deep --sign -", appPath);
            RunCodesign("--verify --deep --strict", appPath);
        }

        private static void RunCodesign(string arguments, string appPath)
        {
            var escapedPath = appPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/usr/bin/codesign",
                Arguments = arguments + " \"" + escapedPath + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("Could not start codesign.");
                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(
                        "codesign failed with exit code " + process.ExitCode + ": "
                        + standardOutput + standardError);
            }
        }
    }
}
