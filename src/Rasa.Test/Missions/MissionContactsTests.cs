using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Runtime;
    using Rasa.Models;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    /// <summary>
    /// Gearing Up (1992) is handed in to Corporal DeSimone. Out of his cells - more than 64 m
    /// away - the client holds no entity for the map and the radar to draw him from.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionContactsTests
    {
        private const uint GearingUp = BootcampRuntimeTestHarness.MissionGearingUp;

        [TestMethod]
        public void TheReceiverOfAFinishedMissionIsGivenToAClientOutOfHisRange()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: true);

            CollectionAssert.AreEqual(new[] { BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId },
                harness.Manager.TurnInReceivers(harness.Client.Player).ToArray());

            MissionContacts.Sync(harness.Client);

            var packets = harness.Drain();
            Assert.HasCount(1, packets.OfType<CreatePhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
            var status = packets.OfType<NPCConversationStatusPacket>().Single();
            Assert.AreEqual(ConversationStatus.MissionComplete, status.ConvoStatusId, "what the map marker is drawn for");
            CollectionAssert.Contains(status.Data, GearingUp);
            Assert.IsTrue(harness.Client.FarContacts.Contains(deSimone.EntityId));

            // Held: not given again.
            MissionContacts.Sync(harness.Client);
            Assert.IsEmpty(harness.Drain().OfType<CreatePhysicalEntityPacket>().ToArray());
        }

        [TestMethod]
        public void AMissionStillInProgressDoesNotGiveItsReceiver()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: false);

            Assert.IsEmpty(harness.Manager.TurnInReceivers(harness.Client.Player).ToArray());

            MissionContacts.Sync(harness.Client);

            Assert.IsEmpty(harness.Drain().OfType<CreatePhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
            Assert.IsFalse(harness.Client.FarContacts.Contains(deSimone.EntityId));
        }

        // The other half of what a mission sends a player to speak to: the NPC an objective is
        // talked through with. Gearing Up opens with Captain Delessio's briefing (objective 4).

        [TestMethod]
        public void TheNpcAnObjectiveIsTalkedThroughWithIsGivenToAClientOutOfHisRange()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Start(harness, finished: false);
            var delessio = Delessio(harness);

            CollectionAssert.AreEqual(new[] { BootcampRuntimeTestHarness.CaptainDelessioPackageId },
                harness.Manager.ObjectiveContacts(harness.Client.Player).ToArray());

            MissionContacts.Sync(harness.Client);

            var packets = harness.Drain();
            Assert.HasCount(1, packets.OfType<CreatePhysicalEntityPacket>().ToArray());
            Assert.AreEqual(delessio.EntityId, packets.OfType<CreatePhysicalEntityPacket>().Single().EntityId);
            var status = packets.OfType<NPCConversationStatusPacket>().Single();
            Assert.AreEqual(ConversationStatus.ObjectivComplete, status.ConvoStatusId, "what the map marker is drawn for");
            CollectionAssert.Contains(status.Data, GearingUp);
            Assert.IsTrue(harness.Client.FarContacts.Contains(delessio.EntityId));

            // Held: not given again.
            MissionContacts.Sync(harness.Client);
            Assert.IsEmpty(harness.Drain().OfType<CreatePhysicalEntityPacket>().ToArray());
        }

        [TestMethod]
        public void TheObjectiveTalkedThroughHeIsNoLongerHeld()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Start(harness, finished: false);
            var delessio = Delessio(harness);
            MissionContacts.Sync(harness.Client);
            harness.Drain();

            // Briefed (beside him, where his cells hold him), and away again.
            harness.MovePlayerTo(delessio);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, GearingUp, 4, 1));
            harness.MovePlayerTo(Far(delessio));
            CellManager.Instance.UpdateVisibility(harness.Client);
            MissionContacts.Sync(harness.Client);

            Assert.IsFalse(harness.Manager.ObjectiveContacts(harness.Client.Player).Contains(BootcampRuntimeTestHarness.CaptainDelessioPackageId));
            Assert.IsFalse(harness.Client.FarContacts.Contains(delessio.EntityId));
            AssertContactsAreTheMarkedNpcs(harness);
        }

        [TestMethod]
        public void WalkingAwayFromHimKeepsHimWhileTheObjectiveIsHisToHear()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Start(harness, finished: false);
            var delessio = Delessio(harness);
            harness.MovePlayerTo(delessio);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();

            harness.MovePlayerTo(Far(delessio));
            CellManager.Instance.UpdateVisibility(harness.Client);

            Assert.IsFalse(harness.Drain().OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == delessio.EntityId),
                "the NPC of an open objective does not go with his cells");
            Assert.IsTrue(harness.Client.FarContacts.Contains(delessio.EntityId));
        }

        [TestMethod]
        public void TheContactsAreTheNpcsTheClientMarks()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Start(harness, finished: false);

            AssertContactsAreTheMarkedNpcs(harness);

            // And with the mission ready to hand in.
            foreach (var objective in harness.Client.Player.Missions[GearingUp].Objectives.Values)
                objective.State = MissionObjectiveState.Completed;
            harness.Client.Player.Missions[GearingUp].Completeable = true;

            AssertContactsAreTheMarkedNpcs(harness);
        }

        [TestMethod]
        public void AnNpcHeldFromAfarIsToldToMoveWhereItsCellsAre()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Start(harness, finished: false);
            var delessio = Delessio(harness);
            var hartmann = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, BootcampRuntimeTestHarness.CorporalHartmannPackageId);
            MissionContacts.Sync(harness.Client);
            Assert.IsTrue(harness.Client.FarContacts.Contains(delessio.EntityId));
            Assert.IsFalse(harness.Client.FarContacts.Contains(hartmann.EntityId));
            WorldTestContext.Drain(harness.Client);

            var movement = new Movement(delessio.Position + new Vector3(1, 0, 0), new Vector2(0f, 0f));
            CellManager.Instance.CellMoveObject(delessio, movement);
            CellManager.Instance.CellMoveObject(hartmann, new Movement(hartmann.Position, new Vector2(0f, 0f)));

            var move = WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<MoveObjectMessage>().Single();
            Assert.AreEqual(delessio.EntityId, move.EntityId);
            Assert.AreSame(movement, move.Movement);

            // Beside him the cells tell the client, once.
            harness.MovePlayerTo(delessio);
            CellManager.Instance.UpdateVisibility(harness.Client);
            WorldTestContext.Drain(harness.Client);
            CellManager.Instance.CellMoveObject(delessio, movement);
            Assert.HasCount(1, WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<MoveObjectMessage>().ToArray());
        }

        [TestMethod]
        public void FinishingTheMissionPutsHimOnTheMapAtOnce()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: false);

            harness.Client.Player.Missions[GearingUp].Completeable = true;
            harness.Manager.RefreshNpcConversationStatuses(harness.Client);

            Assert.IsTrue(harness.Drain().OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == deSimone.EntityId));
            Assert.IsTrue(harness.Client.FarContacts.Contains(deSimone.EntityId));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WalkingAwayFromHimKeepsHimOnlyWhileTheMissionIsHisToTake(bool finished)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished, beside: true);

            harness.MovePlayerTo(Far(deSimone));
            CellManager.Instance.UpdateVisibility(harness.Client);

            var gone = harness.Drain().OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == deSimone.EntityId);
            Assert.AreEqual(!finished, gone, "the receiver of a finished mission does not go with his cells");
            Assert.AreEqual(finished, harness.Client.FarContacts.Contains(deSimone.EntityId));
        }

        [TestMethod]
        public void HeIsTakenAwayWhenTheMissionIsNoLongerHisToTake()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: true);
            MissionContacts.Sync(harness.Client);
            harness.Drain();

            // Abandoned, from the other side of the map.
            harness.Client.Player.Missions.Remove(GearingUp);
            MissionContacts.Sync(harness.Client);

            Assert.HasCount(1, harness.Drain().OfType<DestroyPhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
            Assert.IsEmpty(harness.Client.FarContacts.ToArray());
        }

        [TestMethod]
        public void ComingBackIntoRangeHandsHimBackToHisCells()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: true);
            MissionContacts.Sync(harness.Client);
            harness.Drain();

            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);

            // Taken off the client and made anew, in that order, with his status after: a second
            // CreatePhysicalEntity alone is an update on the client, and the status after an
            // update is one its overhead window already shows - no icon is attached again.
            var his = WorldTestContext.Drain(harness.Client).Select(sent => sent.Message).OfType<CallMethodMessage>()
                .Where(message =>
                    message.Packet is DestroyPhysicalEntityPacket destroyed && destroyed.EntityId == deSimone.EntityId ||
                    message.Packet is CreatePhysicalEntityPacket created && created.EntityId == deSimone.EntityId ||
                    message.Packet is NPCConversationStatusPacket && message.EntityId == deSimone.EntityId)
                .Select(message => message.Packet).ToArray();
            Assert.HasCount(3, his);
            Assert.IsInstanceOfType(his[0], typeof(DestroyPhysicalEntityPacket));
            Assert.IsInstanceOfType(his[1], typeof(CreatePhysicalEntityPacket));
            Assert.AreEqual(ConversationStatus.MissionComplete, ((NPCConversationStatusPacket)his[2]).ConvoStatusId);
            Assert.IsEmpty(harness.Client.FarContacts.ToArray(), "his cells have him now");

            MissionContacts.Sync(harness.Client);

            Assert.IsEmpty(harness.Client.FarContacts.ToArray());
            var after = harness.Drain();
            Assert.IsEmpty(after.OfType<DestroyPhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
            Assert.IsEmpty(after.OfType<CreatePhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
        }

        [TestMethod]
        public void AnNpcTheClientNeverHeldIsOnlyCreated()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: false);

            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);

            var packets = harness.Drain();
            Assert.HasCount(1, packets.OfType<CreatePhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
            Assert.IsEmpty(packets.OfType<DestroyPhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
        }

        [TestMethod]
        public void HeldFromAfarAndWalkingIntoViewHeIsMadeAnewToo()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: true);
            MissionContacts.Sync(harness.Client);
            Assert.IsTrue(harness.Client.FarContacts.Contains(deSimone.EntityId));
            harness.Drain();

            // He walks to the player: his cells change, and the player's are among the new ones.
            Assert.IsTrue(CellManager.TryGetCellCoordinates(harness.Client.Player.Position, out var x, out var z));
            deSimone.Position = harness.Client.Player.Position;
            CreatureManager.Instance.CellUpdateLocation(harness.BootcampMap, deSimone, x, z);

            var his = harness.Drain().Where(packet =>
                packet is DestroyPhysicalEntityPacket destroyed && destroyed.EntityId == deSimone.EntityId ||
                packet is CreatePhysicalEntityPacket created && created.EntityId == deSimone.EntityId).ToArray();
            Assert.HasCount(2, his);
            Assert.IsInstanceOfType(his[0], typeof(DestroyPhysicalEntityPacket));
            Assert.IsInstanceOfType(his[1], typeof(CreatePhysicalEntityPacket));
            Assert.IsEmpty(harness.Client.FarContacts.ToArray());
        }

        [TestMethod]
        public void HandedInBesideHimNothingIsTakenAway()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: true);
            MissionContacts.Sync(harness.Client);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();

            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, deSimone.EntityId, GearingUp, null, null));

            var packets = harness.Drain();
            Assert.IsEmpty(packets.OfType<DestroyPhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
            Assert.IsEmpty(harness.Client.FarContacts.ToArray());
            Assert.IsEmpty(harness.Manager.TurnInReceivers(harness.Client.Player).ToArray());
        }

        [TestMethod]
        public void LeavingTheWorldHeIsTakenOffTheClientThatHeldHimFromAfar()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: true);
            MissionContacts.Sync(harness.Client);
            harness.Drain();

            CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, deSimone);

            Assert.HasCount(1, harness.Drain().OfType<DestroyPhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray(),
                "no cell of his holds this client to tell it");
            Assert.IsEmpty(harness.Client.FarContacts.ToArray());
        }

        [TestMethod]
        public void DeadHeIsNoOneToHandAMissionInTo()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: true);
            MissionContacts.Sync(harness.Client);
            harness.Drain();

            deSimone.State = CharacterState.Dead;
            MissionContacts.Sync(harness.Client);

            Assert.HasCount(1, harness.Drain().OfType<DestroyPhysicalEntityPacket>().Where(packet => packet.EntityId == deSimone.EntityId).ToArray());
            Assert.IsEmpty(harness.Client.FarContacts.ToArray());
        }

        [TestMethod]
        public void TheMapChecksEachClientEveryFewSeconds()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var deSimone = Start(harness, finished: true);
            var now = Environment.TickCount64;

            MissionContacts.Worker(harness.BootcampMap, now);
            Assert.IsTrue(harness.Drain().OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == deSimone.EntityId),
                "the first turn after arriving: no mission event says a player has come onto a map");

            // He respawns - a new entity - and nothing gives it until the next check is due.
            harness.Client.FarContacts.Clear();
            MissionContacts.Worker(harness.BootcampMap, now + MissionContacts.SyncIntervalMs - 1);
            Assert.IsEmpty(harness.Drain().OfType<CreatePhysicalEntityPacket>().ToArray());

            MissionContacts.Worker(harness.BootcampMap, now + MissionContacts.SyncIntervalMs);
            Assert.IsTrue(harness.Drain().OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == deSimone.EntityId));
        }

        [TestMethod]
        public void LeavingTheMapForgetsWhatWasHeld()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Start(harness, finished: true);
            MissionContacts.Worker(harness.BootcampMap, Environment.TickCount64);
            Assert.HasCount(1, harness.Client.FarContacts.ToArray());

            CellManager.Instance.RemoveFromWorld(harness.Client);

            Assert.IsEmpty(harness.Client.FarContacts.ToArray());
            Assert.AreEqual(0L, harness.Client.NextContactSync, "checked afresh on the next map");
        }

        /// <summary>
        /// Gearing Up active, finished or not, the Bootcamp NPCs spawned, and the player either
        /// beside Corporal DeSimone or well out of his cells - with their client given what its
        /// cells hold.
        /// </summary>
        private static Creature Start(BootcampRuntimeTestHarness.Harness harness, bool finished, bool beside = false)
        {
            harness.SeedMission(harness.Client.Player.Id, GearingUp, (uint)MissionState.Active, finished);
            harness.SpawnWorldNpcs();

            var deSimone = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, BootcampRuntimeTestHarness.CorporalDeSimonePackageId);
            Assert.IsNotNull(deSimone);

            if (beside)
                harness.MovePlayerTo(deSimone);
            else
                harness.MovePlayerTo(Far(deSimone));

            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Client.FarContacts.Clear();
            harness.Client.NextContactSync = 0;
            harness.Drain();

            return deSimone;
        }

        private static Creature Delessio(BootcampRuntimeTestHarness.Harness harness) =>
            BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, BootcampRuntimeTestHarness.CaptainDelessioPackageId);

        /// <summary>
        /// MissionContacts' two sets name exactly the NPCs whose status the client puts a mission
        /// marker on (mapwindow.py HandleUpdateOverheadIndicator): MissionComplete, Reward,
        /// ObjectivComplete and ObjectivChoice.
        /// </summary>
        private static void AssertContactsAreTheMarkedNpcs(BootcampRuntimeTestHarness.Harness harness)
        {
            var player = harness.Client.Player;
            var receivers = harness.Manager.TurnInReceivers(player);
            var packages = harness.Manager.ObjectiveContacts(player);
            var npcs = harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Where(creature => creature.Npc != null).Distinct().ToArray();

            Assert.IsTrue(npcs.Length > 0);

            foreach (var npc in npcs)
            {
                harness.Manager.ClassifyNpcConversation(player, npc).TryGetStatus(out var status, out _);
                var marked = status is ConversationStatus.MissionComplete or ConversationStatus.Reward
                    or ConversationStatus.ObjectivComplete or ConversationStatus.ObjectivChoice;

                Assert.AreEqual(marked, receivers.Contains(npc.DbId) || packages.Contains(npc.Npc.NpcPackageId),
                    $"NPC {npc.DbId} (package {npc.Npc.NpcPackageId}) has status {status}");
            }
        }

        /// <summary>Ten cells east of him: nothing of his five by five is shared.</summary>
        private static Vector3 Far(Creature creature) => creature.Position + new Vector3(10 * CellManager.CellSize, 0, 0);
    }
}
