using System;

namespace Rasa.Missions.Runtime
{
    public enum MissionCreditMode { Personal, NearbyParty, EncounterParticipants }

    public sealed record MissionCreditPolicy
    {
        public static MissionCreditPolicy Personal { get; } = new(MissionCreditMode.Personal);
        public MissionCreditMode Mode { get; }
        public float Radius { get; }

        public MissionCreditPolicy(MissionCreditMode mode, float radius = 0)
        {
            if (!Enum.IsDefined(typeof(MissionCreditMode), mode) ||
                !float.IsFinite(radius) || radius < 0 || mode != MissionCreditMode.Personal && radius == 0)
                throw new ArgumentException("Group credit needs an explicit positive finite radius.");
            Mode = mode; Radius = radius;
        }

        public bool Eligible(bool sourceCharacter, bool sameMap, bool sameParty, bool participant, float distance) =>
            sameMap && (sourceCharacter && Mode == MissionCreditMode.Personal ||
                Mode != MissionCreditMode.Personal && (sourceCharacter || sameParty) &&
                float.IsFinite(distance) && distance <= Radius &&
                (Mode != MissionCreditMode.EncounterParticipants || participant));
    }
}
