using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class ConradCorpseDialogueTests
    {
        [TestMethod]
        [DataRow("unopened")]
        [DataRow("mission")]
        [DataRow("objective")]
        [DataRow("flag")]
        [DataRow("distance")]
        [DataRow("owner")]
        [DataRow("missing-entity")]
        [DataRow("non-finite")]
        [DataRow("dead")]
        [DataRow("disabled")]
        [DataRow("foreign-map")]
        [DataRow("removed")]
        [DataRow("assignment")]
        [DataRow("generation")]
        [DataRow("revision")]
        public void InvalidCorpseContinueCannotGrantOrAdvance(string invalid)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            FindMissingSoldiers(harness);
            var corpse = Corpse(harness);
            harness.MovePlayerTo(corpse);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            if (invalid != "unopened")
                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = corpse.EntityId });
            var request = Continue(corpse);
            var originalPosition = corpse.Position;
            if (invalid == "mission") request.MissionId = 2005;
            if (invalid == "objective") request.ObjectiveId = 3;
            if (invalid == "flag") request.PlayerFlagId = 2;
            if (invalid == "distance") harness.MovePlayerTo(corpse.Position + new System.Numerics.Vector3(50, 0, 0));
            if (invalid == "owner") corpse.SceneOwnerCharacterId = 999;
            if (invalid == "missing-entity") request.EntityId = ulong.MaxValue;
            if (invalid == "non-finite") corpse.Position = new System.Numerics.Vector3(float.NaN, 0, 0);
            if (invalid == "dead") harness.Client.Player.State = CharacterState.Dead;
            if (invalid == "disabled") corpse.IsEnabled = false;
            if (invalid == "foreign-map") corpse.RuntimeMapChannel = new MapChannel { MapInfo = harness.BootcampMap.MapInfo };
            if (invalid == "removed") EntityManager.Instance.UnregisterEntity(corpse.EntityId);
            if (invalid is "assignment" or "generation" or "revision")
            {
                using var unit = harness.Context.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var row = unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, 1995);
                    if (invalid == "assignment") row.AssignmentId = Guid.NewGuid().ToString("N");
                    if (invalid == "generation") row.Generation++;
                    if (invalid == "revision") row.ContentRevision = "stale-dialogue";
                });
            }

            try
            {
                npcs.CompleteNPCObjective(harness.Client, request);

                Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[3].State);
                Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
                using var verify = harness.Context.CreateChar();
                Assert.IsNull(verify.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));
            }
            finally
            {
                corpse.Position = originalPosition;
                corpse.RuntimeMapChannel = harness.BootcampMap;
            }
        }

        [TestMethod]
        public void ReconnectingAfterOpeningWithoutContinueGrantsNothingAndAllowsANewDialog()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            FindMissingSoldiers(harness);
            var corpse = Corpse(harness);
            harness.MovePlayerTo(corpse);
            new NpcManager(harness.Context, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = corpse.EntityId });

            harness.ReconnectFresh();

            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.CompleteNPCObjective(harness.Client, DecodeContinue(corpse));
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            harness.UseObjectAndRecover(Corpse(harness));
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }

        [TestMethod]
        public void FailedContinueRollsBackTheBombAndDeadlineAndTheSameDialogCanRetry()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            FindMissingSoldiers(harness);
            var corpse = Corpse(harness);
            harness.MovePlayerTo(corpse);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = corpse.EntityId });
            harness.Context.AfterSave = _ => throw new DbUpdateException("Injected corpse Continue failure.");

            npcs.CompleteNPCObjective(harness.Client, DecodeContinue(corpse));

            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[3].State);
            using (var verify = harness.Context.CreateChar())
                Assert.IsNull(verify.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));
            harness.Context.AfterSave = null;
            npcs.CompleteNPCObjective(harness.Client, Continue(corpse));
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }

        [TestMethod]
        public void FindingTheBodiesCompletesTheSearchWithoutSpawningASurvivorToTalkTo()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);

            FindMissingSoldiers(harness);

            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[2].State,
                "Proximity to the bodies must complete the missing-soldiers objective.");
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.IsNull(BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, 2584),
                "The mission must not invent a living survivor NPC.");
            var corpse = Corpse(harness);
            Assert.AreEqual(21081U, (uint)corpse.EntityClassId,
                "Conrad must use the native male corpse class with NPC conversation support.");
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }

        [TestMethod]
        public void CorpseClickOpensContinueAndOnlyContinueGrantsTheBomb()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            FindMissingSoldiers(harness);
            var corpse = Corpse(harness);
            harness.MovePlayerTo(corpse);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var introduction = harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Single(packet => packet.EntityId == corpse.EntityId);
            Assert.AreEqual(2584U, introduction.EntityData.OfType<NPCInfoPacket>().Single().NpcPackageId);
            Assert.IsFalse(introduction.EntityData.OfType<UsableInfoPacket>().Any(),
                "The native NPC corpse has no Usable augmentation.");
            var npcs = new NpcManager(harness.Context, harness.Manager);

            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = corpse.EntityId });

            var dialogue = harness.Drain().OfType<ConversePacket>().Single();
            var objective = ((System.Collections.Generic.List<CompleteableObjectives>)
                dialogue.ConvoDataDict[ConversationType.ObjectiveComplete]).Single();
            Assert.AreEqual(1995, objective.MissionId);
            Assert.AreEqual(2, objective.ObjectiveId,
                "The native dialogue text is indexed by objective 2, independently of progression objective 3.");
            Assert.AreEqual(1, objective.PlayerFlagId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            using (var before = harness.Context.CreateChar())
                Assert.IsNull(before.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));

            npcs.CompleteNPCObjective(harness.Client, Continue(corpse));

            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[1].State);
            using var verify = harness.Context.CreateChar();
            var deadline = verify.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995);
            Assert.AreEqual(CharacterMissionDeadlineState.Active, deadline.State);
            Assert.AreEqual(harness.UtcNow + TimeSpan.FromSeconds(600), deadline.DueAtUtc);
            npcs.CompleteNPCObjective(harness.Client, Continue(corpse));
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }

        internal static void FindMissingSoldiers(BootcampRuntimeTestHarness.Harness harness)
        {
            var youngblood = harness.AddNpc(BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId, 2561);
            harness.SeedMission(harness.Client.Player.Id, 1994, (uint)MissionState.Completed, true);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, youngblood.EntityId, 1995));
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1995, 435)));
        }

        internal static DynamicObject Corpse(BootcampRuntimeTestHarness.Harness harness) =>
            BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse")
                ?? throw new AssertFailedException("Finding the bodies must make Conrad's corpse available without a survivor conversation.");

        internal static CompleteNPCObjectivePacket Continue(DynamicObject corpse) => new()
        {
            EntityId = corpse.EntityId, MissionId = 1995, ObjectiveId = 2, PlayerFlagId = 1
        };

        private static CompleteNPCObjectivePacket DecodeContinue(DynamicObject corpse)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteULong(corpse.EntityId);
                writer.WriteUInt(1995);
                writer.WriteUInt(2);
                writer.WriteUInt(1);
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new CompleteNPCObjectivePacket();
            packet.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            return packet;
        }

        [TestMethod]
        [DataRow(5f, true)]
        [DataRow(5.001f, false)]
        public void CorpseOpeningUsesTheFiveMetreOriginBoundary(float verticalDistance, bool allowed)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            FindMissingSoldiers(harness);
            var corpse = Corpse(harness);
            harness.MovePlayerTo(corpse.Position + new System.Numerics.Vector3(0, verticalDistance, 0));
            harness.Drain();
            var npcs = new NpcManager(harness.Context, harness.Manager);

            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = corpse.EntityId });

            Assert.AreEqual(allowed ? 1 : 0, harness.Drain().OfType<ConversePacket>().Count());
            npcs.CompleteNPCObjective(harness.Client, DecodeContinue(corpse));
            Assert.AreEqual(allowed ? 1 : 0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
        }
    }
}
