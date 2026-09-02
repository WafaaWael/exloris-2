using UnityEngine;

namespace Exloris.Runtime.Preferences
{
    public readonly struct AccessibilityPreferences
    {
        public AccessibilityPreferences(bool alwaysReduce, bool captionsEnabled)
        {
            AlwaysReduce = alwaysReduce;
            CaptionsEnabled = captionsEnabled;
        }

        public bool AlwaysReduce { get; }
        public bool CaptionsEnabled { get; }
    }

    public interface IPreferenceBackend
    {
        bool GetBool(string key, bool defaultValue);
        void SetBool(string key, bool value);
        void Save();
    }

    public sealed class AccessibilityPreferenceStore
    {
        public const string AlwaysReduceKey = "exloris.accessibility.always_reduce";
        public const string CaptionsEnabledKey = "exloris.accessibility.captions_enabled";

        private readonly IPreferenceBackend _backend;

        public AccessibilityPreferenceStore(IPreferenceBackend backend = null)
        {
            _backend = backend ?? new PlayerPrefsPreferenceBackend();
        }

        public AccessibilityPreferences Load()
        {
            return new AccessibilityPreferences(
                _backend.GetBool(AlwaysReduceKey, false),
                _backend.GetBool(CaptionsEnabledKey, false));
        }

        public void Save(AccessibilityPreferences preferences)
        {
            _backend.SetBool(AlwaysReduceKey, preferences.AlwaysReduce);
            _backend.SetBool(CaptionsEnabledKey, preferences.CaptionsEnabled);
            _backend.Save();
        }

        private sealed class PlayerPrefsPreferenceBackend : IPreferenceBackend
        {
            public bool GetBool(string key, bool defaultValue)
            {
                return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
            }

            public void SetBool(string key, bool value)
            {
                PlayerPrefs.SetInt(key, value ? 1 : 0);
            }

            public void Save()
            {
                PlayerPrefs.Save();
            }
        }
    }
}
