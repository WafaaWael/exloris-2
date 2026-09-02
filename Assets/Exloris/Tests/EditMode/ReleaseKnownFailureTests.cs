using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Editor.Validation;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class ReleaseKnownFailureTests
    {
        private static IEnumerable<string> KnownFailureIds =>
            ReleaseKnownFailureValidator.CanonicalIds;

        [TestCaseSource(nameof(KnownFailureIds))]
        public void EachCanonicalInjectedFailureIsDetectedExactlyOnce(string failureId)
        {
            var probe = PassingProbe();
            Inject(probe, failureId);

            Assert.That(
                ReleaseKnownFailureValidator.Detect(probe),
                Is.EqualTo(new[] { failureId }));
        }

        [Test]
        public void CanonicalRegisterContainsTheExactEighteenFrozenIds()
        {
            Assert.That(ReleaseKnownFailureValidator.CanonicalIds.Count, Is.EqualTo(18));
            Assert.That(ReleaseKnownFailureValidator.CanonicalIds.Distinct().Count(), Is.EqualTo(18));
            Assert.That(ReleaseKnownFailureValidator.CanonicalIds, Is.EqualTo(new[]
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
            }));
        }

        private static ReleaseKnownFailureProbe PassingProbe()
        {
            return new ReleaseKnownFailureProbe
            {
                FullMotionFrameSignatures = new[] { "frame-a", "frame-b" },
                ExpectedControls = new[] { "play" },
                ActualControls = new[] { "play" },
                EnabledControls = new[] { "play" },
                BoundControls = new[] { "play" },
                Route = ReleaseKnownFailureValidator.RequiredRoute.ToArray(),
                Width = 1920,
                Height = 1080,
                TouchTargetGraphicAlpha = 0f,
                TouchTarget = new Rect(0f, 0f, 100f, 100f),
                VisibleArt = new Rect(25f, 25f, 50f, 50f),
                SafeViewport = new Rect(0f, 0f, 100f, 100f),
                MeaningfulContent = new Rect(25f, 25f, 50f, 50f),
                TouchTargets = new[]
                {
                    new Rect(0f, 0f, 100f, 100f),
                    new Rect(120f, 0f, 100f, 100f),
                },
                MinimumTouchSpacing = 10f,
                ShaderName = "Universal Render Pipeline/Unlit",
                PinkPixelCount = 0,
                ExpectedPackages = new Dictionary<string, string> { { "com.unity.inputsystem", "1.11.2" } },
                ActualPackages = new Dictionary<string, string> { { "com.unity.inputsystem", "1.11.2" } },
                AcceptedNavigationTransactions = 1,
                StaleCallbackAccepted = false,
                ExpectedAudioIds = new[] { "voice" },
                ActualAudioIds = new[] { "voice" },
                ApprovedMusicHashes = new[] { "approved-music" },
                MusicHash = "approved-music",
                FullVideoSeconds = 9.6d,
                ReducedVideoSeconds = 4d,
                SkipVisibleFrame = 1,
                ObservationTokens = new[] { "choice", "outcome", "completed" },
                AssetStatus = "Final",
                AssetName = "approved_asset",
                AssetHash = "0123456789abcdef",
            };
        }

        private static void Inject(ReleaseKnownFailureProbe probe, string failureId)
        {
            switch (failureId)
            {
                case "AT-STATIC-001":
                    probe.FullMotionFrameSignatures = new[] { "same", "same" };
                    break;
                case "CTRL-MISSING-001":
                    probe.ActualControls = Array.Empty<string>();
                    probe.EnabledControls = Array.Empty<string>();
                    probe.BoundControls = Array.Empty<string>();
                    break;
                case "CTRL-DEAD-001":
                    probe.BoundControls = Array.Empty<string>();
                    break;
                case "ROUTE-WRONG-001":
                    probe.Route = ReleaseKnownFailureValidator.RequiredRoute.Where(route => route != "stage_1").ToArray();
                    break;
                case "LAYOUT-PORTRAIT-001":
                    probe.Width = 1080;
                    probe.Height = 1920;
                    break;
                case "TOUCH-VISIBLE-001":
                    probe.TouchTargetGraphicAlpha = 1f;
                    break;
                case "TOUCH-MISMATCH-001":
                    probe.VisibleArt = new Rect(-1f, 25f, 50f, 50f);
                    break;
                case "CROP-001":
                    probe.MeaningfulContent = new Rect(-1f, 25f, 50f, 50f);
                    break;
                case "TOUCH-OVERLAP-001":
                    probe.TouchTargets = new[]
                    {
                        new Rect(0f, 0f, 100f, 100f),
                        new Rect(95f, 0f, 100f, 100f),
                    };
                    break;
                case "MATERIAL-PINK-001":
                    probe.ShaderName = "Hidden/InternalErrorShader";
                    break;
                case "PACKAGE-001":
                    probe.ActualPackages = new Dictionary<string, string>
                    {
                        { "com.unity.inputsystem", "9.9.9" },
                    };
                    break;
                case "NAV-DUP-001":
                    probe.AcceptedNavigationTransactions = 2;
                    break;
                case "CALLBACK-STALE-001":
                    probe.StaleCallbackAccepted = true;
                    break;
                case "AUDIO-MISSING-001":
                    probe.ActualAudioIds = Array.Empty<string>();
                    break;
                case "MUSIC-ADULT-001":
                    probe.MusicHash = "unapproved-music";
                    break;
                case "VIDEO-LONG-001":
                    probe.FullVideoSeconds = 10d;
                    break;
                case "ASSESS-UNSUPPORTED-001":
                    probe.ObservationTokens = new[] { "score" };
                    break;
                case "PLACEHOLDER-001":
                    probe.AssetStatus = "Placeholder";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(failureId), failureId, null);
            }
        }
    }
}
