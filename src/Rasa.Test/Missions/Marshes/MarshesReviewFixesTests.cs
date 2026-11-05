using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Marshes
{
    using Rasa.Missions.Content;
    using Rasa.Missions.Content.Marshes;
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader.Missions.Marshes;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The fixes of the Marshes review list (real-client playtest, 2026-11-04), each a migration after
    /// Marshes_on_the_navmesh: each test checks the fix on the migrated World DB, rolls its migration back to the one
    /// before it (IMigrationsAssembly) and finds the old data, then migrates up again.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesReviewFixesTests
    {
        private const string BraziersOnDryFloor = "20271104003100_Marshes_braziers_on_dry_floor";
        private const string KiriiInSight = "20271104003200_Marshes_kirii_in_sight";
        private const string HaxHolds = "20271104003300_Marshes_hax_holds";
        private const string GuardStationHolds = "20271104003400_Marshes_guard_station_holds";
        private const string TrappedSmallerWave = "20271104003500_Marshes_trapped_smaller_wave";
        private const string KiriiInOpen = "20271104003600_Marshes_kirii_in_open";
        private const string HackWavesOnPlayer = "20271104003700_Marshes_hack_waves_on_player";
        private const string TrappedLighterWaves = "20271104003800_Marshes_trapped_lighter_waves";
        private const string TrappedClearOfDepot = "20271104003900_Marshes_trapped_clear_of_depot";
        private const string TrappedRetreadsHold = "20271104004000_Marshes_trapped_retreads_hold";
        private const string GuardStationWavesOut = "20271104004100_Marshes_guard_station_waves_out";

        private static string Previous(DbContext world, string migration)
        {
            var ids = world.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var index = ids.IndexOf(migration);
            Assert.IsGreaterThan(0, index, $"{migration} has a migration before it");
            return ids[index - 1];
        }

        /// <summary>The mission's stored bindings JSON.</summary>
        private static string Binding(DbContext world, uint mission) => world.Database.SqlQueryRaw<string>(
            $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {mission} AND content_revision = '{MarshesMissionDataV1.Revision}'").AsEnumerable().Single();

        private static string Json(SceneActorDefinition actor) => System.Text.Json.JsonSerializer.Serialize(actor, MissionContentCodec.Options);

        private static int Count(DbContext world, string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();

        /// <summary>How many of the mission's bindings hold <paramref name="text"/> (braces doubled for SqlQueryRaw's format).</summary>
        private static int Bound(DbContext world, uint mission, string text) => Count(world,
            $"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id = {mission} AND content_revision = '{MarshesMissionDataV1.Revision}' " +
            $"AND bindings LIKE '%{text.Replace("{", "{{").Replace("}", "}}")}%'");

        /// <summary>
        /// 864's braziers stood in the temple fountains' water (review list, 2026-11-04). Both stand on the temple floor now;
        /// rolled back, both are in the basins again.
        /// </summary>
        [TestMethod]
        public void SpiritHomesBraziersStandOutOfTheFountains()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            int Braziers(bool now) => MarshesBraziersOnDryFloorV1.Moves.Count(move => Bound(world, MarshesBringingTheSpiritHomeV1.MissionId,
                MarshesBindings.Actor(move.Role, SceneActorKind.Object, MarshesBringingTheSpiritHomeV1.BrazierClass, now ? move.Now : move.Before)) == 1);

            Assert.AreEqual((2, 0), (Braziers(true), Braziers(false)), "both braziers on the temple floor");
            Assert.IsTrue(MarshesBraziersOnDryFloorV1.Moves.All(move => move.Now.Y > 231.14f), "over the fountains' water (231.14)");
            migrator.Migrate(Previous(world, BraziersOnDryFloor));
            Assert.AreEqual((0, 2), (Braziers(true), Braziers(false)), "both in the basins as MarshesBringingTheSpiritHomeV1 put them");
            migrator.Migrate(BraziersOnDryFloor);
            Assert.AreEqual((2, 0), (Braziers(true), Braziers(false)), "back on the temple floor");
        }

        /// <summary>
        /// Kirii (891, 894) talked only from his east side (review list, 2026-11-04). His pool and his 891 marker stand at the
        /// new place; rolled back, both at MarshesQuestNpcsHoldV1's. Marshes_kirii_in_open, after it, rolls back first.
        /// </summary>
        [TestMethod]
        public void KiriiAndHisMarkerStandInSight()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            int At((double X, double Y, double Z) at) => KiriiAt(world, at);

            migrator.Migrate(KiriiInSight);
            Assert.AreEqual((2, 0), (At(MarshesKiriiInSightV1.Now), At(MarshesKiriiInSightV1.Before)), "Kirii and his marker in sight");
            migrator.Migrate(Previous(world, KiriiInSight));
            Assert.AreEqual((0, 2), (At(MarshesKiriiInSightV1.Now), At(MarshesKiriiInSightV1.Before)), "both at the rock pile's foot");
            migrator.Migrate(KiriiInSight);
            Assert.AreEqual((2, 0), (At(MarshesKiriiInSightV1.Now), At(MarshesKiriiInSightV1.Before)), "back in sight");
            migrator.Migrate();
            Assert.AreEqual((2, 0), (At(MarshesKiriiInOpenV1.Now), At(MarshesKiriiInOpenV1.Before)), "in the open at the head");
        }

        /// <summary>
        /// At MarshesKiriiInSightV1's place the real client still refused the talk from the south-west, the way its walk to him
        /// came (2026-11-05). His pool and his 891 marker stand in the open now; rolled back, both at MarshesKiriiInSightV1's
        /// place; up again (the Up twice), in the open.
        /// </summary>
        [TestMethod]
        public void KiriiAndHisMarkerStandInTheOpen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            int At((double X, double Y, double Z) at) => KiriiAt(world, at);

            Assert.AreEqual((2, 0), (At(MarshesKiriiInOpenV1.Now), At(MarshesKiriiInOpenV1.Before)), "Kirii and his marker in the open");
            migrator.Migrate(Previous(world, KiriiInOpen));
            Assert.AreEqual((0, 2), (At(MarshesKiriiInOpenV1.Now), At(MarshesKiriiInOpenV1.Before)), "both by the rock pile again");
            migrator.Migrate(KiriiInOpen);
            Assert.AreEqual((2, 0), (At(MarshesKiriiInOpenV1.Now), At(MarshesKiriiInOpenV1.Before)), "back in the open");
            foreach (var sql in MarshesKiriiInOpenV1.Up())
                world.Database.ExecuteSqlRaw(MarshesSql.Guard(sql));
            Assert.AreEqual((2, 0), (At(MarshesKiriiInOpenV1.Now), At(MarshesKiriiInOpenV1.Before)), "the Up twice: still one place each");
        }

        /// <summary>How many of Kirii's pool and his 891 marker stand at <paramref name="at"/>.</summary>
        private static int KiriiAt(DbContext world, (double X, double Y, double Z) at) => Count(world, System.FormattableString.Invariant(
            $"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {MarshesPreoDasVillageNpcsV1.Kirii} AND ABS(pos_x - {at.X}) < 0.01 AND ABS(pos_y - {at.Y}) < 0.01 AND ABS(pos_z - {at.Z}) < 0.01")) +
            Count(world, System.FormattableString.Invariant(
            $"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id = {MarshesNeverSurrenderV1.MissionId} AND content_revision = '{MarshesMissionDataV1.Revision}' AND ABS(pos_x - {at.X}) < 0.01 AND ABS(pos_z - {at.Z}) < 0.01"));

        /// <summary>
        /// Hax died 22-24 s into 546's hack with the escort's 4000 hit points (review list, 2026-11-04). His stat row and
        /// max_hp have 12000 now; rolled back, 4000.
        /// </summary>
        [TestMethod]
        public void HaxHasTheHitPointsForTheHack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            var hax = MarshesLogosResearchNpcsV1.Hax;
            (int, int) Health() => (Count(world, $"SELECT health AS Value FROM creature_stat WHERE id = {hax}"),
                Count(world, $"SELECT max_hp AS Value FROM creature WHERE id = {hax}"));

            Assert.AreEqual(12000U, MarshesHaxHoldsV1.HitPoints, "three times the escort's (ESTIMATE)");
            Assert.AreEqual((12000, 12000), Health(), "Hax's hit points");
            migrator.Migrate(Previous(world, HaxHolds));
            Assert.AreEqual((4000, 4000), Health(), "the escort's");
            migrator.Migrate(HaxHolds);
            Assert.AreEqual((12000, 12000), Health(), "back");
        }

        /// <summary>
        /// 920's guard station held only with GM kills (review list, 2026-11-04). The soldiers stand still, Shaw's stand-in
        /// and the soldiers have 12000 and 8000 hit points, the waves spare Shaw and wave 1 has three Thrax; rolled back, the
        /// Rifleman's speeds, the escort's 4000 and the binding as MarshesHeadEastV2 and Marshes_on_the_navmesh left it, to
        /// the byte.
        /// </summary>
        [TestMethod]
        public void TheGuardStationHasTheMenToHold()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            const uint mission = MarshesHeadEastV2.MissionId, shaw = MarshesEastAndNorthNpcsV1.ShawStandIn, soldier = MarshesEastAndNorthNpcsV1.StationSoldier;
            (int, int, int, int) Health() => (Count(world, $"SELECT health AS Value FROM creature_stat WHERE id = {shaw}"),
                Count(world, $"SELECT max_hp AS Value FROM creature WHERE id = {shaw}"),
                Count(world, $"SELECT health AS Value FROM creature_stat WHERE id = {soldier}"),
                Count(world, $"SELECT max_hp AS Value FROM creature WHERE id = {soldier}"));
            int Still() => Count(world, $"SELECT COUNT(*) AS Value FROM creature WHERE id = {soldier} AND run_speed = 0 AND walk_speed = 0");
            int AsRifleman() => Count(world, $"SELECT COUNT(*) AS Value FROM creature s JOIN creature r ON r.id = {MarshesEastAndNorthNpcsV1.AfsRifleman} " +
                $"WHERE s.id = {soldier} AND s.run_speed = r.run_speed AND s.walk_speed = r.walk_speed AND r.run_speed > 0");

            var before = MarshesGuardStationHoldsV1.Before();
            Assert.AreEqual(before, MarshesBindings.Json(MarshesBindings.Read(before)), "the stored binding reads and writes back the same");
            Assert.Contains(MarshesOnTheNavmeshV1.Moves.Single(move => move.Mission == mission).Now.Z.ToString(System.Globalization.CultureInfo.InvariantCulture), before,
                "with Marshes_on_the_navmesh's ward-3");
            var scene = MarshesBindings.Read(Binding(world, mission));
            Assert.AreEqual(1U, scene.Names[Rasa.Missions.Content.Marshes.MarshesDefenceScene.Spare], "the waves spare Shaw");
            Assert.AreEqual(3, scene.Actors.Keys.Count(role => role.StartsWith("wave-1-")), "wave 1 three Thrax");
            Assert.AreEqual(5, scene.Actors.Keys.Count(role => role.StartsWith("wave-2-")), "wave 2 as before");
            Assert.IsFalse(scene.DefeatSequences.ContainsKey(MarshesGuardStationHoldsV1.LeftOut), "no defeat sequence for the left-out Thrax");
            Assert.AreEqual((12000, 12000, 8000, 8000), Health(), "Shaw's and the soldiers' hit points");
            Assert.AreEqual((1, 0), (Still(), AsRifleman()), "the soldiers stand still");

            migrator.Migrate(Previous(world, GuardStationHolds));
            Assert.AreEqual(before, Binding(world, mission), "the binding as it was");
            Assert.AreEqual((4000, 4000, 4000, 4000), Health(), "the escort's");
            Assert.AreEqual((0, 1), (Still(), AsRifleman()), "the Rifleman's speeds");
            migrator.Migrate(GuardStationHolds);
            Assert.AreEqual(MarshesBindings.Json(MarshesGuardStationHoldsV1.Scene()), Binding(world, mission), "the new binding back");
            Assert.AreEqual((12000, 12000, 8000, 8000), Health(), "back");
            Assert.AreEqual((1, 0), (Still(), AsRifleman()), "still again");
        }

        /// <summary>
        /// 1052's wave 3 (five Thrax) killed the crew and Samuel against one player (review list, to-fix 29 recheck,
        /// 2026-11-04). Wave 3 has three now, waves 1 and 2 and the spare as before; rolled back, the binding as
        /// MarshesTrappedV3 wrote it, to the byte.
        /// </summary>
        [TestMethod]
        public void TrappedsThirdWaveIsOnePlayersFight()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            const uint mission = MarshesTrappedV1.MissionId;
            int Members(MissionSceneDefinition scene, int wave) => scene.Actors.Keys.Count(role => role.StartsWith($"wave-{wave}-"));

            var before = MarshesTrappedSmallerWaveV1.Before();
            Assert.AreEqual(before, MarshesBindings.Json(MarshesBindings.Read(before)), "the stored binding reads and writes back the same");
            // Marshes_trapped_lighter_waves, after it, rolls back first.
            migrator.Migrate(TrappedSmallerWave);
            var scene = MarshesBindings.Read(Binding(world, mission));
            Assert.AreEqual((3, 4, 3), (Members(scene, 1), Members(scene, 2), Members(scene, 3)), "the waves' sizes");
            Assert.AreEqual(1U, scene.Names[Rasa.Missions.Content.Marshes.MarshesDefenceScene.Spare], "the spare stays");
            Assert.IsTrue(MarshesTrappedSmallerWaveV1.LeftOut.All(role => !scene.DefeatSequences.ContainsKey(role)), "no defeat sequences for the left-out Thrax");
            CollectionAssert.AreEqual(MarshesTrappedSmallerWaveV1.Waves.Select(wave => wave.Members.Length).ToArray(), new[] { 3, 4, 3 });

            migrator.Migrate(Previous(world, TrappedSmallerWave));
            Assert.AreEqual(before, Binding(world, mission), "the binding as MarshesTrappedV3 wrote it");
            Assert.AreEqual(5, Members(MarshesBindings.Read(before), 3), "wave 3 five Thrax then");
            migrator.Migrate(TrappedSmallerWave);
            Assert.AreEqual(MarshesBindings.Json(MarshesTrappedSmallerWaveV1.Scene()), Binding(world, mission), "the new binding back");
            migrator.Migrate();
            Assert.AreEqual(MarshesBindings.Json(MarshesTrappedClearOfDepotV1.Scene()), Binding(world, mission), "the head's binding");
        }

        /// <summary>
        /// 1052: Samuel died in wave 2 against one player killing a Thrax per 10-15 s (real client, 2026-11-05). Wave 1 has two
        /// Thrax now and wave 2 three, 100 s after the talk; wave 3, the hold and the spare as before; rolled back, the binding
        /// as MarshesTrappedSmallerWaveV1 wrote it, to the byte; the Up twice, the same.
        /// </summary>
        [TestMethod]
        public void TrappedsFirstWavesAreOnePlayersFight()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            const uint mission = MarshesTrappedV1.MissionId;
            int Members(MissionSceneDefinition scene, int wave) => scene.Actors.Keys.Count(role => role.StartsWith($"wave-{wave}-"));
            uint Name(MissionSceneDefinition scene, string name) => scene.Names[name];
            var now = MarshesBindings.Json(MarshesTrappedLighterWavesV1.Scene());

            // Marshes_trapped_clear_of_depot, after it, rolls back first.
            migrator.Migrate(TrappedLighterWaves);
            Assert.AreEqual(now, Binding(world, mission), "the lighter waves");
            var scene = MarshesBindings.Read(now);
            Assert.AreEqual((2, 3, 3), (Members(scene, 1), Members(scene, 2), Members(scene, 3)), "the waves' sizes");
            Assert.IsTrue(MarshesTrappedLighterWavesV1.LeftOut.All(role => !scene.DefeatSequences.ContainsKey(role)), "no defeat sequences for the left-out Thrax");
            Assert.AreEqual((5000U, 100000U, 150000U, (uint)MarshesTrappedV1.HoldMilliseconds, 1U),
                (Name(scene, MarshesDefenceScene.After(1)), Name(scene, MarshesDefenceScene.After(2)), Name(scene, MarshesDefenceScene.After(3)),
                 Name(scene, MarshesDefenceScene.Hold), Name(scene, MarshesDefenceScene.Spare)), "the times, the hold and the spare");

            migrator.Migrate(Previous(world, TrappedLighterWaves));
            Assert.AreEqual(MarshesTrappedLighterWavesV1.Before(), Binding(world, mission), "the binding as MarshesTrappedSmallerWaveV1 wrote it");
            Assert.AreEqual(75000U, Name(MarshesBindings.Read(Binding(world, mission)), MarshesDefenceScene.After(2)), "wave 2 at 75 s then");
            migrator.Migrate(TrappedLighterWaves);
            Assert.AreEqual(now, Binding(world, mission), "the lighter waves back");
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            MarshesTrappedLighterWavesV1.Up(builder);
            var connection = world.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            foreach (var sql in builder.Operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>())
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql.Sql;   // the JSON's braces as they are: no format pass
                command.ExecuteNonQuery();
            }
            Assert.AreEqual(now, Binding(world, mission), "the Up twice: the same binding");
        }

        /// <summary>
        /// 546: Hax died 42-51 s into the hack against one player killing a Thrax per 10-15 s (real client, 2026-11-05). The
        /// waves are three Thrax each now and every member attacks the player (no target role); rolled back, the binding as
        /// MarshesTasteOfTheirOwnMedicineV1 wrote it, to the byte.
        /// </summary>
        [TestMethod]
        public void TheHackWavesGoForThePlayer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            const uint mission = MarshesTasteOfTheirOwnMedicineV1.MissionId;
            var now = MarshesBindings.Json(MarshesHackWavesOnPlayerV1.Scene());
            var before = MarshesHackWavesOnPlayerV1.Before();
            Assert.AreEqual(before, MarshesBindings.Json(MarshesBindings.Read(before)), "the stored binding reads and writes back the same");

            Assert.AreEqual(now, Binding(world, mission), "the new waves at the head");
            var scene = MarshesBindings.Read(now);
            var attacks = scene.Sequences.Values.SelectMany(sequence => sequence.World).OfType<Rasa.Missions.Scenes.AttackActorIntent>().ToList();
            Assert.HasCount(9, attacks, "nine wave members, each with its attack");
            Assert.IsTrue(attacks.All(attack => attack.TargetRole == null), "each attacks the player");
            Assert.AreEqual(9, scene.Actors.Keys.Count(role => role.StartsWith("wave-")), "three per wave");
            Assert.IsFalse(scene.Sequences.Values.SelectMany(sequence => sequence.World).Any(intent => MarshesHackWavesOnPlayerV1.LeftOut.Contains(intent.Role)),
                "nothing left of the fourth members");

            migrator.Migrate(Previous(world, HackWavesOnPlayer));
            Assert.AreEqual(before, Binding(world, mission), "the binding as MarshesTasteOfTheirOwnMedicineV1 wrote it");
            Assert.AreEqual(6, MarshesBindings.Read(before).Sequences.Values.SelectMany(sequence => sequence.World)
                .OfType<Rasa.Missions.Scenes.AttackActorIntent>().Count(attack => attack.TargetRole == MarshesTasteOfTheirOwnMedicineV1.HaxRole), "six on Hax then");
            migrator.Migrate(HackWavesOnPlayer);
            Assert.AreEqual(now, Binding(world, mission), "the new waves back");
        }

        /// <summary>
        /// 1052: the depot's Thrax (pool 531291) joined the defence in both real-client tries of 2026-11-05. The crew, its
        /// waves, the approach area and the two markers stand 25 m west now, clear of it; rolled back, the binding as
        /// MarshesTrappedLighterWavesV1 wrote it, to the byte, and the area and the markers on MarshesTrappedV3's Samuel.
        /// </summary>
        [TestMethod]
        public void TrappedIsFoughtClearOfTheDepot()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            const uint mission = MarshesTrappedV1.MissionId;
            var now = MarshesBindings.Json(MarshesTrappedClearOfDepotV1.Scene());
            var before = MarshesTrappedClearOfDepotV1.Before();
            string Where(ScenePosition at) => FormattableString.Invariant(
                $"mission_id = {mission} AND content_revision = '{MarshesMissionDataV1.Revision}' AND ABS(pos_x - {at.X}) < 0.01 AND ABS(pos_y - {at.Y}) < 0.01 AND ABS(pos_z - {at.Z}) < 0.01");
            (int, int) Placed(ScenePosition at) => (Count(world, $"SELECT COUNT(*) AS Value FROM mission_area WHERE area_id = {MarshesTrappedV2.ApproachArea} AND {Where(at)}"),
                Count(world, $"SELECT COUNT(*) AS Value FROM mission_indicator WHERE objective_id IN (1, 2) AND {Where(at)}"));
            Assert.AreEqual(before, MarshesBindings.Json(MarshesBindings.Read(before)), "the stored binding reads and writes back the same");

            Assert.AreEqual(now, Binding(world, mission), "the crew clear of the depot at the head");
            var scene = MarshesBindings.Read(now);
            Assert.AreEqual(MarshesTrappedClearOfDepotV1.SamuelAt, scene.Actors[MarshesDefenceScene.Leader].Position, "Samuel's stand-in");
            Assert.AreEqual(MarshesTrappedClearOfDepotV1.WavesAt[1][0], scene.Actors[MarshesDefenceScene.WaveRole(2, 1)].Position, "wave 2 from the north-west");
            var old = MarshesBindings.Read(before);
            Assert.AreEqual(old.Names.Count, scene.Names.Count, "the names as they were");
            Assert.IsTrue(old.Names.All(name => scene.Names[name.Key] == name.Value), "the times, the hold and the spare as they were");
            CollectionAssert.AreEquivalent(old.Actors.Keys.ToArray(), scene.Actors.Keys.ToArray(), "the same actors");
            Assert.IsTrue(old.Actors.All(actor => Json(scene.Actors[actor.Key]) == Json(actor.Value with { Position = scene.Actors[actor.Key].Position })),
                "only the places changed");
            Assert.AreEqual((1, 2), Placed(MarshesTrappedClearOfDepotV1.SamuelAt), "the approach area and the markers on the stand-in");

            migrator.Migrate(Previous(world, TrappedClearOfDepot));
            Assert.AreEqual(before, Binding(world, mission), "the binding as MarshesTrappedLighterWavesV1 wrote it");
            Assert.AreEqual((1, 2), Placed(MarshesTrappedV3.SamuelAt), "the area and the markers where MarshesTrappedV3 put them");
            Assert.AreEqual((0, 0), Placed(MarshesTrappedClearOfDepotV1.SamuelAt), "none at the new place");
            migrator.Migrate(TrappedClearOfDepot);
            Assert.AreEqual(now, Binding(world, mission), "the new places back");
            Assert.AreEqual((1, 2), Placed(MarshesTrappedClearOfDepotV1.SamuelAt), "the area and the markers back");
        }

        /// <summary>
        /// 1052: a Retread lasted 15-30 s under one Thrax with the escort's 4000 hit points (real client, 2026-11-05: the
        /// first died 31 s after wave 2). The Retreads have 8000 now; rolled back, the escort's.
        /// </summary>
        [TestMethod]
        public void TrappedsRetreadsHoldAThrax()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            const uint retread = MarshesRetreadCityNpcsV1.Retread;
            (int, int) Health() => (Count(world, $"SELECT health AS Value FROM creature_stat WHERE id = {retread}"),
                Count(world, $"SELECT max_hp AS Value FROM creature WHERE id = {retread}"));

            Assert.AreEqual((8000, 8000), Health(), "twice the escort's");
            Assert.AreEqual((12000, 12000), (Count(world, $"SELECT health AS Value FROM creature_stat WHERE id = {MarshesRetreadCityNpcsV1.SamuelStandIn}"),
                Count(world, $"SELECT max_hp AS Value FROM creature WHERE id = {MarshesRetreadCityNpcsV1.SamuelStandIn}")), "Samuel's stay");
            migrator.Migrate(Previous(world, TrappedRetreadsHold));
            Assert.AreEqual((4000, 4000), Health(), "the escort's");
            migrator.Migrate(TrappedRetreadsHold);
            Assert.AreEqual((8000, 8000), Health(), "back");
        }

        /// <summary>
        /// 920: from 36-43 m out wave 1 was at the guard station before the player met it (real client, 2026-11-05). The waves
        /// come from 54-62 m out now; rolled back, the binding as MarshesGuardStationHoldsV1 wrote it, to the byte.
        /// </summary>
        [TestMethod]
        public void HeadEastsWavesComeFromFarOut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            const uint mission = MarshesHeadEastV2.MissionId;
            var now = MarshesBindings.Json(MarshesGuardStationWavesOutV1.Scene());
            var before = MarshesGuardStationWavesOutV1.Before();
            Assert.AreEqual(before, MarshesBindings.Json(MarshesBindings.Read(before)), "the stored binding reads and writes back the same");

            Assert.AreEqual(now, Binding(world, mission), "the waves far out at the head");
            var scene = MarshesBindings.Read(now);
            var old = MarshesBindings.Read(before);
            CollectionAssert.AreEquivalent(old.Actors.Keys.ToArray(), scene.Actors.Keys.ToArray(), "the same actors");
            Assert.IsTrue(old.Actors.Where(actor => !actor.Key.StartsWith("wave-")).All(actor => Json(scene.Actors[actor.Key]) == Json(actor.Value)), "the wards and Meeko stay");
            Assert.AreEqual(8, scene.Actors.Keys.Count(role => role.StartsWith("wave-") && scene.Actors[role].Position != old.Actors[role].Position), "every wave member moved");
            Assert.AreEqual(MarshesGuardStationWavesOutV1.WavesAt[1][4], scene.Actors[MarshesDefenceScene.WaveRole(2, 5)].Position, "the Stalker from the east");

            migrator.Migrate(Previous(world, GuardStationWavesOut));
            Assert.AreEqual(before, Binding(world, mission), "the binding as MarshesGuardStationHoldsV1 wrote it");
            migrator.Migrate(GuardStationWavesOut);
            Assert.AreEqual(now, Binding(world, mission), "the new places back");
        }
    }
}
