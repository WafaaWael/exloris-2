using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Exloris.Core.Session
{
    public enum ParentReportCardKind
    {
        Disclosure,
        Fact,
        Empty,
    }

    public readonly struct ParentReportCard
    {
        public ParentReportCard(string stringId, string text, ParentReportCardKind kind)
        {
            if (string.IsNullOrWhiteSpace(stringId)) throw new ArgumentException("A stable string ID is required.", nameof(stringId));
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Visible factual text is required.", nameof(text));
            StringId = stringId;
            Text = text;
            Kind = kind;
        }

        public string StringId { get; }
        public string Text { get; }
        public ParentReportCardKind Kind { get; }
    }

    public sealed class ParentReportDocument
    {
        internal ParentReportDocument(IReadOnlyList<ParentReportCard> cards, int factualCardCount)
        {
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
            FactualCardCount = factualCardCount;
        }

        public IReadOnlyList<ParentReportCard> Cards { get; }
        public int FactualCardCount { get; }
    }

    public static class ParentReportBuilder
    {
        public const string PrivacyDisclosure = "This game keeps only accessibility settings on this device. It does not identify, diagnose, rank, score, compare, or advertise to your child. The parent summary describes only actions from the current play session and disappears when the session is reset or the app data is cleared.";
        public const string EmptyState = "No recorded journey actions";

        public static ParentReportDocument Build(IReadOnlyList<ObservationEvent> observations)
        {
            if (observations == null) throw new ArgumentNullException(nameof(observations));

            var cards = new List<ParentReportCard>
            {
                new ParentReportCard(
                    "parent_report.privacy_disclosure",
                    PrivacyDisclosure,
                    ParentReportCardKind.Disclosure),
            };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var factualCount = 0;

            for (var index = 0; index < observations.Count; index++)
            {
                var observation = observations[index];
                if (!ParentSummaryFormatter.TryFormatObservation(observation, out var stringId, out var text))
                    continue;
                var exactKey = stringId + "\n" + text;
                if (!seen.Add(exactKey)) continue;
                cards.Add(new ParentReportCard(stringId, text, ParentReportCardKind.Fact));
                factualCount++;
            }

            if (factualCount == 0)
            {
                cards.Add(new ParentReportCard(
                    "parent_report.empty",
                    EmptyState,
                    ParentReportCardKind.Empty));
            }

            return new ParentReportDocument(
                new ReadOnlyCollection<ParentReportCard>(cards.ToArray()),
                factualCount);
        }
    }
}
