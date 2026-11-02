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
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using M = MagmaCavernsContent;
    using Side = MagmaCaverns.Side;
    using static Wilderness.WildernessTwinPillarsTests;

    /// <summary>
    /// Magma Caverns as the Deployment 8 dev diary tells it (MagmaCavernsContent, MagmaCaverns):
    /// its rows and five missions, Kane's vote and the copy's side, both arcs with their
    /// assaults, bonuses, doors, wormholes, beacons, laptop and research subjects, Creature
    /// Strike, Ember and his hoard, and the side hospitals.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MagmaCavernsTests
    {
        private const string Migration = "20271013000000_Magma_caverns", Previous = "20271012000000_Timora_mines";

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            MagmaCaverns.Reset();
            MagmaCaverns.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            MagmaCaverns.Reset();
            MagmaCaverns.Now = () => Environment.TickCount64;
        }

        [TestMethod]
        public void TheCavernsHaveTheirPeopleBothSidesAndFiveMissions()
        {
            using var harness = Create();
            var world = harness.World;

            foreach (var missionId in M.Missions)
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out _), $"mission {missionId}");

            var definitions = world.MissionContentDefinitionEntries.AsNoTracking().Where(row => row.ContentRevision == M.Revision).ToList();
            Assert.AreEqual(M.Missions.Length, definitions.Count(row => row.Enabled));
            Assert.IsTrue(definitions.All(row => row.CategoryId == M.Category));
            foreach (var radioed in new[] { M.Consensus, M.FinalChance, M.Executioner, M.LavaBeast })
                Assert.IsTrue(definitions.Single(row => row.MissionId == radioed).RadioCompleteable, $"{radioed}");
            Assert.AreEqual(1, world.Set<MissionRepeatPolicyEntry>().AsNoTracking().Count(row => row.ContentRevision == M.Revision &&
                row.MissionId == M.Consensus), "Consensus is repeatable");

            var packages = world.Set<NpcPackageEntry>().AsNoTracking().ToDictionary(row => row.Id, row => row.PackageId);
            foreach (var (creature, package, _) in M.Packages)
                Assert.AreEqual(package, packages[creature], $"{creature}");

            // Every row is the client's name; both sides stand Neutral until a copy has a side.
            var rows = world.Set<CreatureEntry>().AsNoTracking().Where(c => c.Id >= 678000 && c.Id < 679000).ToList();
            Assert.AreEqual(M.Creatures.Length, rows.Count);
            foreach (var id in M.RebelRows.Concat(M.PenumbraRows).Where(id => id != M.Turret))
                Assert.AreEqual((uint)M.TargetCategoryRow.Neutral, rows.Single(c => c.Id == id).Faction, $"{id}");
            Assert.AreEqual(10472U, rows.Single(c => c.Id == M.Kane).NameId);
            Assert.AreEqual(10491U, rows.Single(c => c.Id == M.Ember).NameId);
            Assert.IsTrue(world.Set<CreatureAppearanceEntry>().Any(a => a.Id == M.Sudevi && a.SlotId == 15));

            // Every pool of ours, and every Flaregasher's new place, stands on the navmesh within reach of the entrance.
            var nav = harness.Map.NavMesh;
            var pools = world.Set<SpawnPoolEntry>().AsNoTracking().Where(pool => pool.MapContextId == M.MagmaCaverns).ToList();
            Assert.AreEqual(M.Pools.Length, pools.Count(pool => pool.Id is >= 678200 and <= 678299));
            foreach (var pool in pools.Where(pool => pool.Id is >= 678200 and <= 678299 || pool.Id is >= 540128 and <= 540134))
                Reachable(harness, new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ), $"pool {pool.Id}");
            foreach (var (pool, _, to) in M.FlaregasherMoves)
            {
                var row = pools.Single(p => p.Id == pool);
                Assert.IsTrue(Vector3.Distance(to, new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ)) < 0.1f, $"pool {pool}");
            }

            // And every place the story goes.
            foreach (var spot in new[]
                     {
                         M.KaneSpot, M.StationTeleporter, M.StationArrival, M.BackDoor, M.Camp, M.SalyardSpot, M.Laptop, M.SudeviSpot,
                         M.TunnelOasisEnd, M.TunnelCampEnd, M.Sakra, M.OasisNorth, M.WestDoorWay, M.EastDoorWay, M.NelbySpot,
                         M.GroundsWormhole, M.GroundsSetup, M.EastCorridor, M.LandingZone1, M.LandingZone2, M.DamaSpot, M.MezenSpot,
                         M.RebelWormhole, M.RebelApproach, M.CauldronRim, M.CauldronSetup, M.Lair, M.Treasure
                     })
                Reachable(harness, spot, $"{spot}");

            // Each assault's route is walkable, stage to stage.
            foreach (var assault in MagmaCaverns.Assaults)
            {
                var from = assault.Wormhole;
                foreach (var stage in assault.Route)
                {
                    var path = nav.FindPath(from, stage.At, out var complete);
                    Assert.IsTrue(complete && path.Count > 0 && Vector3.Distance(path[^1], stage.At) < 1.5f, $"{assault.Name}: {from} to {stage.At}");
                    from = stage.At;
                }
            }

            // Each arc's three choices, and the bonuses the client names.
            var rewards = world.Set<MissionRewardItemEntry>().AsNoTracking().Where(item => item.ContentRevision == M.Revision).ToList();
            CollectionAssert.AreEquivalent(new[] { M.LaserMachineGun, M.MotorAssistVest, M.GravitonHelmet },
                rewards.Where(item => item.MissionId == M.FinalChance).Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEquivalent(new[] { M.FireGrenadeLauncher, M.PhysicalLeechGun, M.MotorAssistHelmet },
                rewards.Where(item => item.MissionId == M.Executioner).Select(item => item.ItemTemplateId).ToArray());

            var markers = world.Set<MissionIndicatorEntry>().AsNoTracking().Where(row => row.ContentRevision == M.Revision).ToList();
            Assert.IsTrue(markers.All(row => row.IndicatorId > MissionIndicator.UnnamedFrom), "no marker carries a name of the client's");
        }

        [TestMethod]
        public void TheMajorityDecidesATieTheFirstToVote()
        {
            var votes = new Dictionary<uint, Side> { [1] = Side.Penumbra, [2] = Side.Rebel, [3] = Side.Rebel };
            Assert.AreEqual(Side.Rebel, MagmaCaverns.Tally(votes, new uint[] { 1, 2, 3 }));

            votes = new Dictionary<uint, Side> { [1] = Side.Penumbra, [2] = Side.Rebel };
            Assert.AreEqual(Side.Penumbra, MagmaCaverns.Tally(votes, new uint[] { 1, 2 }), "the first to vote");
            Assert.AreEqual(Side.Rebel, MagmaCaverns.Tally(votes, new uint[] { 2, 1 }));
            Assert.AreEqual(Side.Rebel, MagmaCaverns.Tally(new Dictionary<uint, Side> { [7] = Side.Rebel }, new uint[] { 7 }), "alone");

            Assert.AreEqual(TargetCategory.Neutral, MagmaCaverns.Standing(Side.None, Side.Rebel));
            Assert.AreEqual(TargetCategory.Friendly, MagmaCaverns.Standing(Side.Rebel, Side.Rebel));
            Assert.AreEqual(TargetCategory.Hostile, MagmaCaverns.Standing(Side.Rebel, Side.Penumbra));
        }

        [TestMethod]
        public void TheCopysSideMakesFriendsAndEnemiesOfThePeople()
        {
            using var harness = Create();
            harness.SpawnWorld(M.KanePool, M.SudeviPool, M.MezenPool, 678212, 678220);
            Work(harness);

            var sudevi = One(harness, M.Sudevi);
            var mezen = One(harness, M.Mezen);
            Assert.AreEqual(TargetCategory.Neutral, sudevi.TargetCategory);
            Assert.AreEqual(TargetCategory.Neutral, mezen.TargetCategory);
            Assert.IsTrue(sudevi.ImmuneToAllDamage && mezen.ImmuneToAllDamage, "an undecided copy fights no one");
            Assert.AreEqual(TargetCategory.Friendly, One(harness, M.Kane).TargetCategory);

            MagmaCaverns.SetSide(harness.Map, Side.Rebel);
            Work(harness);
            Assert.AreEqual(TargetCategory.Friendly, sudevi.TargetCategory);
            Assert.IsTrue(sudevi.ImmuneToAllDamage);
            Assert.AreEqual(TargetCategory.Hostile, mezen.TargetCategory);
            Assert.IsFalse(mezen.ImmuneToAllDamage);
            Assert.IsTrue(Living(harness, M.Defender).All(c => c.TargetCategory == TargetCategory.Friendly));
            Assert.IsTrue(Living(harness, M.BlackOps).All(c => c.TargetCategory == TargetCategory.Hostile));
            Assert.IsTrue(One(harness, M.Kane).ImmuneToAllDamage);

            // The hospitals: the camp's for a Rebel copy, not the landing zone's.
            var hospitals = Hospitals.OnMap(M.MagmaCaverns);
            Assert.AreEqual(M.RebelHospital, MagmaCaverns.SideHospital(harness.Map).TeleporterId);
            Assert.IsFalse(MagmaCaverns.Allows(harness.Map, hospitals.Single(h => h.TeleporterId == M.PenumbraHospital)));
            Assert.IsTrue(MagmaCaverns.Allows(harness.Map, hospitals.Single(h => h.TeleporterId == M.RebelHospital)));
            Assert.IsTrue(MagmaCaverns.Allows(harness.Map, new Hospitals.Hospital { TeleporterId = M.EntranceHospital, MapContextId = M.MagmaCaverns }));
            Assert.IsFalse(Hospitals.AvailableTo(harness.Client.Player, M.MagmaCaverns).Any(h => h.TeleporterId == M.PenumbraHospital));
        }

        [TestMethod]
        public void KaneAsksAndTheRebelsTakeTheTeleporter()
        {
            using var harness = Create();
            Sided(harness, Side.Rebel);

            Assert.AreEqual(Side.Rebel, MagmaCaverns.SideOf(harness.Map));
            Assert.AreEqual(M.RebelSide, harness.Client.Player.PlayerFlags[M.SideFlag]);

            // Final Chance by radio, once Consensus is in; not Executioner.
            harness.Manager.OfferAfterTurnIn(harness.Client, M.Consensus);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, M.Executioner), "not Penumbra's");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, M.FinalChance), "the Rebels' arc");
            harness.Drain();

            // Consensus again: the copy has decided, whatever the vote.
            harness.SpawnWorld(M.KanePool);
            var kane = RequireNpc(harness, M.KanePool, M.Kane);
            Accept(harness, kane, M.Consensus);
            Choose(harness, kane, 1);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Consensus, M.WaitForSquad));
            Assert.AreEqual(M.RebelSide, harness.Client.Player.PlayerFlags[M.SideFlag], "the copy's side, not the vote");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Consensus, M.TakeTeleporter));
        }

        [TestMethod]
        public void PenumbraGoOutTheBackDoorAndTheTeleporterIsShut()
        {
            using var harness = Create();
            Sided(harness, Side.Penumbra);

            Assert.AreEqual(Side.Penumbra, MagmaCaverns.SideOf(harness.Map));
            Assert.AreEqual(M.PenumbraSide, harness.Client.Player.PlayerFlags[M.SideFlag]);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable,
                MagmaCaverns.Refusal(harness.Client, Key(harness, "magma:station-teleporter")), "the Rebels shut it");

            harness.Manager.OfferAfterTurnIn(harness.Client, M.Consensus);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, M.FinalChance));
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, M.Executioner));
        }

        [TestMethod]
        public void FinalChanceHoldsTheCampAndTheDoorThenTakesTheLandingZones()
        {
            using var harness = Create();
            Sided(harness, Side.Rebel);
            harness.Manager.OfferAfterTurnIn(harness.Client, M.Consensus);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, M.FinalChance));
            harness.Drain();
            harness.SpawnWorld(M.SalyardPool, M.SudeviPool, M.NelbyPool, M.DamaPool, M.MezenPool);
            Work(harness);

            // The west door wants Sudevi first.
            var west = Key(harness, "magma:west-door");
            Assert.AreEqual(PlayerMessage.PmMagmaRebelDoor1Closed, MagmaCaverns.Refusal(harness.Client, west));

            Talk(harness, RequireNpc(harness, M.SalyardPool, M.Salyard), M.FinalChance, M.FcSalyard);
            Talk(harness, RequireNpc(harness, M.SudeviPool, M.Sudevi), M.FinalChance, M.FcSudevi);
            Work(harness);

            // Penumbra's first assault: its wormhole open, a wave through, Hostile.
            Assert.IsTrue(MagmaCaverns.AssaultOf(harness.Map, 0).Started);
            var wave = MagmaCaverns.AssaultSoldiers(harness.Map, 0);
            Assert.AreEqual(MagmaCaverns.WaveSize, wave.Count);
            Assert.IsTrue(wave.All(soldier => soldier.TargetCategory == TargetCategory.Hostile));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, MagmaCaverns.Refusal(harness.Client, west), "not before the assault is broken");

            Use(harness, Key(harness, "magma:assault-0"));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FinalChance, M.FcShutWormhole1));
            Assert.IsFalse(MagmaCaverns.AssaultOf(harness.Map, 0).Over, "its soldiers are still about");

            foreach (var soldier in MagmaCaverns.AssaultSoldiers(harness.Map, 0))
                Kill(harness, soldier);
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            Work(harness);
            Assert.IsTrue(MagmaCaverns.AssaultOf(harness.Map, 0).Over);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FinalChance, M.FcHoldCamp));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FinalChance, M.FcBonus1), "none reached the camp");
            Assert.AreEqual(credits + M.BonusCredits, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsTrue(MagmaCaverns.DoorOpen(harness.Map, MagmaCaverns.WestDoor), "the way to the Grounds");

            // The second: one of them gets to the west door, and the bonus is lost.
            Assert.AreEqual(PlayerMessage.PmMagmaRebelDoor2Closed, MagmaCaverns.Refusal(harness.Client, Key(harness, "magma:east-door")));
            Talk(harness, RequireNpc(harness, M.NelbyPool, M.Nelby), M.FinalChance, M.FcNelby);
            Work(harness);
            var second = MagmaCaverns.AssaultSoldiers(harness.Map, 1);
            Assert.AreEqual(MagmaCaverns.WaveSize, second.Count);
            BehaviorManager.Instance.RestoreScriptedPose(harness.Map, second[0], M.WestDoorWay, 0);
            Work(harness);
            Assert.AreEqual(1, MagmaCaverns.AssaultOf(harness.Map, 1).Breach);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, M.FinalChance, M.FcBonus2));
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[M.FinalChance].State, "a bonus is no more than that");

            Use(harness, Key(harness, "magma:assault-1"));
            foreach (var soldier in MagmaCaverns.AssaultSoldiers(harness.Map, 1))
                Kill(harness, soldier);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FinalChance, M.FcShutWormhole2));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FinalChance, M.FcHoldDoor));
            Assert.IsTrue(MagmaCaverns.DoorOpen(harness.Map, MagmaCaverns.EastDoor));

            // The Rebels' own wormhole: set up, then a way to the Cauldron.
            var setup = Key(harness, "magma:grounds-wormhole");
            Use(harness, setup);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FinalChance, M.FcRebelWormhole));
            Use(harness, setup);
            Arrive(harness, M.RebelWormhole);

            // The landing zones, the beacons, Dama and Mezen.
            harness.MoveTo(M.LandingZone2);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FinalChance, M.FcInfiltrate));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, MagmaCaverns.Refusal(harness.Client, Key(harness, "magma:beacon-2")), "the first first");
            Use(harness, Key(harness, "magma:beacon-1"));
            Work(harness);
            Use(harness, Key(harness, "magma:beacon-2"));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.FinalChance, M.FcBeacon2));

            var dama = RequireNpc(harness, M.DamaPool, M.Dama);
            var mezen = RequireNpc(harness, M.MezenPool, M.Mezen);
            Assert.IsFalse(dama.ImmuneToAllDamage || mezen.ImmuneToAllDamage, "the enemy's officers");
            Kill(harness, dama);
            Kill(harness, mezen);
            Work(harness);
            Assert.IsTrue(harness.Client.Player.Missions[M.FinalChance].Completeable);
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, M.FinalChance, 0, null), "radioed in");
            harness.Drain();
            Assert.AreEqual(1L, Held(harness, M.LaserMachineGun));
        }

        [TestMethod]
        public void ExecutionerBreaksTheRebelsAndPutsAnEndToTheirResearch()
        {
            using var harness = Create();
            Sided(harness, Side.Penumbra);
            harness.Manager.OfferAfterTurnIn(harness.Client, M.Consensus);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, M.Executioner));
            harness.Drain();
            harness.SpawnWorld(M.MezenPool, M.DamaPool, M.NelbyPool, M.SudeviPool, M.SalyardPool, M.SubjectsPool);
            Work(harness);

            Talk(harness, RequireNpc(harness, M.MezenPool, M.Mezen), M.Executioner, M.ExMezen);
            Talk(harness, RequireNpc(harness, M.DamaPool, M.Dama), M.Executioner, M.ExDama);
            Work(harness);

            // The Rebel assault: one reaches the first landing zone and places its beacon.
            var wave = MagmaCaverns.AssaultSoldiers(harness.Map, 2);
            Assert.AreEqual(MagmaCaverns.WaveSize, wave.Count);
            Assert.IsTrue(wave.All(soldier => soldier.TargetCategory == TargetCategory.Hostile), "the Rebels, in a Penumbra copy");
            BehaviorManager.Instance.RestoreScriptedPose(harness.Map, wave[0], M.LandingZone1, 0);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, M.Executioner, M.ExBonus1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Executioner, M.ExBonus2));

            var experience = harness.Client.Player.Experience;
            Use(harness, Key(harness, "magma:assault-2"));
            foreach (var soldier in MagmaCaverns.AssaultSoldiers(harness.Map, 2))
                Kill(harness, soldier);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExShutWormhole));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExStopAssault));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExBonus2), "the second landing zone held");
            Assert.IsTrue(harness.Client.Player.Experience > experience, "the bonus's experience");

            // The front, the doors and Nelby.
            var east = Key(harness, "magma:east-door");
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, MagmaCaverns.Refusal(harness.Client, east), "no orders yet");
            harness.MoveTo(M.EastCorridor);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExFrontLine));
            Use(harness, east);
            Work(harness);
            Assert.IsTrue(MagmaCaverns.DoorOpen(harness.Map, MagmaCaverns.EastDoor));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExDoor1));

            var nelby = RequireNpc(harness, M.NelbyPool, M.Nelby);
            Assert.AreEqual(TargetCategory.Hostile, nelby.TargetCategory);
            Kill(harness, nelby);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExNelby));
            Use(harness, Key(harness, "magma:west-door"));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExDoor2));

            // The stronghold and Sudevi, the camp and Salyard.
            harness.MoveTo(M.SudeviSpot + new Vector3(3, 0, 0));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExStronghold));
            Kill(harness, RequireNpc(harness, M.SudeviPool, M.Sudevi));
            Work(harness);
            harness.MoveTo(M.Camp);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExCamp));
            Kill(harness, RequireNpc(harness, M.SalyardPool, M.Salyard));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExSalyard));

            // The laptop, the cells, and the subjects loose.
            Use(harness, Key(harness, "magma:laptop"));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExLaptop));
            var subjects = Living(harness, M.PacifiedThrax);
            Assert.AreEqual((int)M.SubjectsWanted, subjects.Count);
            Assert.IsTrue(subjects.All(subject => subject.TargetCategory == TargetCategory.Neutral), "behind the forcefield");
            Use(harness, Key(harness, "magma:cells"));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExRelease));
            Assert.IsTrue(subjects.All(subject => subject.TargetCategory == TargetCategory.Hostile));
            foreach (var subject in subjects)
                Kill(harness, subject);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Executioner, M.ExSubjects));
            Assert.IsTrue(harness.Client.Player.Missions[M.Executioner].Completeable);
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, M.Executioner, 1, null), "radioed in");
            harness.Drain();
            Assert.AreEqual(1L, Held(harness, M.PhysicalLeechGun));
        }

        [TestMethod]
        public void CreatureStrikeSendsTheOasisAtTheDefenses()
        {
            using var harness = Create();
            Sided(harness, Side.Rebel);
            harness.SpawnWorld(M.SudeviPool);
            Work(harness);
            var sudevi = RequireNpc(harness, M.SudeviPool, M.Sudevi);
            Accept(harness, sudevi, M.CreatureStrike);
            Work(harness);

            var strike = MagmaCaverns.StrikeOf(harness.Map);
            Assert.AreEqual(MagmaCaverns.StrikeMantas + MagmaCaverns.StrikeTicks, strike.Count);
            Assert.AreEqual(MagmaCaverns.StrikeMantas, strike.Count(c => c.DbId == M.BeamManta));
            foreach (var creature in strike)
                Kill(harness, creature);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.CreatureStrike, M.StrikeDefeat));
            Reward(harness, sudevi, M.CreatureStrike, null, null, 60000, 6000);
        }

        [TestMethod]
        public void EmbersDeathShowsHisHoard()
        {
            using var harness = Create();
            harness.SpawnWorld(M.KanePool);
            Work(harness);
            Accept(harness, RequireNpc(harness, M.KanePool, M.Kane), M.LavaBeast);

            var ember = MagmaCaverns.EmberOf(harness.Map);
            Assert.IsNotNull(ember);
            Assert.AreEqual(TargetCategory.Hostile, ember.TargetCategory);
            harness.MoveTo(M.Lair + new Vector3(3, 0, 0));
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.LavaBeast, M.FindLair));

            // A Magmonix shrugs off bullets and fire (IMMUNE_PHYSICAL, IMMUNE_FIRE): ice is what hurts him.
            Kill(harness, ember, DamageType.Physical);
            Assert.IsNotNull(MagmaCaverns.EmberOf(harness.Map), "bullets do nothing");
            Kill(harness, ember, DamageType.Ice);
            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.LavaBeast, M.KillEmber));
            Assert.IsTrue(MagmaCaverns.TreasureOut(harness.Map));

            var hoard = Key(harness, "magma:ember-hoard");
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            Use(harness, hoard);
            Assert.AreEqual(credits + MagmaCaverns.TreasureCredits, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, MagmaCaverns.Refusal(harness.Client, hoard), "a share each");

            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, M.LavaBeast, null, null), "radioed in");
        }

        [TestMethod]
        public void TheMigrationGoesUpAndDownAndPutsTheFlaregashersBack()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            Assert.AreEqual(0, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 678000 AND 678999"));
            Assert.AreEqual(M.FlaregasherMoves.Length, 7, "every one of Crucible's");
            Assert.AreEqual(7, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = {M.MagmaCaverns} AND creature_1_Id = {M.Flaregasher} AND id < 678000 AND radius = 15"));

            migrator.Migrate(Migration);
            Assert.AreEqual(M.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{M.Revision}' AND enabled = 1"));
            Assert.AreEqual(M.Creatures.Length, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 678000 AND 678999"));
            Assert.AreEqual(M.Pools.Length, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 678000 AND 678999"));
            Assert.AreEqual(M.FlaregasherMoves.Length, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = {M.MagmaCaverns} AND creature_1_Id = {M.Flaregasher} AND id < 678000 AND radius = 6"));

            foreach (var sql in M.Up().Where(sql => !sql.StartsWith("INSERT INTO mission") && !sql.StartsWith("UPDATE mission")))
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(M.Creatures.Length, Count(database, "SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 678000 AND 678999"), "guarded");

            migrator.Migrate(Previous);
            foreach (var table in new[] { "creature", "creature_action", "creature_stat", "creature_appearance", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN 678000 AND 678999"), table);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{M.Revision}'"));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_repeat_policy WHERE content_revision = '{M.Revision}'"));
            Assert.AreEqual(7, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = {M.MagmaCaverns} AND creature_1_Id = {M.Flaregasher} AND id < 678000 AND radius = 15"));
            Assert.AreEqual(1, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool WHERE id = 540128 AND pos_x = -472"), "back where it was");
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: M.MagmaCaverns);
            try
            {
                Qualify(harness);
                harness.MoveTo(M.Entrance);
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
                new CharacterStartingExperienceEntry(harness.Client.Player.Id, M.Revision,
                    CharacterStartingExperienceState.Skipped)));
            harness.Client.Player.StartingExperienceCompleted = true;
            harness.Drain();
        }

        private static void Reachable(WildernessRuntimeTestHarness harness, Vector3 spot, string what)
        {
            var nav = harness.Map.NavMesh;
            var near = nav.Nearest(spot);
            Assert.IsNotNull(near, what);
            Assert.IsTrue(Vector3.Distance(near.Value, spot) < 3f, $"{what}: nearest {near}");
            var path = nav.FindPath(M.Entrance, near.Value, out var complete);
            Assert.IsTrue(complete && path.Count > 0 && Vector3.Distance(path[^1], near.Value) < 1.5f, $"{what}: from the entrance");
        }

        /// <summary>Consensus taken from Kane, the side chosen, the way out of the station taken, and it radioed in.</summary>
        private void Sided(WildernessRuntimeTestHarness harness, Side side)
        {
            harness.SpawnWorld(M.KanePool);
            Work(harness);
            var kane = RequireNpc(harness, M.KanePool, M.Kane);
            Accept(harness, kane, M.Consensus);
            Choose(harness, kane, side == Side.Penumbra ? 1 : 2);
            Assert.AreEqual(side == Side.Penumbra ? M.PenumbraSide : M.RebelSide, harness.Client.Player.PlayerFlags[M.VoteFlag]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Consensus, M.WaitForSquad));

            Work(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Consensus, M.WaitForSquad));

            if (side == Side.Rebel)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Consensus, M.TakeTeleporter));
                Use(harness, Key(harness, "magma:station-teleporter"));
                Arrive(harness, M.Camp);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Consensus, M.TakeTeleporter));
            }
            else
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.Consensus, M.TakeBackDoor));
                harness.MoveTo(M.BackDoor);
                Work(harness);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Consensus, M.TakeBackDoor));
            }

            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.Consensus, M.Hidden));
            Assert.IsTrue(harness.Client.Player.Missions[M.Consensus].Completeable);
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, M.Consensus, null, null), "Consensus radioed in");
            harness.Drain();
        }

        /// <summary>Kane's choice, as PoolsSnakePitContactsTests.Choose picks Timlin's.</summary>
        private static void Choose(WildernessRuntimeTestHarness harness, Creature npc, int choice)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(npc.Position);
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = npc.EntityId, MissionId = M.Consensus, ObjectiveId = M.SpeakToKane, PlayerFlagId = 1, ChoiceIdx = choice
            });
            harness.Drain();
        }

        /// <summary>On the way to the spot, and there: the trip ended as the client's answer ends it.</summary>
        private static void Arrive(WildernessRuntimeTestHarness harness, Vector3 spot)
        {
            var transfer = harness.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "on the way");
            Assert.IsTrue(Vector3.Distance(transfer.DestinationPosition, spot) < 5, $"to {transfer.DestinationPosition}");
            harness.Client.PendingTransfer = null;
            harness.MoveTo(transfer.DestinationPosition);
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId) =>
            harness.Client.Player.Missions[missionId].Objectives[objectiveId].State;

        private void Work(WildernessRuntimeTestHarness harness, long milliseconds = 100)
        {
            _now += milliseconds;
            MagmaCaverns.Worker(harness.Map);
            harness.Drain();
        }

        private static List<Creature> Living(WildernessRuntimeTestHarness harness, uint dbId) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(c => c.DbId == dbId && c.State != CharacterState.Dead && EntityManager.Instance.Creatures.ContainsKey(c.EntityId)).ToList();

        private static Creature One(WildernessRuntimeTestHarness harness, uint dbId) => Living(harness, dbId).Single();

        /// <summary>A blow of all its health and armour from the harness's player, through the real damage path.</summary>
        private static void Kill(WildernessRuntimeTestHarness harness, Creature creature, DamageType type = DamageType.Physical)
        {
            harness.MoveTo(creature.Position + new Vector3(2, 0, 0));
            var lethal = creature.Attributes[Attributes.Health].Current + Math.Max(0, creature.Attributes[Attributes.Armor].Current);
            ActorManager.Instance.Damage(harness.Map, creature, lethal, harness.Client.Player, type);
            harness.Drain();
        }

        private static DynamicObject Key(WildernessRuntimeTestHarness harness, string key) =>
            harness.Map.DynamicObjects.Single(obj => obj.ScenarioKey == key);

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

        #endregion
    }
}
