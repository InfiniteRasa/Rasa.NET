extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    /// <summary>
    /// The Plateau's instance maps in the tests: a map loaded as the server loads it, entered through its door as
    /// a player walks in (a squad's copy), and left back to the Plateau.
    /// </summary>
    internal static class PlateauInstanceMaps
    {
        /// <summary>
        /// A map of the World as the server loads it, beside the harness's: its map_info row, its navmesh,
        /// its spawn pools and its Logos shrines (LogosManager.LogosInit), placed on it. The map's own
        /// channel, which a squad's copy is made from.
        /// </summary>
        internal static MapChannel LoadMap(WildernessRuntimeTestHarness harness, uint mapContextId)
        {
            var info = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == mapContextId);
            var map = new MapChannel
            {
                MapInfo = new MapInfo(info.Id, info.MapName, info.MapVersion, 0),
                ClientList = new List<Client>(),
                PlayerLimit = 128,
                NavMesh = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), info.MapName))
            };
            harness.Maps.MapChannelArray.Add(mapContextId, map);
            SpawnPoolManager.Instance.InitializeMapChannel(map);
            foreach (var entry in harness.World.LogosEntries.AsNoTracking().Where(row => row.MapContextId == mapContextId).ToArray())
                map.DynamicObjects.Add(new Logos(entry));
            harness.Objects.DynamicObjectWorker(map, 0);
            return map;
        }

        /// <summary>
        /// Through the map's door from the public map, as MapLinkManager sends a player who walks into it
        /// (MapChannelManager.EnterMap), and arrived (MapLoaded): the channel the player is on then. A map with
        /// more than one door (Ustor Yard) is entered by its main one, the first link into it.
        /// </summary>
        internal static MapChannel ThroughTheDoor(WildernessRuntimeTestHarness harness, uint mapContextId)
        {
            var door = harness.World.MapLinkEntries.AsNoTracking().Where(row => row.DestMapContextId == mapContextId)
                .OrderBy(row => row.Id).First();
            var at = new Vector3((float)door.DestPosX, (float)door.DestPosY, (float)door.DestPosZ);
            Assert.IsTrue(harness.Maps.EnterMap(harness.Client, mapContextId, at, (float)door.DestRotation), "through the door");
            harness.Maps.MapLoaded(harness.Client);
            harness.Drain();
            var map = harness.Client.Player.MapChannel;
            Assert.AreEqual(mapContextId, map.MapInfo.MapContextId);
            harness.Objects.DynamicObjectWorker(map, 0);
            harness.Drain();
            return map;
        }

        /// <summary>The player back on the Plateau, as EnterPlateau puts them there.</summary>
        internal static void Return(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = plateau;
            client.Player.RuntimeMapChannel = plateau;
            client.Player.MapContextId = PlateauMissionDataV1.MapContextId;
            plateau.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;
            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
