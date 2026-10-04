using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Game;
using Rasa.Game.Missions.World;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.MapChannel.Server.PerformRecovery;
using Rasa.Packets.Protocol;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;
using Rasa.Test.World;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionManualCombatTests
    {
        private const uint FixtureMissionId = 65012;
        private const uint SkeevSpawnId = 630130;

        [TestMethod]
        [DataRow("ordinary")]
        [DataRow("revoked-projectile")]
        [DataRow("suppressed-recovery")]
        public void PublicNegotiatorRequiresAnExactAttackCommandBeforeCombatAndReturnsToPassive(string lifetime)
        {
            using var harness = CreateFixture(out var definition);
            harness.SpawnWorld(171, SkeevSpawnId);
            var burke = harness.Npc(171);
            var negotiator = harness.Npc(SkeevSpawnId);
            Assert.IsNotNull(burke);
            Assert.IsNotNull(negotiator);
            harness.MoveTo(burke.Position);

            harness.Tick(3500);

            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction,
                "An unleased negotiator must not automatically attack players or nearby NPCs.");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, burke.Controller.CurrentAction,
                "Nearby AFS contacts must not auto-acquire the protected negotiator.");
            var health = negotiator.Attributes[Attributes.Health].Current;
            harness.Drain();
            var shot = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, negotiator.EntityId, 0);
            MissileManager.Instance.MissileLaunch(harness.Map, shot, 20);
            MissileManager.Instance.DoWork(harness.Map, 1000);
            Assert.AreEqual(health, negotiator.Attributes[Attributes.Health].Current,
                "A direct player shot must not bypass negotiation protection.");
            var immune = harness.Drain().OfType<WeaponAttackRecovery>()
                .SelectMany(packet => packet.Missile.Args.HitData)
                .Single(hit => hit.EntityId == negotiator.EntityId);
            Assert.AreEqual(1, immune.WasImune);
            Assert.AreEqual(0L, immune.FinalAmt);
            BehaviorManager.Instance.SetActionFighting(negotiator, harness.Client.Player.EntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction,
                "Ordinary retaliation cannot authorize scripted combat.");
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, burke.EntityId, FixtureMissionId));
            var firstLease = harness.Manager.PublicActors.Handle(harness.Map, SkeevSpawnId);
            harness.Tick(3500);
            Assert.AreEqual(health, negotiator.Attributes[Attributes.Health].Current);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction);

            Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fight"));
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction);
            if (lifetime == "suppressed-recovery")
            {
                harness.Client.Player.GmFlagAlwaysFriendly = true;
                harness.Manager.Scenes.Attach(harness.Client, firstLease.RunId,
                    definition.Bindings(WildernessMissionDataV1.Revision));
                using (var unit = harness.CreateChar())
                    Assert.AreEqual("Cancelled", unit.CharacterMissions.Runtime.Effects(firstLease.RunId)
                        .Single(effect => effect.OperationKey == "begin-fight").Status);
                Assert.IsFalse(CreatureGameplayRules.CanParticipateInCombat(negotiator),
                    "A cancelled recovery replay must revoke its runtime combat authorization.");
                Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction);
                harness.Client.Player.GmFlagAlwaysFriendly = false;
                Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fresh-fight"));
                Assert.IsTrue(CreatureGameplayRules.CanParticipateInCombat(negotiator));
            }
            if (lifetime == "revoked-projectile")
            {
                for (var tick = 0; tick < 30 &&
                    !harness.Map.QueuedMissiles.Any(missile => ReferenceEquals(missile.Source, negotiator)); tick++)
                    harness.Tick(250);
                Assert.IsTrue(harness.Map.QueuedMissiles.Any(missile => ReferenceEquals(missile.Source, negotiator)));
            }

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, FixtureMissionId));
            var pendingAttacks = harness.Map.QueuedMissiles.Count(missile => ReferenceEquals(missile.Source, negotiator));
            var playerHealth = harness.Client.Player.Attributes[Attributes.Health].Current;
            if (lifetime == "revoked-projectile")
            {
                BehaviorManager.Instance.MapChannelThink(harness.Map, 5000);
                Assert.AreEqual(pendingAttacks,
                    harness.Map.QueuedMissiles.Count(missile => ReferenceEquals(missile.Source, negotiator)),
                    "The native pre-scene think phase must not launch attacks after revocation.");
            }
            harness.Tick();
            Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map, SkeevSpawnId));
            BehaviorManager.Instance.SetActionFighting(negotiator, harness.Client.Player.EntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction);
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            second.SetWorldPosition(burke.Position, 0);
            CellManager.Instance.UpdateVisibility(second);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, burke.EntityId, FixtureMissionId));
            Assert.IsFalse(harness.Manager.PublicActors.AuthorizeCombat(harness.Map, firstLease, negotiator),
                "A retired attack command cannot arm the next owner's negotiator.");
            BehaviorManager.Instance.SetActionFighting(negotiator, second.Player.EntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction);
            if (lifetime == "revoked-projectile")
            {
                Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(second, FixtureMissionId, "fight"));
                MissileManager.Instance.DoWork(harness.Map, 10000);
                Assert.AreEqual(playerHealth, harness.Client.Player.Attributes[Attributes.Health].Current,
                    "An old lease's missile must not become valid when the same actor starts a new fight.");
            }
        }

        [TestMethod]
        [DataRow("area", false)]
        [DataRow("deferred", false)]
        [DataRow("charge", false)]
        [DataRow("area", true)]
        [DataRow("deferred", true)]
        [DataRow("charge", true)]
        public void RetiredAreaDeferredAndChargeAttacksCannotInheritRearmedAuthority(string kind, bool replaceLease)
        {
            using var harness = CreateFixture(out _);
            var attacker = AcceptFixture(harness);
            harness.LoadAbilities();
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            PlaceVictims(harness, attacker, second, kind);
            Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fight"));
            var first = attacker.ScriptedCombatAuthorization;
            Assert.IsNotNull(first);

            try
            {
                var oldMissile = QueueAttack(harness, attacker, kind);
                if (replaceLease)
                {
                    Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, FixtureMissionId));
                    harness.Manager.PublicActors.Tick(harness.Map);
                    Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map, SkeevSpawnId));
                    var burke = harness.Npc(171);
                    second.SetWorldPosition(burke.Position, 0);
                    CellManager.Instance.UpdateVisibility(second);
                    Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, burke.EntityId, FixtureMissionId));
                    Assert.AreSame(attacker, harness.Npc(SkeevSpawnId),
                        "Reuse the actual actor so identity alone cannot reject the old action.");
                    Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(second, FixtureMissionId, "fight"));
                    Assert.AreNotEqual(first.Handle, attacker.ScriptedCombatAuthorization.Handle);
                }
                else
                {
                    harness.Manager.PublicActors.RevokeCombat(harness.Map, first.Handle, first.OperationKey);
                    Assert.IsTrue(harness.Manager.PublicActors.AuthorizeCombat(
                        harness.Map, first.Handle, attacker, first.OperationKey));
                    Assert.AreEqual(first.Handle, attacker.ScriptedCombatAuthorization.Handle);
                    Assert.AreEqual(first.OperationKey, attacker.ScriptedCombatAuthorization.OperationKey);
                }
                Assert.AreNotSame(first, attacker.ScriptedCombatAuthorization);
                Assert.IsTrue(CreatureGameplayRules.CanParticipateInCombat(attacker));
                PlaceVictims(harness, attacker, second, kind, prepareHealth: false);

                FinishQueuedAttacks(harness, attacker);

                Assert.AreEqual(1000, harness.Client.Player.Attributes[Attributes.Health].Current,
                    "The retired primary hit must not acquire the new authorization.");
                Assert.AreEqual(1000, second.Player.Attributes[Attributes.Health].Current,
                    "The retired area's secondary hit must not acquire the new authorization.");
                if (oldMissile != null)
                {
                    Assert.AreEqual(0, oldMissile.Args.HitEntities.Count);
                    Assert.AreEqual(0, oldMissile.Args.HitData.Count);
                }
                Assert.IsFalse(harness.Map.QueuedMissiles.Any(missile => missile.Source == attacker));

                QueueAttack(harness, attacker, kind);
                FinishQueuedAttacks(harness, attacker);
                Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Health].Current < 1000,
                    "A fresh action under the current authorization must still hit its primary target.");
                Assert.IsTrue(second.Player.Attributes[Attributes.Health].Current < 1000,
                    "The real upstream area attack must still hit its secondary target.");
            }
            finally
            {
                RetireQueuedAttacks(harness, attacker);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RetiredDeferredCannotInterruptRearmedSameAction(bool sameOperation)
        {
            using var harness = CreateFixture(out _);
            var attacker = AcceptFixture(harness);
            harness.LoadAbilities();
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            PlaceVictims(harness, attacker, second, "deferred");
            Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fight"));
            var first = attacker.ScriptedCombatAuthorization;
            Assert.IsNotNull(first);
            harness.Drain();

            try
            {
                QueueAttack(harness, attacker, "deferred");
                var firstDeadline = attacker.Controller.WindupUntil;
                harness.Manager.PublicActors.RevokeCombat(harness.Map, first.Handle, first.OperationKey);
                Assert.IsTrue(harness.Manager.PublicActors.AuthorizeCombat(harness.Map, first.Handle, attacker,
                    sameOperation ? first.OperationKey : "replacement-windup"));
                var current = attacker.ScriptedCombatAuthorization;
                Assert.AreNotSame(first, current);
                if (sameOperation)
                    Assert.AreEqual(first, current, "An equal-valued record is still a different grant.");

                // Let A become due without draining it, then start B before the next worker pass.
                Assert.IsTrue(SpinWait.SpinUntil(() => Environment.TickCount64 >= firstDeadline,
                    TimeSpan.FromSeconds(5)));
                QueueAttack(harness, attacker, "deferred");
                var currentDeadline = attacker.Controller.WindupUntil;
                var position = attacker.Position;
                var carry = attacker.KnockbackTo;
                var carrySpeed = attacker.KnockbackSpeed;
                var carryIsPull = attacker.KnockbackIsPull;
                var windups = harness.Drain().OfType<PerformWindupPacket>()
                    .Count(packet => packet.ActionId == ActionId.CrKaelSmash && packet.ActionArgId == 1);

                CreatureWindups.Worker(harness.Map);

                var messages = WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).ToArray();
                Assert.AreEqual(2, windups, "Both grants started the same client-visible action.");
                Assert.IsFalse(messages.OfType<CallMethodMessage>().Select(message => message.Packet)
                    .OfType<ActionInterruptPacket>().Any(packet => packet.SourceId == attacker.EntityId &&
                        packet.ActionId == ActionId.CrKaelSmash && packet.ActionArgId == 1),
                    "A's rejected callback must not cancel B's matching client action.");
                Assert.IsFalse(messages.OfType<MoveObjectMessage>().Any(message => message.EntityId == attacker.EntityId),
                    "Draining A must not publish a movement reset for B.");
                Assert.AreEqual(currentDeadline, attacker.Controller.WindupUntil);
                Assert.AreEqual(position, attacker.Position);
                Assert.AreEqual(carry, attacker.KnockbackTo);
                Assert.AreEqual(carrySpeed, attacker.KnockbackSpeed);
                Assert.AreEqual(carryIsPull, attacker.KnockbackIsPull);
                Assert.AreSame(current, attacker.ScriptedCombatAuthorization);
                Assert.IsTrue(CreatureWindups.HasPending(attacker), "B must still be waiting for its own windup.");
                Assert.IsFalse(harness.Map.QueuedMissiles.Any(missile => missile.Source == attacker),
                    "A must not have executed its damage callback.");
                Assert.AreEqual(1000, harness.Client.Player.Attributes[Attributes.Health].Current);
                Assert.AreEqual(1000, second.Player.Attributes[Attributes.Health].Current);

                FinishQueuedAttacks(harness, attacker);

                Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Health].Current < 1000);
                Assert.IsTrue(second.Player.Attributes[Attributes.Health].Current < 1000,
                    "B's normal callback must still resolve its real area damage.");
            }
            finally
            {
                RetireQueuedAttacks(harness, attacker);
            }
        }

        [TestMethod]
        [DataRow("revoked")]
        [DataRow("stunned")]
        [DataRow("ordinary-stunned")]
        public void DeferredStillInterruptsWithoutANewerGrant(string cancellation)
        {
            var ordinary = cancellation == "ordinary-stunned";
            using var harness = CreateFixture(out _, creatureTarget: ordinary);
            var attacker = AcceptFixture(harness);
            if (ordinary)
                attacker = SceneOpponent(harness);
            harness.LoadAbilities();
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            PlaceVictims(harness, attacker, second, "deferred");
            if (!ordinary)
                Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fight"));

            try
            {
                QueueAttack(harness, attacker, "deferred");
                if (cancellation == "revoked")
                {
                    var grant = attacker.ScriptedCombatAuthorization;
                    harness.Manager.PublicActors.RevokeCombat(harness.Map, grant.Handle, grant.OperationKey);
                    Assert.IsNull(attacker.ScriptedCombatAuthorization);
                }
                else
                    Assert.IsTrue(Stuns.Apply(harness.Map, attacker, harness.Client.Player,
                        Stuns.StunTypeId, 10000, DamageType.Physical));
                harness.Drain();

                FinishQueuedAttacks(harness, attacker);

                Assert.AreEqual(1, harness.Drain().OfType<ActionInterruptPacket>()
                    .Count(packet => packet.SourceId == attacker.EntityId &&
                        packet.ActionId == ActionId.CrKaelSmash && packet.ActionArgId == 1));
                Assert.AreEqual(1000, harness.Client.Player.Attributes[Attributes.Health].Current);
                Assert.AreEqual(1000, second.Player.Attributes[Attributes.Health].Current);
            }
            finally
            {
                RetireQueuedAttacks(harness, attacker);
            }
        }

        [TestMethod]
        [DataRow("deferred", "removed")]
        [DataRow("charge", "removed")]
        [DataRow("deferred", "same-map-id")]
        [DataRow("charge", "same-map-id")]
        [DataRow("deferred", "zero-health")]
        [DataRow("charge", "zero-health")]
        [DataRow("deferred", "dying")]
        [DataRow("charge", "dying")]
        public void InvalidManualActorCannotFinishADeferredOrChargeAttack(string kind, string invalidation)
        {
            using var harness = CreateFixture(out _);
            var attacker = AcceptFixture(harness);
            harness.LoadAbilities();
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            PlaceVictims(harness, attacker, second, kind);
            Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fight"));
            try
            {
                QueueAttack(harness, attacker, kind);
                switch (invalidation)
                {
                    case "removed":
                        Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(harness.Map, attacker));
                        break;
                    case "same-map-id":
                        var otherMap = new MapChannel { MapInfo = harness.Map.MapInfo, InstanceId = harness.Map.InstanceId };
                        Assert.AreNotEqual(attacker.ScriptedCombatAuthorization.Handle.MapEpoch, otherMap.MissionEpoch);
                        attacker.RuntimeMapChannel = otherMap;
                        break;
                    case "zero-health":
                        attacker.Attributes[Attributes.Health].Current = 0;
                        break;
                    case "dying":
                        attacker.State = CharacterState.Dying;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(invalidation));
                }
                Assert.AreNotEqual(CharacterState.Dead, attacker.State,
                    "Exercise more than the original Dead-state check.");
                FinishQueuedAttacks(harness, attacker);
                Assert.AreEqual(1000, harness.Client.Player.Attributes[Attributes.Health].Current);
                Assert.AreEqual(1000, second.Player.Attributes[Attributes.Health].Current);
                Assert.IsFalse(harness.Map.QueuedMissiles.Any(missile => missile.Source == attacker));
            }
            finally
            {
                attacker.RuntimeMapChannel = harness.Map;
                RetireQueuedAttacks(harness, attacker);
            }
        }

        [TestMethod]
        [DataRow("area")]
        [DataRow("deferred")]
        [DataRow("charge")]
        public void OrdinaryActorsKeepTheirAreaDeferredAndChargeAttacks(string kind)
        {
            using var harness = CreateFixture(out _, creatureTarget: true);
            AcceptFixture(harness);
            var attacker = SceneOpponent(harness);
            harness.LoadAbilities();
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            PlaceVictims(harness, attacker, second, kind);
            Assert.IsNull(attacker.ScriptedCombatGate);
            Assert.IsNull(attacker.ScriptedCombatAuthorization);
            try
            {
                QueueAttack(harness, attacker, kind);
                FinishQueuedAttacks(harness, attacker);
                Assert.IsTrue(harness.Client.Player.Attributes[Attributes.Health].Current < 1000);
                Assert.IsTrue(second.Player.Attributes[Attributes.Health].Current < 1000);
            }
            finally
            {
                attacker.State = CharacterState.Dead;
                FinishQueuedAttacks(harness, attacker);
            }
        }

        [TestMethod]
        [DataRow("passive")]
        [DataRow("no-attack")]
        [DataRow("returning")]
        [DataRow("friendly")]
        [DataRow("decoration")]
        [DataRow("frightened")]
        [DataRow("subverted")]
        public void SceneAttackSuppressionDoesNotAuthorizeAnAiRefusalAndRevokesItsReplay(string refusal)
        {
            using var harness = CreateFixture(out var definition);
            var attacker = AcceptFixture(harness);
            var lease = harness.Manager.PublicActors.Handle(harness.Map, SkeevSpawnId);
            var restore = RefuseFight(harness, attacker, refusal);
            Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fight"));
            AssertSuppressed(harness, attacker, lease, "begin-fight");
            restore();

            Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fresh-fight"));
            Assert.IsNotNull(attacker.ScriptedCombatAuthorization);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, attacker.Controller.CurrentAction);
            restore = RefuseFight(harness, attacker, refusal);
            harness.Manager.Scenes.Attach(harness.Client, lease.RunId,
                definition.Bindings(WildernessMissionDataV1.Revision));
            AssertSuppressed(harness, attacker, lease, "fresh-fight");
            restore();
        }

        [TestMethod]
        public void SceneAttackAllowsNeutralActorsToFightEachOther()
        {
            using var harness = CreateFixture(out _, creatureTarget: true);
            var attacker = AcceptFixture(harness);
            var target = SceneOpponent(harness);
            attacker.TargetCategory = TargetCategory.Neutral;
            target.TargetCategory = TargetCategory.Neutral;

            Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, FixtureMissionId, "fight"));

            Assert.IsNotNull(attacker.ScriptedCombatAuthorization);
            Assert.IsTrue(BehaviorManager.MayFight(attacker, target.EntityId));
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, attacker.Controller.CurrentAction);
            Assert.AreEqual(target.EntityId, attacker.Controller.ActionFighting.TargetEntityId);
            using var unit = harness.CreateChar();
            Assert.AreEqual("Applied", unit.CharacterMissions.Runtime.Effects(
                attacker.ScriptedCombatAuthorization.Handle.RunId).Single(effect => effect.OperationKey == "begin-fight").Status);
        }

        private static WildernessRuntimeTestHarness CreateFixture(out MissionSceneDefinition definition,
            bool creatureTarget = false)
        {
            MissionSceneDefinition created = null;
            var harness = WildernessRuntimeTestHarness.Create(migration =>
            {
                var mission = new WildernessMissionDataV1(migration, FixtureMissionId,
                    "Manual combat fixture", 3179, 96, 96, 1);
                mission.Objective(1, 3179, 3180, 1, MissionObjectiveState.Incomplete);
                mission.Transition(1);
                mission.Progress(1, MissionProgressEventKind.CreatureKilled, 3);
                mission.Reward(0, 0);
                mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
                created = new MissionSceneDefinition
                {
                    Script = "data.sequence",
                    PublicEncounter = new PublicEncounterBinding(FixtureMissionId, SkeevSpawnId,
                        "negotiator", "data.sequence", OwnerLossPolicy: "Fail", ManualCombat: true),
                    Actors = new Dictionary<string, SceneActorDefinition>
                    {
                        ["negotiator"] = new("negotiator", SceneActorKind.PublicSpawn, SkeevSpawnId)
                    },
                    Sequences = new()
                    {
                        [0] = new() { World = { new EnsureActorIntent("claim-negotiator", "negotiator") } },
                        [1] = new() { World = { new AttackActorIntent("begin-fight", "negotiator",
                            creatureTarget ? "opponent" : null) } },
                        [2] = new() { World = { new AttackActorIntent("fresh-fight", "negotiator",
                            creatureTarget ? "opponent" : null) } }
                    },
                    Names = new() { ["fight"] = 1, ["fresh-fight"] = 2 }
                };
                if (creatureTarget)
                {
                    created.Actors["opponent"] = new SceneActorDefinition("opponent", SceneActorKind.Creature, 82,
                        new ScenePosition(-399, 173.679004f, 178));
                    created.Sequences[0].World.Add(new EnsureActorIntent("ensure-opponent", "opponent"));
                }
                mission.Enable(created);
            });
            definition = created;
            return harness;
        }

        private static Creature AcceptFixture(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(171, SkeevSpawnId);
            var burke = harness.Npc(171);
            harness.MoveTo(burke.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, burke.EntityId, FixtureMissionId));
            return harness.Npc(SkeevSpawnId);
        }

        private static Creature SceneOpponent(WildernessRuntimeTestHarness harness)
        {
            var lease = harness.Manager.PublicActors.Handle(harness.Map, SkeevSpawnId);
            return harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Distinct().Single(creature => creature.SpawnPool?.SceneRunId == lease.RunId && creature.DbId == 82);
        }

        private static void PlaceVictims(WildernessRuntimeTestHarness harness, Creature attacker, Client second,
            string kind, bool prepareHealth = true)
        {
            var position = attacker.Position + new Vector3(kind == "charge" ? 4 : 1, 0, 0);
            harness.MoveTo(position);
            second.SetWorldPosition(position + new Vector3(0, 0, 0.25f), 0);
            CellManager.Instance.UpdateVisibility(second);
            foreach (var player in new[] { harness.Client.Player, second.Player })
            {
                if (prepareHealth)
                {
                    player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
                    player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
                }
                Assert.IsTrue(CreatureManager.IsLivingOnMap(harness.Map, player));
            }
        }

        private static Missile QueueAttack(WildernessRuntimeTestHarness harness, Creature attacker, string kind)
        {
            var action = new CreatureAction
            {
                ActionId = kind == "charge" ? ActionId.CrKaelRushingBlow : ActionId.CrKaelSmash,
                ActionArgId = 1
            };
            Assert.IsTrue(AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info));
            var target = harness.Client.Player;
            if (kind == "charge")
            {
                KaelRushingBlow.Start(harness.Map, attacker, action, target, 20);
                Assert.IsTrue(KaelRushingBlow.IsCharging(attacker));
                return null;
            }
            var area = CreatureArea.Of(info);
            Assert.IsTrue(area.IsArea, "Use the actual upstream attack's area data.");
            if (kind == "deferred")
            {
                var centre = target.Position;
                var windup = CreatureWindups.WindupMsOf(action, info);
                Assert.IsTrue(windup > 0);
                CellManager.Instance.CellCallMethod(harness.Map, attacker,
                    new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, target.EntityId));
                CreatureWindups.After(harness.Map, attacker, windup,
                    () => MissileManager.Instance.CreatureStrike(harness.Map, attacker, action, target, 20, area, centre),
                    action);
                Assert.IsTrue(CreatureWindups.HasPending(attacker));
                return null;
            }
            var landsIn = CreatureWindups.Begin(harness.Map, attacker, action,
                Vector3.Distance(attacker.Position, target.Position));
            Assert.IsTrue(landsIn.HasValue);
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(attacker, action.ActionId, action.ActionArgId, target.EntityId, 0),
                20, damageType: DamageType.Physical, creatureAction: action, landsInMs: landsIn);
            var missile = harness.Map.QueuedMissiles.Last(entry => entry.Source == attacker);
            Assert.IsTrue(missile.AfterWindup);
            Assert.AreSame(attacker.ScriptedCombatAuthorization, missile.SourceCombatAuthorization);
            return missile;
        }

        private static void FinishQueuedAttacks(WildernessRuntimeTestHarness harness, Creature attacker)
        {
            Assert.IsTrue(SpinWait.SpinUntil(() =>
            {
                CreatureWindups.Worker(harness.Map);
                KaelRushingBlow.Worker(harness.Map);
                return !CreatureWindups.HasPending(attacker) && !KaelRushingBlow.IsCharging(attacker);
            }, TimeSpan.FromSeconds(5)), "The real deferred/charge worker must consume its pending action.");
            MissileManager.Instance.DoWork(harness.Map, 10000);
        }

        private static void RetireQueuedAttacks(WildernessRuntimeTestHarness harness, Creature attacker)
        {
            var lease = harness.Manager.PublicActors.Handle(harness.Map, SkeevSpawnId);
            if (lease != null)
                harness.Manager.PublicActors.RevokeCombat(harness.Map, lease);
            FinishQueuedAttacks(harness, attacker);
        }

        private static void AssertSuppressed(WildernessRuntimeTestHarness harness, Creature attacker,
            ActorHandle lease, string operationKey)
        {
            using var unit = harness.CreateChar();
            Assert.AreEqual("Cancelled", unit.CharacterMissions.Runtime.Effects(lease.RunId)
                .Single(effect => effect.OperationKey == operationKey).Status);
            Assert.IsNull(attacker.ScriptedCombatAuthorization);
            Assert.IsFalse(CreatureGameplayRules.CanParticipateInCombat(attacker));
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, attacker.Controller.CurrentAction);
        }

        private static Action RefuseFight(WildernessRuntimeTestHarness harness, Creature attacker, string refusal)
        {
            var category = attacker.TargetCategory;
            var master = attacker.MasterEntityId;
            var stance = attacker.Stance;
            var actions = attacker.Actions.ToArray();
            GameEffect mindControl = null;
            switch (refusal)
            {
                case "passive":
                    attacker.MasterEntityId = harness.Client.Player.EntityId;
                    attacker.Stance = MinionStance.Passive;
                    break;
                case "no-attack":
                    attacker.MasterEntityId = harness.Client.Player.EntityId;
                    attacker.Actions.Clear();
                    break;
                case "returning":
                    BehaviorManager.Instance.Leash(harness.Map, attacker);
                    break;
                case "friendly":
                    attacker.TargetCategory = TargetCategory.Friendly;
                    break;
                case "decoration":
                    attacker.TargetCategory = TargetCategory.Decoration;
                    break;
                case "frightened":
                case "subverted":
                    mindControl = new GameEffect
                    {
                        EffectId = GameEffectManager.Instance.NextEffectId(harness.Map),
                        TypeId = AbilityManager.MindControlTypeId,
                        MindControlPump = refusal == "frightened"
                            ? AbilityManager.MindControlFrighten : AbilityManager.MindControlSubversion
                    };
                    GameEffectManager.Instance.Attach(harness.Map, attacker, mindControl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(refusal));
            }
            return () =>
            {
                attacker.TargetCategory = category;
                attacker.MasterEntityId = master;
                attacker.Stance = stance;
                attacker.Actions.Clear();
                attacker.Actions.AddRange(actions);
                if (mindControl != null)
                    GameEffectManager.Instance.DettachEffect(harness.Map, attacker, mindControl);
                BehaviorManager.Instance.SetActionAnchor(attacker, attacker.Position);
            };
        }
    }
}
