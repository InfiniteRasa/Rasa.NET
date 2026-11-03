using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Structures.Char;
using Rasa.Packets.Game.Server;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class RanjaEggClusterRegressionTests
    {
        [TestMethod]
        public void EveryClusterIsShownOnlyToItsOwnerAndAllFourTakeWeaponDamage()
        {
            using var h = WildernessRanjaGorgeTests.CreateMigrated();
            h.SpawnWorld(206);
            var sten = h.Npc(206);
            var second = h.Context.CreateAdditionalClient(2, manager: h.Manager);
            var bystander = h.Context.CreateAdditionalClient(3, manager: h.Manager);
            second.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            WildernessRanjaGorgeTests.SetQualification(h, second.Player.Id, CharacterStartingExperienceState.Completed);
            second.SetWorldPosition(sten.Position, second.Player.Rotation);
            CellManager.Instance.UpdateVisibility(second);
            h.MoveTo(sten.Position);
            try
            {
                Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, sten.EntityId, 860));
                Assert.IsTrue(h.Manager.AcceptOfferedMission(second, sten.EntityId, 860));
                var mine = Clusters(h, h.Client);
                var theirs = Clusters(h, second);
                Assert.AreEqual(4, mine.Length);
                Assert.AreEqual(4, theirs.Length);
                for (var i = 0; i < 4; i++)
                {
                    Assert.AreEqual(mine[i].Position, theirs[i].Position);
                    foreach (var client in new[] { h.Client, second, bystander })
                    {
                        var sent = Arrive(h, client, mine[i].Position).OfType<CreatePhysicalEntityPacket>()
                            .Where(packet => (uint)packet.ClassId == 10180).Select(packet => packet.EntityId).ToArray();
                        var ownVisible = h.Map.DynamicObjects.Where(obj => obj.SceneMissionId == 860 &&
                            obj.SceneOwnerCharacterId == client.Player.Id && CellManager.Instance.ClientsSeeing(h.Map, obj).Contains(client))
                            .Select(obj => obj.EntityId).ToArray();
                        CollectionAssert.AreEquivalent(ownVisible, sent);
                        Assert.IsFalse(sent.Contains(client == h.Client ? theirs[i].EntityId : mine[i].EntityId));
                        Assert.AreEqual(client != bystander, sent.Length > 0);
                    }
                    Assert.IsTrue(DynamicObjectManager.IsTargetableFor(h.Client, mine[i]), mine[i].SceneActorRole);
                    Assert.IsFalse(DynamicObjectManager.IsTargetableFor(h.Client, theirs[i]));
                    Hit(h, mine[i], h.Client, 99);
                    Assert.AreEqual(1U, mine[i].CurrentHitPoints);
                    Hit(h, mine[i], h.Client, 1);
                    Assert.AreEqual((uint)i + 1, Count(h));
                    Assert.AreEqual(100U, theirs[i].CurrentHitPoints);
                    Assert.AreEqual(0U, second.Player.Missions[860].Objectives[1].Counters[0]);
                    Assert.IsFalse(PracticeTargetManager.TryGetTarget(h.Map, mine[i].EntityId, out _));
                }
                Assert.AreEqual(MissionObjectiveState.Completed, h.Client.Player.Missions[860].Objectives[1].State);
                Assert.IsTrue(RewardAtSten(h, sten));
            }
            finally
            {
                foreach (var client in new[] { second, bystander })
                {
                    h.Manager.Scenes.Detach(client, h.Map);
                    CellManager.Instance.RemoveFromWorld(client);
                    h.Map.ClientList.Remove(client);
                }
            }
        }

        [TestMethod]
        public void ReconnectAtThreeOfFourPreservesCreditAndRecreatesOnlyTheMissingFirstCluster()
        {
            using var h = WildernessRanjaGorgeTests.CreateMigrated();
            Accept(h);
            var eggs = Clusters(h, h.Client);
            foreach (var egg in eggs.Skip(1)) Hit(h, egg, h.Client, 100);
            Assert.AreEqual(3U, Count(h));
            var assignment = h.Client.Player.Missions[860].AssignmentId;
            h.Manager.Scenes.Detach(h.Client, h.Map);
            WildernessRanjaGorgeTests.Reload(h);
            h.Manager.PublishInitialState(h.Client);
            Assert.AreEqual(assignment, h.Client.Player.Missions[860].AssignmentId);
            Assert.AreEqual(3U, Count(h));
            var remaining = Clusters(h, h.Client);
            Assert.AreEqual(1, remaining.Length);
            Assert.AreEqual("egg-cluster-1", remaining[0].SceneActorRole);
            Assert.AreEqual(100U, remaining[0].CurrentHitPoints);
            Assert.IsTrue(DynamicObjectManager.IsTargetableFor(h.Client, remaining[0]));
            Hit(h, remaining[0], h.Client, 100);
            Assert.AreEqual(4U, Count(h));
            Assert.IsTrue(RewardAtSten(h, h.Npc(206)));
        }

        [TestMethod]
        public void ANewAssignmentGetsFourFreshClustersAndOldObjectsCannotCountAgain()
        {
            using var h = WildernessRanjaGorgeTests.CreateMigrated();
            Accept(h);
            var old = Clusters(h, h.Client);
            foreach (var egg in old.Skip(1)) Hit(h, egg, h.Client, 100);
            var assignment = h.Client.Player.Missions[860].AssignmentId;
            Assert.IsTrue(h.Manager.TryAbandon(h.Client, 860));
            h.MoveTo(h.Npc(206).Position);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, h.Npc(206).EntityId, 860));
            Assert.AreNotEqual(assignment, h.Client.Player.Missions[860].AssignmentId);
            Assert.AreEqual(0U, Count(h));
            var fresh = Clusters(h, h.Client);
            Assert.AreEqual(4, fresh.Length);
            Assert.IsTrue(fresh.All(egg => egg.CurrentHitPoints == 100 && egg.IsEnabled &&
                !old.Any(previous => ReferenceEquals(previous, egg))));
            foreach (var egg in old)
                Assert.IsFalse(h.Manager.Scenes.DamageObject(h.Client, egg, 100), "Stale object references cannot advance a new assignment.");
            Assert.AreEqual(0U, Count(h));
            foreach (var egg in fresh) Hit(h, egg, h.Client, 100);
            Assert.AreEqual(4U, Count(h));
            Assert.IsTrue(RewardAtSten(h, h.Npc(206)));
        }

        static void Accept(WildernessRuntimeTestHarness h)
        {
            h.SpawnWorld(206);
            h.MoveTo(h.Npc(206).Position);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, h.Npc(206).EntityId, 860));
        }
        static bool RewardAtSten(WildernessRuntimeTestHarness h, Creature sten)
        {
            h.MoveTo(sten.Position);
            return h.Manager.CompleteOfferedMission(h.Client, sten.EntityId, 860, 0);
        }
        static uint Count(WildernessRuntimeTestHarness h) => h.Client.Player.Missions[860].Objectives[1].Counters[0];
        static DynamicObject[] Clusters(WildernessRuntimeTestHarness h, Client client) => h.Map.DynamicObjects
            .Where(obj => obj.SceneMissionId == 860 && obj.SceneOwnerCharacterId == client.Player.Id && obj.IsEnabled && obj.IsInWorld)
            .OrderBy(obj => obj.SceneActorRole).ToArray();
        static System.Collections.Generic.IReadOnlyList<Rasa.Packets.PythonPacket> Arrive(WildernessRuntimeTestHarness h, Client client, Vector3 position)
        {
            client.SetWorldPosition(position + new Vector3(900, 0, 900), client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(client);
            MissionTestContext.Drain(client);
            client.SetWorldPosition(position, client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(client);
            return MissionTestContext.Drain(client);
        }
        static void Hit(WildernessRuntimeTestHarness h, DynamicObject egg, Client client, int damage, bool queued = true)
        {
            client.SetWorldPosition(egg.Position + new Vector3(0, 0, 2), client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(client);
            MissileManager.Instance.MissileLaunch(h.Map,
                new ActionData(client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = egg.EntityId }, damage);
            Assert.AreEqual(queued ? 1 : 0, h.Map.QueuedMissiles.Count);
            MissileManager.Instance.DoWork(h.Map, 1000);
        }
    }
}
