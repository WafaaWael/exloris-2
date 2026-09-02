using System;
using System.Linq;

namespace Exloris.Core.Session
{
    public static class ParentSummaryFormatter
    {
        public static string FormatChoiceOutcome(ObservationEvent observation)
        {
            RequireType(
                observation,
                ObservationEventType.BoatRoute,
                ObservationEventType.Stage1Outcome,
                ObservationEventType.Stage2Option,
                ObservationEventType.Stage3Option);
            return $"During this session, your child chose {observation.ChoiceId} and saw {observation.OutcomeId}.";
        }

        public static string FormatOptionsTried(ObservationEvent observation)
        {
            RequireType(
                observation,
                ObservationEventType.Stage1Option,
                ObservationEventType.Stage2Option,
                ObservationEventType.Stage3Option);
            return $"Your child tried {observation.Count} option(s) in {observation.SubjectId} before continuing.";
        }

        public static string FormatBirdGuideUse(ObservationEvent observation)
        {
            RequireType(observation, ObservationEventType.IbisHelp);
            return $"Your child used the bird guide {observation.Count} time(s) during the boat journeys.";
        }

        public static string FormatRevisit(ObservationEvent observation)
        {
            RequireType(observation, ObservationEventType.Revisit);
            return $"Your child revisited {observation.SubjectId} {observation.Count} time(s).";
        }

        public static string FormatCompletedStages(ObservationEvent observation)
        {
            RequireType(observation, ObservationEventType.Complete);
            return $"Your child completed {string.Join(", ", observation.StageIds)} in this session.";
        }

        internal static bool TryFormatObservation(
            ObservationEvent observation,
            out string stringId,
            out string text)
        {
            stringId = null;
            text = null;
            if (observation == null) return false;

            if (!string.IsNullOrEmpty(observation.ChoiceId)
                && !string.IsNullOrEmpty(observation.OutcomeId))
            {
                stringId = "parent_report.choice_outcome";
                text = FormatChoiceOutcome(observation);
                return true;
            }

            if (!string.IsNullOrEmpty(observation.SubjectId)
                && observation.SubjectId.StartsWith("stage_", StringComparison.Ordinal)
                && (observation.Type == ObservationEventType.Stage1Option
                    || observation.Type == ObservationEventType.Stage2Option
                    || observation.Type == ObservationEventType.Stage3Option))
            {
                stringId = "parent_report.options_tried";
                text = FormatOptionsTried(observation);
                return true;
            }

            switch (observation.Type)
            {
                case ObservationEventType.IbisHelp:
                    stringId = "parent_report.bird_guide_use";
                    text = FormatBirdGuideUse(observation);
                    return true;
                case ObservationEventType.Revisit:
                    stringId = "parent_report.revisit";
                    text = FormatRevisit(observation);
                    return true;
                case ObservationEventType.Complete:
                    stringId = "parent_report.completed_stages";
                    text = FormatCompletedStages(observation);
                    return true;
                default:
                    return false;
            }
        }

        private static void RequireType(ObservationEvent observation, params ObservationEventType[] allowed)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            if (!allowed.Contains(observation.Type))
            {
                throw new ArgumentException("Observation does not belong to this approved template family.", nameof(observation));
            }
        }
    }
}
