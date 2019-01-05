using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    public class CellManager
    {
        private static CellManager _instance;
        private static readonly object InstanceLock = new object();
        public const float CellSize = 25.0f;
        public const float CellBias = 32768.0f;
        public const uint CellViewRange = 2;   // view 2 cell's in every direction

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
                            _instance = new CellManager();
                    }
                }

                return _instance;
            }
        }

        private CellManager()
        {
        }

        // creature
        public void AddToWorld(MapChannel mapChannel, Creature creature)
        {
            if (creature == null)
                return;
            // register creature entity
            EntityManager.Instance.RegisterEntity(creature.Actor.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);

            // calculate initial cell(x, z)
            var cellPosX = (uint)(creature.Actor.Position.X / CellSize + CellBias);
            var cellPosZ = (uint)(creature.Actor.Position.Z / CellSize + CellBias);

            // create matrix
            var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

            // add creature to the cell
            mapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].CreatureList.Add(creature);

            // notify client's about new creatures
            var ListOfClients = new List<Client>();

            foreach (var cellSeed in cellMatrix)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    ListOfClients.Add(client);

            CreatureManager.Instance.CellIntroduceCreatureToClients(mapChannel, creature, ListOfClients);
        }

        // Object
        public void AddToWorld(MapChannel mapChannel, DynamicObject dynamicObject)
        {
            if (dynamicObject == null)
                return;

            // register creature entity
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
            if (client.MapClient.Player == null)
                return;

            var mapChannel = client.MapClient.MapChannel;

            // calculate initial cell
            var CellPosX = (uint)(client.MapClient.Player.Actor.Position.X / CellSize + CellBias);
            var CellPosZ = (uint)(client.MapClient.Player.Actor.Position.Z / CellSize + CellBias);

            // create matrix
            var cellMatrix = CreateCellMatrix(mapChannel, CellPosX, CellPosZ);

            // add client to the cell
            client.MapClient.MapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].ClientList.Add(client);
            // add cellMatrix to client
            client.MapClient.Player.Actor.Cells = cellMatrix;

            // notify client about players, creatures, objects
            var ListOfClients = new List<Client>();
            var ListOfCreatures = new List<Creature>();
            var ListOfObjects = new List<DynamicObject>();

            foreach (var cellSeed in cellMatrix)
            {
                foreach (var player in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    ListOfClients.Add(player);

                foreach (var creature in mapChannel.MapCellInfo.Cells[cellSeed].CreatureList)
                    ListOfCreatures.Add(creature);

                foreach (var dinamicObject in mapChannel.MapCellInfo.Cells[cellSeed].DynamicObjectList)
                    ListOfObjects.Add(dinamicObject);

            }

            ManifestationManager.Instance.CellIntroduceClientToSefl(client);
            ManifestationManager.Instance.CellIntroduceClientToPlayers(client, ListOfClients);
            ManifestationManager.Instance.CellIntroducePlayersToClient(client, ListOfClients);

            CreatureManager.Instance.CellIntroduceCreaturesToClient(client, ListOfCreatures);
            DynamicObjectManager.Instance.CellIntroduceDynamicObjectsToClient(client, ListOfObjects);
        }

        public void DoWork(MapChannel mapChannel)
        {
            // 2 times per sec, do we need check more often?
            UpdateVisibility(mapChannel);
            // mob work

            // events etc...
        }

        public MapCell GetCell(MapChannel mapChannel, uint cellPosX, uint cellPosZ)
        {
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

        public void RemoveFromWorld(Client client)
        {
            var mapChannel = client.MapClient.MapChannel;

            if (client.MapClient.Player == null)
                return;

            //notify players
            var ListOfClients = new List<Client>();

            foreach (var cellSeed in client.MapClient.Player.Actor.Cells)
                foreach (var player in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    ListOfClients.Add(player);

            ManifestationManager.Instance.CellDiscardClientToPlayers(client, ListOfClients);
            ManifestationManager.Instance.CellDiscardPlayersToClient(client, ListOfClients);

            // remove player from cell
            mapChannel.MapCellInfo.Cells[client.MapClient.Player.Actor.Cells[2, 2]].ClientList.Remove(client);
        }

        public void UpdateVisibility(MapChannel mapChannel)
        {
            foreach (var client in mapChannel.ClientList)
            {
                if (client.MapClient.Disconected || client.MapClient.Player == null)
                    continue;

                var cellPosX = (uint)(client.MapClient.Player.Actor.Position.X / CellSize + CellBias);
                var cellPosZ = (uint)(client.MapClient.Player.Actor.Position.Z / CellSize + CellBias);

                // create matrix
                var cellMatrix = CreateCellMatrix(mapChannel, cellPosX, cellPosZ);

                // get info about cell we need to update
                var needUpdate = new List<uint>();
                var needDelete = new List<uint>();

                // find all cells that need to be removed
                foreach (var oldCell in client.MapClient.Player.Actor.Cells)
                {
                    var found = false;

                    foreach (var newCell in cellMatrix)
                        if (newCell == oldCell)
                        {
                            found = true;
                            break;
                        }

                    if (!found)
                        needDelete.Add(oldCell);
                }

                // find all cells that need to be added
                foreach (var newCell in cellMatrix)
                {
                    var found = false;

                    foreach (var oldCell in client.MapClient.Player.Actor.Cells)
                        if (oldCell == newCell)
                        {
                            found = true;
                            break;
                        }

                    if (!found)
                        needUpdate.Add(newCell);
                }

                // remove Player from old cell
                mapChannel.MapCellInfo.Cells[client.MapClient.Player.Actor.Cells[2, 2]].ClientList.Remove(client);
                
                // remove players, creatures, object that left visibility range
                var DiscardClients = new List<Client>();
                var DiscardCreatures = new List<Creature>();
                var DiscardObjects = new List<DynamicObject>();

                foreach (var cellSeed in needDelete)
                {
                    foreach (var player in client.MapClient.MapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                        DiscardClients.Add(player);

                    foreach (var creature in client.MapClient.MapChannel.MapCellInfo.Cells[cellSeed].CreatureList)
                        DiscardCreatures.Add(creature);

                    foreach (var dinamicObject in client.MapClient.MapChannel.MapCellInfo.Cells[cellSeed].DynamicObjectList)
                        DiscardObjects.Add(dinamicObject);

                }

                ManifestationManager.Instance.CellDiscardPlayersToClient(client, DiscardClients);
                CreatureManager.Instance.CellDiscardCreaturesToClient(client, DiscardCreatures);
                DynamicObjectManager.Instance.CellDiscardDynamicObjectsToClient(client, DiscardObjects);

                // add player to new cell
                mapChannel.MapCellInfo.Cells[cellMatrix[2, 2]].ClientList.Add(client);
                // set new player visibility
                client.MapClient.Player.Actor.Cells = cellMatrix;
                
                // notify client about players, creatures, objects
                var AddClients = new List<Client>();
                var AddCreatures = new List<Creature>();
                var AddObjects = new List<DynamicObject>();

                foreach (var cellSeed in needUpdate)
                {
                    foreach (var player in client.MapClient.MapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                        AddClients.Add(player);

                    foreach (var creature in client.MapClient.MapChannel.MapCellInfo.Cells[cellSeed].CreatureList)
                        AddCreatures.Add(creature);

                    foreach (var dinamicObject in client.MapClient.MapChannel.MapCellInfo.Cells[cellSeed].DynamicObjectList)
                        AddObjects.Add(dinamicObject);
                }

                ManifestationManager.Instance.CellIntroduceClientToPlayers(client, AddClients);
                ManifestationManager.Instance.CellIntroducePlayersToClient(client, AddClients);
                CreatureManager.Instance.CellIntroduceCreaturesToClient(client, AddCreatures);
                DynamicObjectManager.Instance.CellIntroduceDynamicObjectsToClient(client, AddObjects);
            }
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
    }
}
