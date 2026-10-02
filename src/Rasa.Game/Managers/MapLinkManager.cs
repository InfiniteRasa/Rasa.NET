using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures;

    /// <summary>
    /// The seams between maps. The client draws every zone border and instance door on its map
    /// screen but has no idea what happens when a player walks into one; the original server
    /// watched those volumes and Wonkavated the player into the neighbouring map. Without that
    /// the terrain simply ends and the player falls off the world. This does the watching.
    ///
    /// A link fires on <em>entering</em> its radius, not on being inside it: a player who
    /// arrives on a map inside the reciprocal gate (which is where every arrival lands) has that
    /// gate seeded into <see cref="Manifestation.InsideMapLinks"/> and has to step out and back
    /// in before it takes them anywhere. The same set doubles as the "one transfer per tick" guard.
    /// </summary>
    public class MapLinkManager
    {
        private static MapLinkManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>
        /// How far above or below the trigger position a player still counts as in it. The
        /// marker sits on the pass floor; a bridge or cliff over the pass should not fire it.
        /// </summary>
        public const float VerticalTolerance = 12.0f;

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly Dictionary<uint, MapLink> _links = new Dictionary<uint, MapLink>();

        public static MapLinkManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MapLinkManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private MapLinkManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public IEnumerable<MapLink> Links => _links.Values;

        public bool TryGet(uint id, out MapLink link)
        {
            return _links.TryGetValue(id, out link);
        }

        /// <summary>Loads map_link into the maps' cells. Runs after MapChannelInit, which it needs.</summary>
        public void MapLinkInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entries = unitOfWork.MapLinks.GetMapLinks();
            var registered = 0;

            foreach (var entry in entries)
            {
                var link = MapLink.FromEntry(entry);

                if (Register(link))
                    registered++;
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {registered} map links ({entries.Count} rows)");
        }

        /// <summary>
        /// Puts a link into its map's cell and the index. A link whose source map is not loaded
        /// is dropped with a world error; one whose destination is not loaded is kept, so a GM
        /// can see and fix it, but is reported on its own map and will not fire.
        /// </summary>
        private bool Register(MapLink link)
        {
            if (!MapChannelManager.Instance.MapChannelArray.TryGetValue(link.MapContextId, out var mapChannel))
            {
                MapErrorManager.Instance.Record($"map_link {link.Id} ({link.Comment}) is on map {link.MapContextId}, which is not loaded");
                Logger.WriteLog(LogType.Error, $"map_link {link.Id} ({link.Comment}) is on map {link.MapContextId}, which is not loaded; skipped");
                return false;
            }

            if (!MapChannelManager.Instance.MapChannelArray.ContainsKey(link.DestMapContextId))
                MapErrorManager.Instance.Record(link.MapContextId, $"map_link {link.Id} ({link.Comment}) leads to map {link.DestMapContextId}, which is not loaded");

            _links[link.Id] = link;
            CellManager.Instance.AddToWorld(mapChannel, link);

            return true;
        }

        private void Unregister(MapLink link)
        {
            if (MapChannelManager.Instance.MapChannelArray.TryGetValue(link.MapContextId, out var mapChannel))
                CellManager.Instance.RemoveFromWorld(mapChannel, link);

            _links.Remove(link.Id);
        }

        /// <summary>Once a second per map, from MapChannelWorker.</summary>
        public void Worker(MapChannel mapChannel)
        {
            if (mapChannel.ClientList.Count == 0)
                return;

            // A transfer removes the player from this list mid-walk; iterate a copy.
            foreach (var client in mapChannel.ClientList.ToArray())
            {
                var player = client.Player;

                if (player == null || player.Disconected || client.State != ClientState.Ingame)
                    continue;

                if (player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0)
                    continue;

                var inside = LinksAround(mapChannel, player.Cells, player.Position, true);
                MapLink fire = null;

                foreach (var link in inside)
                    if (!player.InsideMapLinks.Contains(link.Id))
                    {
                        fire = link;
                        break;
                    }

                // Whatever they are no longer standing in is forgotten, so a gate they were
                // carried out of (summon, .teleport) is live again the next time they walk in.
                player.InsideMapLinks.Clear();

                foreach (var link in inside)
                    player.InsideMapLinks.Add(link.Id);

                if (fire != null)
                    Fire(client, fire);
            }
        }

        /// <summary>
        /// Called when a player has been placed in a map's cells (MapLoaded), before the worker
        /// sees them: the gate they arrived through must not fire until they leave it.
        /// </summary>
        public void PlayerEnteredMap(Client client)
        {
            var player = client.Player;

            if (player?.MapChannel == null)
                return;

            player.InsideMapLinks.Clear();

            foreach (var link in LinksAround(player.MapChannel, player.Cells, player.Position, false))
                player.InsideMapLinks.Add(link.Id);
        }

        public void RemovePlayer(Client client)
        {
            client.Player?.InsideMapLinks.Clear();
        }

        /// <summary>The links in the 5x5 cell matrix whose radius contains the position.</summary>
        private static List<MapLink> LinksAround(MapChannel mapChannel, uint[,] cells, Vector3 position, bool enabledOnly)
        {
            var result = new List<MapLink>();

            foreach (var cellSeed in cells)
            {
                if (!mapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out var cell))
                    continue;

                foreach (var link in cell.MapLinks)
                {
                    if (enabledOnly && !link.Enabled)
                        continue;

                    if (Contains(link, position))
                        result.Add(link);
                }
            }

            return result;
        }

        public static bool Contains(MapLink link, Vector3 position)
        {
            var dx = position.X - link.Position.X;
            var dz = position.Z - link.Position.Z;

            if (dx * dx + dz * dz > link.Radius * link.Radius)
                return false;

            return Math.Abs(position.Y - link.Position.Y) <= VerticalTolerance;
        }

        private void Fire(Client client, MapLink link)
        {
            if (!MapChannelManager.Instance.MapChannelArray.ContainsKey(link.DestMapContextId))
            {
                MapErrorManager.Instance.Record(link.MapContextId, $"map_link {link.Id} ({link.Comment}) leads to map {link.DestMapContextId}, which is not loaded");
                return;
            }

            Logger.WriteLog(LogType.Debug, $"{client.Player.FamilyName} took map link {link.Id} ({link.Comment}): {link.MapContextId} -> {link.DestMapContextId}");

            // A map that runs in several copies is entered by its copy with room, or by the
            // instance picker once there is more than one (MapChannelManager.EnterMap).
            if (!MapChannelManager.Instance.EnterMap(client, link.DestMapContextId, link.DestPosition, link.DestRotation))
                Logger.WriteLog(LogType.Error, $"map_link {link.Id} ({link.Comment}) could not move {client.Player.FamilyName} to map {link.DestMapContextId}");
        }

        #region GM editing

        /// <summary>Creates the row and puts the link live. Returns null when the insert failed.</summary>
        public MapLink Add(MapLink link)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entry = link.ToEntry();
            entry.Id = 0;

            var id = unitOfWork.MapLinks.AddMapLink(entry);

            if (id == 0)
                return null;

            link.Id = id;

            return Register(link) ? link : null;
        }

        /// <summary>
        /// Writes the link's current fields to its row. When the trigger has moved, the cell it
        /// sits in is updated as well: pass the position it was registered under.
        /// </summary>
        public bool Update(MapLink link, Vector3 previousPosition, uint previousMapContextId)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            if (!unitOfWork.MapLinks.UpdateMapLink(link.ToEntry()))
                return false;

            if (previousPosition != link.Position || previousMapContextId != link.MapContextId)
            {
                if (MapChannelManager.Instance.MapChannelArray.TryGetValue(previousMapContextId, out var oldMap))
                {
                    var current = link.Position;
                    link.Position = previousPosition;
                    CellManager.Instance.RemoveFromWorld(oldMap, link);
                    link.Position = current;
                }

                if (MapChannelManager.Instance.MapChannelArray.TryGetValue(link.MapContextId, out var newMap))
                    CellManager.Instance.AddToWorld(newMap, link);
            }

            return true;
        }

        public bool Delete(MapLink link)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            if (!unitOfWork.MapLinks.DeleteMapLink(link.Id))
                return false;

            Unregister(link);

            return true;
        }

        /// <summary>The links on a map, nearest to the position first.</summary>
        public List<MapLink> OnMap(uint mapContextId, Vector3 position)
        {
            return _links.Values.Where(l => l.MapContextId == mapContextId)
                                .OrderBy(l => Vector3.Distance(l.Position, position))
                                .ToList();
        }

        #endregion
    }
}
