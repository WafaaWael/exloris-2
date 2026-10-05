using BoatGame.CoreData.Messaging;
using UnityEngine;

namespace BoatGame.CoreData
{
    /// <summary>
    /// Player options kept in PlayerPrefs: 0..1 volumes (publish <see cref="AudioSettingsChangedEvent"/> after changing
    /// them) and display options (call <see cref="FrameRateSettings.Apply"/> after changing them).
    /// </summary>
    public static class GameSettings
    {
        public const int UnlimitedFrameRate = 0;

        private const string MusicKey = "settings.music";
        private const string SoundsKey = "settings.sounds";
        private const string VoiceKey = "settings.voice";
        private const string VSyncKey = "settings.vsync";
        private const string FrameCapKey = "settings.framecap";
        private const int DefaultFrameCap = 60;

        public static float Music
        {
            get => PlayerPrefs.GetFloat(MusicKey, 1f);
            set => PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        }

        public static float Sounds
        {
            get => PlayerPrefs.GetFloat(SoundsKey, 1f);
            set => PlayerPrefs.SetFloat(SoundsKey, Mathf.Clamp01(value));
        }

        public static float Voice
        {
            get => PlayerPrefs.GetFloat(VoiceKey, 1f);
            set => PlayerPrefs.SetFloat(VoiceKey, Mathf.Clamp01(value));
        }

        /// <summary>Sync to the display refresh (no tearing). On by default; phones always sync, so it only matters on PC.</summary>
        public static bool IsVSync
        {
            get => PlayerPrefs.GetInt(VSyncKey, 1) == 1;
            set => PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
        }

        /// <summary>Frames per second cap used when VSync is off (and always on phones); <see cref="UnlimitedFrameRate"/> = no cap.</summary>
        public static int FrameCap
        {
            get => PlayerPrefs.GetInt(FrameCapKey, DefaultFrameCap);
            set => PlayerPrefs.SetInt(FrameCapKey, Mathf.Max(UnlimitedFrameRate, value));
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }

    public readonly struct AudioSettingsChangedEvent : IEvent
    {
    }

    /// <summary>The pause menu opened (true) or closed (false). Game time is already frozen by the sender.</summary>
    public readonly struct GamePausedEvent : IEvent
    {
        public readonly bool IsPaused;

        public GamePausedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }
}