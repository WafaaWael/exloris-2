using System.IO;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Intro;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using Exloris.Runtime.UI;
using Exloris.Runtime.Welcome;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exloris.Editor
{
    public static class FoundationHarnessGenerator
    {
        private const string SceneFolder = "Assets/Exloris/Scenes";
        private const string BootstrapPath = SceneFolder + "/Bootstrap.unity";
        private const string HarnessPath = SceneFolder + "/FoundationHarness.unity";

        [MenuItem("Exloris/Foundation/Generate Foundation Harness Scenes")]
        public static void Generate()
        {
            EnsureSceneFolder();
            StartupIntroPrefabGenerator.Generate();
            WelcomeContentGenerator.Generate();
            WelcomePrefabGenerator.Generate();
            AvatarContentGenerator.Generate();
            AvatarPrefabGenerator.Generate();
            JourneyOverviewContentGenerator.ImportAndGenerate();
            JourneyOverviewPrefabGenerator.Generate();
            Boat1ContentGenerator.ImportAndGenerate();
            Boat1PrefabGenerator.Generate();
            Stage1ContentGenerator.ImportAndGenerate();
            Stage1PrefabGenerator.Generate();
            Boat2ContentGenerator.ImportAndGenerate();
            Boat2PrefabGenerator.Generate();
            Stage2ContentGenerator.ImportAndGenerate();
            Stage2PrefabGenerator.Generate();
            Boat3ContentGenerator.ImportAndGenerate();
            Boat3PrefabGenerator.Generate();
            Stage3ContentGenerator.ImportAndGenerate();
            Stage3PrefabGenerator.Generate();
            CompletionAdultReportContentGenerator.ImportAndGenerate();
            CompletionAdultReportPrefabGenerator.Generate();

            var bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var compositionRoot = new GameObject(
                PersistentCompositionRoot.RootName,
                typeof(PersistentCompositionRoot),
                typeof(Bootstrapper));
            var screenHost = new GameObject(
                "ScreenHost", typeof(ScreenHost), typeof(JourneyOverviewStageHost), typeof(Boat1StageHost),
                typeof(Stage1StageHost), typeof(Boat2StageHost), typeof(Stage2StageHost), typeof(Boat3StageHost),
                typeof(Stage3StageHost), typeof(CompletionAdultReportStageHost));
            screenHost.transform.SetParent(compositionRoot.transform, false);
            var startupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StartupIntroPrefabGenerator.StartupPath);
            var introPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StartupIntroPrefabGenerator.IntroPath);
            var welcomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WelcomePrefabGenerator.WelcomePath);
            var sessionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WelcomePrefabGenerator.SessionChoicePath);
            var avatarSelectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabGenerator.SelectPath);
            var avatarPreviewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabGenerator.PreviewPath);
            var leavePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabGenerator.LeavePath);
            var journeyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                JourneyOverviewPrefabGenerator.OverviewPath);
            var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                JourneyOverviewPrefabGenerator.PausePath);
            var helpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                JourneyOverviewPrefabGenerator.HelpPath);
            var boat1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat1PrefabGenerator.StagePath);
            var boat1HelpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat1PrefabGenerator.HelpPath);
            var stage1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1PrefabGenerator.StagePath);
            var stage1HelpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1PrefabGenerator.HelpPath);
            var boat2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat2PrefabGenerator.StagePath);
            var boat2HelpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat2PrefabGenerator.HelpPath);
            var stage2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage2PrefabGenerator.StagePath);
            var stage2HelpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage2PrefabGenerator.HelpPath);
            var boat3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat3PrefabGenerator.StagePath);
            var boat3HelpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat3PrefabGenerator.HelpPath);
            var stage3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage3PrefabGenerator.StagePath);
            var stage3HelpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage3PrefabGenerator.HelpPath);
            var completionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CompletionAdultReportPrefabGenerator.CompletionPath);
            var adultGatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CompletionAdultReportPrefabGenerator.AdultGatePath);
            var parentReportPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CompletionAdultReportPrefabGenerator.ParentReportPath);
            screenHost.GetComponent<ScreenHost>().ConfigureStartupIntro(
                true,
                startupPrefab,
                introPrefab.GetComponent<IntroStageView>(),
                welcomePrefab.GetComponent<WelcomeStageView>(),
                sessionPrefab.GetComponent<SessionChoiceView>(),
                avatarSelectPrefab.GetComponent<AvatarSelectView>(),
                avatarPreviewPrefab.GetComponent<AvatarPreviewView>(),
                leavePrefab.GetComponent<LeaveConfirmationView>(),
                journeyPrefab.GetComponent<JourneyOverviewStageView>(),
                pausePrefab.GetComponent<PauseOverlayView>(),
                helpPrefab.GetComponent<JourneyOverviewPauseHelpView>(),
                boat1Prefab.GetComponent<Boat1StageView>(),
                boat1HelpPrefab.GetComponent<Boat1HelpView>(),
                stage1Prefab.GetComponent<Stage1StageView>(),
                stage1HelpPrefab.GetComponent<Stage1HelpView>(),
                boat2Prefab.GetComponent<Boat2StageView>(),
                boat2HelpPrefab.GetComponent<Boat2HelpView>(),
                stage2Prefab.GetComponent<Stage2StageView>(),
                stage2HelpPrefab.GetComponent<Stage2HelpView>(),
                boat3Prefab.GetComponent<Boat3StageView>(),
                boat3HelpPrefab.GetComponent<Boat3HelpView>(),
                stage3Prefab.GetComponent<Stage3StageView>(),
                stage3HelpPrefab.GetComponent<Stage3HelpView>());
            screenHost.GetComponent<ScreenHost>().ConfigureClosingRoute(
                completionPrefab.GetComponent<CompletionStageView>(),
                adultGatePrefab.GetComponent<AdultGateView>(),
                parentReportPrefab.GetComponent<ParentReportView>());
            EditorSceneManager.SaveScene(bootstrap, BootstrapPath);

            var harness = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("FoundationCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 39, 55, 255);
            camera.cullingMask = 0;
            EditorSceneManager.SaveScene(harness, HarnessPath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapPath, true),
                new EditorBuildSettingsScene(HarnessPath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Generated Bootstrap and FoundationHarness scenes and pinned build order.");
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                AssetDatabase.CreateFolder("Assets/Exloris", "Scenes");
            }
        }
    }
}
