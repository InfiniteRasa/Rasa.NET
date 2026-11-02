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
    using C = CraterLakeContent;
    using static Wilderness.WildernessTwinPillarsTests;

    /// <summary>
    /// Crater Lake Research Facility as the walkthrough tells it (CraterLakeContent,
    /// CraterLakeFacility): its rows and ten missions, the jammer and the beacon, the terminals,
    /// the crates, the cache, the generators and their fields, the Overseer and his shield, the
    /// pen, Daniel's two ways out, and the Bane beamed in.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CraterLakeTests
    {
        private const string Migration = "20271010000000_Crater_lake", Previous = "20271009000000_Three_devils_respawn";

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            CraterLakeFacility.Reset();
            CraterLakeFacility.Now = () => _now;
            CraterLakeFacility.Roll = (min, max) => max;
            PlacedDestructibles.Now = () => _now;
            PlacedUsables.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            CraterLakeFacility.Reset();
            CraterLakeFacility.Now = () => Environment.TickCount64;
            CraterLakeFacility.Roll = (min, max) => Random.Shared.NextInt64(min, max + 1);
            PlacedDestructibles.Now = () => Environment.TickCount64;
            PlacedUsables.Now = () => Environment.TickCount64;
            PlacedUsables.Reset();
        }

        [TestMethod]
        public void TheFacilityHasItsPeopleItsBaneAndItsTenMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in C.Missions)
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out _), $"mission {missionId}");

            var definitions = world.MissionContentDefinitionEntries.AsNoTracking().Where(row => row.ContentRevision == C.Revision).ToList();
            Assert.AreEqual(C.Missions.Length, definitions.Count(row => row.Enabled));
            foreach (var outdoor in new[] { C.TheDeadLive, C.MendingABrokenHeart, C.CentrifugeFuel, C.LogosMovementAroundChaos })
                Assert.AreEqual(C.WildernessCategory, definitions.Single(row => row.MissionId == outdoor).CategoryId, $"{outdoor}");
            foreach (var inside in new[] { C.WeBeJammin, C.DestroyingTheEvidence, C.GhostInTheMachine, C.ReclaimingWhatsOurs, C.ReturnToSender, C.BendingTheRules })
                Assert.AreEqual(C.Category, definitions.Single(row => row.MissionId == inside).CategoryId, $"{inside}");
            Assert.IsTrue(definitions.Single(row => row.MissionId == C.WeBeJammin).RadioCompleteable);
            Assert.IsTrue(definitions.Single(row => row.MissionId == C.DestroyingTheEvidence).RadioCompleteable);

            // The people: who speaks what, and where Velns stands.
            var packages = world.Set<NpcPackageEntry>().AsNoTracking().ToDictionary(row => row.Id, row => row.PackageId);
            Assert.AreEqual(C.VelnsPackage, packages[C.Velns]);
            Assert.AreEqual(C.FranjaPackage, packages[C.Franja]);
            Assert.AreEqual(8871U, world.Set<CreatureEntry>().Single(c => c.Id == C.Velns).NameId);
            Assert.AreEqual(3103U, world.Set<CreatureEntry>().Single(c => c.Id == C.Daniel).NameId);
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == C.Velns && a.SlotId == 15));
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == C.Daniel));

            var pools = world.Set<SpawnPoolEntry>().AsNoTracking().Where(pool => pool.MapContextId == C.CraterLake).ToList();
            Assert.AreEqual(C.Pools.Length, pools.Count(pool => pool.Id is >= 675200 and <= 675299));
            Assert.AreEqual(C.Velns, pools.Single(pool => pool.Id == C.VelnsPool).Creature1Id);

            // The Bane at the squad's levels, each with its loot on this map.
            var hostile = world.Set<CreatureEntry>().AsNoTracking().Where(c => C.Hostiles.Contains(c.Id)).ToList();
            Assert.AreEqual(C.Hostiles.Length, hostile.Count);
            Assert.IsTrue(hostile.All(c => c.Level is >= 10 and <= 12), string.Join(", ", hostile.Select(c => $"{c.Id}:{c.Level}")));
            Assert.AreEqual(7002U, hostile.Single(c => c.Id == C.Overseer).NameId);
            var loot = world.Set<CreatureLootBindingEntry>().AsNoTracking().Where(b => b.MapContextId == C.CraterLake).ToList();
            CollectionAssert.IsSubsetOf(C.Hostiles, loot.Select(b => b.CreatureId).ToArray());
            foreach (var pool in pools.Where(pool => pool.Id is >= 675210 and <= 675299))
                Assert.IsTrue(C.Hostiles.Contains(pool.Creature1Id), $"pool {pool.Id}");

            // Every piece of gear carries its brand's module; the rest carry none.
            var rewards = world.Set<MissionRewardItemEntry>().AsNoTracking().Where(item => item.ContentRevision == C.Revision).ToList();
            var modules = rewards.ToDictionary(item => (item.MissionId, item.ItemTemplateId), item => item.Module1);
            Assert.AreEqual(C.ResistEmp, modules[(C.WeBeJammin, C.PulsarHazmatBoots)]);
            Assert.AreEqual(0U, modules[(C.WeBeJammin, C.TeleractPhotonicShotgun)]);
            Assert.AreEqual(C.ThreatReduction, modules[(C.ReclaimingWhatsOurs, C.EclipseLaserRifle)]);
            Assert.AreEqual(C.ResistEmp, modules[(C.ReclaimingWhatsOurs, C.PulsarReflectiveGloves)]);
            Assert.AreEqual(C.ResistEmp, modules[(C.BendingTheRules, C.PulsarHazmatVest)]);
            Assert.AreEqual(0U, modules[(C.ReturnToSender, C.LaserChaingun)]);

            // Each marker is the client's own name for it, or none.
            var markers = world.Set<MissionIndicatorEntry>().AsNoTracking().Where(row => row.ContentRevision == C.Revision).ToList();
            Assert.IsTrue(markers.All(row => new[] { C.BeaconMarker, C.BaneEquipmentMarker, C.GeneratorMarker }.Contains(row.IndicatorId) ||
                row.IndicatorId > MissionIndicator.UnnamedFrom), string.Join(", ", markers.Select(row => row.IndicatorId)));
            Assert.IsFalse(markers.Any(row => row.MissionId == C.LogosMovementAroundChaos), "the Logos are for scouting");
        }

        [TestMethod]
        public void FranjaSendsYouToVelnsAndVelnsGivesHisTwo()
        {
            using var harness = Create();
            harness.SpawnWorld(C.VelnsPool);
            var velns = RequireNpc(harness, C.VelnsPool, C.Velns);

            var franja = Put(harness, C.Franja, C.Arrival + new Vector3(2, 0, 0));
            Accept(harness, franja, C.TheDeadLive);

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, velns.EntityId, C.WeBeJammin), "after The Dead Live");
            Talk(harness, velns, C.TheDeadLive, C.SpeakToVelns);
            Reward(harness, velns, C.TheDeadLive, 0, null, 4000, 800);
            Assert.AreEqual(3L, Held(harness, C.StandardMedPack));

            Accept(harness, velns, C.WeBeJammin);
            Assert.AreEqual(1L, Held(harness, C.BeaconTemplate), "the GPS beacon comes with it");
            Accept(harness, velns, C.DestroyingTheEvidence);
        }

        [TestMethod]
        public void TheTransmitterThenTheBeaconAndHeadquartersCallsAboutCasper()
        {
            using var harness = Create();
            StartWithVelns(harness, C.WeBeJammin);
            var log = harness.Client.Player.Missions[C.WeBeJammin];
            var site = Object(harness, C.BeaconSiteClass);

            // While the transmitter stands, the beacon's place takes no fix.
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, CraterLakeFacility.Refusal(harness.Client, site));
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[C.DestroyJammers].State);

            var jammer = PlacedDestructibles.On(harness.Map).Single(live => (uint)live.Placement.ClassId == C.JammerClass);
            Assert.AreEqual(C.JammerHitPoints, jammer.HitPoints);
            Assert.IsTrue(PlacedDestructibles.Destroy(jammer, harness.Client.Player));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.DestroyJammers].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[C.PlaceBeacon].State);

            Use(harness, site);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.PlaceBeacon].State);
            Assert.AreEqual(0L, Held(harness, C.BeaconTemplate), "the place takes the beacon");
            Assert.IsTrue(log.Completeable);

            // Radioed in, and HQ has Casper's location.
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, C.WeBeJammin, 0, null), "radioed in");
            harness.Drain();
            Assert.AreEqual(1L, Held(harness, C.PulsarHazmatBoots));
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, C.GhostInTheMachine), "Ghost in the Machine by radio");
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions.ContainsKey(C.GhostInTheMachine));

            // Long after, the transmitter is still down.
            _now += 3_600_000;
            PlacedDestructibles.Worker(harness.Map);
            Assert.IsTrue(jammer.IsDown);
        }

        [TestMethod]
        public void CasperIsAMachinaAndFranjaTakesHisDogtags()
        {
            using var harness = Create();
            Completed(harness, C.TheDeadLive);
            Completed(harness, C.WeBeJammin);
            harness.Manager.OfferAfterTurnIn(harness.Client, C.WeBeJammin);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, C.GhostInTheMachine));
            harness.Drain();
            Work(harness);

            // A Machina gets up again from an ordinary death: a Critical Death finishes him.
            harness.SpawnWorld(C.Casper);
            var casper = Living(harness, C.Casper).Single();
            harness.MoveTo(casper.Position + new Vector3(2, 0, 0));
            harness.Creatures.HandleCreatureKill(harness.Map, casper, harness.Client.Player, CritKill.Own);
            harness.Drain();
            var log = harness.Client.Player.Missions[C.GhostInTheMachine];
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.EngageCasper].State);

            Assert.AreNotEqual(0UL, casper.CorpseLootEntityId);
            var loot = harness.Map.LootDispensers[casper.CorpseLootEntityId];
            Assert.IsTrue(loot.LootItems.Any(item => item.ItemTemplateId == C.DogtagsTemplate));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client, new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            harness.Drain();
            Assert.AreEqual(1L, Held(harness, C.DogtagsTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.ReturnDogtags].State);

            var franja = Put(harness, C.Franja, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Reward(harness, franja, C.GhostInTheMachine, null, null, 8000, 1500);
            Assert.AreEqual(0L, Held(harness, C.DogtagsTemplate), "Franja takes them");

            // Mending A Broken Heart: her photographs to Baruhi, and back to her.
            Accept(harness, franja, C.MendingABrokenHeart);
            Assert.AreEqual(1L, Held(harness, C.PhotosTemplate));
            var baruhi = Put(harness, C.Baruhi, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Talk(harness, baruhi, C.MendingABrokenHeart, C.DeliverPhotos);
            Assert.AreEqual(0L, Held(harness, C.PhotosTemplate));
            Talk(harness, franja, C.MendingABrokenHeart, C.ReturnToFranja);
            Reward(harness, franja, C.MendingABrokenHeart, null, null, 5000, 1000);
        }

        [TestMethod]
        public void TheTerminalsAndTheBaneEquipmentAreTheEvidence()
        {
            using var harness = Create();
            StartWithVelns(harness, C.DestroyingTheEvidence);
            Work(harness);
            var log = harness.Client.Player.Missions[C.DestroyingTheEvidence];

            foreach (var (objective, _, _, classId, spot, _) in C.Terminals)
            {
                var terminal = harness.Map.DynamicObjects.Single(obj => obj.MissionConversation?.MissionId == C.DestroyingTheEvidence &&
                    obj.MissionConversation.ObjectiveId == objective);
                Assert.AreEqual(classId, (uint)terminal.EntityClassId);
                Assert.AreEqual(C.PackageOf(objective), terminal.MissionConversation.NpcPackageId);
                Assert.IsTrue(Vector3.Distance(terminal.Position, spot) < 1, $"{objective} at {terminal.Position}");
                Converse(harness, terminal, C.DestroyingTheEvidence, objective);
                Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[objective].State, $"objective {objective}");
            }

            Assert.IsTrue(log.Completeable);
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, C.DestroyingTheEvidence, 1, null), "radioed in");
            harness.Drain();
            Assert.AreEqual(1L, Held(harness, C.HazmatHelmet));
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, C.ReclaimingWhatsOurs), "Reclaiming What's Ours by radio");
        }

        [TestMethod]
        public void TheCratesHoldCrystalsAndAreEmptyAWhile()
        {
            using var harness = Create();
            var sten = Put(harness, C.Sten, C.Arrival + new Vector3(2, 0, 0));
            Accept(harness, sten, C.CentrifugeFuel);
            Work(harness);

            var crates = harness.Map.DynamicObjects.Where(obj => (uint)obj.EntityClassId == C.CrateClass).ToList();
            Assert.AreEqual(C.Crates.Length, crates.Count);

            Use(harness, crates[0]);
            Assert.AreEqual(1L, Held(harness, C.CrystalTemplate));
            Assert.IsTrue(CraterLakeFacility.CrateEmpty(harness.Map, 0));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, CraterLakeFacility.Refusal(harness.Client, crates[0]));

            _now += CraterLakeFacility.CrateEmptyMaxMs;
            Work(harness);
            Assert.IsFalse(CraterLakeFacility.CrateEmpty(harness.Map, 0), "back a minute or two later");

            foreach (var crate in crates.Skip(1).Take(4))
                Use(harness, crate);
            Assert.AreEqual((long)C.CrystalsWanted, Held(harness, C.CrystalTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[C.CentrifugeFuel].Objectives[C.FindCrystals].State);

            // Five are enough: no more.
            Use(harness, crates[5]);
            Assert.AreEqual((long)C.CrystalsWanted, Held(harness, C.CrystalTemplate));

            Reward(harness, Put(harness, C.Sten, harness.Client.Player.Position + new Vector3(2, 0, 0)), C.CentrifugeFuel, null, null, 6000, 1200);
            Assert.AreEqual(0L, Held(harness, C.CrystalTemplate), "Sten takes them");
        }

        [TestMethod]
        public void TheGeneratorsTheOverseerAndThePen()
        {
            using var harness = Create();
            StartReclaiming(harness);
            var log = harness.Client.Player.Missions[C.ReclaimingWhatsOurs];

            // The stronghold's fields stop players; shots do nothing to them.
            var fields = PlacedDestructibles.On(harness.Map).Where(live => live.Field != null).Select(live => live.Field).ToList();
            Assert.AreEqual(3, fields.Count);
            Assert.IsTrue(fields.All(ForceFields.StopsPlayer));
            Assert.IsTrue(fields.All(field => !ForceFields.MayShoot(field, harness.Client.Player)));

            // The weapons cache: two detonators.
            Use(harness, Object(harness, C.CacheClass));
            Assert.AreEqual(2L, Held(harness, C.DetonatorTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.GatherBombs].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[C.BlowGenerator].State);

            // Set at the fortification's generator, the bomb goes off and the stronghold opens.
            var outerBomb = Bomb(harness, C.OuterBomb);
            Use(harness, outerBomb.Object);
            Assert.AreEqual(1L, Held(harness, C.DetonatorTemplate), "the bomb takes a detonator");
            Assert.AreEqual(UseObjectState.BombStateArmed, outerBomb.State);
            _now += 10_000;
            PlacedUsables.Worker(harness.Map);
            Work(harness);
            Assert.IsTrue(Generator(harness, C.OuterGenerator).IsDown);
            Assert.IsTrue(CraterLakeFacility.BaseOpen(harness.Map));
            Assert.AreEqual(1, fields.Count(ForceFields.StopsPlayer), "only the pen's");
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.BlowGenerator].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[C.KillOverseer].State);

            // The Overseer: shielded, and drop pods while he fights.
            var overseer = CraterLakeFacility.OverseerOf(harness.Map);
            Assert.IsNotNull(overseer);
            harness.MoveTo(overseer.Position + new Vector3(4, 0, 0));
            var health = overseer.Attributes[Attributes.Health].Current;
            ActorManager.Instance.Damage(harness.Map, overseer, 500, harness.Client.Player);
            harness.Drain();
            Assert.AreEqual(health, overseer.Attributes[Attributes.Health].Current, "his shield takes it");
            overseer.Hate.Ensure(harness.Client.Player.EntityId, Threat.NoticedThreat);
            Work(harness, CraterLakeFacility.DropEveryMs);
            Assert.AreEqual(CraterLakeFacility.DropSize, Living(harness, C.Hunter).Count);

            // The panel does not answer while he lives.
            var panel = harness.Map.DynamicObjects.Single(obj => obj.MissionConversation?.MissionId == C.ReclaimingWhatsOurs);
            Assert.IsFalse(panel.IsEnabled);

            // Down for a few seconds every so often: then anything hurts him.
            for (var i = 0; i < 30 && CraterLakeFacility.ShieldUp(harness.Map); i++)
                Work(harness, 1_000);
            Assert.IsFalse(CraterLakeFacility.ShieldUp(harness.Map));
            Kill(harness, overseer);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.KillOverseer].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[C.UsePanel].State);
            Assert.IsFalse(panel.IsEnabled, "not while the courtyard's generator stands");

            // The courtyard's generator, with the other detonator; the panel answers.
            var innerBomb = Bomb(harness, C.InnerBomb);
            Use(harness, innerBomb.Object);
            _now += 10_000;
            PlacedUsables.Worker(harness.Map);
            Work(harness);
            Assert.IsTrue(Generator(harness, C.InnerGenerator).IsDown);
            Assert.IsTrue(panel.IsEnabled);

            Converse(harness, panel, C.ReclaimingWhatsOurs, C.UsePanel);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.UsePanel].State);
            Work(harness);
            Assert.IsTrue(CraterLakeFacility.PenOpen(harness.Map));
            Assert.IsFalse(fields.Any(ForceFields.StopsPlayer));

            // The pen's two Logos, and Daniel takes it.
            foreach (var logosId in new[] { C.AroundLogos, C.ChaosLogos })
                Learn(harness, logosId);
            Assert.IsTrue(log.Completeable);

            var daniel = CraterLakeFacility.DanielOf(harness.Map);
            Assert.IsNotNull(daniel);
            Assert.IsTrue(Vector3.Distance(daniel.Position, C.DanielInPen) < 2);
            Reward(harness, daniel, C.ReclaimingWhatsOurs, 0, C.EclipseLaserRifle, 15000, 2500);

            // Long after, everything is still down.
            _now += 3_600_000;
            PlacedDestructibles.Worker(harness.Map);
            Work(harness);
            Assert.IsFalse(fields.Any(ForceFields.StopsPlayer));
        }

        [TestMethod]
        public void TheOverseersShieldLetsOnlySonicAndLightningThrough()
        {
            using var harness = Create();
            Work(harness);
            var overseer = CraterLakeFacility.OverseerOf(harness.Map);
            Assert.IsTrue(CraterLakeFacility.ShieldUp(harness.Map));
            Assert.IsTrue(DamageImmunity.IsImmune(overseer, DamageType.Physical));
            Assert.IsTrue(DamageImmunity.IsImmune(overseer, DamageType.Laser));
            Assert.IsFalse(DamageImmunity.IsImmune(overseer, DamageType.Sonic));
            Assert.IsFalse(DamageImmunity.IsImmune(overseer, DamageType.Electrical));

            // Enough Sonic breaks it at once.
            harness.MoveTo(overseer.Position + new Vector3(4, 0, 0));
            var max = overseer.Attributes[Attributes.Health].NormalMax;
            ActorManager.Instance.Damage(harness.Map, overseer, max / 10, harness.Client.Player, DamageType.Sonic);
            harness.Drain();
            Work(harness);
            Assert.IsFalse(CraterLakeFacility.ShieldUp(harness.Map));
            Assert.IsFalse(DamageImmunity.IsImmune(overseer, DamageType.Physical));

            // And it is back a few seconds later.
            Work(harness, CraterLakeFacility.ShieldDownMs);
            Assert.IsTrue(CraterLakeFacility.ShieldUp(harness.Map));
            Assert.IsTrue(DamageImmunity.IsImmune(overseer, DamageType.Physical));
        }

        [TestMethod]
        public void DanielGoesOutWithYouOrToVelnsButNotBoth()
        {
            using var harness = Create();
            Completed(harness, C.TheDeadLive);
            Completed(harness, C.DestroyingTheEvidence);
            Completed(harness, C.ReclaimingWhatsOurs);
            harness.Manager.OfferAfterTurnIn(harness.Client, C.ReclaimingWhatsOurs);
            Work(harness);

            var daniel = CraterLakeFacility.DanielOf(harness.Map);
            Accept(harness, daniel, C.BendingTheRules);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, C.ReturnToSender), "not with Bending the Rules");

            // He follows, and at the door out he is out.
            Work(harness);
            harness.MoveTo(C.Arrival);
            Work(harness);
            Work(harness);
            Assert.IsTrue(Vector3.Distance(daniel.Position, C.Arrival) <= CraterLakeFacility.ExitRadius, $"Daniel at {daniel.Position}");
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[C.BendingTheRules].Objectives[C.EscortOut].State);
            Reward(harness, daniel, C.BendingTheRules, 1, C.LaserChaingun, 12000, 1800);
        }

        [TestMethod]
        public void HeadquartersWantsDanielBroughtToVelns()
        {
            using var harness = Create();
            Completed(harness, C.TheDeadLive);
            Completed(harness, C.DestroyingTheEvidence);
            Completed(harness, C.ReclaimingWhatsOurs);
            harness.SpawnWorld(C.VelnsPool);
            var velns = RequireNpc(harness, C.VelnsPool, C.Velns);
            harness.Manager.OfferAfterTurnIn(harness.Client, C.ReclaimingWhatsOurs);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, C.ReturnToSender));
            harness.Drain();
            Work(harness);

            var daniel = CraterLakeFacility.DanielOf(harness.Map);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, daniel.EntityId, C.BendingTheRules), "not with Return to Sender");

            harness.MoveTo(daniel.Position + new Vector3(2, 0, 0));
            Work(harness);
            harness.MoveTo(velns.Position + new Vector3(2, 0, 0));
            Work(harness);
            Work(harness);
            Assert.IsTrue(Vector3.Distance(daniel.Position, C.VelnsSpot) <= CraterLakeFacility.DeliverRadius, $"Daniel at {daniel.Position}");
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[C.ReturnToSender].Objectives[C.EscortToVelns].State);
            Reward(harness, velns, C.ReturnToSender, 0, C.PulsarHazmatVest, 12000, 1800);
        }

        [TestMethod]
        public void TheTeleportersBeamBaneInUntilTheyAreDown()
        {
            using var harness = Create();
            Work(harness);
            var teleporter = PlacedDestructibles.On(harness.Map).Where(live => (uint)live.Placement.ClassId == C.TeleporterClass)
                .OrderBy(live => Vector3.Distance(live.Object.Position, C.Teleporters[1])).First();

            // Moving about near it, so the lingering waves do not come.
            harness.MoveTo(C.Teleporters[1] + new Vector3(10, 0, 0));
            Work(harness);
            Assert.AreEqual(1, Beamed(harness));
            harness.MoveTo(C.Teleporters[1] + new Vector3(-10, 0, 10));
            Work(harness, CraterLakeFacility.TeleporterEveryMs);
            Assert.AreEqual(2, Beamed(harness));
            harness.MoveTo(C.Teleporters[1] + new Vector3(10, 0, 0));
            Work(harness, CraterLakeFacility.TeleporterEveryMs);
            Assert.AreEqual(CraterLakeFacility.TeleporterLimit, Beamed(harness), "no more than two of its own about");

            Assert.IsTrue(PlacedDestructibles.Destroy(teleporter, harness.Client.Player));
            foreach (var bane in Living(harness, C.Hunter).Concat(Living(harness, C.Predator)))
                Kill(harness, bane);
            harness.MoveTo(C.Teleporters[1] + new Vector3(-10, 0, 10));
            Work(harness, CraterLakeFacility.TeleporterEveryMs);
            Assert.AreEqual(0, Beamed(harness), "down, it sends none");
        }

        [TestMethod]
        public void ASquadThatLingersHasBaneBeamedInBehindIt()
        {
            using var harness = Create();
            Work(harness);
            harness.MoveTo(new Vector3(108f, 136.97f, 58f));
            Work(harness);
            Work(harness, CraterLakeFacility.LingerMs / 2);
            Assert.AreEqual(0, Beamed(harness));

            Work(harness, CraterLakeFacility.LingerMs / 2);
            var wave = Living(harness, C.Hunter).Concat(Living(harness, C.Predator)).ToList();
            Assert.AreEqual(CraterLakeFacility.WaveSize, wave.Count);
            Assert.IsTrue(wave.All(bane => bane.Hate.Ranked().Any(entry => entry.Key == harness.Client.Player.EntityId)));

            // Not again until the copy's cooldown is over.
            Work(harness, CraterLakeFacility.LingerMs);
            Assert.AreEqual(CraterLakeFacility.WaveSize, Beamed(harness));

            // Nobody lingers in the staging post.
            harness.MoveTo(C.VelnsSpot + new Vector3(-2, 0, 0));
            Work(harness, CraterLakeFacility.WaveCooldownMs);
            for (var i = 0; i < 4; i++)
                Work(harness, CraterLakeFacility.LingerMs);
            Assert.AreEqual(CraterLakeFacility.WaveSize, Beamed(harness));
        }

        [TestMethod]
        public void StandleyGivesTheFacilitysThreeLogos()
        {
            using var harness = Create();
            var standley = Put(harness, C.Standley, C.Arrival + new Vector3(2, 0, 0));
            Accept(harness, standley, C.LogosMovementAroundChaos);
            Work(harness);
            Learn(harness, C.MovementLogos);
            Assert.IsFalse(harness.Client.Player.Missions[C.LogosMovementAroundChaos].Completeable);

            // Around and Chaos are in the pen, behind its field: here, the field taken down.
            var pen = PlacedDestructibles.On(harness.Map).Single(live => live.Field != null &&
                Vector3.Distance(live.Object.Position, C.PenGate) < 1);
            PlacedDestructibles.Destroy(pen, null);
            Learn(harness, C.AroundLogos);
            Learn(harness, C.ChaosLogos);
            Assert.IsTrue(harness.Client.Player.Missions[C.LogosMovementAroundChaos].Completeable);
            Reward(harness, Put(harness, C.Standley, harness.Client.Player.Position + new Vector3(2, 0, 0)),
                C.LogosMovementAroundChaos, null, null, 12000, 3000);
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

            foreach (var spot in C.Crates.Concat(C.Caches).Concat(C.DropSpots).Concat(C.Teleporters)
                         .Append(C.BeaconSite).Append(C.Panel).Append(C.BaneData).Append(C.ProcessingTerminal).Append(C.BiologicalTerminal)
                         .Append(C.ObservationTerminal).Append(C.Courtyard).Append(C.DanielInPen).Append(C.VelnsSpot)
                         .Append(C.OuterBomb).Append(C.InnerBomb).Append(C.Jammer))
            {
                var ground = harness.Map.NavMesh.GroundHeight(spot);
                Assert.IsNotNull(ground, $"{spot}");
                Assert.IsTrue(Math.Abs(ground.Value - spot.Y) < 1.5, $"{spot}, ground {ground}");
            }

            foreach (var pool in C.Pools)
            {
                var spot = new Vector3((float)pool.X, (float)pool.Y, (float)pool.Z);
                var ground = harness.Map.NavMesh.GroundHeight(spot);
                Assert.IsNotNull(ground, $"pool {pool.Id}");
                Assert.IsTrue(Math.Abs(ground.Value - spot.Y) < 1.5, $"pool {pool.Id} at {spot}, ground {ground}");
            }
        }

        [TestMethod]
        public void DownTakesItAllOutAndUpAgainChangesNothing()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 675000 AND 675999"));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {C.Franja}"));

            migrator.Migrate(Migration);
            Assert.AreEqual(C.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{C.Revision}' AND enabled = 1"));
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {C.Franja} AND package_id = {C.FranjaPackage}"));
            var rows = Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 675000 AND 675999");
            Assert.AreEqual(C.Creatures.Length, rows);
            Assert.AreEqual(C.Pools.Length, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 675000 AND 675999"));

            foreach (var sql in C.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(rows, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 675000 AND 675999"), "guarded");

            migrator.Migrate(Previous);
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 675000 AND 675999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {C.Franja}"));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM creature_loot_binding WHERE map_context_id = {C.CraterLake} AND creature_id BETWEEN 675000 AND 675999"));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{C.Revision}'"));
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM creature WHERE id = {C.Franja}"), "Franja's row is not ours");

            migrator.Migrate(Migration);
            Assert.AreEqual(C.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{C.Revision}' AND enabled = 1"));
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: C.CraterLake);
            try
            {
                Qualify(harness);
                harness.MoveTo(C.Arrival);
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
                new CharacterStartingExperienceEntry(harness.Client.Player.Id, C.Revision,
                    CharacterStartingExperienceState.Skipped)));
            harness.Client.Player.StartingExperienceCompleted = true;
            harness.Drain();
        }

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
            CraterLakeFacility.Worker(harness.Map);
            harness.Drain();
        }

        private static List<Creature> Living(WildernessRuntimeTestHarness harness, uint dbId) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(c => c.DbId == dbId && c.State != CharacterState.Dead && EntityManager.Instance.Creatures.ContainsKey(c.EntityId)).ToList();

        /// <summary>The Hunters and Predators about: the facility's pools are not spawned here, so all are beamed in.</summary>
        private static int Beamed(WildernessRuntimeTestHarness harness) =>
            Living(harness, C.Hunter).Count + Living(harness, C.Predator).Count;

        /// <summary>A blow of all its health and armour from the harness's player, through the real damage path.</summary>
        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            harness.MoveTo(creature.Position + new Vector3(2, 0, 0));
            var lethal = creature.Attributes[Attributes.Health].Current + Math.Max(0, creature.Attributes[Attributes.Armor].Current);
            ActorManager.Instance.Damage(harness.Map, creature, lethal, harness.Client.Player);
            harness.Drain();
        }

        private static DynamicObject Object(WildernessRuntimeTestHarness harness, uint classId) =>
            harness.Map.DynamicObjects.First(obj => (uint)obj.EntityClassId == classId);

        private static PlacedUsables.Live Bomb(WildernessRuntimeTestHarness harness, Vector3 at) =>
            PlacedUsables.On(harness.Map).Where(live => live.Placement.ClassId == (EntityClasses)C.BombClass)
                .OrderBy(live => Vector3.Distance(live.Object.Position, at)).First();

        private static PlacedDestructibles.Live Generator(WildernessRuntimeTestHarness harness, Vector3 at) =>
            PlacedDestructibles.On(harness.Map).Where(live => (uint)live.Placement.ClassId == C.GeneratorClass)
                .OrderBy(live => Vector3.Distance(live.Object.Position, at)).First();

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

        /// <summary>An object's conversation opened and its objective done, as the client asks it.</summary>
        private static void Converse(WildernessRuntimeTestHarness harness, DynamicObject obj, uint missionId, uint objectiveId)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(obj.Position + new Vector3(2, 0, 0));
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = obj.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = obj.EntityId, MissionId = missionId, ObjectiveId = objectiveId, PlayerFlagId = 1
            });
            harness.Drain();
        }

        /// <summary>The Logos learned at its shrine.</summary>
        private static void Learn(WildernessRuntimeTestHarness harness, uint logosId)
        {
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

        /// <summary>The mission done before, as a history row the server keeps.</summary>
        private static void Completed(WildernessRuntimeTestHarness harness, uint missionId)
        {
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                new CharacterMissionEntry(harness.Client.Player.Id, missionId, (uint)MissionState.Completed)
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    Generation = 1,
                    ContentRevision = C.Revision
                }, harness.UtcNow));
            harness.Client.Player.MissionHistory[missionId] = MissionState.Completed;
            harness.Client.Player.MissionSuccessHistory.Add(missionId);
        }

        /// <summary>The Dead Live done, and one of Velns's two taken from him.</summary>
        private void StartWithVelns(WildernessRuntimeTestHarness harness, uint missionId)
        {
            Completed(harness, C.TheDeadLive);
            harness.SpawnWorld(C.VelnsPool);
            var velns = RequireNpc(harness, C.VelnsPool, C.Velns);
            Accept(harness, velns, missionId);
            Work(harness);
        }

        /// <summary>Destroying the Evidence done, and Reclaiming What's Ours taken by radio.</summary>
        private void StartReclaiming(WildernessRuntimeTestHarness harness)
        {
            Completed(harness, C.TheDeadLive);
            Completed(harness, C.DestroyingTheEvidence);
            harness.Manager.OfferAfterTurnIn(harness.Client, C.DestroyingTheEvidence);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, C.ReclaimingWhatsOurs));
            harness.Drain();
            Work(harness);
        }

        #endregion
    }
}
