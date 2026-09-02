using System;
using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class CompletionAdultReportLayoutPlayModeTests
    {
        [UnityTest]
        public IEnumerator ThreeClosingViewsRenderEveryFrozenRatioWithInertArt()
        {
            var ratios = new[]
            {
                new[] { 2160f, 1620f, 1598.77f, 1183.08f, 264f, 88f },
                new[] { 1920f, 1080f, 1856f, 1016f, 326f, 88f },
                new[] { 2400f, 1000f, 2038.84f, 825.52f, 429f, 144f },
            };
            var owner = new GameObject("ClosingViewsGraphicsSmoke");
            var completionOwner = new GameObject("Completion");
            var adultOwner = new GameObject("AdultGate");
            var reportOwner = new GameObject("ParentReport");
            completionOwner.transform.SetParent(owner.transform);
            adultOwner.transform.SetParent(owner.transform);
            reportOwner.transform.SetParent(owner.transform);
            var completionRouter = Router();
            var adultRouter = Router();
            var reportRouter = Router();
            try
            {
                var content = CompletionAdultReportContent.LoadApproved();
                var completion = completionOwner.AddComponent<CompletionStageView>();
                var adult = adultOwner.AddComponent<AdultGateView>();
                var report = reportOwner.AddComponent<ParentReportView>();
                completion.Configure(completionRouter, content, "avatar_girl");
                adult.Configure(adultRouter, content);
                report.Configure(reportRouter, content);
                var request = new AdultGateRequest(
                    AdultGatePurpose.Dashboard, AdultGateRequestOrigin.Completion,
                    RouteId.Completion, Guid.NewGuid());
                adult.Present(new AdultGateSnapshot(
                    Guid.NewGuid(), AdultGateState.Holding, request, 0.5d, 1, 0d));
                report.Present(new ParentReportSnapshot(
                    Guid.NewGuid(), ParentReportState.Ready,
                    ParentReportBuilder.Build(Array.Empty<ObservationEvent>()).Cards,
                    1f, ParentReportView.CloseControlId));

                for (var index = 0; index < ratios.Length; index++)
                {
                    var ratio = ratios[index];
                    Assert.That(completion.TryApplyResponsiveLayout(
                        ratio[0], ratio[1], ratio[2], ratio[3], ratio[4], (int)ratio[5]), Is.True);
                    Assert.That(adult.TryApplyResponsiveLayout(
                        ratio[0], ratio[1], ratio[2], ratio[3], ratio[4], (int)ratio[5]), Is.True);
                    Assert.That(report.TryApplyResponsiveLayout(
                        ratio[0], ratio[1], ratio[2], ratio[3], ratio[4], (int)ratio[5], 2f, 1.35f), Is.True);
                    var expected = index == 0 ? ResponsiveBackgroundVariant.FourByThree
                        : index == 1 ? ResponsiveBackgroundVariant.SixteenByNine
                        : ResponsiveBackgroundVariant.UltraWide;
                    Assert.That(completion.BackgroundVariant, Is.EqualTo(expected));
                    Assert.That(adult.BackgroundVariant, Is.EqualTo(expected));
                    Assert.That(report.BackgroundVariant, Is.EqualTo(expected));
                    Assert.That(completion.VisibleArtRaycastsDisabled(), Is.True);
                    Assert.That(adult.VisibleArtRaycastsDisabled(), Is.True);
                    Assert.That(report.VisibleArtRaycastsDisabled(), Is.True);
                    yield return null;
                }
            }
            finally
            {
                completionRouter.Dispose();
                adultRouter.Dispose();
                reportRouter.Dispose();
                UnityEngine.Object.Destroy(owner);
            }
        }

        private static ChildInputRouter Router() => new ChildInputRouter(
            new InputAction("Point", InputActionType.PassThrough),
            new InputAction("Press", InputActionType.Button));
    }
}
