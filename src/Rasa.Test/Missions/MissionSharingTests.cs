extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Missions.Definitions;
using Rasa.Missions.Content;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.Missions;
using Rasa.Structures.World;
using Rasa.Test.Missions.Encounters;
using ClientState = RasaGame::Rasa.Data.ClientState;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionSharingTests
    {
        [TestMethod]
        [DataRow("ShareMissionPacket", 547, false)]
        [DataRow("AssignSharedMissionPacket", 409, true)]
        public void NativeSharingRequestsPreserveTupleShapeAndWideActorIdentity(string name, int opcode, bool accept)
        {
            var type = typeof(AssignRadioMissionPacket).Assembly.GetType("Rasa.Packets.MapChannel.Client." + name);
            Assert.IsNotNull(type, "The recovered native sharing request must be implemented.");
            var packet = (ClientPythonPacket)Activator.CreateInstance(type);
            var payload = Write(writer =>
            {
                writer.WriteTuple(accept ? 2 : 1);
                if (accept)
                    writer.WriteULong(0x100000001UL);
                writer.WriteUInt(731);
            });
            using var stream = new MemoryStream(payload);
            using var reader = new BinaryReader(stream);
            packet.Read(new PythonReader(reader));
            Assert.AreEqual((GameOpcode)opcode, packet.Opcode);
            Assert.AreEqual(731U, type.GetProperty("MissionId").GetValue(packet));
            if (accept)
                Assert.AreEqual(0x100000001UL, type.GetProperty("SourcePlayerEntityId").GetValue(packet));
            Assert.AreEqual(stream.Length, stream.Position);
            foreach (var size in new[] { 0, 3 })
            {
                using var wrong = new BinaryReader(new MemoryStream(Write(writer => writer.WriteTuple(size))));
                Assert.ThrowsExactly<InvalidDataException>(() => packet.Read(new PythonReader(wrong)));
            }
        }

        [TestMethod]
        public void NativePartyOfferRequiresExplicitAcceptanceAndCreatesOnlyFreshIndependentProgress()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20));
            var recipient = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(55)));
            var source = context.ReadMission(321);
            using var party = new GroupMissionCreditTests.PartyScope(context.Client, recipient);
            context.Drain();
            context.BeforeSave = _ =>
            {
                Assert.IsFalse(Monitor.IsEntered(context.Client.SyncRoot),
                    "Recipient persistence must not be nested under the sender's mutation lock.");
                Assert.IsEmpty(MissionTestContext.Drain(recipient), "No notification before recipient commit.");
            };

            Route(context, context.Client, new ShareMissionPacket { MissionId = 321 });

            context.BeforeSave = null;
            var offer = MissionTestContext.Drain(recipient).Single(packet => (int)packet.Opcode == 445);
            using (var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(offer)))))
            {
                Assert.AreEqual(3, reader.ReadTuple());
                Assert.AreEqual(context.Client.Player.EntityId, reader.ReadULong(), "Use the actor, not account ID.");
                Assert.AreEqual(321U, reader.ReadUInt());
                Assert.AreEqual(6, reader.ReadTuple(), "Reuse the native six-field MissionInfo offer.");
            }
            using (var unit = context.CreateChar())
            {
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(2, 321));
                var pending = unit.MissionOffers.Read(2, 321);
                Assert.AreEqual(MissionOfferState.Pending, pending.State);
                Assert.AreEqual(source.AssignmentId, pending.SourceAssignmentId);
                Assert.AreEqual(source.Generation, pending.SourceAssignmentGeneration);
            }

            var accept = new AssignSharedMissionPacket
                { SourcePlayerEntityId = context.Client.Player.EntityId, MissionId = 321 };
            Route(context, recipient, accept);
            Route(context, recipient, accept);

            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(0U, recipient.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreNotEqual(source.AssignmentId, recipient.Player.Missions[321].AssignmentId);
            Assert.HasCount(1, MissionTestContext.Drain(recipient).OfType<MissionGainedPacket>().ToArray());
            using var final = context.CreateChar();
            var assignment = final.CharacterMissions.GetByCharacterAndMission(2, 321);
            Assert.AreEqual(1U, assignment.Generation);
            Assert.AreEqual(MissionOfferState.Consumed, final.MissionOffers.Read(2, 321).State);
            Assert.AreEqual(assignment.AssignmentId, final.MissionOffers.Read(2, 321).ConsumedAssignmentId);
        }

        [TestMethod]
        [DataRow(20f, true)]
        [DataRow(20.001f, false)]
        [DataRow(-20f, true)]
        [DataRow(float.NaN, false)]
        [DataRow(float.PositiveInfinity, false)]
        public void SharingUsesTheInclusiveNativeTwentyUnitBoundaryInTheLiveWorld(float distance, bool eligible)
        {
            using var fixture = new SharingFixture();
            fixture.Recipient.Player.Position = fixture.Sender.Player.Position + new Vector3(distance, 0, 0);
            Assert.AreEqual(eligible, fixture.Offer());
            Assert.AreEqual(eligible, fixture.Pending() != null);
            Assert.AreEqual(eligible, fixture.Accept());
        }

        [TestMethod]
        [DataRow("source-dead")]
        [DataRow("recipient-dying")]
        [DataRow("other-instance")]
        [DataRow("source-member-entity")]
        [DataRow("recipient-member-entity")]
        [DataRow("source-character")]
        [DataRow("source-ownership")]
        [DataRow("recipient-ownership")]
        [DataRow("source-runtime")]
        [DataRow("source-history-only")]
        [DataRow("party-id-only")]
        public void UnauthoritativePartyOrActorStateCannotCreateAnOffer(string change)
        {
            using var fixture = new SharingFixture();
            ChangeAuthority(fixture, change);
            Assert.IsFalse(fixture.Offer());
            Assert.IsNull(fixture.Pending());
            Assert.IsFalse(fixture.Accept());
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
        }

        [TestMethod]
        [DataRow("range")]
        [DataRow("source-dead")]
        [DataRow("recipient-dying")]
        [DataRow("other-instance")]
        [DataRow("source-member-entity")]
        [DataRow("recipient-member-entity")]
        [DataRow("source-character")]
        [DataRow("source-ownership")]
        [DataRow("recipient-ownership")]
        [DataRow("source-runtime")]
        [DataRow("source-history-only")]
        [DataRow("source-replacement")]
        [DataRow("source-generation")]
        [DataRow("party-lifetime")]
        [DataRow("leave-rejoin")]
        [DataRow("source-session")]
        [DataRow("recipient-session")]
        [DataRow("source-map-roundtrip")]
        [DataRow("recipient-map-roundtrip")]
        public void AcceptanceRejectsChangedPartySourceAssignmentSessionOrRange(string change)
        {
            using var fixture = new SharingFixture();
            Assert.IsTrue(fixture.Offer());
            fixture.DrainRecipient();
            ChangeAuthority(fixture, change);
            Assert.IsFalse(fixture.Accept());
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            using var unit = fixture.Context.CreateChar();
            Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(2, 321));
            Assert.IsEmpty(fixture.DrainRecipient().OfType<MissionGainedPacket>());
        }

        [TestMethod]
        public void ForgedUnofferedAndWrongChannelCallbacksCannotConsumeAValidPartyOffer()
        {
            using var fixture = new SharingFixture();
            Assert.IsFalse(fixture.Accept());
            Assert.IsTrue(fixture.Offer());
            var offer = fixture.Pending();
            Assert.IsFalse(fixture.Context.Manager.Sharing.TryAccept(fixture.Recipient,
                fixture.Sender.Player.EntityId + 1, 321));
            Assert.IsFalse(fixture.Context.Manager.Sharing.TryAccept(fixture.Recipient,
                fixture.Sender.AccountEntry.Id, 321), "An account ID is not the native actor ID.");
            Assert.IsFalse(fixture.Context.Manager.TryAcceptRadioMission(fixture.Recipient, 321));
            Assert.AreEqual(offer.OfferId, fixture.Pending().OfferId);
            Assert.IsTrue(fixture.Accept());
        }

        [TestMethod]
        public void IdenticalNotificationsDoNotExtendExpiryAndFreshSourcesReplaceOnlyInvalidAuthority()
        {
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            using var fixture = new SharingFixture(utcNow: () => now);
            Assert.IsTrue(fixture.Offer());
            var first = fixture.Pending();
            fixture.DrainRecipient();
            now = now.AddMinutes(4);
            Assert.IsTrue(fixture.Offer());
            Assert.AreEqual(first.OfferId, fixture.Pending().OfferId);
            Assert.AreEqual(first.ExpiresAtUtc, fixture.Pending().ExpiresAtUtc);
            Assert.IsEmpty(fixture.DrainRecipient());
            now = now.AddMinutes(1);
            Assert.IsFalse(fixture.Accept(), "The five-minute boundary is exclusive.");
            Assert.IsTrue(fixture.Offer());
            Assert.AreNotEqual(first.OfferId, fixture.Pending().OfferId);
            var second = fixture.Pending();
            Assert.IsTrue(fixture.Context.Manager.TryAbandon(fixture.Sender, 321));
            Assert.IsTrue(fixture.Context.Manager.AcceptOfferedMission(fixture.Sender, fixture.Giver.EntityId, 321));
            Assert.IsTrue(fixture.Offer());
            Assert.AreNotEqual(second.OfferId, fixture.Pending().OfferId);
            Assert.AreNotEqual(second.SourceAssignmentId, fixture.Pending().SourceAssignmentId);
            Assert.IsTrue(fixture.Accept());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FullRecipientJournalsRejectBothIssuanceAndLaterAcceptance(bool fillAfterOffer)
        {
            using var fixture = new SharingFixture();
            if (fillAfterOffer)
                Assert.IsTrue(fixture.Offer());
            for (uint mission = 1000; mission < 1030; mission++)
                fixture.Context.SeedMission(2, mission, (uint)MissionState.Active, false);
            if (!fillAfterOffer)
                Assert.IsFalse(fixture.Offer());
            Assert.IsFalse(fixture.Accept());
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            Assert.AreEqual(fillAfterOffer, fixture.Pending()?.State == MissionOfferState.Pending);
        }

        [TestMethod]
        [DataRow("offer")]
        [DataRow("accept")]
        public void AFailedRecipientDoesNotRollBackAnotherRecipientsIndependentCommit(string phase)
        {
            using var fixture = new SharingFixture();
            var other = fixture.Context.CreateAdditionalClient(3);
            var party = PartyManager.Instance.PartyOf(fixture.Sender);
            party.Members.Add(new PartyMember(other));
            other.Player.PartyId = party.Id;
            if (phase == "accept")
                Assert.IsTrue(fixture.Offer());
            fixture.Context.BeforeSave = db =>
            {
                if (db.ChangeTracker.Entries<CharacterMissionOfferEntry>().Any(entry =>
                    entry.Entity.CharacterId == 2 && entry.State is EntityState.Added or EntityState.Modified))
                    throw new DbUpdateException("Injected recipient-only persistence failure.");
            };
            if (phase == "offer")
                Assert.IsTrue(fixture.Offer(), "The other recipient's committed offer still succeeds.");
            else
                Assert.IsFalse(fixture.Accept());
            Assert.IsTrue(fixture.Context.Manager.Sharing.TryAccept(other, fixture.Sender.Player.EntityId, 321));
            fixture.Context.BeforeSave = null;
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            Assert.AreNotEqual(other.Player.Missions[321].AssignmentId, fixture.Recipient.Player.Missions[321].AssignmentId);
            using var unit = fixture.Context.CreateChar();
            Assert.AreEqual(MissionOfferState.Consumed, unit.MissionOffers.Read(3, 321).State);
            Assert.AreEqual(MissionOfferState.Consumed, unit.MissionOffers.Read(2, 321).State);
        }

        [TestMethod]
        [DataRow("source-generation")]
        [DataRow("source-ownership")]
        [DataRow("recipient-ownership")]
        [DataRow("range")]
        [DataRow("party-lifetime")]
        [DataRow("journal-growth")]
        public void LateAuthorityChangesRollBackTheAssignmentAndConsumption(string change)
        {
            using var fixture = new SharingFixture();
            Assert.IsTrue(fixture.Offer());
            var offer = fixture.Pending();
            fixture.DrainRecipient();
            var changed = false;
            fixture.Context.AfterSave = db =>
            {
                if (changed || !db.ChangeTracker.Entries<CharacterMissionEntry>()
                    .Any(entry => entry.Entity.CharacterId == 2))
                    return;
                changed = true;
                if (change == "source-generation")
                    db.Database.ExecuteSqlRaw("UPDATE character_mission SET generation = generation + 1 WHERE character_id = 1");
                else if (change.EndsWith("ownership", StringComparison.Ordinal))
                    db.Database.ExecuteSqlRaw("UPDATE character SET account_id = {0}, slot = 1 WHERE id = {1}",
                        change == "source-ownership" ? 2 : 1, change == "source-ownership" ? 1 : 2);
                else if (change == "journal-growth")
                {
                    for (uint mission = 1000; mission < 1030; mission++)
                        db.CharacterMissionEntries.Add(new CharacterMissionEntry(2, mission, (uint)MissionState.Active));
                    db.SaveChanges();
                }
                else
                    ChangeAuthority(fixture, change);
            };
            Assert.IsFalse(fixture.Accept());
            fixture.Context.AfterSave = null;
            Assert.IsTrue(changed);
            Assert.AreEqual(offer.OfferId, fixture.Pending().OfferId);
            Assert.AreEqual(MissionOfferState.Pending, fixture.Pending().State);
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            Assert.IsEmpty(fixture.DrainRecipient().OfType<MissionGainedPacket>());
            if (change is "source-generation" or "source-ownership" or "recipient-ownership" or "journal-growth")
                Assert.IsTrue(fixture.Accept(), "A rolled-back durable source change leaves the same offer retryable.");
        }

        [TestMethod]
        public void PartyAcceptanceUsesCapturedPreconditionsWithoutCopyingFlagsOrRewards()
        {
            using var fixture = new SharingFixture(SharingMission(
                requirement: new NotRequirement(new MissionStateRequirement(321, Accepted: true))));
            using (var unit = fixture.Context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(1, 92, 7));
            fixture.Sender.Player.PlayerFlags[92] = 7;
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept(), "The expected new assignment must not invalidate its own admission predicate.");
            Assert.IsFalse(fixture.Recipient.Player.PlayerFlags.ContainsKey(92));
            using var final = fixture.Context.CreateChar();
            Assert.IsFalse(final.CharacterFlags.Get(2).ContainsKey(92));
            Assert.IsEmpty(final.CharacterMissions.Runtime.History(2));
            Assert.IsFalse(final.CharacterMissions.Runtime.WasRewarded(fixture.Recipient.Player.Missions[321].AssignmentId));
        }

        [TestMethod]
        [DataRow("none")]
        [DataRow("participant")]
        [DataRow("owner")]
        public void SharedEscortHasOneActorAndRunWhileEachAssignmentRetiresIndependently(string retire)
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1),
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.EncounterParticipants, 20));
            var recipient = context.CreateAdditionalClient(2);
            var other = context.CreateAdditionalClient(3);
            var start = SceneNavigationFixture.Attach(context.Map);
            var end = context.Map.NavMesh.Nearest(start + new Vector3(4, 0, 0)).Value;
            context.Client.Player.Position = recipient.Player.Position = other.Player.Position = start;
            var actor = context.AddNpc(77, position: start);
            actor.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            actor.SpawnPool = new SpawnPool
            {
                DbId = 77, MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map, Position = start
            };
            context.Map.SpawnPools.Add(actor.SpawnPool);
            var bindings = new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition> { ["guide"] = new("guide", SceneActorKind.PublicSpawn, 77) },
                new Dictionary<string, SceneRoute>
                {
                    ["outbound"] = new("outbound", new[] { new SceneWaypoint(new ScenePosition(end.X, end.Y, end.Z)) })
                }, new Dictionary<uint, SceneSequence>());
            context.Manager.Scenes.Bind(321, "example.escort", bindings);
            context.Manager.PublicActors.Bind(JsonSerializer.Deserialize<PublicEncounterBinding>(
                "{\"MissionId\":321,\"SpawnId\":77,\"Role\":\"guide\",\"ScriptKey\":\"example.escort\",\"AllowPartyJoin\":true}"));
            using var party = new GroupMissionCreditTests.PartyScope(context.Client, recipient, other);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            var handle = context.Manager.PublicActors.Handle(context.Map, 77);
            var movement = actor.Controller.ScriptedMove;
            Assert.IsNotNull(movement);

            Assert.IsTrue(context.Manager.Sharing.TryShare(context.Client, 321),
                "The party offer must attach to the leased run, not attempt another reservation.");
            Assert.IsTrue(context.Manager.Sharing.TryAccept(recipient, context.Client.Player.EntityId, 321));
            Assert.IsTrue(context.Manager.Sharing.TryAccept(other, context.Client.Player.EntityId, 321));
            Assert.IsFalse(context.Manager.Scenes.Execute(recipient, 321, 0, started: true),
                "A recipient must never acquire the initiator's world control or create another scene.");
            Assert.AreSame(movement, actor.Controller.ScriptedMove);
            Assert.AreEqual(handle, context.Manager.PublicActors.Handle(context.Map, 77));
            using (var unit = context.CreateChar())
            {
                Assert.HasCount(1, unit.CharacterMissions.Runtime.Scenes(
                    Rasa.Game.Missions.World.PublicActorLeaseService.MapKey(context.Map)));
                var participants = unit.CharacterMissions.Runtime.Participants(handle.RunId);
                Assert.HasCount(3, participants);
                foreach (var client in new[] { context.Client, recipient, other })
                {
                    var participant = participants.Single(entry => entry.CharacterId == client.Player.Id);
                    Assert.IsTrue(participant.Active);
                    Assert.AreEqual(client.Player.Missions[321].AssignmentId, participant.AssignmentId);
                    Assert.AreEqual(client.Player.Missions[321].Generation, participant.AssignmentGeneration);
                }
            }
            if (retire != "none")
            {
                Assert.IsTrue(context.Manager.TryAbandon(retire == "owner" ? context.Client : recipient, 321));
                using var unit = context.CreateChar();
                var scene = unit.CharacterMissions.Runtime.ReadScene(handle.RunId);
                Assert.AreEqual(retire == "owner" ? handle.Generation + 1 : handle.Generation, scene.Generation);
                Assert.AreEqual(retire == "owner" ? "Resetting" : "Running", scene.Status);
                Assert.IsFalse(unit.CharacterMissions.Runtime.ReadParticipant(handle.RunId, 2).Active);
                Assert.AreEqual(retire != "owner", unit.CharacterMissions.Runtime.ReadParticipant(handle.RunId, 3).Active);
                if (retire == "participant")
                    Assert.AreSame(movement, actor.Controller.ScriptedMove);
            }
            for (var tick = 0; tick < 60 && !actor.IsInteractable; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(context.Map, 250);
                context.Manager.Scenes.Tick(context.Map);
                context.Manager.Credit.Tick(context.Map);
            }
            Assert.AreEqual(retire == "owner" ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed,
                other.Player.Missions[321].Objectives[1].State);
            if (retire == "none")
                Assert.AreEqual(MissionObjectiveState.Completed, recipient.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(1, context.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Distinct().Count(creature => creature.DbId == 77));
        }

        [TestMethod]
        public void UnsupportedSceneParticipationDoesNotExposeABrokenNativeShareButton()
        {
            using var fixture = new SharingFixture();
            fixture.Context.Manager.Scenes.Bind(321, "data.sequence", new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>()));
            Assert.IsFalse(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable);
            Assert.IsFalse(fixture.Offer());
        }

        [TestMethod]
        public void MigratedPrivateScenesMustBeNonshareableToPassContentValidation()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var definition = harness.WorldContext.MissionContentDefinitionEntries
                .Single(entry => entry.MissionId == 1990 && entry.ContentRevision == "deployment_11");
            definition.Shareable = true;
            harness.WorldContext.SaveChanges();
            var error = Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
            StringAssert.Contains(error.Message, "nonshareable");
        }

        [TestMethod]
        public void FutureEncounterKillsReachOnlyExplicitlyJoinedCurrentAssignments()
        {
            using var fixture = new SharedEncounterFixture(SharingMission(
                credit: new MissionCreditPolicy(MissionCreditMode.EncounterParticipants, 20)));
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            IReadOnlyList<uint> recipients = Array.Empty<uint>();
            using (var unit = fixture.Context.CreateChar())
                unit.ExecuteTransaction(() => recipients = fixture.Context.Manager.Credit.FreezeWorld(unit,
                    fixture.Sender, MissionProgressEvent.Creature(55), fixture.Sender.Player.Position,
                    Guid.NewGuid().ToString("N"), fixture.Run.RunId, fixture.Run.Generation));
            fixture.Context.Manager.Credit.Schedule(recipients);
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(1U, fixture.Recipient.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(1U, fixture.Sender.Player.Missions[321].Objectives[1].Counters[0]);
        }

        [TestMethod]
        public void LateJoinDoesNotReplayPastSignalsAndReceivesOnlyFutureMatchingObjectives()
        {
            using var fixture = new SharedEncounterFixture();
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(1U, fixture.Sender.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.IsTrue(fixture.Accept());
            Assert.AreEqual(0U, fixture.Recipient.Player.Missions[321].Objectives[1].Counters[0]);
            fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1);
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(0U, fixture.Recipient.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 2));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(1U, fixture.Recipient.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(2U, fixture.Sender.Player.Missions[321].Objectives[1].Counters[0]);
        }

        [TestMethod]
        [DataRow("assignment")]
        [DataRow("participant")]
        [DataRow("item")]
        [DataRow("late-participant")]
        [DataRow("late-run")]
        [DataRow("late-inventory")]
        public void SharedAssignmentParticipationAndAcceptanceItemsCommitOrRollBackTogether(string fault)
        {
            var definition = SharingMission().WithItems(
                new[] { new MissionItemBinding("share-tool", 28, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove) },
                new[] { new IssueMissionItemIntent("accept-tool", 321, "share-tool", 28, 1) });
            using var fixture = new SharedEncounterFixture(definition);
            Assert.IsTrue(fixture.Offer());
            MissionTestContext.Drain(fixture.Recipient);
            string offerId;
            using (var unit = fixture.Context.CreateChar())
                offerId = unit.MissionOffers.Read(2, 321).OfferId;
            var injected = false;
            fixture.Context.BeforeSave = db =>
            {
                Assert.IsFalse(Monitor.IsEntered(fixture.Sender.SyncRoot));
                Assert.IsEmpty(MissionTestContext.Drain(fixture.Recipient));
                var failing = fault switch
                {
                    "assignment" => db.ChangeTracker.Entries<CharacterMissionEntry>()
                        .Any(entry => entry.Entity.CharacterId == 2 && entry.State == EntityState.Added),
                    "participant" => db.ChangeTracker.Entries<MissionSceneParticipantEntry>()
                        .Any(entry => entry.Entity.CharacterId == 2 && entry.State == EntityState.Added),
                    "item" => db.ChangeTracker.Entries<CharacterMissionItemEntry>()
                        .Any(entry => entry.Entity.CharacterId == 2 && entry.State == EntityState.Added),
                    _ => false
                };
                if (failing)
                {
                    injected = true;
                    throw new DbUpdateException("Injected shared acceptance " + fault + " failure.");
                }
            };
            fixture.Context.AfterSave = db =>
            {
                if (injected || !fault.StartsWith("late-", StringComparison.Ordinal))
                    return;
                var ownership = db.Set<CharacterMissionItemEntry>().Local.SingleOrDefault(entry => entry.CharacterId == 2);
                if (ownership == null)
                    return;
                injected = true;
                if (fault == "late-participant")
                    db.Database.ExecuteSqlRaw("UPDATE mission_scene_participant SET active = 0 WHERE character_id = 2");
                else if (fault == "late-run")
                    db.Database.ExecuteSqlRaw("UPDATE mission_scene SET generation = generation + 1 WHERE run_id = {0}", fixture.Run.RunId);
                else
                    db.Database.ExecuteSqlRaw("UPDATE character_inventory SET slot_id = 999 WHERE item_id = {0}", ownership.ItemId);
            };

            Assert.IsFalse(fixture.Accept());

            fixture.Context.BeforeSave = null;
            fixture.Context.AfterSave = null;
            Assert.IsTrue(injected);
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            Assert.IsTrue(fixture.Recipient.Player.Inventory.PersonalInventory.All(entityId => entityId == 0));
            using (var unit = fixture.Context.CreateChar())
            {
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(2, 321));
                Assert.IsNull(unit.CharacterMissions.Runtime.ReadParticipant(fixture.Run.RunId, 2));
                Assert.IsEmpty(unit.CharacterMissionItems.GetOwned(2));
                Assert.HasCount(1, unit.CharacterMissionItems.GetOwned(1));
                Assert.AreEqual(fixture.Run.Generation, unit.CharacterMissions.Runtime.ReadScene(fixture.Run.RunId).Generation);
                Assert.AreEqual(offerId, unit.MissionOffers.Read(2, 321).OfferId);
                Assert.AreEqual(MissionOfferState.Pending, unit.MissionOffers.Read(2, 321).State);
            }
            Assert.IsTrue(fixture.Accept());
            Assert.IsFalse(fixture.Accept());
            using var final = fixture.Context.CreateChar();
            var assignment = final.CharacterMissions.GetByCharacterAndMission(2, 321);
            Assert.AreEqual(assignment.AssignmentId, final.CharacterMissionItems.GetOwned(2).Single().AssignmentId);
            Assert.AreEqual(assignment.AssignmentId, final.CharacterMissions.Runtime.ReadParticipant(fixture.Run.RunId, 2).AssignmentId);
            Assert.HasCount(1, final.CharacterMissions.Runtime.Scenes(
                Rasa.Game.Missions.World.PublicActorLeaseService.MapKey(fixture.Context.Map)));
        }

        [TestMethod]
        [DataRow("scene-generation")]
        [DataRow("lease-generation")]
        [DataRow("participant-generation")]
        [DataRow("reset")]
        public void ASharedOfferCannotMoveToADifferentRunOrGeneration(string change)
        {
            using var fixture = new SharedEncounterFixture();
            Assert.IsTrue(fixture.Offer());
            if (change == "reset")
                Assert.IsTrue(fixture.Context.Manager.PublicActors.BeginReset(fixture.Context.Map, fixture.Run.RunId, "Test"));
            else
                using (var unit = fixture.Context.CreateChar())
                    unit.ExecuteTransaction(() =>
                    {
                        if (change == "scene-generation")
                            unit.CharacterMissions.Runtime.Scene(fixture.Run.RunId).Generation++;
                        else if (change == "lease-generation")
                            unit.CharacterMissions.Runtime.Leases(fixture.Run.RunId).Single().Generation++;
                        else
                            unit.CharacterMissions.Runtime.Participants(fixture.Run.RunId)
                                .Single(entry => entry.CharacterId == 1).AssignmentGeneration++;
                    });
            Assert.IsFalse(fixture.Accept());
            using var final = fixture.Context.CreateChar();
            Assert.IsNull(final.CharacterMissions.GetByCharacterAndMission(2, 321));
            Assert.IsNull(final.CharacterMissions.Runtime.ReadParticipant(fixture.Run.RunId, 2));
        }

        [TestMethod]
        public void AParticipantCanShareItsOwnAssignmentWithoutBecomingTheWorldOwner()
        {
            using var fixture = new SharedEncounterFixture();
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            var third = fixture.Context.CreateAdditionalClient(3);
            var party = PartyManager.Instance.PartyOf(fixture.Sender);
            party.Members.Add(new PartyMember(third));
            third.Player.PartyId = party.Id;
            Assert.IsTrue(fixture.Context.Manager.Sharing.TryShare(fixture.Recipient, 321));
            using (var unit = fixture.Context.CreateChar())
            {
                var offer = unit.MissionOffers.Read(3, 321);
                Assert.AreEqual(fixture.Recipient.Player.Missions[321].AssignmentId, offer.SourceAssignmentId);
                Assert.AreEqual(fixture.Run.RunId, offer.SourceInstanceId);
                Assert.AreEqual(fixture.Recipient.Player.EntityId, offer.PartySource.SourceEntityId);
            }
            Assert.IsTrue(fixture.Context.Manager.Sharing.TryAccept(third, fixture.Recipient.Player.EntityId, 321));
            Assert.IsFalse(fixture.Context.Manager.Scenes.Execute(third, 321, 1));
            Assert.IsTrue(fixture.Context.Manager.TryAbandon(fixture.Recipient, 321));
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 2));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(1U, third.Player.Missions[321].Objectives[1].Counters[0]);
            using var final = fixture.Context.CreateChar();
            Assert.AreEqual(1U, final.CharacterMissions.Runtime.ReadScene(fixture.Run.RunId).OwnerCharacterId);
            Assert.IsTrue(final.CharacterMissions.Runtime.ReadParticipant(fixture.Run.RunId, 3).Active);
        }

        [TestMethod]
        public void ReconnectingRecipientsNeedAFreshOfferAndTheOldClientCannotCancelIt()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = SharingMission() });
            var source = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(source, giver.EntityId, 321));
            using var partyScope = new GroupMissionCreditTests.PartyScope(source, context.Client);
            Assert.IsTrue(context.Manager.Sharing.TryShare(source, 321));
            context.Client.State = ClientState.Disconnected;
            var fresh = context.CreateCompetingClient();
            var party = PartyManager.Instance.PartyOf(source);
            fresh.Player.PartyId = party.Id;
            party.Find(fresh.AccountEntry.Id).Refresh(fresh);
            Assert.IsFalse(context.Manager.Sharing.TryAccept(fresh, source.Player.EntityId, 321));
            Assert.IsTrue(context.Manager.Sharing.TryShare(source, 321));
            Assert.IsFalse(context.Manager.Sharing.TryAccept(context.Client, source.Player.EntityId, 321));
            Assert.IsTrue(context.Manager.Sharing.TryAccept(fresh, source.Player.EntityId, 321));
            using var unit = context.CreateChar();
            Assert.AreEqual(fresh.MissionSessionId, unit.MissionOffers.Read(1, 321).SessionId);
            Assert.AreEqual(MissionOfferState.Consumed, unit.MissionOffers.Read(1, 321).State);
        }

        [TestMethod]
        public void ReconnectingSourceRequiresItsNewActorAndMembershipForFreshIssuance()
        {
            using var fixture = new SharingFixture();
            Assert.IsTrue(fixture.Offer());
            var oldActor = fixture.Sender.Player.EntityId;
            var party = PartyManager.Instance.PartyOf(fixture.Sender);
            fixture.Sender.State = ClientState.Disconnected;
            var fresh = fixture.Context.CreateCompetingClient();
            fresh.Player.PartyId = party.Id;
            party.Find(fresh.AccountEntry.Id).Refresh(fresh);
            Assert.IsFalse(fixture.Accept());
            Assert.IsTrue(fixture.Context.Manager.Sharing.TryShare(fresh, 321));
            Assert.IsFalse(fixture.Context.Manager.Sharing.TryAccept(fixture.Recipient, oldActor, 321));
            Assert.IsTrue(fixture.Context.Manager.Sharing.TryAccept(fixture.Recipient, fresh.Player.EntityId, 321));
        }

        [TestMethod]
        public void PartyOffersUseTheSameThirtySlotCapAcrossDifferentSenders()
        {
            var ids = Enumerable.Range(1000, 31).Select(id => (uint)id).ToArray();
            using var context = MissionTestContext.WithDefinitions(ids);
            var recipient = context.CreateAdditionalClient(2);
            var other = context.CreateAdditionalClient(3);
            var giver = context.AddNpc(77);
            using var party = new GroupMissionCreditTests.PartyScope(context.Client, recipient, other);
            foreach (var id in ids.Take(30))
            {
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, id));
                Assert.IsTrue(context.Manager.Sharing.TryShare(context.Client, id));
            }
            Assert.IsTrue(context.Manager.AcceptOfferedMission(other, giver.EntityId, ids.Last()));
            Assert.IsFalse(context.Manager.Sharing.TryShare(other, ids.Last()));
            Assert.HasCount(30, MissionTestContext.Drain(recipient).OfType<DispenseSharedMissionPacket>().ToArray());
            Assert.IsTrue(context.Manager.Sharing.TryShare(context.Client, ids[0]));
            Assert.IsEmpty(MissionTestContext.Drain(recipient));
            using var unit = context.CreateChar();
            Assert.HasCount(30, unit.MissionOffers.ForCharacter(2));
            Assert.IsNull(unit.MissionOffers.Read(2, ids.Last()));
        }

        [TestMethod]
        public void BootcampKeepsPrivateOnceUnshareablePolicyAndDisabledWorldContent()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var ids = new uint[] { 1990, 1992, 1994, 1995, 2005 };
            Assert.IsTrue(harness.BootcampMap.IsPrivateInstance);
            foreach (var id in ids)
            {
                Assert.IsFalse(harness.Manager.LoadedMissions[id].Shareable);
                Assert.AreEqual(MissionRepeatKind.Once, harness.Manager.LoadedMissions[id].RepeatPolicy.Kind);
                Assert.IsFalse(harness.Manager.Sharing.TryShare(harness.Client, id));
            }
            Assert.IsFalse(harness.WorldContext.MissionContentDefinitionEntries.Any(entry => entry.Enabled && !ids.Contains(entry.MissionId)));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void OnlyExplicitlyJoinablePublicScenesAdvertiseSharing(bool allowJoin)
        {
            using var fixture = new SharedEncounterFixture(allowJoin: allowJoin);
            Assert.AreEqual(allowJoin, fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable);
            Assert.AreEqual(allowJoin, fixture.Offer());
        }

        [TestMethod]
        public void RequiredPersonalSceneEventFailsSharingContentValidation()
        {
            var mission = SharingMission(rule: MissionProgressRule.CompleteOnScenarioEvent(321, 9, 55),
                credit: MissionCreditPolicy.Personal);
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                PublicEncounter = new PublicEncounterBinding(321, 77, "guide", "data.sequence", AllowPartyJoin: true)
            };

            var error = Assert.ThrowsExactly<MissionRuleException>(() =>
                Rasa.Game.Missions.Content.MissionSceneValidation.ValidateSharing(mission, scene));

            StringAssert.Contains(error.Message, "nonshareable");
        }

        [TestMethod]
        public void RequiredPersonalSceneEventCannotAdvertiseOrOfferASharedAssignment()
        {
            var mission = SharingMission(rule: MissionProgressRule.CompleteOnScenarioEvent(321, 9, 55),
                credit: MissionCreditPolicy.Personal);
            using var fixture = new SharedEncounterFixture(mission);

            Assert.IsFalse(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable,
                "Joining cannot deliver the initiator's required Personal scene event to this assignment.");
            Assert.IsFalse(fixture.Context.Manager.Sharing.CanShare(mission));
            Assert.IsFalse(fixture.Offer());
            Assert.IsFalse(fixture.Accept());
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
            Assert.AreEqual(MissionObjectiveState.Completed, fixture.Sender.Player.Missions[321].Objectives[1].State);
        }

        [TestMethod]
        public void RequiredPersonalSceneEventCannotBorrowAnotherObjectivesGroupPolicy()
        {
            var groupObjective = SharingMission(
                rule: MissionProgressRule.CompleteOnScenarioEvent(321, 9, 55)).Objectives[1];
            var mission = new Mission(321, "Mixed scene credit", 321, 77, 88, 1, 1, 2, true, false,
                new[] { groupObjective, PersonalSceneSignal(required: true) }, true);
            using var fixture = new SharedEncounterFixture(mission);

            Assert.IsFalse(fixture.Context.Manager.Sharing.CanShare(mission),
                "A matching group rule cannot deliver a different objective's required Personal transition.");
            Assert.IsFalse(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable);
            Assert.IsFalse(fixture.Offer());
        }

        [TestMethod]
        [DataRow(false, MissionActionKind.RevealObjective, false)]
        [DataRow(false, MissionActionKind.ActivateObjective, false)]
        [DataRow(true, MissionActionKind.RevealObjective, false)]
        [DataRow(true, MissionActionKind.ActivateObjective, false)]
        [DataRow(false, MissionActionKind.RevealObjective, true)]
        [DataRow(false, MissionActionKind.ActivateObjective, true)]
        [DataRow(true, MissionActionKind.RevealObjective, true)]
        [DataRow(true, MissionActionKind.ActivateObjective, true)]
        public void OptionalPersonalSceneDependenciesCannotAdvertiseOrAdmitSharedAssignments(
            bool explicitTransitions, MissionActionKind unlock, bool transitive)
        {
            var mission = SceneDependencyMission(explicitTransitions, unlock, transitive);
            using var fixture = new SharedEncounterFixture(mission);
            Assert.AreEqual(unlock == MissionActionKind.RevealObjective ? MissionObjectiveState.Inactive : MissionObjectiveState.NotAssigned,
                fixture.Sender.Player.Missions[321].Objectives[2].State);
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(MissionObjectiveState.Completed, fixture.Sender.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Sender.Player.Missions[321].Objectives[2].State);
            if (transitive)
            {
                Assert.IsTrue(fixture.Context.Manager.RecordProgress(fixture.Sender, MissionProgressEvent.Creature(56)));
                Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Sender.Player.Missions[321].Objectives[3].State);
            }

            var advertised = fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable;
            var capable = fixture.Context.Manager.Sharing.CanShare(mission);
            var offered = fixture.Offer();
            var accepted = fixture.Accept();

            Assert.IsFalse(capable, "The required objective depends on an optional owner-only scene signal.");
            Assert.IsFalse(advertised);
            Assert.IsFalse(offered);
            Assert.IsFalse(accepted);
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            Assert.AreEqual(MissionCreditMode.Personal, mission.Objectives[1].CreditPolicy.Mode);
            Assert.AreEqual(fixture.Run, fixture.Context.Manager.PublicActors.Handle(fixture.Context.Map, 77));
        }

        [TestMethod]
        [DataRow(MissionCreditMode.NearbyParty)]
        [DataRow(MissionCreditMode.EncounterParticipants)]
        public void OptionalGroupSceneDependenciesUnlockOnlyFromFutureSignals(MissionCreditMode mode)
        {
            var mission = SceneDependencyMission(true, MissionActionKind.RevealObjective, true,
                new MissionCreditPolicy(mode, 20));
            using var fixture = new SharedEncounterFixture(mission);
            Assert.IsTrue(fixture.Context.Manager.Sharing.CanShare(mission));
            Assert.IsTrue(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable);
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.IsTrue(fixture.Accept());
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Recipient.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Inactive, fixture.Recipient.Player.Missions[321].Objectives[2].State);
            Assert.AreEqual(MissionObjectiveState.Inactive, fixture.Recipient.Player.Missions[321].Objectives[3].State);
            Assert.IsFalse(fixture.Context.Manager.Scenes.Execute(fixture.Recipient, 321, 2));

            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 2));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Recipient.Player.Missions[321].Objectives[2].State);
            Assert.IsTrue(fixture.Context.Manager.RecordProgress(fixture.Recipient, MissionProgressEvent.Creature(56)));
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Recipient.Player.Missions[321].Objectives[3].State);
            Assert.IsTrue(fixture.Context.Manager.RecordProgress(fixture.Recipient, MissionProgressEvent.Creature(57)));
            Assert.IsTrue(fixture.Recipient.Player.Missions[321].Completeable);
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Sender.Player.Missions[321].Objectives[2].State);
            Assert.AreEqual(mode, mission.Objectives[1].CreditPolicy.Mode);
            Assert.AreEqual(fixture.Run, fixture.Context.Manager.PublicActors.Handle(fixture.Context.Map, 77));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void IndependentlyActiveRequiredProgressDoesNotDependOnOptionalSceneUnlocks(bool transitive)
        {
            var mission = SceneDependencyMission(true, MissionActionKind.RevealObjective, transitive,
                requiredInitiallyActive: true);
            using var fixture = new SharedEncounterFixture(mission);
            Assert.IsTrue(fixture.Context.Manager.Sharing.CanShare(mission));
            Assert.IsTrue(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable);
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(MissionObjectiveState.Completed, fixture.Sender.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Recipient.Player.Missions[321].Objectives[1].State);
            Assert.IsTrue(fixture.Context.Manager.RecordProgress(fixture.Recipient, MissionProgressEvent.Creature(57)));
            Assert.IsTrue(fixture.Recipient.Player.Missions[321].Completeable);
            if (transitive)
                Assert.AreEqual(MissionObjectiveState.Inactive, fixture.Recipient.Player.Missions[321].Objectives[2].State);
            Assert.AreEqual(MissionCreditMode.Personal, mission.Objectives[1].CreditPolicy.Mode);
            Assert.IsFalse(fixture.Context.Manager.Scenes.Execute(fixture.Recipient, 321, 2));
        }

        [TestMethod]
        public void MixedOwnerOnlyDependencyAndAlternateProgressRemainConservativelyUnshareable()
        {
            var mission = SceneDependencyMission(true, MissionActionKind.ActivateObjective, false, alternateProgress: true);
            using var fixture = new SharedEncounterFixture(mission);
            Assert.IsTrue(fixture.Context.Manager.RecordProgress(fixture.Sender, MissionProgressEvent.Creature(58)));
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Sender.Player.Missions[321].Objectives[2].State);
            Assert.IsFalse(fixture.Context.Manager.Sharing.CanShare(mission),
                "An alternate branch does not prove every authored owner-only required-progress path safe to join.");
            Assert.IsFalse(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable);
            Assert.IsFalse(fixture.Offer());
        }

        [TestMethod]
        public void RequiredObjectiveStateDependencyCannotRelyOnAnOptionalOwnerOnlySceneSignal()
        {
            var signal = new MissionObjectiveDefinition(1, 1001, 1002, Array.Empty<uint?>(), 0,
                MissionObjectiveState.Incomplete, false, null, null, Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                MissionProgressRule.CompleteOnScenarioEvent(321, 9, 55));
            var required = new MissionObjectiveDefinition(2, 1001, 1002, Array.Empty<uint?>(), 1,
                MissionObjectiveState.Incomplete, true, null, null, Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                MissionProgressRule.CompleteOnObjectiveState(321, 1, (byte)MissionObjectiveState.Completed));
            var mission = new Mission(321, "Owner-only state dependency", 321, 77, 88, 1, 1, 2, true, false,
                new[] { signal, required }, true);
            using var fixture = new SharedEncounterFixture(mission);
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
            Assert.IsTrue(fixture.Sender.Player.Missions[321].Completeable);
            Assert.IsFalse(fixture.Context.Manager.Sharing.CanShare(mission));
            Assert.IsFalse(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable);
            Assert.IsFalse(fixture.Offer());
        }

        [TestMethod]
        [DataRow(MissionCreditMode.NearbyParty)]
        [DataRow(MissionCreditMode.EncounterParticipants)]
        public void RequiredGroupSceneEventsKeepTheirAuthoredPolicyAndDeliverOnlyFutureSignals(MissionCreditMode mode)
        {
            var mission = SharingMission(rule: MissionProgressRule.CompleteOnScenarioEvent(321, 9, 55),
                credit: new MissionCreditPolicy(mode, 20));
            using var fixture = new SharedEncounterFixture(mission);
            Assert.IsTrue(fixture.Context.Manager.Sharing.CanShare(mission));
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.IsTrue(fixture.Accept());
            Assert.AreEqual(MissionObjectiveState.Completed, fixture.Sender.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Recipient.Player.Missions[321].Objectives[1].State);
            Assert.IsFalse(fixture.Context.Manager.Scenes.Execute(fixture.Recipient, 321, 2));

            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 2));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);

            Assert.AreEqual(MissionObjectiveState.Completed, fixture.Recipient.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(mode, mission.Objectives[1].CreditPolicy.Mode);
            Assert.AreEqual(fixture.Run, fixture.Context.Manager.PublicActors.Handle(fixture.Context.Map, 77));
        }

        [TestMethod]
        [DataRow(true, MissionCreditMode.Personal, false, false, MissionActionKind.ActivateObjective, false)]
        [DataRow(false, MissionCreditMode.Personal, true, false, MissionActionKind.ActivateObjective, false)]
        [DataRow(true, MissionCreditMode.NearbyParty, true, false, MissionActionKind.ActivateObjective, false)]
        [DataRow(true, MissionCreditMode.EncounterParticipants, true, false, MissionActionKind.ActivateObjective, false)]
        [DataRow(false, MissionCreditMode.Personal, false, true, MissionActionKind.RevealObjective, false)]
        [DataRow(false, MissionCreditMode.Personal, false, true, MissionActionKind.ActivateObjective, false)]
        [DataRow(false, MissionCreditMode.Personal, false, true, MissionActionKind.RevealObjective, true)]
        [DataRow(false, MissionCreditMode.Personal, false, true, MissionActionKind.ActivateObjective, true)]
        [DataRow(false, MissionCreditMode.NearbyParty, true, true, MissionActionKind.RevealObjective, true)]
        [DataRow(false, MissionCreditMode.EncounterParticipants, true, true, MissionActionKind.ActivateObjective, true)]
        public void MigratedJoinableScenesValidateAfterApplyingAuthoredCredit(
            bool required, MissionCreditMode mode, bool shareable, bool dependency, MissionActionKind unlock, bool transitive)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var world = harness.WorldContext;
            const string revision = "p8-sharing-validation";
            var definition = new MissionContentDefinitionEntry
            {
                MissionId = 339, ContentRevision = revision, Enabled = true, Shareable = true,
                ClientNameTextId = 4318, GiverId = 510206, ReceiverId = 510207,
                Level = 1, GroupType = 1, CategoryId = 1, Comment = "Isolated sharing capability fixture"
            };
            world.Add(definition);
            var objectiveCount = transitive ? 3U : 2U;
            for (uint id = 1; id <= objectiveCount; id++)
            {
                world.Add(new MissionObjectiveDefinitionEntry
                {
                    MissionId = 339, ContentRevision = revision, ObjectiveId = id,
                    ClientNameTextId = 4318, ClientBodyTextId = 4318, Ordinal = id,
                    InitialState = (byte)(id == 1 || !dependency ? MissionObjectiveState.Incomplete :
                        unlock == MissionActionKind.RevealObjective ? MissionObjectiveState.Inactive : MissionObjectiveState.NotAssigned),
                    IsRequired = id == objectiveCount || id == 1 && required
                });
                world.Add(new MissionObjectiveTransitionEntry
                {
                    MissionId = 339, ContentRevision = revision, ObjectiveId = id, TransitionId = 1, Sequence = 1,
                    FromState = (byte)MissionObjectiveState.Incomplete, ToState = (byte)MissionObjectiveState.Completed
                });
                world.Add(new MissionTriggerEntry
                {
                    MissionId = 339, ContentRevision = revision, ObjectiveId = id, TransitionId = 1,
                    TriggerId = 1, Sequence = 1, Kind = MissionTriggerKind.ProgressEvent,
                    EventKind = (byte)(id == 1 ? MissionProgressEventKind.ScenarioEvent : MissionProgressEventKind.CreatureKilled),
                    SubjectId = id == 1 ? 55U : 510210U, CounterId = id == 1 ? 9U : null
                });
                if (dependency && id < objectiveCount)
                {
                    world.Add(new MissionActionEntry
                    {
                        MissionId = 339, ContentRevision = revision, ObjectiveId = id, TransitionId = 1,
                        ActionId = 1, Sequence = 1, Kind = unlock, TargetObjectiveId = id + 1,
                        ObjectiveState = unlock == MissionActionKind.ActivateObjective ? (byte?)MissionObjectiveState.Incomplete : null
                    });
                    if (unlock == MissionActionKind.RevealObjective)
                        world.Add(new MissionActionEntry
                        {
                            MissionId = 339, ContentRevision = revision, ObjectiveId = id, TransitionId = 1,
                            ActionId = 2, Sequence = 2, Kind = MissionActionKind.ActivateObjective, TargetObjectiveId = id + 1,
                            ObjectiveState = (byte)MissionObjectiveState.Incomplete
                        });
                }
            }
            world.Add(new MissionRewardDefinitionEntry { MissionId = 339, ContentRevision = revision, RewardId = 1 });
            world.Add(new MissionActionEntry
            {
                MissionId = 339, ContentRevision = revision, ObjectiveId = objectiveCount, TransitionId = 1,
                ActionId = 1, Sequence = 1, Kind = MissionActionKind.GrantReward, RewardId = 1
            });
            world.Add(new MissionScenarioEntry
            {
                MissionId = 339, ContentRevision = revision, ScenarioId = 9,
                Name = "Future scene signal", StartPolicy = MissionScenarioStartPolicy.Automatic
            });
            world.Add(new MissionScenarioStepEntry
            {
                MissionId = 339, ContentRevision = revision, ScenarioId = 9, StepId = 1, Sequence = 1,
                Kind = MissionScenarioStepKind.EmitScenarioEvent, ScenarioEventId = 55
            });
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Actors = new() { ["guide"] = new("guide", SceneActorKind.PublicSpawn, 510206) },
                PublicEncounter = new PublicEncounterBinding(339, 510206, "guide", "data.sequence", AllowPartyJoin: true),
                Sequences = new() { [9] = new() { Signals = new() { new(339, 9, 55) } } }
            };
            if (mode != MissionCreditMode.Personal)
                scene.Credit[1] = new MissionCreditPolicy(mode, 20);
            world.Add(new MissionSceneBindingEntry
            {
                MissionId = 339, ContentRevision = revision, ScriptKey = scene.Script,
                Bindings = JsonSerializer.Serialize(scene, MissionContentCodec.Options)
            });
            world.SaveChanges();

            if (!shareable)
            {
                var error = Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
                StringAssert.Contains(error.Message, "required owner-only scene events");
                definition.Shareable = false;
                world.SaveChanges();
            }
            var report = harness.Manager.LoadMissions();
            Assert.IsFalse(report.BlocksReadiness, string.Join("; ", report.Diagnostics.Select(entry => entry.Message)));
            var loaded = harness.Manager.LoadedMissions[339];
            Assert.IsTrue(loaded.IsOperational);
            Assert.AreEqual(mode, loaded.Objectives[1].CreditPolicy.Mode);
            Assert.AreEqual(MissionCreditMode.Personal, loaded.Objectives[2].CreditPolicy.Mode);
            Assert.AreEqual(shareable, harness.Manager.Sharing.CanShare(loaded));
        }

        [TestMethod]
        [DataRow(MissionActionKind.StartScenario)]
        [DataRow(MissionActionKind.ActivateSpawnGroup)]
        public void ParticipantTransitionsNeedingWorldControlMustValidateAsNonshareable(MissionActionKind kind)
        {
            var action = new MissionActionDefinition
            {
                Kind = kind, Sequence = 1, ScenarioId = kind == MissionActionKind.StartScenario ? 1U : null,
                SpawnGroupId = kind == MissionActionKind.ActivateSpawnGroup ? 1U : null
            };
            using var fixture = new SharedEncounterFixture(DialogueMission(action));
            Assert.IsFalse(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable,
                "A participant cannot complete a transition that requires its own world scene.");
            Assert.IsFalse(fixture.Offer());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ParticipantDialogueChangesOnlyItsOwnObjectiveAndFlagsNotTheOwnersWorldRun(bool optionalSceneEvent)
        {
            using var fixture = new SharedEncounterFixture(DialogueMission(new MissionActionDefinition
                { Kind = MissionActionKind.SetPlayerFlag, Sequence = 1, PlayerFlagId = 92, PlayerFlagValue = 2 }, optionalSceneEvent));
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            if (optionalSceneEvent)
            {
                Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
                fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
                Assert.AreEqual(MissionObjectiveState.Completed, fixture.Sender.Player.Missions[321].Objectives[2].State);
                Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Recipient.Player.Missions[321].Objectives[2].State);
            }
            long version;
            int effects;
            using (var unit = fixture.Context.CreateChar())
            {
                version = unit.CharacterMissions.Runtime.ReadScene(fixture.Run.RunId).Version;
                effects = unit.CharacterMissions.Runtime.Effects(fixture.Run.RunId).Count;
            }
            var npc = fixture.Context.AddNpc(88, npcPackageId: 586);
            var manager = new NpcManager(fixture.Context, fixture.Context.Manager);
            manager.RequestNpcConverse(fixture.Recipient, new RequestNPCConversePacket { EntityId = npc.EntityId });
            Assert.IsNotNull(fixture.Recipient.MissionConversation);
            manager.CompleteNPCObjective(fixture.Recipient, new CompleteNPCObjectivePacket
                { EntityId = npc.EntityId, MissionId = 321, ObjectiveId = 1, PlayerFlagId = 1 });
            Assert.AreEqual(MissionObjectiveState.Completed, fixture.Recipient.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Sender.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(2U, fixture.Recipient.Player.PlayerFlags[92]);
            Assert.IsFalse(fixture.Sender.Player.PlayerFlags.ContainsKey(92));
            using var final = fixture.Context.CreateChar();
            Assert.AreEqual(version, final.CharacterMissions.Runtime.ReadScene(fixture.Run.RunId).Version);
            Assert.HasCount(effects, final.CharacterMissions.Runtime.Effects(fixture.Run.RunId));
            Assert.IsEmpty(final.CharacterMissions.Runtime.Scenes(2, 321));
        }

        [TestMethod]
        public void IndependentPersonalDialogueCanUnlockRequiredProgressBesideAnOptionalSceneSignal()
        {
            var dialogue = DialogueMission(new MissionActionDefinition
            {
                Kind = MissionActionKind.ActivateObjective, Sequence = 1, TargetObjectiveId = 3,
                ObjectiveStateValue = (byte)MissionObjectiveState.Incomplete
            }, optionalSceneEvent: true);
            var required = new MissionObjectiveDefinition(3, 1001, 1002, Array.Empty<uint?>(), 2,
                MissionObjectiveState.NotAssigned, true, null, null, Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 57));
            var mission = new Mission(321, "Independent dialogue dependency", 321, 77, 88, 1, 1, 2, true, false,
                dialogue.Objectives.Values.Append(required), true, dialogue: dialogue.Dialogue);
            using var fixture = new SharedEncounterFixture(mission);
            Assert.IsTrue(fixture.Context.Manager.Sharing.CanShare(mission));
            Assert.IsTrue(fixture.Context.Manager.BuildStatusSnapshot(fixture.Sender.Player)[321].MissionConstantData.Shareable);
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            var npc = fixture.Context.AddNpc(88, npcPackageId: 586);
            var manager = new NpcManager(fixture.Context, fixture.Context.Manager);
            manager.RequestNpcConverse(fixture.Recipient, new RequestNPCConversePacket { EntityId = npc.EntityId });
            Assert.IsNotNull(fixture.Recipient.MissionConversation);
            manager.CompleteNPCObjective(fixture.Recipient, new CompleteNPCObjectivePacket
                { EntityId = npc.EntityId, MissionId = 321, ObjectiveId = 1, PlayerFlagId = 1 });
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Recipient.Player.Missions[321].Objectives[3].State);
            Assert.IsTrue(fixture.Context.Manager.RecordProgress(fixture.Recipient, MissionProgressEvent.Creature(57)));
            Assert.IsTrue(fixture.Recipient.Player.Missions[321].Completeable);
            Assert.AreEqual(MissionObjectiveState.NotAssigned, fixture.Sender.Player.Missions[321].Objectives[3].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, fixture.Recipient.Player.Missions[321].Objectives[2].State);
            Assert.IsFalse(fixture.Context.Manager.Scenes.Execute(fixture.Recipient, 321, 1));
            Assert.AreEqual(fixture.Run, fixture.Context.Manager.PublicActors.Handle(fixture.Context.Map, 77));
        }

        [TestMethod]
        [DataRow(MissionRepeatKind.Once)]
        [DataRow(MissionRepeatKind.Immediate)]
        [DataRow(MissionRepeatKind.Cooldown)]
        [DataRow(MissionRepeatKind.Daily)]
        public void PartyAcceptanceAndIndependentRewardsReuseRepeatPolicyAndAssignmentHistory(MissionRepeatKind kind)
        {
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            var policy = new MissionRepeatPolicy(kind, kind == MissionRepeatKind.Cooldown ? 60U : null,
                kind == MissionRepeatKind.Daily ? 0U : null);
            using var fixture = new SharingFixture(SharingMission(repeat: policy), () => now);
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            var first = fixture.Recipient.Player.Missions[321];
            for (var count = 0; count < 5; count++)
                Assert.IsTrue(fixture.Context.Manager.RecordProgress(fixture.Recipient, MissionProgressEvent.Creature(55)));
            var receiver = fixture.Context.AddNpc(88);
            Assert.IsTrue(fixture.Context.Manager.CompleteOfferedMission(fixture.Recipient, receiver.EntityId, 321, null, null));
            Assert.AreEqual(kind == MissionRepeatKind.Immediate, fixture.Offer());
            now = kind == MissionRepeatKind.Cooldown ? now.AddSeconds(60) : now.AddDays(1);
            Assert.AreEqual(kind != MissionRepeatKind.Once, fixture.Offer());
            if (kind != MissionRepeatKind.Once)
            {
                Assert.IsTrue(fixture.Accept());
                Assert.AreEqual(first.Generation + 1, fixture.Recipient.Player.Missions[321].Generation);
                Assert.AreNotEqual(first.AssignmentId, fixture.Recipient.Player.Missions[321].AssignmentId);
                Assert.AreEqual(0U, fixture.Recipient.Player.Missions[321].Objectives[1].Counters[0]);
            }
            using var unit = fixture.Context.CreateChar();
            Assert.HasCount(1, unit.CharacterMissions.Runtime.History(2));
            Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(first.AssignmentId));
            Assert.IsEmpty(unit.CharacterMissions.Runtime.History(1));
        }

        [TestMethod]
        public void RejoiningTheSameRunUsesANewAssignmentAndExpiresPreviouslyFrozenCredit()
        {
            var definition = SharingMission(
                rule: MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.ScenarioEvent, 55, 0, 0, 5),
                credit: new MissionCreditPolicy(MissionCreditMode.EncounterParticipants, 20),
                repeat: new(MissionRepeatKind.Immediate));
            using var fixture = new SharedEncounterFixture(definition);
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            var first = fixture.Recipient.Player.Missions[321];
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 1));
            for (var count = 0; count < 5; count++)
                Assert.IsTrue(fixture.Context.Manager.RecordProgress(fixture.Recipient, MissionProgressEvent.Scenario(321, 9, 55)));
            Assert.IsTrue(fixture.Context.Manager.CompleteOfferedMission(fixture.Recipient,
                fixture.Context.AddNpc(88).EntityId, 321, null, null));
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            var current = fixture.Recipient.Player.Missions[321];
            Assert.AreNotEqual(first.AssignmentId, current.AssignmentId);
            Assert.AreEqual(first.Generation + 1, current.Generation);
            Assert.AreEqual(0U, current.Objectives[1].Counters[0]);
            Assert.AreEqual(fixture.Run, fixture.Context.Manager.PublicActors.Handle(fixture.Context.Map, 77));
            using (var db = fixture.Context.Open())
            {
                Assert.AreEqual("Expired", db.Set<MissionCreditDeliveryEntry>()
                    .Single(entry => entry.CharacterId == 2 && entry.AssignmentId == first.AssignmentId).Status);
                Assert.AreEqual(current.AssignmentId, db.Set<MissionSceneParticipantEntry>()
                    .Single(entry => entry.CharacterId == 2).AssignmentId);
            }
            Assert.IsTrue(fixture.Context.Manager.Scenes.Execute(fixture.Sender, 321, 2));
            fixture.Context.Manager.Credit.Tick(fixture.Context.Map);
            Assert.AreEqual(1U, fixture.Recipient.Player.Missions[321].Objectives[1].Counters[0]);
        }

        [TestMethod]
        [DataRow(MissionState.Active)]
        [DataRow(MissionState.Success)]
        public void DurableActiveOrUnsettledAssignmentsCannotReceiveAnotherSharedOffer(MissionState state)
        {
            using var fixture = new SharingFixture();
            fixture.Context.SeedMission(2, 321, (uint)state, false);
            Assert.IsFalse(fixture.Offer());
            Assert.IsFalse(fixture.Accept());
            Assert.IsNull(fixture.Pending());
            using var unit = fixture.Context.CreateChar();
            Assert.AreEqual((uint)state, unit.CharacterMissions.GetByCharacterAndMission(2, 321).MissionState);
        }

        [TestMethod]
        public void SharedOfferEncodingUsesWideActorThenMissionThenTheExistingSixFieldOffer()
        {
            var info = new MissionInfo { MissionConstantData = new() { Level = 1, GroupType = 2 } };
            info.ItemRequired.Add(3147);
            var packet = new DispenseSharedMissionPacket(0x100000001UL, 321, info);
            var expected = Write(writer =>
            {
                writer.WriteTuple(3);
                writer.WriteULong(0x100000001UL);
                writer.WriteUInt(321);
                writer.WriteTuple(6);
                writer.WriteUInt(1);
                writer.WriteTuple(2);
                writer.WriteTuple(2);
                writer.WriteList(0);
                writer.WriteList(0);
                writer.WriteList(0);
                writer.WriteNoneStruct();
                writer.WriteList(1);
                writer.WriteInt(3147);
                writer.WriteList(0);
                writer.WriteUInt(2);
            });
            Assert.AreEqual(445, (int)packet.Opcode);
            CollectionAssert.AreEqual(expected, MissionTestContext.Encode(packet));
        }

        [TestMethod]
        public void SharingPacketsRejectWrongNativeScalarKindsInsteadOfCoercingThem()
        {
            foreach (var scalar in new Action<PythonWriter>[]
            {
                writer => writer.WriteNoneStruct(), writer => writer.WriteTrueStruct(),
                writer => writer.WriteZeroStruct(), writer => writer.WriteLong(321),
                writer => writer.WriteUnicodeString("321")
            })
            {
                foreach (var sharedAccept in new[] { false, true })
                {
                    var packet = sharedAccept ? (ClientPythonPacket)new AssignSharedMissionPacket() : new ShareMissionPacket();
                    using var reader = new PythonReader(new BinaryReader(new MemoryStream(Write(writer =>
                    {
                        writer.WriteTuple(sharedAccept ? 2 : 1);
                        if (sharedAccept)
                            writer.WriteULong(0x100000001UL);
                        scalar(writer);
                    }))));
                    Assert.ThrowsExactly<InvalidDataException>(() => packet.Read(reader));
                }
            }
            using var intActor = new PythonReader(new BinaryReader(new MemoryStream(Write(writer =>
            {
                writer.WriteTuple(2);
                writer.WriteUInt(1);
                writer.WriteUInt(321);
            }))));
            Assert.ThrowsExactly<InvalidDataException>(() => new AssignSharedMissionPacket().Read(intActor));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CharacterSchemaIncludesNullablePartySourceIdentity(bool mysql)
        {
            Migration migration = mysql ? new Rasa.Migrations.MySqlChar.ConsolidatedCharacterSchema() :
                new Rasa.Migrations.SqliteChar.ConsolidatedCharacterSchema();
            var table = migration.UpOperations.OfType<CreateTableOperation>()
                .Single(operation => operation.Name == "character_mission_offer");
            var column = table.Columns.Single(operation => operation.Name == "party_source");
            Assert.AreEqual("text", column.ColumnType);
            Assert.IsTrue(column.IsNullable);
            Assert.IsTrue(migration.DownOperations.OfType<DropTableOperation>()
                .Any(operation => operation.Name == "character_mission_offer"));
        }

        [TestMethod]
        public void ReinitializationPreservesOffersAssignmentsObjectivesHistoryAndReceipts()
        {
            using var context = new MissionTestContext();
            context.SeedCharacter(1, 0, 1);
            var assignment = new CharacterMissionEntry(1, 321, (uint)MissionState.Completed)
                { ContentRevision = "p7-preserved", Generation = 7, Completeable = true };
            var offerId = Guid.NewGuid().ToString("N");
            var session = Guid.NewGuid();
            var playerEpoch = Guid.NewGuid();
            var mapEpoch = Guid.NewGuid();
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            using var db = context.Open();
            db.Add(assignment);
            db.Add(new CharacterMissionObjectiveEntry(1, 321, 1, (byte)MissionObjectiveState.Completed));
            db.Add(new CharacterMissionHistoryEntry
            {
                CharacterId = 1, MissionId = 321, AssignmentId = assignment.AssignmentId, AssignmentGeneration = 7,
                ContentRevision = assignment.ContentRevision, CompletedAtUtc = now, RewardedAtUtc = now,
                Outcome = (uint)MissionState.Completed, Rewarded = true
            });
            db.Add(new MissionReceiptEntry
            {
                OwnerId = assignment.AssignmentId, Generation = 7, OperationKey = "mission-reward",
                Kind = "Reward", CreatedAtUtc = now
            });
            db.SaveChanges();
            db.Database.ExecuteSqlInterpolated($@"
                INSERT INTO character_mission_offer
                (character_id, mission_id, offer_id, content_revision, account_id, player_entity_id,
                 session_id, player_epoch, map_epoch, source_kind, source_key, source_instance_id,
                 source_generation, source_assignment_generation, prior_assignment_generation,
                 created_at_utc, expires_at_utc, state, consumed_assignment_generation, version)
                VALUES (1, 999, {offerId}, 'p7-preserved', 1, {0x100000001UL},
                        {session}, {playerEpoch}, {mapEpoch}, 0, 'fixture', 'fixture',
                        0, 0, 0, {now}, {now.AddMinutes(5)}, 0, 0, 1)");
            var before = JsonSerializer.Serialize(new
            {
                Assignments = db.CharacterMissionEntries.AsNoTracking().ToArray(),
                Objectives = db.CharacterMissionObjectiveEntries.AsNoTracking().ToArray(),
                History = db.Set<CharacterMissionHistoryEntry>().AsNoTracking().ToArray(),
                Receipts = db.Set<MissionReceiptEntry>().AsNoTracking().ToArray()
            });

            db.GetService<IMigrator>().Migrate();

            Assert.AreEqual(before, JsonSerializer.Serialize(new
            {
                Assignments = db.CharacterMissionEntries.AsNoTracking().ToArray(),
                Objectives = db.CharacterMissionObjectiveEntries.AsNoTracking().ToArray(),
                History = db.Set<CharacterMissionHistoryEntry>().AsNoTracking().ToArray(),
                Receipts = db.Set<MissionReceiptEntry>().AsNoTracking().ToArray()
            }));
            var offer = db.CharacterMissionOfferEntries.Single();
            Assert.AreEqual(offerId, offer.OfferId);
            Assert.AreEqual(MissionOfferSourceKind.ServerEvent, offer.SourceKind);
            Assert.AreEqual(MissionOfferState.Pending, offer.State);
            Assert.IsNull(offer.PartySource);
            Assert.AreEqual(now.AddMinutes(5), offer.ExpiresAtUtc);
            var source = new MissionPartyOfferSource(9, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                1, 1, 0xfedcba9876543210UL, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            offer.PartySource = source;
            db.SaveChanges();
            db.ChangeTracker.Clear();
            Assert.AreEqual(source, db.CharacterMissionOfferEntries.Single().PartySource,
                "Persisting the party descriptor must not narrow its unsigned actor ID.");
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void FinalSourceReaderCannotCrossExpiryOrTheCapturedSourceSession(bool accept, bool sessionChange)
        {
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            using var fixture = new SharingFixture(utcNow: () => now);
            if (accept)
                Assert.IsTrue(fixture.Offer());
            var saved = false;
            var owners = 0;
            var changed = false;
            fixture.Context.AfterSave = _ => saved = true;
            fixture.Context.AfterCommand = sql =>
            {
                if (!saved || changed || !sql.Contains("EXISTS", StringComparison.Ordinal) ||
                    !sql.Contains("FROM \"character\"", StringComparison.Ordinal) || ++owners != 2)
                    return;
                changed = true;
                if (sessionChange)
                    fixture.Sender.InvalidateMissionSession();
                else
                    now = now.AddMinutes(5);
            };
            Assert.IsFalse(accept ? fixture.Accept() : fixture.Offer());
            fixture.Context.AfterSave = null;
            fixture.Context.AfterCommand = null;
            Assert.IsTrue(changed, "Change the clock/session after the final source ownership reader, not before planning.");
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            Assert.AreEqual(accept, fixture.Pending()?.State == MissionOfferState.Pending);
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
        }

        [TestMethod]
        public void AssignmentOwnedSceneClocksCannotBeDeclaredJoinable()
        {
            var mission = SharingMission(rule: MissionProgressRule.CompleteOnDeadlineElapsed(321, 1, 60));
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                PublicEncounter = new PublicEncounterBinding(321, 77, "guide", "data.sequence", AllowPartyJoin: true)
            };
            Assert.ThrowsExactly<MissionRuleException>(() =>
                Rasa.Game.Missions.Content.MissionSceneValidation.ValidateSharing(mission, scene));
        }

        [TestMethod]
        public void SourceCharacterRemovalDuringItsDurableReadRejectsWithoutThrowingOrGranting()
        {
            using var fixture = new SharingFixture();
            Assert.IsTrue(fixture.Offer());
            var original = fixture.Sender.Player;
            var removed = false;
            fixture.Context.AfterCommand = sql =>
            {
                if (!removed && sql.Contains("FROM \"character_mission\"", StringComparison.Ordinal))
                {
                    removed = true;
                    fixture.Sender.Player = null;
                }
            };
            try
            {
                Assert.IsFalse(fixture.Accept());
            }
            finally
            {
                fixture.Context.AfterCommand = null;
                fixture.Sender.Player = original;
            }
            Assert.IsTrue(removed);
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
        }

        [TestMethod]
        public void SharedSourceRemovalAfterOwnershipReadCannotCrashRunValidation()
        {
            using var fixture = new SharedEncounterFixture();
            Assert.IsTrue(fixture.Offer());
            var original = fixture.Sender.Player;
            var reads = 0;
            var removed = false;
            fixture.Context.AfterCommand = sql =>
            {
                if (!removed && sql.Contains("EXISTS", StringComparison.Ordinal) &&
                    sql.Contains("FROM \"character\"", StringComparison.Ordinal) && ++reads == 2)
                {
                    removed = true;
                    fixture.Sender.Player = null;
                }
            };
            try
            {
                Assert.IsFalse(fixture.Accept());
            }
            finally
            {
                fixture.Context.AfterCommand = null;
                fixture.Sender.Player = original;
            }
            Assert.IsTrue(removed);
            Assert.IsFalse(fixture.Recipient.Player.Missions.ContainsKey(321));
            Assert.IsTrue(fixture.Offer());
            Assert.IsTrue(fixture.Accept());
        }

        private static MissionObjectiveDefinition PersonalSceneSignal(bool required) =>
            new(2, 1001, 1002, Array.Empty<uint?>(), 1, MissionObjectiveState.Incomplete, required,
                null, null, Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(), executableTransitions: new[]
                {
                    new MissionObjectiveExecutableTransition(1, 1, MissionObjectiveState.Completed, null,
                        MissionProgressRule.CompleteOnScenarioEvent(321, 9, 55), null, null, null)
                });

        private static Mission SceneDependencyMission(
            bool explicitTransitions, MissionActionKind unlock, bool transitive, MissionCreditPolicy credit = null,
            bool requiredInitiallyActive = false, bool alternateProgress = false)
        {
            var objectives = new List<MissionObjectiveDefinition>();
            var objectiveCount = transitive ? 3U : 2U;
            for (uint id = 1; id <= objectiveCount; id++)
            {
                var rule = id == 1 ? MissionProgressRule.CompleteOnScenarioEvent(321, 9, 55) :
                    MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled,
                        id == objectiveCount ? 57U : 56U);
                var targets = id < objectiveCount ? new[] { id + 1 } : Array.Empty<uint>();
                var actions = targets.SelectMany(target => unlock == MissionActionKind.RevealObjective
                    ? new[]
                    {
                        new MissionActionDefinition { Kind = unlock, Sequence = 1, TargetObjectiveId = target },
                        new MissionActionDefinition { Kind = MissionActionKind.ActivateObjective, Sequence = 2, TargetObjectiveId = target }
                    }
                    : new[] { new MissionActionDefinition { Kind = unlock, Sequence = 1, TargetObjectiveId = target } }).ToArray();
                var transitions = new List<MissionObjectiveExecutableTransition>
                {
                    new(1, 1, MissionObjectiveState.Completed, null, rule, null, null, actions)
                };
                if (id == 1 && alternateProgress)
                    transitions.Add(new MissionObjectiveExecutableTransition(2, 2, MissionObjectiveState.Completed,
                        null, MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 58),
                        null, null, actions));
                objectives.Add(new MissionObjectiveDefinition(id, 1001, 1002, Array.Empty<uint?>(), id - 1,
                    id == 1 || id == objectiveCount && requiredInitiallyActive ? MissionObjectiveState.Incomplete :
                        unlock == MissionActionKind.RevealObjective ? MissionObjectiveState.Inactive : MissionObjectiveState.NotAssigned,
                    id == objectiveCount, null, null, Array.Empty<MissionObjectiveConversation>(),
                    !explicitTransitions && unlock == MissionActionKind.RevealObjective ? targets : Array.Empty<uint>(),
                    !explicitTransitions ? targets : Array.Empty<uint>(),
                    Array.Empty<MissionIndicator>(), explicitTransitions ? null : rule,
                    explicitTransitions ? transitions : null, creditPolicy: id == 1 ? credit : null));
            }
            return new Mission(321, "Optional scene dependency", 321, 77, 88, 1, 1, 2, true, false, objectives, true);
        }

        private static Mission DialogueMission(MissionActionDefinition action, bool optionalSceneEvent = false)
        {
            var conversations = new[] { new MissionObjectiveConversation(586, 1, MissionObjectiveConversationType.Completion) };
            var transition = new MissionObjectiveExecutableTransition(1, 1, MissionObjectiveState.Completed,
                conversations, null, null, null, new[] { action });
            var objective = new MissionObjectiveDefinition(1, 1001, 1002, Array.Empty<uint?>(), 0,
                MissionObjectiveState.Incomplete, true, null, null, conversations, Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(), executableTransitions: new[] { transition });
            var objectives = new List<MissionObjectiveDefinition> { objective };
            if (optionalSceneEvent)
                objectives.Add(PersonalSceneSignal(required: false));
            return new Mission(321, "Shared personal dialogue", 321, 77, 88, 1, 1, 2, true, false,
                objectives, true,
                dialogue: new[] { new MissionDialogueTopicDefinition(1, 586, 1, transitionId: 1) });
        }

        private static void ChangeAuthority(SharingFixture fixture, string change)
        {
            var source = fixture.Sender;
            var recipient = fixture.Recipient;
            var party = PartyManager.Instance.PartyOf(source);
            switch (change)
            {
                case "range": recipient.Player.Position += new Vector3(21, 0, 0); break;
                case "source-dead": source.Player.State = CharacterState.Dead; break;
                case "recipient-dying": recipient.Player.State = CharacterState.Dying; break;
                case "source-member-entity": party.Find(source.AccountEntry.Id).EntityId++; break;
                case "recipient-member-entity": party.Find(recipient.AccountEntry.Id).EntityId++; break;
                case "source-character": source.Player.Id = 3; break;
                case "source-runtime":
                    source.Player.Missions[321] = new MissionLog(321, MissionState.Active, false,
                        source.Player.Missions[321].Objectives);
                    break;
                case "source-history-only":
                case "source-replacement":
                    Assert.IsTrue(fixture.Context.Manager.TryAbandon(source, 321));
                    if (change == "source-replacement")
                        Assert.IsTrue(fixture.Context.Manager.AcceptOfferedMission(source, fixture.Giver.EntityId, 321));
                    break;
                case "source-generation":
                    using (var unit = fixture.Context.CreateChar())
                        unit.ExecuteTransaction(() => unit.CharacterMissions.GetByCharacterAndMission(1, 321).Generation++);
                    break;
                case "source-ownership":
                case "recipient-ownership":
                    using (var db = fixture.Context.Open())
                    {
                        var character = db.CharacterEntries.Single(entry => entry.Id == (change == "source-ownership" ? 1 : 2));
                        character.AccountId = change == "source-ownership" ? 2U : 1U;
                        character.Slot = 1;
                        db.SaveChanges();
                    }
                    break;
                case "party-lifetime":
                    PartyManager.Instance.Parties[party.Id] = new Party(party.Id, party.PartyLeaderId, party.Members);
                    break;
                case "party-id-only": party.Members.Remove(party.Find(recipient.AccountEntry.Id)); break;
                case "leave-rejoin":
                    PartyManager.Instance.LeaveParty(recipient);
                    PartyManager.Instance.Parties[party.Id] = party;
                    party.Members.Add(new PartyMember(source));
                    party.Members.Add(new PartyMember(recipient));
                    source.Player.PartyId = recipient.Player.PartyId = party.Id;
                    break;
                case "source-session": source.InvalidateMissionSession(); break;
                case "recipient-session": recipient.InvalidateMissionSession(); break;
                case "other-instance":
                case "source-map-roundtrip":
                case "recipient-map-roundtrip":
                    var player = change == "source-map-roundtrip" ? source.Player : recipient.Player;
                    var original = player.MapChannel;
                    player.MapChannel = new MapChannel { MapInfo = original.MapInfo, ClientList = new() };
                    if (change != "other-instance")
                        player.MapChannel = original;
                    break;
                default: Assert.Fail($"Unknown mutation {change}."); break;
            }
        }

        private static Mission SharingMission(MissionRequirement requirement = null, MissionProgressRule rule = null,
            MissionCreditPolicy credit = null, MissionRepeatPolicy repeat = null) =>
            new(321, "Sharing fixture", 321, 77, 88, 1, 1, 2, true, false,
                new[]
                {
                    new MissionObjectiveDefinition(1, 1001, 1002, new uint?[] { 1003, null, null }, 0,
                        MissionObjectiveState.Incomplete, true,
                        new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 5) },
                        new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                        Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                        Array.Empty<MissionIndicator>(),
                        rule ?? MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.CreatureKilled, 55, 0, 0, 5),
                        creditPolicy: credit ?? new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20))
                }, enableOperational: true, requirement: requirement, repeatPolicy: repeat);

        private sealed class SharedEncounterFixture : IDisposable
        {
            private readonly GroupMissionCreditTests.PartyScope _party;
            internal MissionTestContext Context { get; }
            internal Client Sender => Context.Client;
            internal Client Recipient { get; }
            internal Creature Actor { get; }
            internal ActorHandle Run { get; }
            internal SharedEncounterFixture(Mission definition = null, bool allowJoin = true)
            {
                definition ??= SharingMission(
                    rule: MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.ScenarioEvent, 55, 0, 0, 5),
                    credit: new MissionCreditPolicy(MissionCreditMode.EncounterParticipants, 20));
                Context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [321] = definition },
                    new Dictionary<uint, MissionRewardDefinition> { [321] = new(0, null, null, null) });
                if (definition.Items.Count > 0)
                    Context.AddRewardTemplate(28, 3147);
                Recipient = Context.CreateAdditionalClient(2);
                Actor = Context.AddNpc(77);
                Actor.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                Actor.SpawnPool = new SpawnPool
                {
                    DbId = 77, MapContextId = Context.Map.MapInfo.MapContextId,
                    RuntimeMapChannel = Context.Map, Position = Actor.Position
                };
                Context.Map.SpawnPools.Add(Actor.SpawnPool);
                Context.Manager.Scenes.Bind(321, "data.sequence", new SceneBindings("unversioned",
                    new Dictionary<string, SceneActorDefinition> { ["guide"] = new("guide", SceneActorKind.PublicSpawn, 77) },
                    new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                    {
                        [1] = new(signals: new[] { new SceneMissionSignal(321, 9, 55) }),
                        [2] = new(signals: new[] { new SceneMissionSignal(321, 9, 55) })
                    }));
                Context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "guide", "data.sequence",
                    AllowPartyJoin: allowJoin));
                _party = new GroupMissionCreditTests.PartyScope(Sender, Recipient);
                Assert.IsTrue(Context.Manager.AcceptOfferedMission(Sender, Actor.EntityId, 321));
                Run = Context.Manager.PublicActors.Handle(Context.Map, 77);
                Context.Drain();
            }
            internal bool Offer() => Context.Manager.Sharing.TryShare(Sender, 321);
            internal bool Accept() => Context.Manager.Sharing.TryAccept(Recipient, Sender.Player.EntityId, 321);
            public void Dispose()
            {
                _party.Dispose();
                Context.Dispose();
            }
        }

        private sealed class SharingFixture : IDisposable
        {
            private readonly GroupMissionCreditTests.PartyScope _party;
            internal MissionTestContext Context { get; }
            internal Client Sender => Context.Client;
            internal Client Recipient { get; }
            internal Creature Giver { get; }
            internal SharingFixture(Mission definition = null, Func<DateTime> utcNow = null)
            {
                Context = MissionTestContext.WithCustomDefinitions(
                    new Dictionary<uint, Mission> { [321] = definition ?? SharingMission() },
                    new Dictionary<uint, MissionRewardDefinition> { [321] = new(0, null, null, null) }, utcNow);
                Recipient = Context.CreateAdditionalClient(2);
                Giver = Context.AddNpc(77);
                Assert.IsTrue(Context.Manager.AcceptOfferedMission(Sender, Giver.EntityId, 321));
                _party = new GroupMissionCreditTests.PartyScope(Sender, Recipient);
                Context.Drain();
            }
            internal bool Offer() => Context.Manager.Sharing.TryShare(Sender, 321);
            internal bool Accept() => Context.Manager.Sharing.TryAccept(Recipient, Sender.Player.EntityId, 321);
            internal CharacterMissionOfferEntry Pending()
            {
                using var unit = Context.CreateChar();
                return unit.MissionOffers.Read(2, 321);
            }
            internal List<PythonPacket> DrainRecipient() => MissionTestContext.Drain(Recipient);
            public void Dispose()
            {
                _party.Dispose();
                Context.Dispose();
            }
        }

        private static void Route(MissionTestContext context, Client client, ClientPythonPacket packet)
        {
            var singleton = typeof(MissionApplication).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);
            try
            {
                var handler = new ClientPacketHandler();
                handler.RegisterClient(client);
                var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
                Assert.AreEqual(packet.GetType(), router.GetPacketType(packet.Opcode));
                router.RoutePacket(handler, packet);
            }
            finally { singleton.SetValue(null, previous); }
        }

        private static byte[] Write(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(new PythonWriter(writer));
            return stream.ToArray();
        }
    }
}
