using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using P = PravusResearchContent;
    using static Wilderness.WildernessTwinPillarsTests;

    /// <summary>
    /// Pravus Research Facility as the walkthrough tells it (PravusResearchContent,
    /// PravusResearchFacility): its rows and six missions, the frontline, the dishes, the gate and
    /// its keypass, the Machina chamber with its capsule and ambush, and Nylla's armband.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PravusResearchTests
    {
        /// <summary>Lt. Cmdr. Parsons' pool by the Wilderness' door to Pravus.</summary>
        private const uint ParsonsPool = 197;

        private long _now = 1_000_000;
        private long _destructiblesNow = 5_000_000;

        [TestInitialize]
        public void Clock()
        {
            PravusResearchFacility.Reset();
            PravusResearchFacility.Now = () => _now;
            PlacedDestructibles.Now = () => _destructiblesNow;
        }

        [TestCleanup]
        public void Restore()
        {
            PravusResearchFacility.Reset();
            PravusResearchFacility.Now = () => Environment.TickCount64;
            PlacedDestructibles.Now = () => Environment.TickCount64;
        }

        [TestMethod]
        public void TheFacilityHasItsPeopleItsBaneAndItsSixMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in P.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(P.Category, mission.CategoryId);
            }

            // The people: who speaks what, and where they stand.
            var packages = world.Set<NpcPackageEntry>().AsNoTracking().ToDictionary(row => row.Id, row => row.PackageId);
            Assert.AreEqual(P.NyllaPackage, packages[P.NyllaCamp]);
            Assert.AreEqual(P.NyllaPackage, packages[P.NyllaControl]);
            Assert.AreEqual(P.PricePackage, packages[P.Price]);
            Assert.AreEqual(P.MaulisPackage, packages[P.Maulis]);
            Assert.AreEqual(P.JohnsonPackage, packages[P.Johnson]);

            var pools = world.Set<SpawnPoolEntry>().AsNoTracking().Where(pool => pool.MapContextId == P.Pravus).ToList();
            Assert.AreEqual(P.Pools.Length, pools.Count(pool => pool.Id is >= 650201 and <= 650299));
            Assert.AreEqual(P.Johnson, pools.Single(pool => pool.Id == P.JohnsonPool).Creature1Id);
            Assert.AreEqual(4843U, world.Set<CreatureEntry>().Single(c => c.Id == P.NyllaCamp).NameId);
            Assert.AreEqual(205U, world.Set<CreatureEntry>().Single(c => c.Id == P.Price).NameId);
            Assert.AreEqual(204U, world.Set<CreatureEntry>().Single(c => c.Id == P.Perkins).NameId);
            Assert.AreEqual(5286U, world.Set<CreatureEntry>().Single(c => c.Id == P.Maulis).NameId);
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == P.Price && a.SlotId == 15));
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == P.Rifleman && a.SlotId == 13));

            // The Bane and Machina at the squad's levels, each with its loot on this map.
            var hostile = world.Set<CreatureEntry>().AsNoTracking().Where(c => P.Hostiles.Contains(c.Id)).ToList();
            Assert.AreEqual(P.Hostiles.Length, hostile.Count);
            Assert.IsTrue(hostile.All(c => c.Level is >= 10 and <= 12), string.Join(", ", hostile.Select(c => $"{c.Id}:{c.Level}")));
            Assert.AreEqual(12U, hostile.Single(c => c.Id == P.Stalker).Level);
            var loot = world.Set<CreatureLootBindingEntry>().AsNoTracking().Where(b => b.MapContextId == P.Pravus).ToList();
            CollectionAssert.IsSubsetOf(P.Hostiles, loot.Select(b => b.CreatureId).ToArray());
            foreach (var pool in pools.Where(pool => pool.Id is >= 650210 and <= 650299))
                Assert.IsTrue(P.Hostiles.Contains(pool.Creature1Id), $"pool {pool.Id}");

            // Every piece of gear carries its brand's module; the rest carry none.
            var rewards = world.Set<MissionRewardItemEntry>().AsNoTracking().Where(item => item.ContentRevision == P.Revision).ToList();
            var modules = rewards.ToDictionary(item => (item.MissionId, item.ItemTemplateId), item => item.Module1);
            Assert.AreEqual(P.StealHealth, modules[(P.MeansOfProduction, P.VitaliusEmpRifle)]);
            Assert.AreEqual(P.ResistEmp, modules[(P.MeansOfProduction, P.PulsarHazmatHelmet)]);
            Assert.AreEqual(P.ResistPhysical, modules[(P.AssembleWithPerkins, P.OlympiaReflectiveVest)]);
            Assert.AreEqual(P.ResistEmp, modules[(P.AssembleWithPerkins, P.PulsarMotorAssistLegs)]);
            Assert.AreEqual(P.CritHitBonus, modules[(P.PirateRadio, P.AccuMaxLeechGun)]);
            Assert.AreEqual(P.ThreatReduction, modules[(P.PirateRadio, P.EclipsePulseShotgun)]);
            Assert.AreEqual(0U, modules[(P.HigherCalling, P.EmpGrenade)]);

            // Each marker is the client's own name for it, or none, and the client names it by its objective.
            var markers = world.Set<MissionIndicatorEntry>().AsNoTracking().Where(row => row.ContentRevision == P.Revision).ToList();
            Assert.IsTrue(markers.All(row => new[] { 17U, 52U, 53U, 54U, 78U, 79U, 80U }.Contains(row.IndicatorId) ||
                row.IndicatorId > MissionIndicator.UnnamedFrom), string.Join(", ", markers.Select(row => row.IndicatorId)));
            Assert.IsFalse(markers.Any(row => row.MissionId == P.CommunicationControlMachine), "the Logos are for scouting");
        }

        [TestMethod]
        public void ParsonsSendsYouToNyllaAndNyllaGivesTheMeansOfProduction()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Qualify(harness);
            harness.SpawnWorld(ParsonsPool);
            var parsons = RequireNpc(harness, ParsonsPool, P.Parsons);

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, parsons.EntityId, P.Escapist), "after Machinations");
            Completed(harness, P.Machinations, Rasa.Services.Preloader.Missions.Wilderness.WildernessMissionDataV1.Revision);
            Accept(harness, parsons, P.Escapist);

            var nylla = Put(harness, P.NyllaCamp, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Talk(harness, nylla, P.Escapist, 1);
            Reward(harness, nylla, P.Escapist, null, null, 8000, 1200);

            Accept(harness, nylla, P.MeansOfProduction);
            var log = harness.Client.Player.Missions[P.MeansOfProduction];
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[P.FindTheSource].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[P.ObtainTheKeypass].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[P.SpeakWithMaulis].State);
            Assert.AreNotEqual(MissionObjectiveState.Incomplete, log.Objectives[P.DestroyTheCapsule].State);
            Assert.AreNotEqual(MissionObjectiveState.Incomplete, log.Objectives[P.Ambush].State);
        }

        [TestMethod]
        public void PriceSendsYouToTheFrontlineAndPerkinsTakesIt()
        {
            using var harness = Create();
            harness.SpawnWorld(P.PricePool, P.PerkinsPool);
            var price = RequireNpc(harness, P.PricePool, P.Price);
            var perkins = RequireNpc(harness, P.PerkinsPool, P.Perkins);
            Accept(harness, price, P.AssembleWithPerkins);

            // Away from the frontline nothing comes; there, the Machina come on, six at most.
            harness.MoveTo(P.NyllaAtCamp);
            Work(harness, PravusResearchFacility.FrontlineEveryMs);
            Assert.AreEqual(0, Living(harness, P.Machina).Count);

            harness.MoveTo(P.Frontline);
            for (var i = 0; i < 10; i++)
                Work(harness, PravusResearchFacility.FrontlineEveryMs);
            var wave = Living(harness, P.Machina);
            Assert.AreEqual(PravusResearchFacility.FrontlineCap, wave.Count);
            Assert.IsTrue(wave.All(machina => Hates(machina, harness.Client.Player.EntityId)));
            Assert.IsTrue(wave.All(machina => Vector3.Distance(machina.Position, P.Frontline) < 60));

            var killed = 0;
            for (var round = 0; round < 10 && harness.Client.Player.Missions[P.AssembleWithPerkins].Objectives[P.DefeatTheMachina].State == MissionObjectiveState.Incomplete; round++)
            {
                foreach (var machina in Living(harness, P.Machina))
                {
                    Finish(harness, machina);
                    killed++;
                }

                for (var i = 0; i < 8; i++)
                    Work(harness, PravusResearchFacility.FrontlineEveryMs);
            }

            Assert.IsTrue(killed >= P.MachinaToKill, $"{killed}");
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[P.AssembleWithPerkins].Objectives[P.DefeatTheMachina].State);
            Reward(harness, perkins, P.AssembleWithPerkins, 0, P.OlympiaReflectiveVest, 8000, 1200);

            // Done, the frontline stays as it is.
            foreach (var machina in Living(harness, P.Machina))
                Finish(harness, machina);
            harness.MoveTo(P.Frontline);
            Work(harness, PravusResearchFacility.FrontlineEveryMs);
            Assert.AreEqual(0, Living(harness, P.Machina).Count);
        }

        [TestMethod]
        public void EachDishsThreeInfestationsAreItsObjectiveAndStayDown()
        {
            using var harness = Create();
            Completed(harness, P.AssembleWithPerkins, P.Revision);
            harness.SpawnWorld(P.JohnsonPool);
            var johnson = RequireNpc(harness, P.JohnsonPool, P.Johnson);
            Accept(harness, johnson, P.PirateRadio);
            Work(harness);

            var infestations = PlacedDestructibles.On(harness.Map).Where(live => (uint)live.Placement.ClassId == P.InfestationClass).ToList();
            Assert.AreEqual(9, infestations.Count);

            for (var dish = 0; dish < P.Dishes.Length; dish++)
            {
                var (objective, _, _, _, _, _, spot) = P.Dishes[dish];
                var mine = infestations.Where(live => Vector2.Distance(new Vector2(live.Object.Position.X, live.Object.Position.Z),
                    new Vector2(spot.X, spot.Z)) < P.InfestationRadius + 1).ToList();
                Assert.AreEqual(3, mine.Count, $"dish {objective}");

                harness.MoveTo(spot + new Vector3(8, 0, 0));
                Assert.IsTrue(PlacedDestructibles.Destroy(mine[0], harness.Client.Player));
                Work(harness);
                Assert.AreEqual(1U, harness.Client.Player.Missions[P.PirateRadio].Objectives[objective].Counters[0], $"dish {objective}");

                foreach (var live in mine.Skip(1))
                    PlacedDestructibles.Destroy(live, harness.Client.Player);
                Work(harness);
                Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[P.PirateRadio].Objectives[objective].State);
            }

            // Long after, they are still down.
            _destructiblesNow += 3_600_000;
            PlacedDestructibles.Worker(harness.Map);
            Assert.IsTrue(infestations.All(live => live.IsDown));

            Reward(harness, johnson, P.PirateRadio, 1, P.EclipsePulseShotgun, 8000, 1200);
        }

        [TestMethod]
        public void TheGateOpensForTheKeypassAndNothingElse()
        {
            using var harness = Create();
            Work(harness);

            var gate = PlacedDestructibles.On(harness.Map).Single(live => live.Field != null).Field;
            var scanner = harness.Map.DynamicObjects.Single(obj => (uint)obj.EntityClassId == P.KeypassScannerClass);
            Assert.AreEqual(P.Gate, gate.Position);
            Assert.IsTrue(ForceFields.StopsPlayer(gate));
            Assert.IsFalse(ForceFields.MayShoot(gate, harness.Client.Player), "shots do nothing to it");
            Assert.IsTrue(Vector3.Distance(scanner.Position, P.Gate) < 25);

            // Without the keypass the scanner refuses.
            Use(harness, scanner);
            Assert.IsFalse(PravusResearchFacility.GateOpen(harness.Map));
            Assert.IsTrue(ForceFields.StopsPlayer(gate));

            Give(harness, P.KeypassTemplate);
            Use(harness, scanner);
            Assert.IsTrue(PravusResearchFacility.GateOpen(harness.Map));
            Assert.IsFalse(ForceFields.StopsPlayer(gate));
            Assert.AreEqual(1L, Held(harness, P.KeypassTemplate), "the keypass is not taken");

            // And it stays open.
            _destructiblesNow += 3_600_000;
            PlacedDestructibles.Worker(harness.Map);
            Work(harness);
            Assert.IsFalse(ForceFields.StopsPlayer(gate));
        }

        [TestMethod]
        public void TheGatesGrenadierCarriesTheKeypass()
        {
            using var harness = Create();
            StartOn(harness, P.MeansOfProduction);
            harness.SpawnWorld(650230);
            var grenadier = Living(harness, P.GateGrenadier).Single();

            Kill(harness, grenadier);
            Assert.AreNotEqual(0UL, grenadier.CorpseLootEntityId);
            var loot = harness.Map.LootDispensers[grenadier.CorpseLootEntityId];
            Assert.IsTrue(loot.LootItems.Any(item => item.ItemTemplateId == P.KeypassTemplate));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client, new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            harness.Drain();

            Assert.AreEqual(1L, Held(harness, P.KeypassTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[P.MeansOfProduction].Objectives[P.ObtainTheKeypass].State);
        }

        [TestMethod]
        public void TheCapsuleFeedsTheChamberUntilItIsDownAndThenTheAmbushComes()
        {
            using var harness = Create();
            StartOn(harness, P.MeansOfProduction);
            var log = harness.Client.Player.Missions[P.MeansOfProduction];

            // Down in the control room: the source is found, and the chamber turns out Machina.
            harness.MoveTo(P.Capsule + new Vector3(4, 0, -6));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[P.FindTheSource].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[P.DestroyTheCapsule].State);

            for (var i = 0; i < 10; i++)
                Work(harness, PravusResearchFacility.ProductionEveryMs);
            Assert.AreEqual(PravusResearchFacility.ProductionCap, Living(harness, P.ProductionMachina).Count);

            // Without end: those killed are made again.
            foreach (var machina in Living(harness, P.ProductionMachina))
                Finish(harness, machina);
            Work(harness, PravusResearchFacility.ProductionEveryMs);
            Assert.AreEqual(1, Living(harness, P.ProductionMachina).Count);

            var capsule = PlacedDestructibles.On(harness.Map).Single(live => (uint)live.Placement.ClassId == P.CapsuleClass);
            Assert.AreEqual(P.CapsuleHitPoints, capsule.HitPoints);
            Assert.IsTrue(PlacedDestructibles.Destroy(capsule, harness.Client.Player));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[P.DestroyTheCapsule].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[P.Ambush].State);

            // No more Machina; the ambush, at whoever is there.
            var made = Living(harness, P.ProductionMachina).Count;
            for (var i = 0; i < 5; i++)
                Work(harness, PravusResearchFacility.ProductionEveryMs);
            Assert.AreEqual(made, Living(harness, P.ProductionMachina).Count);

            var ambush = Living(harness, P.AmbushThrax);
            Assert.AreEqual((int)P.AmbushCount, ambush.Count);
            Assert.IsTrue(ambush.All(bane => Hates(bane, harness.Client.Player.EntityId)));

            Kill(harness, ambush[0]);
            Work(harness);
            Assert.AreEqual(1U, log.Objectives[P.Ambush].Counters[0]);
            foreach (var bane in ambush.Skip(1))
                Kill(harness, bane);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[P.Ambush].State);
            Assert.IsTrue(log.Completeable);

            // Long after, the capsule is still down; Nylla is in the overlook hall to take it.
            _destructiblesNow += 3_600_000;
            PlacedDestructibles.Worker(harness.Map);
            Assert.IsTrue(capsule.IsDown);

            var nylla = Living(harness, P.NyllaControl).Single();
            Assert.IsTrue(Vector3.Distance(nylla.Position, P.ControlCentre) < 3);
            Reward(harness, nylla, P.MeansOfProduction, 0, P.VitaliusEmpRifle, 15000, 2000);

            // A Higher Calling: her armband, to Baruhi at the Memory Tree.
            Accept(harness, nylla, P.HigherCalling);
            Assert.AreEqual(1L, Held(harness, P.ArmbandTemplate));
            var baruhi = Put(harness, P.Baruhi, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Talk(harness, baruhi, P.HigherCalling, 1);
            Assert.AreEqual(0L, Held(harness, P.ArmbandTemplate));
            Reward(harness, baruhi, P.HigherCalling, null, null, 6000, 800);
            Assert.AreEqual(3L, Held(harness, P.EmpGrenade));
        }

        [TestMethod]
        public void ALateComerIsGivenTheCapsuleAndTheAmbushTheCopyHasDone()
        {
            using var harness = Create();
            harness.MoveTo(P.Capsule + new Vector3(4, 0, -6));
            Work(harness);

            // The squad has been through: the capsule down, the ambush dead.
            var capsule = PlacedDestructibles.On(harness.Map).Single(live => (uint)live.Placement.ClassId == P.CapsuleClass);
            PlacedDestructibles.Destroy(capsule, harness.Client.Player);
            Work(harness);
            foreach (var bane in Living(harness, P.AmbushThrax))
                Kill(harness, bane);
            Work(harness);

            StartOn(harness, P.MeansOfProduction);
            harness.MoveTo(P.Capsule + new Vector3(4, 0, -6));
            for (var i = 0; i < 4; i++)
                Work(harness);

            var log = harness.Client.Player.Missions[P.MeansOfProduction];
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[P.FindTheSource].State);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[P.DestroyTheCapsule].State);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[P.Ambush].State);
            Assert.IsTrue(log.Completeable);
        }

        [TestMethod]
        public void MaulisInThePrisonIsTheBonus()
        {
            using var harness = Create();
            StartOn(harness, P.MeansOfProduction);
            harness.SpawnWorld(P.MaulisPool);
            var maulis = RequireNpc(harness, P.MaulisPool, P.Maulis);
            Talk(harness, maulis, P.MeansOfProduction, P.SpeakWithMaulis);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[P.MeansOfProduction].Objectives[P.SpeakWithMaulis].State);
            Assert.IsFalse(harness.Client.Player.Missions[P.MeansOfProduction].Completeable, "a bonus");
        }

        [TestMethod]
        public void StandleyGivesTheFacilitysThreeLogos()
        {
            using var harness = Create();

            // Standley is at Twin Pillars; the shrines are here.
            var standley = Put(harness, P.Standley, P.Arrival + new Vector3(2, 0, 0));
            Accept(harness, standley, P.CommunicationControlMachine);

            foreach (var logosId in new[] { P.CommunicationLogos, P.ControlLogos, P.MachineLogos })
            {
                Assert.IsFalse(harness.Client.Player.Missions[P.CommunicationControlMachine].Completeable);
                var shrine = harness.Map.DynamicObjects.OfType<Logos>().Single(logos => logos.Id == logosId);
                harness.MoveTo(shrine.Position + new Vector3(1, 0, 0));
                harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
                {
                    EntityId = shrine.EntityId,
                    ActionId = ActionId.UseObject,
                    ActionArgId = DynamicObjectManager.LogosUseArgId
                });
                ActorActionManager.Instance.DoWork(harness.Map, 10000);
                harness.Drain();
                Assert.IsTrue(harness.Client.Player.Logos.Contains(logosId), $"Logos {logosId}");
            }

            Assert.IsTrue(harness.Client.Player.Missions[P.CommunicationControlMachine].Completeable);
            Reward(harness, Put(harness, P.Standley, harness.Client.Player.Position + new Vector3(2, 0, 0)),
                P.CommunicationControlMachine, null, null, 12000, 3000);
        }

        [TestMethod]
        public void NothingIsPutWhereThereIsNoGround()
        {
            using var harness = Create();
            Work(harness);

            foreach (var live in PlacedDestructibles.On(harness.Map).Where(live => live.Field == null))
            {
                var at = live.Object.Position;
                var ground = harness.Map.NavMesh.GroundHeight(at);
                Assert.IsNotNull(ground, $"{live.Placement.ClassId} at {at}");
                Assert.IsTrue(Math.Abs(ground.Value - at.Y) < 0.5, $"{live.Placement.ClassId} at {at}, ground {ground}");
            }

            foreach (var spot in P.FrontlineSpawns.Concat(P.ProductionSpawns).Concat(P.AmbushSpawns).Append(P.ControlCentre).Append(P.Frontline))
                Assert.IsNotNull(NavMeshManager.RandomPointAround(harness.Map, spot, 2f), $"{spot}");
        }

        [TestMethod]
        public void DownTakesItAllOutAndUpAgainChangesNothing()
        {
            const string migration = "20270306000000_Pravus_research", previous = "20270305000000_Devils_den";
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(previous);
            Assert.AreEqual(0, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 650000 AND 650999"));

            migrator.Migrate(migration);
            Assert.AreEqual(6, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{P.Revision}' AND enabled = 1"));
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {P.Johnson} AND package_id = {P.JohnsonPackage}"));
            var rows = Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 650000 AND 650999");
            Assert.AreEqual(P.Creatures.Length, rows);
            Assert.AreEqual(P.Pools.Length, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 650000 AND 650999"));

            foreach (var sql in P.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(rows, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 650000 AND 650999"), "guarded");

            migrator.Migrate(previous);
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 650000 AND 650999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {P.Johnson}"));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM creature_loot_binding WHERE map_context_id = {P.Pravus} AND creature_id BETWEEN 650000 AND 650999"));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{P.Revision}'"));
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM creature WHERE id = {P.Johnson}"), "Johnson's row is not ours");

            migrator.Migrate(migration);
            Assert.AreEqual(6, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{P.Revision}' AND enabled = 1"));
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: P.Pravus);
            try
            {
                Qualify(harness);
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        /// <summary>Past the bootcamp, as the missions want.</summary>
        private static void Qualify(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                new CharacterStartingExperienceEntry(harness.Client.Player.Id, P.Revision,
                    CharacterStartingExperienceState.Skipped)));
            harness.Client.Player.StartingExperienceCompleted = true;
            harness.Drain();
        }

        private static bool Hates(Creature creature, ulong entityId) => creature.Hate.Ranked().Any(entry => entry.Key == entityId);

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, harness.Map.MapInfo.MapContextId);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private void Work(WildernessRuntimeTestHarness harness, long milliseconds = 100)
        {
            _now += milliseconds;
            PravusResearchFacility.Worker(harness.Map);
            harness.Drain();
        }

        private static List<Creature> Living(WildernessRuntimeTestHarness harness, uint dbId) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(c => c.DbId == dbId && c.State != CharacterState.Dead && EntityManager.Instance.Creatures.ContainsKey(c.EntityId)).ToList();

        /// <summary>A blow of all its health and armour from the harness's player, through the real damage path.</summary>
        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position + new Vector3(2, 0, 0));
            var lethal = creature.Attributes[Attributes.Health].Current + Math.Max(0, creature.Attributes[Attributes.Armor].Current);
            ActorManager.Instance.Damage(harness.Map, creature, lethal, harness.Client.Player);
            harness.Drain();
        }

        /// <summary>A Machina finished for good: a Critical Death, which its self revive does not get up from.</summary>
        private static void Finish(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position + new Vector3(2, 0, 0));
            harness.Creatures.HandleCreatureKill(harness.Map, creature, harness.Client.Player, CritKill.Own);
            harness.Drain();
        }

        private static void Use(WildernessRuntimeTestHarness harness, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(1, 0, 0));
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId,
                ActionId = ActionId.UseObject,
                ActionArgId = DynamicObjectManager.FootlockerUseArgId
            });
            ActorActionManager.Instance.DoWork(harness.Map, 10000);
            harness.Drain();
        }

        private static void Give(WildernessRuntimeTestHarness harness, uint template)
        {
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(template, 1) }, unit));
            grant.Publish(harness.Client);
        }

        /// <summary>The mission done before, as a history row the server keeps.</summary>
        private static void Completed(WildernessRuntimeTestHarness harness, uint missionId, string revision)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                new CharacterMissionEntry(harness.Client.Player.Id, missionId, (uint)MissionState.Completed)
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    Generation = 1,
                    ContentRevision = revision
                }, harness.UtcNow));
            harness.Client.Player.MissionHistory[missionId] = MissionState.Completed;
            harness.Client.Player.MissionSuccessHistory.Add(missionId);
        }

        /// <summary>The Escapist done, and The Means of Production taken from Nylla at the encampment.</summary>
        private void StartOn(WildernessRuntimeTestHarness harness, uint missionId)
        {
            Assert.AreEqual(P.MeansOfProduction, missionId);
            Completed(harness, P.Escapist, P.Revision);
            harness.SpawnWorld(P.NyllaPool);
            var nylla = RequireNpc(harness, P.NyllaPool, P.NyllaCamp);
            Accept(harness, nylla, P.MeansOfProduction);
            Work(harness);
        }

        #endregion
    }
}
