using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Exloris.Editor.Validation
{
    public sealed class ReleaseKnownFailureProbe
    {
        public IReadOnlyList<string> FullMotionFrameSignatures { get; set; }
        public IReadOnlyCollection<string> ExpectedControls { get; set; }
        public IReadOnlyCollection<string> ActualControls { get; set; }
        public IReadOnlyCollection<string> EnabledControls { get; set; }
        public IReadOnlyCollection<string> BoundControls { get; set; }
        public IReadOnlyList<string> Route { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float TouchTargetGraphicAlpha { get; set; }
        public Rect TouchTarget { get; set; }
        public Rect VisibleArt { get; set; }
        public Rect SafeViewport { get; set; }
        public Rect MeaningfulContent { get; set; }
        public IReadOnlyList<Rect> TouchTargets { get; set; }
        public float MinimumTouchSpacing { get; set; }
        public string ShaderName { get; set; }
        public int PinkPixelCount { get; set; }
        public IReadOnlyDictionary<string, string> ExpectedPackages { get; set; }
        public IReadOnlyDictionary<string, string> ActualPackages { get; set; }
        public int AcceptedNavigationTransactions { get; set; }
        public bool StaleCallbackAccepted { get; set; }
        public IReadOnlyCollection<string> ExpectedAudioIds { get; set; }
        public IReadOnlyCollection<string> ActualAudioIds { get; set; }
        public IReadOnlyCollection<string> ApprovedMusicHashes { get; set; }
        public string MusicHash { get; set; }
        public double FullVideoSeconds { get; set; }
        public double ReducedVideoSeconds { get; set; }
        public int SkipVisibleFrame { get; set; }
        public IReadOnlyCollection<string> ObservationTokens { get; set; }
        public string AssetStatus { get; set; }
        public string AssetName { get; set; }
        public string AssetHash { get; set; }
    }

    public static class ReleaseKnownFailureValidator
    {
        private const float GeometryEpsilon = 0.01f;
        private static readonly string[] FailureIds =
        {
            "AT-STATIC-001",
            "CTRL-MISSING-001",
            "CTRL-DEAD-001",
            "ROUTE-WRONG-001",
            "LAYOUT-PORTRAIT-001",
            "TOUCH-VISIBLE-001",
            "TOUCH-MISMATCH-001",
            "CROP-001",
            "TOUCH-OVERLAP-001",
            "MATERIAL-PINK-001",
            "PACKAGE-001",
            "NAV-DUP-001",
            "CALLBACK-STALE-001",
            "AUDIO-MISSING-001",
            "MUSIC-ADULT-001",
            "VIDEO-LONG-001",
            "ASSESS-UNSUPPORTED-001",
            "PLACEHOLDER-001",
        };

        private static readonly string[] RouteIds =
        {
            "startup_loading",
            "intro_video",
            "welcome",
            "avatar_select",
            "avatar_preview",
            "journey_overview",
            "boat_1",
            "stage_1",
            "boat_2",
            "stage_2",
            "boat_3",
            "stage_3",
            "completion",
            "adult_gate",
            "parent_report",
        };

        private static readonly string[] ForbiddenAssessmentTokens =
        {
            "score", "rank", "diagnosis", "assessment", "inference", "profile",
        };

        public static IReadOnlyList<string> CanonicalIds => FailureIds;
        public static IReadOnlyList<string> RequiredRoute => RouteIds;

        public static IReadOnlyList<string> Detect(ReleaseKnownFailureProbe probe)
        {
            if (probe == null) throw new ArgumentNullException(nameof(probe));
            var detected = new List<string>();

            if (probe.FullMotionFrameSignatures == null
                || probe.FullMotionFrameSignatures.Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.Ordinal).Count() < 2)
                detected.Add("AT-STATIC-001");

            if (!ContainsAll(probe.ActualControls, probe.ExpectedControls))
                detected.Add("CTRL-MISSING-001");
            if (!ContainsAll(probe.BoundControls, probe.EnabledControls))
                detected.Add("CTRL-DEAD-001");
            if (probe.Route == null || !probe.Route.SequenceEqual(RouteIds, StringComparer.Ordinal))
                detected.Add("ROUTE-WRONG-001");
            if (probe.Width <= 0 || probe.Height <= 0 || probe.Height >= probe.Width)
                detected.Add("LAYOUT-PORTRAIT-001");
            if (probe.TouchTargetGraphicAlpha > GeometryEpsilon)
                detected.Add("TOUCH-VISIBLE-001");
            if (!Contains(probe.TouchTarget, probe.VisibleArt))
                detected.Add("TOUCH-MISMATCH-001");
            if (!Contains(probe.SafeViewport, probe.MeaningfulContent))
                detected.Add("CROP-001");
            if (HasOverlapOrInsufficientSpacing(probe.TouchTargets, probe.MinimumTouchSpacing))
                detected.Add("TOUCH-OVERLAP-001");
            if (probe.PinkPixelCount > 0
                || string.IsNullOrWhiteSpace(probe.ShaderName)
                || probe.ShaderName.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0)
                detected.Add("MATERIAL-PINK-001");
            if (!DictionariesEqual(probe.ExpectedPackages, probe.ActualPackages))
                detected.Add("PACKAGE-001");
            if (probe.AcceptedNavigationTransactions != 1)
                detected.Add("NAV-DUP-001");
            if (probe.StaleCallbackAccepted)
                detected.Add("CALLBACK-STALE-001");
            if (!ContainsAll(probe.ActualAudioIds, probe.ExpectedAudioIds))
                detected.Add("AUDIO-MISSING-001");
            if (string.IsNullOrWhiteSpace(probe.MusicHash)
                || probe.ApprovedMusicHashes == null
                || !probe.ApprovedMusicHashes.Contains(probe.MusicHash, StringComparer.OrdinalIgnoreCase))
                detected.Add("MUSIC-ADULT-001");
            if (probe.FullVideoSeconds > 9.6d + 0.000001d
                || probe.ReducedVideoSeconds > 4d + 0.000001d
                || probe.SkipVisibleFrame > 1)
                detected.Add("VIDEO-LONG-001");
            if (ContainsForbiddenAssessmentToken(probe.ObservationTokens))
                detected.Add("ASSESS-UNSUPPORTED-001");
            if (!string.Equals(probe.AssetStatus, "Final", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(probe.AssetHash)
                || ContainsAny(probe.AssetName, "placeholder", "draft", "temp"))
                detected.Add("PLACEHOLDER-001");

            return detected;
        }

        private static bool ContainsAll(
            IReadOnlyCollection<string> actual,
            IReadOnlyCollection<string> expected)
        {
            if (actual == null || expected == null) return false;
            var values = new HashSet<string>(actual, StringComparer.Ordinal);
            return expected.All(values.Contains);
        }

        private static bool Contains(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - GeometryEpsilon
            && inner.xMax <= outer.xMax + GeometryEpsilon
            && inner.yMin >= outer.yMin - GeometryEpsilon
            && inner.yMax <= outer.yMax + GeometryEpsilon;

        private static bool HasOverlapOrInsufficientSpacing(IReadOnlyList<Rect> targets, float spacing)
        {
            if (targets == null || targets.Count == 0 || spacing < 0f) return true;
            var margin = spacing * 0.5f;
            for (var left = 0; left < targets.Count; left++)
            {
                var expandedLeft = Expand(targets[left], margin);
                for (var right = left + 1; right < targets.Count; right++)
                {
                    if (expandedLeft.Overlaps(Expand(targets[right], margin), true)) return true;
                }
            }
            return false;
        }

        private static Rect Expand(Rect rect, float amount) =>
            new Rect(rect.xMin - amount, rect.yMin - amount,
                rect.width + amount * 2f, rect.height + amount * 2f);

        private static bool DictionariesEqual(
            IReadOnlyDictionary<string, string> expected,
            IReadOnlyDictionary<string, string> actual)
        {
            if (expected == null || actual == null || expected.Count != actual.Count) return false;
            foreach (var pair in expected)
            {
                if (!actual.TryGetValue(pair.Key, out var value)
                    || !string.Equals(pair.Value, value, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static bool ContainsForbiddenAssessmentToken(IEnumerable<string> tokens)
        {
            if (tokens == null) return true;
            return tokens.Any(token => ForbiddenAssessmentTokens.Any(forbidden =>
                ContainsAny(token, forbidden)));
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return needles.Any(needle => value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
