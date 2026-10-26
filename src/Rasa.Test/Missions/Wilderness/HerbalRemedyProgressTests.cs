using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass, DoNotParallelize]
    public class HerbalRemedyProgressTests
    {
        private static WildernessRuntimeTestHarness Ready()
        {
            var h=WildernessRuntimeTestHarness.Create(); h.SpawnWorld(175,191);
            WildernessDaghdasUrnTests.SeedHistory(h,682);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client,h.Npc(175).EntityId,695));
            Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client,h.Npc(191).EntityId,695,4,1));
            return h;
        }
        private static MissionObjectiveItemCounter Counter(WildernessRuntimeTestHarness h)
        {
            Assert.IsTrue(h.Manager.TryGetOperationalMission(695,out var definition));
            return h.Manager.BuildPublishedMissionInfo(h.Client.Player,definition,h.Client.Player.Missions[695])
                .ObjectivesList.Single(o=>o.ObjectiveId==1).ItemCounters[10513];
        }
        [TestMethod]
        public void OwnedHerbsGlowAndEverySuccessfulHarvestPublishesItsNativeItemCounter()
        {
            using var h=Ready();
            for(var n=1;n<=4;n++)
            {
                var herb=WildernessDaghdasUrnTests.Object(h,695,$"tinctu-{n}");
                h.MoveTo(herb.Position);
                Assert.AreEqual(695U,MissionObjects.InfoFor(h.Client,herb,true,herb.WindupTime,h.Manager).MissionActivated);
                h.Drain(); WildernessDaghdasUrnTests.Use(h,herb);
                var info=h.Drain().OfType<MissionStatusInfoPacket>().Last().MissionStatusDict[695];
                var count=info.ObjectivesList.Single(o=>o.ObjectiveId==1).ItemCounters[10513];
                Assert.AreEqual((uint)n,count.CounterValue); Assert.AreEqual(5U,count.TargetValue);
                Assert.AreEqual(MissionObjectiveState.Incomplete,h.Client.Player.Missions[695].Objectives[1].State);
                Assert.AreEqual(0U,MissionObjects.InfoFor(h.Client,herb,true,herb.WindupTime,h.Manager).MissionActivated);
            }
            Assert.AreEqual(4U,Counter(h).CounterValue);
        }
        [TestMethod]
        [DataRow("other-character")]
        [DataRow("old-assignment")]
        [DataRow("wrong-generation")]
        [DataRow("wrong-key")]
        [DataRow("unowned")]
        public void JournalCountsOnlyTheCurrentAssignmentsPhysicalHerbs(string change)
        {
            using var h=Ready();
            WildernessDaghdasUrnTests.Use(h,WildernessDaghdasUrnTests.Object(h,695,"tinctu-1"));
            var item=h.Client.Player.Inventory.PersonalInventory.Where(id=>id!=0).Select(EntityManager.Instance.GetItem).Single(i=>i?.MissionOwnership?.ItemKey=="tinctu-herbs");
            var owner=item.MissionOwnership;
            item.MissionOwnership=change switch {
                "other-character"=>owner with { CharacterId=owner.CharacterId+1 },
                "old-assignment"=>owner with { AssignmentId=new string('a',32) },
                "wrong-generation"=>owner with { Generation=owner.Generation+1 },
                "wrong-key"=>owner with { ItemKey="other" }, _=>null };
            try { Assert.AreEqual(0U,Counter(h).CounterValue); }
            finally { item.MissionOwnership=owner; }
            Assert.AreEqual(1U,Counter(h).CounterValue);
        }
        [TestMethod]
        public void LoginSnapshotRestoresPartialHerbProgressFromDurableAssignmentOwnedInventory()
        {
            using var h=Ready();
            WildernessDaghdasUrnTests.Use(h,WildernessDaghdasUrnTests.Object(h,695,"tinctu-1"));
            WildernessDaghdasUrnTests.Use(h,WildernessDaghdasUrnTests.Object(h,695,"tinctu-2"));
            var reconnected=h.Context.CreateCompetingClient(h.Manager);
            InventoryManager.Instance.InitForClient(reconnected);
            MissionTestContext.Drain(reconnected);
            h.Manager.PublishInitialState(reconnected);
            var info=MissionTestContext.Drain(reconnected).OfType<MissionStatusInfoPacket>().Last().MissionStatusDict[695];
            Assert.AreEqual(2U,info.ObjectivesList.Single(o=>o.ObjectiveId==1).ItemCounters[10513].CounterValue);
        }
    }
}
