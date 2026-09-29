using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Game.Missions.World;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionManualCombatTests
    {
        [TestMethod]
        [DataRow("ordinary")]
        [DataRow("revoked-projectile")]
        [DataRow("suppressed-recovery")]
        public void PublicNegotiatorRequiresAnExactAttackCommandBeforeCombatAndReturnsToPassive(string lifetime)
        {
            MissionSceneDefinition definition = null;
            using var harness = WildernessRuntimeTestHarness.Create(migration =>
            {
                var mission = new WildernessMissionDataV1(migration, 65012, "Manual combat fixture", 3179, 96, 96, 1);
                mission.Objective(1, 3179, 3180, 1, MissionObjectiveState.Incomplete);
                mission.Transition(1);
                mission.Progress(1, MissionProgressEventKind.CreatureKilled, 3);
                mission.Reward(0, 0);
                mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
                definition = new MissionSceneDefinition
                {
                    Script = "data.sequence",
                    PublicEncounter = new PublicEncounterBinding(65012, 530130, "negotiator", "data.sequence",
                        OwnerLossPolicy: "Fail", ManualCombat: true),
                    Actors = new Dictionary<string, SceneActorDefinition>
                    {
                        ["negotiator"] = new("negotiator", SceneActorKind.PublicSpawn, 530130)
                    },
                    Sequences = new()
                    {
                        [0] = new() { World = { new EnsureActorIntent("claim-negotiator", "negotiator") } },
                        [1] = new() { World = { new AttackActorIntent("begin-fight", "negotiator") } },
                        [2] = new() { World = { new AttackActorIntent("fresh-fight", "negotiator") } }
                    },
                    Names = new() { ["fight"] = 1, ["fresh-fight"] = 2 }
                };
                mission.Enable(definition);
            });
            harness.SpawnWorld(171, 530130);
            var burke = harness.Npc(171);
            var negotiator = harness.Npc(530130);
            Assert.IsNotNull(burke);
            Assert.IsNotNull(negotiator);
            harness.MoveTo(burke.Position);

            harness.Tick(3500);

            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction,
                "An unleased negotiator must not automatically attack players or nearby NPCs.");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, burke.Controller.CurrentAction,
                "Nearby AFS contacts must not auto-acquire the protected negotiator.");
            var health = negotiator.Attributes[Attributes.Health].Current;
            var shot = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, negotiator.EntityId, 0);
            MissileManager.Instance.MissileLaunch(harness.Map, shot, 20);
            MissileManager.Instance.DoWork(harness.Map, 1000);
            Assert.AreEqual(health, negotiator.Attributes[Attributes.Health].Current,
                "A direct player shot must not bypass negotiation protection.");
            BehaviorManager.Instance.SetActionFighting(negotiator, harness.Client.Player.EntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction,
                "Ordinary retaliation cannot authorize scripted combat.");
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, burke.EntityId, 65012));
            var firstLease = harness.Manager.PublicActors.Handle(harness.Map, 530130);
            harness.Tick(3500);
            Assert.AreEqual(health, negotiator.Attributes[Attributes.Health].Current);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction);

            Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, 65012, "fight"));
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
                Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(harness.Client, 65012, "fresh-fight"));
                Assert.IsTrue(CreatureGameplayRules.CanParticipateInCombat(negotiator));
            }
            if (lifetime == "revoked-projectile")
            {
                for (var tick = 0; tick < 30 &&
                    !harness.Map.QueuedMissiles.Any(missile => ReferenceEquals(missile.Source, negotiator)); tick++)
                    harness.Tick(250);
                Assert.IsTrue(harness.Map.QueuedMissiles.Any(missile => ReferenceEquals(missile.Source, negotiator)));
            }

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 65012));
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
            Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map, 530130));
            BehaviorManager.Instance.SetActionFighting(negotiator, harness.Client.Player.EntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction);
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            second.SetWorldPosition(burke.Position, 0);
            CellManager.Instance.UpdateVisibility(second);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, burke.EntityId, 65012));
            Assert.IsFalse(harness.Manager.PublicActors.AuthorizeCombat(harness.Map, firstLease, negotiator),
                "A retired attack command cannot arm the next owner's negotiator.");
            BehaviorManager.Instance.SetActionFighting(negotiator, second.Player.EntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, negotiator.Controller.CurrentAction);
            if (lifetime == "revoked-projectile")
            {
                Assert.IsTrue(harness.Manager.Scenes.ExecuteNamed(second, 65012, "fight"));
                MissileManager.Instance.DoWork(harness.Map, 10000);
                Assert.AreEqual(playerHealth, harness.Client.Player.Attributes[Attributes.Health].Current,
                    "An old lease's missile must not become valid when the same actor starts a new fight.");
            }
        }
    }
}
