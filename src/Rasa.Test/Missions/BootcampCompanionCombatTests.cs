extern alias RasaGame;

using System;
using System.Linq;
using System.Numerics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class BootcampCompanionCombatTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void EscortCatchesMovingSprintingOwnerOnNavmeshWithTruthfulBoundedSpeed(bool sprint)
        {
            using var harness = CreateEscorts();
            harness.Client.Player.MovementSpeed = 1;
            if (sprint)
            {
                var level = new ActionLevelInfo { ActionId = ActionId.AaRecruitSprint, Level = 5 };
                foreach (var property in harness.WorldContext.ActionPropertyEntries
                    .Where(row => row.ActionId == (uint)ActionId.AaRecruitSprint && row.Level == 5))
                    level.Properties[(AbilityProperty)property.PropertyId] = property.Value;
                GameEffectManager.Instance.AttachSprint(harness.BootcampMap, harness.Client.Player, level);
            }
            var ownerSpeed = (float)(6.5 * harness.Client.Player.MovementSpeed);
            Assert.AreEqual(sprint ? 10.4f : 6.5f, ownerSpeed, 0.001f);
            var escort = Escorts(harness).First();
            var start = Ground(harness, new Vector3(380, 120, 158));
            Place(harness, escort, start);
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(1994, 439, out var exit));
            var route = harness.BootcampMap.NavMesh.FindPath(start, exit.Position, out var complete);
            Assert.IsTrue(complete);
            var owner = start;
            var corner = 0;
            WalkRoute(route, ref corner, ref owner, 28);
            harness.MovePlayerTo(owner);
            CellManager.Instance.UpdateVisibility(harness.Client);
            WorldTestContext.Drain(harness.Client);
            var initialGap = Vector3.Distance(escort.Position, harness.Client.Player.Position);
            var greatestSpeed = 0f;

            for (var tick = 0; tick < 16; tick++)
            {
                WalkRoute(route, ref corner, ref owner, ownerSpeed * 0.25f);
                harness.MovePlayerTo(owner);
                CellManager.Instance.UpdateVisibility(harness.Client);
                var previous = escort.Position;
                Advance(harness, 1);
                var moved = Vector3.Distance(previous, escort.Position);
                Assert.IsTrue(moved <= 3.6f, $"Escort jumped {moved}m in one 250ms tick.");
                AssertGrounded(harness, escort);
                var movements = WorldTestContext.Drain(harness.Client)
                    .Select(packet => packet.Message).OfType<MoveObjectMessage>()
                    .Where(packet => packet.EntityId == escort.EntityId).ToArray();
                foreach (var movement in movements)
                {
                    greatestSpeed = Math.Max(greatestSpeed, movement.Movement.Velocity);
                    Assert.IsTrue(movement.Movement.Velocity <= 14.3f);
                }
            }

            Assert.IsTrue(greatestSpeed > 10.4f, "Catch-up must exceed ordinary player Sprint.");
            Assert.IsTrue(Vector3.Distance(escort.Position, harness.Client.Player.Position) < initialGap - 4,
                "A moving owner must be caught, not merely reached after standing still.");
        }

        [TestMethod]
        public void OwnerAttackNotSelectionDrivesRealEscortDamageRetargetAndFollow()
        {
            using var harness = CreateEscorts();
            // Selection must not issue an attack order independently of the ambient aggro scan.
            foreach (var companion in Escorts(harness))
                companion.AggroRange = 0;
            var escort = Escorts(harness).Single(actor => actor.DbId == 510215);
            var position = Ground(harness, new Vector3(380, 120, 158));
            Place(harness, escort, position);
            harness.MovePlayerTo(position);
            var enemies = Actors(harness).Where(actor => actor.DbId == 510216).Take(2).ToArray();
            Assert.AreEqual(2, enemies.Length);
            for (var i = 0; i < enemies.Length; i++)
            {
                Place(harness, enemies[i], position + new Vector3(4 + i * 3, 0, 2));
                enemies[i].Actions.Clear();
                enemies[i].WalkSpeed = enemies[i].RunSpeed = 0;
            }
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Client.Player.Target = enemies[0].EntityId;
            Advance(harness, 16);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, escort.Controller.CurrentAction,
                "Selection alone must not order a mission escort to attack.");

            foreach (var companion in Escorts(harness))
                companion.AggroRange = 18;
            Shoot(harness, harness.Client.Player, enemies[0], 1);
            var before = Durability(enemies[0]);
            Advance(harness, 12);
            Assert.AreEqual(enemies[0].EntityId, escort.Controller.ActionFighting.TargetEntityId);
            Assert.IsTrue(Durability(enemies[0]) < before, "Escort AI must launch damaging attacks itself.");

            Shoot(harness, harness.Client.Player, enemies[1], 1);
            Advance(harness, 1);
            Assert.AreEqual(enemies[1].EntityId, escort.Controller.ActionFighting.TargetEntityId);
            CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, enemies[1]);
            Advance(harness, 1);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, escort.Controller.CurrentAction);

            Shoot(harness, harness.Client.Player, enemies[0], 1);
            Advance(harness, 1);
            harness.MovePlayerTo(position + new Vector3(0, 0, -30));
            Advance(harness, 1);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, escort.Controller.CurrentAction,
                "Leaving the encounter must recall the escort rather than pinning it in combat.");
            for (var tick = 0; tick < 6; tick++)
            {
                Advance(harness, 1);
                Assert.AreEqual(BehaviorManager.BehaviorActionFollow, escort.Controller.CurrentAction,
                    "Ambient aggro must not interrupt the escort's return to its owner.");
            }
        }

        [TestMethod]
        public void EscortAiKillingBossGrantsNormalRewardsAndObjectiveExactlyOnce()
        {
            using var harness = CreateEscorts();
            var boss = ReachBoss(harness);
            var escort = Escorts(harness).Single(actor => actor.DbId == 510215);
            Place(harness, escort, boss.Position + new Vector3(0, 0, 6));
            harness.MovePlayerTo(escort.Position);
            CellManager.Instance.UpdateVisibility(harness.Client);
            boss.Attributes[Attributes.Armor].Current = 0;
            boss.Attributes[Attributes.Health].Current = 10;
            var xp = harness.Client.Player.Experience;
            Shoot(harness, harness.Client.Player, boss, 1);
            for (var tick = 0; tick < 40 && boss.State != CharacterState.Dead; tick++)
                Advance(harness, 1);

            Assert.AreEqual(CharacterState.Dead, boss.State);
            Assert.IsTrue(harness.Client.Player.Experience > xp, "An owned escort's final blow must award normal XP.");
            Assert.AreNotEqual(0UL, boss.CorpseLootEntityId);
            Assert.AreEqual(harness.Client.Player.EntityId, boss.HarvestOwnerEntityId);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1994].Objectives[1].State);
            AssertCorpseMenuOpensAtFiveMetres(harness, boss);
            var awardedXp = harness.Client.Player.Experience;
            var loot = boss.CorpseLootEntityId;
            CreatureManager.Instance.HandleCreatureKill(harness.BootcampMap, boss, escort);
            Shoot(harness, harness.Client.Player, boss, 10000);
            Assert.AreEqual(awardedXp, harness.Client.Player.Experience);
            Assert.AreEqual(loot, boss.CorpseLootEntityId);
            Advance(harness, 1);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, escort.Controller.CurrentAction);
        }

        [TestMethod]
        public void YoungbloodIgnoresMissileAndDirectDamageButRemainsConversable()
        {
            using var harness = CreateEscorts();
            var youngblood = ReachYoungblood(harness);
            var health = Durability(youngblood);
            Shoot(harness, harness.Client.Player, youngblood, 100000);
            Assert.AreEqual(health, Durability(youngblood));
            Assert.AreEqual(0, ActorManager.Instance.Damage(
                harness.BootcampMap, youngblood, 100000, harness.Client.Player));
            Assert.AreNotEqual(CharacterState.Dead, youngblood.State);
            Assert.IsTrue(youngblood.IsInteractable);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, youngblood.EntityId, 1994, 3, 1));
        }

        [TestMethod]
        [DataRow("player", true)]
        [DataRow("escort", true)]
        [DataRow("ambient", false)]
        [DataRow("logout", false)]
        [DataRow("foreign-owner", false)]
        public void YoungbloodFinalBlowRewardsOnlyCurrentParticipatingOwner(string participation, bool earnsReward)
        {
            using var harness = CreateEscorts();
            var youngblood = ReachYoungblood(harness);
            var enemy = Actors(harness).First(actor => actor.DbId == 510216);
            Place(harness, enemy, youngblood.Position + new Vector3(0, 0, 8));
            var escort = Escorts(harness).First();
            Place(harness, escort, youngblood.Position + new Vector3(0, 0, 3));
            if (participation == "foreign-owner")
                escort.SpawnPool.FollowOwnerCharacterId = 999;
            if (participation is "player" or "logout")
                ActorManager.Instance.Damage(harness.BootcampMap, enemy, 1, harness.Client.Player);
            if (participation is "escort" or "foreign-owner")
                Shoot(harness, escort, enemy, 1);
            var xp = harness.Client.Player.Experience;
            if (participation == "logout")
                harness.Client.State = RasaGame::Rasa.Data.ClientState.Disconnected;

            Shoot(harness, youngblood, enemy, 100000);

            Assert.AreEqual(CharacterState.Dead, enemy.State);
            Assert.AreEqual(earnsReward, harness.Client.Player.Experience > xp);
            Assert.AreEqual(earnsReward, enemy.CorpseLootEntityId != 0);
            Assert.AreEqual(earnsReward ? harness.Client.Player.EntityId : 0UL, enemy.HarvestOwnerEntityId);
            if (earnsReward)
                AssertCorpseMenuOpensAtFiveMetres(harness, enemy);
            var awardedXp = harness.Client.Player.Experience;
            CreatureManager.Instance.HandleCreatureKill(harness.BootcampMap, enemy, youngblood);
            Assert.AreEqual(awardedXp, harness.Client.Player.Experience);
        }

        [TestMethod]
        public void YoungbloodDefendsAgainstNearbyThraxAndReturnsToGroundedBase()
        {
            using var harness = CreateEscorts();
            var youngblood = ReachYoungblood(harness);
            AssertBaseDefense(harness, youngblood);
        }

        [TestMethod]
        public void YoungbloodRuntimePolicyUsesTheApprovedAfsAttackAndMovementTemplate()
        {
            using var harness = CreateEscorts();
            var youngblood = ReachYoungblood(harness);
            // Isolate the runtime policy from the independently authored Youngblood migration.
            var action = Actors(harness).First(actor => actor.DbId == 510217).Actions.Single(row => row.Id == 2);
            youngblood.Actions.Clear();
            youngblood.Actions.Add(new CreatureAction(action));
            youngblood.RunSpeed = 7;
            youngblood.WalkSpeed = 2;
            AssertBaseDefense(harness, youngblood);
        }

        private static void AssertBaseDefense(BootcampRuntimeTestHarness.Harness harness, Creature youngblood)
        {
            foreach (var escort in Escorts(harness))
                CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, escort);
            var home = youngblood.HomePos.Position;
            var enemy = Actors(harness).First(actor => actor.DbId == 510216);
            foreach (var other in Actors(harness).Where(actor => actor != enemy &&
                Rasa.Game.Missions.World.CreatureGameplayRules.TracksParticipation(actor)))
                CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, other);
            Place(harness, enemy, home + new Vector3(0, 0, 10));
            enemy.Actions.Clear();
            enemy.RunSpeed = enemy.WalkSpeed = 0;
            var before = Durability(enemy);
            Advance(harness, 20);
            Assert.IsTrue(Durability(enemy) < before, "Youngblood must deal damage through normal AI ticks.");
            Assert.IsTrue(Vector3.Distance(youngblood.Position, home) <= 18.1f);
            Place(harness, enemy, home + new Vector3(0, 0, 26));
            before = Durability(enemy);
            Advance(harness, 12);
            Assert.AreEqual(before, Durability(enemy), "Base defense must not follow an enemy down the route.");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, youngblood.Controller.CurrentAction);
            CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, enemy);
            Place(harness, youngblood, home + new Vector3(0, 0, 12));
            youngblood.HomePos.Position = home;
            for (var tick = 0; tick < 40; tick++)
            {
                Advance(harness, 1);
                AssertGrounded(harness, youngblood);
            }
            Assert.IsTrue(Vector3.Distance(youngblood.Position, home) < 1);
            Assert.IsTrue(youngblood.IsInteractable);
        }

        [TestMethod]
        public void EscortRunHysteresisStartsBeyondTenEndsWithinSixAndStopsWithinFour()
        {
            using var harness = CreateEscorts();
            var escort = Escorts(harness).First();
            var owner = Ground(harness, new Vector3(380, 120, 158));
            harness.MovePlayerTo(owner);
            foreach (var (gap, running) in new[] { (9, false), (11, true), (7, true), (5, false), (3, false) })
            {
                Place(harness, escort, owner + new Vector3(0, 0, gap));
                var before = escort.Position;
                Advance(harness, 1);
                Assert.AreEqual(running, escort.IsRunning, $"Wrong gait at gap {gap}.");
                if (gap == 3)
                    Assert.AreEqual(before, escort.Position);
            }
        }

        [TestMethod]
        [DataRow("friendly")]
        [DataRow("dead")]
        [DataRow("removed")]
        [DataRow("foreign")]
        [DataRow("dead-owner")]
        [DataRow("foreign-owner")]
        public void InvalidOwnerOrEnemyCannotOrderEscortAssistance(string invalid)
        {
            using var harness = CreateEscorts();
            var escort = Escorts(harness).First();
            var position = Ground(harness, new Vector3(380, 120, 158));
            Place(harness, escort, position);
            harness.MovePlayerTo(position);
            var enemy = Actors(harness).First(actor => actor.DbId == 510216);
            Place(harness, enemy, position + new Vector3(0, 0, 6));
            enemy.Actions.Clear();
            if (invalid == "friendly")
                enemy.Faction = Factions.AFS;
            if (invalid == "dead")
                enemy.State = CharacterState.Dead;
            if (invalid == "removed")
                EntityManager.Instance.UnregisterEntity(enemy.EntityId);
            if (invalid == "foreign")
                enemy.RuntimeMapChannel = harness.Maps.GetOrCreatePrivateInstance(1985, 999);
            if (invalid == "dead-owner")
                harness.Client.Player.State = CharacterState.Dead;
            if (invalid == "foreign-owner")
                escort.SpawnPool.FollowOwnerCharacterId = 999;

            ActorManager.Instance.Damage(harness.BootcampMap, enemy, 1, harness.Client.Player);
            Advance(harness, 1);

            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, escort.Controller.CurrentAction);
            Assert.AreNotEqual(enemy.EntityId, escort.Controller.ActionFighting.TargetEntityId);
        }

        [TestMethod]
        public void RespawnDoesNotCarryYoungbloodParticipationIntoANewLife()
        {
            using var harness = CreateEscorts();
            var youngblood = ReachYoungblood(harness);
            var enemy = Actors(harness).First(actor => actor.DbId == 510216);
            Place(harness, enemy, youngblood.Position + new Vector3(0, 0, 8));
            ActorManager.Instance.Damage(harness.BootcampMap, enemy, 1, harness.Client.Player);
            Shoot(harness, youngblood, enemy, 100000);
            var pool = enemy.SpawnPool;
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 20000);
            var respawn = Actors(harness).Single(actor =>
                ReferenceEquals(actor.SpawnPool, pool) && actor.State != CharacterState.Dead);
            Assert.AreNotEqual(enemy.EntityId, respawn.EntityId);
            Place(harness, respawn, youngblood.Position + new Vector3(0, 0, 8));
            var xp = harness.Client.Player.Experience;

            Shoot(harness, youngblood, respawn, 100000);

            Assert.AreEqual(xp, harness.Client.Player.Experience);
            Assert.AreEqual(0UL, respawn.CorpseLootEntityId);
            Assert.AreEqual(0UL, respawn.HarvestOwnerEntityId);
        }

        [TestMethod]
        public void FriendlyScenarioActorsNeverGrantRewards()
        {
            using var harness = CreateEscorts();
            var escort = Escorts(harness).First();
            harness.MovePlayerTo(escort);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var xp = harness.Client.Player.Experience;
            Shoot(harness, harness.Client.Player, escort, 100000);
            Assert.AreEqual(CharacterState.Dead, escort.State);
            Assert.AreEqual(xp, harness.Client.Player.Experience);
            Assert.AreEqual(0UL, escort.CorpseLootEntityId);
        }

        [TestMethod]
        public void PlayerCommandedMinionKeepsItsPassiveAnchorDespiteOwnerAttacks()
        {
            using var harness = CreateEscorts();
            var minion = Escorts(harness).First();
            minion.MasterEntityId = harness.Client.Player.EntityId;
            minion.Stance = MinionStance.Passive;
            BehaviorManager.Instance.SetActionAnchor(minion, minion.Position);
            var anchor = minion.Position;
            var enemy = Actors(harness).First(actor => actor.DbId == 510216);
            Place(harness, enemy, anchor + new Vector3(0, 0, 6));
            harness.MovePlayerTo(anchor);
            Shoot(harness, harness.Client.Player, enemy, 1);

            Advance(harness, 4);

            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, minion.Controller.CurrentAction);
            Assert.IsTrue(minion.Controller.ActionFollow.HasAnchor);
            Assert.AreEqual(anchor, minion.Controller.ActionFollow.Anchor);
            Assert.AreEqual(0UL, minion.Controller.ActionFighting.TargetEntityId);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void EscortQueuedHitCannotDamageFriendlyOrForeignInstanceTarget(bool foreign)
        {
            using var harness = CreateEscorts();
            var escort = Escorts(harness).First();
            var enemy = Actors(harness).First(actor => actor.DbId == 510216);
            Place(harness, enemy, escort.Position + new Vector3(0, 0, 6));
            MissileManager.Instance.MissileLaunch(harness.BootcampMap,
                new ActionData(escort, ActionId.WeaponAttack, 145, enemy.EntityId, 0), 50);
            var durability = Durability(enemy);
            if (foreign)
                enemy.RuntimeMapChannel = harness.Maps.GetOrCreatePrivateInstance(1985, 999);
            else
                enemy.Faction = Factions.AFS;

            MissileManager.Instance.DoWork(harness.BootcampMap, 250);

            Assert.AreEqual(durability, Durability(enemy));
        }

        [TestMethod]
        public void OldDamageOverTimeTicksDoNotOverrideANewOwnerAttack()
        {
            using var harness = CreateEscorts();
            var escort = Escorts(harness).Single(actor => actor.DbId == 510215);
            harness.MovePlayerTo(escort);
            var enemies = Actors(harness).Where(actor => actor.DbId == 510216).Take(2).ToArray();
            foreach (var enemy in enemies)
                Place(harness, enemy, escort.Position + new Vector3(0, 0, 8));
            var effect = new GameEffect
            {
                TypeId = 999, EffectId = GameEffectManager.Instance.NextEffectId(harness.BootcampMap),
                Source = harness.Client.Player, SourceId = harness.Client.Player.EntityId,
                TickDamageMin = 1, TickDamageMax = 1, TickIntervalMs = 1000,
                NextTickTick = Environment.TickCount64 - 1
            };
            GameEffectManager.Instance.Attach(harness.BootcampMap, enemies[0], effect);
            Shoot(harness, harness.Client.Player, enemies[1], 1);

            GameEffectManager.Instance.DoWork(harness.BootcampMap, 1000);
            Advance(harness, 1);

            Assert.AreEqual(enemies[1].EntityId, escort.Controller.ActionFighting.TargetEntityId);
        }

        [TestMethod]
        [DataRow("database")]
        [DataRow("update")]
        [DataRow("rejection")]
        public void ExpectedLootGenerationFailureStillCreditsTizzikAndSchedulesYoungblood(string failure)
        {
            using var harness = CreateEscorts();
            var boss = ReachBoss(harness);
            harness.MovePlayerTo(boss.Position + new Vector3(0, 0, 6));
            CellManager.Instance.UpdateVisibility(harness.Client);
            var xp = harness.Client.Player.Experience;
            var attempts = 0;
            harness.Context.BeforeSave = database =>
            {
                if (!database.ChangeTracker.Entries<ItemEntry>()
                    .Any(entry => entry.State == Microsoft.EntityFrameworkCore.EntityState.Added))
                    return;
                attempts++;
                throw failure switch
                {
                    "database" => new SqliteException("Injected corpse-loot database failure.", 1),
                    "update" => new DbUpdateException("Injected corpse-loot save failure."),
                    _ => new GameplayRejectionException("Injected corpse-loot validation failure.")
                };
            };

            Shoot(harness, harness.Client.Player, boss, 100000);

            Assert.AreEqual(1, attempts, "The real corpse item transaction must hit the injected failure.");
            Assert.AreEqual(CharacterState.Dead, boss.State);
            Assert.IsTrue(harness.Client.Player.Experience > xp);
            Assert.AreEqual(0UL, boss.CorpseLootEntityId, "Failed generation must not publish success-shaped loot.");
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1994].Objectives[1].State);
            var awardedXp = harness.Client.Player.Experience;
            CreatureManager.Instance.HandleCreatureKill(harness.BootcampMap, boss, harness.Client.Player);
            Assert.AreEqual(awardedXp, harness.Client.Player.Experience);
            Assert.AreEqual(1, attempts, "A repeated kill callback must not duplicate the loot attempt or rewards.");

            harness.Context.BeforeSave = null;
            harness.UtcNow += TimeSpan.FromSeconds(7);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.IsNotNull(Actors(harness).SingleOrDefault(actor => actor.DbId == 510207));
            harness.ReconnectFresh();
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1994].Objectives[1].State);
        }

        [TestMethod]
        public void YoungbloodDefendsAgainstTheActualAuthoredBasePackWithoutPlayerCredit()
        {
            using var harness = CreateEscorts();
            var youngblood = ReachYoungblood(harness);
            foreach (var escort in Escorts(harness))
                CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, escort);
            var enemy = Actors(harness).Single(actor => actor.SpawnPool?.DbId == 510250);
            Assert.IsTrue(Vector3.Distance(youngblood.Position, enemy.Position) < 18);
            var before = Durability(enemy);
            var xp = harness.Client.Player.Experience;
            for (var tick = 0; tick < 40 && Durability(enemy) == before; tick++)
                Advance(harness, 1);

            Assert.IsTrue(Durability(enemy) < before,
                "The actual base pack must take damage from Youngblood's normal AI without moving either actor in test setup.");
            Assert.AreEqual(xp, harness.Client.Player.Experience);
            Assert.IsTrue(Vector3.Distance(youngblood.Position, youngblood.HomePos.Position) <= 18);
            AssertGrounded(harness, youngblood);
        }

        [TestMethod]
        public void UnexpectedLootGenerationFailureIsNotSwallowedAtTheKillBoundary()
        {
            using var harness = CreateEscorts();
            var boss = ReachBoss(harness);
            harness.MovePlayerTo(boss.Position + new Vector3(0, 0, 6));
            CellManager.Instance.UpdateVisibility(harness.Client);
            var failure = new InvalidOperationException("Injected loot programming error.");
            harness.Context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<ItemEntry>()
                    .Any(entry => entry.State == Microsoft.EntityFrameworkCore.EntityState.Added))
                    throw failure;
            };

            var thrown = Assert.ThrowsExactly<InvalidOperationException>(() =>
                ActorManager.Instance.Damage(harness.BootcampMap, boss, 100000, harness.Client.Player));

            Assert.AreSame(failure, thrown);
        }

        [TestMethod]
        [DataRow("escort")]
        [DataRow("ownerless")]
        [DataRow("youngblood")]
        public void StationaryMissionActorsPublishOneStopAndOnlyRepublishChangedPositions(string role)
        {
            using var harness = CreateEscorts();
            var actor = role == "youngblood" ? ReachYoungblood(harness) : Escorts(harness).First();
            foreach (var enemy in Actors(harness).Where(Rasa.Game.Missions.World.CreatureGameplayRules.TracksParticipation))
                CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, enemy);
            if (role == "ownerless")
                actor.SpawnPool.FollowOwnerCharacterId = 999;
            harness.MovePlayerTo(actor);
            CellManager.Instance.UpdateVisibility(harness.Client);
            WorldTestContext.Drain(harness.Client);

            Advance(harness, 1);
            var firstStop = TakeMovements(harness, actor);
            Assert.AreEqual(1, firstStop.Length);
            Assert.AreEqual(0f, firstStop[0].Movement.Velocity);
            Advance(harness, 16);
            Assert.AreEqual(0, TakeMovements(harness, actor).Length,
                "An unchanged stopped actor must not publish another movement packet every AI tick.");

            var corrected = Ground(harness, actor.Position + new Vector3(0.3f, 0, 0));
            actor.Position = corrected;
            Advance(harness, 1);
            var correction = TakeMovements(harness, actor);
            Assert.AreEqual(1, correction.Length, "A real corrected position must still be published.");
            Assert.AreEqual(corrected, correction[0].Movement.Position);
            Assert.AreEqual(0f, correction[0].Movement.Velocity);
            Advance(harness, 4);
            Assert.AreEqual(0, TakeMovements(harness, actor).Length);
        }

        [TestMethod]
        [DataRow(8f, false)]
        [DataRow(12f, true)]
        public void WalkingAndRunningEscortsPublishTheirFirstActualStopExactlyOnce(float gap, bool running)
        {
            using var harness = CreateEscorts();
            var escort = Escorts(harness).First();
            foreach (var companion in Escorts(harness))
                companion.AggroRange = 0;
            harness.MovePlayerTo(Ground(harness, escort.Position + new Vector3(gap, 0, 0)));
            CellManager.Instance.UpdateVisibility(harness.Client);
            WorldTestContext.Drain(harness.Client);
            Advance(harness, 1);
            Assert.IsTrue(TakeMovements(harness, escort).Any(packet => packet.Movement.Velocity > 0));
            Assert.AreEqual(running, escort.IsRunning);
            harness.MovePlayerTo(escort);
            CellManager.Instance.UpdateVisibility(harness.Client);
            WorldTestContext.Drain(harness.Client);

            Advance(harness, 1);

            var messages = WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).ToArray();
            var stopped = messages.OfType<MoveObjectMessage>().Where(packet => packet.EntityId == escort.EntityId).ToArray();
            Assert.AreEqual(1, stopped.Length);
            Assert.AreEqual(0f, stopped[0].Movement.Velocity);
            Assert.IsFalse(escort.IsRunning);
            Assert.AreEqual(running ? 1 : 0, messages.OfType<CallMethodMessage>()
                .Where(message => message.EntityId == escort.EntityId)
                .Select(message => message.Packet).OfType<IsRunningPacket>()
                .Count(packet => !packet.IsRunning));
            Advance(harness, 8);
            Assert.AreEqual(0, TakeMovements(harness, escort).Length);
        }

        private static MoveObjectMessage[] TakeMovements(BootcampRuntimeTestHarness.Harness harness, Creature actor) =>
            WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<MoveObjectMessage>()
                .Where(packet => packet.EntityId == actor.EntityId).ToArray();

        [TestMethod]
        [DataRow("missile")]
        [DataRow("direct")]
        [DataRow("effect")]
        public void AttacksWithoutOwnedMissionEscortsDoNotVisitUnrelatedUnloadedCells(string attack)
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            Assert.IsFalse(harness.BootcampMap.SpawnPools.Any(pool => pool.FollowOwnerCharacterId > 0));
            var enemy = Actors(harness).First(actor => actor.DbId == 510216);
            harness.MovePlayerTo(enemy.Position + new Vector3(0, 0, 6));
            CellManager.Instance.UpdateVisibility(harness.Client);
            var before = Durability(enemy);
            // This unloaded cell is outside both actors' visibility cells.
            harness.BootcampMap.MapCellInfo.Cells.Add(uint.MaxValue, null);
            try
            {
                if (attack == "missile")
                    Shoot(harness, harness.Client.Player, enemy, 1);
                else if (attack == "direct")
                    ActorManager.Instance.Damage(harness.BootcampMap, enemy, 1, harness.Client.Player);
                else
                {
                    GameEffectManager.Instance.Attach(harness.BootcampMap, enemy, new GameEffect
                    {
                        TypeId = 999, EffectId = GameEffectManager.Instance.NextEffectId(harness.BootcampMap),
                        Source = harness.Client.Player, SourceId = harness.Client.Player.EntityId,
                        TickDamageMin = 1, TickDamageMax = 1, TickIntervalMs = 1000,
                        NextTickTick = Environment.TickCount64 - 1
                    });
                    GameEffectManager.Instance.DoWork(harness.BootcampMap, 1000);
                }
                Assert.AreEqual(before - 1, Durability(enemy));
            }
            finally
            {
                harness.BootcampMap.MapCellInfo.Cells.Remove(uint.MaxValue);
            }
        }

        [TestMethod]
        public void NearStopGroundCorrectionUsesTheTickSpeedBudgetAndThenStopsOnce()
        {
            using var harness = CreateEscorts();
            var escort = Escorts(harness).First();
            foreach (var companion in Escorts(harness))
                companion.AggroRange = 0;
            var ground = Ground(harness, new Vector3(380, 120, 158));
            Place(harness, escort, ground);
            escort.Position += new Vector3(0, 0.35f, 0);
            harness.MovePlayerTo(Ground(harness, ground + new Vector3(4.05f, 0, 0)));
            CellManager.Instance.UpdateVisibility(harness.Client);
            WorldTestContext.Drain(harness.Client);
            var previous = escort.Position;

            Advance(harness, 1);

            Assert.IsTrue(Vector3.Distance(escort.Position, previous) <= 0.5001f,
                "Ground correction must remain within the 2m/s walking budget for a 250ms tick.");
            Assert.IsTrue(Math.Abs(escort.Position.Y - Ground(harness, escort.Position).Y) < 0.01f,
                "A correction that fits the tick budget must not get stuck just outside the follow stop radius.");
            Assert.IsTrue(TakeMovements(harness, escort).Any(packet =>
                packet.Movement.Velocity > 0 && packet.Movement.Velocity <= 2.0001f));
            Advance(harness, 1);
            Assert.AreEqual(1, TakeMovements(harness, escort).Count(packet => packet.Movement.Velocity == 0));
            Advance(harness, 8);
            Assert.AreEqual(0, TakeMovements(harness, escort).Length);
        }

        private static Creature ReachBoss(BootcampRuntimeTestHarness.Harness harness)
        {
            var deSimone = Actors(harness).Single(actor => actor.DbId == 510206);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, deSimone.EntityId, 1994, 4, 1));
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(1994, 439, out var exit));
            var previous = harness.Client.Player.Position;
            harness.MovePlayerTo(exit.Position);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(new MissionAreaService(() => harness.Manager)
                .RecordAcceptedMovement(harness.Client, previous, exit.Position));
            BootcampRuntimeTestHarness.PrepareDirectDamageClient(harness.Client);
            harness.Client.Player.Attributes[Attributes.Health].Current =
                harness.Client.Player.Attributes[Attributes.Health].CurrentMax = 100000;
            return Actors(harness).Single(actor => actor.DbId == 510210);
        }

        private static void AssertCorpseMenuOpensAtFiveMetres(
            BootcampRuntimeTestHarness.Harness harness, Creature corpse)
        {
            harness.MovePlayerTo(corpse.Position + new Vector3(5, 0, 0));
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();

            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = corpse.CorpseLootEntityId });

            Assert.AreEqual(1, harness.Drain().OfType<LootCorpsePacket>().Count(),
                "An eligible companion kill must open its owner's loot menu at the default manual-use distance.");
        }

        private static Creature ReachYoungblood(BootcampRuntimeTestHarness.Harness harness)
        {
            var boss = ReachBoss(harness);
            harness.MovePlayerTo(boss.Position + new Vector3(0, 0, 6));
            CellManager.Instance.UpdateVisibility(harness.Client);
            Shoot(harness, harness.Client.Player, boss, 100000);
            harness.UtcNow += TimeSpan.FromSeconds(7);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            var youngblood = Actors(harness).Single(actor => actor.DbId == 510207);
            harness.MovePlayerTo(youngblood);
            CellManager.Instance.UpdateVisibility(harness.Client);
            return youngblood;
        }

        private static int Durability(Creature creature) =>
            creature.Attributes[Attributes.Health].Current + creature.Attributes[Attributes.Armor].Current;

        private static void Shoot(BootcampRuntimeTestHarness.Harness harness, Actor source, Creature target, int damage)
        {
            MissileManager.Instance.MissileLaunch(harness.BootcampMap,
                new ActionData(source, ActionId.WeaponAttack, 133, target.EntityId, 0), damage);
            MissileManager.Instance.DoWork(harness.BootcampMap, 250);
        }

        private static BootcampRuntimeTestHarness.Harness CreateEscorts()
        {
            var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = Actors(harness).Single(actor => actor.DbId == 510206);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
            return harness;
        }

        private static Creature[] Actors(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToArray();

        private static Creature[] Escorts(BootcampRuntimeTestHarness.Harness harness) =>
            Actors(harness).Where(actor => actor.SpawnPool?.FollowOwnerCharacterId == harness.Client.Player.Id).ToArray();

        private static Vector3 Ground(BootcampRuntimeTestHarness.Harness harness, Vector3 position) =>
            harness.BootcampMap.NavMesh.Nearest(position) ??
            throw new AssertFailedException($"No navmesh near {position}.");

        private static void WalkRoute(System.Collections.Generic.List<Vector3> route,
            ref int corner, ref Vector3 position, float distance)
        {
            while (corner < route.Count && distance > 0)
            {
                var gap = Vector3.Distance(position, route[corner]);
                if (gap <= distance)
                {
                    position = route[corner++];
                    distance -= gap;
                }
                else
                {
                    position += Vector3.Normalize(route[corner] - position) * distance;
                    distance = 0;
                }
            }
        }

        private static void Place(BootcampRuntimeTestHarness.Harness harness, Creature creature, Vector3 position)
        {
            CreatureManager.Instance.SetLocation(creature, Ground(harness, position), 0, 1985);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            CreatureManager.Instance.CellUpdateLocation(harness.BootcampMap, creature, seed & 0xffff, seed >> 16);
        }

        private static void Advance(BootcampRuntimeTestHarness.Harness harness, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
                MissileManager.Instance.DoWork(harness.BootcampMap, 250);
            }
        }

        private static void AssertGrounded(BootcampRuntimeTestHarness.Harness harness, Creature creature) =>
            Assert.IsTrue(Vector3.Distance(Ground(harness, creature.Position), creature.Position) < 0.5f,
                $"Creature {creature.DbId} is off ground at {creature.Position}.");
    }
}
