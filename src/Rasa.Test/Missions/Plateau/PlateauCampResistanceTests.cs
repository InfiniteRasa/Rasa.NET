extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Packets.Game.Server;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Packets.Protocol;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Wilderness;
using Rasa.Test.World;

namespace Rasa.Test.Missions.Plateau
{
    /// <summary>
    /// Colonel Bosley's missions at Camp Resistance, Plateau, played through as a player plays
    /// them: Giant Killer (939) and Incommunicado (1040).
    ///
    /// Why these tests: PlateauMissionTests only reads the rows the migrations write. Upstream
    /// tests every quest it adds by playing it in the runtime harness - offer, acceptance, each
    /// objective at the right NPC, kills and pickups, turn-in and reward - as
    /// WildernessRanjaGorgeTests, WildernessTwinPillarsTests and WildernessDaghdasUrnTests do. A
    /// change to the mission runtime or to these missions' data that breaks them in game then
    /// fails here first.
    ///
    /// The Wilderness harness migrates the whole World, so it has the Plateau's pools, creatures
    /// and missions; it only runs the Wilderness map. Each test adds the Plateau (map 1497) with
    /// its navmesh, spawns the pools it needs from the migrated rows and moves the player there.
    /// No upstream test adds a map this way: upstream quest tests use the harness's Wilderness map,
    /// and the battlefields' Targets of Opportunity tests stand their giver on an empty map.
    ///
    /// As the upstream quest tests, each playthrough also checks what the NPCs offer in
    /// conversation at each step (the topics and missions in ConvoDataDict, as
    /// WildernessLandingZoneTests and WildernessSmugglerBranchTests do), the client packets, a reload of
    /// the character from the database halfway (progress and items are saved), and a save that
    /// fails (BeforeSave): nothing of the step is kept, and the retry does it once. And, as
    /// WildernessLandingZoneTests, WildernessAliaBranchesTests and WildernessCormanArcTests, a full
    /// bag: the head stays on the corpse, the communicators are not issued, the reward waits;
    /// each works once a slot is free (WildernessAliaBranchesTests.FillCategory / FreeOneSlot).
    ///
    /// [TestCategory]: none. Some upstream quest tests have one (WildernessRanjaGorgeTests
    /// "W6Independent"); what the categories are for is unknown, so none is given here.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauCampResistanceTests
    {
        private const uint GiantKiller = PlateauGiantKillerV1.MissionId;
        private const uint Incommunicado = PlateauIncommunicadoV1.MissionId;
        private const uint Bosley = PlateauGiantKillerV1.Bosley;             // creature and pool
        private const uint Goliath = PlateauGiantKillerV1.Goliath;           // creature and pool
        private const uint Head = PlateauGiantKillerV1.HeadTemplate;
        private const uint Communicator = PlateauIncommunicadoV1.CommunicatorTemplate;
        private const uint Carvelle = PlateauReconSquadsV1.Carvelle;         // creature and pool
        private const uint Petrie = PlateauReconSquadsV1.Petrie;
        private const uint Locke = PlateauReconSquadsV1.Locke;

        /// <summary>Fillers the upstream full-bag tests use: a mission item (inventory category 4) and a weapon (1).</summary>
        private const uint MissionItemFiller = 11519, WeaponFiller = 3869;

        [TestMethod]
        public void GoliathWalksWithTwoToFourKael()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Goliath);

