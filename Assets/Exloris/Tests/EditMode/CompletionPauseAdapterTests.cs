using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionPauseAdapterTests
    {
        [Test]
        public void ExactSnapshotAndMediaElapsedRestoreWithFreshResumeToken()
        {
            var ids = Ids();
            var session = new Exloris.Core.Session.SessionState();
            session.Begin(RouteId.Stage3);
            var navigation = new NavigationAuthority(RouteId.Stage3, () => ids.Dequeue());
            var entry = navigation.Request(RouteId.Completion, 10d).Transaction;
            var view = new View();
            var controller = new CompletionController(
                session, navigation, view, new Adult(), new Pause(), new Leave(), () => ids.Dequeue());
            Assert.That(controller.Enter(entry.Id, 10d, false), Is.True);

            var backend = new Backend();
            var director = new AudioDirector(backend, () => ids.Dequeue());
            var content = AssetDatabase.LoadAssetAtPath<CompletionAdultReportContent>(
                "Assets/Exloris/Resources/Content/CompletionAdultReportContent.asset");
            var media = new CompletionMediaPresenter(
                director, backend, content, session, new AccessibilityState(false, true),
                new MediaSink(), () => ids.Dequeue());
            var handle = media.Start(controller.CurrentTokenId, 10d);
            var adapter = new CompletionPauseAdapter(controller, media, () => ids.Dequeue());
            adapter.BindMediaHandle(handle);
            Assert.That(controller.Advance(
                controller.CurrentTokenId, controller.CurrentPresentationTokenId, 11d), Is.True);
            var captured = controller.Snapshot;

            Assert.That(adapter.PreparePause(11d), Is.True);
            Assert.That(adapter.TryCapture(out var pauseHandle), Is.True);
            Assert.That(adapter.PauseMedia(pauseHandle, 11d), Is.True);
            Assert.That(controller.HandlePause(controller.CurrentTokenId, 11d), Is.True);
            Assert.That(adapter.Restore(pauseHandle, 20d), Is.True);
            Assert.That(controller.Snapshot.State, Is.EqualTo(captured.State));
            Assert.That(controller.Snapshot.ElapsedSeconds, Is.EqualTo(captured.ElapsedSeconds));
            Assert.That(adapter.ResumeMedia(pauseHandle, 20d), Is.True);
            Assert.That(backend.ResumeCount, Is.EqualTo(1));
            Assert.That(adapter.PauseMedia(pauseHandle, 20d), Is.False);
        }

        private static Queue<Guid> Ids()
        {
            var ids = new Queue<Guid>();
            for (var index = 1; index <= 40; index++)
                ids.Enqueue(Guid.Parse($"72000000-0000-0000-0000-{index:000000000000}"));
            return ids;
        }

        private sealed class View : ICompletionView
        {
            public void Present(CompletionSnapshot snapshot) { }
            public void SetInputReady(bool ready) { }
        }

        private sealed class Adult : IAdultGateRequestSink
        {
            public bool TryRequest(AdultGateRequest request) => true;
        }

        private sealed class Pause : ICompletionPauseSink
        {
            public bool TryShow(CompletionSnapshot snapshot) => true;
        }

        private sealed class Leave : ILeaveConfirmationSink
        {
            public bool TryShow(RouteId caller) => true;
        }

        private sealed class MediaSink : ICompletionMediaCueSink
        {
            public void PresentCompletionMedia(CompletionMediaCue cue) { }
            public void FreezeCompletionMedia(double elapsedSeconds) { }
            public void ResumeCompletionMedia(Guid mediaTokenId, double elapsedSeconds) { }
            public void SetCompletionInputLocked(bool locked) { }
        }

        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _clips = new HashSet<string>();
            public int ResumeCount;
            public bool IsClipRegistered(string id) => _clips.Contains(id);
            public void RegisterClip(string id, AudioClip clip, bool loop = false) => _clips.Add(id);
            public void Start(AudioPlaybackToken token, AudioBus bus) { }
            public void Stop(Guid tokenId) { }
            public void PauseAllImmediately() { }
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float targetMasterVolume, float rampSeconds) => ResumeCount++;
            public void SetMasterVolume(float linearVolume) { }
            public void ApplyDuck(AudioDuckState state) { }
        }
    }
}
