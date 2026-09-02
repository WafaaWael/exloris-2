using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Exloris.Core.Navigation;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using Exloris.Runtime.UI;
using Exloris.Runtime.Welcome;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class ReleaseCandidateEvidenceCapture : MonoBehaviour
    {
        private static readonly HashSet<string> Targets = new HashSet<string>(StringComparer.Ordinal)
        {
            "startup_loading", "intro_video", "welcome", "avatar_select", "avatar_preview",
            "journey_overview", "boat_1", "stage_1", "boat_2", "stage_2", "boat_3",
            "stage_3", "completion", "adult_gate", "parent_report",
            "avatar_recovery", "journey_pause", "journey_leave", "boat_1_pause",
            "stage_1_help", "stage_2_leave", "stage_3_pause", "completion_pause",
            "adult_half", "parent_report_expanded",
        };

        private readonly List<string> _routeTrace = new List<string>();
        private readonly List<float> _soakFrameMilliseconds = new List<float>(120000);
        private ScreenHost _host;
        private Mouse _physicalMouse;
        private Touchscreen _touchscreen;
        private int _touchSequence;
        private Func<bool> _quitAudit;
        private bool _failed;
        private bool _soakMode;
        private bool _soakPerformanceSampling;
        private bool _soakReachedFinalRoute;
        private bool _soakRouteDrift;
        private double _soakStartRealtime;
        private double _soakPerformanceSamplingStartElapsedSeconds;
        private long _maximumAllocatedBytes;
        private long _maximumReservedBytes;
        private int _maximumActiveVoiceCount;
        private int _pauseCount;
        private int _audioInterruptionCount;
        private bool _originalRunInBackground;
        private bool _changedRunInBackground;

        private void Awake()
        {
            _host = GetComponent<ScreenHost>();
            var arguments = Environment.GetCommandLineArgs();
            if (ShouldRunInBackgroundForEvidence(arguments))
            {
                _originalRunInBackground = Application.runInBackground;
                _changedRunInBackground = true;
                Application.runInBackground = true;
                InitializeEvidenceInputForTests();
                _quitAudit = AuditQuitRequest;
                Application.wantsToQuit += _quitAudit;
            }
        }

        public static bool ShouldRunInBackgroundForEvidence(string[] arguments)
        {
            return arguments != null
                && Array.IndexOf(arguments, "-releaseCapture") >= 0;
        }

        public static bool ShouldSampleSoakFrame(bool soakMode, bool resolutionWarmupComplete)
        {
            return soakMode && resolutionWarmupComplete;
        }

        public void InitializeEvidenceInputForTests()
        {
            if (_touchscreen != null) return;
            _physicalMouse = Mouse.current;
            if (_physicalMouse != null && _physicalMouse.added && _physicalMouse.enabled)
                InputSystem.DisableDevice(_physicalMouse);
            _touchscreen = InputSystem.AddDevice<Touchscreen>("ReleaseEvidenceTouchscreen");
        }

        public static bool ShouldHoldStartupLoading(string[] arguments)
        {
            if (arguments == null) return false;
            var hasCapture = false;
            var target = string.Empty;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (string.Equals(arguments[index], "-releaseCapture", StringComparison.Ordinal)
                    && index + 1 < arguments.Length)
                {
                    hasCapture = true;
                }
                else if (string.Equals(arguments[index], "-releaseTarget", StringComparison.Ordinal)
                    && index + 1 < arguments.Length)
                {
                    target = arguments[index + 1];
                }
            }
            return hasCapture && string.Equals(target, "startup_loading", StringComparison.Ordinal);
        }

        private void OnDestroy()
        {
            if (_quitAudit != null) Application.wantsToQuit -= _quitAudit;
            if (_touchscreen != null && _touchscreen.added) InputSystem.RemoveDevice(_touchscreen);
            if (_physicalMouse != null && _physicalMouse.added && !_physicalMouse.enabled)
                InputSystem.EnableDevice(_physicalMouse);
            if (_changedRunInBackground) Application.runInBackground = _originalRunInBackground;
        }

        private bool AuditQuitRequest()
        {
            Debug.LogWarning("RELEASE_QUIT_REQUEST elapsed_seconds="
                + (Time.realtimeSinceStartupAsDouble - _soakStartRealtime)
                    .ToString("F3", CultureInfo.InvariantCulture)
                + " route=" + (_host == null ? "<none>" : _host.CurrentRoute.ToString())
                + " last_control=" + (_host?.LastAcceptedControlId ?? string.Empty)
                + " stack=" + Environment.StackTrace.Replace('\n', '|').Replace('\r', '|'));
            return true;
        }

        private void OnApplicationQuit()
        {
            Debug.LogWarning("RELEASE_ON_APPLICATION_QUIT elapsed_seconds="
                + (Time.realtimeSinceStartupAsDouble - _soakStartRealtime)
                    .ToString("F3", CultureInfo.InvariantCulture)
                + " route=" + (_host == null ? "<none>" : _host.CurrentRoute.ToString()));
        }

        private IEnumerator Start()
        {
            if (!TryOptions(Environment.GetCommandLineArgs(), out var options)) yield break;
            _soakMode = options.SoakSeconds > 0d;
            _soakStartRealtime = Time.realtimeSinceStartupAsDouble;
            var resolution = ResolutionFor(options.Ratio);
            Screen.SetResolution(resolution.x, resolution.y, false);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            yield return null;
            RecordRoute();
            if (_soakMode)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                _soakPerformanceSampling = true;
                _soakPerformanceSamplingStartElapsedSeconds =
                    Time.realtimeSinceStartupAsDouble - _soakStartRealtime;
            }

            if (options.Target == "startup_loading")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return WaitFor(() => _host.CurrentRoute == RouteId.IntroVideo, 12f, "intro_video");
            if (_failed) yield break;
            RecordRoute();
            if (options.Target == "intro_video")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveIntroView.SkipScreenBounds.center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.Welcome && _host.WelcomeInputReady,
                8f, "welcome");
            if (_failed) yield break;
            RecordRoute();
            if (options.PersistencePhase == "verify")
            { yield return CaptureAndQuit(options, resolution); yield break; }
            _host.ConfigureWelcomeEvidenceForDiagnostics(
                options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);
            if (options.Target == "welcome")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveWelcomeView.GetControlScreenBounds(
                WelcomeStageView.PlayControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.AvatarSelect && _host.AvatarInputReady,
                5f, "avatar_select");
            if (_failed) yield break;
            RecordRoute();
            if (options.Target == "avatar_recovery")
            {
                if (!_host.PresentAvatarRecoveryForDiagnostics())
                { Fail("avatar recovery injection was rejected", 11); yield break; }
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "avatar_select")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveAvatarSelectView.GetControlScreenBounds(
                options.Child == "girl" ? AvatarSelectView.GirlControlId : AvatarSelectView.BoyControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.AvatarPreview && _host.AvatarInputReady,
                5f, "avatar_preview");
            if (_failed) yield break;
            RecordRoute();
            if (options.Target == "avatar_preview")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveAvatarPreviewView.GetControlScreenBounds(
                AvatarPreviewView.ConfirmControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.JourneyOverview && _host.JourneyInputReady,
                5f, "journey_overview");
            if (_failed) yield break;
            RecordRoute();
            if (_soakMode)
            {
                yield return ExerciseInterruption(false);
                if (_failed) yield break;
            }
            if (options.Target == "journey_pause")
            {
                yield return Click(_host.ActiveJourneyOverviewView.GetControlScreenBounds(
                    JourneyOverviewStageView.PauseControlId).center);
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "journey_leave")
            {
                yield return Click(_host.ActiveJourneyOverviewView.GetControlScreenBounds(
                    JourneyOverviewStageView.HomeControlId).center);
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "journey_overview")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveJourneyOverviewView.GetControlScreenBounds(
                JourneyOverviewStageView.SkipControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.Boat1 && _host.Boat1InputReady,
                5f, "boat_1");
            if (_failed) yield break;
            RecordRoute();
            if (_soakMode)
            {
                yield return ExerciseInterruption(true);
                if (_failed) yield break;
            }
            if (options.Target == "boat_1_pause")
            {
                yield return Click(_host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.PauseControlId).center);
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "boat_1")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.ReedsControlId).center);
            yield return Click(_host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.Stage1 && _host.Stage1InputReady,
                6f, "stage_1");
            if (_failed) yield break;
            RecordRoute();
            if (_soakMode)
            {
                yield return ExerciseInterruption(false);
                if (_failed) yield break;
            }
            if (options.Target == "stage_1_help")
            {
                yield return Click(_host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
                yield return Click(_host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.HelpControlId).center);
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "stage_1")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
            yield return Click(_host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center);
            yield return Click(_host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center);
            yield return Click(_host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ConfirmControlId).center);
            yield return WaitFor(() => _host.Stage1Snapshot.State == Stage1State.OutcomeReady, 6f, "stage_1_outcome");
            if (_failed) yield break;
            yield return Click(_host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ContinueControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.Boat2 && _host.Boat2InputReady,
                6f, "boat_2");
            if (_failed) yield break;
            RecordRoute();
            if (_soakMode)
            {
                yield return ExerciseInterruption(true);
                if (_failed) yield break;
            }
            if (options.Target == "boat_2")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.DockControlId).center);
            yield return Click(_host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.SkipControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.Stage2 && _host.Stage2InputReady,
                6f, "stage_2");
            if (_failed) yield break;
            RecordRoute();
            if (_soakMode)
            {
                yield return ExerciseInterruption(false);
                if (_failed) yield break;
            }
            if (options.Target == "stage_2_leave")
            {
                yield return Click(_host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.HomeControlId).center);
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "stage_2")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.PlayControlId).center);
            yield return Click(_host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.MiddleControlId).center);
            foreach (var id in new[]
            {
                Stage2StageView.FieldControlId, Stage2StageView.PathControlId, Stage2StageView.BoatControlId,
            })
            {
                yield return Click(_host.ActiveStage2View.GetControlScreenBounds(id).center);
                yield return WaitFor(() => _host.Stage2Snapshot.State == Stage2State.Inspecting,
                    5f, "stage_2_consequence");
                if (_failed) yield break;
            }
            yield return Click(_host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.FinishControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.Boat3 && _host.Boat3InputReady,
                6f, "boat_3");
            if (_failed) yield break;
            RecordRoute();
            if (_soakMode)
            {
                yield return ExerciseInterruption(true);
                if (_failed) yield break;
            }
            if (options.Target == "boat_3")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.GrainStoreControlId).center);
            yield return Click(_host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.SkipControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.Stage3 && _host.Stage3InputReady,
                6f, "stage_3");
            if (_failed) yield break;
            RecordRoute();
            if (_soakMode)
            {
                yield return ExerciseInterruption(false);
                if (_failed) yield break;
            }
            if (options.Target == "stage_3_pause")
            {
                yield return Click(_host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.PauseControlId).center);
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "stage_3")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.PlayControlId).center);
            yield return Click(_host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.GrainControlId).center);
            yield return WaitFor(() => _host.Stage3Snapshot.State == Stage3State.OutcomeReady,
                10f, "stage_3_outcome");
            if (_failed) yield break;
            yield return Click(_host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.FinishControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.Completion
                && _host.ClosingCompletionSnapshot.State == CompletionState.Ready,
                8f, "completion");
            if (_failed) yield break;
            RecordRoute();
            if (_soakMode)
            {
                yield return ExerciseInterruption(true);
                if (_failed) yield break;
            }
            if (options.Target == "completion_pause")
            {
                yield return Click(_host.ActiveCompletionView.GetControlScreenBounds(
                    CompletionStageView.PauseControlId).center);
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "completion")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Click(_host.ActiveCompletionView.GetControlScreenBounds(
                CompletionStageView.AdultControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.AdultGate
                && _host.ActiveAdultGateView != null, 5f, "adult_gate");
            if (_failed) yield break;
            RecordRoute();
            if (options.Target == "adult_half")
            {
                yield return BeginHold(_host.ActiveAdultGateView.GetControlScreenBounds(
                    AdultGateView.HoldControlId).center);
                yield return new WaitForSecondsRealtime(1.5f);
                yield return CaptureAndQuit(options, resolution); yield break;
            }
            if (options.Target == "adult_gate")
            { yield return CaptureAndQuit(options, resolution); yield break; }

            yield return Hold(_host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center, 3.1d);
            yield return WaitFor(() => _host.ClosingAdultSnapshot.State == AdultGateState.AwaitingConfirm,
                5f, "adult_confirm");
            if (_failed) yield break;
            yield return Click(_host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);
            yield return WaitFor(() => _host.CurrentRoute == RouteId.ParentReport
                && _host.ActiveParentReportView != null, 5f, "parent_report");
            if (_failed) yield break;
            RecordRoute();
            if (options.Target == "parent_report_expanded")
            {
                if (!_host.ActiveParentReportView.TryApplyResponsiveLayout(
                    resolution.x, resolution.y, resolution.x, resolution.y, 326f, 88,
                    options.TextScale, 1f + options.PseudoPercent / 100f))
                { Fail("expanded Parent Report layout is infeasible", 12); yield break; }
            }
            if (_soakMode)
            {
                yield return SoakAndQuit(options, resolution);
                yield break;
            }
            yield return CaptureAndQuit(options, resolution);
        }

        private void Update()
        {
            if (!_soakMode) return;
            if (ShouldSampleSoakFrame(_soakMode, _soakPerformanceSampling))
            {
                var milliseconds = Time.unscaledDeltaTime * 1000f;
                if (milliseconds >= 0f && !float.IsNaN(milliseconds) && !float.IsInfinity(milliseconds))
                    _soakFrameMilliseconds.Add(milliseconds);
            }
            _maximumActiveVoiceCount = Math.Max(_maximumActiveVoiceCount, _host.ActiveVoiceCount);
            if (Time.frameCount % 60 == 0)
            {
                _maximumAllocatedBytes = Math.Max(
                    _maximumAllocatedBytes, Profiler.GetTotalAllocatedMemoryLong());
                _maximumReservedBytes = Math.Max(
                    _maximumReservedBytes, Profiler.GetTotalReservedMemoryLong());
            }
            if (_soakReachedFinalRoute && !_soakRouteDrift
                && _host.CurrentRoute != RouteId.ParentReport)
            {
                _soakRouteDrift = true;
                Debug.LogError("RELEASE_SOAK_ROUTE_DRIFT route=" + _host.CurrentRoute
                    + " last_control=" + (_host.LastAcceptedControlId ?? string.Empty));
            }
        }

        private IEnumerator ExerciseInterruption(bool audioFocus)
        {
            if (audioFocus)
            {
                _host.HandleAudioRouteChangeForTests();
                _audioInterruptionCount++;
            }
            else
            {
                _host.SendMessage("OnApplicationPause", true);
                _pauseCount++;
            }
            yield return null;
            if (_host.ActivePauseOverlayView == null || !_host.ActivePauseOverlayView.IsVisible)
            {
                Fail((audioFocus ? "audio route change" : "app pause")
                    + " did not produce the mapped Pause overlay", audioFocus ? 22 : 21);
                yield break;
            }
            _host.HandleFocusRestoredForTests();
            yield return null;
            yield return Click(_host.ActivePauseOverlayView.GetControlScreenBounds(
                PauseOverlayView.ResumeControlId).center);
            if (!audioFocus) _host.SendMessage("OnApplicationPause", false);
            yield return WaitFor(() => !_host.ActivePauseOverlayView.IsVisible
                && !_host.AudioInterrupted, 2f,
                audioFocus ? "audio interruption resume" : "app pause resume");
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private IEnumerator SoakAndQuit(Options options, Vector2Int resolution)
        {
            _soakReachedFinalRoute = true;
            var nextHeartbeat = 60d;
            while (Time.realtimeSinceStartupAsDouble - _soakStartRealtime < options.SoakSeconds)
            {
                var elapsed = Time.realtimeSinceStartupAsDouble - _soakStartRealtime;
                if (elapsed >= nextHeartbeat)
                {
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "RELEASE_SOAK_HEARTBEAT elapsed_seconds={0:F1} route={1} reserved_mib={2:F2} active_voices={3}",
                        elapsed, _host.CurrentRoute,
                        _maximumReservedBytes / (1024f * 1024f), _host.ActiveVoiceCount));
                    nextHeartbeat += 60d;
                }
                yield return null;
            }

            _maximumAllocatedBytes = Math.Max(
                _maximumAllocatedBytes, Profiler.GetTotalAllocatedMemoryLong());
            _maximumReservedBytes = Math.Max(
                _maximumReservedBytes, Profiler.GetTotalReservedMemoryLong());
            yield return new WaitForEndOfFrame();
            WriteCapture(options, resolution);
            var elapsedSeconds = Time.realtimeSinceStartupAsDouble - _soakStartRealtime;
            var report = FoundationPerformanceMetrics.Summarize(
                _soakFrameMilliseconds, _maximumAllocatedBytes, _maximumReservedBytes,
                resolution.x, resolution.y);
            var passed = ReleaseSoakGate.Passes(
                elapsedSeconds, _routeTrace.Count, _pauseCount, _audioInterruptionCount,
                report.P95FrameMilliseconds, report.P99FrameMilliseconds,
                report.MaximumConsecutiveFramesOver33Milliseconds,
                report.ReservedMemoryMiB, _maximumActiveVoiceCount, _soakRouteDrift);
            var record = new SoakRecord
            {
                started_utc = DateTime.UtcNow.AddSeconds(-elapsedSeconds).ToString("O", CultureInfo.InvariantCulture),
                completed_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                elapsed_seconds = elapsedSeconds,
                required_seconds = ReleaseSoakGate.RequiredSeconds,
                performance_sampling_started_elapsed_seconds =
                    _soakPerformanceSamplingStartElapsedSeconds,
                frame_samples = report.SampleCount,
                average_frame_ms = report.AverageFrameMilliseconds,
                p95_frame_ms = report.P95FrameMilliseconds,
                p99_frame_ms = report.P99FrameMilliseconds,
                maximum_frame_ms = report.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms = report.MaximumConsecutiveFramesOver33Milliseconds,
                maximum_allocated_mib = report.AllocatedMemoryMiB,
                maximum_reserved_mib = report.ReservedMemoryMiB,
                route_count = _routeTrace.Count,
                route_trace = string.Join(">", _routeTrace),
                final_route = _host.CurrentRoute.ToString(),
                pause_count = _pauseCount,
                audio_interruption_count = _audioInterruptionCount,
                maximum_active_voice_count = _maximumActiveVoiceCount,
                audio_overlap_detected = _maximumActiveVoiceCount > 1,
                route_drift_detected = _soakRouteDrift,
                replay_destination_seen = false,
                executable_sha256 = options.ExecutableSha256,
                result = passed ? "PASS" : "FAIL",
            };
            var directory = Path.GetDirectoryName(options.SoakPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(options.SoakPath, JsonUtility.ToJson(record, true));
            Debug.Log((passed ? "RELEASE_SOAK_PASS" : "RELEASE_SOAK_FAIL")
                + " elapsed_seconds=" + elapsedSeconds.ToString("F3", CultureInfo.InvariantCulture)
                + " frames=" + report.SampleCount);
            Application.Quit(passed ? 0 : 30);
        }

        private IEnumerator Click(Vector2 position)
        {
            var touchId = ++_touchSequence;
            InputSystem.QueueStateEvent(_touchscreen, new TouchState
            {
                touchId = touchId,
                phase = UnityEngine.InputSystem.TouchPhase.Began,
                position = position,
                pressure = 1f,
            },
                InputState.currentTime);
            InputSystem.Update();
            yield return null;

            InputSystem.QueueStateEvent(_touchscreen, new TouchState
            {
                touchId = touchId,
                phase = UnityEngine.InputSystem.TouchPhase.Ended,
                position = position,
                pressure = 0f,
            }, InputState.currentTime);
            InputSystem.Update();
            yield return new WaitForSecondsRealtime(0.08f);
        }

        private IEnumerator BeginHold(Vector2 position)
        {
            InputSystem.QueueStateEvent(_touchscreen, new TouchState
            {
                touchId = ++_touchSequence,
                phase = UnityEngine.InputSystem.TouchPhase.Began,
                position = position,
                pressure = 1f,
            }, InputState.currentTime);
            InputSystem.Update();
            yield return null;
        }

        private IEnumerator Hold(Vector2 position, double seconds)
        {
            var touchId = ++_touchSequence;
            InputSystem.QueueStateEvent(_touchscreen, new TouchState
            {
                touchId = touchId,
                phase = UnityEngine.InputSystem.TouchPhase.Began,
                position = position,
                pressure = 1f,
            }, InputState.currentTime);
            InputSystem.Update();
            yield return new WaitForSecondsRealtime((float)seconds);
            InputSystem.QueueStateEvent(_touchscreen, new TouchState
            {
                touchId = touchId,
                phase = UnityEngine.InputSystem.TouchPhase.Ended,
                position = position,
                pressure = 0f,
            }, InputState.currentTime);
            InputSystem.Update();
            yield return new WaitForSecondsRealtime(0.1f);
        }

        private IEnumerator WaitFor(Func<bool> predicate, float seconds, string label)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            if (!predicate()) Fail("timeout waiting for " + label, 20);
        }

        private IEnumerator CaptureAndQuit(Options options, Vector2Int resolution)
        {
            yield return new WaitForSecondsRealtime((float)options.DelaySeconds);
            yield return new WaitForEndOfFrame();
            WriteCapture(options, resolution);
            Application.Quit(0);
        }

        private void WriteCapture(Options options, Vector2Int resolution)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("ReleaseCandidateEvidenceCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 39, 55, 255);
            camera.cullingMask = ~0;
            var target = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.gameObject.activeInHierarchy) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
            texture.Apply();
            File.WriteAllBytes(options.Path, texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Destroy(texture); Destroy(target); Destroy(cameraObject);

            var record = new EvidenceRecord
            {
                width = resolution.x,
                height = resolution.y,
                requested_ratio = options.Ratio,
                requested_target = options.Target,
                selected_child = options.Child,
                sound_enabled = _host.SessionSoundEnabled,
                captions_enabled = _host.WelcomeCaptionsEnabled,
                reduced_motion = _host.ReducedMotionEnabled,
                text_scale = options.TextScale,
                pseudo_percent = options.PseudoPercent,
                route = _host.CurrentRoute.ToString(),
                session_location = _host.SessionLocation.ToString(),
                route_trace = string.Join(">", _routeTrace),
                session_open = _host.SessionOpen,
                accepted_input_count = _host.AcceptedInputCount,
                adult_hold_progress = _host.ClosingAdultSnapshot.HoldProgress,
                report_card_count = _host.ClosingParentReportSnapshot.Cards == null
                    ? 0 : _host.ClosingParentReportSnapshot.Cards.Count,
                executable_sha256 = options.ExecutableSha256,
                input_mode = "UnityEngine.InputSystem.QueueStateEvent",
                persistence_phase = options.PersistencePhase,
                session_selected_avatar_id = _host.SessionSelectedAvatarId,
                visual_equivalent_visible = VisualEquivalentVisible(),
                recovery_visible = _host.RecoveryVisible,
                pause_visible = _host.ActivePauseOverlayView != null
                    && _host.ActivePauseOverlayView.IsVisible,
                leave_visible = _host.ActiveLeaveConfirmationView != null
                    && _host.ActiveLeaveConfirmationView.IsVisible,
                diagnostic_control_present = false,
            };
            File.WriteAllText(Path.ChangeExtension(options.Path, ".json"),
                JsonUtility.ToJson(record, true));
            Debug.Log("RELEASE_CANDIDATE_CAPTURE_PASS target=" + options.Target);
        }

        private bool VisualEquivalentVisible()
        {
            if (_host.ActiveWelcomeView != null) return _host.ActiveWelcomeView.VisualEquivalentVisible;
            if (_host.ActiveAvatarSelectView != null) return _host.ActiveAvatarSelectView.VisualEquivalentVisible;
            if (_host.ActiveAvatarPreviewView != null) return _host.ActiveAvatarPreviewView.VisualEquivalentVisible;
            if (_host.ActiveStage3View != null) return _host.ActiveStage3View.VisualEquivalentVisible;
            return _host.ActiveCompletionView != null && _host.ActiveCompletionView.VisualEquivalentVisible;
        }

        private void RecordRoute()
        {
            var id = RouteName(_host.CurrentRoute);
            if (_routeTrace.Count == 0 || _routeTrace[_routeTrace.Count - 1] != id) _routeTrace.Add(id);
        }

        private static string RouteName(RouteId route)
        {
            switch (route)
            {
                case RouteId.StartupLoading: return "startup_loading";
                case RouteId.IntroVideo: return "intro_video";
                case RouteId.Welcome: return "welcome";
                case RouteId.AvatarSelect: return "avatar_select";
                case RouteId.AvatarPreview: return "avatar_preview";
                case RouteId.JourneyOverview: return "journey_overview";
                case RouteId.Boat1: return "boat_1";
                case RouteId.Stage1: return "stage_1";
                case RouteId.Boat2: return "boat_2";
                case RouteId.Stage2: return "stage_2";
                case RouteId.Boat3: return "boat_3";
                case RouteId.Stage3: return "stage_3";
                case RouteId.Completion: return "completion";
                case RouteId.AdultGate: return "adult_gate";
                case RouteId.ParentReport: return "parent_report";
                default: return route.ToString();
            }
        }

        private static bool TryOptions(string[] args, out Options options)
        {
            options = default;
            if (!Get(args, "-releaseCapture", null, out var path)
                || !Get(args, "-releaseRatio", "16x9", out var ratio)
                || !Get(args, "-releaseChild", "girl", out var child)
                || !Get(args, "-releaseSound", "off", out var sound)
                || !Get(args, "-releaseCaptions", "on", out var captions)
                || !Get(args, "-releaseMotion", "full", out var motion)
                || !Get(args, "-releaseTarget", "welcome", out var target)
                || !Get(args, "-releaseTextScale", "1", out var scaleText)
                || !Get(args, "-releasePseudoPercent", "0", out var pseudoText)
                || !Get(args, "-releaseExecutableHash", null, out var executableHash)
                || !Get(args, "-releasePersistence", "normal", out var persistence)
                || !Get(args, "-releaseCaptureDelay", "0.1", out var delayText)
                || !Get(args, "-releaseSoak", "", out var soakPath)
                || !Get(args, "-releaseSoakSeconds", "0", out var soakSecondsText)) return false;
            if (string.IsNullOrWhiteSpace(path)
                || (ratio != "4x3" && ratio != "16x9" && ratio != "24x10")
                || (child != "boy" && child != "girl")
                || (sound != "on" && sound != "off")
                || (captions != "on" && captions != "off")
                || (motion != "full" && motion != "reduced")
                || !Targets.Contains(target)
                || !float.TryParse(scaleText, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale)
                || (scale != 1f && scale != 2f)
                || !int.TryParse(pseudoText, out var pseudo) || (pseudo != 0 && pseudo != 35)
                || string.IsNullOrWhiteSpace(executableHash) || executableHash.Length != 64
                || (persistence != "normal" && persistence != "seed" && persistence != "verify")
                || !double.TryParse(delayText, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay)
                || delay < 0d || delay > 5d
                || !double.TryParse(soakSecondsText, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var soakSeconds)
                || (soakSeconds == 0d && !string.IsNullOrWhiteSpace(soakPath))
                || (soakSeconds > 0d && (soakSeconds < ReleaseSoakGate.RequiredSeconds
                    || string.IsNullOrWhiteSpace(soakPath) || target != "parent_report"))) return false;
            options = new Options(path, ratio, child, sound == "on", captions == "on",
                motion == "reduced", target, scale, pseudo, executableHash, persistence, delay,
                soakPath, soakSeconds);
            return true;
        }

        private static bool Get(string[] args, string name, string fallback, out string value)
        {
            value = fallback;
            var found = false;
            for (var index = 0; args != null && index < args.Length; index++)
            {
                if (!string.Equals(args[index], name, StringComparison.Ordinal)) continue;
                if (found || index + 1 >= args.Length || args[index + 1].StartsWith("-release", StringComparison.Ordinal))
                    return false;
                value = args[index + 1];
                found = true;
            }
            return found || fallback != null;
        }

        private static Vector2Int ResolutionFor(string ratio) => ratio == "4x3"
            ? new Vector2Int(1440, 1080) : ratio == "24x10"
                ? new Vector2Int(1920, 800) : new Vector2Int(1600, 900);

        private void Fail(string message, int code)
        {
            _failed = true;
            Debug.LogError("RELEASE_CANDIDATE_CAPTURE_FAIL " + message);
            Application.Quit(code);
        }

        private readonly struct Options
        {
            public Options(string path, string ratio, string child, bool sound, bool captions,
                bool reduced, string target, float textScale, int pseudoPercent,
                string executableSha256, string persistencePhase, double delaySeconds,
                string soakPath, double soakSeconds)
            {
                Path = path; Ratio = ratio; Child = child; SoundEnabled = sound;
                CaptionsEnabled = captions; ReducedMotion = reduced; Target = target;
                TextScale = textScale; PseudoPercent = pseudoPercent;
                ExecutableSha256 = executableSha256; DelaySeconds = delaySeconds;
                PersistencePhase = persistencePhase;
                SoakPath = soakPath; SoakSeconds = soakSeconds;
            }
            public string Path { get; }
            public string Ratio { get; }
            public string Child { get; }
            public bool SoundEnabled { get; }
            public bool CaptionsEnabled { get; }
            public bool ReducedMotion { get; }
            public string Target { get; }
            public float TextScale { get; }
            public int PseudoPercent { get; }
            public string ExecutableSha256 { get; }
            public string PersistencePhase { get; }
            public double DelaySeconds { get; }
            public string SoakPath { get; }
            public double SoakSeconds { get; }
        }

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width, height, pseudo_percent, accepted_input_count, report_card_count;
            public string requested_ratio, requested_target, selected_child, route, session_location,
                route_trace, executable_sha256, input_mode, persistence_phase,
                session_selected_avatar_id;
            public bool sound_enabled, captions_enabled, reduced_motion, session_open,
                visual_equivalent_visible, recovery_visible, pause_visible, leave_visible,
                diagnostic_control_present;
            public float text_scale;
            public double adult_hold_progress;
        }

        [Serializable]
        private sealed class SoakRecord
        {
            public int frame_samples, maximum_consecutive_over_33_3_ms, route_count,
                pause_count, audio_interruption_count, maximum_active_voice_count;
            public string started_utc, completed_utc, route_trace, final_route,
                executable_sha256, result;
            public bool audio_overlap_detected, route_drift_detected, replay_destination_seen;
            public double elapsed_seconds, required_seconds,
                performance_sampling_started_elapsed_seconds;
            public float average_frame_ms, p95_frame_ms, p99_frame_ms, maximum_frame_ms,
                maximum_allocated_mib, maximum_reserved_mib;
        }
    }
}
