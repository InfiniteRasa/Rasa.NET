extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Wedge Rock Outpost's missions, Plateau, played through as a player plays them: Colonel
    /// Thibodeau's All Along The Watchtowers (1825) and Comm Officer Devinchy's Cryptic Messages (557,
    /// turned in by radio); with them Commander Grissom's Soyuz Salvage: Plateau (2007), Fort Defiance.
    ///
    /// Why these tests: as PlateauNewVelonVillageTests. The terminals are talked to as
    /// WildernessTwinPillarsTests talks to the blood analyzer; the radio turn-in is
    /// MissionRadioLifecycleTests' TryCompleteRadioMission; the debris is used as
    /// MissionSceneObjectActionsTests uses a scene object.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauWedgeRockTests
    {
        private const uint Watchtowers = PlateauWatchtowersV1.MissionId;
        private const uint CrypticMessages = PlateauCrypticMessagesV1.MissionId;
        private const uint SoyuzSalvage = PlateauSoyuzSalvageV1.MissionId;
        private const uint Thibodeau = PlateauWatchtowersV1.Thibodeau;           // creature and pool
        private const uint Devinchy = PlateauCrypticMessagesV1.Devinchy;
        private const uint McKinley = PlateauCrypticMessagesV1.McKinley;
        private const uint Vorvaak = PlateauCrypticMessagesV1.Vorvaak;
        private const uint Grissom = PlateauSoyuzSalvageV1.Grissom;
        private const uint Device = PlateauCrypticMessagesV1.DeviceTemplate;
        private const uint Debris = PlateauSoyuzSalvageV1.DebrisTemplate;

        [TestMethod]
        public void TheWatchtowersTerminalsRebootOneTalkEachAndOnlyWithTheMission()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Thibodeau);
            var thibodeau = RequireNpc(plateau, Thibodeau);
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == Watchtowers), "no terminal before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, thibodeau, Watchtowers);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Watchtowers,
                (PlateauWatchtowersV1.BioHelmet, 23383), (PlateauWatchtowersV1.GravitonHelmet, 15089),
                (PlateauWatchtowersV1.MechHelmet, 17996), (PlateauWatchtowersV1.StealthHelmet, 19889));
            Assert.AreEqual(PlateauWatchtowersV1.Credits, offer.FixedReward.Credits[CurencyType.Credits]);
            harness.MoveTo(thibodeau.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, thibodeau.EntityId, Watchtowers));
            harness.Drain();

            foreach (var terminal in PlateauWatchtowersV1.Terminals)
            {
                var obj = plateau.DynamicObjects.Single(candidate => candidate.SceneMissionId == Watchtowers &&
                    candidate.SceneActorRole == $"terminal-{terminal.Objective}");
                Assert.AreEqual(PlateauWatchtowersV1.TerminalClass, (uint)obj.EntityClassId);
                Assert.AreEqual(new Vector3(terminal.At.X, terminal.At.Y, terminal.At.Z), obj.Position, "on the tower's upper floor");
                Assert.AreEqual(terminal.Package, obj.MissionConversation.NpcPackageId);
                Assert.AreEqual(harness.Client.Player.Id, obj.SceneOwnerCharacterId, "the player's own");

                var talk = Open(harness, obj);
                Assert.IsNotNull(talk, $"terminal {terminal.Objective} talks");
                Assert.IsTrue(talk.ConvoDataDict.ContainsKey(ConversationType.ObjectiveComplete));
                new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
                {
                    EntityId = obj.EntityId, MissionId = Watchtowers, ObjectiveId = terminal.Objective, PlayerFlagId = 1
                });
                Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[Watchtowers].Objectives[terminal.Objective].State);
                Assert.IsNull(Open(harness, obj), $"terminal {terminal.Objective} rebooted once");
                if (terminal.Objective == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.IsFalse(harness.Client.Player.Missions[Watchtowers].Completeable, "two of three");
                    harness.Drain();
                }
            }
            Assert.IsTrue(harness.Client.Player.Missions[Watchtowers].Completeable);

            var before = harness.Context.ReadRewardTotals();
            harness.MoveTo(thibodeau.Position);
            harness.Drain();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Watchtowers turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, thibodeau.EntityId, Watchtowers, 0));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, thibodeau, Watchtowers, 0,
                PlateauWatchtowersV1.Experience, PlateauWatchtowersV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauWatchtowersV1.BioHelmet), "the selected helmet");
        }

        [TestMethod]
        public void CrypticMessagesTakesVorvaaksDeviceToDevinchyAndEndsByRadio()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Devinchy, McKinley, Vorvaak);
            var devinchy = RequireNpc(plateau, Devinchy);
            var mckinley = RequireNpc(plateau, McKinley);

            var offer = WildernessRanjaGorgeTests.Offer(harness, devinchy, CrypticMessages);
            WildernessRanjaGorgeTests.AssertSelectable(offer, CrypticMessages,
                (PlateauCrypticMessagesV1.BioLegs, 23391), (PlateauCrypticMessagesV1.GravitonLegs, 15125),
                (PlateauCrypticMessagesV1.MechLegs, 18032), (PlateauCrypticMessagesV1.StealthLegs, 19925));
            harness.MoveTo(devinchy.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, devinchy.EntityId, CrypticMessages));
            harness.Drain();
            Assert.IsTrue(harness.Manager.LoadedMissions[CrypticMessages].RadioCompletable == true);

            // Vorvaak drops nothing before McKinley names him.
            Assert.IsFalse(Holds(Kill(harness, plateau, Vorvaak), Device));
            Respawn(plateau, Vorvaak);

            Talk(harness, mckinley, 1);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[CrypticMessages].Objectives[2].State);
            var corpse = Kill(harness, plateau, Vorvaak);
            Assert.IsTrue(Holds(corpse, Device), "the device on Vorvaak");
            PickUp(harness, corpse);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Device));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (harness.Client.Player.Missions[CrypticMessages].Objectives[2].State,
                 harness.Client.Player.Missions[CrypticMessages].Objectives[3].State));

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Device));
            harness.Drain();

            Talk(harness, devinchy, 3);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Device), "Devinchy decodes with it");
            Assert.IsTrue(harness.Client.Player.Missions[CrypticMessages].Completeable);
            Assert.IsFalse(Conversation(harness, devinchy).ContainsKey(ConversationType.MissionComplete), "no NPC turn-in");
            harness.Drain();

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, CrypticMessages, 1, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[CrypticMessages].State);
            Assert.AreEqual(before.Credits + PlateauCrypticMessagesV1.Credits, harness.Context.ReadRewardTotals().Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauCrypticMessagesV1.GravitonLegs), "the selected legs");
        }

        [TestMethod]
        public void SoyuzSalvageTakesOnePieceOfThreeToGrissom()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            Spawn(plateau, Grissom);
            var grissom = RequireNpc(plateau, Grissom);
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == SoyuzSalvage), "no debris before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, grissom, SoyuzSalvage);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no choice: the model rocket");
            harness.MoveTo(grissom.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, grissom.EntityId, SoyuzSalvage));
            harness.Drain();

            var pieces = PlateauSoyuzSalvageV1.Pieces.Select(piece => plateau.DynamicObjects.Single(obj =>
                obj.SceneMissionId == SoyuzSalvage && obj.SceneActorRole == piece.Role)).ToArray();
            for (var index = 0; index < pieces.Length; index++)
            {
                var piece = PlateauSoyuzSalvageV1.Pieces[index];
                Assert.AreEqual(piece.Class, (uint)pieces[index].EntityClassId);
                Assert.AreEqual(new Vector3(piece.At.X, piece.At.Y, piece.At.Z), pieces[index].Position);
                var ground = plateau.NavMesh.GroundHeight(pieces[index].Position);
                Assert.IsNotNull(ground, $"{piece.Role} has navmesh under it");
                Assert.IsTrue(System.Math.Abs(ground.Value - piece.At.Y) < 0.5f, $"{piece.Role} at {piece.At.Y}, ground {ground}");
            }

            harness.MoveTo(pieces[1].Position + new Vector3(0, 0, 1));
            Use(harness, plateau, pieces[1], 1, recovers: false);
            Use(harness, plateau, pieces[1], PlateauSoyuzSalvageV1.UseArgument, recovers: true);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Debris));
            Assert.IsTrue(harness.Client.Player.Missions[SoyuzSalvage].Completeable);
            Assert.AreEqual((UseObjectState)PlateauSoyuzSalvageV1.TakenState, pieces[1].StateId);
            Assert.IsTrue(pieces.All(obj => !obj.IsEnabled), "one piece closes all three");
            harness.MoveTo(pieces[2].Position + new Vector3(0, 0, 1));
            Use(harness, plateau, pieces[2], PlateauSoyuzSalvageV1.UseArgument, recovers: false);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Debris), "one piece");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Debris));
            harness.Drain();

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, grissom, SoyuzSalvage, null,
                PlateauSoyuzSalvageV1.Experience, PlateauSoyuzSalvageV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauSoyuzSalvageV1.ModelRocket), "the model rocket");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, Debris), "Grissom keeps the piece");
        }

        /// <summary>The NPC's hand-in topic of this Cryptic Messages objective, and the objective completes there.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), CrypticMessages, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, CrypticMessages, objective, 1),
                $"objective {objective}");
            harness.Drain();
        }

        /// <summary>The terminal's conversation, as WildernessTwinPillarsTests.OpenAnalyzer (private) opens the analyzer's.</summary>
        private static ConversePacket Open(WildernessRuntimeTestHarness harness, DynamicObject terminal)
        {
            harness.MoveTo(terminal.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = terminal.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault();
        }

        /// <summary>Kills the living creature of this kind as the player; returns its corpse.</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel plateau, uint kind)
        {
            var creature = Living(plateau).FirstOrDefault(candidate => candidate.DbId == kind && candidate.Npc == null);
            Assert.IsNotNull(creature, $"no living {kind} left");
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(plateau, creature, harness.Client.Player);
            Assert.IsTrue(plateau.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        private static bool Holds(LootDispenser loot, uint template) => loot.LootItems.Any(item => item.ItemTemplateId == template);

        /// <summary>These pools' dead come back: their respawn time and more has passed.</summary>
        private static void Respawn(MapChannel plateau, params uint[] pools)
        {
            var all = plateau.SpawnPools.ToArray();
            try
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all.Where(pool => pools.Contains(pool.DbId)));
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 600_000);
                SpawnPoolManager.Instance.SpawnPoolWorker(plateau, 600_000);
            }
            finally
            {
                plateau.SpawnPools.Clear();
                plateau.SpawnPools.AddRange(all);
            }
        }

        /// <summary>A use of the object: the request, then its recovery if one is due.</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel plateau, DynamicObject obj, uint argument, bool recovers)
        {
            var count = plateau.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), plateau.PerformRecovery.Count, $"argument {argument}");
            if (!recovers)
                return;
            var recovery = plateau.PerformRecovery.Last();
            plateau.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(plateau, recovery);
        }
    }
}
