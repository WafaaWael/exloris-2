using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exloris.Runtime.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class Bootstrapper : MonoBehaviour
    {
        public const string HarnessScenePath = "Assets/Exloris/Scenes/FoundationHarness.unity";
        public const int HarnessSceneBuildIndex = 1;
        public const ScreenOrientation StartupOrientation = ScreenOrientation.AutoRotation;
        public const bool AllowLandscapeLeft = true;
        public const bool AllowLandscapeRight = true;
        public const bool AllowPortrait = false;
        public const bool AllowPortraitUpsideDown = false;

        private void Awake()
        {
            var framePacing = FramePacingPolicy.For(Application.platform);
            QualitySettings.vSyncCount = framePacing.VSyncCount;
            Application.targetFrameRate = framePacing.TargetFrameRate;
            Screen.autorotateToPortrait = AllowPortrait;
            Screen.autorotateToPortraitUpsideDown = AllowPortraitUpsideDown;
            Screen.autorotateToLandscapeLeft = AllowLandscapeLeft;
            Screen.autorotateToLandscapeRight = AllowLandscapeRight;
            Screen.orientation = StartupOrientation;
        }

        private void Start()
        {
            if (SceneManager.GetSceneByBuildIndex(HarnessSceneBuildIndex).isLoaded) return;
            Debug.Log("Foundation bootstrap loading harness build index 1 additively.");
            SceneManager.LoadScene(HarnessSceneBuildIndex, LoadSceneMode.Additive);
        }
    }
}
