using System;

namespace Rasa.Missions.Runtime
{
    public enum MissionRepeatKind { Once, Immediate, Cooldown, Daily }

    public sealed record MissionRepeatPolicy(MissionRepeatKind Kind,
        uint? CooldownSeconds = null, uint? ResetSecondUtc = null)
    {
        public static MissionRepeatPolicy Once { get; } = new(MissionRepeatKind.Once);

        public string ValidationError => Kind switch
        {
            MissionRepeatKind.Once or MissionRepeatKind.Immediate when CooldownSeconds == null && ResetSecondUtc == null => null,
            MissionRepeatKind.Cooldown when CooldownSeconds > 0 && ResetSecondUtc == null => null,
            MissionRepeatKind.Daily when ResetSecondUtc < 86400 && CooldownSeconds == null => null,
            _ => "Repeat policy requires Once/Immediate with no timing, a positive Cooldown, or a Daily UTC reset second (0..86399)."
        };

        public void Validate()
        {
            if (ValidationError is { } error)
                throw new MissionRuleException(error);
        }

        public bool Allows(DateTime utcNow, bool everSucceeded, DateTime? lastRewardedAtUtc)
        {
            Validate();
            RequireUtc(utcNow);
            return Kind switch
            {
                MissionRepeatKind.Once => !everSucceeded,
                MissionRepeatKind.Immediate => true,
                MissionRepeatKind.Cooldown => lastRewardedAtUtc == null ||
                    utcNow - lastRewardedAtUtc.Value >= TimeSpan.FromSeconds(CooldownSeconds.Value),
                MissionRepeatKind.Daily => lastRewardedAtUtc == null || lastRewardedAtUtc.Value < WindowStartUtc(utcNow),
                _ => throw new MissionRuleException("Unsupported repeat policy.")
            };
        }

        public DateTime WindowStartUtc(DateTime utcNow)
        {
            Validate();
            RequireUtc(utcNow);
            if (Kind != MissionRepeatKind.Daily)
                throw new MissionRuleException("Only Daily missions have reward windows.");
            var start = utcNow.Date.AddSeconds(ResetSecondUtc.Value);
            return utcNow < start ? start.AddDays(-1) : start;
        }

        private static void RequireUtc(DateTime utcNow)
        {
            if (utcNow.Kind != DateTimeKind.Utc)
                throw new MissionRuleException("Mission repeatability requires a UTC clock.");
        }
    }
}
