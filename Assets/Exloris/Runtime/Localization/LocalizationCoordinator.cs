using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization.Settings;

namespace Exloris.Runtime.Localization
{
    public static class FoundationStringIds
    {
        public const string Loading = "foundation.loading";
        public const string Retry = "foundation.recovery.retry";
        public const string Home = "foundation.recovery.home";
        public const string ReducedMotion = "foundation.accessibility.reduced_motion";
        public const string Captions = "foundation.accessibility.captions";
        public const string HarnessReady = "foundation.harness.ready";
        public const string InputTest = "foundation.harness.input_test";
        public const string AudioTest = "foundation.harness.audio_test";
        public const string IntroVoiceCaption = "foundation.intro.caption.vo_intro_001";

        public static IReadOnlyList<string> All { get; } = new[]
        {
            Loading,
            Retry,
            Home,
            ReducedMotion,
            Captions,
            HarnessReady,
            InputTest,
            AudioTest,
            IntroVoiceCaption
        };
    }

    public static class FoundationEnglishStrings
    {
        public static IReadOnlyDictionary<string, string> ById { get; } =
            new Dictionary<string, string>
            {
                [FoundationStringIds.Loading] = "Loading",
                [FoundationStringIds.Retry] = "Try again",
                [FoundationStringIds.Home] = "Home",
                [FoundationStringIds.ReducedMotion] = "Reduced motion",
                [FoundationStringIds.Captions] = "Captions",
                [FoundationStringIds.HarnessReady] = "Foundation ready",
                [FoundationStringIds.InputTest] = "Tap the picture",
                [FoundationStringIds.AudioTest] = "Listen",
                [FoundationStringIds.IntroVoiceCaption] = "Come explore the Nile with me."
            };

        public static IReadOnlyCollection<string> Values => ById.Values.ToArray();
    }

    public enum TextFlowDirection
    {
        LeftToRight,
        RightToLeft
    }

    public interface IStringResolver
    {
        bool TryResolve(string stableId, out string value);
    }

    public sealed class DictionaryStringResolver : IStringResolver
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public DictionaryStringResolver(IReadOnlyDictionary<string, string> values)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public bool TryResolve(string stableId, out string value)
        {
            return _values.TryGetValue(stableId, out value);
        }
    }

    public sealed class UnityStringTableResolver : IStringResolver
    {
        public const string TableName = "ExlorisStringTable";

        public bool TryResolve(string stableId, out string value)
        {
            value = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, stableId);
            return !string.IsNullOrEmpty(value) && !value.StartsWith("No translation found", StringComparison.Ordinal);
        }
    }

    public sealed class LocalizationCoordinator
    {
        private static readonly HashSet<string> RtlLanguageCodes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ar", "fa", "he", "ur"
            };

        private readonly IStringResolver _resolver;

        public LocalizationCoordinator(IStringResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public string Resolve(string stableId)
        {
            if (!FoundationStringIds.All.Contains(stableId) ||
                !_resolver.TryResolve(stableId, out var value))
            {
                throw new KeyNotFoundException($"Missing approved localization ID '{stableId}'.");
            }

            return value;
        }

        public bool CanActivateLocale(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                return false;
            }

            return string.Equals(localeCode, "en", StringComparison.OrdinalIgnoreCase) ||
                   localeCode.StartsWith("en-", StringComparison.OrdinalIgnoreCase);
        }

        public static TextFlowDirection GetDirection(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                throw new ArgumentException("Locale code is required.", nameof(localeCode));
            }

            var language = localeCode.Split('-')[0];
            return RtlLanguageCodes.Contains(language)
                ? TextFlowDirection.RightToLeft
                : TextFlowDirection.LeftToRight;
        }
    }

    public sealed class LocalizedInstruction
    {
        public LocalizedInstruction(string stringId, string visualEquivalentId)
        {
            if (!FoundationStringIds.All.Contains(stringId))
            {
                throw new ArgumentException("Instruction must use an approved string ID.", nameof(stringId));
            }

            if (string.IsNullOrWhiteSpace(visualEquivalentId))
            {
                throw new ArgumentException(
                    "A non-text visual equivalent is required.",
                    nameof(visualEquivalentId));
            }

            StringId = stringId;
            VisualEquivalentId = visualEquivalentId;
        }

        public string StringId { get; }
        public string VisualEquivalentId { get; }
    }

    public static class Pseudolocalizer
    {
        private static readonly Dictionary<char, char> Accents = new Dictionary<char, char>
        {
            ['a'] = 'á', ['e'] = 'é', ['i'] = 'í', ['o'] = 'ó', ['u'] = 'ú',
            ['A'] = 'Á', ['E'] = 'É', ['I'] = 'Í', ['O'] = 'Ó', ['U'] = 'Ú'
        };

        public static string Expand(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var multiplier = source.Length <= 10 ? 2f : 1.35f;
            var targetLength = (int)Math.Ceiling(source.Length * multiplier);
            var characters = source.Select(character =>
                Accents.TryGetValue(character, out var accented) ? accented : character);
            var result = "⟦" + new string(characters.ToArray()) + "⟧";
            if (result.Length < targetLength)
            {
                result = result.Insert(result.Length - 1, new string('~', targetLength - result.Length));
            }

            return result;
        }
    }

    public static class LocalizationLayoutValidator
    {
        public static bool Fits(
            string text,
            float width,
            float height,
            float averageGlyphWidth,
            float lineHeight,
            int maximumLines)
        {
            if (string.IsNullOrEmpty(text) || width <= 0f || height <= 0f ||
                averageGlyphWidth <= 0f || lineHeight <= 0f || maximumLines <= 0)
            {
                return false;
            }

            var charactersPerLine = Math.Max(1, (int)Math.Floor(width / averageGlyphWidth));
            var requiredLines = (int)Math.Ceiling(text.Length / (double)charactersPerLine);
            var physicalLineCapacity = (int)Math.Floor(height / lineHeight);
            return requiredLines <= Math.Min(maximumLines, physicalLineCapacity);
        }
    }

    public static class FoundationFontFallbackPolicy
    {
        public const string PrimaryFontAssetId = "font-noto-sans-semibold";
        public const string SymbolFontAssetId = "font-noto-sans-math";

        public static bool RequiresVerifiedScriptFallback(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                return true;
            }

            var language = localeCode.Split('-')[0];
            return !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        }
    }
}
