using System;
using System.Collections;
using Exloris.Runtime.Input;
using Exloris.Runtime.Stage1;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Stage1MediaPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReducedMotionPresentsStaticBeforeThenAfterWithoutHiddenFullMotionFrame()
        {
            var content = Resources.Load<Stage1Content>(Stage1Content.ResourcePath);
            Assert.That(content, Is.Not.Null);
            var owner = new GameObject("Stage1MediaPlayMode", typeof(Stage1StageView));
            var router = new ChildInputRouter(
                new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
            try
            {
                var view = owner.GetComponent<Stage1StageView>(); view.Configure(router, content, false);
                var presenter = new Stage1OutcomePresenter(content, view);
                var snapshot = new Stage1Snapshot(Guid.NewGuid(), Stage1State.OutcomePlaying, true, 0.5d, 0, 1,
                    Stage1OutcomeKind.InRange, Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, false, false);
                Assert.That(presenter.Start(snapshot, true, 0d), Is.True);
                Assert.That(view.LastOutcomeFrame.Phase, Is.EqualTo(Stage1OutcomePhase.Before));
                Assert.That(view.LastOutcomeFrame.SpatialMotionEnabled, Is.False);
                yield return null;
                Assert.That(view.LastOutcomeFrame.Phase, Is.EqualTo(Stage1OutcomePhase.Before));
                Assert.That(presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 0.8d), Is.True);
                Assert.That(view.LastOutcomeFrame.Phase, Is.EqualTo(Stage1OutcomePhase.After));
                Assert.That(view.LastOutcomeFrame.MarkerPosition, Is.EqualTo(0.5d));
            }
            finally { router.Dispose(); UnityEngine.Object.Destroy(owner); }
        }
    }
}
