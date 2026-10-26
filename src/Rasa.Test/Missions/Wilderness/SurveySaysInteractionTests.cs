using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
namespace Rasa.Test.Missions.Wilderness
{
    [TestClass, DoNotParallelize]
    public class SurveySaysInteractionTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SurveyFinishesOrCancelsAndAreaCanBeCollectedWithoutRelogging(bool cancel)
        {
            using var h=WildernessElohPinholeTests.CreateHarness();
            h.SpawnWorld(205);
            WildernessElohPinholeTests.CompleteEarlierHistory(h,506);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client,h.Npc(205).EntityId,508));
            var survey=h.Map.DynamicObjects.Single(o=>o.SceneMissionId==508 && o.SceneActorRole=="survey-unit-1");
            h.MoveTo(new Vector3(441.6f,203.7f,560.7f)); h.Drain();
            h.Objects.RequestUseObjectPacket(h.Client,new RequestUseObjectPacket {EntityId=survey.EntityId,ActionId=ActionId.UseObject,ActionArgId=3});
            Assert.AreEqual(1,h.Map.PerformRecovery.Count(a=>a.Actor==h.Client.Player && a.SourceId==survey.EntityId));
            Assert.AreSame(h.Client.Player,survey.UsedBy);
            var windup=h.Drain().OfType<PerformWindupPacket>().Single(p=>p.ActionId==ActionId.UseObject);
            Assert.AreEqual(PerformType.ThreeArgs,windup.PerformType,"UseObject.Windup(actor, usableId) needs the target.");
            Assert.AreEqual(survey.EntityId,windup.Arg);
            if(cancel) ActorManager.Instance.RequestActionInterrupt(h.Client,new RequestActionInterruptPacket {ActionId=ActionId.UseObject,ActionArgId=3});
            ActorActionManager.Instance.DoWork(h.Map,survey.WindupTime+1);
            var sent=h.Drain();
            Assert.AreEqual(0,h.Map.PerformRecovery.Count(a=>a.Actor==h.Client.Player));
            Assert.IsNull(survey.UsedBy);
            Assert.IsFalse(survey.TriggeredByPlayers.Contains(h.Client));
            Assert.IsTrue(sent.OfType<LockToActorPacket>().Any(p=>p.ActorId==0));
            if(cancel) Assert.IsTrue(sent.OfType<ActionInterruptPacket>().Any(p=>p.ActionArgId==3));
            else Assert.IsTrue(sent.OfType<PerformRecoveryPacket>().Any(p=>p.ActionId==ActionId.UseObject && p.ActionArgId==3));
            Assert.AreEqual(cancel?0U:1U,h.Client.Player.Missions[508].Objectives[7].Counters[0]);
            Assert.AreEqual(cancel?UseObjectState.TsState0:UseObjectState.TsState1,survey.StateId);
            var shrine=h.Map.DynamicObjects.OfType<Logos>().Single(o=>o.Id==1);
            var reported=new Vector3(366.2f,213.2f,610.8f);
            var route=h.Map.NavMesh.FindPath(reported,shrine.Position,out var complete);
            Assert.IsTrue(complete); Assert.IsNotEmpty(route);
            var stand=route.Last();
            Assert.IsTrue(Vector3.Distance(stand,shrine.Position)<3,"A reachable standing point fits the native client use radius.");
            h.MoveTo(stand);h.Drain();
            h.Objects.RequestUseObjectPacket(h.Client,new RequestUseObjectPacket {EntityId=shrine.EntityId,ActionId=ActionId.UseObject,ActionArgId=6});
            var logosWindup=h.Drain().OfType<PerformWindupPacket>().Single(p=>p.ActionId==ActionId.UseObject);
            Assert.AreEqual(PerformType.ThreeArgs,logosWindup.PerformType);
            Assert.AreEqual(shrine.EntityId,logosWindup.Arg);
            ActorActionManager.Instance.DoWork(h.Map,10001);
            Assert.IsTrue(h.Client.Player.Logos.Contains(1));
            Assert.AreEqual(0,h.Map.PerformRecovery.Count(a=>a.Actor==h.Client.Player));
            Assert.IsNull(shrine.UsedBy);
            using var unit=h.CreateChar();
            Assert.AreEqual(1,unit.CharacterLogoses.GetLogos(h.Client.Player.Id).Count(id=>id==1));
        }

        [TestMethod]
        public void BystanderWithoutSurveySaysCannotSeeTargetOrChannelAnotherPlayersSurvey()
        {
            using var h=WildernessElohPinholeTests.CreateHarness();
            Assert.IsFalse(h.Map.DynamicObjects.Any(o=>o.SceneMissionId==508));
            h.SpawnWorld(205); WildernessElohPinholeTests.CompleteEarlierHistory(h,506);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client,h.Npc(205).EntityId,508));
            var survey=h.Map.DynamicObjects.Single(o=>o.SceneMissionId==508 && o.SceneActorRole=="survey-unit-1");
            var bystander=h.Context.CreateAdditionalClient(2,manager:h.Manager);
            try
            {
                bystander.SetWorldPosition(new Vector3(441.6f,203.7f,560.7f),0);
                Assert.IsFalse(DynamicObjectManager.IsShownTo(bystander,survey));
                Assert.IsFalse(DynamicObjectManager.IsTargetableFor(bystander,survey));
                Assert.IsFalse(MissionObjects.InfoFor(bystander,survey,true,survey.WindupTime,h.Manager).Enabled);
                h.Objects.RequestUseObjectPacket(bystander,new RequestUseObjectPacket {EntityId=survey.EntityId,ActionId=ActionId.UseObject,ActionArgId=3});
                Assert.IsFalse(h.Map.PerformRecovery.Any(a=>a.Actor==bystander.Player));
                Assert.IsNull(survey.UsedBy);
                var refused=World.WorldTestContext.Drain(bystander).Select(p=>p.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>().Select(p=>p.Packet).ToArray();
                Assert.IsTrue(refused.OfType<UserActionFailedPacket>().Any(p=>p.ActionArgId==3));
                Assert.IsTrue(refused.OfType<ActionFailedPacket>().Any(p=>p.ActionArgId==3));
            }
            finally { CellManager.Instance.RemoveFromWorld(bystander); h.Map.ClientList.Remove(bystander); }
        }
    }
}