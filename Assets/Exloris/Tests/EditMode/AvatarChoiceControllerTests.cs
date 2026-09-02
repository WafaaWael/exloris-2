using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class AvatarChoiceControllerTests
    {
        [TestCase(AvatarChoice.Boy, AvatarChoice.Girl)]
        [TestCase(AvatarChoice.Girl, AvatarChoice.Boy)]
        public void FirstEntryUsesInjectedOrderWithoutPreselection(
            AvatarChoice initialLeft,
            AvatarChoice expectedRight)
        {
            var context = CreateContext(initialLeft);

            Assert.That(context.Controller.EnterSelect(), Is.True);

            Assert.That(context.Controller.PreviewChoice, Is.Null);
            Assert.That(context.Session.SelectedAvatarId, Is.Null);
            Assert.That(context.View.SelectLeft, Is.EqualTo(initialLeft));
            Assert.That(context.View.SelectRight, Is.EqualTo(expectedRight));
            Assert.That(context.OrderSource.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void SelectReentryAlternatesOrderWithoutCallingRandomSourceAgain()
        {
            var context = CreateContext(AvatarChoice.Boy);
            context.Controller.EnterSelect();

            Assert.That(context.Controller.EnterSelect(), Is.True);

            Assert.That(context.View.SelectLeft, Is.EqualTo(AvatarChoice.Girl));
            Assert.That(context.View.SelectRight, Is.EqualTo(AvatarChoice.Boy));
            Assert.That(context.OrderSource.CallCount, Is.EqualTo(1));
        }

        [TestCase(AvatarChoice.Boy, "avatar_boy")]
        [TestCase(AvatarChoice.Girl, "avatar_girl")]
        public void PreviewDoesNotCommitUntilConfirm(AvatarChoice choice, string expectedAvatarId)
        {
            var context = CreateContext(AvatarChoice.Boy);
            context.Controller.EnterSelect();

            Assert.That(context.Controller.HandleChoice(choice, 10.0), Is.True);
            Assert.That(context.Controller.PreviewChoice, Is.EqualTo(choice));
            Assert.That(context.Controller.CurrentPreviewToken, Is.EqualTo(1));
            Assert.That(context.Session.SelectedAvatarId, Is.Null);
            Assert.That(context.Navigation.ActiveTransaction.Destination,
                Is.EqualTo(RouteId.AvatarPreview));

            CompleteAndEnterPreview(context);
            Assert.That(context.Controller.HandleConfirm(11.0), Is.True);
            Assert.That(context.Controller.HandleConfirm(11.1), Is.False);
            Assert.That(context.Session.SelectedAvatarId, Is.EqualTo(expectedAvatarId));
            Assert.That(context.Navigation.ActiveTransaction.Destination,
                Is.EqualTo(RouteId.JourneyOverview));
        }

        [Test]
        public void ChooseOtherReturnsOnlyToSelectAndClearsPreview()
        {
            var context = CreateContext(AvatarChoice.Girl);
            context.Controller.EnterSelect();
            context.Controller.HandleChoice(AvatarChoice.Girl, 20.0);
            CompleteAndEnterPreview(context);

            var accepted = context.Controller.HandleOther(21.0);

            Assert.That(accepted, Is.True);
            Assert.That(context.Controller.PreviewChoice, Is.Null);
            Assert.That(context.Navigation.ActiveTransaction.Caller,
                Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(context.Navigation.ActiveTransaction.Destination,
                Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(context.Session.SelectedAvatarId, Is.Null);
        }

        [Test]
        public void RepeatCreatesNewPreviewTokenWithoutNavigationOrCommit()
        {
            var context = CreateContext(AvatarChoice.Boy);
            context.Controller.EnterSelect();
            context.Controller.HandleChoice(AvatarChoice.Boy, 30.0);
            CompleteAndEnterPreview(context);
            var firstToken = context.Controller.CurrentPreviewToken;

            Assert.That(context.Controller.HandleRepeat(), Is.True);

            Assert.That(context.Controller.CurrentPreviewToken, Is.GreaterThan(firstToken));
            Assert.That(context.View.PreviewTokens,
                Is.EqualTo(new[] { firstToken, context.Controller.CurrentPreviewToken }));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Session.SelectedAvatarId, Is.Null);
        }

        [TestCase(RouteId.AvatarSelect)]
        [TestCase(RouteId.AvatarPreview)]
        public void HomeOpensLeaveForExactCallerWithoutChangingSession(RouteId caller)
        {
            var context = CreateContext(AvatarChoice.Girl);
            context.Controller.EnterSelect();
            if (caller == RouteId.AvatarPreview)
            {
                context.Controller.HandleChoice(AvatarChoice.Girl, 40.0);
                CompleteAndEnterPreview(context);
            }

            Assert.That(context.Controller.HandleHome(), Is.True);

            Assert.That(context.LeaveSink.Callers, Is.EqualTo(new[] { caller }));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Session.IsOpen, Is.True);
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(caller));
        }

        [Test]
        public void ChoiceAndPreviewActionsRejectWrongScreenState()
        {
            var context = CreateContext(AvatarChoice.Boy);

            Assert.That(context.Controller.HandleChoice(AvatarChoice.Boy, 50.0), Is.False);
            Assert.That(context.Controller.HandleRepeat(), Is.False);
            Assert.That(context.Controller.HandleOther(50.1), Is.False);
            Assert.That(context.Controller.HandleConfirm(50.2), Is.False);
            Assert.That(context.Controller.HandleHome(), Is.False);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Session.SelectedAvatarId, Is.Null);
        }

        private static TestContext CreateContext(AvatarChoice initialLeft)
        {
            var session = new SessionState();
            session.Begin(RouteId.AvatarSelect);
            var navigation = new NavigationAuthority(
                RouteId.AvatarSelect,
                new IncrementingGuidFactory().Next);
            var view = new RecordingAvatarChoiceView();
            var leaveSink = new RecordingLeaveConfirmationSink();
            var orderSource = new FixedAvatarOrderSource(initialLeft);
            var controller = new AvatarChoiceController(
                session,
                navigation,
                view,
                leaveSink,
                orderSource);
            return new TestContext(session, navigation, view, leaveSink, orderSource, controller);
        }

        private static void CompleteAndEnterPreview(TestContext context)
        {
            var transaction = context.Navigation.ActiveTransaction;
            Assert.That(transaction, Is.Not.Null);
            Assert.That(context.Navigation.Complete(transaction.Id),
                Is.EqualTo(NavigationCompletion.Completed));
            Assert.That(context.Session.MoveTo(RouteId.AvatarPreview), Is.True);
            Assert.That(context.Controller.EnterPreview(), Is.True);
        }

        private sealed class TestContext
        {
            public TestContext(
                SessionState session,
                NavigationAuthority navigation,
                RecordingAvatarChoiceView view,
                RecordingLeaveConfirmationSink leaveSink,
                FixedAvatarOrderSource orderSource,
                AvatarChoiceController controller)
            {
                Session = session;
                Navigation = navigation;
                View = view;
                LeaveSink = leaveSink;
                OrderSource = orderSource;
                Controller = controller;
            }

            public SessionState Session { get; }
            public NavigationAuthority Navigation { get; }
            public RecordingAvatarChoiceView View { get; }
            public RecordingLeaveConfirmationSink LeaveSink { get; }
            public FixedAvatarOrderSource OrderSource { get; }
            public AvatarChoiceController Controller { get; }
        }

        private sealed class FixedAvatarOrderSource : IAvatarOrderSource
        {
            private readonly AvatarChoice _initialLeft;

            public FixedAvatarOrderSource(AvatarChoice initialLeft)
            {
                _initialLeft = initialLeft;
            }

            public int CallCount { get; private set; }

            public AvatarChoice ChooseInitialLeft()
            {
                CallCount++;
                return _initialLeft;
            }
        }

        private sealed class RecordingAvatarChoiceView : IAvatarChoiceView
        {
            public AvatarChoice SelectLeft { get; private set; }
            public AvatarChoice SelectRight { get; private set; }
            public List<long> PreviewTokens { get; } = new List<long>();

            public void ShowSelect(AvatarChoice left, AvatarChoice right)
            {
                SelectLeft = left;
                SelectRight = right;
            }

            public void ShowPreview(AvatarChoice choice, long previewToken)
            {
                PreviewTokens.Add(previewToken);
            }
        }

        private sealed class RecordingLeaveConfirmationSink : ILeaveConfirmationSink
        {
            public List<RouteId> Callers { get; } = new List<RouteId>();

            public bool TryShow(RouteId caller)
            {
                Callers.Add(caller);
                return true;
            }
        }

        private sealed class IncrementingGuidFactory
        {
            private int _value;

            public Guid Next()
            {
                _value++;
                var bytes = new byte[16];
                bytes[15] = (byte)_value;
                return new Guid(bytes);
            }
        }
    }
}
