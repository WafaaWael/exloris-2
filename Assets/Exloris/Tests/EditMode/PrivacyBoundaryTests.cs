using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Exloris.Core.Session;
using Exloris.Runtime.Preferences;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class PrivacyBoundaryTests
    {
        [Test]
        public void ObservationRegisterHasExactlyNineFactualFamiliesAndNoAvatarFamily()
        {
            var names = Enum.GetNames(typeof(ObservationEventType));

            Assert.That(names, Has.Length.EqualTo(9));
            Assert.That(names.Any(name => name.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }

        [Test]
        public void AvatarSelectionIsTransientAndNeverCreatesObservation()
        {
            var session = new SessionState();

            session.SelectAvatar("avatar_boy");

            Assert.That(session.SelectedAvatarId, Is.EqualTo("avatar_boy"));
            Assert.That(session.Observations, Is.Empty);

            session.Reset();

            Assert.That(session.SelectedAvatarId, Is.Null);
            Assert.That(session.Observations, Is.Empty);
        }

        [Test]
        public void SessionResetClearsPlayDataAndRestoresVolatileSoundDefault()
        {
            var session = new SessionState();
            session.SoundEnabled = false;
            session.Record(ObservationEvent.ChoiceOutcome(
                ObservationEventType.BoatRoute,
                "choice_left_bank",
                "outcome_reeds_move"));

            session.Reset();

            Assert.That(session.SoundEnabled, Is.True);
            Assert.That(session.Observations, Is.Empty);
        }

        [Test]
        public void NewSessionCannotReconstructPriorSessionData()
        {
            var first = new SessionState();
            first.SelectAvatar("avatar_girl");
            first.Record(ObservationEvent.BirdGuideUsed(2));

            var reconstructed = new SessionState();

            Assert.That(reconstructed.SelectedAvatarId, Is.Null);
            Assert.That(reconstructed.Observations, Is.Empty);
            Assert.That(typeof(SessionState).IsSerializable, Is.False);
            Assert.That(
                typeof(SessionState).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(method => method.Name == "Save" || method.Name == "Load" || method.Name == "Persist"),
                Is.False);
        }

        [Test]
        public void OnlyFiveFrozenParentTemplateFamiliesAreExposed()
        {
            var methods = typeof(ParentSummaryFormatter)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(method => method.Name)
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "FormatBirdGuideUse",
                    "FormatChoiceOutcome",
                    "FormatCompletedStages",
                    "FormatOptionsTried",
                    "FormatRevisit",
                },
                methods);
        }

        [Test]
        public void ParentReportDomainHasNoPersistenceNetworkAnalyticsIdentityOrAssessmentSurface()
        {
            var roots = new[]
            {
                Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Core", "Session", "ParentReportBuilder.cs"),
                Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime", "Adult", "ParentReportContracts.cs"),
                Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime", "Adult", "ParentReportController.cs"),
            };
            var forbidden = new[]
            {
                "PlayerPrefs", "UnityWebRequest", "System.Net.", "HttpClient", "WebClient",
                "Analytics", "Telemetry", "advertisingIdentifier", "deviceUniqueIdentifier",
                "child_id", "user_id", "account_id", "profile_id", "SelectedAvatarId",
                "RawPointer", "PointerPath", "response_time", "focus_time", "error_count",
                "support_tier", "score_", "ability_", "personality_", "diagnosis_", "ranking_",
                "comparison_", "recommendation_", "File.", "Directory.", "Reflection",
            };

            foreach (var file in roots)
            {
                var source = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(source, Does.Not.Contain(token), $"{Path.GetFileName(file)} contains {token}");
            }
        }

        [Test]
        public void FrozenParentTemplatesRenderOnlyDirectLoggedFields()
        {
            Assert.That(
                ParentSummaryFormatter.FormatChoiceOutcome(ObservationEvent.ChoiceOutcome(
                    ObservationEventType.BoatRoute,
                    "choice_left_bank",
                    "outcome_reeds_move")),
                Is.EqualTo("During this session, your child chose choice_left_bank and saw outcome_reeds_move."));
            Assert.That(
                ParentSummaryFormatter.FormatOptionsTried(ObservationEvent.OptionsTried(
                    ObservationEventType.Stage1Option,
                    "stage_1",
                    2)),
                Is.EqualTo("Your child tried 2 option(s) in stage_1 before continuing."));
            Assert.That(
                ParentSummaryFormatter.FormatBirdGuideUse(ObservationEvent.BirdGuideUsed(3)),
                Is.EqualTo("Your child used the bird guide 3 time(s) during the boat journeys."));
            Assert.That(
                ParentSummaryFormatter.FormatRevisit(ObservationEvent.Revisited("stage_1", 1)),
                Is.EqualTo("Your child revisited stage_1 1 time(s)."));
            Assert.That(
                ParentSummaryFormatter.FormatCompletedStages(ObservationEvent.CompletedStages(
                    "stage_1",
                    "stage_2",
                    "stage_3")),
                Is.EqualTo("Your child completed stage_1, stage_2, stage_3 in this session."));
        }

        [Test]
        [Property("KnownFailureId", "ASSESS-UNSUPPORTED-001")]
        public void AvatarAndInferenceTokensAreRejectedFromObservationFields()
        {
            Assert.Throws<ArgumentException>(() => ObservationEvent.ChoiceOutcome(
                ObservationEventType.BoatRoute,
                "avatar_boy",
                "outcome_reeds_move"));
            Assert.Throws<ArgumentException>(() => ObservationEvent.ChoiceOutcome(
                ObservationEventType.BoatRoute,
                "choice_avatar_boy",
                "outcome_reeds_move"));
            Assert.Throws<ArgumentException>(() => ObservationEvent.ChoiceOutcome(
                ObservationEventType.BoatRoute,
                "choice_smart",
                "outcome_reeds_move"));
            Assert.Throws<ArgumentException>(() => ObservationEvent.OptionsTried(
                ObservationEventType.Stage1Option,
                "stage_ability",
                2));
            Assert.Throws<ArgumentException>(() => ObservationEvent.OptionsTried(
                ObservationEventType.Stage1Option,
                "stage_support_tier",
                2));
            Assert.Throws<ArgumentException>(() => ObservationEvent.Revisited(
                "outcome_response_time",
                1));
            Assert.Throws<ArgumentException>(() => ObservationEvent.ChoiceOutcome(
                ObservationEventType.BoatRoute,
                "choice_previous_session",
                "outcome_reeds_move"));
        }

        [Test]
        public void ObservationEventExposesOnlyTheSixFrozenDirectFactFields()
        {
            var properties = typeof(ObservationEvent)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEqual(
                new[] { "ChoiceId", "Count", "OutcomeId", "StageIds", "SubjectId", "Type" },
                properties);
        }

        [Test]
        public void AccessibilityPreferenceStoreWritesOnlyTwoApprovedKeys()
        {
            var backend = new FakePreferenceBackend();
            var store = new AccessibilityPreferenceStore(backend);

            store.Save(new AccessibilityPreferences(alwaysReduce: true, captionsEnabled: false));

            CollectionAssert.AreEquivalent(
                new[]
                {
                    AccessibilityPreferenceStore.AlwaysReduceKey,
                    AccessibilityPreferenceStore.CaptionsEnabledKey,
                },
                backend.Keys);
            Assert.That(store.Load().AlwaysReduce, Is.True);
            Assert.That(store.Load().CaptionsEnabled, Is.False);
        }

        [Test]
        public void SessionResetDoesNotClearDeviceAccessibilityPreferences()
        {
            var backend = new FakePreferenceBackend();
            var store = new AccessibilityPreferenceStore(backend);
            store.Save(new AccessibilityPreferences(alwaysReduce: true, captionsEnabled: true));
            var session = new SessionState();

            session.Reset();

            Assert.That(store.Load().AlwaysReduce, Is.True);
            Assert.That(store.Load().CaptionsEnabled, Is.True);
        }

        [Test]
        public void AvatarRuntimeContainsNoPersistenceNetworkAnalyticsOrReportWrites()
        {
            var runtimeRoot = Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime");
            var files = Directory.GetFiles(Path.Combine(runtimeRoot, "Avatar"), "*.cs")
                .Concat(new[]
                {
                    Path.Combine(runtimeRoot, "Recovery", "LeaveConfirmationController.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "LeaveConfirmationView.cs"),
                })
                .OrderBy(path => path)
                .ToArray();
            var forbidden = new[]
            {
                "PlayerPrefs",
                "UnityWebRequest",
                "UnityEngine.Analytics",
                "Analytics.",
                "deviceUniqueIdentifier",
                "ObservationEvent",
                ".Record(",
                "ParentSummary",
                "parent_report",
                "ChildProfile",
                "PlayerProfile",
                "UserProfile",
                "profile_id",
            };

            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(source, Does.Not.Contain(token), $"{Path.GetFileName(file)} contains {token}");
            }
        }

        [Test]
        public void JourneyOverviewRuntimeContainsNoPersistenceNetworkAnalyticsObservationIdentityAssessmentOrReportSurface()
        {
            var runtimeRoot = Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime");
            var files = Directory.GetFiles(Path.Combine(runtimeRoot, "Overview"), "*.cs")
                .Concat(new[]
                {
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayContracts.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayController.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayView.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "JourneyOverviewPauseHelpController.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "JourneyOverviewPauseHelpView.cs"),
                })
                .OrderBy(path => path)
                .ToArray();
            var forbidden = new[]
            {
                "PlayerPrefs",
                "UnityWebRequest",
                "System.Net.",
                "HttpClient",
                "WebClient",
                "WebRequest",
                "System.Net.Sockets",
                "NetworkManager",
                "UnityEngine.Analytics",
                "Analytics.",
                "AnalyticsService",
                "Telemetry",
                "advertisingIdentifier",
                "deviceUniqueIdentifier",
                "userIdentifier",
                "child_id",
                "user_id",
                "account_id",
                "profile_id",
                "ChildProfile",
                "PlayerProfile",
                "UserProfile",
                "ObservationEvent",
                ".Record(",
                "ParentSummary",
                "ParentReport",
                "parent_report",
                "Assessment",
                "assessment_",
            };

            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(source, Does.Not.Contain(token),
                        $"{Path.GetFileName(file)} contains {token}");
            }
        }

        [Test]
        public void JourneyOverviewPreferenceDependencyIsRestrictedToApprovedAccessibilityComposition()
        {
            var runtimeRoot = Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime");
            var files = Directory.GetFiles(Path.Combine(runtimeRoot, "Overview"), "*.cs")
                .Concat(new[]
                {
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayContracts.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayController.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayView.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "JourneyOverviewPauseHelpController.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "JourneyOverviewPauseHelpView.cs"),
                })
                .OrderBy(path => path)
                .ToArray();

            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                Assert.That(source, Does.Not.Contain("PlayerPrefs"));
                if (!source.Contains("AccessibilityPreferenceStore")) continue;
                Assert.That(Path.GetFileName(file), Is.EqualTo("JourneyOverviewStageHost.cs")
                    .Or.EqualTo("PauseOverlayController.cs"));
            }
        }

        [Test]
        public void BoatRuntimeContainsNoPersistenceNetworkAnalyticsIdentityObservationAssessmentOrReportSurface()
        {
            var runtimeRoot = Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime");
            var files = Directory.GetFiles(Path.Combine(runtimeRoot, "Boat"), "*.cs")
                .Concat(new[]
                {
                    Path.Combine(runtimeRoot, "Recovery", "Boat1HelpController.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "Boat1HelpView.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayContracts.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayController.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "PauseOverlayView.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "LeaveConfirmationController.cs"),
                    Path.Combine(runtimeRoot, "Recovery", "LeaveConfirmationView.cs"),
                })
                .OrderBy(path => path)
                .ToArray();
            var forbidden = new[]
            {
                "PlayerPrefs", "UnityWebRequest", "System.Net.", "HttpClient", "WebClient",
                "WebRequest", "System.Net.Sockets", "NetworkManager", "UnityEngine.Analytics",
                "Analytics.", "AnalyticsService", "Telemetry", "advertisingIdentifier",
                "deviceUniqueIdentifier", "userIdentifier", "child_id", "user_id", "account_id",
                "profile_id", "ChildProfile", "PlayerProfile", "UserProfile", "ObservationEvent",
                ".Record(", "ParentSummary", "ParentReport", "parent_report", "Assessment",
                "assessment_", "Diagnosis", "diagnosis_", "Ranking", "ranking_",
            };

            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(source, Does.Not.Contain(token),
                        $"{Path.GetFileName(file)} contains {token}");
            }
        }

        [Test]
        public void Boat2RejectsForbiddenObservationAndPersistenceFields()
        {
            var runtimeRoot = Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime");
            var files = Directory.GetFiles(Path.Combine(runtimeRoot, "Boat"), "Boat2*.cs")
                .Concat(Directory.GetFiles(Path.Combine(runtimeRoot, "Recovery"), "Boat2*.cs"))
                .OrderBy(path => path)
                .ToArray();
            Assert.That(files, Is.Not.Empty);
            var forbidden = new[]
            {
                "PlayerPrefs", "UnityWebRequest", "System.Net.", "Analytics", "Telemetry",
                "ObservationEvent", ".Record(", "RawPointer", "PointerPath", "pointer_path",
                "child_id", "avatar_id", "profile_id", "ChildProfile", "PlayerProfile",
                "score", "ability", "Assessment", "Diagnosis", "Ranking", "ParentReport",
                "parent_report", "cross_session", "persistent_progress",
            };
            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(source, Does.Not.Contain(token), $"{Path.GetFileName(file)} contains {token}");
            }
        }

        [Test]
        public void Boat3RejectsForbiddenObservationAndPersistenceFields()
        {
            var runtimeRoot = Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime");
            var files = Directory.GetFiles(Path.Combine(runtimeRoot, "Boat"), "Boat3*.cs")
                .Concat(Directory.GetFiles(Path.Combine(runtimeRoot, "Recovery"), "Boat3*.cs"))
                .OrderBy(path => path)
                .ToArray();
            Assert.That(files, Is.Not.Empty);
            var forbidden = new[]
            {
                "PlayerPrefs", "UnityWebRequest", "System.Net.", "Analytics", "Telemetry",
                "ObservationEvent", ".Record(", "RawPointer", "PointerPath", "pointer_path",
                "child_id", "avatar_id", "profile_id", "ChildProfile", "PlayerProfile",
                "score", "ability", "Assessment", "Diagnosis", "Ranking", "ParentReport",
                "parent_report", "cross_session", "persistent_progress",
            };
            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(source, Does.Not.Contain(token), $"{Path.GetFileName(file)} contains {token}");
            }
        }

        [Test]
        public void Stage2RejectsForbiddenObservationAndPersistenceFields()
        {
            var runtimeRoot = Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime");
            var files = Directory.GetFiles(Path.Combine(runtimeRoot, "Stage2"), "Stage2*.cs")
                .Concat(Directory.GetFiles(Path.Combine(runtimeRoot, "Recovery"), "Stage2*.cs"))
                .OrderBy(path => path)
                .ToArray();
            Assert.That(files, Is.Not.Empty);
            var forbidden = new[]
            {
                "PlayerPrefs", "UnityWebRequest", "System.Net.", "Analytics", "Telemetry",
                "RawPointer", "PointerPath", "pointer_path", "pointer_coordinate",
                "child_id", "avatar_id", "profile_id", "ChildProfile", "PlayerProfile",
                "score", "ability", "Assessment", "Diagnosis", "Ranking", "ParentReport",
                "parent_report", "cross_session", "persistent_progress",
            };
            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(source, Does.Not.Contain(token), $"{Path.GetFileName(file)} contains {token}");
            }
        }

        [Test]
        public void Stage3RejectsForbiddenObservationAndPersistenceFields()
        {
            var runtimeRoot = Path.Combine(UnityEngine.Application.dataPath, "Exloris", "Runtime");
            var files = Directory.GetFiles(Path.Combine(runtimeRoot, "Stage3"), "Stage3*.cs")
                .Concat(Directory.GetFiles(Path.Combine(runtimeRoot, "Recovery"), "Stage3*.cs"))
                .OrderBy(path => path)
                .ToArray();
            Assert.That(files, Is.Not.Empty);
            var forbidden = new[]
            {
                "PlayerPrefs", "UnityWebRequest", "System.Net.", "Analytics", "Telemetry",
                "RawPointer", "PointerPath", "pointer_path", "pointer_coordinate",
                "child_id", "avatar_id", "profile_id", "ChildProfile", "PlayerProfile",
                "score", "ability", "Assessment", "Diagnosis", "Ranking", "ParentReport",
                "parent_report", "cross_session", "persistent_progress",
            };
            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(source, Does.Not.Contain(token), $"{Path.GetFileName(file)} contains {token}");
            }
        }

        private sealed class FakePreferenceBackend : IPreferenceBackend
        {
            private readonly Dictionary<string, bool> _values = new Dictionary<string, bool>();

            public IEnumerable<string> Keys => _values.Keys;

            public bool GetBool(string key, bool defaultValue)
            {
                return _values.TryGetValue(key, out var value) ? value : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _values[key] = value;
            }

            public void Save()
            {
            }
        }
    }
}
