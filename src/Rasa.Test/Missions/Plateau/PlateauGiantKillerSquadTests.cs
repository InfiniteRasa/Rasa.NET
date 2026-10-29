using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using G = Rasa.Services.Preloader.Missions.Plateau.PlateauGiantKillerSquadV1;
    using S = Rasa.Managers.NpcSquads;

    /// <summary>
    /// Goliath's pack (PlateauGiantKillerSquadV1; NpcSquads), as AliaDasAfsSquadTests tests Alia
    /// Das's squad: its rows and Down, and the squad on the Plateau - Goliath leading two to four
    /// Kael from the west end, the Kael on a 30 m tether, the beat walked to the ruins and back at
    /// 5 m/s with 10 s at each end, and sent again 50 s after it is wiped out. Goliath's pool no
    /// longer spawns him. That a squad Goliath counts for Giant Killer (939) is played through in
    /// PlateauCampResistanceTests, whose kills take him from this squad.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauGiantKillerSquadTests
    {
        // The squad is in Giant Killer's own migration, after its mission (PlateauGiantKillerV1) and Goliath's scale.
        private const string Migration = "20270922000000_Plateau_giant_killer";
        private const string Previous = "20270921000100_Take_down_field_action";

        private static readonly string[] Tables =
        {
            "npc_squad_type", "npc_squad_member", "npc_squad_path", "npc_squad_path_point", "npc_squad_spawn"
        };

        private static readonly Vector3 WestEnd = new((float)G.SpawnX, (float)G.SpawnY, (float)G.SpawnZ);
        private static readonly Vector3 EastEnd = new((float)G.Points[^1].X, (float)G.Points[^1].Y, (float)G.Points[^1].Z);

        /// <summary>Colonel Bosley's spot at Camp Resistance, 190 m west of the beat: the player stands there.</summary>
        private static readonly Vector3 Camp = new(-632.3f, 447.95f, -232.5f);

        private static long _now = 1_000_000;

        [TestInitialize]
        public void Clock() => UseClock();

        [TestCleanup]
        public void Restore() => RestoreClock();

        internal static void UseClock()
        {
            S.Load(null, null, null, null, null);
            S.Now = () => _now;
            KaelRushingBlow.Now = () => _now;
        }

        internal static void RestoreClock()
        {
            S.Load(null, null, null, null, null);
            S.Now = () => Environment.TickCount64;
            KaelRushingBlow.Now = () => Environment.TickCount64;
        }

        #region The rows

        [TestMethod]
        public void TheSquadGoesInAndDownTakesItOutExactly()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            const string pool = "SELECT id, mode, creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id FROM spawnpool WHERE id = 520051";
            const string evidence = "SELECT evidence_id, source_kind, source_uri, confidence FROM mission_evidence WHERE mission_id = 939 ORDER BY evidence_id";

            migrator.Migrate(Previous);
            var before = Tables.Append("spawnpool").ToDictionary(table => table, table => Dump(database, table));
            var evidenceBefore = Rows(database, evidence);
            CollectionAssert.AreEqual(new[] { "520051|0|520051|1|1|0" }, Rows(database, pool), "upstream's boss pool: automatic, Goliath alone");

            migrator.Migrate(Migration);

            CollectionAssert.AreEqual(new[] { "750111|Giant Killer Goliath's pack|0|5|0|30" },
                Rows(database, "SELECT id, name, formation, spacing, engage_range, tether FROM npc_squad_type WHERE id = 750111"));
            CollectionAssert.AreEqual(new[] { "0|520051|1|1", "1|531088|2|4" },
                Rows(database, "SELECT slot, creature_id, min_count, max_count FROM npc_squad_member WHERE type_id = 750111 ORDER BY slot"),
                "Goliath leads; two to four Kael");
            CollectionAssert.AreEqual(new[] { "750111|1497|1" }, Rows(database, "SELECT id, map_context_id, mode FROM npc_squad_path WHERE id = 750111"),
                "the Plateau, there and back");

            var points = Rows(database, "SELECT step, pos_x, pos_y, pos_z, pause_ms, run, engage_range FROM npc_squad_path_point WHERE path_id = 750111 ORDER BY step");
            Assert.HasCount(31, points, "the way out only: Back walks it back");
            Assert.AreEqual("0|-440|442.37|-230|10000|0|0", points[0], "west end, 10 s");
            Assert.AreEqual("30|-254|442.87|-346|10000|0|0", points[30], "east end, near the ruins, 10 s");
            Assert.IsTrue(points.Skip(1).Take(29).All(point => point.EndsWith("|0|0|0")), "walked, no stand between the ends");

            CollectionAssert.AreEqual(new[] { "750111|1497|750111|750111|-440|442.37|-230|50|1|0" },
                Rows(database, "SELECT id, map_context_id, type_id, path_id, pos_x, pos_y, pos_z, respawn_seconds, enabled, arrival FROM npc_squad_spawn WHERE id = 750111"));
            CollectionAssert.AreEqual(new[] { "520051|2|520051|1|1|0" }, Rows(database, pool), "the pool scripted: the squad brings Goliath");
            CollectionAssert.Contains(Rows(database, evidence), $"4|{(byte)Rasa.Structures.World.MissionEvidenceSourceKind.Reconstruction}|{G.EvidenceUri}|0.3");

            foreach (var sql in G.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql), sql);

            migrator.Migrate(Previous);

            foreach (var table in Tables.Append("spawnpool"))
                CollectionAssert.AreEqual(before[table], Dump(database, table), $"Down puts {table} back as it was");
            CollectionAssert.AreEqual(evidenceBefore, Rows(database, evidence));
        }

        [TestMethod]
        public void APoolNoLongerUpstreamsIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET mode = 1 WHERE id = 520051");
            migrator.Migrate(Migration);
            Assert.AreEqual("1", Rows(database, "SELECT mode FROM spawnpool WHERE id = 520051").Single(), "Up: not upstream's mode, not touched");

            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET mode = 2 WHERE id = 520051");
            database.Context.Database.ExecuteSqlRaw("DELETE FROM npc_squad_spawn WHERE id = 750111");
            migrator.Migrate(Previous);
            Assert.AreEqual("2", Rows(database, "SELECT mode FROM spawnpool WHERE id = 520051").Single(), "Down: the squad not ours, the pool not touched");
        }

        [TestMethod]
        public void OnMySqlTheSquadGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in G.Up())
                StringAssert.Contains(up, sql);
            foreach (var sql in G.Down())
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void TheBeatGoesOverThePlateauNavmesh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = PlateauCampResistanceTests.EnterPlateau(harness).NavMesh;
            var from = nav.Nearest(WestEnd) ?? throw new AssertFailedException("the west end is off the navmesh");

            Assert.IsTrue(Vector3.Distance(from, WestEnd) < 0.5f);

            foreach (var point in G.Points)
            {
                var to = nav.Nearest(new Vector3((float)point.X, (float)point.Y, (float)point.Z)) ?? throw new AssertFailedException($"{point} is off the navmesh");
                nav.FindPath(from, to, out var complete);
                Assert.IsTrue(complete, $"a way to {point}");
                from = to;
            }
        }

        #endregion

        #region On the Plateau

        [TestMethod]
        public void GoliathComesAtTheWestEndWithTwoToFourKaelOnATether()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = Enter(harness);

            PlateauCampResistanceTests.Spawn(plateau, G.GoliathPool);
            Assert.AreEqual(SpawnPoolManager.ModeScripted, plateau.SpawnPools.Single(pool => pool.DbId == G.GoliathPool).Mode);
            Assert.IsFalse(PlateauCampResistanceTests.Living(plateau).Any(creature => creature.SpawnPool?.DbId == G.GoliathPool),
                "his pool spawns nothing of its own");

            var squad = Send(harness, plateau);
            Assert.AreEqual(G.Goliath, squad.Leader.Creature.DbId, "Goliath leads");
            Assert.AreEqual(1, squad.Members.Count(member => member.Creature.DbId == G.Goliath));
            var kael = squad.Members.Where(member => member.Creature.DbId == G.Kael).Select(member => member.Creature).ToArray();
            Assert.IsTrue(kael.Length is >= 2 and <= 4, $"{kael.Length} Kael with Goliath");
            Assert.AreEqual(kael.Length + 1, squad.Members.Count);
            Assert.IsTrue(Vector2.Distance(Flat(squad.Leader.Creature.Position), Flat(WestEnd)) < 1f, $"at the west end: {squad.Leader.Creature.Position}");
            Assert.AreEqual(5f, S.Types[G.Squad].Spacing);

            foreach (var member in squad.Members.Where(member => member.Creature.DbId == G.Kael))
            {
                var guard = member.Creature;
                Assert.AreSame(squad.Leader.Creature, S.TetherOf(guard, out var tether), "a Kael is tethered to Goliath");
                Assert.AreEqual(30f, tether);
                var place = S.PlaceOf(plateau, S.Types[G.Squad], member.Place, squad.Leader.Creature.Position, squad.Heading);
                Assert.IsLessThan(1f, Vector2.Distance(Flat(guard.Position), Flat(place)), "in his place in the wedge, 5 m apart");
            }

            Assert.IsNull(S.TetherOf(squad.Leader.Creature, out _), "Goliath himself is not");

            // Every number of Kael the roll can give.
            foreach (var count in new[] { 2, 4 })
            {
                S.TakeAway(plateau, squad);
                S.Roll = (min, max) => min == 1 ? 1 : count;
                try
                {
                    S.Send(plateau, squad, _now);
                }
                finally
                {
                    S.Roll = (min, max) => Random.Shared.Next(min, max + 1);
                }

                Assert.AreEqual(count, squad.Members.Count(member => member.Creature.DbId == G.Kael));
            }
        }

        [TestMethod]
        public void TheyWalkTheBeatToTheRuinsAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = Enter(harness);
            var squad = Send(harness, plateau);

            // 10 s at the west end first.
            Step(harness, plateau, 250);
            Assert.AreEqual(S.Phase.Pausing, squad.Phase, "standing at the west end");
            Step(harness, plateau, 9_000);
            Assert.AreEqual(S.Phase.Pausing, squad.Phase, "still");
            Assert.IsTrue(Vector2.Distance(Flat(squad.Leader.Creature.Position), Flat(WestEnd)) < 1.5f);

            var reached = new List<int>();
            var speeds = new List<float>();
            var pausedAtEast = 0L;
            var count = squad.Members.Count;

            for (var i = 0; i < 4 * 400 && reached.Count < 61; i++)
            {
                var before = squad.Leader.Creature.Position;
                Step(harness, plateau, 250);
                // One tick can carry a step and a half (a waypoint turned, the navmesh snapping), so the pace is the median
                // of the walking ticks, not the fastest of them.
                var step = Vector2.Distance(Flat(before), Flat(squad.Leader.Creature.Position)) * 4;
                if (squad.Phase == S.Phase.Moving && step > 0.1f)
                    speeds.Add(step);

                if (squad.Phase == S.Phase.Pausing && squad.PathIndex == 29)
                    pausedAtEast += 250;

                var at = squad.Phase == S.Phase.Pausing ? -1 : squad.PathIndex;
                if (at >= 0 && (reached.Count == 0 || reached[^1] != at))
                    reached.Add(at);

                foreach (var member in squad.Living.Skip(1))
                    Assert.IsLessThan(20f, Vector2.Distance(Flat(member.Creature.Position), Flat(squad.Leader.Creature.Position)),
                        $"step {i}: a Kael keeps up with Goliath");
            }

            CollectionAssert.AreEqual(Enumerable.Range(1, 30).Concat(Enumerable.Range(0, 30).Reverse()).Append(1).ToArray(), reached.ToArray(),
                "every point out, the east end, every point back, the west end, and out again");
            Assert.AreEqual(10_000, pausedAtEast, 500, "10 s at the east end");
            // walk_speed 5 is the seed's placeholder: the Kael classes walk 2.4 m/s (EntityMovementRates).
            Assert.IsTrue(squad.Living.All(member => member.Creature.WalkSpeed == 2.4f), "every one of them walks at 2.4 m/s");
            Assert.AreEqual(2.4f, squad.Leader.Speed, 0.01f, "the pace of the pack");
            speeds.Sort();
            Assert.AreEqual(2.4f, speeds[speeds.Count / 2], 0.1f, "the pace of the pack while it walks, m/s (median tick)");
            Assert.IsFalse(squad.Living.Any(member => member.Creature.IsRunning), "they walk");
            Assert.AreEqual(count, squad.Living.Count());
        }

        [TestMethod]
        public void WipedOutTheyComeBackAtTheWestEndFiftySecondsLater()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = Enter(harness);
            var squad = Send(harness, plateau);

            // 10 s at the west end, then 30 s out at 2.4 m/s.
            for (var i = 0; i < 4 * 40; i++)
                Step(harness, plateau, 250);

            var first = squad.Members.Select(m => m.Creature).ToList();
            Assert.IsTrue(Vector2.Distance(Flat(squad.Leader.Creature.Position), Flat(WestEnd)) > 50f, "out along the beat");

            foreach (var creature in first)
            {
                creature.Attributes[Attributes.Health].Current = 0;
                creature.State = CharacterState.Dead;
            }

            Step(harness, plateau, 250);
            Assert.AreEqual(S.Phase.Gone, squad.Phase);

            Step(harness, plateau, 49_000);
            Assert.AreEqual(S.Phase.Gone, squad.Phase, "not yet");

            Step(harness, plateau, 1_000);
            Assert.AreNotEqual(S.Phase.Gone, squad.Phase, "back");
            Assert.AreEqual(G.Goliath, squad.Leader.Creature.DbId);
            Assert.IsTrue(Vector2.Distance(Flat(squad.Leader.Creature.Position), Flat(WestEnd)) < 1f, "at the west end");
            Assert.IsTrue(squad.Members.All(m => !first.Contains(m.Creature)));
        }

        #endregion

        #region Helpers

        /// <summary>The Plateau with the squads of the world database; the player at Camp Resistance, out of the pack's way.</summary>
        internal static MapChannel Enter(WildernessRuntimeTestHarness harness)
        {
            var plateau = PlateauCampResistanceTests.EnterPlateau(harness);
            S.Init(harness);
            Assert.IsTrue(S.Spawns.ContainsKey(G.Squad), "the squad is in the world database");
            harness.MoveTo(Camp);
            harness.Drain();
            return plateau;
        }

        /// <summary>Goliath's pack sent at the west end (the squads' worker on the Plateau).</summary>
        internal static S.LiveSquad Send(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            S.Worker(plateau);
            var squad = S.On(plateau)[G.Squad];
            Assert.AreNotEqual(S.Phase.Gone, squad.Phase, "sent");
            harness.Drain();
            return squad;
        }

        internal static void Step(WildernessRuntimeTestHarness harness, MapChannel plateau, long milliseconds)
        {
            for (var left = milliseconds; left > 0; left -= 250)
            {
                var step = Math.Min(250, left);
                _now += step;
                KaelRushingBlow.Worker(plateau);
                S.Worker(plateau);
                harness.UtcNow = harness.UtcNow.AddMilliseconds(step);
                BehaviorManager.Instance.MapChannelThink(plateau, step);
                harness.Manager.Scenes.Tick(plateau);
            }

            harness.Drain();
        }

        private static Vector2 Flat(Vector3 position) => new(position.X, position.Z);

        private static List<string> Rows(OrdinaryLootTestDatabase database, string sql)
        {
            using var command = database.Context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            database.Context.Database.OpenConnection();
            using var reader = command.ExecuteReader();
            var rows = new List<string>();

            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));

            return rows;
        }

        private static List<string> Dump(OrdinaryLootTestDatabase database, string table) => Rows(database, $"SELECT * FROM {table} ORDER BY 1, 2");

        #endregion
    }
}
