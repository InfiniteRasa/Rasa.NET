using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Navigation;
    using Structures;

    /// <summary>
    /// What the server holds: the memory in use, the navigation every map holds (its navmesh and
    /// cover, the largest part of a running server), and how many of everything there are, in
    /// total and map by map. Logged in full after "Server ready!". Counts are live: creatures and
    /// actors appear as maps come alive, so the startup report shows the spawn pools, not yet
    /// what they spawn.
    ///
    /// Navigation sizes are worked out from what the meshes hold (<see cref="MemoryFootprint"/>):
    /// a walk over every navmesh tile, poly and link, millions of them, so only at startup, when
    /// nobody is playing. The .memory command leaves navigation out (it doesn't change after
    /// startup) and costs no more than counting what is on each map.
    /// </summary>
    public static class MemoryReport
    {
        /// <summary>One map's line of the report. Navigation is empty unless it was asked for.</summary>
        internal sealed record MapLine(
            string Name,
            uint ContextId,
            MemoryFootprint.NavMeshFootprint NavMesh,
            long CoverBytes,
            int CoverTriangles,
            int Creatures,
            int OtherActors,
            int Players,
            int DynamicObjects,
            int SpawnPools,
            int Instances)
        {
            public long NavigationBytes => NavMesh.Bytes + CoverBytes;

            /// <summary>How busy the map is, for the .memory command's list.</summary>
            public int Busy => Creatures + OtherActors + Players;
        }

        /// <summary>The startup report: everything, navigation included.</summary>
        public static void Log()
        {
            foreach (var line in Lines(navigation: true))
                Logger.WriteLog(LogType.Initialize, line);
        }

        /// <summary>
        /// The totals, then every map: the largest navigation first with <paramref name="navigation"/>,
        /// otherwise the busiest first.
        /// </summary>
        internal static List<string> Lines(bool navigation)
        {
            var maps = Maps(navigation);
            var lines = Summary(maps, navigation);

            if (navigation)
            {
                lines.Add("Maps (largest navigation first): navigation (navmesh + cover), creatures, other actors, players, dynamic objects, spawn pools");
                lines.AddRange(maps
                    .OrderByDescending(map => map.NavigationBytes).ThenBy(map => map.Name, StringComparer.Ordinal)
                    .Select(map => "  " + Describe(map, navigation)));
            }
            else
            {
                lines.Add("Maps (busiest first): creatures, other actors, players, dynamic objects, spawn pools (navigation: see the startup report)");
                lines.AddRange(Busiest(maps, int.MaxValue).Select(map => "  " + Describe(map, navigation)));
            }

            return lines;
        }

        /// <summary>
        /// Short enough for the chat window: a few lines, no word wrap to read through.
        /// <paramref name="contextId"/> adds the map a player is on; <paramref name="top"/> the busiest maps.
        /// </summary>
        internal static List<string> Chat(uint contextId, int top)
        {
            var maps = Maps(navigation: false);
            var entities = EntityManager.Instance;
            var lines = new List<string>
            {
                $"Memory {MemoryFootprint.Format(Environment.WorkingSet)} | heap {MemoryFootprint.Format(GC.GetTotalMemory(false))}",
                $"{maps.Count} maps, {maps.Sum(map => map.Instances)} inst | {entities.Creatures.Count:N0} creatures | {entities.Players.Count} players",
            };

            var here = maps.FirstOrDefault(m => m.ContextId == contextId);
            lines.Add(here == null
                ? $"Here: map {contextId}, not loaded"
                : $"Here: {here.Name} | {here.Creatures} cr, {here.Players} pl, {here.DynamicObjects} obj, {here.Instances} inst");

            foreach (var map in Busiest(maps, top))
                lines.Add($"{map.Creatures} cr, {map.Players} pl  {map.Name}");

            return lines;
        }

        private static IEnumerable<MapLine> Busiest(List<MapLine> maps, int count) =>
            maps.OrderByDescending(map => map.Busy).ThenBy(map => map.Name, StringComparer.Ordinal).Take(count);

        private static string Describe(MapLine map, bool navigation) =>
            $"{map.Name} ({map.ContextId}): " +
            (navigation
                ? $"{MemoryFootprint.Format(map.NavigationBytes)} ({MemoryFootprint.Format(map.NavMesh.Bytes)} + {MemoryFootprint.Format(map.CoverBytes)}), "
                : "") +
            $"{map.Creatures:N0} creatures, {map.OtherActors:N0} actors, {map.Players:N0} players, {map.DynamicObjects:N0} objects, {map.SpawnPools:N0} pools" +
            (map.Instances > 0 ? $", {map.Instances:N0} instances" : "");

        /// <summary>Memory, navigation (when asked for) and object totals.</summary>
        private static List<string> Summary(List<MapLine> maps, bool navigation)
        {
            var entities = EntityManager.Instance;
            var lines = new List<string>();

            // Memory, from the runtime.
            var info = GC.GetGCMemoryInfo();
            var generationInfo = info.GenerationInfo;
            var generations = new long[generationInfo.Length];
            for (var i = 0; i < generationInfo.Length; i++)
                generations[i] = generationInfo[i].SizeAfterBytes;
            string Generation(int index) => index < generations.Length ? MemoryFootprint.Format(generations[index]) : "?";

            lines.Add("");
            lines.Add("Memory:");
            lines.Add($"  process {MemoryFootprint.Format(Environment.WorkingSet)}, managed heap {MemoryFootprint.Format(GC.GetTotalMemory(false))}, committed {MemoryFootprint.Format(info.TotalCommittedBytes)}");
            lines.Add($"  heap by generation (at the last collection): long-lived {Generation(2)}, large objects {Generation(3)}, gen0 {Generation(0)}, gen1 {Generation(1)}, pinned {Generation(4)}; fragmentation {MemoryFootprint.Format(info.FragmentedBytes)}");

            // Navigation, worked out from the meshes.
            if (navigation)
                AddNavigation(lines, maps);

            // Totals.
            lines.Add("Objects:");
            lines.Add($"  {entities.RegisteredEntities.Count:N0} entities: {entities.Actors.Count:N0} actors ({entities.Creatures.Count:N0} creatures, {entities.Players.Count:N0} players), {entities.DynamicObjects.Count:N0} dynamic objects, {entities.Items.Count:N0} items");
            lines.Add($"  {maps.Count:N0} maps, {maps.Sum(map => map.Instances):N0} instances: {maps.Sum(map => map.SpawnPools):N0} spawn pools, {maps.Sum(map => map.Players):N0} players in world");

            return lines;
        }

        private static void AddNavigation(List<string> lines, List<MapLine> maps)
        {
            var navMesh = maps.Aggregate(default(MemoryFootprint.NavMeshFootprint), (sum, map) => sum + map.NavMesh);
            var cover = maps.Sum(map => map.CoverBytes);

            lines.Add($"Navigation: {MemoryFootprint.Format(navMesh.Bytes + cover)} across {maps.Count(map => map.NavigationBytes > 0)} maps");
            lines.Add($"  navmesh {MemoryFootprint.Format(navMesh.Bytes)}: {navMesh.Tiles:N0} tiles, {navMesh.Polys:N0} polys, {navMesh.Links:N0} links, {navMesh.BvNodes:N0} BV nodes");
            lines.Add($"  cover   {MemoryFootprint.Format(cover)}: {maps.Sum(map => (long)map.CoverTriangles):N0} triangles");
        }

        /// <summary>Every loaded map, with what it holds; its navigation only with <paramref name="navigation"/>.</summary>
        internal static List<MapLine> Maps(bool navigation)
        {
            var entities = EntityManager.Instance;

            // Actors by the map they're on: creatures apart from the rest (NPCs, players' actors).
            var creatures = new Dictionary<uint, int>();
            var others = new Dictionary<uint, int>();

            foreach (var actor in entities.Actors.Values)
            {
                var counts = actor is Creature ? creatures : others;
                counts[actor.MapContextId] = counts.GetValueOrDefault(actor.MapContextId) + 1;
            }

            foreach (var creature in entities.Creatures.Values)
                if (!entities.Actors.ContainsKey(creature.EntityId))
                    creatures[creature.MapContextId] = creatures.GetValueOrDefault(creature.MapContextId) + 1;

            // Private, shared and squad copies of a map share its navigation, so they add their
            // players, objects and pools to its line, not navigation of their own.
            var instances = PrivateMapInstanceService.Instance.Snapshot()
                .Concat(PrivateMapInstanceService.Instance.SquadOf())
                .Distinct()
                .Where(map => map.MapInfo != null)
                .ToLookup(map => map.MapInfo.MapContextId);

            int Players(MapChannel map) => map.ClientList?.Count ?? 0;

            return MapChannelManager.Instance.MapChannelArray.Values
                .Select(map => (map, copies: instances[map.MapInfo?.MapContextId ?? 0].Where(copy => copy != map).ToList()))
                .Select(entry => new MapLine(
                    entry.map.MapInfo?.MapName ?? "?",
                    entry.map.MapInfo?.MapContextId ?? 0,
                    navigation ? MemoryFootprint.Of(entry.map.NavMesh?.NavMesh) : default,
                    navigation ? entry.map.Cover?.MemoryBytes ?? 0 : 0,
                    navigation ? entry.map.Cover?.TriangleCount ?? 0 : 0,
                    creatures.GetValueOrDefault(entry.map.MapInfo?.MapContextId ?? 0),
                    others.GetValueOrDefault(entry.map.MapInfo?.MapContextId ?? 0),
                    Players(entry.map) + entry.copies.Sum(Players),
                    entry.map.DynamicObjects.Count + entry.copies.Sum(copy => copy.DynamicObjects.Count),
                    entry.map.SpawnPools.Count + entry.copies.Sum(copy => copy.SpawnPools.Count),
                    entry.copies.Count))
                .ToList();
        }
    }
}
