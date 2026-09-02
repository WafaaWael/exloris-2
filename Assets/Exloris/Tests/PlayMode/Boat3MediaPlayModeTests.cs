using System;
using System.Collections;
using Exloris.Runtime.Boat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Boat3MediaPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReducedMotionUsesStaticLeftFrameBeforeFirstRender()
        {
            var content=Resources.Load<Boat3Content>(Boat3Content.ResourcePath); var sink=new Sink(); var presenter=new Boat3MotionPresenter(content,sink);
            var snapshot=new Boat3Snapshot(Guid.NewGuid(),Boat3State.Traveling,Boat3Choice.FollowIbis,BoatDirection.Left,0,0,null,false);
            Assert.That(presenter.Present(snapshot,true,1000),Is.True); Assert.That(sink.Frame.SpatialMotionEnabled,Is.False); Assert.That(sink.Frame.IbisSprite,Is.SameAs(content.IbisSprites[7]));
            yield return null; Assert.That(sink.Count,Is.EqualTo(1));
        }
        private sealed class Sink:IBoat3MotionSink { public Boat3MotionFrame Frame{get;private set;} public int Count{get;private set;} public void PresentMotion(Boat3MotionFrame frame){Frame=frame;Count++;} }
    }
}
