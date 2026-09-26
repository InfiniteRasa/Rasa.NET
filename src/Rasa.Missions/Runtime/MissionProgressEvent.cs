namespace Rasa.Structures
{
    using Data;

    public readonly struct MissionProgressEvent
    {
        public MissionProgressEventKind Kind { get; }
        public uint SubjectId { get; }
        public uint Quantity { get; }
        public uint? ScopeId { get; }
        public uint? DetailId { get; }

        private MissionProgressEvent(
            MissionProgressEventKind kind,
            uint subjectId,
            uint quantity = 1,
            uint? scopeId = null,
            uint? detailId = null)
        {
            Kind = kind;
            SubjectId = subjectId;
            Quantity = quantity;
            ScopeId = scopeId;
            DetailId = detailId;
        }

        internal static MissionProgressEvent Restore(MissionProgressEventKind kind, uint subject,
            uint quantity, uint? scope, uint? detail) => new(kind, subject, quantity, scope, detail);

        public static MissionProgressEvent Waypoint(uint waypointId) =>
            new(MissionProgressEventKind.WaypointAcquired, waypointId);

        public static MissionProgressEvent Logos(uint logosId) =>
            new(MissionProgressEventKind.LogosAcquired, logosId);

        public static MissionProgressEvent Creature(uint creatureDbId) =>
            new(MissionProgressEventKind.CreatureKilled, creatureDbId);

        public static MissionProgressEvent Mission(uint completedMissionId) =>
            new(MissionProgressEventKind.MissionCompleted, completedMissionId);

        public static MissionProgressEvent ItemAcquired(
            uint itemClassId,
            uint quantity) =>
            new(MissionProgressEventKind.ItemAcquired, itemClassId, quantity);

        public static MissionProgressEvent ItemConsumed(
            uint itemClassId,
            uint quantity) =>
            new(MissionProgressEventKind.ItemConsumed, itemClassId, quantity);

        public static MissionProgressEvent Interaction(uint entityClassId) =>
            new(MissionProgressEventKind.InteractionUsed, entityClassId);

        public static MissionProgressEvent Area(uint missionId, uint areaId) =>
            new(
                MissionProgressEventKind.AreaEntered,
                areaId,
                scopeId: missionId);

        public static MissionProgressEvent ItemEquipped(
            uint itemClassId,
            uint itemTemplateId) =>
            new(
                MissionProgressEventKind.ItemEquipped,
                itemClassId,
                detailId: itemTemplateId);

        public static MissionProgressEvent AbilityHit(
            uint actionId,
            uint targetCreatureId) =>
            new(
                MissionProgressEventKind.AbilityHit,
                actionId,
                detailId: targetCreatureId);

        public static MissionProgressEvent ObjectHit(uint entityClassId, uint actionId) =>
            new(MissionProgressEventKind.ObjectHit, entityClassId, detailId: actionId);

        public static MissionProgressEvent Scenario(
            uint missionId,
            uint scenarioId,
            uint scenarioEventId) =>
            new(
                MissionProgressEventKind.ScenarioEvent,
                scenarioEventId,
                scopeId: missionId,
                detailId: scenarioId);

        public static MissionProgressEvent Deadline(
            uint missionId,
            uint objectiveId) =>
            new(
                MissionProgressEventKind.DeadlineElapsed,
                objectiveId,
                scopeId: missionId);

        public static MissionProgressEvent ObjectiveState(
            uint missionId,
            uint objectiveId,
            byte state) =>
            new(
                MissionProgressEventKind.ObjectiveStateReached,
                objectiveId,
                scopeId: missionId,
                detailId: state);
    }
}
