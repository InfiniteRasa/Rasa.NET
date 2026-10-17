using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Server objects that are part of the view: seen from further off than their cells.
    ///
    /// A client is given the objects of the five by five cells around its player, 51 to 77 m
    /// out depending on where in its cell the player stands. The map's own scenery is the
    /// client's to draw from the .map, as far as it draws; an object the server puts down is
    /// not there until the player is within those cells. For most that is right. For one that
    /// stands in the scenery - the rocks that block the Proving Grounds' cavern doorway, west of
    /// the bridge (BootcampCaveInBreach) - it is a hole in the view: the open doorway seen from
    /// across the bridge, and the pile appearing in it halfway over.
    ///
    /// An object with a <see cref="DynamicObject.ViewRadius"/> (from its scene actor's
    /// ViewRadius) is a landmark, given to every client whose player is within that radius of
    /// it, wherever its cells are, as <see cref="MissionContacts"/> gives a mission's NPC:
    ///  - <see cref="Worker"/> gives it to a client that has come within the radius and does not
    ///    hold it, and takes it from one that has gone <see cref="LetGoMargin"/> past it;
    ///  - one the player walks away from out of its cells stays on their client while they are
    ///    within the radius (<see cref="Keep"/>), rather than going with its cells and coming
    ///    back on the next pass;
    ///  - what its cells are told, a client holding it from afar is told too (<see cref="Relay"/>):
    ///    a state change - the rocks blown open - is seen from across the bridge;
    ///  - walking into its cells takes it off the client and gives it again as the cells give any
    ///    object (<see cref="Entering"/>): made anew, not updated;
    ///  - taken out of the world, it is taken off every client holding it from afar
    ///    (<see cref="Removed"/>): no cell would tell them.
    /// </summary>
    public static class Landmarks
    {
        /// <summary>How far past its radius a client holding a landmark from afar goes before it is taken away: no blinking at the edge.</summary>
        public const float LetGoMargin = 10f;

        private static readonly Dictionary<DynamicObject, HashSet<Client>> Held = new();
        private static readonly object HeldLock = new();

        /// <summary>Whether the client holds the object from beyond its cells.</summary>
        public static bool HeldFromAfar(Client client, DynamicObject obj)
        {
            lock (HeldLock)
                return Held.TryGetValue(obj, out var holders) && holders.Contains(client);
        }

        /// <summary>
        /// Gives each landmark on the map to the clients within its radius that do not hold it, and
        /// takes it from those held from afar that have gone past it or left. Run with the cells'
        /// own work, once a second (CellManager.DoWork).
        /// </summary>
        public static void Worker(MapChannel mapChannel)
        {
            var landmarks = mapChannel.DynamicObjects.Where(obj => obj.ViewRadius > 0 && obj.RuntimeMapChannel == mapChannel).ToList();

            if (landmarks.Count == 0)
                return;

            var clients = mapChannel.ClientList.Where(client => IsHere(mapChannel, client)).ToList();

            foreach (var obj in landmarks)
            {
                if (!CellManager.TryGetCellCoordinates(obj.CellPosition, out var cellX, out var cellZ))
                    continue;

                var inCells = CellManager.Instance.GetClientsInCells(mapChannel, CellManager.Instance.CreateCellMatrix(mapChannel, cellX, cellZ)).ToHashSet();
                var give = new List<Client>();
                var take = new List<Client>();

                lock (HeldLock)
                {
                    if (!Held.TryGetValue(obj, out var holders))
                        Held[obj] = holders = new HashSet<Client>();

                    // Gone from the map or the game: their client let go of it with everything else.
                    holders.RemoveWhere(client => !IsHere(mapChannel, client));

                    foreach (var client in clients)
                    {
                        // Its cells give it, and Entering took it off the far list.
                        if (inCells.Contains(client))
                        {
                            holders.Remove(client);
                            continue;
                        }

                        var distance = Vector3.Distance(client.Player.Position, obj.Position);

                        if (!holders.Contains(client) && distance <= obj.ViewRadius)
                            give.Add(client);
                        else if (holders.Contains(client) && distance > obj.ViewRadius + LetGoMargin)
                        {
                            holders.Remove(client);
                            take.Add(client);
                        }
                    }
                }

                foreach (var client in give)
                {
                    DynamicObjectManager.Instance.CreateDynamicObjectOnClient(client, obj);

                    lock (HeldLock)
                        Held[obj].Add(client);
                }

                foreach (var client in take)
                    client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(obj.EntityId));
            }
        }

        /// <summary>
        /// The client is about to be given the object as its cells give it
        /// (DynamicObjectManager.CreateDynamicObjectOnClient): if it holds it from afar, it is
        /// taken off first, so the object is made anew rather than updated.
        /// </summary>
        public static void Entering(Client client, DynamicObject obj)
        {
            lock (HeldLock)
            {
                if (!Held.TryGetValue(obj, out var holders) || !holders.Remove(client))
                    return;
            }

            client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(obj.EntityId));
        }

        /// <summary>
        /// Of the objects the client's player has walked out of the cells of, the ones it is to
        /// be sent away: a landmark still within its radius stays, held from afar.
        /// </summary>
        public static List<DynamicObject> Keep(Client client, List<DynamicObject> leaving)
        {
            var player = client?.Player;

            if (player == null || leaving.Count == 0)
                return leaving;

            var discard = new List<DynamicObject>();

            lock (HeldLock)
                foreach (var obj in leaving)
                {
                    if (obj.ViewRadius > 0 && Vector3.Distance(player.Position, obj.Position) <= obj.ViewRadius)
                    {
                        if (!Held.TryGetValue(obj, out var holders))
                            Held[obj] = holders = new HashSet<Client>();

                        holders.Add(client);
                    }
                    else
                        discard.Add(obj);
                }

            return discard;
        }

        /// <summary>What the object's cells were told, to the clients holding it from afar.</summary>
        public static void Relay(DynamicObject obj, PythonPacket packet)
        {
            List<Client> holders;

            lock (HeldLock)
            {
                if (!Held.TryGetValue(obj, out var held) || held.Count == 0)
                    return;

                holders = held.ToList();
            }

            foreach (var client in holders)
                client.CallMethod(obj.EntityId, packet);
        }

        /// <summary>The object has been taken out of the world: off every client holding it from afar.</summary>
        public static void Removed(DynamicObject obj)
        {
            List<Client> holders;

            lock (HeldLock)
            {
                if (!Held.Remove(obj, out var held))
                    return;

                holders = held.ToList();
            }

            foreach (var client in holders)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(obj.EntityId));
        }

        /// <summary>The client has left the map: it holds nothing from afar any more.</summary>
        public static void Forget(Client client)
        {
            lock (HeldLock)
                foreach (var holders in Held.Values)
                    holders.Remove(client);
        }

        private static bool IsHere(MapChannel mapChannel, Client client) =>
            client?.Player != null && client.State == ClientState.Ingame && client.Player.MapChannel == mapChannel && !client.Player.Disconected;
    }
}
