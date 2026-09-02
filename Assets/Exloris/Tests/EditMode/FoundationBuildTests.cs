using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Exloris.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Exloris.Tests.EditMode
{
    public sealed class FoundationBuildTests
    {
        private static readonly string[] ExpectedScenes =
        {
            "Assets/Exloris/Scenes/Bootstrap.unity",
            "Assets/Exloris/Scenes/FoundationHarness.unity"
        };

        [TestCase(BuildTarget.iOS, "ios")]
        [TestCase(BuildTarget.Android, "android/ExlorisFoundation.apk")]
        public void CreateOptionsTargetsOnlyTheFoundationHarness(
            BuildTarget target,
            string relativeOutput)
        {
            var workspaceRoot = Path.Combine(Path.GetTempPath(), "Exloris2BuildTest");

            var options = FoundationBuild.CreateOptions(target, workspaceRoot);

            Assert.That(options.target, Is.EqualTo(target));
            Assert.That(options.scenes, Is.EqualTo(ExpectedScenes));
            Assert.That(
                options.locationPathName,
                Is.EqualTo(Path.Combine(workspaceRoot, "builds", "foundation-stage-0", relativeOutput)));
        }

        [Test]
        public void CreateOptionsRejectsUnsupportedTargets()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FoundationBuild.CreateOptions(BuildTarget.StandaloneOSX, "/tmp/Exloris2BuildTest"));
        }

        [Test]
        public void CreateMacOptionsUsesFoundationScenesAndCanonicalOutput()
        {
            var workspaceRoot = Path.Combine(Path.GetTempPath(), "Exloris2MacBuildTest");

            var options = FoundationBuild.CreateMacOptions(workspaceRoot);

            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.scenes, Is.EqualTo(ExpectedScenes));
            Assert.That(
                options.locationPathName,
                Is.EqualTo(Path.Combine(
                    workspaceRoot,
                    "builds",
                    "foundation-stage-0",
                    "macos",
                    "ExlorisFoundation.app")));
        }

        [Test]
        public void ConfigurePlatformIdentityUsesTheFrozenExlorisIdentifier()
        {
            Assert.That(FoundationBuild.ApplicationIdentifier, Is.EqualTo("com.exloris.exloris2"));
        }

        [Test]
        public void ProjectSettingsDisableUnityCloudDiagnosticsAndIdentifiers()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var connect = File.ReadAllText(Path.Combine(
                projectRoot,
                "ProjectSettings",
                "UnityConnectSettings.asset"));
            var settings = File.ReadAllText(Path.Combine(
                projectRoot,
                "ProjectSettings",
                "ProjectSettings.asset"));

            Assert.That(connect, Does.Contain("UnityConnectSettings:\n  m_ObjectHideFlags: 0\n  serializedVersion: 1\n  m_Enabled: 0"));
            Assert.That(connect, Does.Contain("m_EngineDiagnosticsEnabled: 0"));
            Assert.That(connect, Does.Contain("m_EnableCloudDiagnosticsReporting: 0"));
            Assert.That(connect, Does.Contain("UnityAnalyticsSettings:\n    m_Enabled: 0"));
            Assert.That(connect, Does.Contain("UnityAdsSettings:\n    m_Enabled: 0"));
            Assert.That(settings, Does.Contain("cloudProjectId: \n"));
            Assert.That(settings, Does.Contain("organizationId: \n"));
            Assert.That(settings, Does.Contain("submitAnalytics: 0"));
            Assert.That(PlayerSettings.enableCrashReportAPI, Is.False);
        }

        [Test]
        public void LocalOnlyAddressablesCannotCheckForCatalogUpdatesOnStartup()
        {
            var settings = AssetDatabase.LoadMainAssetAtPath(
                "Assets/AddressableAssetsData/AddressableAssetSettings.asset");
            var serialized = new SerializedObject(settings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(serialized.FindProperty("m_BuildRemoteCatalog").boolValue, Is.False);
            Assert.That(serialized.FindProperty("m_DisableCatalogUpdateOnStart").boolValue, Is.True);
        }

        [Test]
        public void CurrentIosExportContainsNoUnityCloudIdentifierOrTelemetryEndpoint()
        {
            var workspaceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var globalManagers = File.ReadAllBytes(Path.Combine(
                workspaceRoot,
                "builds",
                "foundation-stage-0",
                "ios",
                "Data",
                "globalgamemanagers"));
            var text = Encoding.UTF8.GetString(globalManagers);

            Assert.That(text, Does.Not.Contain("875ef47f-918b-475f-88bf-daa42374bd75"));
            Assert.That(text, Does.Not.Contain("nasser_unity884"));
            Assert.That(text, Does.Not.Contain("api.uca.cloud.unity3d.com"));
            Assert.That(text, Does.Not.Contain("cdp.cloud.unity3d.com"));
            Assert.That(text, Does.Not.Contain("engine-data.unity3d.com"));
            Assert.That(text, Does.Not.Contain("engine-data.services.api.unity.com"));
            Assert.That(text, Does.Not.Contain("perf-events.cloud.unity3d.com"));
        }

        [Test]
        public void ConfigurePlatformSettingsPinsIdentityAndAndroidToOpenGles3Only()
        {
            var oldStandalone = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone);
            var oldDefaultApis = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            var oldApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            try
            {
                FoundationBuild.ConfigurePlatformSettings();

                Assert.That(
                    PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone),
                    Is.EqualTo(FoundationBuild.ApplicationIdentifier));
                Assert.That(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android), Is.False);
                CollectionAssert.AreEqual(
                    new[] { GraphicsDeviceType.OpenGLES3 },
                    PlayerSettings.GetGraphicsAPIs(BuildTarget.Android));
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, oldStandalone);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, oldDefaultApis);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, oldApis);
            }
        }

        [Test]
        public void ConfigurePlatformSettingsRestoresOwnerApprovedUnity63MobileMinimums()
        {
            var oldAndroid = PlayerSettings.Android.minSdkVersion;
            var oldIos = PlayerSettings.iOS.targetOSVersionString;
            try
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                PlayerSettings.iOS.targetOSVersionString = "13.0";

                FoundationBuild.ConfigurePlatformSettings();

                Assert.That(
                    PlayerSettings.Android.minSdkVersion,
                    Is.EqualTo(AndroidSdkVersions.AndroidApiLevel25));
                Assert.That(PlayerSettings.iOS.targetOSVersionString, Is.EqualTo("15.0"));
            }
            finally
            {
                PlayerSettings.Android.minSdkVersion = oldAndroid;
                PlayerSettings.iOS.targetOSVersionString = oldIos;
            }
        }

        [Test]
        public void IosExportValidatorRejectsCorruptNonApplicationPlist()
        {
            var exportRoot = Path.Combine(Path.GetTempPath(), "Exloris2InvalidIosExport");
            Directory.CreateDirectory(exportRoot);
            File.WriteAllText(Path.Combine(exportRoot, "Info.plist"), "[\"arm64\",\"metal\"]");

            var error = Assert.Throws<InvalidDataException>(() =>
                FoundationIOSExportValidator.Validate(exportRoot));

            Assert.That(error.Message, Does.Contain("Info.plist"));
        }

        [Test]
        public void IosExportValidatorAcceptsApplicationDictionaryWithRequiredKeys()
        {
            var exportRoot = Path.Combine(Path.GetTempPath(), "Exloris2ValidIosExport");
            Directory.CreateDirectory(exportRoot);
            File.WriteAllText(
                Path.Combine(exportRoot, "Info.plist"),
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<plist version=\"1.0\"><dict>" +
                "<key>CFBundleIdentifier</key><string>com.exloris.exloris2</string>" +
                "<key>CFBundleExecutable</key><string>Exloris2</string>" +
                "</dict></plist>");

            Assert.DoesNotThrow(() => FoundationIOSExportValidator.Validate(exportRoot));
        }

        [Test]
        public void SanitizeManifestRemovesOnlyInternetPermission()
        {
            const string androidNamespace = "http://schemas.android.com/apk/res/android";
            const string manifest =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">" +
                "<uses-permission android:name=\"android.permission.INTERNET\" />" +
                "<uses-permission android:name=\"android.permission.VIBRATE\" />" +
                "<application />" +
                "</manifest>";

            var sanitized = FoundationAndroidManifest.Sanitize(manifest);
            var document = XDocument.Parse(sanitized);
            var permissions = document.Root!
                .Elements("uses-permission")
                .Select(element => (string)element.Attribute(XName.Get("name", androidNamespace)))
                .ToArray();

            Assert.That(permissions, Does.Not.Contain("android.permission.INTERNET"));
            Assert.That(permissions, Does.Contain("android.permission.VIBRATE"));
            Assert.That(document.Root.Element("application"), Is.Not.Null);
        }
    }
}
