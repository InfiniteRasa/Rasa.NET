using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;

    public class CellManager
    {
        private static CellManager _instance;
        private static readonly object InstanceLock = new object();
        public static readonly float CellSize = 25.6f;
        public static readonly float CellBias = 32768.0f;
        private const uint CellViewRange = 2;
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        public static CellManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new CellManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private CellManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        //creature
        public void AddToWorld(MapChannel mapChannel, Creature creature)
        {
            if (creature == null)
                return;
            creature.RuntimeMapChannel = mapChannel;
            // register creature entity
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);

            // calculate initial cell(x, z)
            var cellPosX = (uint)(creature.Position.X / CellSize + CellBias);
            var cellPosZ = (uint)(creature.Position.Z / CellSize + CellBias);

            // create matrix
            var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

            // add creature to the cell
            mapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].CreatureList.Add(creature);
            // add cellMatrix to creature
            creature.Cells = cellMatrix;

            // notify client's about new creatures
            var ListOfClients = new List<Client>();

            foreach (var cellSeed in cellMatrix)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    ListOfClients.Add(client);

            CreatureManager.Instance.CellIntroduceCreatureToClients(mapChannel, creature, ListOfClients);
        }

        //mapTrigger
        public void AddToWorld(MapChannel mapChannel, MapTrigger trigger)
        {
            if (trigger == null)
                return;

            // calculate initial cell(x, z)
            var cellPosX = (uint)(trigger.Position.X / CellSize + CellBias);
            var cellPosZ = (uint)(trigger.Position.Z / CellSize + CellBias);

            // create matrix
            var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

            // add mapTrigger to the cell
            mapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].MapTriggers.Add(trigger);
        }

        //mapLink
        public void AddToWorld(MapChannel mapChannel, MapLink link)
        {
            if (link == null)
                return;

            var cellPosX = (uint)(link.Position.X / CellSize + CellBias);
            var cellPosZ = (uint)(link.Position.Z / CellSize + CellBias);

            // The matrix is built so the link's neighbours exist for the players who will look
            // at it from up to two cells away.
            var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

            mapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].MapLinks.Add(link);
        }

        public void RemoveFromWorld(MapChannel mapChannel, MapLink link)
        {
            if (link == null)
                return;

            var cellSeed = GetCellSeed(link.Position);

            if (mapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out var cell))
                cell.MapLinks.Remove(link);
        }

        // Object
        public void AddToWorld(MapChannel mapChannel, DynamicObject dynamicObject)
        {
            if (dynamicObject == null)
                return;
            dynamicObject.RuntimeMapChannel = mapChannel;

            // register object entity
            EntityManager.Instance.RegisterEntity(dynamicObject.EntityId, EntityType.Object);
            EntityManager.Instance.RegisterDynamicObject(dynamicObject);

            // calculate initial cell(x, z)
            var cellPosX = (uint)(dynamicObject.Position.X / CellSize + CellBias);
            var cellPosZ = (uint)(dynamicObject.Position.Z / CellSize + CellBias);

            // create matrix
            var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

            // add Object to the cell
            mapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].DynamicObjectList.Add(dynamicObject);

            // notify client's about new object
            var ListOfClients = new List<Client>();

            foreach (var cellSeed in cellMatrix)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    ListOfClients.Add(client);

            DynamicObjectManager.Instance.CellIntroduceDynamicObjectToClients(dynamicObject, ListOfClients);
        }

        // Player
        public void AddToWorld(Client client)
        {
            if (client.Player?.MapChannel == null ||
                !TryGetCellCoordinates(client.Player.Position, out var CellPosX, out var CellPosZ))
            {
                Logger.WriteLog(LogType.Error, "Cannot add a player without a valid map position.");
                return;
            }
            if (IsInWorld(client))
            {
                UpdateVisibility(client);
                return;
            }

            // create matrix
            var cellMatrix = CreateCellMatrix(client.Player.MapChannel, CellPosX, CellPosZ);

            // add client to the cell
            client.Player.MapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].ClientList.Add(client);
            // add cellMatrix to client
            client.Player.Cells = cellMatrix;
            client.Player.RuntimeMapChannel = client.Player.MapChannel;

            // notify client about players, creatures, objects
            var ListOfClients = GetClientsInCells(client.Player.MapChannel, cellMatrix);
            var ListOfCreatures = new List<Creature>();
            var ListOfObjects = new List<DynamicObject>();

            foreach (var cellSeed in cellMatrix)
            {
                foreach (var creature in client.Player.MapChannel.MapCellInfo.Cells[cellSeed].CreatureList)
                    ListOfCreatures.Add(creature);

                foreach (var dinamicObject in client.Player.MapChannel.MapCellInfo.Cells[cellSeed].DynamicObjectList)
                    ListOfObjects.Add(dinamicObject);

            }

            ManifestationManager.Instance.CellIntroduceClientToSefl(client);
            ManifestationManager.Instance.CellIntroduceClientToPlayers(client, ListOfClients);
            ManifestationManager.Instance.CellIntroducePlayersToClient(client, ListOfClients);

            CreatureManager.Instance.CellIntroduceCreaturesToClient(client, ListOfCreatures);
            DynamicObjectManager.Instance.CellIntroduceDynamicObjectsToClient(client, ListOfObjects);
        }

        internal bool RemoveCreatureFromWorld(MapChannel mapChannel, Creature creature)
        {
            if (!MapInstanceScope.Contains(mapChannel, creature))
                return false;

            Game.Missions.World.CreatureGameplayRules.ClearRole(creature);
            var isRegistered = EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) &&
                registered == creature;
            if (isRegistered)
                foreach (var player in GetClientsInCells(mapChannel, creature.Cells))
                    player.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));
            foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                cell.CreatureList.RemoveAll(candidate => candidate == creature);
            if (!isRegistered)
                return false;

            LootDispenserManager.Instance.RemoveForCreature(mapChannel, creature);
            EntityManager.Instance.ReleaseEntity(creature.EntityId, EntityType.Creature);
            creature.RuntimeMapChannel = null;
            if (creature.State == CharacterState.Dead && creature.SpawnPool != null)
                SpawnPoolManager.Instance.DecreaseDeadCreatureCount(creature.SpawnPool);
            return true;
        }

        public void DoWork(MapChannel mapChannel)
        {
            // 1 time per sec, do we need check more often?
            UpdateVisibility(mapChannel);
            // mob work

            // events etc...
        }

        public MapCell GetCell(MapChannel mapChannel, uint cellPosX, uint cellPosZ)
        {
            if (cellPosX > ushort.MaxValue || cellPosZ > ushort.MaxValue)
                throw new InvalidDataException("Cell coordinates exceed their 16-bit key fields.");
            var cellSeed = (cellPosX & 0xFFFF) | (cellPosZ << 16);

            if (mapChannel.MapCellInfo.Cells.ContainsKey(cellSeed))
                return mapChannel.MapCellInfo.Cells[cellSeed];
            else
            {
                //create new cell
                var cell = new MapCell
                {
                    CellSeed = cellSeed,
                    CellPosX = cellPosX,
                    CellPosZ = cellPosZ
                };

                // register cell
                mapChannel.MapCellInfo.Cells.Add(cellSeed, cell);

                return cell;
            }
        }

        public void RemoveFromWorld(MapChannel mapChannel, DynamicObject dynObject)
        {
            if (!MapInstanceScope.Contains(mapChannel, dynObject))
                return;

            if (dynObject.LootDispenserEntityId != 0)
                LootDispenserManager.Instance.RemoveForObject(mapChannel, dynObject);

            // unregister object entity
            EntityManager.Instance.UnregisterEntity(dynObject.EntityId);
            EntityManager.Instance.UnregisterDynamicObject(dynObject.EntityId);
            EntityManager.Instance.FreeEntity(dynObject.EntityId);
            dynObject.RuntimeMapChannel = null;

            var cellX = (uint)((dynObject.Position.X / CellSize) + CellBias);
            var cellZ = (uint)((dynObject.Position.Z / CellSize) + CellBias);
            var cellMatrix = CreateCellMatrix(mapChannel, cellX, cellZ);
            var ListOfClients = new List<Client>();

            foreach (var cellSeed in cellMatrix)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    ListOfClients.Add(client);

            DynamicObjectManager.Instance.CellDiscardDynamicObjectToClients(dynObject.EntityId, ListOfClients);

            // remove object from cell
            mapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].DynamicObjectList.Remove(dynObject);
        }

        public void RemoveFromWorld(MapChannel mapChannel, ulong entityId)
        {
            if (entityId == 0)
                return;

            DynamicObjectManager.Instance.CellDiscardDynamicObjectToClients(entityId,
                mapChannel.ClientList.Where(client => client?.Player?.MapChannel == mapChannel &&
                    client.State != ClientState.Disconnected).Distinct().ToList());
        }

        public uint GetCellSeed(Vector3 position)
        {
            if (!TryGetCellCoordinates(position, out var cellPosX, out var cellPosZ))
                throw new InvalidDataException("Position cannot be represented by the world cell grid.");
            var cellSeed = (cellPosX & 0xFFFF) | (cellPosZ << 16);

            return cellSeed;
        }

        public void RemoveFromWorld(Client client)
        {
            var mapChannel = client.Player?.MapChannel;
            if (mapChannel == null)
                return;
            DetachClient(mapChannel, client);
        }

        internal void DetachClient(MapChannel map, Client client)
        {
            if (client.Player?.MapChannel == map)
                client.InvalidateMissionSession();
            if (client.MissionConversation?.Map == map)
                client.MissionConversation = null;
            var memberships = map.MapCellInfo.Cells.Values.Where(cell => cell.ClientList.Contains(client)).ToArray();
            if (memberships.Length == 0)
                return;
            var observers = new HashSet<Client>();
            foreach (var cell in memberships)
            {
                var cells = CreateCellMatrix(map, cell.CellPosX, cell.CellPosZ);
                observers.UnionWith(GetClientsInCells(map, cells, client));
            }
            var notify = observers.ToList();
            ManifestationManager.Instance.CellDiscardClientToPlayers(client, notify);
            if (client.State != ClientState.Disconnected)
                ManifestationManager.Instance.CellDiscardPlayersToClient(client, notify);

            foreach (var cell in memberships)
                cell.ClientList.RemoveAll(player => player == client);
            if (client.Player.MapChannel == map)
            {
                client.Player.Cells = new uint[5, 5];
                client.Player.RuntimeMapChannel = null;
            }
        }

        public void UpdateVisibility(MapChannel mapChannel)
        {
            foreach (var client in mapChannel.ClientList.ToArray())
                if (client?.Player?.MapChannel == mapChannel)
                    UpdateVisibility(client);
        }

        internal void UpdateVisibility(Client client)
        {
            Game.Missions.Integration.MissionInteractionPolicy.InvalidateIfUnavailable(client);
            var player = client.Player;
            if (player?.MapChannel == null || player.Disconected ||
                client.State == ClientState.Disconnected || client.State == ClientState.Loading)
                return;
            if (!TryGetCellCoordinates(player.Position, out var x, out var z))
            {
                Logger.WriteLog(LogType.Error, $"Cannot update visibility for invalid player position: {player.EntityId}.");
                return;
            }
            if (!IsInWorld(client))
            {
                AddToWorld(client);
                return;
            }

            var map = player.MapChannel;
            var newCenter = (x & 0xFFFF) | (z << 16);
            if (player.Cells[2, 2] == newCenter)
            {
                var members = map.MapCellInfo.Cells[newCenter].ClientList;
                if (members.Count(member => member == client) > 1)
                {
                    members.RemoveAll(member => member == client);
                    members.Add(client);
                }
                return;
            }

            var next = CreateCellMatrix(map, x, z);
            GetCellMatrixDiff(player.Cells, next, out var added, out var removed);
            var leaving = GetClientsInCells(map, removed, client);
            var removedCells = GetCells(map, removed).ToList();
            foreach (var cell in GetCells(map, player.Cells.Cast<uint>()))
                cell.ClientList.RemoveAll(member => member == client);
            map.MapCellInfo.Cells[newCenter].ClientList.Add(client);
            player.Cells = next;

            ManifestationManager.Instance.CellDiscardClientToPlayers(client, leaving);
            ManifestationManager.Instance.CellDiscardPlayersToClient(client, leaving);
            CreatureManager.Instance.CellDiscardCreaturesToClient(client,
                removedCells.SelectMany(cell => cell.CreatureList).Distinct().ToList());
            DynamicObjectManager.Instance.CellDiscardDynamicObjectsToClient(client,
                removedCells.SelectMany(cell => cell.DynamicObjectList).Distinct().ToList());

            var entering = GetClientsInCells(map, added, client);
            var addedCells = GetCells(map, added).ToList();
            ManifestationManager.Instance.CellIntroduceClientToPlayers(client, entering);
            ManifestationManager.Instance.CellIntroducePlayersToClient(client, entering);
            CreatureManager.Instance.CellIntroduceCreaturesToClient(client,
                addedCells.SelectMany(cell => cell.CreatureList).Distinct().ToList());
            DynamicObjectManager.Instance.CellIntroduceDynamicObjectsToClient(client,
                addedCells.SelectMany(cell => cell.DynamicObjectList).Distinct().ToList());
        }

        internal static bool TryGetCellCoordinates(Vector3 position, out uint x, out uint z)
        {
            x = z = 0;
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                return false;
            var biasedX = position.X / CellSize + CellBias;
            var biasedZ = position.Z / CellSize + CellBias;
            if (biasedX < CellViewRange || biasedZ < CellViewRange ||
                biasedX >= ushort.MaxValue - CellViewRange + 1 ||
                biasedZ >= ushort.MaxValue - CellViewRange + 1)
                return false;
            x = (uint)biasedX;
            z = (uint)biasedZ;
            return true;
        }

        internal bool IsInWorld(Client client)
        {
            var player = client.Player;
            return player?.MapChannel != null && player.Cells != null &&
                player.MapChannel.MapCellInfo.Cells.TryGetValue(player.Cells[2, 2], out var cell) &&
                cell.ClientList.Contains(client);
        }

        internal List<Client> GetClientsInCells(MapChannel map, uint[,] cells, Client excluded = null)
        {
            return GetClientsInCells(map, cells.Cast<uint>(), excluded);
        }

        private static List<Client> GetClientsInCells(MapChannel map, IEnumerable<uint> cells, Client excluded)
        {
            return GetCells(map, cells).SelectMany(cell => cell.ClientList)
                .Where(client => client != null && client != excluded &&
                    client.State == ClientState.Ingame && client.Player?.MapChannel == map &&
                    !client.Player.Disconected)
                .Distinct().ToList();
        }

        private static IEnumerable<MapCell> GetCells(MapChannel map, IEnumerable<uint> seeds)
        {
            foreach (var seed in seeds.Distinct())
                if (map.MapCellInfo.Cells.TryGetValue(seed, out var cell))
                    yield return cell;
        }

        public void GetCellMatrixDiff(uint[,] oldCellMatrix, uint[,] newCellMatrix, out List<uint> needUpdate, out List<uint> needDelete)
        {
            var oldCells = new List<uint>();
            var newCells = new List<uint>();

            // find all cells that need to be removed
            foreach (var oldCell in oldCellMatrix)
            {
                var found = false;

                foreach (var newCell in newCellMatrix)
                    if (newCell == oldCell)
                    {
                        found = true;
                        break;
                    }

                if (!found)
                    oldCells.Add(oldCell);
            }

            // find all cells that need to be added
            foreach (var newCell in newCellMatrix)
            {
                var found = false;

                foreach (var oldCell in oldCellMatrix)
                    if (oldCell == newCell)
                    {
                        found = true;
                        break;
                    }

                if (!found)
                    newCells.Add(newCell);
            }

            needDelete = oldCells;
            needUpdate = newCells;
        }

        public uint[,] CreateCellMatrix(MapChannel mapChannel, uint cellPosX, uint cellPosZ)
        {
            var cellMatrix = new uint[5, 5];
            cellMatrix[0, 0] = GetCell(mapChannel, cellPosX - 2, cellPosZ - 2).CellSeed;
            cellMatrix[0, 1] = GetCell(mapChannel, cellPosX - 2, cellPosZ - 1).CellSeed;
            cellMatrix[0, 2] = GetCell(mapChannel, cellPosX - 2, cellPosZ).CellSeed;
            cellMatrix[0, 3] = GetCell(mapChannel, cellPosX - 2, cellPosZ + 1).CellSeed;
            cellMatrix[0, 4] = GetCell(mapChannel, cellPosX - 2, cellPosZ + 2).CellSeed;
            cellMatrix[1, 0] = GetCell(mapChannel, cellPosX - 1, cellPosZ - 2).CellSeed;
            cellMatrix[1, 1] = GetCell(mapChannel, cellPosX - 1, cellPosZ - 1).CellSeed;
            cellMatrix[1, 2] = GetCell(mapChannel, cellPosX - 1, cellPosZ).CellSeed;
            cellMatrix[1, 3] = GetCell(mapChannel, cellPosX - 1, cellPosZ + 1).CellSeed;
            cellMatrix[1, 4] = GetCell(mapChannel, cellPosX - 1, cellPosZ + 2).CellSeed;
            cellMatrix[2, 0] = GetCell(mapChannel, cellPosX, cellPosZ - 2).CellSeed;
            cellMatrix[2, 1] = GetCell(mapChannel, cellPosX, cellPosZ - 1).CellSeed;
            cellMatrix[2, 2] = GetCell(mapChannel, cellPosX, cellPosZ).CellSeed;        // Actor is here
            cellMatrix[2, 3] = GetCell(mapChannel, cellPosX, cellPosZ + 1).CellSeed;
            cellMatrix[2, 4] = GetCell(mapChannel, cellPosX, cellPosZ + 2).CellSeed;
            cellMatrix[3, 0] = GetCell(mapChannel, cellPosX + 1, cellPosZ - 2).CellSeed;
            cellMatrix[3, 1] = GetCell(mapChannel, cellPosX + 1, cellPosZ - 1).CellSeed;
            cellMatrix[3, 2] = GetCell(mapChannel, cellPosX + 1, cellPosZ).CellSeed;
            cellMatrix[3, 3] = GetCell(mapChannel, cellPosX + 1, cellPosZ + 1).CellSeed;
            cellMatrix[3, 4] = GetCell(mapChannel, cellPosX + 1, cellPosZ + 2).CellSeed;
            cellMatrix[4, 0] = GetCell(mapChannel, cellPosX + 2, cellPosZ - 2).CellSeed;
            cellMatrix[4, 1] = GetCell(mapChannel, cellPosX + 2, cellPosZ - 1).CellSeed;
            cellMatrix[4, 2] = GetCell(mapChannel, cellPosX + 2, cellPosZ).CellSeed;
            cellMatrix[4, 3] = GetCell(mapChannel, cellPosX + 2, cellPosZ + 1).CellSeed;
            cellMatrix[4, 4] = GetCell(mapChannel, cellPosX + 2, cellPosZ + 2).CellSeed;

            return cellMatrix;
        }

        #region SendPackets
        internal void CellMoveObject(Creature creature, Movement movementData)
        {
            var mapChannel = creature?.RuntimeMapChannel;
            if (mapChannel == null)
                return;

            // calculate initial cell(x, z)
            var cellPosX = (uint)(creature.Position.X / CellSize + CellBias);
            var cellPosZ = (uint)(creature.Position.Z / CellSize + CellBias);

            // create matrix
            var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

            foreach (var client in GetClientsInCells(mapChannel, cellMatrix))
                client.MoveObject(creature.EntityId, movementData);
        }

        internal void CellCallMethod(DynamicObject obj, PythonPacket packet)
        {
            var mapChannel = obj?.RuntimeMapChannel;
            if (mapChannel == null)
                return;
            CellCallMethod(mapChannel, obj, packet);
        }

        internal void CellCallMethod(MapChannel mapChannel, DynamicObject obj, PythonPacket packet)
        {
            if (!TryGetCellCoordinates(obj.Position, out var cellPosX, out var cellPosZ))
                throw new InvalidDataException("Dynamic object position is outside the cell grid.");
            var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

            foreach (var client in GetClientsInCells(mapChannel, cellMatrix))
                client.CallMethod(obj.EntityId, packet);
        }

        internal void CellCallMethod(Creature creature, PythonPacket packet)
        {
            var mapChannel = creature?.RuntimeMapChannel;
            if (mapChannel == null)
                return;

            // calculate initial cell(x, z)
            var cellPosX = (uint)(creature.Position.X / CellSize + CellBias);
            var cellPosZ = (uint)(creature.Position.Z / CellSize + CellBias);

            // create matrix
            var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

            foreach (var client in GetClientsInCells(mapChannel, cellMatrix))
                client.CallMethod(creature.EntityId, packet);
        }

        internal void CellCallMethod(MapChannel mapChannel, Actor origin, PythonPacket packet)
        {
            foreach (var client in GetClientsInCells(mapChannel, origin.Cells))
                client.CallMethod(origin.EntityId, packet);
        }

        /// <summary>
        /// The cells of <paramref name="mapChannel"/> that a stored cell matrix names, skipping
        /// any this map does not have.
        ///
        /// A matrix belongs to the map it was built for, but an actor's outlives that: nothing
        /// clears it when they leave a map, and it is five by five zeroes until they first enter
        /// one. Indexing a map's cell table with one straight - which is what every broadcast
        /// over a stored matrix used to do - throws KeyNotFoundException whenever the two do not
        /// belong together. On the world loop that costs the rest of the tick, and for something
        /// re-run every tick it costs every tick after it as well.
        ///
        /// A cell the matrix names that this map has not got is simply nobody to send to, so it
        /// is skipped rather than being an error.
        /// </summary>
        internal static IEnumerable<MapCell> CellsIn(MapChannel mapChannel, uint[,] cellMatrix)
        {
            if (mapChannel == null || cellMatrix == null)
                yield break;

            foreach (var cellSeed in cellMatrix)
                if (mapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out var cell))
                    yield return cell;
        }
        #endregion
    }
}
