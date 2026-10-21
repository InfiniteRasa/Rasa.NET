extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.Missions;

namespace Rasa.Test.Missions
{
    /// <summary>
    /// A mission offered by radio when an earlier one is rewarded: a ServerEvent source keyed
    /// "mission.rewarded:{id}" (MissionOfferSourceDefinition.MissionRewardedKey). Mission 731 is
    /// the trigger, rewarded by radio; 732 and 733 follow it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionRadioFollowUpOfferTests
    {
        private const uint Trigger = 731;
        private const uint FollowUp = 732;
        // WorldTestContext's map: the Concordia Wilderness, map context 1220.
        private const uint FixtureMap = 1220;

        [TestMethod]
        public void MissionRewardedKeysRoundTripAndRejectEveryOtherForm()
        {
            Assert.AreEqual("mission.rewarded:867", MissionOfferSourceDefinition.MissionRewardedKey(867));
            Assert.IsTrue(MissionOfferSourceDefinition.TryParseMissionRewarded("mission.rewarded:867", out var id));
            Assert.AreEqual(867U, id);
            foreach (var key in new[] { "mission.rewarded:", "mission.rewarded:0", "mission.rewarded:0867",
                "mission.rewarded:+867", "mission.rewarded:-1", "mission.rewarded: 867", "mission.rewarded:867 ",
                "mission.rewarded:4294967296", "mission.rewarded:86x", "Mission.Rewarded:867", "map.arrival", null })
                Assert.IsFalse(MissionOfferSourceDefinition.TryParseMissionRewarded(key, out _), key ?? "null");

            Assert.IsNull(MissionOfferSourceDefinition.MissionRewarded(867).ValidationError);
            Assert.IsNotNull(new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "mission.rewarded:0").ValidationError);
            Assert.IsNotNull(new MissionOfferSourceDefinition(MissionOfferSourceKind.Scene, "mission.rewarded:867").ValidationError);
            var malformed = FollowUpMission(FollowUp, new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "mission.rewarded:0867"));
            Assert.IsFalse(malformed.IsOperational);
        }

        [TestMethod]
        public void RadioTurnInOffersTheFollowUpOnceAndItCanBeAccepted()
        {
            using var context = FollowUpContext(FollowUpMission(FollowUp));
            Ready(context);
            Assert.IsEmpty(context.Drain().OfType<DispenseRadioMissionPacket>().Where(offer => offer.MissionId == FollowUp));

            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));

            var packets = context.Drain();
            var offer = packets.OfType<DispenseRadioMissionPacket>().Single();
            Assert.AreEqual(FollowUp, offer.MissionId);
            Assert.IsTrue(offer.ForceDialog);
            Assert.IsLessThan(packets.IndexOf(offer), packets.FindIndex(packet => packet is MissionRewardedPacket),
                "The offer comes after the reward.");
            var pending = ReadOffer(context, FollowUp);
            Assert.AreEqual(MissionOfferState.Pending, pending.State);
            Assert.AreEqual("mission.rewarded:731", pending.SourceKey);

            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, FollowUp));
            Assert.AreEqual(FollowUp, context.Drain().OfType<MissionGainedPacket>().Single().MissionId);
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[FollowUp].State);
            Assert.AreEqual(MissionOfferState.Consumed, ReadOffer(context, FollowUp).State);
        }

        [TestMethod]
        public void TheRadioTurnInPacketOffersTheFollowUp()
        {
            using var context = FollowUpContext(FollowUpMission(FollowUp));
            Ready(context);
            context.Drain();
            WithHandler(context, (router, handler) =>
                router.RoutePacket(handler, new CompleteRadioMissionPacket { MissionId = Trigger, SelectionIdx = 1 }));
            Assert.AreEqual(FollowUp, context.Drain().OfType<DispenseRadioMissionPacket>().Single().MissionId);
        }

        [TestMethod]
        public void PayingAnOwedRadioRewardOffersTheFollowUp()
        {
            using var context = FollowUpContext(FollowUpMission(FollowUp));
            context.SeedMission(1, Trigger, (uint)MissionState.Success, false);
            context.ReloadPlayerMissions();
            context.Drain();
            WithHandler(context, (router, handler) =>
                router.RoutePacket(handler, new RewardRadioMissionPacket { MissionId = Trigger, SelectionIdx = 0 }));
            Assert.AreEqual(MissionState.Completed, context.Client.Player.Missions[Trigger].State);
            Assert.AreEqual(FollowUp, context.Drain().OfType<DispenseRadioMissionPacket>().Single().MissionId);
        }

        [TestMethod]
        [DataRow("held")]
        [DataRow("succeeded")]
        [DataRow("journal full")]
        [DataRow("requirement missing")]
        [DataRow("source requirement missing")]
        [DataRow("other map")]
        [DataRow("other trigger")]
        public void NoFollowUpOfferWhenTheMissionIsNotAdmissible(string reason)
        {
            var otherMap = reason == "other map";
            var followUp = FollowUpMission(FollowUp,
                triggeredBy: reason == "other trigger" ? Trigger + 100 : Trigger,
                requirement: reason == "requirement missing"
                    ? new AllRequirements(new MissionRequirement[] { Completed(Trigger), new FlagRequirement(91, 1) })
                    : null,
                repeat: new MissionRepeatPolicy(MissionRepeatKind.Once),
                otherMap: otherMap,
                sourceRequirement: reason == "source requirement missing" ? new FlagRequirement(91, 1) : null);
            using var context = FollowUpContext(followUp);
            if (reason == "held")
                context.SeedMission(1, FollowUp, (uint)MissionState.Active, false);
            if (reason == "succeeded")
                context.SeedMission(1, FollowUp, (uint)MissionState.Completed, false);
            if (reason is "held" or "succeeded")
                context.ReloadPlayerMissions();
            Ready(context);
            if (reason == "journal full")
                // 30 active missions besides the trigger: the trigger leaves the count when it is rewarded.
                for (uint mission = 1000; mission < 1030; mission++)
                    context.SeedMission(1, mission, (uint)MissionState.Active, false);
            context.Drain();

            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));

            var packets = context.Drain();
            Assert.HasCount(1, packets.OfType<MissionRewardedPacket>().ToArray());
            Assert.IsEmpty(packets.OfType<DispenseRadioMissionPacket>().ToArray(), reason);
            Assert.IsNull(ReadOffer(context, FollowUp), reason);
        }

        [TestMethod]
        public void TwoFollowUpsOfOneTriggerAreBothOfferedAndAccepted()
        {
            using var context = FollowUpContext(FollowUpMission(FollowUp), FollowUpMission(733));
            Ready(context);
            context.Drain();

            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));

            CollectionAssert.AreEqual(new[] { FollowUp, 733U },
                context.Drain().OfType<DispenseRadioMissionPacket>().Select(offer => offer.MissionId).ToArray());
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, FollowUp));
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, 733));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AFollowUpOfTwoTriggersIsOfferedAfterTheLastOfThem(bool otherTriggerDone)
        {
            const uint otherTrigger = 734;
            var followUp = FollowUpMission(FollowUp,
                MissionOfferSourceDefinition.MissionRewarded(otherTrigger),
                requirement: new AllRequirements(new MissionRequirement[] { Completed(Trigger), Completed(otherTrigger) }));
            using var context = FollowUpContext(followUp, RadioMission(otherTrigger));
            if (otherTriggerDone)
            {
                context.SeedMission(1, otherTrigger, (uint)MissionState.Completed, false);
                context.ReloadPlayerMissions();
            }
            Ready(context);
            context.Drain();

            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));

            var offers = context.Drain().OfType<DispenseRadioMissionPacket>().ToArray();
            if (!otherTriggerDone)
            {
                Assert.IsEmpty(offers);
                return;
            }
            Assert.AreEqual(FollowUp, offers.Single().MissionId);
            Assert.AreEqual("mission.rewarded:731", ReadOffer(context, FollowUp).SourceKey);
        }

        [TestMethod]
        public void AFailedRewardSaveSendsNoOfferAndItsRetryOffersOnce()
        {
            using var context = FollowUpContext(FollowUpMission(FollowUp));
            Ready(context);
            context.Drain();
            var fired = false;
            context.BeforeSave = db =>
            {
                if (db.ChangeTracker.Entries<MissionReceiptEntry>().Any(entry => entry.Entity.OperationKey == "mission-reward"))
                {
                    fired = true;
                    throw new DbUpdateException("Injected radio reward receipt failure.");
                }
            };
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));
            context.BeforeSave = null;
            Assert.IsTrue(fired);
            Assert.IsEmpty(context.Drain().OfType<DispenseRadioMissionPacket>().ToArray());
            Assert.IsNull(ReadOffer(context, FollowUp));

            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));
            Assert.AreEqual(FollowUp, context.Drain().OfType<DispenseRadioMissionPacket>().Single().MissionId);
            Assert.IsFalse(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));
            Assert.IsEmpty(context.Drain().OfType<DispenseRadioMissionPacket>().ToArray());
        }

        [TestMethod]
        public void AnOfferNotTakenComesAgainOnTheNextArrivalAfterItExpires()
        {
            var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
            using var context = FollowUpContext(FollowUpMission(FollowUp), utcNow: () => now);
            Ready(context);
            context.Drain();
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));
            Assert.AreEqual(FollowUp, context.Drain().OfType<DispenseRadioMissionPacket>().Single().MissionId);

            now = now.AddMinutes(5);
            context.Manager.OfferArrivalMissions(context.Client);

            Assert.AreEqual(FollowUp, context.Drain().OfType<DispenseRadioMissionPacket>().Single().MissionId);
            Assert.AreEqual(MissionOfferSourceDefinition.MapArrivalKey, ReadOffer(context, FollowUp).SourceKey);
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, FollowUp));
            context.Manager.OfferArrivalMissions(context.Client);
            Assert.IsEmpty(context.Drain().OfType<DispenseRadioMissionPacket>().ToArray(), "Held: no offer on arrival.");
        }

        [TestMethod]
        public void TheArrivalSourceOffersNothingBeforeTheTriggerIsRewarded()
        {
            using var context = FollowUpContext(FollowUpMission(FollowUp));
            context.Manager.OfferArrivalMissions(context.Client);
            Assert.IsEmpty(context.Drain().OfType<DispenseRadioMissionPacket>().Where(offer => offer.MissionId == FollowUp));
        }

        [TestMethod]
        public void AnOfferIsRefusedAtAcceptWhenTheMissionWasTakenAnotherWay()
        {
            using var context = FollowUpContext(FollowUpMission(FollowUp));
            Ready(context);
            Assert.IsTrue(context.Manager.TryCompleteRadioMission(context.Client, Trigger, 1, null));
            Assert.AreEqual(MissionOfferState.Pending, ReadOffer(context, FollowUp).State);
            context.SeedMission(1, FollowUp, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            context.Drain();

            Assert.IsFalse(context.Manager.TryAcceptRadioMission(context.Client, FollowUp));
            Assert.IsEmpty(context.Drain().OfType<MissionGainedPacket>().ToArray());
        }

        private static MissionStateRequirement Completed(uint missionId) => new(missionId, MissionState.Completed);

        private static void Ready(MissionTestContext context)
        {
            Assert.IsTrue(context.Manager.Offers.TryOffer(context.Client, Trigger,
                MissionOfferSourceIdentity.ServerEvent("fixture.arrival")));
            Assert.IsTrue(context.Manager.TryAcceptRadioMission(context.Client, Trigger));
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(999)));
        }

        private static CharacterMissionOfferEntry ReadOffer(MissionTestContext context, uint missionId)
        {
            using var unit = context.CreateChar();
            return unit.MissionOffers.Read(context.Client.Player.Id, missionId);
        }

        private static void WithHandler(MissionTestContext context,
            Action<PacketRouter<ClientPacketHandler, GameOpcode>, ClientPacketHandler> body)
        {
            var singleton = typeof(MissionApplication).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);
            try
            {
                var handler = new ClientPacketHandler();
                handler.RegisterClient(context.Client);
                body(new PacketRouter<ClientPacketHandler, GameOpcode>(), handler);
            }
            finally { singleton.SetValue(null, previous); }
        }

        /// <summary>
        /// The trigger 731 is the primary definition: RadioContext gives a reward only to it.
        /// </summary>
        private static MissionTestContext FollowUpContext(params Mission[] followUps) =>
            FollowUpContext(followUps, null);

        private static MissionTestContext FollowUpContext(Mission followUp, Func<DateTime> utcNow) =>
            FollowUpContext(new[] { followUp }, utcNow);

        private static MissionTestContext FollowUpContext(IEnumerable<Mission> followUps, Func<DateTime> utcNow)
        {
            var trigger = RadioMission(Trigger);
            var reward = new MissionRewardDefinition(100, new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 },
                new[] { new MissionRewardItem(28, 3) }, new[] { new MissionRewardItem(29, 2), new MissionRewardItem(29, 4) });
            var definitions = followUps.Append(trigger).ToDictionary(mission => mission.MissionId);
            var context = MissionTestContext.WithCustomDefinitions(definitions,
                new Dictionary<uint, MissionRewardDefinition> { [Trigger] = reward }, utcNow);
            context.AddRewardTemplate(28, 3147);
            context.AddRewardTemplate(29, 3147);
            foreach (var mission in definitions.Values)
                Assert.IsTrue(mission.IsOperational, mission.OperationalDiagnostic);
            return context;
        }

        /// <summary>
        /// A follow-up of 731 as content authors it: radio acceptance, the mission-rewarded source,
        /// the map-arrival source of its map as the fallback, and 731 completed as its requirement
        /// (custom contexts have no prerequisite rows). The fixture map is MissionTestContext's
        /// world map; otherMap moves the mission-rewarded source to another map.
        /// </summary>
        private static Mission FollowUpMission(uint missionId, MissionOfferSourceDefinition extraSource = null,
            MissionRequirement requirement = null, MissionRepeatPolicy repeat = null, bool otherMap = false,
            uint triggeredBy = Trigger, MissionRequirement sourceRequirement = null)
        {
            var sources = new List<MissionOfferSourceDefinition>
            {
                MissionOfferSourceDefinition.MissionRewarded(triggeredBy, otherMap ? FixtureMap + 1 : null, sourceRequirement),
                new(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, FixtureMap)
            };
            if (extraSource != null)
                sources.Add(extraSource);
            return RadioMission(missionId, requirement ?? Completed(Trigger), repeat, sources);
        }

        private static Mission RadioMission(uint missionId, MissionRequirement requirement = null,
            MissionRepeatPolicy repeat = null, IEnumerable<MissionOfferSourceDefinition> radioSources = null) =>
            new(missionId, "Radio fixture", missionId, null, null, 1, 1, 2, false, false,
                new[] { Objective() }, enableOperational: true, contentRevision: "radio-v1",
                requirement: requirement,
                repeatPolicy: repeat ?? new MissionRepeatPolicy(MissionRepeatKind.Immediate),
                acceptanceChannel: MissionChannel.Radio, completionChannel: MissionChannel.Radio,
                radioSources: radioSources ?? new[] { new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "fixture.arrival") });

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
