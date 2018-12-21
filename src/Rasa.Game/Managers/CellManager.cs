namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    public class CellManager
    {
        private static CellManager _instance;
        private static readonly object InstanceLock = new object();
        public const double CellSize = 25.0D;
        public const double CellBias = 32768.0D;
        public static uint CellPosX { get; set; }
        public static uint CellPosZ { get; set; }
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

        public void AddToWorld(MapChannel mapChannel, Creature creature)
        {
            if (creature == null)
                return;
            // register creature entity
            EntityManager.Instance.RegisterEntity(creature.Actor.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            // get initial cell
            var x = (uint)(creature.Actor.Position.X / CellSize + CellBias);
            var z = (uint)(creature.Actor.Position.Z / CellSize + CellBias);
            // calculate initial cell
            creature.Actor.CellLocation.CellPosX = x;
            creature.Actor.CellLocation.CellPosY = z;
            // get cell
            var mapCell = GetCell(mapChannel, x, z);
            if (mapCell != null)
            {
                // register object
                //hashTable_set(&mapCell->ht_creatureList, creature->actor.entityId, creature);
                mapCell.CreatureList.Add(creature);
                // notify all players of object
                if (mapCell.ClientNotifyList.Count > 0)
                    CreatureManager.Instance.CellIntroduceCreatureToClients(mapChannel, creature, mapCell.ClientNotifyList);
            }
        }

        // Player
        public void AddToWorld(Client client)
        {
            if (client.MapClient.Player == null)
                return;

            var mapChannel = client.MapClient.MapChannel;
            CellPosX = (uint)(client.MapClient.Player.Actor.Position.X / CellSize + CellBias);
            CellPosZ = (uint)(client.MapClient.Player.Actor.Position.Z / CellSize + CellBias);

            // calculate initial cell
            client.MapClient.Player.Actor.CellLocation.CellPosX = CellPosX;
            client.MapClient.Player.Actor.CellLocation.CellPosY = CellPosZ;

            // get cell
            var mapCell = GetCell(mapChannel, CellPosX, CellPosZ);
            if (mapCell != null)
            {
                // register player in cell
                mapCell.ClientList.Add(client);
                // register notifications in visible area
                for (var ix = CellPosX - CellViewRange; ix <= CellPosX + CellViewRange; ix++)
                {
                    for (var iz = CellPosZ - CellViewRange; iz <= CellPosZ + CellViewRange; iz++)
                    {
                        var nMapCell = GetCell(mapChannel, ix, iz);
                        nMapCell?.ClientNotifyList.Add(client);
                        // notify me about all objects that are visible to the cell
                        //if (nMapCell.ObjectList.Count > 0)
                        //{
                        // dynamicObject_cellIntroduceObjectsToClient(mapChannel, client, &nMapCell->ht_objectList[0], nMapCell->ht_objectList.size());
                        //}

                        // notify me about all creatures that are visible to the cell
                        if (nMapCell.CreatureList.Count > 0)
                            CreatureManager.Instance.CellIntroduceCreaturesToClient(mapChannel, client, mapCell.CreatureList);
                    }
                }
                if (mapCell.ClientNotifyList.Count > 0)
                {
                    // notify all players of me, including me
                    ManifestationManager.Instance.CellIntroduceClientToPlayers(client, mapCell.ClientNotifyList);
                    // notify me about all players that are visible here
                    ManifestationManager.Instance.CellIntroducePlayersToClient(client, mapCell.ClientNotifyList);
                }
            }
        }

        public void DoWork(MapChannel mapChannel)
        {
            // 2 times per sec, do we need check more often?
            UpdateVisibility(mapChannel);
            // mob work

            // events etc...
        }

        public MapCell GetCell(MapChannel mapChannel, uint cellPosX, uint cellPosY)
        {
            var cellSeed = (cellPosX & 0xFFFF) | (cellPosY << 16);
            MapCell cell;
            if (mapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out cell))
            {
                cell.CellPosX = cellPosX;
                cell.CellPosY = cellPosY;
                return cell;
            }
            else
            {
                cell = new MapCell
                {
                    CellPosX = cellPosX,
                    CellPosY = cellPosY
                };
                // save cell
                mapChannel.MapCellInfo.LoadedCellList.Add(cell);
                mapChannel.MapCellInfo.LoadedCellCount++;
                // register cell
                mapChannel.MapCellInfo.Cells.Add(cellSeed, cell);   // ToDo check why it crash here sometimes
                return cell;
            }

        }

        public void RemoveFromWorld(Client client)
        {
            var mapChannel = client.MapClient.MapChannel;
            var player = client.MapClient.Player;
            var actor = player.Actor;

            if (player == null)
                return;

            var oldX1 = actor.CellLocation.CellPosX - CellViewRange;
            var oldX2 = actor.CellLocation.CellPosX + CellViewRange;
            var oldY1 = actor.CellLocation.CellPosY - CellViewRange;
            var oldY2 = actor.CellLocation.CellPosY + CellViewRange;

            for (var ix = oldX1; ix <= oldX2; ix++)
            {
                for (var iy = oldY1; iy <= oldY2; iy++)
                {
                    var nMapCell = GetCell(mapChannel, ix, iy);

                    if (nMapCell != null)
                    {
                        // remove notify entry
                        for (int i = nMapCell.ClientNotifyList.Count - 1; i >= 0; i--)
                            if (nMapCell.ClientNotifyList[i] == client)
                            {
                                nMapCell.ClientNotifyList.RemoveAt(i);
                                break;
                            }

                        // remove player visibility client-side
                        if (nMapCell.ClientNotifyList.Count > 0)
                        {
                            ManifestationManager.Instance.CellDiscardClientToPlayers(client, nMapCell.ClientNotifyList);
                            ManifestationManager.Instance.CellDiscardPlayersToClient(client, nMapCell.ClientNotifyList);
                        }
                    }
                }
            }

            var mapCell = GetCell(mapChannel, actor.CellLocation.CellPosX, actor.CellLocation.CellPosY);

            if (mapCell != null)
            {
                var count = mapCell.ClientNotifyList.Count;
                for (var i = 0; i < count; i++)
                {
                    if (mapCell.ClientNotifyList[i] == client)
                    {
                        mapCell.ClientNotifyList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public MapCell TryGetCell(MapChannel mapChannel, uint cellPosX, uint cellPosY)
        {
            var cellSeed = (cellPosX & 0xFFFF) | (cellPosY << 16);
            MapCell cell;
            if (mapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out cell))
            {
                cell.CellPosX = cellPosX;
                cell.CellPosY = cellPosY;
                return cell;
            }
            return null;
        }

        public void UpdateVisibility(MapChannel mapChannel)
        {
            for (var i = 0; i < mapChannel.ClientList.Count; i++)
            {
                var client = mapChannel.ClientList[i];
                if (client.MapClient.Disconected || client.MapClient.Player == null)
                    continue;

                var cellPosX = (uint)(client.MapClient.Player.Actor.Position.X / CellSize + CellBias);
                var cellPosZ = (uint)(client.MapClient.Player.Actor.Position.Z / CellSize + CellBias);
                if (client.MapClient.Player.Actor.CellLocation.CellPosX != cellPosX ||
                    client.MapClient.Player.Actor.CellLocation.CellPosY != cellPosZ)
                {
                    var oldX1 = client.MapClient.Player.Actor.CellLocation.CellPosX - CellViewRange;
                    var oldX2 = client.MapClient.Player.Actor.CellLocation.CellPosX + CellViewRange;
                    var oldY1 = client.MapClient.Player.Actor.CellLocation.CellPosY - CellViewRange;
                    var oldY2 = client.MapClient.Player.Actor.CellLocation.CellPosY + CellViewRange;

                    // find players that leave visibility range
                    for (var ix = oldX1; ix <= oldX2; ix++)
                        for (var iz = oldY1; iz <= oldY2; iz++)
                        {
                            if ((ix >= (cellPosX - CellViewRange) && ix <= (cellPosX + CellViewRange)) && (iz >= (cellPosZ - CellViewRange) && iz <= (cellPosZ + CellViewRange)))
                                continue;

                            var nMapCell = GetCell(mapChannel, ix, iz);

                            if (nMapCell != null)
                            {
                                if (nMapCell.ClientNotifyList.Count > 0)
                                {
                                    // remove notify entry
                                    for (int j = nMapCell.ClientNotifyList.Count - 1; i >= 0; i--)
                                        if (nMapCell.ClientNotifyList[j] == client)
                                        {
                                            nMapCell.ClientNotifyList.RemoveAt(j);
                                            break;
                                        }

                                    // remove player visibility client-side
                                    ManifestationManager.Instance.CellDiscardClientToPlayers(client, nMapCell.ClientNotifyList);
                                    ManifestationManager.Instance.CellDiscardPlayersToClient(client, nMapCell.ClientNotifyList);
                                }

                                // remove object visibility
                                // if (nMapCell->ht_objectList.empty() == false)
                                //     dynamicObject_cellDiscardObjectsToClient(mapChannel, client, &nMapCell->ht_objectList[0], nMapCell->ht_objectList.size());

                                // remove creature visibility
                                //if (nMapCell.CreatureList.Count > 0)
                                //   CreatureManager.Instance.CellDiscardCreaturesToClient(mapChannel, client, nMapCell.CreatureList);
                            }
                        }


                    // find players that enter visibility range
                    for (var ix = cellPosX - CellViewRange; ix <= CellPosX + CellViewRange; ix++)
                        for (var iz = cellPosZ - CellViewRange; iz <= cellPosZ + CellViewRange; iz++)
                        {
                            if ((ix >= oldX1 && ix <= oldX2) && (iz >= oldY1 && iz <= oldY2))
                                continue;

                            var nnMapCell = GetCell(mapChannel, ix, iz);

                            if (nnMapCell != null)
                            {
                                // add player visibility client-side
                                if (nnMapCell.ClientNotifyList.Count > 0)
                                {
                                    // notify all players of me
                                    ManifestationManager.Instance.CellIntroduceClientToPlayers(client, nnMapCell.ClientNotifyList);
                                    // notify me about all players that are visible here
                                    ManifestationManager.Instance.CellIntroducePlayersToClient(client, nnMapCell.ClientNotifyList);
                                }
                                //if (nMapCell.ObjectList > 0)
                                //    dynamicObject_cellIntroduceObjectsToClient(mapChannel, client, &nMapCell->ht_objectList[0], nMapCell->ht_objectList.size());
                                // add creature visibility client-side
                                if (nnMapCell.CreatureList.Count > 0)
                                    CreatureManager.Instance.CellIntroduceCreaturesToClient(mapChannel, client, nnMapCell.CreatureList);
                            }
                        }

                    // move the player entry
                    var mapCell = GetCell(mapChannel, client.MapClient.Player.Actor.CellLocation.CellPosX, client.MapClient.Player.Actor.CellLocation.CellPosX);

                    if (mapCell != null)
                        if (mapCell.ClientNotifyList.Count > 0)
                            for (int k = mapCell.ClientNotifyList.Count - 1; k >= 0; k--)
                                if (mapCell.ClientNotifyList[k] == client)
                                {
                                    mapCell.ClientNotifyList.RemoveAt(k);
                                    break;
                                }
                }

                // update location
                client.MapClient.Player.Actor.CellLocation.CellPosX = cellPosX;
                client.MapClient.Player.Actor.CellLocation.CellPosY = cellPosZ;
            }
        }
    }
}
