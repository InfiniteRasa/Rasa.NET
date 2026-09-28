extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Packets;
using Rasa.Packets.Manifestation.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.Missions;
using Rasa.Structures.World;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionRadioLifecycleTests
    {
        [TestMethod]
        public void LegacyRadioBooleanDoesNotAdvertiseAnUnauthoredCompletionChannel()
        {
            var definition = new Mission(731, "Radio fixture", 731, 77, 88, 1, 1, 2, false, true,
                new[] { Objective() }, enableOperational: true);

            Assert.IsTrue(definition.IsOperational, definition.OperationalDiagnostic);
            Assert.IsFalse(definition.CreateInfo(MissionState.Active, false)
                .MissionConstantData.RadioCompletable);
        }

        [TestMethod]
        public void RadioOnlyDefinitionsNeedNoDummyNpcAndAllCopiesPreserveChannelsAndRepeatPolicy()
        {
            var definition = RadioMission();
            var runtime = new MissionRuntime(new[] { definition });
            Assert.IsEmpty(runtime.ForNpc(0, 0));
            var requirement = new LevelRequirement(2);
            foreach (var copy in new[]
            {
                definition,
                definition.WithPolicies(new Dictionary<uint, MissionCreditPolicy>(), requirement),
                definition.WithWorldMetadata(definition),
                definition.WithDialogue(definition.Dialogue),
                definition.WithItems(definition.Items.Values, definition.AcceptanceItems)
            })
            {
                Assert.IsTrue(copy.IsOperational, copy.OperationalDiagnostic);
                Assert.IsNull(copy.MissionGiver);
                Assert.IsNull(copy.MissionReciver);
                Assert.AreEqual(MissionChannel.Radio, copy.AcceptanceChannel);
                Assert.AreEqual(MissionChannel.Radio, copy.CompletionChannel);
                Assert.AreEqual(MissionRepeatKind.Immediate, copy.RepeatPolicy.Kind);
                Assert.HasCount(1, copy.RadioSources);
                Assert.IsTrue(copy.CreateInfo(MissionState.Active, false).MissionConstantData.RadioCompletable);
            }
        }

        [TestMethod]
        public void RadioAcceptanceRequiresAPendingServerOfferNotJustAnEligibleMissionId()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [731] = RadioMission() });

            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            using var unit = context.CreateChar();
            Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(1, 731));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
        }

        [TestMethod]
        public void NonBootcampRadioLifecyclePersistsOfferThenUsesNativeAcceptanceAndSelectedReward()
        {
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            using var context = RadioContext(utcNow: () => now);
            context.BeforeSave = _ => Assert.IsEmpty(context.Drain(), "No offer UI may escape before commit.");
            Assert.IsTrue(Offer(context));
            context.BeforeSave = null;
            var pending = ReadOffer(context);
            Assert.AreEqual(MissionOfferState.Pending, pending.State);
            Assert.AreEqual(now, pending.CreatedAtUtc);
            Assert.AreEqual(now.AddMinutes(5), pending.ExpiresAtUtc);
            Assert.AreEqual("fixture.arrival", pending.SourceKey);
            var packet = context.Drain().OfType<DispenseRadioMissionPacket>().Single();
            Assert.AreEqual(731U, packet.MissionId);
            Assert.IsTrue(packet.ForceDialog);

            var singleton = typeof(MissionApplication).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);
            try
            {
                var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
                var handler = new ClientPacketHandler();
                handler.RegisterClient(context.Client);
                router.RoutePacket(handler, new AssignRadioMissionPacket { MissionId = 731 });
                var assignment = context.ReadMission(731);
                Assert.AreEqual(MissionOfferState.Consumed, ReadOffer(context).State);
                Assert.AreEqual(assignment.AssignmentId, ReadOffer(context).ConsumedAssignmentId);
                Assert.AreEqual(assignment.Generation, ReadOffer(context).ConsumedAssignmentGeneration);
                Assert.AreEqual(assignment.AssignmentId, context.Client.Player.Missions[731].AssignmentId);
                Assert.HasCount(1, context.Drain().OfType<MissionGainedPacket>().ToArray());
                Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null));
                Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(999)));
                var before = context.ReadRewardTotals();
                router.RoutePacket(handler, new CompleteRadioMissionPacket { MissionId = 731, SelectionIdx = 1 });
                Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[731].State);
                Assert.AreEqual(before.Credits + 7, context.ReadRewardTotals().Credits);
                Assert.AreEqual(before.ItemCount + 7, context.ReadRewardTotals().ItemCount);
                using var unit = context.CreateChar();
                Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
                Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null));
                Assert.AreEqual(before.Credits + 7, context.ReadRewardTotals().Credits);
            }
            finally { singleton.SetValue(null, previous); }
        }

        [TestMethod]
        public void DuplicateOffersNeitherExtendFiveMinuteExpiryNorNotifyAgain()
        {
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            using var context = RadioContext(utcNow: () => now);
            Assert.IsTrue(Offer(context));
            var first = ReadOffer(context);
            context.Drain();
            now = now.AddMinutes(4);
            Assert.IsTrue(Offer(context));
            Assert.AreEqual(first.OfferId, ReadOffer(context).OfferId);
            Assert.AreEqual(first.ExpiresAtUtc, ReadOffer(context).ExpiresAtUtc);
            Assert.IsEmpty(context.Drain());
            now = now.AddMinutes(1);
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731), "Expiry is exclusive at exactly five minutes.");
            Assert.AreEqual(MissionOfferState.Cancelled, ReadOffer(context).State);
            Assert.IsTrue(Offer(context));
            Assert.AreNotEqual(first.OfferId, ReadOffer(context).OfferId);
            Assert.HasCount(1, context.Drain().OfType<DispenseRadioMissionPacket>().ToArray());
        }

        [TestMethod]
        public void OfferIdentitySurvivesTheMySqlDatetimeSixPrecisionBoundary()
        {
            var now = DateTime.Parse("2026-09-25T12:00:00.1234567Z", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            using var context = RadioContext(utcNow: () => now);
            context.AfterSave = db => db.Database.ExecuteSqlRaw(
                "UPDATE character_mission_offer SET created_at_utc = {0}, expires_at_utc = {1}",
                "2026-09-25 12:00:00.123456", "2026-09-25 12:05:00.123456");
            Assert.IsTrue(Offer(context), "Persisted offer identity must survive MySQL datetime(6) precision.");
            context.AfterSave = null;
            Assert.AreEqual(1234560L, ReadOffer(context).CreatedAtUtc.Ticks % TimeSpan.TicksPerSecond);
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, 731));
        }

        [TestMethod]
        [DataRow("cancelled")]
        [DataRow("consumed")]
        [DataRow("revision")]
        [DataRow("source")]
        [DataRow("map")]
        [DataRow("session")]
        [DataRow("character")]
        public void InvalidPendingAuthorityCannotAssign(string change)
        {
            using var context = RadioContext();
            Assert.IsTrue(Offer(context));
            context.Drain();
            using (var db = context.Open())
            {
                var offer = db.CharacterMissionOfferEntries.Single();
                switch (change)
                {
                    case "cancelled": offer.State = MissionOfferState.Cancelled; break;
                    case "consumed": offer.State = MissionOfferState.Consumed; break;
                    case "revision": offer.ContentRevision = "old-radio"; break;
                    case "source": offer.SourceKey = "not-authored"; break;
                    case "map": offer.MapEpoch = Guid.NewGuid(); break;
                    case "session": offer.SessionId = Guid.NewGuid(); break;
                    case "character":
                        var other = context.CreateAdditionalClient(2);
                        Assert.IsFalse(context.Manager.TryAcceptRadioMission(other, 731));
                        Assert.AreEqual(MissionOfferState.Pending, ReadOffer(context).State);
                        return;
                }
                db.SaveChanges();
            }
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
            Assert.IsEmpty(context.Drain().OfType<MissionGainedPacket>());
        }

        [TestMethod]
        [DataRow("loading")]
        [DataRow("map-roundtrip")]
        [DataRow("character-roundtrip")]
        [DataRow("detach")]
        public void SameClientRoundTripsInvalidateTheOldOfferAndAllowFreshAuthoredIssuance(string change)
        {
            using var context = RadioContext();
            Assert.IsTrue(Offer(context));
            var old = ReadOffer(context).OfferId;
            switch (change)
            {
                case "loading":
                    context.Client.State = RasaGame::Rasa.Data.ClientState.Loading;
                    context.Client.State = RasaGame::Rasa.Data.ClientState.Ingame;
                    break;
                case "map-roundtrip":
                    context.Client.Player.MapChannel = new MapChannel { MapInfo = context.Map.MapInfo };
                    context.Client.Player.MapChannel = context.Map;
                    break;
                case "character-roundtrip":
                    context.Client.Player.Id = 2;
                    context.Client.Player.Id = 1;
                    break;
                case "detach":
                    CellManager.Instance.DetachClient(context.Map, context.Client);
                    CellManager.Instance.AddToWorld(context.Client);
                    break;
            }
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.IsTrue(Offer(context));
            Assert.AreNotEqual(old, ReadOffer(context).OfferId);
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, 731));
        }

        [TestMethod]
        public void OffersAreBoundedAtThirtyAndExpiredSlotsCanBeReused()
        {
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            using var context = MissionTestContext.WithCustomDefinitions(
                Enumerable.Range(731, 31).ToDictionary(id => (uint)id, id => RadioMission((uint)id)), utcNow: () => now);
            for (uint id = 731; id < 761; id++)
                Assert.IsTrue(Offer(context, id));
            Assert.IsFalse(Offer(context, 761));
            using (var db = context.Open())
                Assert.AreEqual(30, db.CharacterMissionOfferEntries.Count(entry => entry.State == MissionOfferState.Pending));
            now = now.AddMinutes(5);
            Assert.IsTrue(Offer(context, 761));
            using (var db = context.Open())
                Assert.AreEqual(1, db.CharacterMissionOfferEntries.Count(), "Transient rows are pruned, not accumulated as history.");
        }

        [TestMethod]
        public void DuplicateConcurrentAcceptancesConsumeOneOfferAndCreateOneAttempt()
        {
            using var context = RadioContext();
            Assert.IsTrue(Offer(context));
            var results = new bool[2];
            Parallel.Invoke(
                () => results[0] = context.Manager.TryAcceptRadioMission(context.Client, 731),
                () => results[1] = context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.AreEqual(1, results.Count(value => value));
            using var db = context.Open();
            Assert.AreEqual(1, db.CharacterMissionEntries.Count());
            Assert.AreEqual(1, db.CharacterMissionObjectiveEntries.Count());
            Assert.AreEqual(MissionOfferState.Consumed, ReadOffer(context).State);
        }

        [TestMethod]
        public void OfferWriteFailureCannotNotifyAndAnEligibleSourceCanRetry()
        {
            using var context = RadioContext();
            context.BeforeSave = _ => throw new DbUpdateException("Injected pending-offer write failure.");
            Assert.IsFalse(Offer(context));
            context.BeforeSave = null;
            Assert.IsNull(ReadOffer(context));
            Assert.IsEmpty(context.Drain());
            Assert.IsTrue(Offer(context));
            Assert.HasCount(1, context.Drain().OfType<DispenseRadioMissionPacket>().ToArray());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailedAcceptanceAndItemWritesRollBackConsumptionAndCanRetryTheSameOffer(bool failItem)
        {
            var definition = RadioMission().WithItems(
                new[] { new MissionItemBinding("radio-tool", 28, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove) },
                new[] { new IssueMissionItemIntent("accept-tool", 731, "radio-tool", 28, 1) });
            using var context = RadioContext(definition);
            Assert.IsTrue(Offer(context));
            var original = ReadOffer(context).OfferId;
            context.Drain();
            var injected = false;
            context.BeforeSave = db =>
            {
                Assert.IsEmpty(context.Drain());
                if (!failItem || db.ChangeTracker.Entries<ItemEntry>().Any(entry => entry.State == EntityState.Added))
                {
                    injected = true;
                    throw new DbUpdateException("Injected acceptance persistence failure.");
                }
            };
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.IsTrue(injected);
            context.BeforeSave = null;
            Assert.AreEqual(original, ReadOffer(context).OfferId);
            Assert.AreEqual(MissionOfferState.Pending, ReadOffer(context).State);
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
            Assert.AreEqual(0L, context.ReadRewardTotals().ItemCount);
            using (var unit = context.CreateChar())
            {
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(1, 731));
                Assert.IsEmpty(unit.CharacterMissionItems.GetOwned(1));
            }
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, 731));
            using var verify = context.CreateChar();
            Assert.AreEqual(context.ReadMission(731).AssignmentId, verify.CharacterMissionItems.GetOwned(1).Single().AssignmentId);
        }

        [TestMethod]
        public void AcceptanceRechecksTheThirtySlotJournalWithoutConsumingTheOffer()
        {
            using var context = RadioContext();
            Assert.IsTrue(Offer(context));
            for (uint mission = 1000; mission < 1030; mission++)
                context.SeedMission(1, mission, (uint)MissionState.Active, false);
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.AreEqual(MissionOfferState.Pending, ReadOffer(context).State);
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
        }

        [TestMethod]
        public void RequirementsAndOwnershipAreRecheckedAfterAnOfferWasSent()
        {
            using var context = RadioContext(RadioMission(requirement: new FlagRequirement(91, 1)));
            Assert.IsFalse(Offer(context));
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(1, 91, 1));
            context.Client.Player.PlayerFlags[91] = 1;
            Assert.IsTrue(Offer(context));
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Remove(1, 91));
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(1, 91, 1));
            context.CreateAdditionalClient(2);
            using (var db = context.Open())
            {
                var character = db.CharacterEntries.Single(entry => entry.Id == 1);
                character.AccountId = 2;
                character.Slot = 1;
                db.SaveChanges();
            }
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UnacceptedAdmissionRequirementAllowsItsOwnAssignmentThroughEitherPlanner(bool radio)
        {
            var definition = RadioMission(acceptance: MissionChannel.Mixed,
                requirement: new NotRequirement(new MissionStateRequirement(731, Accepted: true)));
            using var context = RadioContext(definition);
            Assert.IsTrue(Offer(context));
            context.Drain();

            Assert.IsTrue(radio
                ? context.Manager.TryAcceptRadioMission(context.Client, 731)
                : context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 731),
                "Creating the admitted assignment must not invalidate its unaccepted precondition.");

            var assignment = context.ReadMission(731);
            Assert.AreEqual((uint)MissionState.Active, assignment.MissionState);
            Assert.AreEqual(assignment.AssignmentId, context.Client.Player.Missions[731].AssignmentId);
            Assert.AreEqual(radio ? MissionOfferState.Consumed : MissionOfferState.Cancelled, ReadOffer(context).State);
            Assert.HasCount(1, context.Drain().OfType<MissionGainedPacket>().ToArray());
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void UnacceptedSourceRequirementSurvivesOnlyItsPlannedAssignmentWrite(bool scene, bool changeSource)
        {
            var requirement = new AllRequirements(new MissionRequirement[]
            {
                new NotRequirement(new MissionStateRequirement(731, Accepted: true)),
                new CustomRequirement("character.starting-experience-active")
            });
            using var context = scene ? SceneOfferContext(requirement) : RadioContext(RadioMission(radioSources: new[]
            {
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "fixture.arrival", Requirement: requirement)
            }));
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(1, "fixture", CharacterStartingExperienceState.Bootcamp)));
            Assert.IsTrue(scene ? context.Manager.Scenes.ExecuteNamed(context.Client, 730, "offer") : Offer(context));
            var offerId = ReadOffer(context).OfferId;
            context.Drain();
            var changed = false;
            if (changeSource)
                context.AfterSave = db =>
                {
                    if (changed)
                        return;
                    changed = true;
                    db.Database.ExecuteSqlRaw("UPDATE character_starting_experience SET state = 3 WHERE character_id = 1");
                };

            Assert.AreEqual(!changeSource, context.Manager.TryAcceptRadioMission(context.Client, 731),
                "Only the new assignment is an expected source-fact change.");
            context.AfterSave = null;
            if (changeSource)
            {
                Assert.IsTrue(changed, "The source must change after the actual assignment flush.");
                Assert.AreEqual(MissionOfferState.Pending, ReadOffer(context).State);
                Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
                Assert.IsEmpty(context.Drain().OfType<MissionGainedPacket>());
                using (var unit = context.CreateChar())
                {
                    Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(1, 731));
                    Assert.AreEqual(CharacterStartingExperienceState.Bootcamp, unit.CharacterStartingExperience.ReadState(1));
                }
                Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, 731),
                    "Rollback must leave the same authorized offer retryable.");
            }
            Assert.AreEqual(offerId, ReadOffer(context).OfferId);
            Assert.AreEqual(MissionOfferState.Consumed, ReadOffer(context).State);
            Assert.AreEqual(context.ReadMission(731).AssignmentId, ReadOffer(context).ConsumedAssignmentId);
            Assert.HasCount(1, context.Drain().OfType<MissionGainedPacket>().ToArray());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SourceEligibilityReadsFreshPersistenceAfterTheOfferOrAssignmentFlush(bool accepting)
        {
            var definition = RadioMission(radioSources: new[]
            {
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "fixture.arrival",
                    Requirement: new CustomRequirement("character.starting-experience-active"))
            });
            using var context = RadioContext(definition);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(1, "fixture", CharacterStartingExperienceState.Bootcamp)));
            if (accepting)
                Assert.IsTrue(Offer(context));
            context.Drain();
            context.AfterSave = db => db.Database.ExecuteSqlRaw(
                "UPDATE character_starting_experience SET state = 3 WHERE character_id = 1");
            Assert.IsFalse(accepting
                ? context.Manager.TryAcceptRadioMission(context.Client, 731)
                : Offer(context), "A tracked Bootcamp state must not override the final source read.");
            context.AfterSave = null;
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
            if (accepting)
                Assert.AreEqual(MissionOfferState.Pending, ReadOffer(context).State);
            else
                Assert.IsNull(ReadOffer(context));
            Assert.IsEmpty(context.Drain().OfType<DispenseRadioMissionPacket>());
            Assert.IsTrue(accepting ? context.Manager.TryAcceptRadioMission(context.Client, 731) : Offer(context));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ExpiryOrSessionChangeAfterTheFinalOfferReaderRollsBackAcceptance(bool changeSession)
        {
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            using var context = RadioContext(utcNow: () => now);
            Assert.IsTrue(Offer(context));
            context.Drain();
            var saved = false;
            var fired = false;
            context.AfterSave = _ => saved = true;
            context.AfterCommand = sql =>
            {
                if (saved && !fired && sql.Contains("EXISTS", StringComparison.Ordinal) &&
                    sql.Contains("FROM \"character\"", StringComparison.Ordinal))
                {
                    fired = true;
                    if (changeSession)
                        context.Client.InvalidateMissionSession();
                    else
                        now = now.AddMinutes(5);
                }
            };
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            context.AfterSave = null;
            context.AfterCommand = null;
            Assert.IsTrue(fired, "Cross the boundary after the actual final source/owner reader.");
            Assert.AreEqual(MissionOfferState.Pending, ReadOffer(context).State);
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
            Assert.IsEmpty(context.Drain().OfType<MissionGainedPacket>());
        }

        [TestMethod]
        [DataRow(-1, null)]
        [DataRow(2, null)]
        [DataRow(null, null)]
        [DataRow(0, 1)]
        [DataRow(0, -1)]
        public void InvalidRewardSelectionAndAllNonNullRatingsAreRejected(int? selection, int? rating)
        {
            using var context = RadioContext();
            Ready(context);
            var before = context.ReadRewardTotals();
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, selection, rating));
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[731].State);
        }

        [TestMethod]
        [DataRow(MissionChannel.Npc, MissionChannel.Radio)]
        [DataRow(MissionChannel.Radio, MissionChannel.Npc)]
        [DataRow(MissionChannel.Mixed, MissionChannel.Mixed)]
        public void AcceptanceAndCompletionChannelsAreIndependentAndMixedUsesTheSamePlanners(
            MissionChannel acceptance, MissionChannel completion)
        {
            using var context = RadioContext(RadioMission(acceptance: acceptance, completion: completion));
            if (acceptance == MissionChannel.Npc)
            {
                Assert.IsFalse(Offer(context));
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 731));
            }
            else
            {
                Assert.IsTrue(Offer(context));
                Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, 731));
            }
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(999)));
            if (completion == MissionChannel.Npc)
            {
                Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
                Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 731, 0, null));
            }
            else
                Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            Assert.AreEqual(107, context.ReadRewardTotals().Credits);
        }

        [TestMethod]
        public void LegacySuccessIsSettledThroughRadioAndCannotBeAcceptedAgainBeforeSettlement()
        {
            using var context = RadioContext();
            context.SeedMission(1, 731, (uint)MissionState.Success, false);
            context.ReloadPlayerMissions();
            Assert.IsFalse(Offer(context));
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[731].State);
            Assert.AreEqual(107, context.ReadRewardTotals().Credits);
        }

        [TestMethod]
        public void RepeatAttemptsHaveIndependentIdentityReceiptsAndRejectAStaleRuntime()
        {
            using var context = RadioContext();
            Ready(context);
            var first = context.ReadMission(731);
            var stale = context.CreateCompetingClient();
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            Ready(context);
            var second = context.ReadMission(731);
            Assert.AreNotEqual(first.AssignmentId, second.AssignmentId);
            Assert.AreEqual(first.Generation + 1, second.Generation);
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(stale, 731, 0, null));
            Assert.AreEqual(107, context.ReadRewardTotals().Credits);
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            using var unit = context.CreateChar();
            Assert.HasCount(2, unit.CharacterMissions.Runtime.History(1));
            Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(first.AssignmentId));
            Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(second.AssignmentId));
        }

        [TestMethod]
        [DataRow(MissionRepeatKind.Once)]
        [DataRow(MissionRepeatKind.Cooldown)]
        [DataRow(MissionRepeatKind.Daily)]
        public void RadioReusesTheRepeatAdmissionPolicy(MissionRepeatKind kind)
        {
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            var policy = new MissionRepeatPolicy(kind, kind == MissionRepeatKind.Cooldown ? 60U : null,
                kind == MissionRepeatKind.Daily ? 0U : null);
            using var context = RadioContext(RadioMission(repeat: policy), () => now);
            Ready(context);
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            Assert.IsFalse(Offer(context));
            now = now.AddSeconds(59);
            Assert.IsFalse(Offer(context));
            now = kind == MissionRepeatKind.Cooldown ? now.AddSeconds(1) : now.AddDays(1);
            Assert.AreEqual(kind != MissionRepeatKind.Once, Offer(context));
        }

        [TestMethod]
        public void RewardWriteFailureRollsBackItemsHistoryAndReceiptThenRetriesOnce()
        {
            using var context = RadioContext();
            Ready(context);
            context.Drain();
            var before = context.ReadRewardTotals();
            var assignment = context.ReadMission(731);
            var fired = false;
            context.BeforeSave = db =>
            {
                if (db.ChangeTracker.Entries<MissionReceiptEntry>().Any(entry => entry.Entity.OperationKey == "mission-reward"))
                {
                    fired = true;
                    throw new DbUpdateException("Injected radio reward receipt failure.");
                }
            };
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null));
            context.BeforeSave = null;
            Assert.IsTrue(fired);
            Assert.AreEqual(before, context.ReadRewardTotals());
            using (var unit = context.CreateChar())
                Assert.IsFalse(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
            Assert.IsEmpty(context.Drain().OfType<MissionRewardedPacket>());
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null));
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null));
            Assert.AreEqual(before.Credits + 7, context.ReadRewardTotals().Credits);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FinalRadioRewardReaderCannotCrossADailyResetOrChangeSession(bool changeSession)
        {
            var now = new DateTime(2026, 9, 25, 23, 59, 59, DateTimeKind.Utc);
            using var context = RadioContext(RadioMission(repeat: new(MissionRepeatKind.Daily, ResetSecondUtc: 0)), () => now);
            Ready(context);
            var before = context.ReadRewardTotals();
            var saved = false;
            var fired = false;
            context.AfterSave = _ => saved = true;
            context.AfterCommand = sql =>
            {
                if (saved && !fired && sql.Contains("EXISTS", StringComparison.Ordinal) &&
                    sql.Contains("FROM \"character\"", StringComparison.Ordinal))
                {
                    fired = true;
                    if (changeSession)
                        context.Client.InvalidateMissionSession();
                    else
                        now = now.AddSeconds(1);
                }
            };
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            context.AfterSave = null;
            context.AfterCommand = null;
            Assert.IsTrue(fired);
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
        }

        [TestMethod]
        public void CompetingRadioRewardRequestsCommitOneReceiptAndOneInventoryGrant()
        {
            using var context = RadioContext();
            Ready(context);
            var second = context.CreateCompetingClient();
            var results = new bool[2];
            Parallel.Invoke(
                () => results[0] = context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null),
                () => results[1] = context.Manager.TryCompleteRadioMission(second, 731, 0, null));
            Assert.AreEqual(1, results.Count(value => value));
            Assert.AreEqual(107, context.ReadRewardTotals().Credits);
            Assert.AreEqual(5L, context.ReadRewardTotals().ItemCount);
            using var unit = context.CreateChar();
            Assert.HasCount(1, unit.CharacterMissions.Runtime.History(1));
        }

        [TestMethod]
        public void ANewConnectionCannotUseTheOldOfferButCanReceiveAFreshEligibleOffer()
        {
            using var context = RadioContext();
            Assert.IsTrue(Offer(context));
            var original = ReadOffer(context);
            var reconnect = context.CreateCompetingClient();
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(reconnect, 731));
            Assert.IsTrue(context.Manager.Offers.TryOffer(reconnect, 731,
                MissionOfferSourceIdentity.ServerEvent("fixture.arrival")));
            Assert.AreNotEqual(original.OfferId, ReadOffer(context).OfferId);
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(reconnect, 731));
        }

        [TestMethod]
        public void AnNpcConversationDoesNotReplaceRadioAuthorityAndNpcAcceptanceCancelsItsMixedOffer()
        {
            using var context = RadioContext(RadioMission(acceptance: MissionChannel.Mixed, completion: MissionChannel.Mixed));
            Assert.IsTrue(Offer(context));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, giver.EntityId));
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.AreEqual(MissionOfferState.Consumed, ReadOffer(context).State);
            Assert.IsNotNull(context.Client.MissionConversation, "Radio and NPC sessions are separate authorities.");
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(999)));
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            Assert.IsTrue(Offer(context));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 731));
            Assert.AreEqual(MissionOfferState.Cancelled, ReadOffer(context).State);
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ActiveTurnInRequirementAllowsItsOwnCompletedTransitionThroughEitherPlanner(bool radio)
        {
            using var context = RadioContext(RadioMission(completion: MissionChannel.Mixed,
                turnIn: new MissionStateRequirement(731, MissionState.Active)));
            Ready(context);
            var assignment = context.ReadMission(731);
            var before = context.ReadRewardTotals();
            context.Drain();

            Assert.IsTrue(radio
                ? context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null)
                : context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 731, 1, null),
                "Completing the admitted attempt must not invalidate its Active precondition.");

            Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(731).MissionState);
            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[731].State);
            Assert.AreEqual(before.Credits + 7, context.ReadRewardTotals().Credits);
            Assert.AreEqual(before.ItemCount + 7, context.ReadRewardTotals().ItemCount);
            using var unit = context.CreateChar();
            var history = unit.CharacterMissions.Runtime.History(1).Single();
            Assert.AreEqual(assignment.AssignmentId, history.AssignmentId);
            Assert.IsTrue(history.Rewarded);
            Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
            Assert.HasCount(1, context.Drain().OfType<MissionRewardedPacket>().ToArray());
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void EligibleLevelNineTurnInCanReachLevelTenThroughEitherPlanner(bool radio, bool customLevel)
        {
            MissionRequirement requirement = customLevel
                ? new NotRequirement(new CustomRequirement("example.even-level"))
                : new NotRequirement(new LevelRequirement(10));
            using var context = RadioContext(RadioMission(completion: MissionChannel.Mixed, turnIn: requirement));
            ReadyNearLevelTen(context);
            context.Drain();
            context.BeforeSave = _ =>
            {
                Assert.AreEqual((byte)9, context.Client.Player.Level);
                Assert.AreEqual(272400U, context.Client.Player.Experience);
                Assert.IsEmpty(context.Drain(), "No reward may publish before the eligible transaction commits.");
            };

            Assert.IsTrue(radio
                ? context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null)
                : context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, 731, 1, null),
                "The reward's own level gain must not invalidate the admitted level requirement.");
            context.BeforeSave = null;

            Assert.AreEqual(272500U, context.ReadRewardTotals().Experience);
            Assert.AreEqual(272500U, context.Client.Player.Experience);
            Assert.AreEqual((byte)10, context.Client.Player.Level);
            using var unit = context.CreateChar();
            Assert.AreEqual((byte)10, unit.Characters.Get(1).Level);
            Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(731).MissionState);
            Assert.IsTrue(unit.CharacterMissions.Runtime.History(1).Single().Rewarded);
            var packets = context.Drain();
            Assert.HasCount(1, packets.OfType<MissionRewardedPacket>().ToArray());
            Assert.HasCount(1, packets.OfType<ExperienceChangedPacket>().ToArray());
            Assert.HasCount(1, packets.OfType<LevelUpPacket>().ToArray());
            Assert.HasCount(1, packets.OfType<AttributeInfoPacket>().ToArray());
        }

        [TestMethod]
        [DataRow(false, "level")]
        [DataRow(true, "level")]
        [DataRow(false, "flag")]
        [DataRow(true, "flag")]
        [DataRow(false, "assignment")]
        [DataRow(true, "assignment")]
        [DataRow(false, "starting-experience")]
        [DataRow(true, "starting-experience")]
        public void UnexpectedLateTurnInFactsRollBackTheWholeRewardThroughEitherPlanner(bool radio, string change)
        {
            var requirement = new AllRequirements(new MissionRequirement[]
            {
                new MissionStateRequirement(731, MissionState.Active),
                new NotRequirement(new LevelRequirement(10)),
                new FlagRequirement(92, 1),
                new NotRequirement(new CustomRequirement("character.starting-experience-completed"))
            });
            using var context = RadioContext(RadioMission(completion: MissionChannel.Mixed, turnIn: requirement));
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.CharacterFlags.Set(1, 92, 1);
                    unit.CharacterStartingExperience.Add(
                        new CharacterStartingExperienceEntry(1, "fixture", CharacterStartingExperienceState.Bootcamp));
                });
            context.Client.Player.PlayerFlags[92] = 1;
            ReadyNearLevelTen(context);
            var assignment = context.ReadMission(731);
            var before = context.ReadRewardTotals();
            var receiver = context.AddNpc(88);
            context.Drain();
            var changed = false;
            context.AfterSave = db =>
            {
                if (changed || !db.ChangeTracker.Entries<MissionReceiptEntry>()
                    .Any(entry => entry.Entity.OwnerId == assignment.AssignmentId && entry.Entity.OperationKey == "mission-reward"))
                    return;
                changed = true;
                switch (change)
                {
                    case "level":
                        db.Database.ExecuteSqlRaw("UPDATE character SET level = 12 WHERE id = 1");
                        break;
                    case "flag":
                        db.Database.ExecuteSqlRaw("UPDATE character_flag SET value = 2 WHERE character_id = 1 AND flag_id = 92");
                        break;
                    case "assignment":
                        db.Database.ExecuteSqlRaw("UPDATE character_mission SET mission_state = 2 WHERE character_id = 1 AND mission_id = 731");
                        break;
                    case "starting-experience":
                        db.Database.ExecuteSqlRaw("UPDATE character_starting_experience SET state = 3 WHERE character_id = 1");
                        break;
                }
            };

            bool Complete() => radio
                ? context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null)
                : context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 731, 1, null);
            Assert.IsFalse(Complete(), "Unexpected final facts are not part of the planned assignment and XP changes.");
            context.AfterSave = null;
            Assert.IsTrue(changed, "Inject after the reward receipt has actually been flushed.");
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual((uint)MissionState.Active, context.ReadMission(731).MissionState);
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[731].State);
            Assert.AreEqual((byte)9, context.Client.Player.Level);
            Assert.AreEqual(272400U, context.Client.Player.Experience);
            using (var unit = context.CreateChar())
            {
                Assert.AreEqual((byte)9, unit.Characters.Get(1).Level);
                Assert.AreEqual(1U, unit.CharacterFlags.GetValue(1, 92));
                Assert.AreEqual(CharacterStartingExperienceState.Bootcamp, unit.CharacterStartingExperience.ReadState(1));
                Assert.IsEmpty(unit.CharacterMissions.Runtime.History(1));
                Assert.IsFalse(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
            }
            Assert.IsEmpty(context.Drain().OfType<MissionRewardedPacket>());
            Assert.IsTrue(Complete());
            Assert.IsFalse(Complete(), "Retry must still grant this attempt exactly once.");
            Assert.AreEqual(272500U, context.ReadRewardTotals().Experience);
            Assert.AreEqual(before.Credits + 7, context.ReadRewardTotals().Credits);
            Assert.AreEqual(before.ItemCount + 7, context.ReadRewardTotals().ItemCount);
            Assert.HasCount(1, context.Drain().OfType<MissionRewardedPacket>().ToArray());
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, true, false)]
        [DataRow(false, false, true)]
        [DataRow(true, false, true)]
        [DataRow(false, true, true)]
        [DataRow(true, true, true)]
        public void AuthoredRewardProgressFlagTransitionCommitsThroughEitherPlanner(bool radio, bool lateOverwrite, bool writeZero)
        {
            var expectedFlag = writeZero ? 0U : 2U;
            var progress = new Mission(732, "Reward progress", 732, 77, 88, 1, 1, 2, false, false,
                new[] { RewardProgressObjective(1, new MissionActionDefinition[]
                {
                    new() { Kind = MissionActionKind.SetPlayerFlag, Sequence = 1, PlayerFlagId = 92, PlayerFlagValue = expectedFlag }
                }) }, enableOperational: true);
            MissionRequirement requirement = writeZero
                ? new NotRequirement(new FlagRequirement(92, 0))
                : new FlagRequirement(92, 1);
            using var context = RadioContext(RadioMission(completion: MissionChannel.Mixed,
                turnIn: requirement), additionalDefinitions: new[] { progress });
            if (!writeZero)
            {
                using var unit = context.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(1, 92, 1));
                context.Client.Player.PlayerFlags[92] = 1;
            }
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 732));
            Ready(context);
            var assignment = context.ReadMission(731);
            var before = context.ReadRewardTotals();
            var receiver = context.AddNpc(88);
            context.Drain();
            var changed = false;
            context.AfterSave = db =>
            {
                if (!lateOverwrite || changed || !db.ChangeTracker.Entries<MissionReceiptEntry>()
                    .Any(entry => entry.Entity.OwnerId == assignment.AssignmentId && entry.Entity.OperationKey == "mission-reward"))
                    return;
                changed = true;
                db.Database.ExecuteSqlRaw(writeZero
                    ? "DELETE FROM character_flag WHERE character_id = 1 AND flag_id = 92"
                    : "UPDATE character_flag SET value = 3 WHERE character_id = 1 AND flag_id = 92");
            };

            bool Complete() => radio
                ? context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null)
                : context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 731, 1, null);
            Assert.AreEqual(!lateOverwrite, Complete(),
                "The authored flag write must commit, but a later different value or removal must roll back.");
            context.AfterSave = null;
            if (lateOverwrite)
            {
                Assert.IsTrue(changed, "The unexpected write must happen after the reward receipt is flushed.");
                Assert.AreEqual(before, context.ReadRewardTotals());
                Assert.AreEqual((uint)MissionState.Active, context.ReadMission(731).MissionState);
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    (MissionObjectiveState)context.ReadProgress(732).Missions[732].Objectives[1].State);
                Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[732].Objectives[1].State);
                Assert.AreEqual(!writeZero, context.Client.Player.PlayerFlags.ContainsKey(92));
                if (!writeZero)
                    Assert.AreEqual(1U, context.Client.Player.PlayerFlags[92]);
                using (var unit = context.CreateChar())
                {
                    Assert.AreEqual<uint?>(writeZero ? null : 1U, unit.CharacterFlags.GetValue(1, 92));
                    Assert.IsEmpty(unit.CharacterMissions.Runtime.History(1));
                    Assert.IsFalse(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
                }
                Assert.IsEmpty(context.Drain());
                Assert.IsTrue(Complete(), "Rollback must leave the authored transition and reward retryable.");
            }

            Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(731).MissionState);
            Assert.AreEqual(MissionObjectiveState.Completed, (MissionObjectiveState)context.ReadProgress(732).Missions[732].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[732].Objectives[1].State);
            Assert.AreEqual(expectedFlag, context.Client.Player.PlayerFlags[92]);
            Assert.AreEqual(before.Experience + 100, context.ReadRewardTotals().Experience);
            Assert.AreEqual(before.Credits + 7, context.ReadRewardTotals().Credits);
            Assert.AreEqual(before.ItemCount + 7, context.ReadRewardTotals().ItemCount);
            using (var unit = context.CreateChar())
            {
                Assert.AreEqual(expectedFlag, unit.CharacterFlags.GetValue(1, 92));
                Assert.AreEqual(assignment.AssignmentId, unit.CharacterMissions.Runtime.History(1).Single().AssignmentId);
                Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
            }
            var packets = context.Drain();
            Assert.HasCount(1, packets.OfType<ObjectiveCompletedPacket>().ToArray());
            Assert.HasCount(1, packets.OfType<MissionRewardedPacket>().ToArray());
            Assert.IsFalse(Complete(), "The rewarded assignment must remain exactly once.");
        }

        [TestMethod]
        [DataRow(false, true, "none")]
        [DataRow(true, true, "none")]
        [DataRow(false, true, "flag")]
        [DataRow(true, true, "flag")]
        [DataRow(false, true, "earlier-flag")]
        [DataRow(true, true, "earlier-flag")]
        [DataRow(false, true, "assignment")]
        [DataRow(true, true, "assignment")]
        [DataRow(false, true, "history")]
        [DataRow(true, true, "history")]
        [DataRow(false, false, "none")]
        [DataRow(true, false, "none")]
        [DataRow(false, false, "flag")]
        [DataRow(true, false, "flag")]
        [DataRow(false, false, "history")]
        [DataRow(true, false, "history")]
        public void OrderedRewardProgressProjectsFailureHistoryAndLastFlagWrite(bool radio, bool required, string lateChange)
        {
            var failing = new Mission(732, "Failure on reward", 732, 77, 88, 1, 1, 2, false, false,
                new[] { RewardProgressObjective(1, new MissionActionDefinition[]
                {
                    new() { Kind = MissionActionKind.SetPlayerFlag, Sequence = 2, PlayerFlagId = 92, PlayerFlagValue = 4 },
                    new() { Kind = MissionActionKind.SetPlayerFlag, Sequence = 4, PlayerFlagId = 93, PlayerFlagValue = 5 },
                    new() { Kind = MissionActionKind.SetPlayerFlag, Sequence = 1, PlayerFlagId = 92, PlayerFlagValue = 2 },
                    new() { Kind = MissionActionKind.SetPlayerFlag, Sequence = 3, PlayerFlagId = 93, PlayerFlagValue = 3 }
                }, MissionObjectiveState.Failed, required) }, enableOperational: true,
                repeatPolicy: new(MissionRepeatKind.Immediate));
            var succeeding = new Mission(733, "Acquisition after reward", 733, 77, 88, 1, 1, 2, false, false,
                new[] { RewardProgressObjective(1, new MissionActionDefinition[]
                {
                    new() { Kind = MissionActionKind.SetPlayerFlag, Sequence = 1, PlayerFlagId = 92, PlayerFlagValue = 6 }
                }, rule: MissionProgressRule.IncrementCounterOnExactSubject(MissionProgressEventKind.ItemAcquired, 3147, 0, 0, 7),
                    counters: new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, 7) }) },
                enableOperational: true);
            var requirement = new AllRequirements(new MissionRequirement[]
            {
                new FlagRequirement(92, 1),
                new FlagRequirement(93, 1),
                new MissionStateRequirement(731, MissionState.Active),
                new MissionStateRequirement(732, MissionState.Active),
                new MissionStateRequirement(732, MissionState.Completed),
                new MissionStateRequirement(733, MissionState.Active)
            });
            using var context = RadioContext(RadioMission(completion: MissionChannel.Mixed, turnIn: requirement),
                additionalDefinitions: new[] { succeeding, failing });
            context.SeedMission(1, 732, (uint)MissionState.Completed, true);
            var oldAssignment = context.ReadMission(732);
            string oldHistory;
            using (var unit = context.CreateChar())
            {
                unit.ExecuteTransaction(() =>
                {
                    unit.CharacterMissions.Runtime.Archive(oldAssignment, new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc));
                    unit.CharacterFlags.Set(1, 92, 1);
                    unit.CharacterFlags.Set(1, 93, 1);
                });
                oldHistory = JsonSerializer.Serialize(unit.CharacterMissions.Runtime.ReadHistory(oldAssignment.AssignmentId));
            }
            context.Client.Player.PlayerFlags[92] = 1;
            context.Client.Player.PlayerFlags[93] = 1;
            context.ReloadPlayerMissions();
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 732));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 733));
            Ready(context);
            var assignment = context.ReadMission(731);
            var failedAttempt = context.ReadMission(732);
            var before = context.ReadRewardTotals();
            var receiver = context.AddNpc(88);
            context.Drain();
            var changed = false;
            context.AfterSave = db =>
            {
                if (lateChange == "none" || changed || !db.ChangeTracker.Entries<MissionReceiptEntry>()
                    .Any(entry => entry.Entity.OwnerId == assignment.AssignmentId && entry.Entity.OperationKey == "mission-reward"))
                    return;
                changed = true;
                switch (lateChange)
                {
                    case "flag":
                        db.Database.ExecuteSqlRaw("UPDATE character_flag SET value = 9 WHERE character_id = 1 AND flag_id = 92");
                        break;
                    case "earlier-flag":
                        db.Database.ExecuteSqlRaw("UPDATE character_flag SET value = 4 WHERE character_id = 1 AND flag_id = 92");
                        break;
                    case "assignment":
                        db.Database.ExecuteSqlRaw("UPDATE character_mission SET mission_state = 1 WHERE character_id = 1 AND mission_id = 732");
                        break;
                    case "history":
                        db.Database.ExecuteSqlRaw("UPDATE character_mission_history SET outcome = 3, rewarded = 0 WHERE character_id = 1 AND mission_id = 732");
                        break;
                }
            };
            bool Complete() => radio
                ? context.Manager.TryCompleteRadioMission(context.Client, 731, 1, null)
                : context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 731, 1, null);

            Assert.AreEqual(lateChange == "none", Complete(),
                "Project the failure outcome and all ordered action/counter effects, not just the rewarded assignment.");
            context.AfterSave = null;
            if (lateChange != "none")
            {
                Assert.IsTrue(changed);
                Assert.AreEqual(before, context.ReadRewardTotals());
                Assert.AreEqual((uint)MissionState.Active, context.ReadMission(731).MissionState);
                Assert.AreEqual((uint)MissionState.Active, context.ReadMission(732).MissionState);
                Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[732].Objectives[1].State);
                Assert.AreEqual(0U, context.Client.Player.Missions[733].Objectives[1].Counters[0]);
                Assert.AreEqual(1U, context.Client.Player.PlayerFlags[92]);
                Assert.AreEqual(1U, context.Client.Player.PlayerFlags[93]);
                using (var unit = context.CreateChar())
                {
                    Assert.AreEqual(1U, unit.CharacterFlags.GetValue(1, 92));
                    Assert.AreEqual(1U, unit.CharacterFlags.GetValue(1, 93));
                    Assert.IsNull(unit.CharacterMissions.Runtime.ReadHistory(failedAttempt.AssignmentId));
                    Assert.IsFalse(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
                    Assert.AreEqual(oldHistory, JsonSerializer.Serialize(unit.CharacterMissions.Runtime.ReadHistory(oldAssignment.AssignmentId)));
                }
                Assert.IsEmpty(context.Drain());
                Assert.IsTrue(Complete(), "The same reward and ordered transitions must remain retryable.");
            }

            var expectedState = required ? MissionState.Failed : MissionState.Active;
            Assert.AreEqual((uint)MissionState.Completed, context.ReadMission(731).MissionState);
            Assert.AreEqual((uint)expectedState, context.ReadMission(732).MissionState);
            Assert.AreEqual(expectedState, context.Client.Player.Missions[732].State);
            Assert.AreEqual(MissionObjectiveState.Failed, context.Client.Player.Missions[732].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[733].Objectives[1].State);
            Assert.AreEqual(7U, context.Client.Player.Missions[733].Objectives[1].Counters[0]);
            Assert.AreEqual(6U, context.Client.Player.PlayerFlags[92]);
            Assert.AreEqual(5U, context.Client.Player.PlayerFlags[93]);
            Assert.AreEqual(before.Credits + 7, context.ReadRewardTotals().Credits);
            Assert.AreEqual(before.ItemCount + 7, context.ReadRewardTotals().ItemCount);
            using (var unit = context.CreateChar())
            {
                Assert.AreEqual(6U, unit.CharacterFlags.GetValue(1, 92));
                Assert.AreEqual(5U, unit.CharacterFlags.GetValue(1, 93));
                var history = unit.CharacterMissions.Runtime.ReadHistory(failedAttempt.AssignmentId);
                if (required)
                {
                    Assert.IsNotNull(history);
                    Assert.AreEqual((uint)MissionState.Failed, history.Outcome);
                    Assert.IsFalse(history.Rewarded);
                    Assert.IsNull(history.RewardedAtUtc);
                }
                else
                    Assert.IsNull(history, "An optional objective failure must not archive the active mission.");
                Assert.AreEqual(oldHistory, JsonSerializer.Serialize(unit.CharacterMissions.Runtime.ReadHistory(oldAssignment.AssignmentId)));
                Assert.IsTrue(unit.CharacterMissions.Runtime.EverSucceeded(1, 732));
                Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(oldAssignment.AssignmentId));
                Assert.IsFalse(unit.CharacterMissions.Runtime.WasRewarded(failedAttempt.AssignmentId));
                Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(assignment.AssignmentId));
            }
            Assert.HasCount(1, context.Drain().OfType<MissionRewardedPacket>().ToArray());
            Assert.IsFalse(Complete());
        }

        [TestMethod]
        public void RadioCompletionEnforcesDurableTurnInRequirementsAndInventoryCapacity()
        {
            using var context = RadioContext(RadioMission(turnIn: new FlagRequirement(92, 1)));
            Ready(context);
            var before = context.ReadRewardTotals();
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            context.Client.Player.PlayerFlags[92] = 1;
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(1, 92, 1));
            context.FillRewardCategory();
            var full = context.ReadRewardTotals();
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, 731, 0, null));
            Assert.AreEqual(full, context.ReadRewardTotals());
            Assert.AreEqual(before.Credits, full.Credits);
        }

        [TestMethod]
        public void TypedSceneOfferUsesItsExactOriginAndCanReofferAfterReconnectWithoutDuplicateNotification()
        {
            using var context = SceneOfferContext();
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 730, "offer"));
            var offer = ReadOffer(context);
            var source = context.ReadMission(730);
            Assert.AreEqual(MissionOfferSourceKind.Scene, offer.SourceKind);
            Assert.AreEqual(source.AssignmentId, offer.SourceAssignmentId);
            Assert.AreEqual(source.Generation, offer.SourceAssignmentGeneration);
            Assert.HasCount(1, context.Drain().OfType<DispenseRadioMissionPacket>().ToArray());
            Assert.IsFalse(context.Manager.Scenes.ExecuteNamed(context.Client, 730, "offer"),
                "The durable sequence inbox remains exactly once.");
            Assert.IsTrue(context.Manager.Scenes.Submit(offer.SourceInstanceId,
                new SceneObservation(SceneEventKind.Signal, offer.SourceGeneration, SequenceId: 1)));
            Assert.AreEqual(offer.OfferId, ReadOffer(context).OfferId);
            Assert.IsEmpty(context.Drain().OfType<DispenseRadioMissionPacket>());
            var reconnect = context.CreateCompetingClient();
            context.Manager.Scenes.Resume(reconnect);
            Assert.IsTrue(context.Manager.Scenes.Submit(offer.SourceInstanceId,
                new SceneObservation(SceneEventKind.Signal, offer.SourceGeneration, SequenceId: 1)));
            Assert.AreNotEqual(offer.OfferId, ReadOffer(context).OfferId);
            Assert.HasCount(1, MissionTestContext.Drain(reconnect).OfType<DispenseRadioMissionPacket>().ToArray());
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(reconnect, 731));
        }

        [TestMethod]
        [DataRow("scene-generation")]
        [DataRow("assignment")]
        [DataRow("assignment-generation")]
        [DataRow("participant-generation")]
        [DataRow("participant")]
        [DataRow("source-revision")]
        public void RadioOffersNeverRebindARetiredSceneOrAssignmentSource(string change)
        {
            using var context = SceneOfferContext();
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 730, "offer"));
            var offer = ReadOffer(context);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, 730);
                    var scene = unit.CharacterMissions.Runtime.Scene(offer.SourceInstanceId);
                    var participant = unit.CharacterMissions.Runtime.Participants(scene.RunId).Single();
                    switch (change)
                    {
                        case "scene-generation": scene.Generation++; break;
                        case "assignment": assignment.AssignmentId = Guid.NewGuid().ToString("N"); break;
                        case "assignment-generation": assignment.Generation++; break;
                        case "participant-generation": participant.AssignmentGeneration++; break;
                        case "participant": participant.Active = false; break;
                        case "source-revision": assignment.ContentRevision = "retired-source"; break;
                    }
                });
            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.AreEqual(MissionOfferState.Cancelled, ReadOffer(context).State);
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(731));
        }

        [TestMethod]
        public void ANewAuthorizedSourceGenerationReplacesAnInvalidOfferRatherThanRenewingIt()
        {
            using var context = SceneOfferContext();
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 730, "offer"));
            var original = ReadOffer(context);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Scene(original.SourceInstanceId).Generation++);
            Assert.IsTrue(context.Manager.Offers.TryOffer(context.Client, 731, new MissionOfferSourceIdentity(
                MissionOfferSourceKind.Scene, original.SourceKey, original.SourceInstanceId, original.SourceGeneration + 1,
                original.SourceAssignmentId, original.SourceAssignmentGeneration)));
            Assert.AreNotEqual(original.OfferId, ReadOffer(context).OfferId);
            Assert.AreEqual(original.SourceGeneration + 1, ReadOffer(context).SourceGeneration);
        }

        [TestMethod]
        public void TypedSceneOfferAndItsReceiptRollBackTogetherAndRetryThePendingInput()
        {
            using var context = SceneOfferContext();
            var fired = false;
            context.BeforeSave = db =>
            {
                if (db.ChangeTracker.Entries<CharacterMissionOfferEntry>().Any(entry => entry.State == EntityState.Added))
                {
                    fired = true;
                    throw new DbUpdateException("Injected scene offer persistence failure.");
                }
            };
            Assert.IsFalse(context.Manager.Scenes.ExecuteNamed(context.Client, 730, "offer"));
            context.BeforeSave = null;
            Assert.IsTrue(fired);
            Assert.IsNull(ReadOffer(context));
            Assert.IsEmpty(context.Drain().OfType<DispenseRadioMissionPacket>());
            Assert.IsTrue(context.Manager.Scenes.ExecuteNamed(context.Client, 730, "offer"));
            var offer = ReadOffer(context);
            using var unit = context.CreateChar();
            Assert.IsTrue(unit.CharacterMissions.Runtime.HasReceipt(offer.SourceInstanceId, offer.SourceGeneration, "radio-brief"));
            Assert.HasCount(1, context.Drain().OfType<DispenseRadioMissionPacket>().ToArray());
        }

        [TestMethod]
        public void ReinitializationPreservesRadioAssignmentsObjectivesHistoryAndReceipts()
        {
            using var context = new MissionTestContext();
            context.SeedCharacter(1, 0, 1);
            context.SeedMission(1, 731, (uint)MissionState.Completed, false);
            context.SeedMission(1, 732, (uint)MissionState.Failed, false);
            context.SeedObjective(1, 731, 1, MissionObjectiveState.Completed);
            context.SeedObjective(1, 732, 1, MissionObjectiveState.Failed);
            var now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
            string history;
            string assignments;
            using (var unit = context.CreateChar())
            {
                unit.ExecuteTransaction(() =>
                {
                    foreach (var assignment in unit.CharacterMissions.Get(1))
                    {
                        unit.CharacterMissions.Runtime.Archive(assignment, now);
                        if (assignment.MissionState == (uint)MissionState.Completed)
                            unit.CharacterMissions.Runtime.Add(new MissionReceiptEntry
                            {
                                OwnerId = assignment.AssignmentId, Generation = assignment.Generation,
                                OperationKey = "mission-reward", Kind = "Grant", CreatedAtUtc = now
                            });
                    }
                });
                history = JsonSerializer.Serialize(unit.CharacterMissions.Runtime.History(1));
                assignments = JsonSerializer.Serialize(unit.CharacterMissions.Get(1)
                    .Select(row => new { row.MissionId, row.AssignmentId, row.Generation, row.Version, row.ContentRevision, row.MissionState }));
            }
            using (var db = context.Open())
                db.Database.Migrate();
            using (var unit = context.CreateChar())
            {
                Assert.AreEqual(history, JsonSerializer.Serialize(unit.CharacterMissions.Runtime.History(1)));
                Assert.AreEqual(assignments, JsonSerializer.Serialize(unit.CharacterMissions.Get(1)
                    .Select(row => new { row.MissionId, row.AssignmentId, row.Generation, row.Version, row.ContentRevision, row.MissionState })));
                Assert.IsTrue(unit.CharacterMissions.Runtime.WasRewarded(
                    unit.CharacterMissions.GetByCharacterAndMission(1, 731).AssignmentId));
                Assert.IsEmpty(unit.MissionOffers.ForCharacter(1));
                Assert.HasCount(2, unit.CharacterMissionProgress.Get(1).Missions);
            }
        }

        private static MissionTestContext SceneOfferContext(MissionRequirement sourceRequirement = null)
        {
            var target = RadioMission(radioSources: new[]
                { new MissionOfferSourceDefinition(MissionOfferSourceKind.Scene, "data.sequence", Requirement: sourceRequirement) });
            var source = RadioMission(730, acceptance: MissionChannel.Npc, completion: MissionChannel.Npc);
            var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [730] = source, [731] = target });
            context.Manager.Scenes.Bind(730, "data.sequence", new SceneBindings("radio-v1",
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence> { [1] = new(characterIntents: new[] { new OfferRadioMissionIntent("radio-brief", 731) }) },
                new Dictionary<string, uint> { ["offer"] = 1 }));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(77).EntityId, 730));
            context.Drain();
            return context;
        }

        private static void ReadyNearLevelTen(MissionTestContext context)
        {
            using (var db = context.Open())
            {
                var character = db.CharacterEntries.Single(entry => entry.Id == 1);
                character.Level = 9;
                character.Experience = 272400;
                character.CloneCredits = 1;
                db.SaveChanges();
            }
            context.Client.Player.Level = 9;
            context.Client.Player.Experience = 272400;
            context.Client.Player.CloneCredits = 1;
            foreach (var attribute in Enum.GetValues<Attributes>())
                context.Client.Player.Attributes.TryAdd(attribute, new ActorAttributes(attribute, 0, 0, 0, 0, 0));
            Ready(context);
        }

        private static void Ready(MissionTestContext context)
        {
            Assert.IsTrue(Offer(context));
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, 731));
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(999)));
        }

        private static CharacterMissionOfferEntry ReadOffer(MissionTestContext context, uint missionId = 731)
        {
            using var unit = context.CreateChar();
            return unit.MissionOffers.Read(context.Client.Player.Id, missionId);
        }

        private static bool Offer(MissionTestContext context, uint missionId = 731) =>
            context.Manager.Offers.TryOffer(context.Client, missionId, MissionOfferSourceIdentity.ServerEvent("fixture.arrival"));

        private static MissionTestContext RadioContext(Mission definition = null, Func<DateTime> utcNow = null,
            IEnumerable<Mission> additionalDefinitions = null)
        {
            definition ??= RadioMission();
            var reward = new MissionRewardDefinition(100, new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 },
                new[] { new MissionRewardItem(28, 3) }, new[] { new MissionRewardItem(29, 2), new MissionRewardItem(29, 4) });
            var definitions = (additionalDefinitions ?? Array.Empty<Mission>()).Append(definition)
                .ToDictionary(mission => mission.MissionId);
            var context = MissionTestContext.WithCustomDefinitions(definitions,
                new Dictionary<uint, MissionRewardDefinition> { [definition.MissionId] = reward }, utcNow);
            context.AddRewardTemplate(28, 3147);
            context.AddRewardTemplate(29, 3147);
            return context;
        }

        private static MissionObjectiveDefinition RewardProgressObjective(uint objectiveId, IEnumerable<MissionActionDefinition> actions,
            MissionObjectiveState state = MissionObjectiveState.Completed, bool required = true,
            MissionProgressRule rule = null, IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters = null) =>
            new(objectiveId, 1001, 1002, new uint?[] { counters == null ? null : 1003U, null, null }, objectiveId,
                MissionObjectiveState.Incomplete, required,
                counters ?? new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(), executableTransitions: new[]
                {
                    new MissionObjectiveExecutableTransition(objectiveId, objectiveId, state,
                        null, rule ?? MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.MissionCompleted, 731),
                        counters, null, actions)
                });

        private static Mission RadioMission(uint missionId = 731, MissionRepeatPolicy repeat = null,
            MissionChannel acceptance = MissionChannel.Radio, MissionChannel completion = MissionChannel.Radio,
            MissionRequirement requirement = null, MissionRequirement turnIn = null,
            IEnumerable<MissionOfferSourceDefinition> radioSources = null) =>
            new(missionId, "Radio fixture", missionId,
                acceptance.HasFlag(MissionChannel.Npc) ? 77U : null,
                completion.HasFlag(MissionChannel.Npc) ? 88U : null, 1, 1, 2, false, false,
                new[] { Objective() }, enableOperational: true, contentRevision: "radio-v1",
                requirement: requirement, turnInRequirement: turnIn,
                repeatPolicy: repeat ?? new MissionRepeatPolicy(MissionRepeatKind.Immediate),
                acceptanceChannel: acceptance, completionChannel: completion,
                radioSources: radioSources ?? (acceptance.HasFlag(MissionChannel.Radio)
                    ? new[] { new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "fixture.arrival") } : null));

        private static MissionObjectiveDefinition Objective() =>
            new(1, 1001, 1002, new uint?[] { null, null, null }, 0,
                MissionObjectiveState.Incomplete, true,
                new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(),
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 999));
    }
}
