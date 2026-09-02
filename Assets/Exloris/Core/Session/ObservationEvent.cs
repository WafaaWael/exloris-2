using System;
using System.Collections.Generic;
using System.Linq;

namespace Exloris.Core.Session
{
    public enum ObservationEventType
    {
        BoatRoute,
        IbisHelp,
        BoatSkip,
        Stage1Option,
        Stage1Outcome,
        Stage2Option,
        Stage3Option,
        Revisit,
        Complete,
    }

    public sealed class ObservationEvent
    {
        private static readonly string[] BannedInferenceFragments =
        {
            "advice",
            "advert",
            "abilit",
            "analytic",
            "avatar",
            "best",
            "compare",
            "cross_session",
            "diagnos",
            "error",
            "fast",
            "focus",
            "good",
            "bad",
            "identif",
            "learning_style",
            "network",
            "personality",
            "percentile",
            "pointer",
            "previous_session",
            "profile",
            "rank",
            "recommend",
            "response_time",
            "score",
            "slow",
            "smart",
            "speed",
            "support",
            "talent",
            "timing",
            "user_id",
        };

        private ObservationEvent(
            ObservationEventType type,
            string subjectId,
            string choiceId,
            string outcomeId,
            int count,
            IReadOnlyList<string> stageIds)
        {
            Type = type;
            SubjectId = subjectId;
            ChoiceId = choiceId;
            OutcomeId = outcomeId;
            Count = count;
            StageIds = stageIds ?? Array.Empty<string>();
        }

        public ObservationEventType Type { get; }
        public string SubjectId { get; }
        public string ChoiceId { get; }
        public string OutcomeId { get; }
        public int Count { get; }
        public IReadOnlyList<string> StageIds { get; }

        public static ObservationEvent ChoiceOutcome(
            ObservationEventType type,
            string choiceId,
            string outcomeId)
        {
            if (type != ObservationEventType.BoatRoute
                && type != ObservationEventType.Stage1Outcome
                && type != ObservationEventType.Stage2Option
                && type != ObservationEventType.Stage3Option)
            {
                throw new ArgumentException("This event family cannot populate the choice/outcome template.", nameof(type));
            }

            return new ObservationEvent(
                type,
                null,
                ValidateToken(choiceId, "choice_", nameof(choiceId)),
                ValidateToken(outcomeId, "outcome_", nameof(outcomeId)),
                1,
                null);
        }

        public static ObservationEvent OptionsTried(
            ObservationEventType type,
            string stageId,
            int count)
        {
            if (type != ObservationEventType.Stage1Option
                && type != ObservationEventType.Stage2Option
                && type != ObservationEventType.Stage3Option)
            {
                throw new ArgumentException("This event family cannot populate the options template.", nameof(type));
            }

            return new ObservationEvent(
                type,
                ValidateToken(stageId, "stage_", nameof(stageId)),
                null,
                null,
                ValidateCount(count),
                null);
        }

        public static ObservationEvent BirdGuideUsed(int count)
        {
            return new ObservationEvent(
                ObservationEventType.IbisHelp,
                null,
                null,
                null,
                ValidateCount(count),
                null);
        }

        public static ObservationEvent BoatSkipped(string boatId, int count)
        {
            return new ObservationEvent(
                ObservationEventType.BoatSkip,
                ValidateToken(boatId, "boat_", nameof(boatId)),
                null,
                null,
                ValidateCount(count),
                null);
        }

        public static ObservationEvent Revisited(string subjectId, int count)
        {
            var isStage = subjectId != null && subjectId.StartsWith("stage_", StringComparison.Ordinal);
            var prefix = isStage ? "stage_" : "outcome_";
            return new ObservationEvent(
                ObservationEventType.Revisit,
                ValidateToken(subjectId, prefix, nameof(subjectId)),
                null,
                null,
                ValidateCount(count),
                null);
        }

        public static ObservationEvent CompletedStages(params string[] stageIds)
        {
            if (stageIds == null || stageIds.Length == 0)
            {
                throw new ArgumentException("At least one directly completed stage is required.", nameof(stageIds));
            }

            var validated = stageIds
                .Select(stageId => ValidateToken(stageId, "stage_", nameof(stageIds)))
                .ToArray();
            return new ObservationEvent(
                ObservationEventType.Complete,
                null,
                null,
                null,
                validated.Length,
                Array.AsReadOnly(validated));
        }

        private static int ValidateCount(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return count;
        }

        private static string ValidateToken(string value, string requiredPrefix, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)
                || !value.StartsWith(requiredPrefix, StringComparison.Ordinal)
                || value.Length > 64
                || value.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character == '_'))
                || BannedInferenceFragments.Any(fragment => value.Contains(fragment)))
            {
                throw new ArgumentException("Observation fields must be approved factual content tokens.", parameterName);
            }

            return value;
        }
    }
}
