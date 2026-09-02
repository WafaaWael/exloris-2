using System;
using System.Collections.Generic;
using Exloris.Core.Session;

namespace Exloris.Runtime.Adult
{
    public enum ParentReportState
    {
        Hidden,
        Ready,
        NavigatingClose,
        NavigatingRestart,
        Recovery,
    }

    public readonly struct ParentReportSnapshot
    {
        public ParentReportSnapshot(
            Guid tokenId,
            ParentReportState state,
            IReadOnlyList<ParentReportCard> cards,
            float scrollNormalized,
            string focusedControlId)
        {
            TokenId = tokenId;
            State = state;
            Cards = cards;
            ScrollNormalized = scrollNormalized;
            FocusedControlId = focusedControlId;
        }

        public Guid TokenId { get; }
        public ParentReportState State { get; }
        public IReadOnlyList<ParentReportCard> Cards { get; }
        public float ScrollNormalized { get; }
        public string FocusedControlId { get; }
    }

    public interface IParentReportView
    {
        void Present(ParentReportSnapshot snapshot);
        void SetInputReady(bool ready);
    }
}
