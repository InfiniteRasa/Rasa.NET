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
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using C = EnergyWeaponCenterContent;
    using static Wilderness.WildernessTwinPillarsTests;

    /// <summary>
    /// The Energy Weapon Center as the walkthrough tells it (EnergyWeaponCenterContent,
    /// EnergyWeaponCenter): its rows and five missions, the fields, the reactors' beams, the
    /// generator, the datablade and the coils, the monitors, Engelsen's charges and the fuel
    /// tanks, and Casso's link to his minions.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class EnergyWeaponCenterTests
    {
        private const string Migration = "20271011000000_Energy_weapon_center", Previous = "20271010000000_Crater_lake";

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            EnergyWeaponCenter.Reset();
            EnergyWeaponCenter.Now = () => _now;
            EnergyWeaponCenter.Roll = (min, max) => max;
            PlacedDestructibles.Now = () => _now;
            PlacedUsables.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            EnergyWeaponCenter.Reset();
            EnergyWeaponCenter.Now = () => Environment.TickCount64;
            EnergyWeaponCenter.Roll = (min, max) => Random.Shared.NextInt64(min, max + 1);
            PlacedDestructibles.Now = () => Environment.TickCount64;
            PlacedUsables.Now = () => Environment.TickCount64;
            PlacedUsables.Reset();
        }

        [TestMethod]
        public void TheCenterHasItsPeopleItsBaneAndItsFiveMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in C.Missions)
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out _), $"mission {missionId}");

            var definitions = world.MissionContentDefinitionEntries.AsNoTracking().Where(row => row.ContentRevision == C.Revision).ToList();
            Assert.AreEqual(C.Missions.Length, definitions.Count(row => row.Enabled));
            Assert.IsTrue(definitions.All(row => row.CategoryId == C.Category));
            CollectionAssert.AreEquivalent(new[] { C.AllInTheTiming, C.TeslaInversion, C.LookBeforeYouSabotage },
                definitions.Where(row => row.RadioCompleteable).Select(row => row.MissionId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 28, 28, 29, 29, 30 },
                C.Missions.Select(id => definitions.Single(row => row.MissionId == id).Level).ToArray());

            // The people: Wolfe's package, and where the two stand.
            var packages = world.Set<NpcPackageEntry>().AsNoTracking().ToDictionary(row => row.Id, row => row.PackageId);
            Assert.AreEqual(C.WolfePackage, packages[C.Wolfe]);
            Assert.AreEqual(9341U, world.Set<CreatureEntry>().Single(c => c.Id == C.Wolfe).NameId);
            Assert.AreEqual(8680U, world.Set<CreatureEntry>().Single(c => c.Id == C.Brandenburg).NameId);
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == C.Wolfe && a.SlotId == 13));
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == C.Brandenburg && a.SlotId == 15));

            var pools = world.Set<SpawnPoolEntry>().AsNoTracking().Where(pool => pool.Id >= 710000 && pool.Id <= 710999).ToList();
            Assert.AreEqual(C.Pools.Length + C.AlarmPools.Length, pools.Count);
            Assert.AreEqual(C.Mires, pools.Single(pool => pool.Id == C.BrandenburgPool).MapContextId);
            Assert.IsTrue(pools.Where(pool => pool.Id != C.BrandenburgPool).All(pool => pool.MapContextId == C.Center));
            CollectionAssert.AreEquivalent(new[] { C.LabAlarmPool, C.WarehouseAlarmPool },
                pools.Where(pool => pool.Mode == C.Scripted).Select(pool => pool.Id).ToArray());

            // The seed's level 36 Thrax sleep.
            var seed = world.Set<SpawnPoolEntry>().AsNoTracking().Where(pool => C.SeedPools.Contains(pool.Id)).ToList();
            Assert.AreEqual(C.SeedPools.Length, seed.Count);
            Assert.IsTrue(seed.All(pool => pool.Mode == C.Scripted));

            // The Bane at the squad's levels, and each pool of them one of ours.
            var hostile = world.Set<CreatureEntry>().AsNoTracking().Where(c => C.Hostiles.Contains(c.Id)).ToList();
            Assert.AreEqual(C.Hostiles.Length, hostile.Count);
            Assert.IsTrue(hostile.All(c => c.Level is >= 28 and <= 30), string.Join(", ", hostile.Select(c => $"{c.Id}:{c.Level}")));
            Assert.AreEqual(9380U, hostile.Single(c => c.Id == C.Casso).NameId);
            Assert.AreEqual(9516U, hostile.Single(c => c.Id == C.Xirnon).NameId);
            Assert.AreEqual(9515U, hostile.Single(c => c.Id == C.Zixno).NameId);
            Assert.AreEqual(9376U, hostile.Single(c => c.Id == C.GeneratorGuardian).NameId);
            foreach (var pool in pools.Where(pool => pool.Id >= 710100))
                Assert.IsTrue(C.Hostiles.Contains(pool.Creature1Id), $"pool {pool.Id}");

            // The reward choices are the level 30 gear they are.
            var rewards = world.Set<MissionRewardItemEntry>().AsNoTracking().Where(item => item.ContentRevision == C.Revision).ToList();
            CollectionAssert.AreEquivalent(C.StrongestLinkChoices,
                rewards.Where(item => item.MissionId == C.TheStrongestLink).Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(rewards.Any(item => item.MissionId == C.IncursionInvestigation));

            // Each marker is the client's own name for it, or none.
            var markers = world.Set<MissionIndicatorEntry>().AsNoTracking().Where(row => row.ContentRevision == C.Revision).ToList();
            var named = new[] { C.GeneratorMarker, C.ResearchCenterMarker, C.ConsoleMarker, C.EngelsenMarker, C.TanksMarker, C.CassoMarker, C.SecurityMarker };
            Assert.IsTrue(markers.All(row => named.Contains(row.IndicatorId) || row.IndicatorId > MissionIndicator.UnnamedFrom),
                string.Join(", ", markers.Select(row => row.IndicatorId)));
            Assert.AreEqual(1, markers.Count(row => row.IndicatorId > MissionIndicator.UnnamedFrom), "Incursion Investigation's, on the Mires' door");
        }

        [TestMethod]
        public void BrandenburgSendsYouToWolfeAndWolfeSendsYouToTheReactors()
        {
            using var harness = Create();
            harness.SpawnWorld(C.WolfePool);
            var wolfe = RequireNpc(harness, C.WolfePool, C.Wolfe);

            // Brandenburg is outside in the Mires: here, beside the way in.
            var brandenburg = Put(harness, C.Brandenburg, C.Arrival + new Vector3(-3, 0, 2));
            Accept(harness, brandenburg, C.IncursionInvestigation);
            Talk(harness, wolfe, C.IncursionInvestigation, C.DetermineFate);
            Reward(harness, wolfe, C.IncursionInvestigation, null, null, 25000, 3100);

            Accept(harness, wolfe, C.AllInTheTiming);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[C.AllInTheTiming].Objectives[C.ShutDownReactors].State);
        }

        [TestMethod]
        public void WolfeGivesTheTimingToWhoeverComesIn()
        {
            using var harness = Create();
            harness.SpawnWorld(C.WolfePool);
            Accept(harness, RequireNpc(harness, C.WolfePool, C.Wolfe), C.AllInTheTiming);
        }

        [TestMethod]
        public void TheReactorsFireDownTheirTubesByTurnsAndThePitsAreSafe()
        {
            using var harness = Create();
            Work(harness);
            var north = new Vector3(240f, 100.54f, C.NorthTubeZ);
            var south = new Vector3(240f, 100.54f, C.SouthTubeZ);

            Assert.IsTrue(EnergyWeaponCenter.InBeam(C.NorthTubeZ, north));
            Assert.IsFalse(EnergyWeaponCenter.InBeam(C.NorthTubeZ, new Vector3(248f, 97.58f, -24f)), "down in the pit at its east end");
            Assert.IsFalse(EnergyWeaponCenter.InBeam(C.NorthTubeZ, new Vector3(232f, 97.5f, -32f)), "in a side room");
            Assert.IsFalse(EnergyWeaponCenter.InBeam(C.NorthTubeZ, south));

            // By turns: the north charges and fires while the south is quiet, and the other way round.
            for (long t = 0; t < EnergyWeaponCenter.CycleMs; t += 500)
            {
                var n = EnergyWeaponCenter.PhaseOf(t, EnergyWeaponCenter.NorthChargesAt);
                var s = EnergyWeaponCenter.PhaseOf(t, EnergyWeaponCenter.SouthChargesAt);
                Assert.IsFalse(n != EnergyWeaponCenter.Phase.Idle && s != EnergyWeaponCenter.Phase.Idle, $"both at {t}");
            }

            // In the south tube as the north fires: nothing.
            harness.MoveTo(south);
            var health = Health(harness);
            Run(harness, EnergyWeaponCenter.ChargeMs + EnergyWeaponCenter.FireMs);
            Assert.AreEqual(health, Health(harness));
            var light = PlacedUsables.On(harness.Map).Single(live => live.Placement.ClassId == (EntityClasses)C.AlarmLightClass &&
                Vector3.Distance(live.Object.Position, C.SouthReactor) < 1);
            Assert.AreEqual(UseObjectState.TsState0, light.State);

            // The south charges, its light on; then it fires on whoever is in its beam.
            Run(harness, EnergyWeaponCenter.SouthChargesAt - EnergyWeaponCenter.ChargeMs - EnergyWeaponCenter.FireMs + 200);
            Assert.AreEqual(UseObjectState.TsState1, light.State);
            Assert.AreEqual(health, Health(harness), "a warning first");
            Run(harness, EnergyWeaponCenter.ChargeMs);
            Assert.IsTrue(Health(harness) < health || harness.Client.Player.State == CharacterState.Dead, "the beam");
        }

        [TestMethod]
        public void InAPitTheBeamGoesOver()
        {
            using var harness = Create();
            Work(harness);
            harness.MoveTo(new Vector3(248f, 97.58f, C.NorthTubeZ));
            Assert.IsTrue(harness.Client.Player.Position.Y < EnergyWeaponCenter.BeamFloor, $"{harness.Client.Player.Position}");
            var health = Health(harness);
            Run(harness, EnergyWeaponCenter.CycleMs);
            Assert.AreEqual(health, Health(harness));
        }

        [TestMethod]
        public void TheGeneratorTakesLightningHardestAndItsFallOpensTheLab()
        {
            using var harness = Create();
            harness.SpawnWorld(C.WolfePool);
            Accept(harness, RequireNpc(harness, C.WolfePool, C.Wolfe), C.AllInTheTiming);
            Work(harness);
            var log = harness.Client.Player.Missions[C.AllInTheTiming];

            // The three fields stop players, wherever across their ways, and shots do nothing.
            var tunnel = FieldAt(harness, C.TunnelField);
            var lab = FieldAt(harness, C.LabField);
            var command = FieldAt(harness, C.CommandField);
            var yard = FieldAt(harness, C.YardField);
            foreach (var field in new[] { tunnel, lab, command, yard })
            {
                Assert.IsTrue(ForceFields.StopsPlayer(field));
                Assert.IsFalse(ForceFields.MayShoot(field, harness.Client.Player));
            }
            var minor = ForceFields.ClassOf(C.FieldClass.ToString());
            for (var across = -3f; across <= 3f; across += 1f)
            {
                Assert.IsTrue(ForceFields.Crosses(minor, C.TunnelField, C.TunnelFieldYaw, new Vector3(276, 98.7f, across), new Vector3(268, 98.7f, across)), $"tunnel {across}");
                Assert.IsTrue(ForceFields.Crosses(minor, C.LabField, C.LabFieldYaw, new Vector3(132, 106.1f, -48 + across), new Vector3(124, 106.1f, -48 + across)), $"lab {across}");
                Assert.IsTrue(ForceFields.Crosses(minor, C.CommandField, C.CommandFieldYaw, new Vector3(296 + across, 98.7f, 36), new Vector3(296 + across, 98.7f, 44)), $"command {across}");
                Assert.IsTrue(ForceFields.Crosses(minor, C.YardField, C.YardFieldYaw, new Vector3(50, 98.8f, 88 + across), new Vector3(42, 98.8f, 88 + across)), $"yard {across}");
            }

            // Lightning three times as hard; the rest as it is.
            var generator = PlacedDestructibles.On(harness.Map).Single(live => (uint)live.Placement.ClassId == C.GeneratorClass);
            Assert.AreEqual(C.GeneratorHitPoints, generator.HitPoints);
            Assert.AreEqual(300, EnergyWeaponCenter.Struck(harness.Map, generator.Object, 100, DamageType.Electrical));
            Assert.AreEqual(100, EnergyWeaponCenter.Struck(harness.Map, generator.Object, 100, DamageType.Fire));
            Assert.AreEqual(100, EnergyWeaponCenter.Struck(harness.Map, tunnel.Object, 100, DamageType.Electrical));

            Assert.IsTrue(PlacedDestructibles.Destroy(generator, harness.Client.Player));
            Work(harness);
            Assert.IsTrue(EnergyWeaponCenter.ReactorsDown(harness.Map));
            Assert.IsFalse(ForceFields.StopsPlayer(tunnel));
            Assert.IsFalse(ForceFields.StopsPlayer(lab));
            Assert.IsTrue(ForceFields.StopsPlayer(command), "the command chamber's stays");
            Assert.IsTrue(ForceFields.StopsPlayer(yard), "and the yard's");
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.ShutDownReactors].State);
            Assert.IsTrue(log.Completeable);

            // The reactors are off: their beams are gone.
            harness.MoveTo(new Vector3(240f, 100.54f, C.NorthTubeZ));
            var health = Health(harness);
            Run(harness, EnergyWeaponCenter.CycleMs);
            Assert.AreEqual(health, Health(harness));

            // Radioed in, and HQ has Tesla Inversion.
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, C.AllInTheTiming, 0, null), "radioed in");
            harness.Drain();
            Assert.AreEqual(1L, Held(harness, C.TimingChoices[0]));
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, C.TeslaInversion), "Tesla Inversion by radio");

            // Long after, all of it is as it was left.
            _now += 3_600_000;
            PlacedDestructibles.Worker(harness.Map);
            Work(harness);
            Assert.IsTrue(generator.IsDown);
            Assert.IsFalse(ForceFields.StopsPlayer(tunnel));
        }

        [TestMethod]
        public void ASquadThatHasShutTheReactorsDownBeforeFindsTheWayOpen()
        {
            using var harness = Create();
            Completed(harness, C.AllInTheTiming);
            Work(harness);
            Assert.IsTrue(EnergyWeaponCenter.ReactorsDown(harness.Map));
            Assert.IsFalse(ForceFields.StopsPlayer(FieldAt(harness, C.TunnelField)));
            Assert.IsTrue(ForceFields.StopsPlayer(FieldAt(harness, C.CommandField)));
        }

        [TestMethod]
        public void TheDatabladeTurnsTheCoilsOnTheirMakers()
        {
            using var harness = Create();
            Work(harness);
            var station = Object(harness, C.DispenserClass);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, EnergyWeaponCenter.Refusal(harness.Client, station), "for Tesla Inversion");

            StartByRadio(harness, C.TeslaInversion, C.AllInTheTiming);
            var log = harness.Client.Player.Missions[C.TeslaInversion];
            var console = Object(harness, C.ConsoleClass);
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, EnergyWeaponCenter.Refusal(harness.Client, console));

            Use(harness, station);
            Assert.AreEqual(1L, Held(harness, C.DatabladeTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.FindDatablade].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[C.TriggerInversion].State);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, EnergyWeaponCenter.Refusal(harness.Client, station), "one at a time");

            // The test range's Thrax and Stalker, out of reach beyond the window.
            harness.SpawnWorld(C.YardStalkerPool, 710602, 710603);
            var stalker = Living(harness, C.Stalker).Single();
            var yard = Living(harness, C.Thrax).Concat(Living(harness, C.Technician))
                .Where(bane => Vector3.Distance(bane.Position, C.Yard) < 20).ToList();
            Assert.IsTrue(yard.Count > 0);
            Assert.IsTrue(ForceFields.StopsPlayer(FieldAt(harness, C.YardField)), "nobody reaches the yard");

            var coils = PlacedDestructibles.On(harness.Map).Where(live => (uint)live.Placement.ClassId == C.CoilClass).ToList();
            Assert.AreEqual(C.Coils.Length, coils.Count);
            var stalkerHealth = stalker.Attributes[Attributes.Health].Current;

            Use(harness, console);
            Work(harness);
            Assert.IsTrue(EnergyWeaponCenter.Inverted(harness.Map));
            Assert.IsTrue(stalker.Attributes[Attributes.Health].Current < stalkerHealth,
                $"zapped: {stalker.Attributes[Attributes.Health].Current} of {stalkerHealth}, at {stalker.Position}, coils at " +
                string.Join(" ", coils.Select(coil => coil.Object.Position)));
            Assert.AreEqual(0L, Held(harness, C.DatabladeTemplate), "the console takes it");
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.TriggerInversion].State);
            Assert.IsTrue(log.Completeable);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, EnergyWeaponCenter.Refusal(harness.Client, console), "once");
            Assert.IsTrue(harness.Map.SpawnPools.Single(pool => pool.DbId == C.LabAlarmPool).Woken, "the alarm brings the guards");

            // The coils zap their makers until they burn out.
            for (var i = 0; i < 20; i++)
                Work(harness, EnergyWeaponCenter.ZapEveryMs);
            Assert.AreEqual(CharacterState.Dead, stalker.State);
            Assert.IsTrue(yard.All(thrax => thrax.State == CharacterState.Dead));
            Assert.IsTrue(Health(harness) > 0, "the player is not zapped");

            Work(harness, EnergyWeaponCenter.InversionMs);
            Assert.IsTrue(coils.All(coil => coil.IsDown), "burnt out");

            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, C.TeslaInversion, 1, null), "radioed in");
            harness.Drain();
            Assert.AreEqual(1L, Held(harness, C.InversionChoices[1]));
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, C.LookBeforeYouSabotage), "Look Before You Sabotage by radio");
        }

        [TestMethod]
        public void TheMonitorsShowEngelsenAndHisChargesBlowTheTanks()
        {
            using var harness = Create();
            Completed(harness, C.AllInTheTiming);
            StartByRadio(harness, C.LookBeforeYouSabotage, C.TeslaInversion);
            var log = harness.Client.Player.Missions[C.LookBeforeYouSabotage];

            var monitors = harness.Map.DynamicObjects.Where(obj => C.MonitorClasses.Contains((uint)obj.EntityClassId)).ToList();
            Assert.AreEqual(3, monitors.Count);
            Use(harness, monitors[1]);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.AccessNetwork].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[C.RetrieveCharges].State);

            var engelsen = Object(harness, C.CorpseClass);
            Use(harness, engelsen);
            Work(harness);
            Assert.AreEqual((long)C.Tanks, Held(harness, C.ChargeTemplate));
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.RetrieveCharges].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[C.PlaceCharges].State);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, EnergyWeaponCenter.Refusal(harness.Client, engelsen), "three are enough");

            var charges = PlacedUsables.On(harness.Map).Where(live => live.Placement.ClassId == (EntityClasses)C.BombClass)
                .OrderBy(live => live.Object.Position.X).ToList();
            Assert.AreEqual((int)C.Tanks, charges.Count);

            for (var i = 0; i < C.Tanks; i++)
            {
                var charge = charges[i];
                Use(harness, charge.Object);
                Assert.AreEqual(UseObjectState.BombStateArmed, charge.State);
                Assert.AreEqual((long)(C.Tanks - i - 1), Held(harness, C.ChargeTemplate));
                Work(harness);
                Assert.AreEqual((uint)(i + 1), log.Objectives[C.PlaceCharges].Counters[0]);

                // Set, it stays set; well away, its fuse burns down and the tank goes with it.
                Use(harness, charge.Object);
                Assert.AreEqual(UseObjectState.BombStateArmed, charge.State, "a charge cannot be taken out again");
                harness.MoveTo(C.Engelsen);
                _now += EnergyWeaponCenter.FuseSeconds * 1000L;
                PlacedUsables.Worker(harness.Map);
                Work(harness);
                Assert.AreEqual(UseObjectState.BombStateDetonated, charge.State);
                Assert.AreEqual(i + 1, EnergyWeaponCenter.TanksBlown(harness.Map));
            }

            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.PlaceCharges].State);
            Assert.IsTrue(log.Completeable);
            Assert.IsTrue(harness.Map.SpawnPools.Single(pool => pool.DbId == C.WarehouseAlarmPool).Woken, "the blast brings the guards");

            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, C.LookBeforeYouSabotage, 2, null), "radioed in");
            harness.Drain();
            Assert.AreEqual(1L, Held(harness, C.SabotageChoices[2]));
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, C.TheStrongestLink), "The Strongest Link by radio");
        }

        [TestMethod]
        public void CassoTakesNoHarmWhileHisMinionsStand()
        {
            using var harness = Create();
            Completed(harness, C.AllInTheTiming);
            Completed(harness, C.TeslaInversion);
            StartByRadio(harness, C.TheStrongestLink, C.LookBeforeYouSabotage);
            var log = harness.Client.Player.Missions[C.TheStrongestLink];
            var command = FieldAt(harness, C.CommandField);

            harness.SpawnWorld(new[] { C.CassoPool }.Concat(C.MinionPools).ToArray());
            Work(harness);
            var casso = EnergyWeaponCenter.CassoOf(harness.Map);
            Assert.IsNotNull(casso);
            Assert.IsTrue(EnergyWeaponCenter.CassoLinked(harness.Map));

            harness.MoveTo(casso.Position + new Vector3(4, 0, 0));
            var health = casso.Attributes[Attributes.Health].Current;
            ActorManager.Instance.Damage(harness.Map, casso, 500, harness.Client.Player);
            harness.Drain();
            Assert.AreEqual(health, casso.Attributes[Attributes.Health].Current, "his link takes it");

            var minions = Living(harness, C.Linker).Concat(Living(harness, C.Caretaker)).ToList();
            Assert.AreEqual(C.MinionPools.Length, minions.Count);
            foreach (var minion in minions.Take(minions.Count - 1))
                Kill(harness, minion);
            Work(harness);
            Assert.IsTrue(EnergyWeaponCenter.CassoLinked(harness.Map), "while one stands");

            Kill(harness, minions.Last());
            Work(harness);
            Assert.IsFalse(EnergyWeaponCenter.CassoLinked(harness.Map));
            Assert.IsTrue(ForceFields.StopsPlayer(command));

            Kill(harness, casso);
            Work(harness);
            Assert.AreEqual(CharacterState.Dead, casso.State);
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[C.DefeatCasso].State);
            Assert.IsFalse(ForceFields.StopsPlayer(command), "the way back to Wolfe");
            Assert.IsTrue(ForceFields.StopsPlayer(FieldAt(harness, C.YardField)), "the yard's never fails");

            harness.SpawnWorld(C.WolfePool);
            Reward(harness, RequireNpc(harness, C.WolfePool, C.Wolfe), C.TheStrongestLink, 3, C.StrongestLinkChoices[3], 100000, 6600);
        }

        [TestMethod]
        public void ALateComerIsGivenWhatTheCopyHasDone()
        {
            using var harness = Create();
            Work(harness);
            var generator = PlacedDestructibles.On(harness.Map).Single(live => (uint)live.Placement.ClassId == C.GeneratorClass);
            PlacedDestructibles.Destroy(generator, harness.Client.Player);
            Work(harness);

            harness.SpawnWorld(C.WolfePool);
            Accept(harness, RequireNpc(harness, C.WolfePool, C.Wolfe), C.AllInTheTiming);
            Work(harness);
            Assert.IsTrue(harness.Client.Player.Missions[C.AllInTheTiming].Completeable);
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

            foreach (var spot in C.Monitors.Concat(C.TankSpots).Concat(C.ChargeSpots).Concat(C.Coils)
                         .Append(C.Generator).Append(C.Datablade).Append(C.Console).Append(C.Engelsen).Append(C.WolfeSpot)
                         .Append(C.LabCentre).Append(C.WarehouseCentre).Append(C.CommandChamber).Append(C.Yard))
            {
                var ground = harness.Map.NavMesh.GroundHeight(spot);
                Assert.IsNotNull(ground, $"{spot}");
                Assert.IsTrue(Math.Abs(ground.Value - spot.Y) < 1.5, $"{spot}, ground {ground}");
            }

            foreach (var pool in C.Pools.Concat(C.AlarmPools).Where(pool => pool.Map == C.Center))
            {
                var spot = new Vector3((float)pool.X, (float)pool.Y, (float)pool.Z);
                var ground = harness.Map.NavMesh.GroundHeight(spot);
                Assert.IsNotNull(ground, $"pool {pool.Id}");
                Assert.IsTrue(Math.Abs(ground.Value - spot.Y) < 1.5, $"pool {pool.Id} at {spot}, ground {ground}");
            }

            // The way in reaches every room; the yard is behind its field, at the one way to it.
            foreach (var room in new[] { C.LabCentre, C.Datablade, C.Console, C.Monitors[1], C.WarehouseCentre, C.Engelsen, C.CommandChamber, C.Generator })
                Assert.IsTrue(harness.Map.NavMesh.Connected(C.Arrival, room) ?? false, $"{room}");
            var way = harness.Map.NavMesh.FindPath(C.Arrival, C.Yard, out var complete);
            Assert.IsTrue(complete);
            var minor = ForceFields.ClassOf(C.FieldClass.ToString());
            Assert.IsTrue(way.Zip(way.Skip(1)).Any(step => ForceFields.Crosses(minor, C.YardField, C.YardFieldYaw,
                step.First + new Vector3(0, 1, 0), step.Second + new Vector3(0, 1, 0))), "the field is across the way to the yard");
        }

        [TestMethod]
        public void DownTakesItAllOutAndUpAgainChangesNothing()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            const string seedAwake = "SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN (531435, 531436, 531437, 531438) AND mode = 0";

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 710000 AND 710999"));
            Assert.AreEqual(4, Count(database, seedAwake));

            migrator.Migrate(Migration);
            Assert.AreEqual(C.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{C.Revision}' AND enabled = 1"));
            var rows = Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 710000 AND 710999");
            Assert.AreEqual(C.Creatures.Length, rows);
            Assert.AreEqual(C.Actions.Length, Count(database, "SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN 710000 AND 710999"));
            Assert.AreEqual(C.Pools.Length + C.AlarmPools.Length, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 710000 AND 710999"));
            Assert.AreEqual(0, Count(database, seedAwake));

            foreach (var sql in C.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(rows, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 710000 AND 710999"), "guarded");

            migrator.Migrate(Previous);
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 710000 AND 710999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{C.Revision}'"));
            Assert.AreEqual(4, Count(database, seedAwake), "the seed's pools awake again");

            migrator.Migrate(Migration);
            Assert.AreEqual(C.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{C.Revision}' AND enabled = 1"));
        }

        [TestMethod]
        public void OnMySqlItIsTheSameStatements()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in C.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in C.Down())
                StringAssert.Contains(down, sql);
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: C.Center);
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
            EnergyWeaponCenter.Worker(harness.Map);
            harness.Drain();
        }

        /// <summary>The worker run every tenth of a second for so long, as the map's worker does.</summary>
        private void Run(WildernessRuntimeTestHarness harness, long milliseconds)
        {
            for (long done = 0; done < milliseconds; done += 100)
                Work(harness);
        }

        private static int Health(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Attributes[Attributes.Health].Current;

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

        private static DynamicObject Object(WildernessRuntimeTestHarness harness, uint classId) =>
            harness.Map.DynamicObjects.First(obj => (uint)obj.EntityClassId == classId);

        private static ForceFields.Field FieldAt(WildernessRuntimeTestHarness harness, Vector3 at) =>
            PlacedDestructibles.On(harness.Map).Where(live => live.Field != null)
                .OrderBy(live => Vector3.Distance(live.Object.Position, at)).First().Field;

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

        /// <summary>The mission before done and rewarded, and this one taken by radio as HQ offers it.</summary>
        private void StartByRadio(WildernessRuntimeTestHarness harness, uint missionId, uint after)
        {
            Completed(harness, after);
            harness.Manager.OfferAfterTurnIn(harness.Client, after);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, missionId), $"{missionId} by radio");
            harness.Drain();
            Work(harness);
        }

        #endregion
    }
}
