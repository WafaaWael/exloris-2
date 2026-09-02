using System;
using System.Collections;
using Exloris.Runtime.Input;
using Exloris.Runtime.Stage2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Stage2MediaPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReducedMotionShowsStaticBeforeThenAfterWithoutFullMotionFrame()
        {
            var content = Resources.Load<Stage2Content>(Stage2Content.ResourcePath);
            var owner = new GameObject("Stage2MediaPlayMode", typeof(Stage2StageView));
            var router = new ChildInputRouter(new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
            try
            {
                var view = owner.GetComponent<Stage2StageView>(); view.Configure(router, content, false);
                var presenter = new Stage2OutcomePresenter(content, view);
                var snapshot = new Stage2Snapshot(Guid.NewGuid(), Stage2State.ConsequencePlaying, Stage2River.Low,
                    Stage2ObservedHotspots.None, Stage2ObservedHotspots.None, Stage2ObservedHotspots.None,
                    Stage2Hotspot.Field, Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, 1, false);
                Assert.That(presenter.Start(snapshot, true, 0d), Is.True);
                Assert.That(view.LastOutcomeFrame.Phase, Is.EqualTo(Stage2OutcomePhase.Before));
                yield return null;
                Assert.That(view.LastOutcomeFrame.Phase, Is.EqualTo(Stage2OutcomePhase.Before));
                presenter.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 0.6d);
                Assert.That(view.LastOutcomeFrame.Phase, Is.EqualTo(Stage2OutcomePhase.After));
                Assert.That(view.LastOutcomeFrame.SpatialMotionEnabled, Is.False);
            }
            finally { router.Dispose(); UnityEngine.Object.Destroy(owner); }
        }
    }
}
