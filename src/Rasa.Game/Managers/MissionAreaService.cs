using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Game;
    using Structures;
    using Structures.Missions;

    internal sealed class MissionAreaService
    {
        private static MissionAreaService _instance;
        private static readonly object InstanceLock = new();
        private readonly Func<MissionApplication> _missionManager;

        internal static MissionAreaService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        _instance ??= new MissionAreaService();
                    }
                }

                return _instance;
            }
        }

        internal MissionAreaService(Func<MissionApplication> missionManager = null)
        {
            _missionManager = missionManager ?? (() => MissionApplication.Instance);
        }

        internal bool RecordAcceptedMovement(
            Client client,
            Vector3 previousPosition,
            Vector3 currentPosition) =>
            Record(client, area => !Contains(area, previousPosition) && Contains(area, currentPosition));

        /// <summary>
        /// The character has come onto a map (ManifestationManager.AssignPlayer): an area that
        /// is the whole of that map (MissionAreaShape.Map) is entered. No Move takes them into
        /// it - there is no outside of it to come from - so the step
        /// <see cref="RecordAcceptedMovement"/> looks for never comes. It is how "Entered the
        /// Fluxite Mines" is told. An area with a shape is not entered this way: arriving inside
        /// one is as it was, and it is entered by walking into it.
        /// </summary>
        internal bool RecordArrival(Client client) =>
            Record(client, area => area.Shape == Rasa.Structures.World.MissionAreaShape.Map);

        private bool Record(Client client, Func<MissionAreaDefinition, bool> entered)
        {
            if (client?.Player?.MapChannel == null)
                return false;

            var missionManager = _missionManager();
            if (missionManager == null)
                return false;

            var events = new HashSet<(uint MissionId, uint AreaId)>();
            foreach (var missionLog in client.Player.Missions.Values
                         .Where(mission => mission.State == Data.MissionState.Active)
                         .OrderBy(mission => mission.MissionId))
            {
                if (!missionManager.LoadedMissions.TryGetValue(
                        missionLog.MissionId,
                        out var definition) ||
                    !definition.IsOperational)
                    continue;

                foreach (var objective in definition.Objectives.Values
                             .OrderBy(objective => objective.ObjectiveId))
                {
                    if (!missionLog.Objectives.TryGetValue(
                            objective.ObjectiveId,
                            out var runtimeObjective) ||
                        runtimeObjective.State != Data.MissionObjectiveState.Incomplete)
                        continue;

                    foreach (var transition in objective.GetExecutableTransitionsOrLegacyDefault()
                                 .Where(transition =>
                                     transition.ProgressRule?.Kind == Data.MissionProgressEventKind.AreaEntered &&
                                     transition.ProgressRule.ScopeId == missionLog.MissionId)
                                 .OrderBy(transition => transition.Sequence)
                                 .ThenBy(transition => transition.TransitionId))
                    {
                        var areaId = transition.ProgressRule.Subjects.Single();
                        if (!missionManager.TryGetAreaDefinition(
                                missionLog.MissionId,
                                areaId,
                                out var area) ||
                            area.MapContextId != client.Player.MapChannel.MapInfo.MapContextId)
                            continue;

                        if (entered(area))
                            events.Add((missionLog.MissionId, areaId));
                    }
                }
            }

            var changed = false;
            foreach (var (missionId, areaId) in events)
                changed |= missionManager.RecordProgress(
                    client,
                    MissionProgressEvent.Area(missionId, areaId));
            return changed;
        }

        private static bool Contains(
            MissionAreaDefinition area,
            Vector3 position)
        {
            var delta = position - area.Position;
            return area.Shape switch
            {
                Rasa.Structures.World.MissionAreaShape.Sphere =>
                    area.Radius.HasValue &&
                    Vector3.DistanceSquared(position, area.Position) <=
                    area.Radius.Value * area.Radius.Value,
                Rasa.Structures.World.MissionAreaShape.Cylinder =>
                    area.Radius.HasValue &&
                    HorizontalDistanceSquared(delta) <= area.Radius.Value * area.Radius.Value &&
                    (!area.ExtentY.HasValue || Math.Abs(delta.Y) <= area.ExtentY.Value),
                Rasa.Structures.World.MissionAreaShape.Box =>
                    area.ExtentX.HasValue &&
                    area.ExtentY.HasValue &&
                    area.ExtentZ.HasValue &&
                    Math.Abs(delta.X) <= area.ExtentX.Value &&
                    Math.Abs(delta.Y) <= area.ExtentY.Value &&
                    Math.Abs(delta.Z) <= area.ExtentZ.Value,
                Rasa.Structures.World.MissionAreaShape.Map => true,
                _ => false
            };

            static double HorizontalDistanceSquared(Vector3 vector) =>
                vector.X * vector.X + vector.Z * vector.Z;
        }
    }
}
