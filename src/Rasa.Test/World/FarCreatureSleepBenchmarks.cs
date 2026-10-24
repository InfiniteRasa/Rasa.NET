using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// Numbers for a question, not a change: how much of a busy map's creature think is spent on
    /// creatures far from every player, which a City of Heroes-style sleep would skip. The pools
    /// come from a seeded world database (RASA_WORLD_DB, a copy of rasaworld.db); the think cost
    /// from idle creatures thinking on a test map. RASA_BENCH=1 only.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FarCreatureSleepBenchmarks
    {
        [TestMethod]
        public void ShareOfCreaturesFarFromEveryPlayer()
        {
            AiCostBenchmarks.RequireBench();

            var path = Environment.GetEnvironmentVariable("RASA_WORLD_DB") ?? @"C:\tmp\trgen\troon\ai-run\rasaworld.db";

            if (!File.Exists(path))
                Assert.Inconclusive($"no world database at {path}");

            var pools = new List<(uint Map, Vector3 At, int Creatures)>();

            using (var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly"))
            {
                connection.Open();

                var counts = new List<string>();

                using (var columns = connection.CreateCommand())
                {
                    columns.CommandText = "PRAGMA table_info(spawnpool)";

                    using var reader = columns.ExecuteReader();

                    while (reader.Read())
                        if (reader.GetString(1).EndsWith("_max_count", StringComparison.Ordinal))
                            counts.Add(reader.GetString(1));
                }

                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT map_context_id, pos_x, pos_y, pos_z, {string.Join(" + ", counts.Select(c => $"IFNULL({c}, 0)"))} FROM spawnpool";

                using var rows = command.ExecuteReader();

                while (rows.Read())
                    pools.Add(((uint)rows.GetInt64(0), new Vector3((float)rows.GetDouble(1), (float)rows.GetDouble(2), (float)rows.GetDouble(3)), rows.GetInt32(4)));
            }

            var random = new Random(5);

            foreach (var map in pools.GroupBy(p => p.Map).OrderByDescending(g => g.Sum(p => p.Creatures)).Take(5))
            {
                var mapPools = map.Where(p => p.Creatures > 0).ToList();
                var total = mapPools.Sum(p => p.Creatures);

                // The densest 100 m disc: where a crowd would stand.
                var densest = mapPools.OrderByDescending(p => mapPools.Where(q => Flat(p.At, q.At) <= 100).Sum(q => q.Creatures)).First().At;

                Console.WriteLine($"map {map.Key}: {mapPools.Count} pools, up to {total} creatures");

                foreach (var players in new[] { 10, 30, 100 })
                {
                    // Spread: each player at a random pool. Crowded: all within 100 m of the densest spot.
                    var spread = Enumerable.Range(0, players).Select(_ => mapPools[random.Next(mapPools.Count)].At).ToList();
                    var crowded = Enumerable.Range(0, players).Select(_ =>
                    {
                        var angle = random.NextDouble() * 2 * Math.PI;
                        var r = (float)(random.NextDouble() * 100);
                        return densest + new Vector3((float)Math.Cos(angle) * r, 0, (float)Math.Sin(angle) * r);
                    }).ToList();

                    foreach (var radius in new[] { 100f, 150f, 250f })
                    {
                        Console.WriteLine($"  {players,3} players, sleep beyond {radius,3} m: " +
                                          $"spread {Far(mapPools, spread, radius) * 100.0 / total:0}% asleep, " +
                                          $"crowded {Far(mapPools, crowded, radius) * 100.0 / total:0}% asleep");
                    }
                }
            }
        }

        /// <summary>What one idle creature's think costs on the loop, staggered and not.</summary>
        [TestMethod]
        public void CostOfAnIdleCreaturesThink()
        {
            AiCostBenchmarks.RequireBench();

            foreach (var staggered in new[] { false, true })
            {
                var previous = BehaviorManager.Config;
                BehaviorManager.Config = new CreatureAiConfig { StaggeredThink = staggered };

                try
                {
                    using var world = new WorldTestContext();
                    var creatures = Enumerable.Range(0, 3000).Select(i => Spawn(world, new Vector3(i % 60 * 9f, 0, i / 60 * 9f))).ToList();

                    for (var ms = 0; ms < 3000; ms += 100)
                        BehaviorManager.Instance.MapChannelThink(world.Map, 100);

                    var passes = new List<double>();
                    var thought = creatures.Sum(c => c.LastAgression);

                    for (var ms = 0; ms < 30000; ms += 100)
                    {
                        var from = Stopwatch.GetTimestamp();
                        BehaviorManager.Instance.MapChannelThink(world.Map, 100);
                        passes.Add(Stopwatch.GetElapsedTime(from).TotalMilliseconds);
                    }

                    var thinks = (creatures.Sum(c => c.LastAgression) - thought) / (staggered ? 250.0 : 300.0);
                    passes.Sort();
                    Console.WriteLine($"{(staggered ? "staggered" : "whole map")}: 3000 idle creatures, 300 passes: total {passes.Sum():0} ms, " +
                                      $"slowest pass {passes[^1]:0.0} ms, p95 {passes[(int)(passes.Count * 0.95)]:0.0} ms; " +
                                      $"about {passes.Sum() * 1000 / thinks:0.0} us a think");
                }
                finally
                {
                    BehaviorManager.Config = previous;
                }
            }
        }

        private static int Far(List<(uint Map, Vector3 At, int Creatures)> pools, List<Vector3> players, float radius) =>
            pools.Where(p => players.All(player => Flat(p.At, player) > radius)).Sum(p => p.Creatures);

        private static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));

        private static Creature Spawn(WorldTestContext world, Vector3 at)
        {
            var creature = new Creature
            {
                Name = "Idler",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = at,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 20,
                WalkSpeed = 0,
                RunSpeed = 0,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(1000);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            creature.LastAgression = 0;
            return creature;
        }
    }
}
