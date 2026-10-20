using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A usable lock's mission requirement, resolved by the server for each player.
    ///
    /// The client opens a lock by a mission itself: usable.py CanUnlock asks
    /// HasCompletedMission(lockMissionId), and useobject.py will not send the use of a locked
    /// object it cannot unlock. HasCompletedMission reads the client's mission log, which knows a
    /// turned-in mission only from the MissionRewarded it was sent in the session: MissionStatusInfo
    /// replaces the log and carries the missions in hand, not the turned-in ones (each would cost
    /// it about 47 bytes, and the whole message has to fit one send block). So after a relog, or
    /// the next status refresh, a lock a completed mission opens would be refused by the client.
    ///
    /// The server knows what each player has completed (Player.Missions), so it decides: a lock
    /// whose mission the player has completed is sent to that player as open (LockInfo's
    /// unlocked), and to everyone else as it is. The client then lets them use it, with no lock
    /// icon over it, which is how an open lock reads. When a player's missions change, every
    /// locked object in their cells whose answer has changed is sent again (<see cref="Refresh"/>,
    /// after every mission change, as MissionObjects is).
    ///
    /// Nothing in the world carries a lock with a mission yet (UsableLock).
    /// </summary>
    public static class MissionLocks
    {
        /// <summary>Whether the lock is open for this player by a mission they have completed, though not open for everyone.</summary>
        public static bool OpensFor(UsableLock usableLock, Manifestation player)
        {
            if (usableLock == null || usableLock.Unlocked || usableLock.MissionId == 0 || player?.Missions == null)
                return false;

            return player.Missions.TryGetValue(usableLock.MissionId, out var mission) && mission.State == MissionState.Completed;
        }

        /// <summary>The object's LockInfo as this client is to have it; what it has been told is noted for <see cref="Refresh"/>.</summary>
        public static LockInfoPacket InfoFor(Client client, DynamicObject obj)
        {
            var open = OpensFor(obj.Lock, client?.Player);

            if (client != null)
            {
                if (open)
                    client.MissionLocks.Add(obj.EntityId);
                else
                    client.MissionLocks.Remove(obj.EntityId);
            }

            return new LockInfoPacket(obj.Lock, open);
        }

        /// <summary>
        /// After a change to the player's missions: every locked object in their cells that a
        /// mission opens, whose answer for them has changed, is sent to them again.
        /// </summary>
        public static void Refresh(Client client)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null || player.Cells == null || client.State != ClientState.Ingame)
                return;

            var inView = new HashSet<ulong>();

            foreach (var obj in CellManager.CellsIn(mapChannel, player.Cells).SelectMany(cell => cell.DynamicObjectList).Distinct().ToList())
            {
                if (obj.Lock == null || obj.Lock.MissionId == 0)
                    continue;

                inView.Add(obj.EntityId);

                var open = OpensFor(obj.Lock, player);

                if (open != client.MissionLocks.Contains(obj.EntityId))
                    client.CallMethod(obj.EntityId, InfoFor(client, obj));
            }

            // What has left the cells is gone from the client; it is told again when met again.
            client.MissionLocks.RemoveWhere(entityId => !inView.Contains(entityId));
        }
    }
}
