using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    internal static class MapInstanceScope
    {
        internal static bool Contains(MapChannel mapChannel, Actor actor)
        {
            if (actor == null || mapChannel == null)
                return false;

            return actor switch
            {
                Manifestation player => player.MapContextId == mapChannel.MapInfo.MapContextId &&
                                        ReferenceEquals(player.MapChannel, mapChannel),
                Creature creature => ReferenceEquals(creature.RuntimeMapChannel, mapChannel) ||
                                     mapChannel.MapCellInfo.Cells.Values.Any(cell =>
                                         cell.CreatureList.Any(candidate =>
                                             ReferenceEquals(candidate, creature))),
                _ => ReferenceEquals(actor.RuntimeMapChannel, mapChannel)
            };
        }

        internal static bool Contains(MapChannel mapChannel, DynamicObject obj)
        {
            if (obj == null || mapChannel == null)
                return false;

            return ReferenceEquals(obj.RuntimeMapChannel, mapChannel) ||
                   mapChannel.DynamicObjects.Any(candidate =>
                       ReferenceEquals(candidate, obj)) ||
                   mapChannel.ControlPoints.Values.Any(candidate =>
                       ReferenceEquals(candidate, obj)) ||
                   mapChannel.FootLockers.Values.Any(candidate =>
                       ReferenceEquals(candidate, obj)) ||
                   mapChannel.Teleporters.Values.Any(candidate =>
                       ReferenceEquals(candidate, obj)) ||
                   mapChannel.Kraftwerks.Values.Any(candidate =>
                       ReferenceEquals(candidate, obj)) ||
                   mapChannel.MapCellInfo.Cells.Values.Any(cell =>
                       cell.DynamicObjectList.Any(candidate =>
                           ReferenceEquals(candidate, obj)));
        }

        internal static bool Share(MapChannel mapChannel, Actor actor, DynamicObject obj) =>
            Contains(mapChannel, actor) && Contains(mapChannel, obj);

        internal static bool TryGetCreature(
            MapChannel mapChannel,
            ulong entityId,
            out Creature creature)
        {
            creature = null;
            if (mapChannel == null ||
                EntityManager.Instance.GetEntityType(entityId) != EntityType.Creature ||
                !EntityManager.Instance.Creatures.TryGetValue(entityId, out var candidate) ||
                !Contains(mapChannel, candidate))
                return false;

            creature = candidate;
            return true;
        }
    }
}
