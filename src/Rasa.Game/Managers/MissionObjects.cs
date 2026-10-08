using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Missions;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The sparkle on an object a player's mission wants used.
    ///
    /// The client puts MISSION_USABLE_INDICATOR on a usable object that is in service and
    /// "mission activated": the fifth argument of UsableInfo (client/augmentations/usable.py
    /// Recv_UsableInfo keeps it, _SetEnabled attaches the effect). The server always sent 0, so
    /// no object ever had it.
    ///
    /// An object is mission activated for a player while using it would move one of their
    /// missions on: its entity class is the subject of an InteractionUsed trigger on a
    /// transition of an objective they have active (MissionApplication.WantedInteractions),
    /// which is the event a finished use of such an object records
    /// (DynamicObjectManager.FootlockerRecovery). It is the player's and not the object's, so
    /// every UsableInfo of an object is made for the client it goes to (<see cref="InfoFor"/>).
    ///
    /// The client only takes the effect off when the object goes out of service: an object still
    /// in service that is told it is no longer mission activated keeps sparkling. So when a
    /// mission's progress has taken the want away (<see cref="Refresh"/>, after every mission
    /// change), the object is sent out of service with no activation, which detaches the effect,
    /// and then put back in service if it is.
    ///
    /// An object a mission talks through (MissionConversation) has an NPC's status and no
    /// UsableInfo, so it is not given this.
    /// </summary>
    public static class MissionObjects
    {
        /// <summary>
        /// The mission this object is activated by for this client's player, or 0: what
        /// UsableInfo's missionActivated is sent as.
        /// </summary>
        public static uint ActivationFor(Client client, DynamicObject obj, MissionApplication missions = null)
        {
            if (obj == null)
                return 0;

            if (obj.ActivateMission != 0)
                return obj.ActivateMission;

            var player = client?.Player;

            if (player?.Missions == null || player.Missions.Count == 0)
                return 0;

            if (obj.MissionConversation != null)
                return 0;

            if (!MapInstanceScope.Contains(player.MapChannel, obj))
                return 0;

            var wanted = (missions ?? MissionApplication.Instance).WantedInteractions(player);

            return wanted.TryGetValue((uint)obj.EntityClassId, out var missionId) ? missionId : 0;
        }

        /// <summary>
        /// The object's UsableInfo as this client is to have it, with the mission that activates
        /// it for its player; what the client has been told is noted for <see cref="Refresh"/>.
        /// </summary>
        public static UsableInfoPacket InfoFor(Client client, DynamicObject obj, bool enabled, uint windupTime, MissionApplication missions = null)
        {
            var activation = ActivationFor(client, obj, missions);

            if (client != null)
            {
                if (activation != 0)
                    client.MissionObjects.Add(obj.EntityId);
                else
                    client.MissionObjects.Remove(obj.EntityId);
            }

            return new UsableInfoPacket(enabled, obj.StateId, obj.NameOverrideId, windupTime, activation);
        }

        /// <summary>
        /// After a change to the player's missions: every object in their cells whose activation
        /// for them has changed is told so.
        /// </summary>
        public static void Refresh(Client client, MissionApplication missions = null)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null || player.Cells == null || client.State != ClientState.Ingame)
                return;

            // Nothing to give and nothing given: most players, most of the time.
            if (client.MissionObjects.Count == 0 && (player.Missions == null || player.Missions.Count == 0))
                return;

            var inView = new HashSet<ulong>();

            foreach (var obj in CellManager.CellsIn(mapChannel, player.Cells).SelectMany(cell => cell.DynamicObjectList).Distinct().ToList())
            {
                inView.Add(obj.EntityId);

                if (obj.DynamicObjectType == DynamicObjectType.Emitter || obj.DynamicObjectType == DynamicObjectType.Scenery ||
                    obj.DynamicObjectType == DynamicObjectType.AmbientNpc || obj.MissionConversation != null)
                    continue;

                var activation = ActivationFor(client, obj, missions);
                var told = client.MissionObjects.Contains(obj.EntityId);

                if (activation != 0 && !told)
                {
                    client.MissionObjects.Add(obj.EntityId);
                    client.CallMethod(obj.EntityId, new UsableInfoPacket(obj.IsEnabled, obj.StateId, obj.NameOverrideId, obj.WindupTime, activation));
                }
                else if (activation == 0 && told)
                {
                    client.MissionObjects.Remove(obj.EntityId);

                    // Out of service takes the effect off; in service again without it.
                    client.CallMethod(obj.EntityId, new UsableInfoPacket(false, obj.StateId, obj.NameOverrideId, obj.WindupTime, 0));

                    if (obj.IsEnabled)
                        client.CallMethod(obj.EntityId, new SetUsablePacket(true));
                }
            }

            // What has left the cells is gone from the client; it is made again when met again.
            client.MissionObjects.RemoveWhere(entityId => !inView.Contains(entityId));
        }
    }
}
