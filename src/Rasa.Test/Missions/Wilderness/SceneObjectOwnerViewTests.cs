using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    /// <summary>
    /// A mission scene's object is shown to its owner alone (DynamicObjectManager.IsShownTo).
    /// Hitting 'em Where It Hurts (432) puts three gas harvesters down for each player who takes
    /// it, each player's in the same three places; every copy went to everyone in range, and a
    /// player who locked on to someone else's could not damage it or use Lightning on it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SceneObjectOwnerViewTests
    {
        private const uint GasHarvesters = 432;
        private const uint HarvesterClass = 7906;

        [TestMethod]
        public void EachPlayerOnTheMissionIsSentTheirOwnHarvesterAndNoOneElses()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            harness.SpawnWorld(213);
            var jennings = harness.Npc(213);
            var second = Additional(harness, 2);
            var bystander = Additional(harness, 3);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, jennings.EntityId, GasHarvesters));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, jennings.EntityId, GasHarvesters));

            var mine = Harvester(harness, harness.Client);
            var theirs = Harvester(harness, second);
            Assert.AreNotEqual(mine.EntityId, theirs.EntityId);
            Assert.IsTrue(Vector3.Distance(mine.Position, theirs.Position) < 0.01f, "two copies in one place");

            foreach (var client in new[] { harness.Client, second, bystander })
            {
                var sent = ArriveAtHarvester(harness, client)
                    .OfType<CreatePhysicalEntityPacket>().Where(packet => (uint)packet.ClassId == HarvesterClass)
                    .Select(packet => packet.EntityId).ToList();
                var own = harness.Map.DynamicObjects
                    .Where(obj => obj.SceneMissionId == GasHarvesters && obj.SceneOwnerCharacterId == client.Player.Id &&
                        Vector3.Distance(obj.Position, mine.Position) < 0.01f)
                    .Select(obj => obj.EntityId).ToList();

                CollectionAssert.AreEquivalent(own, sent, $"character {client.Player.Id} is sent its own harvester, and no one else's");
                Assert.IsFalse(CellManager.Instance.ClientsSeeing(harness.Map, client == second ? mine : theirs).Contains(client));
            }

            // Not even a method call on one they have not been given.
            Discard(second, bystander);
            CellManager.Instance.CellCallMethod(harness.Map, mine, new UpdateHitPointsPacket(50));
            Assert.IsFalse(MissionTestContext.Drain(second).OfType<UpdateHitPointsPacket>().Any());
            Assert.IsFalse(MissionTestContext.Drain(bystander).OfType<UpdateHitPointsPacket>().Any());
            Assert.IsTrue(harness.Drain().OfType<UpdateHitPointsPacket>().Any(), "its owner hears of it");

            // Nor told it has gone.
            Discard(second, bystander);
            CellManager.Instance.RemoveFromWorld(harness.Map, theirs);
            Assert.IsTrue(MissionTestContext.Drain(second).OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == theirs.EntityId));
            Assert.IsFalse(harness.Drain().OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == theirs.EntityId));
            Assert.IsFalse(MissionTestContext.Drain(bystander).OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == theirs.EntityId));

            Remove(harness, second, bystander);
        }

        /// <summary>The report: with a second player's harvester where theirs is, Lightning takes theirs down.</summary>
        [TestMethod]
        public void LightningDestroysTheirOwnHarvesterBesideAnothersAndTheMissionCountsIt()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            harness.SpawnWorld(213);
            var abilities = harness.LoadAbilities();
            var jennings = harness.Npc(213);
            var second = Additional(harness, 2);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, jennings.EntityId, GasHarvesters));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, jennings.EntityId, GasHarvesters));
            Learn(harness);
            var mine = Harvester(harness, harness.Client);
            var theirs = Harvester(harness, second);
            ArriveAtHarvester(harness, harness.Client);

            // Someone else's, were the client to send it: refused, and it stands.
            Assert.AreEqual(PlayerMessage.PmActionFailedNoTarget, Lightning(harness, abilities, theirs));
            Assert.AreEqual(100U, theirs.CurrentHitPoints);

            // Their own, the one they are sent: down, and counted.
            Assert.IsNull(Lightning(harness, abilities, mine));
            Assert.AreEqual(0U, mine.CurrentHitPoints);
            Assert.AreEqual(UseObjectState.StateDestroyed, mine.StateId);
            Assert.AreEqual(1U, harness.Client.Player.Missions[GasHarvesters].Objectives[1].Counters[0]);
            Assert.AreEqual(0U, second.Player.Missions[GasHarvesters].Objectives[1].Counters[0]);
            Assert.AreEqual(100U, theirs.CurrentHitPoints);

            Remove(harness, second);
        }

        #region Fixture

        private static Client Additional(WildernessRuntimeTestHarness harness, uint id)
        {
            var client = harness.Context.CreateAdditionalClient(id, manager: harness.Manager);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            return client;
        }

        private static void Remove(WildernessRuntimeTestHarness harness, params Client[] clients)
        {
            foreach (var client in clients)
            {
                CellManager.Instance.RemoveFromWorld(client);
                harness.Map.ClientList.Remove(client);
            }
        }

        private static void Discard(params Client[] clients)
        {
            foreach (var client in clients)
                MissionTestContext.Drain(client);
        }

        private static DynamicObject Harvester(WildernessRuntimeTestHarness harness, Client owner) =>
            harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == GasHarvesters &&
                obj.SceneActorRole == "gas-harvester-1" && obj.SceneOwnerCharacterId == owner.Player.Id);

        /// <summary>From far off to beside the first harvester: what they are sent on the way.</summary>
        private static System.Collections.Generic.IReadOnlyList<PythonPacket> ArriveAtHarvester(WildernessRuntimeTestHarness harness, Client client)
        {
            var (approach, _) = WildernessElohPinholePlacementTests.HarvesterApproach("gas-harvester-1");
            client.SetWorldPosition(approach + new Vector3(900f, 0f, 900f), client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(client);
            MissionTestContext.Drain(client);
            client.SetWorldPosition(approach, client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(client);
            return MissionTestContext.Drain(client);
        }

        private static void Learn(WildernessRuntimeTestHarness harness)
        {
            var player = harness.Client.Player;
            player.Level = 6;
            player.Skills[SkillId.Lightning] = new SkillsData(SkillId.Lightning, (int)ActionId.AaRecruitLightning, 1);
            player.Logos.Add(AbilityLogos.Power);
            using var unit = harness.CreateChar();
            unit.CharacterSkills.AddOrUpdate(player.Id, (uint)SkillId.Lightning, (int)ActionId.AaRecruitLightning, 1);
            unit.CharacterLogoses.SetLogos(player.Id, AbilityLogos.Power);
        }

        /// <summary>Lightning at the object, through the request and its recovery: the refusal, or null.</summary>
        private static PlayerMessage? Lightning(WildernessRuntimeTestHarness harness, AbilityManager abilities, DynamicObject target)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true)))
                writer.WriteULong(target.EntityId);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var request = new RequestPerformAbilityPacket
            {
                ActionId = ActionId.AaRecruitLightning, ActionArgId = 1, Target = ActionTarget.Read(reader)
            };

            harness.Client.Player.ActionReuseUntil.Clear();
            harness.Drain();
            lock (Server.Clients)
                Server.Clients.Add(harness.Client);
            try
            {
                abilities.RequestPerformAbility(harness.Client, request);
                var refused = harness.Drain().OfType<UserActionFailedPacket>().FirstOrDefault();
                if (refused != null)
                    return refused.MsgId ?? PlayerMessage.PmCannotPerformActionNow;

                var action = harness.Map.PerformRecovery.Single(entry => entry.ActionId == ActionId.AaRecruitLightning);
                harness.Map.PerformRecovery.Remove(action);
                abilities.PerformRecovery(harness.Map, action);
                return null;
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
        }

        #endregion
    }
}
