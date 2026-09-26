using System;
using System.Globalization;

namespace Rasa.Structures
{
    internal enum MissionScenarioStepStateKind
    {
        CompletedStep,
        ScheduledScenario,
        SpawnDeathCount
    }

    internal readonly struct MissionScenarioStepState
    {
        private const string AttemptPrefix = "attempt:";
        private const string StepPrefix = "scenario:";
        private const string StepSeparator = ":step:";
        private const string SchedulePrefix = "schedule:";
        private const string SpawnDeathPrefix = "spawn-state:";

        public MissionScenarioStepStateKind Kind { get; }
        public string StepKey { get; }
        public string AttemptKey { get; }
        public uint ScenarioId { get; }
        public uint StepId { get; }
        public uint? TargetScenarioId { get; }
        public DateTime? DueAtUtc { get; }
        public uint? SpawnGroupId { get; }
        public uint? SpawnId { get; }
        public uint? DeathCount { get; }

        private MissionScenarioStepState(
            MissionScenarioStepStateKind kind,
            string stepKey,
            string attemptKey,
            uint scenarioId,
            uint stepId,
            uint? targetScenarioId = null,
            DateTime? dueAtUtc = null,
            uint? spawnGroupId = null,
            uint? spawnId = null,
            uint? deathCount = null)
        {
            Kind = kind;
            StepKey = stepKey ?? string.Empty;
            AttemptKey = attemptKey ?? string.Empty;
            ScenarioId = scenarioId;
            StepId = stepId;
            TargetScenarioId = targetScenarioId;
            DueAtUtc = dueAtUtc;
            SpawnGroupId = spawnGroupId;
            SpawnId = spawnId;
            DeathCount = deathCount;
        }

        internal static string CreateCompletedKey(
            uint scenarioId,
            uint stepId,
            string attemptKey = null)
        {
            var key = $"{StepPrefix}{scenarioId}{StepSeparator}{stepId}";
            return string.IsNullOrWhiteSpace(attemptKey)
                ? key
                : CreateAttemptPrefix(attemptKey) + key;
        }

        internal static string CreateScheduledKey(
            uint scenarioId,
            uint stepId,
            uint targetScenarioId,
            DateTime dueAtUtc,
            string attemptKey = null)
        {
            var due = new DateTimeOffset(dueAtUtc).ToUnixTimeMilliseconds();
            var key = SchedulePrefix + scenarioId.ToString(CultureInfo.InvariantCulture) +
                      ":" + stepId.ToString(CultureInfo.InvariantCulture) +
                      ":" + targetScenarioId.ToString(CultureInfo.InvariantCulture) +
                      ":" + due.ToString(CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(attemptKey)
                ? key
                : CreateAttemptPrefix(attemptKey) + key;
        }

        internal static string CreateScenarioPrefix(uint scenarioId) =>
            $"{StepPrefix}{scenarioId}{StepSeparator}";

        internal static string CreateAttemptPrefix(string attemptKey) =>
            $"{AttemptPrefix}{attemptKey}:";

        internal static string CreateSpawnDeathPrefix(
            uint spawnGroupId,
            uint spawnId,
            string attemptKey = null)
        {
            var key = $"{SpawnDeathPrefix}{spawnGroupId}:{spawnId}:";
            return string.IsNullOrWhiteSpace(attemptKey)
                ? key
                : CreateAttemptPrefix(attemptKey) + key;
        }

        internal static string CreateSpawnDeathGroupPrefix(
            uint spawnGroupId,
            string attemptKey = null)
        {
            var key = $"{SpawnDeathPrefix}{spawnGroupId}:";
            return string.IsNullOrWhiteSpace(attemptKey)
                ? key
                : CreateAttemptPrefix(attemptKey) + key;
        }

        internal static string CreateSpawnDeathKey(
            uint spawnGroupId,
            uint spawnId,
            uint deathCount,
            string attemptKey = null) =>
            CreateSpawnDeathPrefix(spawnGroupId, spawnId, attemptKey) +
            deathCount.ToString(CultureInfo.InvariantCulture);

        internal static bool TryParse(string stepKey, out MissionScenarioStepState state)
        {
            var rawKey = stepKey;
            var attemptKey = string.Empty;
            if (!string.IsNullOrWhiteSpace(rawKey) &&
                rawKey.StartsWith(AttemptPrefix, StringComparison.Ordinal))
            {
                var attemptSeparator = rawKey.IndexOf(':', AttemptPrefix.Length);
                if (attemptSeparator <= AttemptPrefix.Length)
                {
                    state = default;
                    return false;
                }

                attemptKey = rawKey.Substring(
                    AttemptPrefix.Length,
                    attemptSeparator - AttemptPrefix.Length);
                rawKey = rawKey[(attemptSeparator + 1)..];
            }

            if (!string.IsNullOrWhiteSpace(rawKey) &&
                rawKey.StartsWith(StepPrefix, StringComparison.Ordinal))
            {
                var separatorIndex = rawKey.IndexOf(StepSeparator, StringComparison.Ordinal);
                if (separatorIndex > StepPrefix.Length &&
                    uint.TryParse(
                        rawKey.Substring(
                            StepPrefix.Length,
                            separatorIndex - StepPrefix.Length),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var scenarioId) &&
                    uint.TryParse(
                        rawKey[(separatorIndex + StepSeparator.Length)..],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var stepId))
                {
                    state = new MissionScenarioStepState(
                        MissionScenarioStepStateKind.CompletedStep,
                        stepKey,
                        attemptKey,
                        scenarioId,
                        stepId);
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(rawKey) &&
                rawKey.StartsWith(SchedulePrefix, StringComparison.Ordinal))
            {
                var tokens = rawKey.Split(':');
                if (tokens.Length == 5 &&
                    uint.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out var scenarioId) &&
                    uint.TryParse(tokens[2], NumberStyles.None, CultureInfo.InvariantCulture, out var stepId) &&
                    uint.TryParse(tokens[3], NumberStyles.None, CultureInfo.InvariantCulture, out var targetScenarioId) &&
                    long.TryParse(tokens[4], NumberStyles.None, CultureInfo.InvariantCulture, out var dueUnixMilliseconds))
                {
                    state = new MissionScenarioStepState(
                        MissionScenarioStepStateKind.ScheduledScenario,
                        stepKey,
                        attemptKey,
                        scenarioId,
                        stepId,
                        targetScenarioId,
                        DateTimeOffset.FromUnixTimeMilliseconds(dueUnixMilliseconds).UtcDateTime);
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(rawKey) &&
                rawKey.StartsWith(SpawnDeathPrefix, StringComparison.Ordinal))
            {
                var tokens = rawKey.Split(':');
                if (tokens.Length == 4 &&
                    uint.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out var spawnGroupId) &&
                    uint.TryParse(tokens[2], NumberStyles.None, CultureInfo.InvariantCulture, out var spawnId) &&
                    uint.TryParse(tokens[3], NumberStyles.None, CultureInfo.InvariantCulture, out var deathCount))
                {
                    state = new MissionScenarioStepState(
                        MissionScenarioStepStateKind.SpawnDeathCount,
                        stepKey,
                        attemptKey,
                        0,
                        0,
                        spawnGroupId: spawnGroupId,
                        spawnId: spawnId,
                        deathCount: deathCount);
                    return true;
                }
            }

            state = default;
            return false;
        }
    }
}
