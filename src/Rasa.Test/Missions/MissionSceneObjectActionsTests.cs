using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Missions.World;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionSceneObjectActionsTests
    {
        private const uint MissionId = 65010;

        [TestMethod]
        public void NativeSurveyArgumentThreeRunsItsExactAssignmentSequenceOnce()
        {
            using var harness = Create(destructible: false);
            var obj = Target(harness);

            Use(harness, harness.Client, obj, 3);

            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[MissionId].Objectives[1].State);
            using (var unit = harness.CreateChar())
                Assert.AreEqual(1U, unit.CharacterFlags.Get(1)[65010]);
            Assert.IsFalse(obj.IsEnabled);
            Assert.AreEqual((UseObjectState)56, obj.StateId);
            var count = harness.Map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = 3
            });
            Assert.AreEqual(count, harness.Map.PerformRecovery.Count);
        }

        [TestMethod]
        [DataRow("other-player")]
        [DataRow("wrong-argument")]
        [DataRow("stale-generation")]
        public void NativeSceneUseRejectsUnownedOrStaleSourcesBeforeWindup(string invalid)
        {
            using var harness = Create(destructible: false);
            var obj = Target(harness);
            var second = invalid == "other-player"
                ? harness.Context.CreateAdditionalClient(2, manager: harness.Manager) : null;
            try
            {
                var client = second ?? harness.Client;
                client.SetWorldPosition(obj.Position, 0);
                if (invalid == "stale-generation")
                    obj.SceneGeneration++;

                harness.Objects.RequestUseObjectPacket(client, new RequestUseObjectPacket
                {
                    EntityId = obj.EntityId, ActionId = ActionId.UseObject,
                    ActionArgId = invalid == "wrong-argument" ? 1U : 3U
                });

                Assert.AreEqual(0, harness.Map.PerformRecovery.Count);
                Assert.AreEqual(0, obj.TriggeredByPlayers.Count);
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    harness.Client.Player.Missions[MissionId].Objectives[1].State);
            }
            finally
            {
                if (second != null)
                {
                    CellManager.Instance.RemoveFromWorld(second);
                    harness.Map.ClientList.Remove(second);
                }
            }
        }

        [TestMethod]
        public void RealObjectDamageCompletesOnlyAtDestructionAndCannotReplay()
        {
            using var harness = Create(destructible: true);
            var obj = Target(harness);
            Assert.IsTrue(PracticeTargetManager.TryGetTarget(harness.Map, obj.EntityId, out var target));
            Assert.AreSame(obj, target);

            Hit(99);
            Assert.IsTrue(obj.IsEnabled);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[MissionId].Objectives[1].State);

            Hit(1);
            Assert.IsFalse(obj.IsEnabled);
            Assert.AreEqual(UseObjectState.StateDestroyed, obj.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[MissionId].Objectives[1].State);
            Assert.IsFalse(PracticeTargetManager.TryGetTarget(harness.Map, obj.EntityId, out _));
            using var unit = harness.CreateChar();
            Assert.AreEqual(1U, unit.CharacterFlags.Get(1)[65010]);

            void Hit(int damage)
            {
                var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0)
                {
                    TargetId = obj.EntityId
                };
                MissileManager.Instance.MissileLaunch(harness.Map, action, damage);
                Assert.AreEqual(1, harness.Map.QueuedMissiles.Count);
                MissileManager.Instance.DoWork(harness.Map, 1000);
            }
        }

        [TestMethod]
        public void LearnedHigherPumpLightningDamagesTheNativeDestructibleObject()
        {
            using var harness = Create(destructible: true);
            var abilities = harness.LoadAbilities();
            Assert.IsTrue(abilities.TryGetLevel(ActionId.AaRecruitLightning, 2, out var level));
            Assert.AreEqual(2U, level.Level);
            harness.Client.Player.Level = 5;
            harness.Client.Player.Skills[SkillId.Lightning] = new SkillsData(SkillId.Lightning, (int)ActionId.AaRecruitLightning, 2);
            using (var unit = harness.CreateChar())
                unit.CharacterSkills.AddOrUpdate(1, (uint)SkillId.Lightning, (int)ActionId.AaRecruitLightning, 2);
            var target = Target(harness);
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)))
                writer.WriteULong(target.EntityId);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var request = new RequestPerformAbilityPacket
            {
                ActionId = ActionId.AaRecruitLightning, ActionArgId = 2, Target = ActionTarget.Read(reader)
            };
            lock (Server.Clients)
                Server.Clients.Add(harness.Client);
            try
            {
                abilities.RequestPerformAbility(harness.Client, request);
                var action = harness.Map.PerformRecovery.SingleOrDefault(entry => entry.ActionId == ActionId.AaRecruitLightning);
                Assert.IsNotNull(action, "A learned higher pump must reach the real object damage recovery.");

                abilities.PerformRecovery(harness.Map, action);

                Assert.IsTrue(target.CurrentHitPoints < 100, "The native ability recovery must apply actual object damage.");
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
        }

        [TestMethod]
        public void NativeAlternateMeleeCannotDestroyAnObjectOutsideItsAuthoredRange()
        {
            using var harness = Create(destructible: true);
            var abilities = harness.LoadAbilities();
            harness.Client.Player.Skills[SkillId.Firearms] = new SkillsData(SkillId.Firearms, -1, 1);
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                        new[] { new InventoryManager.InventoryItemGrant(13713, 1) }, unit));
                grant.Publish(harness.Client);
            }
            var rifle = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplateId == 13713);
            while (harness.Client.Player.Inventory.WeaponDrawer.Count < 5)
                harness.Client.Player.Inventory.WeaponDrawer.Add(0);
            harness.Client.Player.ActiveWeapon = 0;
            InventoryManager.Instance.RequestEquipWeapon(harness.Client, new RequestEquipWeaponPacket
            {
                InventoryType = InventoryType.Personal, SrcSlot = rifle.OwnerSlotId, DestSlot = 0
            });
            ManifestationManager.Instance.WeaponReady(harness.Client, true);
            Assert.AreSame(rifle, InventoryManager.Instance.CurrentWeapon(harness.Client));
            var weapon = rifle.ItemTemplate.WeaponInfo;
            Assert.IsTrue(abilities.TryGetLevel((ActionId)weapon.AltActionId, weapon.AltActionArgId, out var melee));
            Assert.IsTrue(melee.MaxRange + 2.5 < 50);
            var target = Target(harness);
            harness.MoveTo(target.Position + new Vector3(50, 0, 0));
            var request = new RequestWeaponAttackPacket
            {
                ActionId = (ActionId)weapon.AltActionId, ActionArgId = (int)weapon.AltActionArgId,
                TargetId = (long)target.EntityId, IsAltAction = true
            };

            MissileManager.Instance.RequestWeaponAttack(harness.Client, request);

            Assert.AreEqual(0, harness.Map.QueuedMissiles.Count, "Melee must use its own native range, not the ranged missile limit.");
            Assert.AreEqual(100U, target.CurrentHitPoints);
            harness.MoveTo(target.Position + Vector3.UnitX);
            harness.Client.Player.NextMeleeAt = 0;
            MissileManager.Instance.RequestWeaponAttack(harness.Client, request);
            Assert.AreEqual(1, harness.Map.QueuedMissiles.Count);
            MissileManager.Instance.DoWork(harness.Map, 1000);
            Assert.IsTrue(target.CurrentHitPoints < 100);
        }

        [TestMethod]
        [DataRow(0, CharacterState.Idle)]
        [DataRow(100, CharacterState.Dying)]
        public void ArrivedRouteCannotPublishCompletionForAZeroHealthOrDyingActor(int health, CharacterState state)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(530070);
            var actor = harness.Npc(530070);
            Assert.IsNotNull(actor);
            var handle = new ActorHandle(Guid.NewGuid().ToString("N"), "pierre", 1, harness.Map.MissionEpoch);
            var observations = new List<SceneObservation>();
            var controller = new SceneRouteController(
                (map, requested) => map == harness.Map && requested == handle ? actor : null,
                (_, observation) => observations.Add(observation));
            var point = new ScenePosition(actor.Position.X, actor.Position.Y, actor.Position.Z);
            controller.Start(harness.Map, handle, actor, new RunRouteIntent("arrive", "pierre", "end"),
                new SceneRoute("end", new[] { new SceneWaypoint(point) }));
            Assert.IsNotNull(actor.Controller.ScriptedMove);
            actor.Controller.ScriptedMove.Arrived = true;
            actor.Attributes[Attributes.Health].Current = health;
            actor.State = state;

            controller.Tick(harness.Map, harness.UtcNow);

            Assert.IsFalse(observations.Any(observation => observation.Kind == SceneEventKind.RouteCompleted));
            Assert.IsTrue(observations.Any(observation => observation.Kind == SceneEventKind.ActorDied));
        }

        [TestMethod]
        public void SceneDeadlineUsesTheMissionClockAndCannotBeSatisfiedAfterItsDurableDueTime()
        {
            using var harness = Create(destructible: false, timed: true);
            using (var unit = harness.CreateChar())
                Assert.AreEqual(harness.UtcNow.AddSeconds(1),
                    unit.CharacterMissionDeadlines.Get(1, MissionId).DueAtUtc);
            harness.UtcNow = harness.UtcNow.AddSeconds(1);

            Use(harness, harness.Client, Target(harness), 3);

            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[MissionId].Objectives[1].State);
            using var verify = harness.CreateChar();
            Assert.IsFalse(verify.CharacterFlags.Get(1).ContainsKey(65010));
        }

        private static WildernessRuntimeTestHarness Create(bool destructible, bool timed = false)
        {
            var harness = WildernessRuntimeTestHarness.Create(migration =>
            {
                var data = new WildernessMissionDataV1(migration, MissionId, "Scoped object fixture",
                    1971, 100, 100, 1);
                data.Objective(1, 2662, 2664, 1, MissionObjectiveState.Incomplete);
                data.Transition(1);
                data.Progress(1, MissionProgressEventKind.ScenarioEvent, 1, counter: 1);
                data.Scenario(1, "Native object result");
                migration.InsertData("mission_scenario_step",
                    new[]
                    {
                        "mission_id", "content_revision", "scenario_id", "step_id", "kind", "sequence",
                        "requirement", "scenario_event_id", "comment"
                    },
                    new object[]
                    {
                        MissionId, WildernessMissionDataV1.Revision, 1U, 1U,
                        (byte)MissionScenarioStepKind.EmitScenarioEvent, 1U,
                        (byte)MissionContentRequirement.Required, 1U, "Native source result"
                    });
                data.Reward(0, 0);
                data.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
                var scene = new MissionSceneDefinition { Script = "data.sequence" };
                scene.Actors["target"] = new SceneActorDefinition("target", SceneActorKind.Object,
                    destructible ? 9260U : 7827U, new ScenePosition(868, 294.31787f, 383),
                    InitialObjectState: destructible ? 110U : 55U, WindupMilliseconds: 100,
                    UseAction: destructible ? null : new SceneObjectAction(MissionId, 1, 1, 3),
                    Destruction: destructible ? new SceneObjectDestruction(MissionId, 1, 1, 100) : null);
                scene.Sequences[0] = new SceneSequenceDefinition
                {
                    World = { new EnsureActorIntent("create-target", "target") }
                };
                scene.Sequences[1] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent("target-finished", "target",
                            destructible ? (uint)UseObjectState.StateDestroyed : 56U),
                        new SetInteractionIntent("disable-target", "target", false)
                    },
                    Character = { new SetCharacterFlagIntent("object-receipt", 65010, 1) },
                    Signals = { new SceneMissionSignal(MissionId, 1, 1) }
                };
                if (timed)
                {
                    scene.Sequences[0].Character.Add(
                        new MissionDeadlineIntent("start-clock", MissionId, DeadlineIntentKind.Start, 1000));
                    scene.Sequences[1].Character.Insert(0,
                        new MissionDeadlineIntent("satisfy-clock", MissionId, DeadlineIntentKind.Satisfy));
                }
                data.Enable(scene);
            });
            try
            {
                harness.SpawnWorld(100);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(100).EntityId, MissionId));
                harness.MoveTo(Target(harness).Position);
                harness.Drain();
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        private static DynamicObject Target(WildernessRuntimeTestHarness harness) =>
            harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == MissionId && obj.SceneActorRole == "target");

        private static void Use(WildernessRuntimeTestHarness harness, Client client, DynamicObject obj, uint argument)
        {
            harness.Objects.RequestUseObjectPacket(client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            var recovery = harness.Map.PerformRecovery.Single();
            harness.Map.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(harness.Map, recovery);
        }
    }
}
