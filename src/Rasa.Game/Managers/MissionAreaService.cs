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
            Vector3 currentPosition)
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

                        if (!Contains(area, previousPosition) &&
                            Contains(area, currentPosition))
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
                _ => false
            };

            static double HorizontalDistanceSquared(Vector3 vector) =>
                vector.X * vector.X + vector.Z * vector.Z;
        }
    }
}
