using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass, DoNotParallelize]
    public class TinctuHerbInteractionTests
    {
        private static WildernessRuntimeTestHarness Ready()
        {
            var h = WildernessRuntimeTestHarness.Create();
            h.SpawnWorld(175, 191);
            WildernessDaghdasUrnTests.SeedHistory(h, 682);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, h.Npc(175).EntityId, 695));
            Assert.IsFalse(h.Map.DynamicObjects.Any(obj => obj.SceneMissionId == 695), "Gadfly must activate collection first.");
            Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, h.Npc(191).EntityId, 695, 4, 1));
            return h;
        }
        private static DynamicObject Herb(WildernessRuntimeTestHarness h, int number = 1) =>
            WildernessDaghdasUrnTests.Object(h, 695, $"tinctu-{number}");

        [TestMethod]
        public void NoMissionPlayerCannotSeeSelectOrUseAnotherPlayersHerbs()
        {
            using var h = Ready();
            var herb = Herb(h);
            var visitor = h.Context.CreateAdditionalClient(2, manager: h.Manager);
            try
            {
                visitor.SetWorldPosition(herb.Position, 0);
                CellManager.Instance.UpdateVisibility(visitor);
                MissionTestContext.Drain(visitor);
                h.Objects.CreateDynamicObjectOnClient(visitor, herb);
                Assert.IsFalse(MissionTestContext.Drain(visitor).OfType<CreatePhysicalEntityPacket>().Any());
                Assert.IsFalse(DynamicObjectManager.IsTargetableFor(visitor, herb));
                Assert.IsFalse(MissionObjects.InfoFor(visitor, herb, true, herb.WindupTime, h.Manager).Enabled);
                Assert.IsFalse(visitor.Player.Missions.ContainsKey(695));
                MissionTestContext.Drain(visitor);
                h.Objects.SetScenarioInteractionEnabled(h.Map, herb, false);
                var updates = MissionTestContext.Drain(visitor);
                Assert.IsFalse(updates.OfType<IsTargetablePacket>().Any());
                Assert.IsFalse(updates.OfType<UsableInfoPacket>().Any(), "Hidden herb state must not be sent to a non-owner.");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(visitor);
                h.Map.ClientList.Remove(visitor);
            }
        }

        [TestMethod]
        public void PresentationTracksMissionEligibilityAndHarvestingClearsTheNativeTargetFlag()
        {
            using var h = Ready();
            var herb = Herb(h);
            h.MoveTo(herb.Position);
            Assert.IsTrue(EntityClassManager.Instance.GetClassInfo(herb.EntityClassId).TargetFlag,
                "This native flag previously kept disabled herbs targetable.");
            h.Drain();
            h.Objects.CreateDynamicObjectOnClient(h.Client, herb);
            var creation = h.Drain().OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == herb.EntityId);
            Assert.IsTrue(creation.EntityData.OfType<IsTargetablePacket>().Single().IsTargetable);
            Assert.IsTrue(creation.EntityData.OfType<UsableInfoPacket>().Single().Enabled);

            // A journal change may precede scene cleanup. Such an object must already be inert.
            var mission = h.Client.Player.Missions[695];
            h.Client.Player.Missions.Remove(695);
            h.Drain();
            MissionObjects.Refresh(h.Client, h.Manager);
            var ineligible = h.Drain();
            Assert.IsFalse(ineligible.OfType<IsTargetablePacket>().Last().IsTargetable);
            Assert.IsFalse(ineligible.OfType<UsableInfoPacket>().Last().Enabled);
            Assert.IsFalse(DynamicObjectManager.IsTargetableFor(h.Client, herb));
            h.Client.Player.Missions.Add(695, mission);
            h.Drain();
            MissionObjects.Refresh(h.Client, h.Manager);
            var restored = h.Drain().OfType<IsTargetablePacket>().ToArray();
            Assert.IsTrue(restored.Length > 0 && restored.All(p => p.IsTargetable));

            h.Drain();
            WildernessDaghdasUrnTests.Use(h, herb);
            var harvested = h.Drain();
            Assert.IsFalse(herb.IsEnabled);
            Assert.IsFalse(DynamicObjectManager.IsTargetableFor(h.Client, herb));
            Assert.IsTrue(harvested.OfType<IsTargetablePacket>().Any(p => !p.IsTargetable));
            Assert.IsFalse(MissionObjects.InfoFor(h.Client, herb, true, herb.WindupTime, h.Manager).Enabled,
                "Explicitly disabled herbs may not be put back in service by a presentation refresh.");
        }

        [TestMethod]
        [DataRow("other-player")]
        [DataRow("no-mission")]
        [DataRow("wrong-argument")]
        [DataRow("out-of-range")]
        [DataRow("stale-generation")]
        [DataRow("abandoned")]
        [DataRow("disabled")]
        public void InvalidHerbUseResolvesTheExactPendingActionAndAllowsAnotherObject(string reason)
        {
            using var h = Ready();
            var herb = Herb(h);
            Client visitor = null;
            var client = h.Client;
            var saved = h.Client.Player.Missions[695];
            var argument = reason == "wrong-argument" ? 1U : 3U;
            try
            {
                if (reason == "other-player") client = visitor = h.Context.CreateAdditionalClient(2, manager: h.Manager);
                client.SetWorldPosition(herb.Position, 0);
                CellManager.Instance.UpdateVisibility(client);
                switch (reason)
                {
                    case "no-mission": h.Client.Player.Missions.Remove(695); break;
                    case "out-of-range": client.SetWorldPosition(herb.Position + new Vector3(100, 0, 0), 0); break;
                    case "stale-generation": herb.SceneGeneration++; break;
                    case "abandoned": new NpcManager(h, h.Manager).AbandonMission(client, new AbandonMissionPacket { MissionId = 695 }); break;
                    case "disabled": WildernessDaghdasUrnTests.Use(h, herb); break;
                }
                MissionTestContext.Drain(client);
                h.Objects.RequestUseObjectPacket(client, new RequestUseObjectPacket
                    { EntityId = herb.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument });
                var refused = MissionTestContext.Drain(client);
                var request = refused.OfType<UserActionFailedPacket>().Single();
                var action = refused.OfType<ActionFailedPacket>().Single();
                Assert.AreEqual((ActionId.UseObject, argument), (request.ActionId, request.ActionArgId));
                Assert.AreEqual((ActionId.UseObject, argument), (action.ActionId, action.ActionArgId));
                Assert.IsFalse(refused.OfType<PerformWindupPacket>().Any());
                Assert.AreEqual(0, h.Map.PerformRecovery.Count);
                Assert.IsFalse(herb.TriggeredByPlayers.Contains(client));

                // A new interaction must queue on its own source, never on the rejected herb.
                var crate = h.Objects.CreateScenarioDynamicObject(h.Map, EntityClasses.UsableLockBoxHumFootlockerV01,
                    client.Player.Position + new Vector3(1, 0, 0), 0, "tinctu-refusal-next-object", true);
                h.Map.DynamicObjects.Add(crate);
                CellManager.Instance.AddToWorld(h.Map, crate);
                crate.IsInWorld = true;
                MissionTestContext.Drain(client);
                h.Objects.RequestUseObjectPacket(client, new RequestUseObjectPacket
                    { EntityId = crate.EntityId, ActionId = ActionId.UseObject, ActionArgId = 3 });
                Assert.AreEqual(crate.EntityId, h.Map.PerformRecovery.Single().SourceId);
                var next = MissionTestContext.Drain(client);
                Assert.IsTrue(next.OfType<PerformWindupPacket>().Any());
                Assert.IsFalse(next.OfType<UserActionFailedPacket>().Any());
            }
            finally
            {
                if (reason == "no-mission") h.Client.Player.Missions[695] = saved;
                if (visitor != null)
                {
                    CellManager.Instance.RemoveFromWorld(visitor);
                    h.Map.ClientList.Remove(visitor);
                }
            }
        }
    }
}
