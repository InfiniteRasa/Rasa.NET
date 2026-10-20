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
    /// total and map by map. Logged after "Server ready!", and on demand with the .memory
    /// command (all of it, or the map you're on). Counts are live: creatures and actors appear as
    /// maps come alive, so a report just after startup shows the spawn pools, not yet what they spawn.
    ///
    /// Navigation sizes are worked out from what the meshes hold (<see cref="MemoryFootprint"/>),
    /// the rest of memory from the runtime, so this costs no more than walking the maps once.
    /// </summary>
    public static class MemoryReport
    {
        /// <summary>One map's line of the report.</summary>
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
        }

        public static void Log()
        {
            foreach (var line in Lines())
                Logger.WriteLog(LogType.Initialize, line);
        }

        /// <summary>Everything: the totals, then every map, the largest navigation first.</summary>
        internal static List<string> Lines()
        {
            var maps = Maps();
            var lines = Summary(maps);

            lines.Add("Maps (largest navigation first): navigation (navmesh + cover), creatures, other actors, players, dynamic objects, spawn pools");
            lines.AddRange(Largest(maps, int.MaxValue).Select(map => "  " + Describe(map)));
            return lines;
        }

        /// <summary>The totals, and the <paramref name="count"/> maps with the most navigation.</summary>
        internal static List<string> Top(int count)
        {
            var maps = Maps();
            var lines = Summary(maps);

            lines.Add($"Largest {Math.Min(count, maps.Count)} of {maps.Count} maps:");
            lines.AddRange(Largest(maps, count).Select(map => "  " + Describe(map)));
            return lines;
        }

        /// <summary>The totals, and one map in full: the one a player is on.</summary>
        internal static List<string> ForMap(uint contextId)
        {
            var maps = Maps();
            var lines = Summary(maps);
            var map = maps.FirstOrDefault(m => m.ContextId == contextId);

            if (map == null)
            {
                lines.Add($"Map {contextId}: not loaded");
                return lines;
            }

            lines.Add($"This map: {map.Name} ({map.ContextId})");
            lines.Add($"  navigation {MemoryFootprint.Format(map.NavigationBytes)}: navmesh {MemoryFootprint.Format(map.NavMesh.Bytes)} ({map.NavMesh.Tiles:N0} tiles, {map.NavMesh.Polys:N0} polys), cover {MemoryFootprint.Format(map.CoverBytes)} ({map.CoverTriangles:N0} triangles)");
            lines.Add($"  {map.Creatures:N0} creatures, {map.OtherActors:N0} other actors, {map.Players:N0} players, {map.DynamicObjects:N0} dynamic objects, {map.SpawnPools:N0} spawn pools");
            lines.Add($"  {map.Instances:N0} instances (private, shared or squad copies; they share its navigation)");
            lines.Add($"  rank {maps.Count(m => m.NavigationBytes > map.NavigationBytes) + 1} of {maps.Count} by navigation");
            return lines;
        }

        /// <summary>
        /// The same, short enough for the chat window: a few lines, no word wrap to read through.
        /// <paramref name="contextId"/> adds the map a player is on; <paramref name="top"/> the largest maps.
        /// </summary>
        internal static List<string> Chat(uint? contextId, int top)
        {
            var maps = Maps();
            var entities = EntityManager.Instance;
            var navigation = maps.Sum(map => map.NavigationBytes);
            var lines = new List<string>
            {
                $"Memory {MemoryFootprint.Format(Environment.WorkingSet)} | heap {MemoryFootprint.Format(GC.GetTotalMemory(false))} | nav {MemoryFootprint.Format(navigation)}",
                $"{maps.Count} maps, {maps.Sum(map => map.Instances)} inst | {entities.Creatures.Count:N0} creatures | {entities.Players.Count} players",
            };

            if (contextId is uint id)
            {
                var map = maps.FirstOrDefault(m => m.ContextId == id);
                lines.Add(map == null
                    ? $"Here: map {id}, not loaded"
                    : $"Here: {map.Name} | nav {MemoryFootprint.Format(map.NavigationBytes)} (#{maps.Count(m => m.NavigationBytes > map.NavigationBytes) + 1}) | {map.Creatures} cr, {map.Players} pl, {map.Instances} inst");
            }

            foreach (var map in Largest(maps, top))
                lines.Add($"{MemoryFootprint.Format(map.NavigationBytes)}  {map.Name}");

            return lines;
        }

        private static IEnumerable<MapLine> Largest(List<MapLine> maps, int count) =>
            maps.OrderByDescending(map => map.NavigationBytes).ThenBy(map => map.Name, StringComparer.Ordinal).Take(count);

        private static string Describe(MapLine map) =>
            $"{map.Name} ({map.ContextId}): {MemoryFootprint.Format(map.NavigationBytes)} " +
            $"({MemoryFootprint.Format(map.NavMesh.Bytes)} + {MemoryFootprint.Format(map.CoverBytes)}), " +
            $"{map.Creatures:N0} creatures, {map.OtherActors:N0} actors, {map.Players:N0} players, {map.DynamicObjects:N0} objects, {map.SpawnPools:N0} pools" +
            (map.Instances > 0 ? $", {map.Instances:N0} instances" : "");

        /// <summary>Memory, navigation and object totals.</summary>
        private static List<string> Summary(List<MapLine> maps)
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
            var navMesh = maps.Aggregate(default(MemoryFootprint.NavMeshFootprint), (sum, map) => sum + map.NavMesh);
            var cover = maps.Sum(map => map.CoverBytes);

            lines.Add($"Navigation: {MemoryFootprint.Format(navMesh.Bytes + cover)} across {maps.Count(map => map.NavigationBytes > 0)} maps");
            lines.Add($"  navmesh {MemoryFootprint.Format(navMesh.Bytes)}: {navMesh.Tiles:N0} tiles, {navMesh.Polys:N0} polys, {navMesh.Links:N0} links, {navMesh.BvNodes:N0} BV nodes");
            lines.Add($"  cover   {MemoryFootprint.Format(cover)}: {maps.Sum(map => (long)map.CoverTriangles):N0} triangles");

            // Totals.
            lines.Add("Objects:");
            lines.Add($"  {entities.RegisteredEntities.Count:N0} entities: {entities.Actors.Count:N0} actors ({entities.Creatures.Count:N0} creatures, {entities.Players.Count:N0} players), {entities.DynamicObjects.Count:N0} dynamic objects, {entities.Items.Count:N0} items");
            lines.Add($"  {maps.Count:N0} maps, {maps.Sum(map => map.Instances):N0} instances: {maps.Sum(map => map.SpawnPools):N0} spawn pools, {maps.Sum(map => map.Players):N0} players in world");

            return lines;
        }

        /// <summary>Every loaded map, with what it holds.</summary>
        internal static List<MapLine> Maps()
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
                    MemoryFootprint.Of(entry.map.NavMesh?.NavMesh),
                    entry.map.Cover?.MemoryBytes ?? 0,
                    entry.map.Cover?.TriangleCount ?? 0,
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
