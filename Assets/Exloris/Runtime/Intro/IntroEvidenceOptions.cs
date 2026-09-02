using System;
using System.Globalization;

namespace Exloris.Runtime.Intro
{
    public readonly struct IntroEvidenceCaptureOptions
    {
        public IntroEvidenceCaptureOptions(
            string path,
            double atSeconds,
            bool captionsEnabled,
            bool muted)
        {
            Path = path;
            AtSeconds = atSeconds;
            CaptionsEnabled = captionsEnabled;
            Muted = muted;
        }

        public string Path { get; }
        public double AtSeconds { get; }
        public bool CaptionsEnabled { get; }
        public bool Muted { get; }
    }

    public static class IntroEvidenceOptions
    {
        public static bool TryGetReducedMotion(string[] arguments, out bool reducedMotion)
        {
            reducedMotion = true;
            if (!TryGetValue(arguments, "-introEvidenceMotion", out var value)) return false;
            if (string.Equals(value, "full", StringComparison.OrdinalIgnoreCase))
            {
                reducedMotion = false;
                return true;
            }

            if (string.Equals(value, "reduced", StringComparison.OrdinalIgnoreCase))
            {
                reducedMotion = true;
                return true;
            }

            return false;
        }

        public static bool TryGetCapture(
            string[] arguments,
            out IntroEvidenceCaptureOptions capture)
        {
            capture = default;
            if (!TryGetValue(arguments, "-introCapture", out var path) || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var atSeconds = 0d;
            if (TryGetValue(arguments, "-introCaptureAt", out var atValue) &&
                !double.TryParse(atValue, NumberStyles.Float, CultureInfo.InvariantCulture, out atSeconds))
            {
                return false;
            }

            if (atSeconds < 0d) return false;
            capture = new IntroEvidenceCaptureOptions(
                path,
                atSeconds,
                Contains(arguments, "-introCaptions"),
                Contains(arguments, "-introMuted"));
            return true;
        }

        private static bool TryGetValue(string[] arguments, string option, out string value)
        {
            value = null;
            if (arguments == null) return false;
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (!string.Equals(arguments[index], option, StringComparison.Ordinal)) continue;
                value = arguments[index + 1];
                return true;
            }

            return false;
        }

        private static bool Contains(string[] arguments, string option)
        {
            if (arguments == null) return false;
            foreach (var value in arguments)
            {
                if (string.Equals(value, option, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }
}
