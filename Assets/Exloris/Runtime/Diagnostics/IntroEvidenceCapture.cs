using System;
using System.Collections;
using System.IO;
using Exloris.Core.Navigation;
using Exloris.Runtime.Intro;
using Exloris.Runtime.UI;
using UnityEngine;

namespace Exloris.Runtime.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class IntroEvidenceCapture : MonoBehaviour
    {
        private ScreenHost _host;

        private void Awake()
        {
            _host = GetComponent<ScreenHost>();
        }

        private IEnumerator Start()
        {
            if (!IntroEvidenceOptions.TryGetCapture(
                    Environment.GetCommandLineArgs(),
                    out var options))
            {
                yield break;
            }

            if (options.Muted) AudioListener.volume = 0f;
            var deadline = Time.realtimeSinceStartup + 20f;
            while ((_host.CurrentRoute != RouteId.IntroVideo ||
                    _host.CurrentIntroMediaTime < options.AtSeconds) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (_host.CurrentRoute != RouteId.IntroVideo)
            {
                Debug.LogError("Intro evidence capture did not reach the intro before timeout.");
                Application.Quit(2);
                yield break;
            }

            yield return new WaitForEndOfFrame();
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false);
            texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(options.Path, texture.EncodeToPNG());
            Destroy(texture);

            var record = new EvidenceRecord
            {
                width = Screen.width,
                height = Screen.height,
                selected_file = _host.CurrentIntroVariantFile,
                prepared_frame_count = _host.PreparedIntroFrameCount,
                presented_frame_index = _host.LastPresentedIntroFrameIndex,
                media_time_seconds = _host.CurrentIntroMediaTime,
                reduced_motion = _host.CurrentIntroReducedMotion,
                captions_enabled = options.CaptionsEnabled,
                muted = options.Muted,
                voice_playing = _host.IntroVoiceIsPlaying,
                overlay_drift_seconds = _host.IntroOverlayDriftSeconds,
                voice_video_start_drift_seconds = _host.IntroVoiceVideoStartDriftSeconds,
                startup_to_first_child_frame_seconds = _host.IntroFirstChildFrameElapsedSeconds,
                child_facing_controls = string.Join(",", _host.ActiveIntroView.ChildFacingControlIds)
            };
            var jsonPath = Path.ChangeExtension(options.Path, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(record, prettyPrint: true));
            Debug.Log("INTRO_EVIDENCE_CAPTURE path=" + options.Path + " metadata=" + jsonPath);
            Application.Quit(0);
        }

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width;
            public int height;
            public string selected_file;
            public long prepared_frame_count;
            public long presented_frame_index;
            public double media_time_seconds;
            public bool reduced_motion;
            public bool captions_enabled;
            public bool muted;
            public bool voice_playing;
            public double overlay_drift_seconds;
            public double voice_video_start_drift_seconds;
            public double startup_to_first_child_frame_seconds;
            public string child_facing_controls;
        }
    }
}
