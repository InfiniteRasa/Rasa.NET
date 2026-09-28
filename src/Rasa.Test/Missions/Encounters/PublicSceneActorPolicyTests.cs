extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Encounters
{
    using Rasa.Data;
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Game.Missions.World;
    using Rasa.Game.Missions.Integration;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class PublicSceneActorPolicyTests
    {
        [TestMethod]
        public void PublicCreatedHostileWithRolePolicyGrantsNormalXpAndAuthoredLootOnce()
        {
            using var fixture = new Fixture();
            var actor = JsonSerializer.Deserialize<SceneActorDefinition>(
                @"{""role"":""enemy"",""kind"":""Creature"",""templateId"":510210,
                   ""position"":{""x"":0,""y"":0,""z"":0},
                   ""gameplayPolicy"":{""rewardScenarioKills"":true,
                     ""loot"":{""drops"":[{""templateId"":28,""chancePercent"":100,""minimum"":2,""maximum"":2}]}}}",
                MissionContentCodec.Options);
            var run = fixture.Context.Manager.Scenes.Start(fixture.Context.Client, "data.sequence", Bindings(actor));
            var enemy = fixture.Actor(run, "enemy");
            var player = fixture.Context.Client.Player;

            fixture.Kill(enemy);

            Assert.AreEqual(CharacterState.Dead, enemy.State);
            Assert.IsTrue(player.Experience is >= 90 and <= 110, "Use the existing level-one kill XP range.");
            Assert.AreEqual(player.EntityId, enemy.HarvestOwnerEntityId);
            Assert.AreNotEqual(0UL, enemy.CorpseLootEntityId);
            var loot = fixture.Context.Map.LootDispensers[enemy.CorpseLootEntityId];
            Assert.AreEqual(player.EntityId, loot.Owner);
            Assert.AreEqual(28U, loot.LootItems.Single().ItemTemplateId);
            Assert.AreEqual(2U, loot.LootItems.Single().ItemQuantity);
            var experience = player.Experience;
            var corpseLoot = enemy.CorpseLootEntityId;
            fixture.Kill(enemy);
            Assert.AreEqual(experience, player.Experience);
            Assert.AreEqual(corpseLoot, enemy.CorpseLootEntityId);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LeasedStaticRoleCanExplicitlyEnableOrSuppressNormalRewards(bool rewards)
        {
            using var fixture = new Fixture();
            var actor = fixture.PublicActor();
            fixture.BindLease(new ActorGameplayPolicy
            {
                RewardScenarioKills = rewards,
                Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 2, 2) })
            });
            Assert.IsTrue(fixture.Context.Manager.AcceptOfferedMission(fixture.Context.Client, actor.EntityId, 321));
            Assert.IsNull(actor.SpawnPool.ScenarioKey, "This must exercise a leased ordinary static spawn.");

            fixture.Kill(actor);

            Assert.AreEqual(rewards, fixture.Context.Client.Player.Experience > 0);
            Assert.AreEqual(rewards, actor.CorpseLootEntityId != 0);
            Assert.AreEqual(rewards ? fixture.Context.Client.Player.EntityId : 0UL, actor.HarvestOwnerEntityId);
            if (rewards)
                Assert.AreEqual(2U, fixture.Context.Map.LootDispensers[actor.CorpseLootEntityId]
                    .LootItems.Single().ItemQuantity);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DeathDiscardsRolePolicyButKeepsItsRolledLoot(bool leased)
        {
            using var fixture = new Fixture();
            var policy = new ActorGameplayPolicy
            {
                RewardScenarioKills = true, Tags = new[] { "this-life" },
                Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 2, 2) })
            };
            Creature actor;
            if (leased)
            {
                actor = fixture.PublicActor();
                fixture.BindLease(policy);
                Assert.IsTrue(fixture.Context.Manager.AcceptOfferedMission(fixture.Context.Client, actor.EntityId, 321));
            }
            else
            {
                var run = fixture.Context.Manager.Scenes.Start(fixture.Context.Client, "data.sequence",
                    Bindings(Enemy("enemy", policy)));
                actor = fixture.Actor(run, "enemy");
            }

            fixture.Kill(actor);

            Assert.AreEqual(0, CreatureGameplayRules.Policy(actor).Tags.Count, "A dead actor must lose its live role override.");
            Assert.AreEqual(2U, fixture.Context.Map.LootDispensers[actor.CorpseLootEntityId].LootItems.Single().ItemQuantity);
        }

        [TestMethod]
        public void WorldAttachmentInstallsTheAuthoredRolePolicyOnTheExactCommittedLease()
        {
            using var fixture = new Fixture();
            var actor = fixture.PublicActor();
            var policy = new ActorGameplayPolicy { Invulnerable = true };
            fixture.Context.Manager.Scenes.Bind(321, "data.sequence",
                Bindings(new SceneActorDefinition("guide", SceneActorKind.PublicSpawn, 77, GameplayPolicy: policy)));
            fixture.Context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "guide", "data.sequence"));

            Assert.IsTrue(fixture.Context.Manager.AcceptOfferedMission(fixture.Context.Client, actor.EntityId, 321));

            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));
            Assert.AreSame(actor, fixture.Context.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Distinct().Single(creature => creature.SpawnPool?.DbId == 77));
        }

        [TestMethod]
        public void ReattachingANewGenerationRemovesTheOldActorAndCannotRetainItsPolicy()
        {
            using var fixture = new Fixture();
            var bindings = Bindings(Enemy("enemy", new ActorGameplayPolicy { Invulnerable = true }));
            var scenes = fixture.Context.Manager.Scenes;
            var run = scenes.Start(fixture.Context.Client, "data.sequence", bindings);
            var old = fixture.Actor(run, "enemy");
            using (var unit = fixture.Context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var row = unit.CharacterMissions.Runtime.Scene(run);
                    row.Generation++;
                    row.Version++;
                    row.Checkpoint = "{}";
                });

            scenes.Attach(fixture.Context.Client, run, bindings);
            Assert.IsTrue(scenes.Submit(run, new SceneObservation(SceneEventKind.Started, 2)));

            Assert.IsFalse(MapInstanceScope.Contains(fixture.Context.Map, old));
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(old));
            var current = fixture.Actor(run, "enemy");
            Assert.AreNotSame(old, current);
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(current));
            Assert.IsFalse(scenes.Submit(run, new SceneObservation(SceneEventKind.Started, 1)));
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(current));
        }

        [TestMethod]
        public void LeaseResetFailurePreservesPolicyUntilCommitAndTheNextHolderGetsOnlyItsOwnPolicy()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var actor = fixture.PublicActor();
            fixture.BindLeases(
                (321, new ActorGameplayPolicy { Invulnerable = true, Tags = new[] { "first" } }, "Reset"),
                (322, new ActorGameplayPolicy { RewardScenarioKills = true, Tags = new[] { "second" } }, "Reset"));
            var other = context.CreateAdditionalClient(2);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            var old = context.Manager.PublicActors.Handle(context.Map, 77);
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionActorLeaseEntry>().Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected policy lease reset failure.");
            };

            Assert.ThrowsExactly<DbUpdateException>(() => context.Manager.PublicActors.BeginReset(context.Map, old.RunId, "Test"));
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));
            Assert.IsTrue(context.Manager.PublicActors.TryResolve(context.Map, old, out _));
            context.BeforeSave = null;
            Assert.IsTrue(context.Manager.PublicActors.BeginReset(context.Map, old.RunId, "Test"));
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(actor));
            Assert.AreEqual(0, CreatureGameplayRules.Policy(actor).Tags.Count);
            context.Manager.PublicActors.Tick(context.Map);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(other, actor.EntityId, 322));

            CollectionAssert.AreEqual(new[] { "second" }, CreatureGameplayRules.Policy(actor).Tags.ToArray());
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(actor));
            Assert.IsFalse(context.Manager.PublicActors.TryResolve(context.Map, old, out _));
            Assert.IsFalse(context.Manager.PublicActors.BeginReset(context.Map, old.RunId, "Stale"));
            CollectionAssert.AreEqual(new[] { "second" }, CreatureGameplayRules.Policy(actor).Tags.ToArray());
        }

        [TestMethod]
        public void SameTemplateRolesHaveIndependentPoliciesAndUnboundStaticRewardsStayNormal()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var run = context.Manager.Scenes.Start(context.Client, "data.sequence", Bindings(
                Enemy("protected", new ActorGameplayPolicy { Invulnerable = true, Tags = new[] { "protected" } }),
                Enemy("unrewarded", new ActorGameplayPolicy { RewardScenarioKills = false }),
                Enemy("default")));
            var protectedActor = fixture.Actor(run, "protected");
            var unrewarded = fixture.Actor(run, "unrewarded");
            var ordinaryScene = fixture.Actor(run, "default");
            var ordinaryStatic = fixture.StaticEnemy();

            Assert.AreEqual(0, ActorManager.Instance.Damage(context.Map, protectedActor, 1, context.Client.Player));
            Assert.AreEqual(1, ActorManager.Instance.Damage(context.Map, ordinaryScene, 1, context.Client.Player));
            Assert.AreEqual(0, CreatureGameplayRules.Policy(ordinaryStatic).Tags.Count);
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(fixture.Creatures.LoadedCreatures[510210]));
            fixture.Kill(unrewarded);
            fixture.Kill(ordinaryScene);
            Assert.AreEqual(0U, context.Client.Player.Experience);
            Assert.AreEqual(0UL, unrewarded.CorpseLootEntityId);
            Assert.AreEqual(0UL, ordinaryScene.CorpseLootEntityId);
            fixture.Kill(ordinaryStatic);
            Assert.IsTrue(context.Client.Player.Experience is >= 90 and <= 110);
            Assert.AreNotEqual(0UL, ordinaryStatic.CorpseLootEntityId);
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(protectedActor));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ExperiencePolicyAppliesOnlyOnItsActualPrivateMapAndRolePolicyTakesPrecedence(bool isPrivate)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            context.Map.IsPrivateInstance = isPrivate;
            context.Map.OwnerCharacterId = isPrivate ? context.Client.Player.Id : 0;
            context.Manager.ActorPolicies.Add(context.Map.MapInfo.MapContextId, 510210,
                new ActorGameplayPolicy { Invulnerable = true, RewardScenarioKills = true });
            var run = context.Manager.Scenes.Start(context.Client, "data.sequence", Bindings(
                Enemy("inherited"), Enemy("explicit", new ActorGameplayPolicy { RewardScenarioKills = false })));
            var inherited = fixture.Actor(run, "inherited");
            var explicitActor = fixture.Actor(run, "explicit");

            Assert.AreEqual(isPrivate, CreatureGameplayRules.IsInvulnerable(inherited));
            fixture.Kill(explicitActor);
            Assert.AreEqual(CharacterState.Dead, explicitActor.State);
            Assert.AreEqual(0U, context.Client.Player.Experience);
            Assert.AreEqual(0UL, explicitActor.CorpseLootEntityId);
        }

        [TestMethod]
        public void PrivateStaticRoleRemovalRestoresTheExperienceFallbackAndReconnectRestoresTheRole()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            context.Map.IsPrivateInstance = true;
            context.Map.OwnerCharacterId = 1;
            var actor = fixture.PublicActor();
            context.Manager.ActorPolicies.Add(context.Map.MapInfo.MapContextId, actor.DbId,
                new ActorGameplayPolicy { Tags = new[] { "experience" } });
            var definition = new SceneActorDefinition("guide", SceneActorKind.PublicSpawn, 77,
                GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true, Tags = new[] { "role" } });
            var original = Bindings(definition);
            var bindings = new SceneBindings(original.Release, original.Actors.ToDictionary(entry => entry.Key, entry => entry.Value),
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [0] = original.Sequences[0],
                    [1] = new(worldIntents: new[] { new RemoveActorIntent("release", "guide") })
                });
            var scenes = context.Manager.Scenes;
            var run = scenes.Start(context.Client, "data.sequence", bindings);
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));

            scenes.Detach(context.Client, context.Map);
            CollectionAssert.AreEqual(new[] { "experience" }, CreatureGameplayRules.Policy(actor).Tags.ToArray());
            scenes.Attach(context.Client, run, bindings);
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));
            Assert.IsTrue(scenes.Submit(run, new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));

            Assert.IsTrue(MapInstanceScope.Contains(context.Map, actor), "Releasing a private static role must not delete the spawn.");
            CollectionAssert.AreEqual(new[] { "experience" }, CreatureGameplayRules.Policy(actor).Tags.ToArray());
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(actor));
        }

        [TestMethod]
        [DataRow("Wait")]
        [DataRow("Continue")]
        public void PublicReconnectRetainsTheSameActorsAndTheirImmutableRolePolicies(string ownerLoss)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var actor = fixture.PublicActor();
            var tags = new List<string> { "bound" };
            var policy = new ActorGameplayPolicy { Invulnerable = true, Tags = tags };
            fixture.BindLease(policy, ownerLoss: ownerLoss);
            var bindings = Bindings(
                new SceneActorDefinition("guide", SceneActorKind.PublicSpawn, 77, GameplayPolicy: policy),
                Enemy("enemy", new ActorGameplayPolicy { Tags = tags }));
            context.Manager.Scenes.ClearBindings();
            context.Manager.Scenes.Bind(321, "data.sequence", bindings);
            tags[0] = "caller-mutation-before-acceptance";
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            context.Manager.PublishInitialState(context.Client);
            var handle = context.Manager.PublicActors.Handle(context.Map, 77);
            var enemy = fixture.Actor(handle.RunId, "enemy");
            context.Client.State = ClientState.Disconnected;
            fixture.Maps().CleanupDisconnected(context.Client);
            tags.Clear();

            var reconnected = context.CreateCompetingClient();
            context.Manager.PublishInitialState(reconnected);
            context.Manager.Scenes.Tick(context.Map);

            Assert.IsTrue(context.Manager.PublicActors.TryResolve(context.Map, handle, out var current));
            Assert.AreSame(actor, current);
            Assert.AreSame(enemy, fixture.Actor(handle.RunId, "enemy"));
            CollectionAssert.AreEqual(new[] { "bound" }, CreatureGameplayRules.Policy(actor).Tags.ToArray());
            CollectionAssert.AreEqual(new[] { "bound" }, CreatureGameplayRules.Policy(enemy).Tags.ToArray());
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));
        }

        [TestMethod]
        public void FailedLeaseAcceptanceCannotPublishAnyRoleOverride()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var actor = fixture.PublicActor();
            fixture.BindLease(new ActorGameplayPolicy { Invulnerable = true });
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionActorLeaseEntry>().Any(entry => entry.State == EntityState.Added))
                {
                    Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(actor));
                    throw new DbUpdateException("Injected lease policy acceptance failure.");
                }
            };

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(actor));
            Assert.IsTrue(actor.IsInteractable);
            context.BeforeSave = null;
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));
        }

        [TestMethod]
        [DataRow("generation")]
        [DataRow("role")]
        [DataRow("run")]
        [DataRow("runtime-map")]
        [DataRow("spawn-map")]
        [DataRow("template")]
        [DataRow("spawn-reference")]
        public void ChangedActorOwnershipDiscardsTheOldRoleOverride(string change)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var run = context.Manager.Scenes.Start(context.Client, "data.sequence",
                Bindings(Enemy("enemy", new ActorGameplayPolicy { Invulnerable = true })));
            var actor = fixture.Actor(run, "enemy");
            var otherMap = new MapChannel { MapInfo = context.Map.MapInfo, ClientList = new() };
            switch (change)
            {
                case "generation": actor.SpawnPool.SceneGeneration++; break;
                case "role": actor.SpawnPool.SceneActorRole = "other-role"; break;
                case "run": actor.SpawnPool.SceneRunId = "other-run"; break;
                case "runtime-map": actor.RuntimeMapChannel = otherMap; break;
                case "spawn-map": actor.SpawnPool.RuntimeMapChannel = otherMap; break;
                case "template": actor.DbId = 510216; break;
                case "spawn-reference": actor.SpawnPool = new SpawnPool { RuntimeMapChannel = context.Map }; break;
            }

            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(actor));
        }

        [TestMethod]
        public void WrongEpochOrMapCannotAttachAPolicyToAValidLease()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var actor = fixture.PublicActor();
            fixture.BindLease(new ActorGameplayPolicy { Tags = new[] { "current" } });
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            var handle = context.Manager.PublicActors.Handle(context.Map, 77);
            var foreignMap = new MapChannel { MapInfo = context.Map.MapInfo, ClientList = new() };

            Assert.IsFalse(context.Manager.PublicActors.AttachPolicy(context.Map,
                handle with { MapEpoch = Guid.NewGuid() }, new ActorGameplayPolicy { Invulnerable = true }));
            Assert.IsFalse(context.Manager.PublicActors.AttachPolicy(foreignMap,
                handle, new ActorGameplayPolicy { Invulnerable = true }));
            Assert.IsFalse(context.Manager.PublicActors.AttachPolicy(context.Map,
                handle with { Generation = handle.Generation + 1 }, new ActorGameplayPolicy { Invulnerable = true }));
            CollectionAssert.AreEqual(new[] { "current" }, CreatureGameplayRules.Policy(actor).Tags.ToArray());
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(actor));
        }

        [TestMethod]
        public void PrivateSharedRolePolicySurvivesDetachingOneOfItsSceneReferences()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            context.Map.IsPrivateInstance = true;
            context.Map.OwnerCharacterId = 1;
            var world = new SceneWorldAdapter(context.Manager.PublicActors, (_, _) => { });
            var definition = Enemy("enemy", new ActorGameplayPolicy { Invulnerable = true }) with { SharedKey = "shared" };
            var bindings = Bindings(definition);
            var first = new SceneRun("first", "unversioned", "data.sequence", 1, 1, 0, "{}", SceneStatus.Running, 1);
            var second = first with { Id = "second" };
            world.Attach(first, bindings, context.Client, context.Map);
            world.Apply(first, new EnsureActorIntent("first", "enemy"));
            world.Attach(second, bindings, context.Client, context.Map);
            world.Apply(second, new EnsureActorIntent("second", "enemy"));
            var actor = fixture.Actor(first.Id, "enemy");

            world.Detach(second.Id);

            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));
            world.Detach(first.Id);
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(actor));
        }

        [TestMethod]
        public void PublicHostileRewardsReuseGroupMissionCreditWithoutSharingXpOrLoot()
        {
            using var fixture = new Fixture(groupCredit: true);
            var context = fixture.Context;
            var member = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            foreach (var client in new[] { context.Client, member })
                Assert.IsTrue(context.Manager.AcceptOfferedMission(client, giver.EntityId, 321));
            using var party = new GroupMissionCreditTests.PartyScope(context.Client, member);
            var run = context.Manager.Scenes.Start(context.Client, "data.sequence",
                Bindings(Enemy("enemy", new ActorGameplayPolicy { RewardScenarioKills = true })), 321);
            var enemy = fixture.Actor(run, "enemy");

            fixture.Kill(enemy);
            context.Manager.Credit.Tick(context.Map);

            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Completed, member.Player.Missions[321].Objectives[1].State);
            Assert.IsTrue(context.Client.Player.Experience > 0);
            Assert.AreEqual(0U, member.Player.Experience);
            Assert.AreEqual(context.Client.Player.EntityId, context.Map.LootDispensers[enemy.CorpseLootEntityId].Owner);
        }

        [TestMethod]
        public void LeasedStaticWithoutRolePolicyRetainsNormalRewards()
        {
            using var fixture = new Fixture();
            var actor = fixture.PublicActor();
            fixture.BindLease(null);
            Assert.IsTrue(fixture.Context.Manager.AcceptOfferedMission(fixture.Context.Client, actor.EntityId, 321));

            fixture.Kill(actor);

            Assert.IsTrue(fixture.Context.Client.Player.Experience is >= 90 and <= 110);
            Assert.AreNotEqual(0UL, actor.CorpseLootEntityId);
        }

        [TestMethod]
        public void UnboundPrivateStaticSpawnRetainsLegacyRewardsWithANonrewardingExperiencePolicy()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            context.Map.IsPrivateInstance = true;
            context.Map.OwnerCharacterId = 1;
            context.Manager.ActorPolicies.Add(context.Map.MapInfo.MapContextId, 510210, new ActorGameplayPolicy
            {
                Tags = new[] { "experience" }, RewardScenarioKills = false,
                Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 2, 2) })
            });
            var actor = fixture.StaticEnemy();

            fixture.Kill(actor);

            Assert.IsTrue(context.Client.Player.Experience is >= 90 and <= 110,
                "Without a role override, the legacy experience reward flag gates scene-created actors only.");
            Assert.AreEqual(2U, context.Map.LootDispensers[actor.CorpseLootEntityId].LootItems.Single().ItemQuantity);
        }

        [TestMethod]
        public void PrivateExperienceEnablesSceneRewardsOnlyWithoutAnExplicitRoleOverride()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            context.Map.IsPrivateInstance = true;
            context.Map.OwnerCharacterId = 1;
            context.Manager.ActorPolicies.Add(context.Map.MapInfo.MapContextId, 510210, new ActorGameplayPolicy
            {
                RewardScenarioKills = true, Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 2, 2) })
            });
            var run = context.Manager.Scenes.Start(context.Client, "data.sequence",
                Bindings(Enemy("inherited"), Enemy("explicit", new ActorGameplayPolicy())));

            fixture.Kill(fixture.Actor(run, "explicit"));
            Assert.AreEqual(0U, context.Client.Player.Experience);
            var inherited = fixture.Actor(run, "inherited");
            fixture.Kill(inherited);

            Assert.IsTrue(context.Client.Player.Experience is >= 90 and <= 110);
            Assert.AreEqual(2U, context.Map.LootDispensers[inherited.CorpseLootEntityId].LootItems.Single().ItemQuantity);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PublicDefenderFinalBlowRewardsOnlyCurrentParticipation(bool participating)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            fixture.Creatures.LoadedCreatures[510207] = new Creature(fixture.Creatures.LoadedCreatures[510210])
                { DbId = 510207, Faction = Factions.AFS };
            var run = context.Manager.Scenes.Start(context.Client, "data.sequence", Bindings(
                Enemy("defender", new ActorGameplayPolicy { DefenseRadius = 10, DefenseTargetTag = "target" })
                    with { TemplateId = 510207 },
                Enemy("enemy", new ActorGameplayPolicy
                {
                    Tags = new[] { "target" }, TrackParticipation = true, RewardScenarioKills = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 2, 2) })
                })));
            var defender = fixture.Actor(run, "defender");
            var enemy = fixture.Actor(run, "enemy");
            if (participating)
                Assert.AreEqual(1, ActorManager.Instance.Damage(context.Map, enemy, 1, context.Client.Player));

            fixture.Creatures.HandleCreatureKill(context.Map, enemy, defender);

            Assert.AreEqual(participating, context.Client.Player.Experience > 0);
            Assert.AreEqual(participating, enemy.CorpseLootEntityId != 0);
            Assert.AreEqual(0, CreatureGameplayRules.Policy(enemy).Tags.Count);
            if (participating)
                Assert.AreEqual(2U, context.Map.LootDispensers[enemy.CorpseLootEntityId].LootItems.Single().ItemQuantity);
        }

        [TestMethod]
        public void RemovalAndReensureCreateANewActorWithoutLeakingPolicyToItsTemplate()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var actor = Enemy("enemy", new ActorGameplayPolicy { Invulnerable = true });
            var bindings = new SceneBindings("unversioned", new Dictionary<string, SceneActorDefinition> { ["enemy"] = actor },
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [0] = new(worldIntents: new[] { new EnsureActorIntent("initial", "enemy") }),
                    [1] = new(worldIntents: new[] { new RemoveActorIntent("remove", "enemy") }),
                    [2] = new(worldIntents: new[] { new EnsureActorIntent("replace", "enemy") })
                });
            var run = context.Manager.Scenes.Start(context.Client, "data.sequence", bindings);
            var old = fixture.Actor(run, "enemy");

            Assert.IsTrue(context.Manager.Scenes.Submit(run, new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
            Assert.IsFalse(MapInstanceScope.Contains(context.Map, old));
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(old));
            Assert.IsTrue(context.Manager.Scenes.Submit(run, new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 2)));

            var replacement = fixture.Actor(run, "enemy");
            Assert.AreNotSame(old, replacement);
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(replacement));
            Assert.IsFalse(CreatureGameplayRules.IsInvulnerable(fixture.StaticEnemy()));
        }

        [TestMethod]
        public void PublicDeathReplacementAndResetDoNotTransferThePreviousLeasePolicy()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var old = fixture.PublicActor();
            var pool = old.SpawnPool;
            fixture.Creatures.LoadedCreatures[77] = new Creature(old);
            fixture.BindLeases(
                (321, new ActorGameplayPolicy { Tags = new[] { "first" }, TrackParticipation = true }, "Reset"),
                (322, new ActorGameplayPolicy { Tags = new[] { "second" }, Invulnerable = true }, "Reset"));
            var nextOwner = context.CreateAdditionalClient(2);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, old.EntityId, 321));
            var oldHandle = context.Manager.PublicActors.Handle(context.Map, 77);
            fixture.Kill(old);
            Assert.AreEqual(0U, context.Client.Player.Experience);
            Assert.IsTrue(context.Manager.PublicActors.BeginReset(context.Map, oldHandle.RunId, "ActorDied"));
            Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(context.Map, old));
            var replacement = fixture.Creatures.CreateScenarioCreature(pool, 77, pool.Position, pool.Rotation);
            CellManager.Instance.AddToWorld(context.Map, replacement);

            Assert.AreEqual(0, CreatureGameplayRules.Policy(replacement).Tags.Count);
            Assert.IsFalse(CreatureGameplayRules.TracksParticipation(replacement));
            context.Manager.PublicActors.Tick(context.Map);
            Assert.IsTrue(replacement.IsInteractable);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(nextOwner, replacement.EntityId, 322));

            Assert.IsFalse(context.Manager.PublicActors.AttachPolicy(context.Map, oldHandle,
                new ActorGameplayPolicy { TrackParticipation = true }));
            Assert.IsFalse(CreatureGameplayRules.TracksParticipation(replacement));
            CollectionAssert.AreEqual(new[] { "second" }, CreatureGameplayRules.Policy(replacement).Tags.ToArray());
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(replacement));
            Assert.AreEqual(0, CreatureGameplayRules.Policy(old).Tags.Count);
        }

        [TestMethod]
        public void StaleWorldAttachmentCannotRemoveTheCurrentGenerationActor()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var world = new SceneWorldAdapter(context.Manager.PublicActors, (_, _) => { });
            var bindings = Bindings(Enemy("enemy", new ActorGameplayPolicy { Invulnerable = true }));
            var current = new SceneRun("current", "unversioned", "data.sequence", 1, 2, 0, "{}", SceneStatus.Running, 1);
            world.Attach(current, bindings, context.Client, context.Map);
            Assert.AreEqual(WorldEffectState.Applied, world.Apply(current, new EnsureActorIntent("ensure", "enemy")).State);
            var actor = fixture.Actor(current.Id, "enemy");

            Assert.ThrowsExactly<GameplayRejectionException>(() =>
                world.Attach(current with { Generation = 1 }, bindings, context.Client, context.Map));

            Assert.IsTrue(MapInstanceScope.Contains(context.Map, actor));
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));
        }

        [TestMethod]
        public void NewRolePolicyCannotInheritParticipationFromThePriorExperiencePolicy()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            context.Map.IsPrivateInstance = true;
            context.Map.OwnerCharacterId = 1;
            var actor = fixture.PublicActor();
            actor.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            context.Manager.ActorPolicies.Add(context.Map.MapInfo.MapContextId, actor.DbId,
                new ActorGameplayPolicy { TrackParticipation = true });
            CreatureManager.RecordCombatDamage(context.Map, actor, context.Client.Player, 1);
            fixture.Creatures.LoadedCreatures[510207] = new Creature(fixture.Creatures.LoadedCreatures[510210])
                { DbId = 510207, Faction = Factions.AFS };
            var bindings = Bindings(
                new SceneActorDefinition("guide", SceneActorKind.PublicSpawn, 77,
                    GameplayPolicy: new ActorGameplayPolicy { TrackParticipation = true, RewardScenarioKills = true }),
                Enemy("defender", new ActorGameplayPolicy { DefenseRadius = 10, DefenseTargetTag = "target" })
                    with { TemplateId = 510207 });
            var run = context.Manager.Scenes.Start(context.Client, "data.sequence", bindings);

            fixture.Creatures.HandleCreatureKill(context.Map, actor, fixture.Actor(run, "defender"));

            Assert.AreEqual(0U, context.Client.Player.Experience);
            Assert.AreEqual(0UL, actor.CorpseLootEntityId);
        }

        [TestMethod]
        public void PublicResetClearsTheRoleWithoutDiscardingAlreadyEarnedCorpseLoot()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var giver = fixture.PublicActor();
            fixture.BindLease(null);
            context.Manager.Scenes.ClearBindings();
            context.Manager.Scenes.Bind(321, "data.sequence", Bindings(
                new SceneActorDefinition("guide", SceneActorKind.PublicSpawn, 77),
                Enemy("enemy", new ActorGameplayPolicy
                {
                    RewardScenarioKills = true,
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 2, 2) })
                })));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            var handle = context.Manager.PublicActors.Handle(context.Map, 77);
            var enemy = fixture.Actor(handle.RunId, "enemy");
            fixture.Kill(enemy);
            var lootId = enemy.CorpseLootEntityId;
            Assert.AreNotEqual(0UL, lootId);

            Assert.IsTrue(context.Manager.PublicActors.BeginReset(context.Map, handle.RunId, "Completed"));
            context.Manager.PublicActors.Tick(context.Map);

            Assert.IsTrue(MapInstanceScope.Contains(context.Map, enemy), "Earned loot keeps the normal corpse lifetime.");
            Assert.AreEqual(2U, context.Map.LootDispensers[lootId].LootItems.Single().ItemQuantity);
            Assert.IsFalse(LootDispenserManager.Instance.AdvanceCorpseLifetime(context.Map, enemy, 1001));
            Assert.AreEqual(0, CreatureGameplayRules.Policy(enemy).Tags.Count);
            Assert.IsFalse(context.Map.SpawnPools.Any(pool => pool.SceneRunId == handle.RunId));
            Assert.IsTrue(giver.IsInteractable);
        }

        private static SceneActorDefinition Enemy(string role, ActorGameplayPolicy policy = null) =>
            new(role, SceneActorKind.Creature, 510210, new ScenePosition(0, 0, 0), GameplayPolicy: policy);

        private static SceneBindings Bindings(params SceneActorDefinition[] actors) =>
            new("unversioned", actors.ToDictionary(actor => actor.Role),
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [0] = new(worldIntents: actors.Select(actor =>
                        new EnsureActorIntent($"ensure-{actor.Role}", actor.Role)))
                });

        internal sealed class Fixture : IDisposable
        {
            private readonly List<(FieldInfo Field, object Previous)> _singletons = new();
            private readonly EntityClass _previousClass;
            internal MissionTestContext Context { get; }
            internal CreatureManager Creatures { get; }

            internal Fixture(bool groupCredit = false, IReadOnlyDictionary<uint, Mission> definitions = null)
            {
                Context = definitions != null
                    ? MissionTestContext.WithCustomDefinitions(definitions)
                    : groupCredit
                    ? MissionTestContext.WithProgressMission(
                        MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 510210),
                        creditPolicy: new MissionCreditPolicy(MissionCreditMode.NearbyParty, 20))
                    : MissionTestContext.WithDefinitions(321, 322);
                BootcampRuntimeTestHarness.PrepareDirectDamageClient(Context.Client);
                SetSingleton(typeof(ItemManager), Activator.CreateInstance(typeof(ItemManager),
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { Context }, null));
                Context.AddRewardTemplate(28, 3147);
                _previousClass = EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale];
                EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale] =
                    new EntityClass((uint)EntityClasses.HumanBaseMale, "fixture", 0, 0,
                        new() { AugmentationType.Creature }, true);
                Creatures = new CreatureManager(Context, new ManifestationManager(Context), Context.Manager);
                Creatures.LoadedCreatures[510210] = new Creature
                {
                    DbId = 510210, EntityClass = EntityClasses.HumanBaseMale, Faction = Factions.Bane,
                    Level = 1, AppearanceData = new(), State = CharacterState.Idle
                };
                SetSingleton(typeof(MissionApplication), Context.Manager);
                SetSingleton(typeof(CreatureManager), Creatures);
                SetSingleton(typeof(LootDispenserManager), new LootDispenserManager(Context,
                    missionManager: Context.Manager, lootRoll: (minimum, maximum) => minimum));
            }

            internal Creature Actor(string run, string role) => Context.Map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct().Single(creature =>
                    creature.SpawnPool?.SceneRunId == run && creature.SpawnPool.SceneActorRole == role);

            internal void Kill(Creature creature) =>
                Creatures.HandleCreatureKill(Context.Map, creature, Context.Client.Player);

            internal Creature StaticEnemy()
            {
                var pool = new SpawnPool
                {
                    DbId = 78, RuntimeMapChannel = Context.Map, MapContextId = Context.Map.MapInfo.MapContextId,
                    SpawnSlot = new() { new(510210, 1, 1) }
                };
                Context.Map.SpawnPools.Add(pool);
                var creature = Creatures.CreateScenarioCreature(pool, 510210, pool.Position, pool.Rotation);
                CellManager.Instance.AddToWorld(Context.Map, creature);
                return creature;
            }

            internal MapChannelManager Maps()
            {
                var maps = new MapChannelManager(Context, scenarioService: Context.Manager.ScenarioService,
                    updateCharacter: (_, _, _) => { }, refreshStats: (_, _) => { },
                    assignPlayer: client => Context.Manager.PublishInitialState(client), enterMapChannels: _ => { });
                maps.MapChannelArray.Add(Context.Map.MapInfo.MapContextId, Context.Map);
                return maps;
            }

            internal Creature PublicActor()
            {
                var actor = Context.AddNpc(77);
                actor.Faction = Factions.Bane;
                actor.Level = 1;
                actor.SpawnPool = new SpawnPool
                {
                    DbId = 77, RuntimeMapChannel = Context.Map, MapContextId = Context.Map.MapInfo.MapContextId,
                    Position = actor.Position, AliveCreatures = 1
                };
                Context.Map.SpawnPools.Add(actor.SpawnPool);
                return actor;
            }

            internal void BindLease(ActorGameplayPolicy policy, uint missionId = 321, string ownerLoss = "Reset")
                => BindLeases((missionId, policy, ownerLoss));

            internal void BindLeases(params (uint MissionId, ActorGameplayPolicy Policy, string OwnerLoss)[] leases)
            {
                var catalog = new MissionContentCatalog(Context, Context.Manager.LoadedMissions, null);
                foreach (var lease in leases)
                    catalog.SceneBindings.Add(lease.MissionId, new MissionSceneDefinition
                    {
                        Script = "data.sequence",
                        Actors = new() { ["guide"] = new("guide", SceneActorKind.PublicSpawn, 77, GameplayPolicy: lease.Policy) },
                        PublicEncounter = new PublicEncounterBinding(lease.MissionId, 77, "guide", "data.sequence", lease.OwnerLoss)
                    });
                MissionRuntimeComposition.Bind(catalog, Context.Manager.Scenes,
                    Context.Manager.PublicActors, Context.Manager.ActorPolicies);
            }

            private void SetSingleton(Type type, object value)
            {
                var field = type.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
                _singletons.Add((field, field.GetValue(null)));
                field.SetValue(null, value);
            }

            public void Dispose()
            {
                foreach (var creature in Context.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Distinct().Where(creature => creature.SpawnPool != null).ToArray())
                    CellManager.Instance.RemoveCreatureFromWorld(Context.Map, creature);
                EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale] = _previousClass;
                Context.Dispose();
                foreach (var (field, previous) in _singletons.AsEnumerable().Reverse())
                    field.SetValue(null, previous);
            }
        }
    }
}
