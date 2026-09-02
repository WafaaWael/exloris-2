using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Exloris.Editor
{
    public static class FoundationPrivacyValidator
    {
        private static readonly string[] ForbiddenArtifactTokens =
        {
            "875ef47f-918b-475f-88bf-daa42374bd75",
            "nasser_unity884",
            "api.uca.cloud.unity3d.com",
            "cdp.cloud.unity3d.com",
            "config.uca.cloud.unity3d.com",
            "engine-data.unity3d.com",
            "engine-data.services.api.unity.com",
            "perf-events.cloud.unity3d.com",
            "#define ENABLE_CRASH_REPORT_SUBMISSION 1",
            "NSPrivacyCollectedDataTypeUserID",
            "NSPrivacyCollectedDataTypeProductInteraction",
            "NSPrivacyCollectedDataTypePerformanceData",
            "NSPrivacyCollectedDataTypeCrashData",
            "NSPrivacyCollectedDataTypeOtherDiagnosticData",
            "NSPrivacyCollectedDataTypeOtherUsageData"
        };

        public static void ValidateProjectSettings(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            }

            var connect = File.ReadAllText(Path.Combine(
                projectRoot,
                "ProjectSettings",
                "UnityConnectSettings.asset"));
            var settings = File.ReadAllText(Path.Combine(
                projectRoot,
                "ProjectSettings",
                "ProjectSettings.asset"));

            Require(connect.Contains("UnityConnectSettings:\n  m_ObjectHideFlags: 0\n  serializedVersion: 1\n  m_Enabled: 0"),
                "Unity Connect must be disabled.");
            Require(connect.Contains("m_EngineDiagnosticsEnabled: 0"),
                "Unity Engine Diagnostics must be disabled.");
            Require(connect.Contains("m_EnableCloudDiagnosticsReporting: 0"),
                "Unity cloud diagnostics reporting must be disabled.");
            Require(connect.Contains("UnityAnalyticsSettings:\n    m_Enabled: 0"),
                "Unity Analytics must be disabled.");
            Require(connect.Contains("UnityAdsSettings:\n    m_Enabled: 0"),
                "Unity Ads must be disabled.");
            Require(settings.Contains("cloudProjectId: \n"),
                "Unity cloud project ID must be empty.");
            Require(settings.Contains("organizationId: \n"),
                "Unity organization ID must be empty.");
            Require(settings.Contains("enableCrashReportAPI: 0"),
                "Unity crash report API must be disabled.");
            Require(settings.Contains("submitAnalytics: 0"),
                "Unity player analytics submission must be disabled.");
        }

        public static void ValidateBuildArtifact(string artifactPath)
        {
            if (string.IsNullOrWhiteSpace(artifactPath) ||
                !File.Exists(artifactPath) && !Directory.Exists(artifactPath))
            {
                throw new FileNotFoundException("Built-player privacy artifact is missing.", artifactPath);
            }

            var findings = new List<string>();
            if (Directory.Exists(artifactPath))
            {
                foreach (var file in Directory.EnumerateFiles(artifactPath, "*", SearchOption.AllDirectories))
                {
                    ScanBytes(File.ReadAllBytes(file), file, findings);
                }
            }
            else if (string.Equals(Path.GetExtension(artifactPath), ".apk", StringComparison.OrdinalIgnoreCase))
            {
                using (var archive = ZipFile.OpenRead(artifactPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Length == 0) continue;
                        using (var stream = entry.Open())
                        using (var memory = new MemoryStream())
                        {
                            stream.CopyTo(memory);
                            ScanBytes(memory.ToArray(), entry.FullName, findings);
                        }
                    }
                }
            }
            else
            {
                ScanBytes(File.ReadAllBytes(artifactPath), artifactPath, findings);
            }

            if (findings.Count > 0)
            {
                throw new InvalidDataException(
                    "Built player contains forbidden Unity telemetry/privacy payload: " +
                    string.Join("; ", findings));
            }
        }

        private static void ScanBytes(byte[] bytes, string source, ICollection<string> findings)
        {
            var text = Encoding.UTF8.GetString(bytes);
            foreach (var token in ForbiddenArtifactTokens)
            {
                if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    findings.Add(source + " => " + token);
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }
    }
}
