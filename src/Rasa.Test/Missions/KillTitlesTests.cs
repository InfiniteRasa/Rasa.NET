using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Encounters;

    /// <summary>
    /// The kill titles: "Wilderness Bug Zapper" is what objective 3 of the Wilderness Targets of
    /// Opportunity gives, "Kill 40 Xanx!", and the fourteen other battlefields have theirs.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class KillTitlesTests
    {
        private const uint Wilderness = 1449;
        private const uint KillXanx = 3;                // "Kill 40 Xanx!"
        private const uint BugZapper = 365;             // "Wilderness Bug Zapper"
        private const uint WildernessMap = BootcampRuntimeTestHarness.WildernessMapContextId;

        private const uint Xanx = TargetsOfOpportunitySeed.Flag.Xanx;
        private const uint Thrax = TargetsOfOpportunitySeed.Flag.Thrax;
        private const uint Stalker = TargetsOfOpportunitySeed.Flag.Stalker;
        private const uint Strider = TargetsOfOpportunitySeed.Flag.Strider;

        #region Counting a kill by what was killed

        [TestMethod]
        public void AKillCountsForTheFlagsOfItsClassAndOnceHoweverManyAreNamed()
        {
            using var context = Counting(MissionProgressEventKind.CreatureFlagKilled, new[] { Stalker, Strider }, 3);

            // A walker that is both: one kill, one count.
            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(55, 4001, Stalker, Strider), Vector3.Zero));
            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);

            // Either is enough.
            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(56, 4002, Strider), Vector3.Zero));
            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);

            // A Thrax is neither.
            Assert.IsFalse(context.Manager.Credit.RecordKill(context.Client, Kill(57, 4003, Thrax), Vector3.Zero));
            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);

            var counter = context.Drain().OfType<UpdateObjectiveCounterPacket>().Last();
            Assert.AreEqual(2U, counter.CounterValue);
            Assert.AreEqual(3U, counter.TargetValue);
        }

        [TestMethod]
        public void AKillCountsForItsClass()
        {
            using var context = Counting(MissionProgressEventKind.CreatureClassKilled, new uint[] { 6960, 6961 }, 2);

            Assert.IsFalse(context.Manager.Credit.RecordKill(context.Client, Kill(55, 4001, Thrax), Vector3.Zero));
            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(56, 6961), Vector3.Zero));
            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(57, 6960), Vector3.Zero));

            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[1].State);
        }

        [TestMethod]
        public void TheSquadSharesTheKillAndEachCountsItOnce()
        {
            using var context = Counting(MissionProgressEventKind.CreatureFlagKilled, new[] { Stalker, Strider }, 3,
                new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20));
            var near = context.CreateAdditionalClient(2);
            var far = context.CreateAdditionalClient(3);
            var giver = EntityManager.Instance.Creatures.Values.Single(creature => creature.DbId == 77);

            foreach (var client in new[] { near, far })
                Assert.IsTrue(context.Manager.AcceptOfferedMission(client, giver.EntityId, 321));

            far.Player.Position = new Vector3(100, 0, 0);
            using var party = new GroupMissionCreditTests.PartyScope(context.Client, near, far);

            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(55, 4001, Stalker, Strider), Vector3.Zero));
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
            Assert.AreEqual(1U, near.Player.Missions[321].Objectives[1].Counters[0], "a kill under two of its names is still one kill");
            Assert.AreEqual(0U, far.Player.Missions[321].Objectives[1].Counters[0]);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void AnObjectiveOfAPlaceCountsOnlyThere(bool there)
        {
            var map = 4242u;
            using var context = Counting(MissionProgressEventKind.CreatureFlagKilled, new[] { Xanx }, 3, here: mapContextId =>
            {
                map = mapContextId;
                return new MapRequirement(there ? new[] { 77777u, mapContextId } : new[] { 77777u });
            });

            Assert.AreEqual(there, context.Manager.Credit.RecordKill(context.Client, Kill(55, 4001, Xanx), Vector3.Zero), $"on map {map}");
            Assert.AreEqual(there ? 1U : 0U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
        }

        [TestMethod]
        public void TheKillOfACreatureGoesByItsRowItsClassAndItsFlags()
        {
            using var context = Counting(MissionProgressEventKind.CreatureFlagKilled, new[] { Xanx }, 3);
            var entityClass = new EntityClass(4001, "kill_titles_xanx", 0, 0, new List<AugmentationType>(), true);
            entityClass.CreatureFlags.Add((CreatureFlag)Xanx);
            entityClass.CreatureFlags.Add((CreatureFlag)43);
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)4001] = entityClass;

            try
            {
                var events = CreatureManager.KillEvents(new Creature { DbId = 55, EntityClass = (EntityClasses)4001 });

                CollectionAssert.AreEqual(
                    new[]
                    {
                        (MissionProgressEventKind.CreatureKilled, 55u),
                        (MissionProgressEventKind.CreatureClassKilled, 4001u),
                        (MissionProgressEventKind.CreatureFlagKilled, Xanx),
                        (MissionProgressEventKind.CreatureFlagKilled, 43u)
                    },
                    events.Select(progress => (progress.Kind, progress.SubjectId)).ToArray());

                // A scene's own actor: the scene has the kill of the creature, the kind is still counted.
                Assert.IsFalse(CreatureManager.KillEvents(new Creature { DbId = 55, EntityClass = (EntityClasses)4001 }, false)
                    .Any(progress => progress.Kind == MissionProgressEventKind.CreatureKilled));
            }
            finally
            {
                EntityClassManager.Instance.LoadedEntityClasses.Remove((EntityClasses)4001);
            }
        }

        #endregion

        #region Authoring: several kill triggers, one counter

        [TestMethod]
        public void SeveralKillTriggersOnOneTransitionAreOneCounter()
        {
            var triggers = new[] { Stalker, Strider }.Select((flag, index) => Trigger((uint)index + 1, MissionProgressEventKind.CreatureFlagKilled, flag, 10)).ToArray();

            Assert.IsTrue(MissionProgressRuleAuthoring.TryBuild(triggers, out var rule, out var counters, out _, out var diagnostic), diagnostic);

            Assert.AreEqual(MissionProgressEventKind.CreatureFlagKilled, rule.Kind);
            CollectionAssert.AreEqual(new[] { Stalker, Strider }, rule.Subjects.ToArray());
            Assert.AreEqual(0U, rule.CounterId);
            Assert.AreEqual(10U, rule.TargetValue);
            Assert.AreEqual(10U, counters[0].TargetValue);
        }

        [TestMethod]
        public void KillTriggersThatDisagreeAreRefused()
        {
            var counts = new[]
            {
                Trigger(1, MissionProgressEventKind.CreatureFlagKilled, Stalker, 10),
                Trigger(2, MissionProgressEventKind.CreatureFlagKilled, Strider, 5)
            };
            var kinds = new[]
            {
                Trigger(1, MissionProgressEventKind.CreatureFlagKilled, Stalker, 10),
                Trigger(2, MissionProgressEventKind.CreatureClassKilled, 6961, 10)
            };

            foreach (var triggers in new[] { counts, kinds })
            {
                Assert.IsFalse(MissionProgressRuleAuthoring.TryBuild(triggers, out var rule, out _, out _, out var diagnostic));
                Assert.IsNull(rule);
                StringAssert.StartsWith(diagnostic, "several kill triggers on one transition");
            }
        }

        [TestMethod]
        public void ACounterOverSeveralSubjectsIsForKills()
        {
            Assert.IsTrue(MissionProgressRule.IsKill(MissionProgressEventKind.CreatureKilled));
            Assert.IsTrue(MissionProgressRule.IsKill(MissionProgressEventKind.CreatureFlagKilled));
            Assert.IsTrue(MissionProgressRule.IsKill(MissionProgressEventKind.CreatureClassKilled));
            Assert.IsFalse(MissionProgressRule.IsKill(MissionProgressEventKind.InteractionUsed));

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                MissionProgressRule.IncrementCounterOnAnySubject(MissionProgressEventKind.InteractionUsed, new uint[] { 1, 2 }, 0, 0, 5));
        }

        #endregion

        #region The map requirement

        [TestMethod]
        public void AMapRequirementIsMetOnItsMapsOnly()
        {
            var evaluator = new MissionRequirementEvaluator();
            var requirement = new MapRequirement(new uint[] { 1220, 1416 });
            MissionRequirementFacts On(uint map) => new(1, new Dictionary<uint, MissionState>(), new Dictionary<uint, MissionState>(),
                new Dictionary<uint, uint>(), MapContextId: map);

            Assert.IsTrue(evaluator.Evaluate(requirement, On(1220)));
            Assert.IsTrue(evaluator.Evaluate(requirement, On(1416)));
            Assert.IsFalse(evaluator.Evaluate(requirement, On(1148)));
            Assert.IsFalse(evaluator.Evaluate(requirement, On(0)));
            Assert.IsEmpty(evaluator.RequiredFacts(requirement).ToArray());

            foreach (var malformed in new[] { new MapRequirement(null), new MapRequirement(Array.Empty<uint>()), new MapRequirement(new uint[] { 1220, 0 }) })
                Assert.ThrowsExactly<MissionRuleException>(() => evaluator.RequiredFacts(malformed));
        }

        [TestMethod]
        public void AMapRequirementIsWrittenAsItsKindAndItsMaps()
        {
            var scene = new MissionSceneDefinition
            {
                ObjectiveRequirements = { [3] = new MapRequirement(new uint[] { 1220, 1416 }) },
                Titles = new Dictionary<uint, uint> { [3] = BugZapper },
                Category = 10000044
            };

            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);

            StringAssert.Contains(json, "\"objectiveRequirements\":{\"3\":{\"$kind\":\"map\",\"maps\":[1220,1416]}}");
            StringAssert.Contains(json, "\"titles\":{\"3\":365}");
            StringAssert.Contains(json, "\"category\":10000044");

            var read = JsonSerializer.Deserialize<MissionSceneDefinition>(json, MissionContentCodec.Options);
            CollectionAssert.AreEqual(new uint[] { 1220, 1416 }, ((MapRequirement)read.ObjectiveRequirements[3]).Maps.ToArray());
            Assert.AreEqual(BugZapper, read.Titles[3]);
            Assert.AreEqual(10000044u, read.Category);

            // A binding that says nothing of them writes nothing of them.
            var plain = JsonSerializer.Serialize(new MissionSceneDefinition(), MissionContentCodec.Options);
            Assert.IsFalse(plain.Contains("titles") || plain.Contains("category"), plain);
        }

        #endregion

        #region The title of an objective

        [TestMethod]
        public void CompletingATitledObjectiveGivesItsTitleOnce()
        {
            using var context = Counting(MissionProgressEventKind.CreatureFlagKilled, new[] { Xanx }, 2, titleId: BugZapper);

            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(55, 4001, Xanx), Vector3.Zero));
            Assert.IsEmpty(context.Drain().OfType<TitleAddedPacket>().ToArray(), "not before the count is full");
            Assert.IsFalse(context.Client.Player.Titles.Contains(BugZapper));

            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(56, 4001, Xanx), Vector3.Zero));

            var packets = context.Drain();
            Assert.AreEqual(BugZapper, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(IndexOf<ObjectiveCompletedPacket>(packets) < IndexOf<TitleAddedPacket>(packets), "the objective, then what it gave");
            Assert.IsTrue(context.Client.Player.Titles.Contains(BugZapper));

            using (var unit = context.CreateChar())
                CollectionAssert.Contains(unit.CharacterTitles.Get(context.Client.Player.Id), BugZapper);

            // Done: further Xanx are nothing to it.
            Assert.IsFalse(context.Manager.Credit.RecordKill(context.Client, Kill(57, 4001, Xanx), Vector3.Zero));
            Assert.IsEmpty(context.Drain().OfType<TitleAddedPacket>().ToArray());
        }

        [TestMethod]
        public void ATitleTheCharacterHasIsNotGivenAgain()
        {
            using var context = Counting(MissionProgressEventKind.CreatureFlagKilled, new[] { Xanx }, 1, titleId: BugZapper);

            using (var unit = context.CreateChar())
                Assert.IsTrue(unit.CharacterTitles.Add(context.Client.Player.Id, BugZapper));

            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(55, 4001, Xanx), Vector3.Zero));

            var packets = context.Drain();
            Assert.HasCount(1, packets.OfType<ObjectiveCompletedPacket>().ToArray());
            Assert.IsEmpty(packets.OfType<TitleAddedPacket>().ToArray());
        }

        [TestMethod]
        public void AnObjectiveWithoutATitleGivesNone()
        {
            using var context = Counting(MissionProgressEventKind.CreatureFlagKilled, new[] { Xanx }, 1);

            Assert.IsTrue(context.Manager.Credit.RecordKill(context.Client, Kill(55, 4001, Xanx), Vector3.Zero));

            Assert.IsEmpty(context.Drain().OfType<TitleAddedPacket>().ToArray());
            using var unit = context.CreateChar();
            Assert.IsEmpty(unit.CharacterTitles.Get(context.Client.Player.Id));
        }

        #endregion

        #region The seed

        [TestMethod]
        public void TheSeedIsFifteenMissionsAndSixtyThreeKills()
        {
            var zones = TargetsOfOpportunitySeed.Zones;
            var kills = zones.SelectMany(zone => zone.Kills).ToArray();

            Assert.HasCount(15, zones);
            Assert.HasCount(63, kills);
            Assert.HasCount(15, zones.Select(zone => zone.MissionId).Distinct().ToArray());
            Assert.HasCount(15, zones.Select(zone => zone.MapContextId).Distinct().ToArray());
            Assert.HasCount(63, kills.Select(kill => kill.TitleId).Distinct().ToArray(), "a title is one objective's");

            foreach (var zone in zones)
            {
                Assert.AreEqual(zone.MapContextId, zone.Maps[0], $"{zone.Name}: the battlefield first, then its instances");
                Assert.HasCount(zone.Maps.Count, zone.Maps.Distinct().ToArray());
                Assert.IsFalse(zone.Kills.Any(kill => kill.ObjectiveId == zone.AllObjectiveId), zone.Name);
                Assert.HasCount(zone.Kills.Count, zone.Kills.Select(kill => kill.ObjectiveId).Distinct().ToArray());

                foreach (var kill in zone.Kills)
                {
                    Assert.IsTrue(kill.Count > 0 && kill.Subjects.Count > 0 && kill.Subjects.All(subject => subject != 0), kill.Title);
                    Assert.IsTrue(kill.NameTextId != 0 && kill.BodyTextId != 0 && kill.CounterTextId != 0, kill.Title);
                    Assert.IsTrue(MissionProgressRule.IsKill(kill.EventKind), kill.Title);
                }
            }

            // The comments fit their columns, and the SQL carries nothing MySQL would read as an escape.
            foreach (var row in TargetsOfOpportunitySeed.Definitions.Concat(TargetsOfOpportunitySeed.Objectives)
                .Concat(TargetsOfOpportunitySeed.Transitions).Concat(TargetsOfOpportunitySeed.Triggers).Concat(TargetsOfOpportunitySeed.Actions))
                Assert.IsTrue(((string)row[^1]).Length <= 64, (string)row[^1]);

            foreach (var row in TargetsOfOpportunitySeed.Evidence)
                Assert.IsTrue(((string)row[^1]).Length <= 256 && ((string)row[7]).Length <= 256, (string)row[^1]);

            foreach (var statement in TargetsOfOpportunitySeed.InsertStatements.Concat(TargetsOfOpportunitySeed.DeleteStatements))
                Assert.IsFalse(statement.Contains('\\'), statement);
        }

        [TestMethod]
        public void TheBindingOfAMissionIsItsTitlesItsMapsAndSquadCredit()
        {
            var zone = TargetsOfOpportunitySeed.Zones.Single(candidate => candidate.MissionId == Wilderness);
            var scene = TargetsOfOpportunitySeed.Scene(zone);

            Assert.IsNull(scene.Script, "no scene: counters and nothing else");
            Assert.AreEqual(10000044u, scene.Category, "Battlefield (Wilderness)");
            Assert.AreEqual(BugZapper, scene.Titles[KillXanx]);
            CollectionAssert.AreEquivalent(zone.Kills.Select(kill => kill.ObjectiveId).ToArray(), scene.Titles.Keys.ToArray());
            CollectionAssert.AreEquivalent(scene.Titles.Keys.ToArray(), scene.Credit.Keys.ToArray());
            CollectionAssert.AreEquivalent(scene.Titles.Keys.ToArray(), scene.ObjectiveRequirements.Keys.ToArray());
            Assert.AreEqual(MissionCreditMode.NearbyParty, scene.Credit[KillXanx].Mode);
            Assert.AreEqual(PartyManager.LootShareRange, scene.Credit[KillXanx].Radius, "the squad shares a kill as far as it shares its loot");
            CollectionAssert.AreEqual(new uint[] { 1220, 1416, 1430, 1506, 1721, 2368 }, ((MapRequirement)scene.ObjectiveRequirements[KillXanx]).Maps.ToArray());

            // Crucible's category is one a byte holds: the definition's own.
            var crucible = TargetsOfOpportunitySeed.Zones.Single(candidate => candidate.Name == "Crucible");
            Assert.IsNull(TargetsOfOpportunitySeed.Scene(crucible).Category);
            Assert.AreEqual((byte)1, TargetsOfOpportunitySeed.Definitions.Single(row => (uint)row[0] == crucible.MissionId)[10]);

            var source = JsonSerializer.Deserialize<MissionOfferSourceDefinition[]>(TargetsOfOpportunitySeed.RadioSources(zone)).Single();
            Assert.AreEqual(MissionOfferSourceKind.ServerEvent, source.Kind);
            Assert.AreEqual(MissionOfferSourceDefinition.MapArrivalKey, source.Key);
            Assert.AreEqual(WildernessMap, source.MapContextId);
            Assert.IsNull(source.ValidationError);
        }

        [TestMethod]
        public void WhatTheSeedNamesIsInTheWorld()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var world = harness.WorldContext;
            var flags = world.Set<CreatureClassFlagEntry>().AsNoTracking().Select(row => row.FlagId).Distinct().ToHashSet();
            var classes = world.Set<EntityClassEntry>().AsNoTracking().Select(row => row.Id).ToHashSet();
            var maps = world.Set<MapInfoEntry>().AsNoTracking().Select(row => row.Id).ToHashSet();

            foreach (var zone in TargetsOfOpportunitySeed.Zones)
            {
                foreach (var map in zone.Maps)
                    Assert.IsTrue(maps.Contains(map), $"{zone.Name}: map {map}");

                foreach (var kill in zone.Kills)
                    foreach (var subject in kill.Subjects)
                        Assert.IsTrue((kill.EventKind == MissionProgressEventKind.CreatureFlagKilled ? flags : classes).Contains(subject),
                            $"{kill.Title}: {kill.EventKind} {subject}");
            }

            Assert.AreEqual(15, world.Set<MissionContentDefinitionEntry>().AsNoTracking().Count(row => row.ContentRevision == TargetsOfOpportunitySeed.Revision));
            Assert.AreEqual(63 + 15, world.Set<MissionObjectiveDefinitionEntry>().AsNoTracking().Count(row => row.ContentRevision == TargetsOfOpportunitySeed.Revision));
            Assert.AreEqual(TargetsOfOpportunitySeed.Zones.Sum(zone => zone.Kills.Sum(kill => kill.Subjects.Count)),
                world.Set<MissionTriggerEntry>().AsNoTracking().Count(row => row.ContentRevision == TargetsOfOpportunitySeed.Revision));
            Assert.AreEqual(45, world.Set<MissionEvidenceEntry>().AsNoTracking().Count(row => row.ContentRevision == TargetsOfOpportunitySeed.Revision));
        }

        #endregion

        #region The missions in the world

        [TestMethod]
        public void TheFifteenMissionsStandUp()
        {
            using var harness = BootcampRuntimeTestHarness.Create();

            foreach (var zone in TargetsOfOpportunitySeed.Zones)
            {
                Assert.IsTrue(harness.Manager.LoadedMissions.TryGetValue(zone.MissionId, out var mission), zone.Name);
                Assert.IsTrue(mission.IsOperational, $"{zone.Name}: {mission.OperationalDiagnostic}");
                Assert.AreEqual(TargetsOfOpportunitySeed.Revision, mission.ContentRevision);
                Assert.AreEqual(MissionChannel.Radio, mission.AcceptanceChannel);
                Assert.AreEqual(MissionChannel.Radio, mission.CompletionChannel);
                Assert.HasCount(zone.Kills.Count + 1, mission.Objectives);

                var all = mission.Objectives[zone.AllObjectiveId];
                Assert.IsTrue(all.IsRequired.Value, "the one required objective: nothing completes it, the mission stays");
                Assert.IsEmpty(all.GetExecutableTransitionsOrLegacyDefault());
                Assert.IsNull(all.TitleId);

                foreach (var kill in zone.Kills)
                {
                    var objective = mission.Objectives[kill.ObjectiveId];
                    Assert.IsFalse(objective.IsRequired.Value, kill.Title);
                    Assert.AreEqual(kill.TitleId, objective.TitleId, kill.Title);
                    Assert.AreEqual(MissionCreditMode.NearbyParty, objective.CreditPolicy.Mode, kill.Title);
                    Assert.AreEqual(kill.EventKind, objective.ProgressRule.Kind, kill.Title);
                    CollectionAssert.AreEquivalent(kill.Subjects.ToArray(), objective.ProgressRule.Subjects.ToArray(), kill.Title);
                    Assert.AreEqual(kill.Count, objective.Counters[0].TargetValue, kill.Title);
                    Assert.AreEqual(kill.CounterTextId, objective.ClientCounterTextIds[0], kill.Title);
                    CollectionAssert.AreEqual(zone.Maps.ToArray(), ((MapRequirement)mission.ObjectiveRequirements[kill.ObjectiveId]).Maps.ToArray(), kill.Title);
                }
            }

            // The log files it under its battlefield: a category past the definition's byte.
            var wilderness = harness.Manager.LoadedMissions[Wilderness];
            Assert.AreEqual(10000044u, wilderness.ClientCategoryId);
            Assert.AreEqual(10000044u, wilderness.CreateInfo(MissionState.Active, false,
                wilderness.CreateInitialObjectiveLogs()).MissionConstantData.CategoryId);
        }

        [TestMethod]
        public void ArrivingOnTheBattlefieldOffersItsMissionByRadio()
        {
            using var harness = BootcampRuntimeTestHarness.Create();

            // Bootcamp is nobody's battlefield.
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsFalse(harness.Drain().OfType<DispenseRadioMissionPacket>().Any(offer => offer.MissionId == Wilderness));

            // Onto Wilderness, as a map change puts a player there (MapChannelManager: AssignPlayer).
            Arrive(harness, WildernessMap);
            harness.Drain();
            ManifestationManager.Instance.AssignPlayer(harness.Client);

            var offered = harness.Drain().OfType<DispenseRadioMissionPacket>().Single();
            Assert.AreEqual(Wilderness, offered.MissionId);
            Assert.IsTrue(offered.ForceDialog);

            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Wilderness));

            var gained = harness.Drain().OfType<MissionGainedPacket>().Single();
            Assert.AreEqual(Wilderness, gained.MissionId);
            var log = harness.Client.Player.Missions[Wilderness];
            Assert.AreEqual(MissionState.Active, log.State);
            Assert.IsFalse(log.Completeable, "its one required objective is not done");
            Assert.AreEqual(0U, log.Objectives[KillXanx].Counters[0]);

            // Held: arriving again offers nothing.
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsEmpty(harness.Drain().OfType<DispenseRadioMissionPacket>().ToArray());
        }

        [TestMethod]
        public void FortyXanxOnWildernessMakeABugZapper()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Arrive(harness, WildernessMap);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Wilderness));
            harness.Drain();

            // A Thrax is another objective's.
            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(3, 20757, Thrax), Vector3.Zero));
            var thrax = harness.Drain().OfType<UpdateObjectiveCounterPacket>().Single();
            Assert.AreEqual(5U, thrax.ObjectiveId, "Kill 200 Thrax Soldiers!");
            Assert.AreEqual(200U, thrax.TargetValue);

            for (var kills = 1u; kills <= 39; kills++)
                Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, 4001, Xanx), Vector3.Zero));

            var packets = harness.Drain();
            var last = packets.OfType<UpdateObjectiveCounterPacket>().Last();
            Assert.AreEqual((Wilderness, KillXanx, 0u, 39u, 0u, 40u),
                (last.MissionId, last.ObjectiveId, last.CounterId, last.CounterValue, last.InitialValue, last.TargetValue));
            Assert.IsEmpty(packets.OfType<TitleAddedPacket>().ToArray());

            // Xanx of another place are not Wilderness's.
            Arrive(harness, BootcampRuntimeTestHarness.BootcampMapContextId);
            Assert.IsFalse(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, 4001, Xanx), Vector3.Zero));
            Assert.AreEqual(39U, harness.Client.Player.Missions[Wilderness].Objectives[KillXanx].Counters[0]);

            Arrive(harness, WildernessMap);
            harness.Drain();
            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, 4001, Xanx), Vector3.Zero));

            packets = harness.Drain();
            Assert.AreEqual(KillXanx, packets.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            Assert.AreEqual(BugZapper, packets.OfType<TitleAddedPacket>().Single().TitleId);
            Assert.IsTrue(harness.Client.Player.Titles.Contains(BugZapper));
            Assert.IsEmpty(packets.OfType<MissionCompleteablePacket>().ToArray(), "a title is not the mission");

            var log = harness.Client.Player.Missions[Wilderness];
            Assert.AreEqual(MissionObjectiveState.Completed, log.Objectives[KillXanx].State);
            Assert.AreEqual(MissionState.Active, log.State);
            Assert.IsFalse(log.Completeable);
        }

        [TestMethod]
        public void TheCountIsKeptOverALogout()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Arrive(harness, WildernessMap);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Wilderness));

            for (var kills = 0; kills < 3; kills++)
                Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, 4001, Xanx), Vector3.Zero));

            // The server restarted and the character back: the journal as the database has it.
            harness.ReconnectFresh();

            var log = harness.Client.Player.Missions[Wilderness];
            Assert.AreEqual(MissionState.Active, log.State);
            Assert.AreEqual(3U, log.Objectives[KillXanx].Counters[0]);
            Assert.AreEqual(0U, log.Objectives[5].Counters[0], "the Thrax count beside it");

            Arrive(harness, WildernessMap);
            harness.Drain();
            Assert.IsTrue(harness.Manager.Credit.RecordKill(harness.Client, Kill(900, 4001, Xanx), Vector3.Zero));
            Assert.AreEqual(4U, harness.Drain().OfType<UpdateObjectiveCounterPacket>().Single().CounterValue);
        }

        [TestMethod]
        public void AXanxKilledOnWildernessIsCountedByItsDeath()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            Arrive(harness, WildernessMap);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Wilderness));

            // A Xanx of the world: a creature row whose class carries the flag.
            var world = harness.WorldContext;
            var xanxClasses = world.Set<CreatureClassFlagEntry>().AsNoTracking().Where(row => row.FlagId == Xanx).Select(row => row.ClassId).ToList();
            var entry = world.Set<CreatureEntry>().AsNoTracking().Where(row => xanxClasses.Contains(row.ClassId)).OrderBy(row => row.Id).First();
            var classEntry = world.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == entry.ClassId);
            var entityClass = new EntityClass(classEntry.Id, classEntry.ClassName, classEntry.MeshId, classEntry.ClassCollisionRole,
                classEntry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), classEntry.TargetFlag != 0);
            entityClass.CreatureFlags.AddRange(world.Set<CreatureClassFlagEntry>().AsNoTracking()
                .Where(row => row.ClassId == entry.ClassId).Select(row => (CreatureFlag)row.FlagId));
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)entry.ClassId] = entityClass;
            CreatureManager.Instance.LoadedCreatures[entry.Id] = new Creature(entry) { AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };

            var map = harness.Client.Player.MapChannel;
            map.SpawnPools.Add(new SpawnPool
            {
                DbId = 990900,
                Position = harness.Client.Player.Position + new Vector3(3, 0, 0),
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Mode = SpawnPoolManager.ModeAutomatic,
                AnimType = 0,
                RespawnTime = 300_000,
                UpdateTimer = 300_000,
                SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(entry.Id, 1, 1) }
            });
            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);
            var xanx = map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Single(creature => creature.DbId == entry.Id);
            harness.Drain();

            xanx.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(map, xanx, harness.Client.Player);

            var counter = harness.Drain().OfType<UpdateObjectiveCounterPacket>().Single();
            Assert.AreEqual((Wilderness, KillXanx, 1u), (counter.MissionId, counter.ObjectiveId, counter.CounterValue));
        }

        [TestMethod]
        public void AMissionThatNamesWhatTheWorldHasNotGotIsLeftOutAndTheRestRun()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.WorldContext.Database.ExecuteSqlRaw(
                $"update mission_trigger set subject_id = 250 where mission_id = {Wilderness} and objective_id = {KillXanx}");

            var report = harness.Manager.LoadMissions();

            Assert.IsFalse(report.BlocksReadiness, "optional content");
            var diagnostic = report.Diagnostics.Single(entry => entry.MissionId == Wilderness);
            Assert.AreEqual("missing-creature-flag", diagnostic.Code);
            Assert.AreEqual(KillXanx, diagnostic.ObjectiveId);
            Assert.IsFalse(harness.Manager.LoadedMissions[Wilderness].IsOperational);
            Assert.IsTrue(harness.Manager.LoadedMissions[1582].IsOperational, "Divide's");
        }

        #endregion

        /// <summary>Mission 321, one objective: a counter to <paramref name="count"/> fed by the kill of any of the subjects.</summary>
        private static MissionTestContext Counting(MissionProgressEventKind kind, uint[] subjects, uint count,
            MissionCreditPolicy creditPolicy = null, Func<uint, MissionRequirement> here = null, uint? titleId = null)
        {
            var counters = new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, count) };
            var objective = new MissionObjectiveDefinition(
                1, 1001, 1002, new uint?[] { 9000, null, null }, 0, MissionObjectiveState.Incomplete, true,
                counters, new Dictionary<uint, MissionObjectiveItemCounterDefinition>(), Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                MissionProgressRule.IncrementCounterOnAnySubject(kind, subjects, 0, 0, count),
                creditPolicy: creditPolicy, titleId: titleId);
            Mission Build(MissionRequirement requirement) => new(
                321, "Mission 321", 321, 77, 88, 5, 1, 2, true, false, new[] { objective }, true,
                objectiveRequirements: requirement == null ? null : new Dictionary<uint, MissionRequirement> { [1] = requirement });

            var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [321] = Build(null) });

            if (here != null)
            {
                context.Dispose();
                var mapContextId = context.Map.MapInfo.MapContextId;
                context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [321] = Build(here(mapContextId)) });
            }

            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            return context;
        }

        /// <summary>A kill as CreatureManager.KillEvents names it: the creature row, its class, the flags of its class.</summary>
        private static MissionProgressEvent[] Kill(uint creatureId, uint classId, params uint[] flags) =>
            new[] { MissionProgressEvent.Creature(creatureId), MissionProgressEvent.CreatureClass(classId) }
                .Concat(flags.Select(MissionProgressEvent.CreatureFlag)).ToArray();

        private static MissionTriggerDefinition Trigger(uint triggerId, MissionProgressEventKind kind, uint subject, uint count) =>
            new(new MissionTriggerEntry
            {
                MissionId = 321, ContentRevision = "test", ObjectiveId = 1, TransitionId = 1, TriggerId = triggerId,
                Kind = MissionTriggerKind.ProgressEvent, Sequence = triggerId, EventKind = (byte)kind, SubjectId = subject,
                CounterId = 0, InitialValue = 0, TargetValue = count
            });

        /// <summary>The player onto another of the harness's maps, as a map change leaves them.</summary>
        private static void Arrive(BootcampRuntimeTestHarness.Harness harness, uint mapContextId)
        {
            var client = harness.Client;
            var destination = mapContextId == BootcampRuntimeTestHarness.BootcampMapContextId
                ? harness.BootcampMap
                : harness.Maps.FindByContextId(mapContextId);

            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
        }

        private static int IndexOf<T>(IReadOnlyList<Rasa.Packets.PythonPacket> packets)
        {
            for (var i = 0; i < packets.Count; i++)
                if (packets[i] is T)
                    return i;

            return -1;
        }
    }
}
