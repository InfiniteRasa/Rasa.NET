using System;
using System.Collections;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Structures;

    public static class CellManager
    {
        public static double CellSize { get; set; }
        public static double CellBias { get; set; }
        public static uint CellPosX { get; set; }
        public static uint CellPosY { get; set; }
        public static uint CellViewRange { get; set; }
        //public static MapChannelClient[] Itr { get; set; }

        public static void AddToWorld(MapChannelClient client)
        {
            if (client.Player == null)
                return;
            CellSize = 25.0f;
            CellBias = 32768.0f;
            CellViewRange = 2;

            var mapChannel = client.MapChannel;
            CellPosX = (uint) (client.Player.Actor.PosX/CellSize + CellBias);
            CellPosY = (uint) (client.Player.Actor.PosZ/CellSize + CellBias);

            // calculate initial cell
            client.Player.Actor.CellLocation = new MapCellLocation
            {
                CellPosX = CellPosX,
                CellPosY = CellPosY
            };

            // get cell
            var mapCell = GetCell(mapChannel, CellPosX, CellPosY);
            if (mapCell != null)
            {
                // register player in cell
                mapCell.PlayerList.Add(client);
                // register notifications in visible area
                for (var ix = CellPosX - CellViewRange; ix <= CellPosX + CellViewRange; ix++)
                {
                    for (var iy = CellPosY - CellViewRange; iy <= CellPosY + CellViewRange; iy++)
                    {
                        var nMapCell = GetCell(mapChannel, ix, iy);
                        nMapCell?.PlayerNotifyList.Add(client);
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
                // notify all players of me
                if (mapCell.PlayerNotifyList.Count > 0)
                    PlayerManager.CellIntroduceClientToPlayers(mapChannel, client, mapCell.PlayerNotifyList.Count);
                // notify me about all players that are visible here
                 if (mapCell.PlayerNotifyList.Count > 0)
                    PlayerManager.CellIntroducePlayersToClient(mapChannel, client, mapCell.PlayerNotifyList.Count);

            }
        }

        public static MapCell GetCell(MapChannel mapChannel, uint cellPosX, uint cellPosY)
        {
            var cellSeed = (cellPosX & 0xFFFF) | (cellPosY << 16);
            try
            {
                var mapCell = mapChannel.MapCellInfo.Cells[cellSeed];
                mapCell.CellPosX = cellPosX;
                mapCell.CellPosY = cellPosY;
                return mapCell;
            }
            catch (Exception)
            {
                // create cell
                var mapCell = new MapCell
                {
                    CellPosX = cellPosX,
                    CellPosY = cellPosY
                };
                // save cell
                mapChannel.MapCellInfo.LoadedCellList.Add(mapCell);
                mapChannel.MapCellInfo.LoadedCellCount++;
                // register cell
                mapChannel.MapCellInfo.Cells.Add(cellSeed, mapCell);
                return mapCell;
            }
        }

        public static bool InitForMapChannel(MapChannel mapChannel)
        {
            mapChannel.MapCellInfo.Cells = new Dictionary<uint, MapCell>();
            mapChannel.MapCellInfo.LoadedCellCount = 0;
            mapChannel.MapCellInfo.LoadedCellLimit = 2048;
            mapChannel.MapCellInfo.LoadedCellList = new List<MapCell>();
            mapChannel.MapCellInfo.TimeUpdateVisibility = Environment.TickCount + 1000;
            return true;
        }

        public static void DoWork(MapChannel mapChannel)
        {
            var currentTime = Environment.TickCount;
            if (mapChannel.MapCellInfo.TimeUpdateVisibility < currentTime)
            {
                UpdateVisibility(mapChannel);
                // update three times a second
                mapChannel.MapCellInfo.TimeUpdateVisibility = currentTime + 300;
            }
            // mob work

            // events etc...
        }

        public static void UpdateVisibility(MapChannel mapChannel)
        {
            CellViewRange = 2;  // view 2 cell's in every direction

            for (var i = 0; i < mapChannel.PlayerCount; i++)
            {
                var client = mapChannel.PlayerList[i];
                if (client.Disconected || client.Player == null)
                    continue;

                var cellPosX = (uint)(client.Player.Actor.PosX / CellSize + CellBias);
                var cellPosY = (uint)(client.Player.Actor.PosZ / CellSize + CellBias);
                if (client.Player.Actor.CellLocation.CellPosX != cellPosX ||
                    client.Player.Actor.CellLocation.CellPosY != cellPosY)
                {
                    // find players that leave visibility range
                    var oldX1 = client.Player.Actor.CellLocation.CellPosX - CellViewRange;
                    var oldX2 = client.Player.Actor.CellLocation.CellPosX + CellViewRange;
                    var oldY1 = client.Player.Actor.CellLocation.CellPosY - CellViewRange;
                    var oldY2 = client.Player.Actor.CellLocation.CellPosY + CellViewRange;

                    for (var ix = oldX1; ix <= oldX2; ix++)
                    {
                        for (var iy = oldY1; iy <= oldY2; iy++)
                        {
                            if ((ix >= (cellPosX - CellViewRange) && ix <= (cellPosX + CellViewRange)) &&
                                (iy >= (cellPosY - CellViewRange) && iy <= (cellPosY + CellViewRange)))
                                continue;
                            var mapCell = GetCell(mapChannel, ix, iy);
                            if (mapCell != null)
                            {
                                // to be continued
                            }
                        }
                    }
                }
            }
        }
    }
}