            var pack = Living(plateau).Where(creature => creature.SpawnPool?.DbId == PlateauGiantKillerV1.GoliathPool).ToArray();
            Assert.AreEqual(1, pack.Count(creature => creature.DbId == Goliath));
            var guards = pack.Count(creature => creature.DbId == PlateauGiantKillerV1.KaelGuard);
            Assert.IsTrue(guards >= PlateauGiantKillerV1.GuardsMin && guards <= PlateauGiantKillerV1.GuardsMax,
                $"{guards} Kael with Goliath");
        }

        /// <summary>
        /// Goliath stands half again as tall as his Kael (PlateauGiantKillerScaleV1): his body goes
        /// to the client at 1.5 (BodyAttributes scale), theirs at their models' own size.
        /// </summary>
        [TestMethod]
        public void GoliathIsHalfAgainTheSizeOfHisKael()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Goliath);

            var pack = Living(plateau).Where(creature => creature.SpawnPool?.DbId == PlateauGiantKillerV1.GoliathPool).ToArray();
            double SentScale(Creature creature)
            {
                WorldTestContext.Drain(harness.Client);
                CreatureManager.Instance.CreateCreatureOnClient(harness.Client, creature);
                return WorldTestContext.Drain(harness.Client)
                    .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet)
                    .OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == creature.EntityId)
                    .EntityData.OfType<BodyAttributesPacket>().Single().Scale;
            }

            var goliath = pack.Single(creature => creature.DbId == Goliath);
            Assert.AreEqual(1.5, goliath.Scale);
            Assert.AreEqual(1.5, SentScale(goliath));

            foreach (var guard in pack.Where(creature => creature.DbId == PlateauGiantKillerV1.KaelGuard))
                Assert.AreEqual(1.0, SentScale(guard), "a guard is its model's size");
        }

        /// <summary>
        /// Goliath leads his Kael along his beat (PlateauGiantKillerPatrolV1) on the Plateau's own
        /// navmesh: he walks it at his group's 2 m/s (PlateauGiantKillerPaceV1), not the jog of his
        /// walk_speed; they follow him (PatrolGroups), walking, and keep up with him the whole way,
        /// mostly at their places 5 m to his side (PlateauGiantKillerSpacingV1) - not the default 3 m.
        /// </summary>
        [TestMethod]
        public void GoliathLeadsHisKaelAlongHisBeat()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Goliath);

            var pack = Living(plateau).Where(creature => creature.SpawnPool?.DbId == PlateauGiantKillerV1.GoliathPool).ToArray();
            var goliath = pack.Single(creature => creature.DbId == Goliath);
            var guards = pack.Where(creature => creature.DbId == PlateauGiantKillerV1.KaelGuard).ToArray();
            var start = goliath.Position;

            Assert.AreEqual(60, goliath.Patrol?.Count, "Goliath has the beat");
            Assert.AreSame(goliath, goliath.SpawnPool.Leader);
            foreach (var guard in guards)
            {
                Assert.IsTrue(PatrolGroups.Follows(guard), "a guard follows");
                Assert.IsNull(guard.Patrol, "a guard has no beat of its own");
            }

            // 120 s: his 10 s at the west end, then most of the way east at 2 m/s. The guards keep up.
            var farthest = 0f;
            var fastest = 0f;
            var sideways = new List<float>();
            for (var tick = 0; tick < 480; tick++)
            {
                var before = goliath.Position;
                BehaviorManager.Instance.MapChannelThink(plateau, 250);
                farthest = Math.Max(farthest, Vector3.Distance(start, goliath.Position));
                fastest = Math.Max(fastest, Spacing.AcrossGround(before, goliath.Position) * 4);

                if (tick < 8)
                    continue;

                var walking = goliath.Controller.LastMovement?.Velocity > 0f;
                foreach (var guard in guards)
                {
                    if (walking)
                    {
                        // Across his heading: how far to his side the guard walks.
                        var ahead = new Vector2(-MathF.Sin((float)goliath.Rotation), -MathF.Cos((float)goliath.Rotation));
                        var from = new Vector2(guard.Position.X - goliath.Position.X, guard.Position.Z - goliath.Position.Z);
                        sideways.Add(MathF.Abs(ahead.X * from.Y - ahead.Y * from.X));
                    }

                    Assert.IsLessThan(14f, Spacing.AcrossGround(guard.Position, goliath.Position),
                        $"tick {tick}: guard at {guard.Position}, Goliath at {goliath.Position}");
                    var ground = plateau.NavMesh.GroundHeight(guard.Position);
                    Assert.IsTrue(ground is float floor && Math.Abs(guard.Position.Y - floor) < 0.5f,
                        $"tick {tick}: guard at {guard.Position} on the navmesh ({ground}); 5 m to his side the ground may be a bank above or below his");
                }
            }

            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, goliath.Controller.CurrentAction);
            Assert.IsGreaterThan(150f, farthest, $"Goliath walked {farthest:0} m");
            Assert.AreEqual(2f, fastest, 0.05f, "his pace, m/s");
            sideways.Sort();
            var median = sideways[sideways.Count / 2];
            Assert.AreEqual(5f, median, 0.6f, $"to his side, median of {sideways.Count}: {median:0.00} m");
            Assert.IsFalse(guards.Any(guard => guard.IsRunning), "the guards walk at the end, in their places");
        }

        [TestMethod]
        public void GiantKillerIsBosleysAndPaysForGoliathsHead()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley);
            var bosley = RequireNpc(plateau, Bosley);

            var offer = WildernessRanjaGorgeTests.Offer(harness, bosley, GiantKiller);
            WildernessRanjaGorgeTests.AssertSelectable(offer, GiantKiller,
                (PlateauGiantKillerV1.CryogenicBlade, 26908), (PlateauGiantKillerV1.CryogenicDisperser, 27174),
                (PlateauGiantKillerV1.VirulentInjectorGun, 26989), (PlateauGiantKillerV1.VirulentLeechGun, 27023));
            Assert.AreEqual(PlateauGiantKillerV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            Assert.IsFalse(((Dictionary<uint, MissionInfo>)Conversation(harness, bosley)[ConversationType.MissionDispense])
                .ContainsKey(Incommunicado), "Incommunicado only after Giant Killer");
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, GiantKiller));
            Assert.AreEqual(GiantKiller, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);

            // Back at Bosley without the head: no hand-in topic, nothing to hand in.
            var waiting = Conversation(harness, bosley);
            Assert.IsFalse(waiting.ContainsKey(ConversationType.ObjectiveComplete));
            Assert.IsFalse(waiting.ContainsKey(ConversationType.MissionComplete));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, bosley.EntityId, GiantKiller, 2, 1));

            KillAndLoot(harness, plateau, Goliath, Head);
            Assert.AreEqual(1U, harness.Drain().OfType<ObjectiveCompletedPacket>()
                .Single(packet => packet.MissionId == GiantKiller).ObjectiveId, "the pickup, not the kill");

            // The head and the first objective are saved.
            WildernessRanjaGorgeTests.Reload(harness);
            var log = harness.Client.Player.Missions[GiantKiller];
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[2].State);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Head));

            AssertHandInTopic(Conversation(harness, bosley), GiantKiller, 2);
            harness.MoveTo(bosley.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, bosley.EntityId, GiantKiller, 2, 1));
            var handIn = harness.Drain();
            Assert.AreEqual(2U, handIn.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            Assert.IsTrue(handIn.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == GiantKiller).IsCompleteable);

            // The turn-in topic shows the reward to choose from.
            var turnIn = (Dictionary<uint, RewardInfo>)Conversation(harness, bosley)[ConversationType.MissionComplete];
            CollectionAssert.AreEqual(offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                turnIn[GiantKiller].SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            // A turn-in whose save fails pays nothing and keeps the head.
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Giant Killer turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, bosley.EntityId, GiantKiller, 0));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Head));
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bosley, GiantKiller, 0,
                PlateauGiantKillerV1.Experience, PlateauGiantKillerV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauGiantKillerV1.CryogenicBlade), "the selected weapon");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Head), "Bosley keeps the head");
            Assert.IsTrue(((Dictionary<uint, MissionInfo>)Conversation(harness, bosley)[ConversationType.MissionDispense])
                .ContainsKey(Incommunicado), "Giant Killer done: Bosley offers Incommunicado");
        }

        [TestMethod]
        public void IncommunicadoWaitsForGiantKillerAndEachCommanderTakesHisCommunicator()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Carvelle, Petrie, Locke);
            var bosley = RequireNpc(plateau, Bosley);
            harness.MoveTo(bosley.Position);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, Incommunicado),
                "not before Giant Killer");
            Assert.IsFalse(Conversation(harness, RequireNpc(plateau, Carvelle)).ContainsKey(ConversationType.ObjectiveComplete),
                "no delivery without the mission");

            SeedCompleted(harness, GiantKiller);
            var offer = WildernessRanjaGorgeTests.Offer(harness, bosley, Incommunicado);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Incommunicado,
                (PlateauIncommunicadoV1.LaserChaingun, 27054), (PlateauIncommunicadoV1.ElectricNetGun, 27074),
                (PlateauIncommunicadoV1.LaserPistol, 27115), (PlateauIncommunicadoV1.PolarityGun, 27157));
            Assert.AreEqual(PlateauIncommunicadoV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, Incommunicado));
            Assert.AreEqual(Incommunicado, harness.Drain().OfType<MissionGainedPacket>().Single().MissionId);
            Assert.AreEqual(3U, WildernessRanjaGorgeTests.Held(harness, Communicator));

            // Any order; each commander takes his own delivery only, and only once.
            var carvelle = RequireNpc(plateau, Carvelle);
            harness.MoveTo(carvelle.Position);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, carvelle.EntityId, Incommunicado, 3, 1),
                "Locke's delivery");
            Deliver(harness, plateau, Locke, 3, 2);

            // The delivery and the communicators left are saved.
            WildernessRanjaGorgeTests.Reload(harness);
            var log = harness.Client.Player.Missions[Incommunicado];
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[3].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[2].State);
            Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, Communicator));

            // A delivery whose save fails keeps the communicator and the objective open.
            harness.MoveTo(carvelle.Position);
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Incommunicado delivery failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, carvelle.EntityId, Incommunicado, 1, 1));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, Communicator));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[Incommunicado].Objectives[1].State);
            harness.Drain();

            Deliver(harness, plateau, Carvelle, 1, 1);
            var last = Deliver(harness, plateau, Petrie, 2, 0);
            Assert.IsTrue(last.OfType<MissionCompleteablePacket>()
                .Single(packet => packet.MissionId == Incommunicado).IsCompleteable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bosley, Incommunicado, 2,
                PlateauIncommunicadoV1.Experience, PlateauIncommunicadoV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauIncommunicadoV1.LaserPistol), "the selected weapon");
        }

        /// <summary>One communicator to the commander: his objective completes, and he takes no second one. Returns the delivery's packets.</summary>
        private static IReadOnlyList<Rasa.Packets.PythonPacket> Deliver(WildernessRuntimeTestHarness harness, MapChannel plateau, uint commander,
            uint objective, uint left)
        {
            var npc = RequireNpc(plateau, commander);
            AssertHandInTopic(Conversation(harness, npc), Incommunicado, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Incommunicado, objective, 1),
                $"delivery {objective}");
            var packets = harness.Drain();
            Assert.AreEqual(objective, packets.OfType<ObjectiveCompletedPacket>()
                .Single(packet => packet.MissionId == Incommunicado).ObjectiveId);
            Assert.AreEqual(left, WildernessRanjaGorgeTests.Held(harness, Communicator));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Incommunicado, objective, 1),
                $"delivery {objective} again");
            Assert.IsFalse(Conversation(harness, npc).ContainsKey(ConversationType.ObjectiveComplete), "delivered");
            harness.Drain();
            return packets;
        }

        [TestMethod]
        public void AbandoningIncommunicadoTakesBackTheCommunicatorsLeft()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley, Carvelle);
            var bosley = RequireNpc(plateau, Bosley);
            SeedCompleted(harness, GiantKiller);
            harness.MoveTo(bosley.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, Incommunicado));
            var carvelle = RequireNpc(plateau, Carvelle);
            harness.MoveTo(carvelle.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, carvelle.EntityId, Incommunicado, 1, 1));
            Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, Communicator));

            new NpcManager(harness, harness.Manager).AbandonMission(harness.Client,
                new AbandonMissionPacket { MissionId = Incommunicado });

            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Communicator));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Incommunicado));
        }

        [TestMethod]
        public void GoliathsHeadStaysOnTheCorpseWhileTheBagIsFull()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley);
            var bosley = RequireNpc(plateau, Bosley);
            harness.MoveTo(bosley.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, GiantKiller));
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, plateau, Goliath, Head);
            PickUp(harness, loot);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Head));
            Assert.IsTrue(loot.LootItems.Any(item => item.ItemTemplateId == Head), "the head stays on the corpse");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[GiantKiller].Objectives[1].State);

            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Head));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[GiantKiller].Objectives[1].State);
        }

        [TestMethod]
        public void BosleyCannotIssueTheCommunicatorsIntoAFullBag()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley);
            var bosley = RequireNpc(plateau, Bosley);
            SeedCompleted(harness, GiantKiller);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(bosley.Position);

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, Incommunicado));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Incommunicado));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Communicator));
            using (var unit = harness.CreateChar())
            {
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, Incommunicado));
                Assert.AreEqual(0, unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Count);
            }

            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, Incommunicado));
            Assert.AreEqual(3U, WildernessRanjaGorgeTests.Held(harness, Communicator), "one stack of three");
        }

        [TestMethod]
        public void AFullRewardBagKeepsTheHeadUntilTheWeaponFits()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Bosley);
            var bosley = RequireNpc(plateau, Bosley);
            harness.MoveTo(bosley.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, bosley.EntityId, GiantKiller));
            KillAndLoot(harness, plateau, Goliath, Head);
            harness.MoveTo(bosley.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, bosley.EntityId, GiantKiller, 2, 1));
            WildernessAliaBranchesTests.FillCategory(harness, WeaponFiller, 50);
            harness.FillInbox();
            var before = harness.Context.ReadRewardTotals();

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, bosley.EntityId, GiantKiller, 0));
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Head));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauGiantKillerV1.CryogenicBlade));

            WildernessAliaBranchesTests.FreeOneSlot(harness, WeaponFiller);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bosley, GiantKiller, 0,
                PlateauGiantKillerV1.Experience, PlateauGiantKillerV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Head));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauGiantKillerV1.CryogenicBlade));
        }

        /// <summary>The Plateau as a map of the harness, with its pools from the migrated rows; the player stands on it.</summary>
        internal static MapChannel EnterPlateau(WildernessRuntimeTestHarness harness)
        {
            var plateau = new MapChannel
            {
                MapInfo = new MapInfo(PlateauMissionDataV1.MapContextId, "adv_foreas_valverde_plateau", 1556, 0),
                ClientList = new List<Client>(),
                PlayerLimit = 128,
                NavMesh = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"),
                    "adv_foreas_valverde_plateau"))
            };
            harness.Maps.MapChannelArray.Add(PlateauMissionDataV1.MapContextId, plateau);
            SpawnPoolManager.Instance.InitializeMapChannel(plateau);

            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = plateau;
            client.Player.RuntimeMapChannel = plateau;
            client.Player.MapContextId = PlateauMissionDataV1.MapContextId;
            plateau.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
            return plateau;
        }

        /// <summary>Only these pools of the Plateau, as WildernessRuntimeTestHarness.SpawnWorld does on the Wilderness.</summary>
        internal static void Spawn(MapChannel plateau, params uint[] pools)
        {
            var all = plateau.SpawnPools.ToArray();
            try
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all.Where(pool => pools.Contains(pool.DbId)));
                Assert.AreEqual(pools.Length, plateau.SpawnPools.Count, "a migrated Plateau pool is absent");
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 0);
            }
            finally
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all);
            }
        }

        internal static IEnumerable<Creature> Living(MapChannel plateau) => plateau.MapCellInfo.Cells.Values
            .SelectMany(cell => cell.CreatureList).Distinct()
            .Where(creature => creature.State != CharacterState.Dead && CreatureManager.IsLivingOnMap(plateau, creature));

        internal static Creature RequireNpc(MapChannel plateau, uint pool)
        {
            var npc = Living(plateau).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(npc, $"Pool {pool} must stand a real public NPC on the Plateau.");
            Assert.AreEqual(pool, npc.DbId);
            return npc;
        }

        /// <summary>What the NPC offers when the player talks to him (WildernessRanjaGorgeTests.OpenConversation, which is private).</summary>
        internal static Dictionary<ConversationType, object> Conversation(WildernessRuntimeTestHarness harness, Creature npc)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = npc.EntityId });
            return harness.Drain().OfType<ConversePacket>().Single().ConvoDataDict;
        }

        /// <summary>The hand-in topic of exactly this objective, on the objective's flag 1.</summary>
        internal static void AssertHandInTopic(Dictionary<ConversationType, object> conversation, uint mission, uint objective)
        {
            Assert.IsTrue(conversation.TryGetValue(ConversationType.ObjectiveComplete, out var value), $"hand-in of {mission}.{objective}");
            var topic = ((List<CompleteableObjectives>)value).Single();
            Assert.AreEqual(((int)mission, (int)objective, 1), (topic.MissionId, topic.ObjectiveId, topic.PlayerFlagId));
        }

        /// <summary>Kills the pool's creature as the player and takes all of its corpse: the template is in it.</summary>
        private static void KillAndLoot(WildernessRuntimeTestHarness harness, MapChannel plateau, uint pool, uint template)
        {
            var loot = Kill(harness, plateau, pool, template);
            var held = WildernessRanjaGorgeTests.Held(harness, template);
            PickUp(harness, loot);
            Assert.AreEqual(held + 1, WildernessRanjaGorgeTests.Held(harness, template));
        }

        /// <summary>Kills the pool's creature as the player; its corpse holds the template, which is not yet the player's.</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel plateau, uint pool, uint template)
        {
            Spawn(plateau, pool);
            var creature = Living(plateau).First(candidate => candidate.SpawnPool?.DbId == pool && candidate.DbId == pool);
            harness.MoveTo(creature.Position);
            var held = WildernessRanjaGorgeTests.Held(harness, template);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, creature, harness.Client.Player);
            Assert.IsTrue(plateau.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot));
            Assert.IsTrue(loot.LootItems.Any(item => item.ItemTemplateId == template), $"corpse of {pool} holds {template}");
            Assert.AreEqual(held, WildernessRanjaGorgeTests.Held(harness, template), "a death is not a pickup");
            return loot;
        }

        internal static void PickUp(WildernessRuntimeTestHarness harness, LootDispenser loot) =>
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

        /// <summary>Completed missions in the character's history, as WildernessRanjaGorgeTests seeds them.</summary>
        internal static void SeedCompleted(WildernessRuntimeTestHarness harness, params uint[] completed)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var mission in completed)
                        unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                        {
                            CharacterId = harness.Client.Player.Id,
                            MissionId = mission,
                            AssignmentId = Guid.NewGuid().ToString("N"),
                            AssignmentGeneration = 1,
                            ContentRevision = PlateauMissionDataV1.Revision,
                            CompletedAtUtc = harness.UtcNow,
                            RewardedAtUtc = harness.UtcNow,
                            Outcome = (uint)MissionState.Completed,
                            Rewarded = true
                        });
                });
            WildernessRanjaGorgeTests.Reload(harness);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;
            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
