using System;

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
        public static uint CellPosY { get; set; }
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
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            // get initial cell
            var x = (uint)(creature.Actor.Position.PosX / CellSize + CellBias);
            var y = (uint)(creature.Actor.Position.PosY / CellSize + CellBias);
            // calculate initial cell
            creature.Actor.CellLocation.CellPosX = x;
            creature.Actor.CellLocation.CellPosY = y;
            // get cell
            var mapCell = GetCell(mapChannel, x, y);
            if (mapCell != null)
            {
                // register object
                //hashTable_set(&mapCell->ht_creatureList, creature->actor.entityId, creature);
                mapCell.CreatureList.Add(creature);
                // notify all players of object
                if (mapCell.PlayerNotifyList.Count > 0)
                    CreatureManager.Instance.CellIntroduceCreatureToClients(mapChannel, creature, mapCell.PlayerNotifyList);
            }
        }

        // Player
        public void AddToWorld(MapChannelClient mapClient)
        {
            if (mapClient.Player == null)
                return;

            var mapChannel = mapClient.MapChannel;
            CellPosX = (uint) (mapClient.Player.Actor.Position.PosX/CellSize + CellBias);
            CellPosY = (uint) (mapClient.Player.Actor.Position.PosY/CellSize + CellBias);

            // calculate initial cell
            mapClient.Player.Actor.CellLocation.CellPosX = CellPosX;
            mapClient.Player.Actor.CellLocation.CellPosY = CellPosY;
            
            // get cell
            var mapCell = GetCell(mapChannel, CellPosX, CellPosY);
            if (mapCell != null)
            {
                // register player in cell
                mapCell.PlayerList.Add(mapClient);
                // register notifications in visible area
                for (var ix = CellPosX - CellViewRange; ix <= CellPosX + CellViewRange; ix++)
                {
                    for (var iy = CellPosY - CellViewRange; iy <= CellPosY + CellViewRange; iy++)
                    {
                        var nMapCell = GetCell(mapChannel, ix, iy);
                        nMapCell?.PlayerNotifyList.Add(mapClient);
                        // notify me about all objects that are visible to the cell
                        //if (nMapCell.ObjectList.Count > 0)
                        //{
                            // dynamicObject_cellIntroduceObjectsToClient(mapChannel, client, &nMapCell->ht_objectList[0], nMapCell->ht_objectList.size());
                        //}
                        // notify me about all creatures that are visible to the cell
                        //if (nMapCell.CreatureList.Count > 0)
                       // {
                            //creature_cellIntroduceCreaturesToClient(mapChannel, client, &nMapCell->ht_creatureList[0], nMapCell->ht_creatureList.size());
                       // }
                    }
                }
                if (mapCell.PlayerNotifyList.Count > 0)
                {
                    // notify all players of me, including me
                    PlayerManager.Instance.CellIntroduceClientToPlayers(mapClient, mapCell.PlayerNotifyList);
                    // notify me about all players that are visible here
                    PlayerManager.Instance.CellIntroducePlayersToClient(mapChannel, mapClient, mapCell.PlayerNotifyList);
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

        public void RemoveFromWorld(MapChannelClient mapClient)
        {
            var mapChannel = mapClient.MapChannel;
            var player = mapClient.Player;
            var actor = player.Actor;

            if (player == null)
                return;
            
            var oldX1 = actor.CellLocation.CellPosX - CellViewRange;
            var oldX2 = actor.CellLocation.CellPosX + CellViewRange;
            var oldY1 = actor.CellLocation.CellPosY - CellViewRange;
            var oldY2 = actor.CellLocation.CellPosY + CellViewRange;
            for (var ix = oldX1; ix <=oldX2; ix++)
            {
                for (var iy = oldY1; iy <= oldY2; iy++)
                {
                    var nMapCell = GetCell(mapChannel, ix, iy);
                    if (nMapCell != null)
                    {
                        // remove notify entry
                        for (var i = 0; i < nMapCell.PlayerNotifyList.Count; i++)
                        {
                            if (nMapCell.PlayerNotifyList[i] == mapClient)
                            {
                                nMapCell.PlayerNotifyList.RemoveAt(i);
                                break;
                            }
                        }
                        // remove player visibility client-side
                        if (nMapCell.PlayerNotifyList.Count > 0)
                        {
                            PlayerManager.Instance.CellDiscardClientToPlayers(mapClient, nMapCell.PlayerNotifyList);
                            PlayerManager.Instance.CellDiscardPlayersToClient(mapClient, nMapCell.PlayerNotifyList);
                        }
                    }
                }
            }
            var mapCell = GetCell(mapChannel, actor.CellLocation.CellPosX, actor.CellLocation.CellPosY);
            if (mapCell != null)
            {
                var count = mapCell.PlayerNotifyList.Count;
                for (var i = 0; i < count; i++)
                {
                    if (mapCell.PlayerNotifyList[i] == mapClient)
                    {
                        mapCell.PlayerNotifyList.RemoveAt(i);
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
            for (var i = 0; i < mapChannel.PlayerList.Count; i++)
            {
                var mapClient = mapChannel.PlayerList[i];
                if (mapClient.Disconected || mapClient.Player == null)
                    continue;

                var cellPosX = (uint)(mapClient.Player.Actor.Position.PosX / CellSize + CellBias);
                var cellPosY = (uint)(mapClient.Player.Actor.Position.PosY / CellSize + CellBias);
                if (mapClient.Player.Actor.CellLocation.CellPosX != cellPosX ||
                    mapClient.Player.Actor.CellLocation.CellPosY != cellPosY)
                {
                    var oldX1 = mapClient.Player.Actor.CellLocation.CellPosX - CellViewRange;
                    var oldX2 = mapClient.Player.Actor.CellLocation.CellPosX + CellViewRange;
                    var oldY1 = mapClient.Player.Actor.CellLocation.CellPosY - CellViewRange;
                    var oldY2 = mapClient.Player.Actor.CellLocation.CellPosY + CellViewRange;
                    // find players that leave visibility range
                    for (var ix = oldX1; ix <= oldX2; ix++)
                    {
                        for (var iy = oldY1; iy <= oldY2; iy++)
                        {
                            if ((ix >= (cellPosX - CellViewRange) && ix <= (cellPosX + CellViewRange)) && (iy >= (cellPosY - CellViewRange) && iy <= (cellPosY + CellViewRange)))
                                continue;

                            var nMapCell = GetCell(mapChannel, ix, iy);
                            if (nMapCell != null)
                            {
                                // remove notify entry
                                var count = nMapCell.PlayerNotifyList.Count;
                                for (var j = 0; j < count; j++)
                                {
                                    if (nMapCell.PlayerNotifyList[j] == mapClient)
                                    {
                                        nMapCell.PlayerNotifyList.RemoveAt(j);
                                        break;
                                    }
                                }
                                // remove player visibility client-side
                                if (nMapCell.PlayerNotifyList.Count > 0)
                                {
                                    PlayerManager.Instance.CellDiscardClientToPlayers(mapClient, nMapCell.PlayerNotifyList);
                                    PlayerManager.Instance.CellDiscardPlayersToClient(mapClient, nMapCell.PlayerNotifyList);
                                }
                                // remove object visibility
                               // if (nMapCell->ht_objectList.empty() == false)
                               //     dynamicObject_cellDiscardObjectsToClient(mapChannel, client, &nMapCell->ht_objectList[0], nMapCell->ht_objectList.size());
                                // remove creature visibility
                               // if (nMapCell->ht_creatureList.empty() == false)
                               //     creature_cellDiscardCreaturesToClient(mapChannel, client, &nMapCell->ht_creatureList[0], nMapCell->ht_creatureList.size());
                            }
                        }
                    }
                    // find players that enter visibility range
                    for (var ix = cellPosX - CellViewRange; ix <= CellPosX + CellViewRange; ix++)
                    {
                        for(var iy = cellPosY - CellViewRange; iy <= cellPosY + CellViewRange; iy++)
                        {
                            if ((ix >= oldX1 && ix <= oldX2) && (iy >= oldY1 && iy <= oldY2))
                                continue;
                            var nnMapCell = GetCell(mapChannel, ix, iy);
                            if (nnMapCell != null)
                            {
                                // add player visibility client-side
                                if (nnMapCell.PlayerNotifyList.Count > 0)
                                {
                                    // notify all players of me
                                    PlayerManager.Instance.CellIntroduceClientToPlayers(mapClient, nnMapCell.PlayerNotifyList);
                                    // notify me about all players that are visible here
                                    PlayerManager.Instance.CellIntroducePlayersToClient(mapChannel, mapClient, nnMapCell.PlayerNotifyList);
                                }
                                //if (nMapCell.ObjectList > 0)
                                //    dynamicObject_cellIntroduceObjectsToClient(mapChannel, client, &nMapCell->ht_objectList[0], nMapCell->ht_objectList.size());
                                // add creature visibility client-side
                                //if (nMapCell.CcreatureList > 0)
                                //    creature_cellIntroduceCreaturesToClient(mapChannel, client, &nMapCell->ht_creatureList[0], nMapCell->ht_creatureList.size());
                            }
                        }
                    }
                    // move the player entry
                    var mapCell = GetCell(mapChannel, mapClient.Player.Actor.CellLocation.CellPosX, mapClient.Player.Actor.CellLocation.CellPosX);
                    if (mapCell != null)
                    {
                        var count = mapCell.PlayerNotifyList.Count;
                        for (var k = 0; k < count; k++)
                        {
                            if (mapCell.PlayerNotifyList[k] == mapClient)
                            {
                                mapCell.PlayerNotifyList.RemoveAt(k);
                                break;
                            }
                        }
                    }

                    // update location
                    mapClient.Player.Actor.CellLocation.CellPosX = cellPosX;
                    mapClient.Player.Actor.CellLocation.CellPosY = cellPosY;
                }
            }
        }
    }
}
